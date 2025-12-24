namespace _3DApi.Infrastructure.Services.Slicer;

using System.Diagnostics;
using System.Globalization;
using DataAccess.Repo;
using Errors;
using Infrastructure;
using Models;
using Microsoft.EntityFrameworkCore;

public class SlicerService : ISlicerService
{
    private readonly IGenericRepository<Material> _materialRepository;
    private readonly IGenericRepository<Printer> _printerRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SlicerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _slicerPath;

    public SlicerService(
        IGenericRepository<Material> materialRepository,
        IGenericRepository<Printer> printerRepository,
        IWebHostEnvironment environment,
        ILogger<SlicerService> logger,
        IConfiguration configuration)
    {
        _materialRepository = materialRepository;
        _printerRepository = printerRepository;
        _environment = environment;
        _logger = logger;
        _configuration = configuration;

        var configuredPath = configuration["Slicer:Path"] ?? @"E:\Programs\Slicer";

        // If path is a directory, look for common slicer executables
        if (Directory.Exists(configuredPath))
        {
            var possibleExes = new[] { "slicer.exe", "Slicer.exe" };
            var foundExe = possibleExes.FirstOrDefault(exe =>
                File.Exists(Path.Combine(configuredPath, exe)));

            _slicerPath = foundExe != null
                ? Path.Combine(configuredPath, foundExe)
                : configuredPath;
        }
        else
        {
            _slicerPath = configuredPath;
        }

        if (!File.Exists(_slicerPath) && !Directory.Exists(_slicerPath))
        {
            _logger.LogWarning($"Slicer path does not exist: {_slicerPath}");
        }
    }

    public async Task<Result<string>> SliceAsync(string stlFilePath, int materialId, int printerId)
    {
        try
        {
            // Get material and printer info for slicer parameters
            var materialResult = await _materialRepository.GetSingleByConditionAsync(m => m.Id == materialId);
            if (!materialResult.IsSuccess)
            {
                return Result<string>.Failure(
                    Error.NotFound("material.NOT_FOUND", "Material not found for slicing"));
            }

            var material = materialResult.Value;

            // Resolve full path to STL file
            var fullStlPath = Path.IsPathRooted(stlFilePath)
                ? stlFilePath
                : Path.Combine(_environment.ContentRootPath, stlFilePath);

            if (!File.Exists(fullStlPath))
            {
                return Result<string>.Failure(
                    Error.NotFound("stl.NOT_FOUND", $"STL file not found: {fullStlPath}"));
            }

            // Prepare output directory for G-code using standardized path
            var basePath = _configuration["FileStorage:BasePath"] ?? @"C:\3d";
            var gcodeFolder = _configuration["FileStorage:GCodeFolder"] ?? "gcode";
            var gcodeOutputDir = Path.Combine(basePath, gcodeFolder);
            Directory.CreateDirectory(gcodeOutputDir);

            var gcodeFileName = $"{Guid.NewGuid()}.gcode";
            var fullGcodePath = Path.Combine(gcodeOutputDir, gcodeFileName);

            string arguments = $"--output \"{fullGcodePath}\" " +
                               $"--layer-height {0.2.ToString(CultureInfo.InvariantCulture)} " +
                               $"--fill-density 20% " +
                               $"--filament-diameter {material.DiameterMm.ToString(CultureInfo.InvariantCulture)} " +
                               $"--nozzle-diameter {0.4.ToString(CultureInfo.InvariantCulture)} " +
                               $"--temperature 200 " +
                               $"--bed-temperature 60 " +
                               $"--fill-pattern rectilinear " +
                               $"--perimeters 3 " +
                               $"--top-solid-layers 3 " +
                               $"--bottom-solid-layers 3 " +
                               $"--retract-length {2.ToString(CultureInfo.InvariantCulture)} " +
                               $"--retract-speed 40 " +
                               $"--print-center 100,100 " +
                               $"\"{fullStlPath}\"";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = _slicerPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(_slicerPath) ?? _environment.ContentRootPath
            };

            using var process = new Process { StartInfo = processStartInfo };

            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for process to complete with timeout for 10 minutes
            var timeout = TimeSpan.FromMinutes(10);
            var completed = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds));

            if (!completed)
            {
                process.Kill();
                var partialOutput = outputBuilder.ToString();
                var partialError = errorBuilder.ToString();
                _logger.LogError($"Slicer timeout. Output so far:\n{partialOutput}\nErrors:\n{partialError}");
                return Result<string>.Failure(
                    Error.InternalServerError("slicer.TIMEOUT",
                        $"Slicer process timed out after {timeout.TotalMinutes} minutes"));
            }

            // Capture complete output and error logs for analysis
            var fullOutput = outputBuilder.ToString();
            var fullError = errorBuilder.ToString();

            if (process.ExitCode != 0)
            {
                _logger.LogError($"Slicer process failed with exit code {process.ExitCode}.");
                _logger.LogError($"Full Standard Output:\n{fullOutput}");
                _logger.LogError($"Full Standard Error:\n{fullError}");
                return Result<string>.Failure(
                    Error.InternalServerError("slicer.FAILED",
                        $"Slicer process failed with exit code {process.ExitCode}. Error: {fullError}. Output: {fullOutput}"));
            }

            // Verify G-code file was created
            if (!File.Exists(fullGcodePath))
            {
                _logger.LogError($"G-code file was not created at: {fullGcodePath}");
                _logger.LogError($"Full Standard Output:\n{fullOutput}");
                _logger.LogError($"Full Standard Error:\n{fullError}");
                return Result<string>.Failure(
                    Error.InternalServerError("slicer.NO_OUTPUT",
                        $"Slicer process completed but G-code file was not created. Output: {fullOutput}. Error: {fullError}"));
            }

            _logger.LogInformation($"Slicer process completed successfully for job. G-code: {fullGcodePath}");

            return Result<string>.Success(fullGcodePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error running slicer for job");
            return Result<string>.Failure(
                Error.InternalServerError("slicer.EXCEPTION",
                    $"Exception while running slicer: {ex.Message}"));
        }
    }
}