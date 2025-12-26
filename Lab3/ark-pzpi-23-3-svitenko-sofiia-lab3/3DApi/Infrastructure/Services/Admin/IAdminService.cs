namespace _3DApi.Infrastructure.Services.Admin;

using Models;
using Models.DTOs;

/// <summary>
/// Service for administrative operations and business logic management
/// </summary>
public interface IAdminService
{
    // User Management
    Task<Result<IEnumerable<UserManagementResponse>>> GetAllUsersAsync();
    Task<Result<UserManagementResponse>> GetUserByIdAsync(int userId);
    Task<Result<UserManagementResponse>> UpdateUserRoleAsync(int userId, string role);
    Task<Result> DeleteUserAsync(int userId);

    // Printer Management
    Task<Result<IEnumerable<PrinterManagementResponse>>> GetAllPrintersAsync();
    Task<Result<PrinterManagementResponse>> CreatePrinterAsync(CreatePrinterRequest request);
    Task<Result<PrinterManagementResponse>> UpdatePrinterAsync(int printerId, UpdatePrinterRequest request);
    Task<Result> DeletePrinterAsync(int printerId);

    // Material Management
    Task<Result<IEnumerable<MaterialManagementResponse>>> GetAllMaterialsAsync();
    Task<Result<MaterialManagementResponse>> CreateMaterialAsync(CreateMaterialRequest request);
    Task<Result<MaterialManagementResponse>> UpdateMaterialAsync(int materialId, UpdateMaterialRequest request);
    Task<Result> DeleteMaterialAsync(int materialId);

    // Print Job Management
    Task<Result<IEnumerable<PrintJobManagementResponse>>> GetAllPrintJobsAsync(string? status = null);
    Task<Result<PrintJobManagementResponse>> GetPrintJobByIdAsync(int jobId);
    Task<Result> CancelPrintJobAsync(int jobId, string reason);
    Task<Result> ReassignPrintJobAsync(int jobId, int newPrinterId);

    // Printer-Material Assignment
    Task<Result> AssignMaterialToPrinterAsync(int printerId, int materialId, double quantityInGrams);
    Task<Result> UpdatePrinterMaterialQuantityAsync(int printerId, int materialId, double quantityInGrams);
    Task<Result> RemoveMaterialFromPrinterAsync(int printerId, int materialId);
}