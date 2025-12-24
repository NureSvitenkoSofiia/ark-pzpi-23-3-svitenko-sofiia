namespace _3DApi.Infrastructure.Services.PrintJob;

using DataAccess.Repo;
using Errors;
using Email;
using Models;
using Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Slicer;

public class PrintJobService : IPrintJobService
{
    private readonly IGenericRepository<PrintJob> _printJobRepository;
    private readonly IGenericRepository<Material> _materialRepository;
    private readonly IGenericRepository<Printer> _printerRepository;
    private readonly IGenericRepository<PrinterMaterial> _printerMaterialRepository;
    private readonly IGenericRepository<User> _userRepository;
    private readonly IEmailService _emailService;
    private readonly ISlicerService _slicerService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PrintJobService> _logger;
    private readonly IConfiguration _configuration;

    public PrintJobService(
        IGenericRepository<PrintJob> printJobRepository,
        IGenericRepository<Material> materialRepository,
        IGenericRepository<Printer> printerRepository,
        IGenericRepository<PrinterMaterial> printerMaterialRepository,
        IGenericRepository<User> userRepository,
        IEmailService emailService,
        ISlicerService slicerService,
        IWebHostEnvironment environment,
        ILogger<PrintJobService> logger,
        IConfiguration configuration)
    {
        _printJobRepository = printJobRepository;
        _materialRepository = materialRepository;
        _printerRepository = printerRepository;
        _printerMaterialRepository = printerMaterialRepository;
        _userRepository = userRepository;
        _emailService = emailService;
        _slicerService = slicerService;
        _environment = environment;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Result<PrintJobResponse>> CreatePrintJobAsync(int userId, CreatePrintJobRequest request,
        IFormFile? stlFile)
    {
        var materialResult = await _materialRepository.GetSingleByConditionAsync(
            m => m.Id == request.RequiredMaterialId);
        if (!materialResult.IsSuccess)
        {
            return Result<PrintJobResponse>.Failure(
                Error.NotFound("material.NOT_FOUND", "Required material not found"));
        }

        var printerResult = await _printerRepository.GetSingleByConditionAsync(
            p => p.Id == request.PrinterId);
        if (!printerResult.IsSuccess)
        {
            return Result<PrintJobResponse>.Failure(
                Error.NotFound("printer.NOT_FOUND", "Printer not found"));
        }

        var userResult = await _userRepository.GetSingleByConditionAsync(
            u => u.Id == userId);
        if (!userResult.IsSuccess)
        {
            return Result<PrintJobResponse>.Failure(
                Error.NotFound("user.NOT_FOUND", "User not found"));
        }

        var printerMaterialResult = await _printerMaterialRepository.GetSingleByConditionAsync(
            pm => pm.PrinterId == request.PrinterId && pm.MaterialId == request.RequiredMaterialId);
        if (!printerMaterialResult.IsSuccess)
        {
            return Result<PrintJobResponse>.Failure(
                Error.Validation("printer.MATERIAL_NOT_AVAILABLE",
                    "Printer does not have the required material loaded"));
        }

        string? stlFilePath = null;
        string? gcodeFilePath = null;
        double estimatedMaterialInGrams = 0;
        double estimatedPrintTimeMinutes = 0;

        if (stlFile != null && stlFile.Length > 0)
        {
            var basePath = _configuration["FileStorage:BasePath"] ?? @"C:\3d";
            var stlFolder = _configuration["FileStorage:StlFolder"] ?? "stl";
            var uploadsFolder = Path.Combine(basePath, stlFolder);
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}_{stlFile.FileName}";
            var fullFilePath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = new FileStream(fullFilePath, FileMode.Create))
            {
                await stlFile.CopyToAsync(stream);
            }

            stlFilePath = fullFilePath;

            _logger.LogInformation($"Starting slicer process for new print job");
            var sliceResult = await _slicerService.SliceAsync(
                stlFilePath,
                request.RequiredMaterialId,
                request.PrinterId);

            if (!sliceResult.IsSuccess)
            {
                return Result<PrintJobResponse>.Failure(
                    Error.InternalServerError("slicing.FAILED",
                        $"Failed to slice STL file: {string.Join("; ", sliceResult.Errors.Select(e => e.Description))}"));
            }

            gcodeFilePath = sliceResult.Value;
            _logger.LogInformation($"Slicing completed. G-code: {gcodeFilePath}");

            var estimationResult = await CalculateGCodeEstimationsAsync(
                gcodeFilePath,
                materialResult.Value);

            if (estimationResult.IsSuccess)
            {
                estimatedMaterialInGrams = estimationResult.Value.MaterialInGrams;
                estimatedPrintTimeMinutes = estimationResult.Value.EstimatedTimeMinutes;
                _logger.LogInformation(
                    $"G-code analysis: Material={estimatedMaterialInGrams:F2}g, " +
                    $"PrintTime={estimatedPrintTimeMinutes:F1}min");
            }
            else
            {
                _logger.LogWarning(
                    $"Failed to calculate G-code estimations: {string.Join("; ", estimationResult.Errors.Select(e => e.Description))}");
            }
        }

        var printJob = new PrintJob
        {
            StlFilePath = stlFilePath ?? string.Empty,
            GCodeFilePath = gcodeFilePath ?? string.Empty,
            RequiredMaterialId = request.RequiredMaterialId,
            EstimatedMaterialInGrams = estimatedMaterialInGrams,
            ActualMaterialInGrams = estimatedMaterialInGrams,
            EstimatedPrintTimeMinutes = estimatedPrintTimeMinutes,
            PrinterId = request.PrinterId,
            RequesterId = userId,
            Status = "pending",
            StartedAt = null,
            CompletedAt = null
        };

        var addResult = await _printJobRepository.AddAsync(printJob);
        if (!addResult.IsSuccess)
        {
            return Result<PrintJobResponse>.Failure(addResult.Errors);
        }

        var jobResult = await _printJobRepository.GetSingleByConditionAsync(
            pj => pj.Id == printJob.Id,
            includes:
            [
                q => q.Include(pj => pj.RequiredMaterial),
                q => q.Include(pj => pj.Printer)
            ]);

        if (!jobResult.IsSuccess)
        {
            return Result<PrintJobResponse>.Failure(jobResult.Errors);
        }

        var job = jobResult.Value;
        var response = new PrintJobResponse
        {
            Id = job.Id,
            StlFilePath = job.StlFilePath,
            GCodeFilePath = job.GCodeFilePath,
            RequiredMaterial = new MaterialInfo
            {
                Id = job.RequiredMaterial.Id,
                Color = job.RequiredMaterial.Color,
                MaterialType = job.RequiredMaterial.MaterialType
            },
            EstimatedMaterialInGrams = job.EstimatedMaterialInGrams,
            ActualMaterialInGrams = job.ActualMaterialInGrams,
            EstimatedPrintTimeMinutes = job.EstimatedPrintTimeMinutes,
            Printer = new PrinterInfo
            {
                Id = job.Printer.Id,
                Name = job.Printer.Name,
                Status = job.Printer.Status
            },
            Status = job.Status,
            ErrorMessage = job.ErrorMessage,
            CreatedOn = job.CreatedOn,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt
        };

        return Result<PrintJobResponse>.Success(response);
    }

    public async Task<Result<IEnumerable<AvailableMaterialResponse>>> GetAvailableMaterialsAsync()
    {
        var materialsResult = await _materialRepository.GetListByConditionAsync(
            includes: [q => q.Include(m => m.Printers).ThenInclude(pm => pm.Printer)]);

        if (!materialsResult.IsSuccess)
        {
            return Result<IEnumerable<AvailableMaterialResponse>>.Failure(materialsResult.Errors);
        }

        var response = materialsResult.Value.Select(material => new AvailableMaterialResponse
        {
            Id = material.Id,
            Color = material.Color,
            ColorHex = $"#{material.ColorHex.R:X2}{material.ColorHex.G:X2}{material.ColorHex.B:X2}",
            MaterialType = material.MaterialType,
            DensityInGramsPerCm3 = material.DensityInGramsPerCm3,
            DiameterMm = material.DiameterMm,
            AvailableOnPrinters = material.Printers?.Select(pm => new PrinterMaterialInfo
            {
                PrinterId = pm.PrinterId,
                PrinterName = pm.Printer?.Name ?? "Unknown",
                QuantityInG = pm.QuantityInG
            }).ToList() ?? new List<PrinterMaterialInfo>()
        });

        return Result<IEnumerable<AvailableMaterialResponse>>.Success(response);
    }

    private async Task<Result<GCodeEstimation>> CalculateGCodeEstimationsAsync(string gcodeFilePath, Material material)
    {
        try
        {
            if (!File.Exists(gcodeFilePath))
            {
                return Result<GCodeEstimation>.Failure(
                    Error.NotFound("gcode.NOT_FOUND", $"G-code file not found: {gcodeFilePath}"));
            }

            double totalExtrusionMm = 0;
            double currentE = 0;
            double totalMoveTime = 0;
            double currentFeedrate = 1500; 
            double lastX = 0, lastY = 0, lastZ = 0;

            var lines = await File.ReadAllLinesAsync(gcodeFilePath);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Skip comments and empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";"))
                    continue;

                // Remove inline comments
                var commentIndex = trimmedLine.IndexOf(';');
                if (commentIndex >= 0)
                    trimmedLine = trimmedLine.Substring(0, commentIndex).Trim();

                // Parse G-code commands
                var parts = trimmedLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                var command = parts[0].ToUpper();

                // G1 - Linear move (extrusion or travel)
                if (command == "G1" || command == "G0")
                {
                    double? x = null, y = null, z = null, e = null, f = null;

                    foreach (var part in parts.Skip(1))
                    {
                        if (part.Length < 2)
                            continue;

                        var axis = part[0];
                        if (double.TryParse(part.Substring(1), System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var value))
                        {
                            switch (axis)
                            {
                                case 'X': x = value; break;
                                case 'Y': y = value; break;
                                case 'Z': z = value; break;
                                case 'E': e = value; break;
                                case 'F': f = value; break;
                            }
                        }
                    }

                    // Update feedrate if specified
                    if (f.HasValue)
                        currentFeedrate = f.Value;

                    // Calculate extrusion
                    if (e.HasValue)
                    {
                        var extrusionDelta = e.Value - currentE;
                        if (extrusionDelta > 0) // Only count positive extrusion (not retractions)
                        {
                            totalExtrusionMm += extrusionDelta;
                        }

                        currentE = e.Value;
                    }

                    // Calculate move distance and time
                    var newX = x ?? lastX;
                    var newY = y ?? lastY;
                    var newZ = z ?? lastZ;

                    var distance = Math.Sqrt(
                        Math.Pow(newX - lastX, 2) +
                        Math.Pow(newY - lastY, 2) +
                        Math.Pow(newZ - lastZ, 2));

                    if (distance > 0 && currentFeedrate > 0)
                    {
                        totalMoveTime += (distance / currentFeedrate); // time in minutes
                    }

                    lastX = newX;
                    lastY = newY;
                    lastZ = newZ;
                }
                // G92 - Set position (resets E axis)
                else if (command == "G92")
                {
                    foreach (var part in parts.Skip(1))
                    {
                        if (part.Length >= 2 && part[0] == 'E')
                        {
                            if (double.TryParse(part.Substring(1), System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, out var value))
                            {
                                currentE = value;
                            }
                        }
                    }
                }
            }

            // Calculate material weight from extrusion length
            // Volume = PI * (diameter/2)^2 * length
            var filamentRadiusMm = material.DiameterMm / 2.0;
            var volumeCm3 = Math.PI * Math.Pow(filamentRadiusMm / 10.0, 2) * (totalExtrusionMm / 10.0);
            var materialGrams = volumeCm3 * material.DensityInGramsPerCm3;

            _logger.LogInformation(
                $"G-code analysis: TotalExtrusion={totalExtrusionMm:F2}mm, " +
                $"Volume={volumeCm3:F2}cm³, Material={materialGrams:F2}g, " +
                $"EstimatedTime={totalMoveTime:F1}min");

            return Result<GCodeEstimation>.Success(new GCodeEstimation
            {
                MaterialInGrams = materialGrams,
                EstimatedTimeMinutes = totalMoveTime,
                TotalExtrusionMm = totalExtrusionMm
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating G-code estimations from {gcodeFilePath}");
            return Result<GCodeEstimation>.Failure(
                Error.InternalServerError("gcode.ANALYSIS_FAILED",
                    $"Failed to analyze G-code: {ex.Message}"));
        }
    }

    private class GCodeEstimation
    {
        public double MaterialInGrams { get; set; }
        public double EstimatedTimeMinutes { get; set; }
        public double TotalExtrusionMm { get; set; }
    }
}