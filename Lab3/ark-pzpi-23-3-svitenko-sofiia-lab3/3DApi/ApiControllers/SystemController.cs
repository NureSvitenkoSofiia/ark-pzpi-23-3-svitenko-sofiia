using _3DApi.Infrastructure.ApiResult;
using _3DApi.Infrastructure.Services.DataManagement;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace _3DApi.ApiControllers;

/// <summary>
/// System administration endpoints for backup, export, import, and monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    private readonly IDataExportService _exportService;
    private readonly ILogger<SystemController> _logger;
    private readonly IConfiguration _configuration;

    public SystemController(
        IDataExportService exportService,
        ILogger<SystemController> logger,
        IConfiguration configuration)
    {
        _exportService = exportService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Export all system data to JSON file
    /// </summary>
    [HttpPost("export/all")]
    public async Task<IActionResult> ExportAllData()
    {
        try
        {
            var tempFileName = $"export_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json";
            var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);

            var result = await _exportService.ExportAllDataToJsonAsync(tempPath);
            
            if (!result.IsSuccess)
            {
                return result.Match(StatusCodes.Status500InternalServerError);
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(tempPath);
            var fileName = $"3dapi_export_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json";

            // Clean up temp file
            try
            {
                System.IO.File.Delete(tempPath);
            }
            catch
            {
                // ignored
            }

            return File(fileBytes, "application/json", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data");
            return StatusCode(500, new { error = "Failed to export data", message = ex.Message });
        }
    }

    /// <summary>
    /// Get system health status
    /// </summary>
    [HttpGet("health")]
    public IActionResult GetSystemHealth()
    {
        var health = new
        {
            Status = "healthy",
            Timestamp = DateTimeOffset.UtcNow,
            Version = "1.0",
            Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
            Environment = new
            {
                Environment.MachineName,
                OSVersion = Environment.OSVersion.ToString(),
                Environment.ProcessorCount,
                WorkingSetMemoryMB = Environment.WorkingSet / 1024.0 / 1024.0,
                DotNetVersion = Environment.Version.ToString()
            },
            Configuration = new
            {
                FileStorageBasePath = _configuration["FileStorage:BasePath"],
                HasEmailConfig = !string.IsNullOrEmpty(_configuration["Email:SmtpServer"]),
                HasBackupDirectory = !string.IsNullOrEmpty(_configuration["Backup:Directory"])
            }
        };

        return Ok(health);
    }

    /// <summary>
    /// Get system information and diagnostics
    /// </summary>
    [HttpGet("info")]
    public IActionResult GetSystemInfo()
    {
        var info = new
        {
            Application = new
            {
                Name = "3D Print Management API",
                Version = "1.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            },
            Server = new
            {
                Environment.MachineName,
                OSVersion = Environment.OSVersion.ToString(),
                Platform = Environment.OSVersion.Platform.ToString(),
                Environment.ProcessorCount,
                Is64Bit = Environment.Is64BitOperatingSystem,
                Environment.CurrentDirectory
            },
            Runtime = new
            {
                DotNetVersion = Environment.Version.ToString(),
                RuntimeIdentifier = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier,
                ProcessArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()
            },
            Memory = new
            {
                WorkingSetMB = Math.Round(Environment.WorkingSet / 1024.0 / 1024.0, 2),
                GCTotalMemoryMB = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 2),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            },
            Timestamp = DateTimeOffset.UtcNow,
            UptimeSeconds = Environment.TickCount64 / 1000.0
        };

        return Ok(info);
    }

    /// <summary>
    /// Get storage information
    /// </summary>
    [HttpGet("storage")]
    public IActionResult GetStorageInfo()
    {
        try
        {
            var basePath = _configuration["FileStorage:BasePath"] ?? @"C:\3d";
            var driveInfo = new DriveInfo(Path.GetPathRoot(basePath) ?? "C:\\");

            var storage = new
            {
                FileStorage = new
                {
                    BasePath = basePath,
                    Exists = Directory.Exists(basePath),
                    StlFolder = _configuration["FileStorage:StlFolder"],
                    GCodeFolder = _configuration["FileStorage:GCodeFolder"]
                },
                Drive = new
                {
                    Name = driveInfo.Name,
                    DriveType = driveInfo.DriveType.ToString(),
                    FileSystem = driveInfo.DriveFormat,
                    TotalSizeGB = Math.Round(driveInfo.TotalSize / 1024.0 / 1024.0 / 1024.0, 2),
                    AvailableFreeSpaceGB = Math.Round(driveInfo.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0, 2),
                    UsedSpaceGB = Math.Round((driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / 1024.0 / 1024.0 / 1024.0, 2),
                    PercentUsed = Math.Round(((driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / (double)driveInfo.TotalSize) * 100, 2)
                },
                Timestamp = DateTimeOffset.UtcNow
            };

            return Ok(storage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage info");
            return StatusCode(500, new { error = "Failed to get storage information", message = ex.Message });
        }
    }

    /// <summary>
    /// Clear system logs (diagnostic operation)
    /// </summary>
    [HttpPost("logs/clear")]
    public IActionResult ClearLogs()
    {
        try
        {
            var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            var clearedFiles = new List<string>();
            var errors = new List<string>();

            // Clear log files if logs directory exists
            if (Directory.Exists(logDirectory))
            {
                var logFiles = Directory.GetFiles(logDirectory, "*.log", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(logDirectory, "*.txt", SearchOption.AllDirectories))
                    .Where(f => Path.GetFileName(f).Contains("log", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var logFile in logFiles)
                {
                    try
                    {
                        System.IO.File.Delete(logFile);
                        clearedFiles.Add(Path.GetFileName(logFile));
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to delete {Path.GetFileName(logFile)}: {ex.Message}");
                        _logger.LogWarning(ex, "Failed to delete log file: {LogFile}", logFile);
                    }
                }
            }

            // Also check common log locations
            var commonLogPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "3DApi", "logs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "3DApi", "logs"),
                Path.Combine(_configuration["FileStorage:BasePath"] ?? "C:\\3d", "logs")
            };

            foreach (var logPath in commonLogPaths)
            {
                if (Directory.Exists(logPath))
                {
                    var logFiles = Directory.GetFiles(logPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => f.EndsWith(".log", StringComparison.OrdinalIgnoreCase) || 
                                   f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    foreach (var logFile in logFiles)
                    {
                        try
                        {
                            System.IO.File.Delete(logFile);
                            clearedFiles.Add(Path.GetFileName(logFile));
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Failed to delete {Path.GetFileName(logFile)}: {ex.Message}");
                            _logger.LogWarning(ex, "Failed to delete log file: {LogFile}", logFile);
                        }
                    }
                }
            }

            _logger.LogInformation("Log clearing completed. Cleared {Count} files", clearedFiles.Count);

            return Ok(new
            {
                success = true,
                message = "Logs cleared successfully",
                clearedFilesCount = clearedFiles.Count,
                clearedFiles = clearedFiles,
                errors = errors,
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing logs");
            return StatusCode(500, new { error = "Failed to clear logs", message = ex.Message });
        }
    }

}