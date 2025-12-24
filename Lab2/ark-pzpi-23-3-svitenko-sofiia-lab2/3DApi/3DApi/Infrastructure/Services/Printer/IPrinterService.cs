using _3DApi.Models.DTOs;

namespace _3DApi.Infrastructure.Services.Printer;

public interface IPrinterService
{
    Task<Result> PingAsync(int printerId, PingRequest request);
    
    Task<Result<IEnumerable<JobQueueItemResponse>>> PollJobQueueAsync(int printerId);
    
    Task<Result> StartJobAsync(int printerId, int jobId);
    
    Task<Result> FinishJobAsync(int printerId, int jobId, FinishJobRequest request);
}

