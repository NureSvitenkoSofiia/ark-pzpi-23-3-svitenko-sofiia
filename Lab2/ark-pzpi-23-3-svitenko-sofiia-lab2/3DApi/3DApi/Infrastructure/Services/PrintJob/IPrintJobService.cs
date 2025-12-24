using _3DApi.Models.DTOs;

namespace _3DApi.Infrastructure.Services.PrintJob;

public interface IPrintJobService
{
    Task<Result<PrintJobResponse>> CreatePrintJobAsync(int userId, CreatePrintJobRequest request, IFormFile? stlFile);
    
    Task<Result<IEnumerable<AvailableMaterialResponse>>> GetAvailableMaterialsAsync();
}

