namespace _3DApi.Infrastructure.Services.Admin;

using DataAccess.Repo;
using Errors;
using Models;
using Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

public class AdminService : IAdminService
{
    private readonly IGenericRepository<User> _userRepository;
    private readonly IGenericRepository<Printer> _printerRepository;
    private readonly IGenericRepository<Material> _materialRepository;
    private readonly IGenericRepository<PrintJob> _printJobRepository;
    private readonly IGenericRepository<PrinterMaterial> _printerMaterialRepository;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IGenericRepository<User> userRepository,
        IGenericRepository<Printer> printerRepository,
        IGenericRepository<Material> materialRepository,
        IGenericRepository<PrintJob> printJobRepository,
        IGenericRepository<PrinterMaterial> printerMaterialRepository,
        ILogger<AdminService> logger)
    {
        _userRepository = userRepository;
        _printerRepository = printerRepository;
        _materialRepository = materialRepository;
        _printJobRepository = printJobRepository;
        _printerMaterialRepository = printerMaterialRepository;
        _logger = logger;
    }

    #region User Management

    public async Task<Result<IEnumerable<UserManagementResponse>>> GetAllUsersAsync()
    {
        try
        {
            var usersResult = await _userRepository.GetListByConditionAsync();
            if (!usersResult.IsSuccess)
            {
                return Result<IEnumerable<UserManagementResponse>>.Failure(usersResult.Errors);
            }

            var users = usersResult.Value.ToList();
            var responses = new List<UserManagementResponse>();

            foreach (var user in users)
            {
                var jobsResult = await _printJobRepository.GetListByConditionAsync(j => j.RequesterId == user.Id);
                var jobs = jobsResult.IsSuccess ? jobsResult.Value.ToList() : new List<PrintJob>();

                responses.Add(new UserManagementResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = user.Role,
                    CreatedOn = user.CreatedOn,
                    TotalJobsSubmitted = jobs.Count,
                    CompletedJobs = jobs.Count(j => j.Status == "completed"),
                    LastJobDate = jobs.Any() ? jobs.Max(j => j.CreatedOn) : null
                });
            }

            return Result<IEnumerable<UserManagementResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return Result<IEnumerable<UserManagementResponse>>.Failure(
                Error.InternalServerError("admin.USER_LIST_FAILED", $"Failed to get users: {ex.Message}"));
        }
    }

    public async Task<Result<UserManagementResponse>> GetUserByIdAsync(int userId)
    {
        try
        {
            var userResult = await _userRepository.GetSingleByConditionAsync(u => u.Id == userId);
            if (!userResult.IsSuccess)
            {
                return Result<UserManagementResponse>.Failure(
                    Error.NotFound("user.NOT_FOUND", "User not found"));
            }

            var user = userResult.Value;
            var jobsResult = await _printJobRepository.GetListByConditionAsync(j => j.RequesterId == userId);
            var jobs = jobsResult.IsSuccess ? jobsResult.Value.ToList() : new List<PrintJob>();

            var response = new UserManagementResponse
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                CreatedOn = user.CreatedOn,
                TotalJobsSubmitted = jobs.Count,
                CompletedJobs = jobs.Count(j => j.Status == "completed"),
                LastJobDate = jobs.Any() ? jobs.Max(j => j.CreatedOn) : null
            };

            return Result<UserManagementResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user {userId}");
            return Result<UserManagementResponse>.Failure(
                Error.InternalServerError("admin.USER_GET_FAILED", $"Failed to get user: {ex.Message}"));
        }
    }

    public async Task<Result<UserManagementResponse>> UpdateUserRoleAsync(int userId, string role)
    {
        try
        {
            if (role != "admin" && role != "user")
            {
                return Result<UserManagementResponse>.Failure(
                    Error.Validation("user.INVALID_ROLE", "Role must be 'admin' or 'user'"));
            }

            var userResult = await _userRepository.GetSingleByConditionAsync(u => u.Id == userId);
            if (!userResult.IsSuccess)
            {
                return Result<UserManagementResponse>.Failure(
                    Error.NotFound("user.NOT_FOUND", "User not found"));
            }

            var user = userResult.Value;
            user.Role = role;

            var updateResult = await _userRepository.UpdateAsync(user);
            if (!updateResult.IsSuccess)
            {
                return Result<UserManagementResponse>.Failure(updateResult.Errors);
            }

            _logger.LogInformation($"User {userId} role updated to {role}");

            return await GetUserByIdAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating user {userId} role");
            return Result<UserManagementResponse>.Failure(
                Error.InternalServerError("admin.USER_UPDATE_FAILED", $"Failed to update user role: {ex.Message}"));
        }
    }

    public async Task<Result> DeleteUserAsync(int userId)
    {
        try
        {
            var userResult = await _userRepository.GetSingleByConditionAsync(u => u.Id == userId);
            if (!userResult.IsSuccess)
            {
                return Result.Failure(Error.NotFound("user.NOT_FOUND", "User not found"));
            }

            // Check for active jobs
            var activeJobsResult = await _printJobRepository.GetListByConditionAsync(
                j => j.RequesterId == userId && (j.Status == "pending" || j.Status == "printing"));

            if (activeJobsResult.IsSuccess && activeJobsResult.Value.Any())
            {
                return Result.Failure(
                    Error.Conflict("user.HAS_ACTIVE_JOBS", "Cannot delete user with active print jobs"));
            }

            var user = userResult.Value;
            var deleteResult = await _userRepository.DeleteAsync(u => u.Id == user.Id);
            if (!deleteResult.IsSuccess)
            {
                return Result.Failure(deleteResult.Errors);
            }

            _logger.LogInformation($"User {userId} deleted");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting user {userId}");
            return Result.Failure(
                Error.InternalServerError("admin.USER_DELETE_FAILED", $"Failed to delete user: {ex.Message}"));
        }
    }

    #endregion

    #region Printer Management

    public async Task<Result<IEnumerable<PrinterManagementResponse>>> GetAllPrintersAsync()
    {
        try
        {
            var printersResult = await _printerRepository.GetListByConditionAsync(
                includes: [q => q.Include(p => p.Materials).ThenInclude(pm => pm.Material)]);

            if (!printersResult.IsSuccess)
            {
                return Result<IEnumerable<PrinterManagementResponse>>.Failure(printersResult.Errors);
            }

            var printers = printersResult.Value.ToList();
            var responses = new List<PrinterManagementResponse>();

            foreach (var printer in printers)
            {
                var jobsResult = await _printJobRepository.GetListByConditionAsync(j => j.PrinterId == printer.Id);
                var jobs = jobsResult.IsSuccess ? jobsResult.Value.ToList() : new List<PrintJob>();

                responses.Add(new PrinterManagementResponse
                {
                    Id = printer.Id,
                    Name = printer.Name,
                    Status = printer.Status,
                    IpAddress = printer.Ip.ToString(),
                    LastPing = printer.LastPing,
                    CreatedOn = printer.CreatedOn,
                    LoadedMaterials = printer.Materials?.Select(pm => new PrinterMaterialDetails
                    {
                        MaterialId = pm.MaterialId,
                        MaterialType = pm.Material?.MaterialType ?? "Unknown",
                        Color = pm.Material?.Color ?? "Unknown",
                        QuantityInG = pm.QuantityInG
                    }).ToList() ?? new List<PrinterMaterialDetails>(),
                    PendingJobsCount = jobs.Count(j => j.Status == "pending"),
                    CompletedJobsCount = jobs.Count(j => j.Status == "completed")
                });
            }

            return Result<IEnumerable<PrinterManagementResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all printers");
            return Result<IEnumerable<PrinterManagementResponse>>.Failure(
                Error.InternalServerError("admin.PRINTER_LIST_FAILED", $"Failed to get printers: {ex.Message}"));
        }
    }

    public async Task<Result<PrinterManagementResponse>> CreatePrinterAsync(CreatePrinterRequest request)
    {
        try
        {
            if (!System.Net.IPAddress.TryParse(request.IpAddress, out var ipAddress))
            {
                return Result<PrinterManagementResponse>.Failure(
                    Error.Validation("printer.INVALID_IP", "Invalid IP address format"));
            }

            var printer = new Printer
            {
                Name = request.Name,
                Ip = ipAddress,
                Status = "offline",
                LastPing = DateTimeOffset.UtcNow
            };

            var addResult = await _printerRepository.AddAsync(printer);
            if (!addResult.IsSuccess)
            {
                return Result<PrinterManagementResponse>.Failure(addResult.Errors);
            }

            _logger.LogInformation($"Printer {printer.Name} created with ID {printer.Id}");

            var response = new PrinterManagementResponse
            {
                Id = printer.Id,
                Name = printer.Name,
                Status = printer.Status,
                IpAddress = printer.Ip.ToString(),
                LastPing = printer.LastPing,
                CreatedOn = printer.CreatedOn,
                LoadedMaterials = new List<PrinterMaterialDetails>(),
                PendingJobsCount = 0,
                CompletedJobsCount = 0
            };

            return Result<PrinterManagementResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating printer");
            return Result<PrinterManagementResponse>.Failure(
                Error.InternalServerError("admin.PRINTER_CREATE_FAILED", $"Failed to create printer: {ex.Message}"));
        }
    }

    public async Task<Result<PrinterManagementResponse>> UpdatePrinterAsync(int printerId, UpdatePrinterRequest request)
    {
        try
        {
            var printerResult = await _printerRepository.GetSingleByConditionAsync(p => p.Id == printerId);
            if (!printerResult.IsSuccess)
            {
                return Result<PrinterManagementResponse>.Failure(
                    Error.NotFound("printer.NOT_FOUND", "Printer not found"));
            }

            var printer = printerResult.Value;

            if (!string.IsNullOrEmpty(request.Name))
                printer.Name = request.Name;

            if (!string.IsNullOrEmpty(request.IpAddress))
            {
                if (!System.Net.IPAddress.TryParse(request.IpAddress, out var ipAddress))
                {
                    return Result<PrinterManagementResponse>.Failure(
                        Error.Validation("printer.INVALID_IP", "Invalid IP address format"));
                }
                printer.Ip = ipAddress;
            }

            if (!string.IsNullOrEmpty(request.Status))
                printer.Status = request.Status;

            var updateResult = await _printerRepository.UpdateAsync(printer);
            if (!updateResult.IsSuccess)
            {
                return Result<PrinterManagementResponse>.Failure(updateResult.Errors);
            }

            _logger.LogInformation($"Printer {printerId} updated");

            // Reload with materials
            var updatedResult = await _printerRepository.GetSingleByConditionAsync(
                p => p.Id == printerId,
                includes: [q => q.Include(p => p.Materials).ThenInclude(pm => pm.Material)]);

            var updated = updatedResult.Value;
            var jobsResult = await _printJobRepository.GetListByConditionAsync(j => j.PrinterId == printerId);
            var jobs = jobsResult.IsSuccess ? jobsResult.Value.ToList() : new List<PrintJob>();

            var response = new PrinterManagementResponse
            {
                Id = updated.Id,
                Name = updated.Name,
                Status = updated.Status,
                IpAddress = updated.Ip.ToString(),
                LastPing = updated.LastPing,
                CreatedOn = updated.CreatedOn,
                LoadedMaterials = updated.Materials?.Select(pm => new PrinterMaterialDetails
                {
                    MaterialId = pm.MaterialId,
                    MaterialType = pm.Material?.MaterialType ?? "Unknown",
                    Color = pm.Material?.Color ?? "Unknown",
                    QuantityInG = pm.QuantityInG
                }).ToList() ?? new List<PrinterMaterialDetails>(),
                PendingJobsCount = jobs.Count(j => j.Status == "pending"),
                CompletedJobsCount = jobs.Count(j => j.Status == "completed")
            };

            return Result<PrinterManagementResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating printer {printerId}");
            return Result<PrinterManagementResponse>.Failure(
                Error.InternalServerError("admin.PRINTER_UPDATE_FAILED", $"Failed to update printer: {ex.Message}"));
        }
    }

    public async Task<Result> DeletePrinterAsync(int printerId)
    {
        try
        {
            var printerResult = await _printerRepository.GetSingleByConditionAsync(p => p.Id == printerId);
            if (!printerResult.IsSuccess)
            {
                return Result.Failure(Error.NotFound("printer.NOT_FOUND", "Printer not found"));
            }

            // Check for active jobs
            var activeJobsResult = await _printJobRepository.GetListByConditionAsync(
                j => j.PrinterId == printerId && (j.Status == "pending" || j.Status == "printing"));

            if (activeJobsResult.IsSuccess && activeJobsResult.Value.Any())
            {
                return Result.Failure(
                    Error.Conflict("printer.HAS_ACTIVE_JOBS", "Cannot delete printer with active print jobs"));
            }

            var printer = printerResult.Value;
            var deleteResult = await _printerRepository.DeleteAsync(p => p.Id == printer.Id);
            if (!deleteResult.IsSuccess)
            {
                return Result.Failure(deleteResult.Errors);
            }

            _logger.LogInformation($"Printer {printerId} deleted");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting printer {printerId}");
            return Result.Failure(
                Error.InternalServerError("admin.PRINTER_DELETE_FAILED", $"Failed to delete printer: {ex.Message}"));
        }
    }

    #endregion

    #region Material Management

    public async Task<Result<IEnumerable<MaterialManagementResponse>>> GetAllMaterialsAsync()
    {
        try
        {
            var materialsResult = await _materialRepository.GetListByConditionAsync(
                includes: [q => q.Include(m => m.Printers)]);

            if (!materialsResult.IsSuccess)
            {
                return Result<IEnumerable<MaterialManagementResponse>>.Failure(materialsResult.Errors);
            }

            var materials = materialsResult.Value.ToList();
            var responses = new List<MaterialManagementResponse>();

            foreach (var material in materials)
            {
                var jobsResult = await _printJobRepository.GetListByConditionAsync(
                    j => j.RequiredMaterialId == material.Id && j.Status == "completed");
                var totalUsage = jobsResult.IsSuccess ? jobsResult.Value.Sum(j => j.ActualMaterialInGrams) : 0;

                responses.Add(new MaterialManagementResponse
                {
                    Id = material.Id,
                    Color = material.Color,
                    ColorHex = $"#{material.ColorHex.R:X2}{material.ColorHex.G:X2}{material.ColorHex.B:X2}",
                    MaterialType = material.MaterialType,
                    DensityInGramsPerCm3 = material.DensityInGramsPerCm3,
                    DiameterMm = material.DiameterMm,
                    CreatedOn = material.CreatedOn,
                    PrintersLoadedOn = material.Printers?.Count() ?? 0,
                    TotalUsageGrams = Math.Round(totalUsage, 2)
                });
            }

            return Result<IEnumerable<MaterialManagementResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all materials");
            return Result<IEnumerable<MaterialManagementResponse>>.Failure(
                Error.InternalServerError("admin.MATERIAL_LIST_FAILED", $"Failed to get materials: {ex.Message}"));
        }
    }

    public async Task<Result<MaterialManagementResponse>> CreateMaterialAsync(CreateMaterialRequest request)
    {
        try
        {
            // Parse color hex
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.ColorHex, "^[0-9A-Fa-f]{6}$"))
            {
                return Result<MaterialManagementResponse>.Failure(
                    Error.Validation("material.INVALID_COLOR", "ColorHex must be in format RRGGBB"));
            }

            var r = Convert.ToInt32(request.ColorHex.Substring(0, 2), 16);
            var g = Convert.ToInt32(request.ColorHex.Substring(2, 2), 16);
            var b = Convert.ToInt32(request.ColorHex.Substring(4, 2), 16);

            var material = new Material
            {
                Color = request.Color,
                ColorHex = Color.FromArgb(r, g, b),
                MaterialType = request.MaterialType,
                DensityInGramsPerCm3 = request.DensityInGramsPerCm3,
                DiameterMm = request.DiameterMm
            };

            var addResult = await _materialRepository.AddAsync(material);
            if (!addResult.IsSuccess)
            {
                return Result<MaterialManagementResponse>.Failure(addResult.Errors);
            }

            _logger.LogInformation($"Material {material.Color} {material.MaterialType} created with ID {material.Id}");

            var response = new MaterialManagementResponse
            {
                Id = material.Id,
                Color = material.Color,
                ColorHex = $"#{material.ColorHex.R:X2}{material.ColorHex.G:X2}{material.ColorHex.B:X2}",
                MaterialType = material.MaterialType,
                DensityInGramsPerCm3 = material.DensityInGramsPerCm3,
                DiameterMm = material.DiameterMm,
                CreatedOn = material.CreatedOn,
                PrintersLoadedOn = 0,
                TotalUsageGrams = 0
            };

            return Result<MaterialManagementResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating material");
            return Result<MaterialManagementResponse>.Failure(
                Error.InternalServerError("admin.MATERIAL_CREATE_FAILED", $"Failed to create material: {ex.Message}"));
        }
    }

    public async Task<Result<MaterialManagementResponse>> UpdateMaterialAsync(int materialId, UpdateMaterialRequest request)
    {
        try
        {
            var materialResult = await _materialRepository.GetSingleByConditionAsync(m => m.Id == materialId);
            if (!materialResult.IsSuccess)
            {
                return Result<MaterialManagementResponse>.Failure(
                    Error.NotFound("material.NOT_FOUND", "Material not found"));
            }

            var material = materialResult.Value;

            if (!string.IsNullOrEmpty(request.Color))
                material.Color = request.Color;

            if (!string.IsNullOrEmpty(request.ColorHex))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.ColorHex, "^[0-9A-Fa-f]{6}$"))
                {
                    return Result<MaterialManagementResponse>.Failure(
                        Error.Validation("material.INVALID_COLOR", "ColorHex must be in format RRGGBB"));
                }

                var r = Convert.ToInt32(request.ColorHex.Substring(0, 2), 16);
                var g = Convert.ToInt32(request.ColorHex.Substring(2, 2), 16);
                var b = Convert.ToInt32(request.ColorHex.Substring(4, 2), 16);
                material.ColorHex = Color.FromArgb(r, g, b);
            }

            if (!string.IsNullOrEmpty(request.MaterialType))
                material.MaterialType = request.MaterialType;

            if (request.DensityInGramsPerCm3.HasValue)
                material.DensityInGramsPerCm3 = request.DensityInGramsPerCm3.Value;

            if (request.DiameterMm.HasValue)
                material.DiameterMm = request.DiameterMm.Value;

            var updateResult = await _materialRepository.UpdateAsync(material);
            if (!updateResult.IsSuccess)
            {
                return Result<MaterialManagementResponse>.Failure(updateResult.Errors);
            }

            _logger.LogInformation($"Material {materialId} updated");

            // Reload with relations
            var updatedResult = await _materialRepository.GetSingleByConditionAsync(
                m => m.Id == materialId,
                includes: [q => q.Include(m => m.Printers)]);

            var updated = updatedResult.Value;
            var jobsResult = await _printJobRepository.GetListByConditionAsync(
                j => j.RequiredMaterialId == materialId && j.Status == "completed");
            var totalUsage = jobsResult.IsSuccess ? jobsResult.Value.Sum(j => j.ActualMaterialInGrams) : 0;

            var response = new MaterialManagementResponse
            {
                Id = updated.Id,
                Color = updated.Color,
                ColorHex = $"#{updated.ColorHex.R:X2}{updated.ColorHex.G:X2}{updated.ColorHex.B:X2}",
                MaterialType = updated.MaterialType,
                DensityInGramsPerCm3 = updated.DensityInGramsPerCm3,
                DiameterMm = updated.DiameterMm,
                CreatedOn = updated.CreatedOn,
                PrintersLoadedOn = updated.Printers?.Count() ?? 0,
                TotalUsageGrams = Math.Round(totalUsage, 2)
            };

            return Result<MaterialManagementResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating material {materialId}");
            return Result<MaterialManagementResponse>.Failure(
                Error.InternalServerError("admin.MATERIAL_UPDATE_FAILED", $"Failed to update material: {ex.Message}"));
        }
    }

    public async Task<Result> DeleteMaterialAsync(int materialId)
    {
        try
        {
            var materialResult = await _materialRepository.GetSingleByConditionAsync(m => m.Id == materialId);
            if (!materialResult.IsSuccess)
            {
                return Result.Failure(Error.NotFound("material.NOT_FOUND", "Material not found"));
            }

            // Check if material is loaded on any printer
            var printersResult = await _printerMaterialRepository.GetListByConditionAsync(pm => pm.MaterialId == materialId);
            if (printersResult.IsSuccess && printersResult.Value.Any())
            {
                return Result.Failure(
                    Error.Conflict("material.IN_USE", "Cannot delete material that is loaded on printers"));
            }

            // Check for active jobs using this material
            var activeJobsResult = await _printJobRepository.GetListByConditionAsync(
                j => j.RequiredMaterialId == materialId && (j.Status == "pending" || j.Status == "printing"));

            if (activeJobsResult.IsSuccess && activeJobsResult.Value.Any())
            {
                return Result.Failure(
                    Error.Conflict("material.HAS_ACTIVE_JOBS", "Cannot delete material with active print jobs"));
            }

            var material = materialResult.Value;
            var deleteResult = await _materialRepository.DeleteAsync(m => m.Id == material.Id);
            if (!deleteResult.IsSuccess)
            {
                return Result.Failure(deleteResult.Errors);
            }

            _logger.LogInformation($"Material {materialId} deleted");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting material {materialId}");
            return Result.Failure(
                Error.InternalServerError("admin.MATERIAL_DELETE_FAILED", $"Failed to delete material: {ex.Message}"));
        }
    }

    #endregion

    #region Print Job Management

    public async Task<Result<IEnumerable<PrintJobManagementResponse>>> GetAllPrintJobsAsync(string? status = null)
    {
        try
        {
            var jobsResult = status != null
                ? await _printJobRepository.GetListByConditionAsync(
                    j => j.Status == status,
                    includes: [
                        q => q.Include(j => j.Requester),
                        q => q.Include(j => j.Printer),
                        q => q.Include(j => j.RequiredMaterial)
                    ])
                : await _printJobRepository.GetListByConditionAsync(
                    includes: [
                        q => q.Include(j => j.Requester),
                        q => q.Include(j => j.Printer),
                        q => q.Include(j => j.RequiredMaterial)
                    ]);

            if (!jobsResult.IsSuccess)
            {
                return Result<IEnumerable<PrintJobManagementResponse>>.Failure(jobsResult.Errors);
            }

            var jobs = jobsResult.Value.Select(j => new PrintJobManagementResponse
            {
                Id = j.Id,
                RequesterId = j.RequesterId,
                RequesterEmail = j.Requester?.Email ?? "Unknown",
                PrinterId = j.PrinterId,
                PrinterName = j.Printer?.Name ?? "Unknown",
                MaterialType = j.RequiredMaterial?.MaterialType ?? "Unknown",
                MaterialColor = j.RequiredMaterial?.Color ?? "Unknown",
                EstimatedMaterialInGrams = j.EstimatedMaterialInGrams,
                ActualMaterialInGrams = j.ActualMaterialInGrams,
                EstimatedPrintTimeMinutes = j.EstimatedPrintTimeMinutes,
                Status = j.Status,
                ErrorMessage = j.ErrorMessage,
                CreatedOn = j.CreatedOn,
                StartedAt = j.StartedAt,
                CompletedAt = j.CompletedAt,
                StlFilePath = j.StlFilePath,
                GCodeFilePath = j.GCodeFilePath
            }).ToList();

            return Result<IEnumerable<PrintJobManagementResponse>>.Success(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all print jobs");
            return Result<IEnumerable<PrintJobManagementResponse>>.Failure(
                Error.InternalServerError("admin.JOB_LIST_FAILED", $"Failed to get print jobs: {ex.Message}"));
        }
    }

    public async Task<Result<PrintJobManagementResponse>> GetPrintJobByIdAsync(int jobId)
    {
        try
        {
            var jobResult = await _printJobRepository.GetSingleByConditionAsync(
                j => j.Id == jobId,
                includes: [
                    q => q.Include(j => j.Requester),
                    q => q.Include(j => j.Printer),
                    q => q.Include(j => j.RequiredMaterial)
                ]);

            if (!jobResult.IsSuccess)
            {
                return Result<PrintJobManagementResponse>.Failure(
                    Error.NotFound("job.NOT_FOUND", "Print job not found"));
            }

            var j = jobResult.Value;
            var response = new PrintJobManagementResponse
            {
                Id = j.Id,
                RequesterId = j.RequesterId,
                RequesterEmail = j.Requester?.Email ?? "Unknown",
                PrinterId = j.PrinterId,
                PrinterName = j.Printer?.Name ?? "Unknown",
                MaterialType = j.RequiredMaterial?.MaterialType ?? "Unknown",
                MaterialColor = j.RequiredMaterial?.Color ?? "Unknown",
                EstimatedMaterialInGrams = j.EstimatedMaterialInGrams,
                ActualMaterialInGrams = j.ActualMaterialInGrams,
                EstimatedPrintTimeMinutes = j.EstimatedPrintTimeMinutes,
                Status = j.Status,
                ErrorMessage = j.ErrorMessage,
                CreatedOn = j.CreatedOn,
                StartedAt = j.StartedAt,
                CompletedAt = j.CompletedAt,
                StlFilePath = j.StlFilePath,
                GCodeFilePath = j.GCodeFilePath
            };

            return Result<PrintJobManagementResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting print job {jobId}");
            return Result<PrintJobManagementResponse>.Failure(
                Error.InternalServerError("admin.JOB_GET_FAILED", $"Failed to get print job: {ex.Message}"));
        }
    }

    public async Task<Result> CancelPrintJobAsync(int jobId, string reason)
    {
        try
        {
            var jobResult = await _printJobRepository.GetSingleByConditionAsync(j => j.Id == jobId);
            if (!jobResult.IsSuccess)
            {
                return Result.Failure(Error.NotFound("job.NOT_FOUND", "Print job not found"));
            }

            var job = jobResult.Value;

            if (job.Status == "completed" || job.Status == "failed")
            {
                return Result.Failure(
                    Error.Validation("job.ALREADY_FINISHED", "Cannot cancel a finished job"));
            }

            job.Status = "failed";
            job.ErrorMessage = $"Cancelled by admin: {reason}";
            job.CompletedAt = DateTimeOffset.UtcNow;

            var updateResult = await _printJobRepository.UpdateAsync(job);
            if (!updateResult.IsSuccess)
            {
                return Result.Failure(updateResult.Errors);
            }

            _logger.LogInformation($"Print job {jobId} cancelled: {reason}");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error cancelling print job {jobId}");
            return Result.Failure(
                Error.InternalServerError("admin.JOB_CANCEL_FAILED", $"Failed to cancel job: {ex.Message}"));
        }
    }

    public async Task<Result> ReassignPrintJobAsync(int jobId, int newPrinterId)
    {
        try
        {
            var jobResult = await _printJobRepository.GetSingleByConditionAsync(j => j.Id == jobId);
            if (!jobResult.IsSuccess)
            {
                return Result.Failure(Error.NotFound("job.NOT_FOUND", "Print job not found"));
            }

            var job = jobResult.Value;

            if (job.Status != "pending")
            {
                return Result.Failure(
                    Error.Validation("job.NOT_PENDING", "Can only reassign pending jobs"));
            }

            var printerResult = await _printerRepository.GetSingleByConditionAsync(p => p.Id == newPrinterId);
            if (!printerResult.IsSuccess)
            {
                return Result.Failure(Error.NotFound("printer.NOT_FOUND", "Printer not found"));
            }

            // Check if new printer has required material
            var materialResult = await _printerMaterialRepository.GetSingleByConditionAsync(
                pm => pm.PrinterId == newPrinterId && pm.MaterialId == job.RequiredMaterialId);

            if (!materialResult.IsSuccess)
            {
                return Result.Failure(
                    Error.Validation("printer.NO_MATERIAL", "New printer does not have required material"));
            }

            job.PrinterId = newPrinterId;

            var updateResult = await _printJobRepository.UpdateAsync(job);
            if (!updateResult.IsSuccess)
            {
                return Result.Failure(updateResult.Errors);
            }

            _logger.LogInformation($"Print job {jobId} reassigned to printer {newPrinterId}");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reassigning print job {jobId}");
            return Result.Failure(
                Error.InternalServerError("admin.JOB_REASSIGN_FAILED", $"Failed to reassign job: {ex.Message}"));
        }
    }

    #endregion

    #region Printer-Material Assignment

    public async Task<Result> AssignMaterialToPrinterAsync(int printerId, int materialId, double quantityInGrams)
    {
        try
        {
            var printerResult = await _printerRepository.GetSingleByConditionAsync(p => p.Id == printerId);
            if (!printerResult.IsSuccess)
            {
                return Result.Failure(Error.NotFound("printer.NOT_FOUND", "Printer not found"));
            }

            var materialResult = await _materialRepository.GetSingleByConditionAsync(m => m.Id == materialId);
            if (!materialResult.IsSuccess)
            {
                return Result.Failure(Error.NotFound("material.NOT_FOUND", "Material not found"));
            }

            // Check if already assigned
            var existingResult = await _printerMaterialRepository.GetSingleByConditionAsync(
                pm => pm.PrinterId == printerId && pm.MaterialId == materialId);

            if (existingResult.IsSuccess)
            {
                return Result.Failure(
                    Error.Conflict("printer.MATERIAL_ALREADY_LOADED", "Material already loaded on this printer"));
            }

            var printerMaterial = new PrinterMaterial
            {
                PrinterId = printerId,
                MaterialId = materialId,
                QuantityInG = quantityInGrams
            };

            var addResult = await _printerMaterialRepository.AddAsync(printerMaterial);
            if (!addResult.IsSuccess)
            {
                return Result.Failure(addResult.Errors);
            }

            _logger.LogInformation($"Material {materialId} assigned to printer {printerId} with {quantityInGrams}g");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error assigning material {materialId} to printer {printerId}");
            return Result.Failure(
                Error.InternalServerError("admin.MATERIAL_ASSIGN_FAILED", $"Failed to assign material: {ex.Message}"));
        }
    }

    public async Task<Result> UpdatePrinterMaterialQuantityAsync(int printerId, int materialId, double quantityInGrams)
    {
        try
        {
            var printerMaterialResult = await _printerMaterialRepository.GetSingleByConditionAsync(
                pm => pm.PrinterId == printerId && pm.MaterialId == materialId);

            if (!printerMaterialResult.IsSuccess)
            {
                return Result.Failure(
                    Error.NotFound("printer.MATERIAL_NOT_FOUND", "Material not found on this printer"));
            }

            var printerMaterial = printerMaterialResult.Value;
            printerMaterial.QuantityInG = quantityInGrams;

            var updateResult = await _printerMaterialRepository.UpdateAsync(printerMaterial);
            if (!updateResult.IsSuccess)
            {
                return Result.Failure(updateResult.Errors);
            }

            _logger.LogInformation($"Material {materialId} quantity updated on printer {printerId} to {quantityInGrams}g");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating material {materialId} quantity on printer {printerId}");
            return Result.Failure(
                Error.InternalServerError("admin.MATERIAL_UPDATE_FAILED", $"Failed to update material quantity: {ex.Message}"));
        }
    }

    public async Task<Result> RemoveMaterialFromPrinterAsync(int printerId, int materialId)
    {
        try
        {
            var printerMaterialResult = await _printerMaterialRepository.GetSingleByConditionAsync(
                pm => pm.PrinterId == printerId && pm.MaterialId == materialId);

            if (!printerMaterialResult.IsSuccess)
            {
                return Result.Failure(
                    Error.NotFound("printer.MATERIAL_NOT_FOUND", "Material not found on this printer"));
            }

            // Check for pending jobs using this material on this printer
            var pendingJobsResult = await _printJobRepository.GetListByConditionAsync(
                j => j.PrinterId == printerId && j.RequiredMaterialId == materialId && j.Status == "pending");

            if (pendingJobsResult.IsSuccess && pendingJobsResult.Value.Any())
            {
                return Result.Failure(
                    Error.Conflict("printer.MATERIAL_HAS_PENDING_JOBS", "Cannot remove material with pending jobs"));
            }

            var printerMaterial = printerMaterialResult.Value;
            var deleteResult = await _printerMaterialRepository.DeleteAsync(pm => pm.Id == printerMaterial.Id);
            if (!deleteResult.IsSuccess)
            {
                return Result.Failure(deleteResult.Errors);
            }

            _logger.LogInformation($"Material {materialId} removed from printer {printerId}");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error removing material {materialId} from printer {printerId}");
            return Result.Failure(
                Error.InternalServerError("admin.MATERIAL_REMOVE_FAILED", $"Failed to remove material: {ex.Message}"));
        }
    }

    #endregion
}