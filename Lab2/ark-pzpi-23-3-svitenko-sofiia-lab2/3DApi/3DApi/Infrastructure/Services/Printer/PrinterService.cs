
namespace _3DApi.Infrastructure.Services.Printer;

using Microsoft.EntityFrameworkCore;
using DataAccess.Repo;
using Errors;
using Email;
using Models;
using Models.DTOs;

public class PrinterService : IPrinterService
{
    private readonly IGenericRepository<Printer> _printerRepository;
    private readonly IGenericRepository<PrintJob> _printJobRepository;
    private readonly IGenericRepository<PrinterMaterial> _printerMaterialRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<PrinterService> _logger;

    public PrinterService(
        IGenericRepository<Printer> printerRepository,
        IGenericRepository<PrintJob> printJobRepository,
        IGenericRepository<PrinterMaterial> printerMaterialRepository,
        IEmailService emailService,
        ILogger<PrinterService> logger)
    {
        _printerRepository = printerRepository;
        _printJobRepository = printJobRepository;
        _printerMaterialRepository = printerMaterialRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result> PingAsync(int printerId, PingRequest request)
    {
        var printerResult = await _printerRepository.GetSingleByConditionAsync(
            p => p.Id == printerId);

        if (!printerResult.IsSuccess)
        {
            return Result.Failure(
                Error.NotFound("printer.NOT_FOUND", "Printer not found"));
        }

        var printer = printerResult.Value;
        printer.Status = request.Status;
        printer.LastPing = DateTimeOffset.UtcNow;

        var updateResult = await _printerRepository.UpdateAsync(printer);
        return updateResult;
    }

    public async Task<Result<IEnumerable<JobQueueItemResponse>>> PollJobQueueAsync(int printerId)
    {
        // Validate printer exists
        var printerResult = await _printerRepository.GetSingleByConditionAsync(
            p => p.Id == printerId);

        if (!printerResult.IsSuccess)
        {
            return Result<IEnumerable<JobQueueItemResponse>>.Failure(
                Error.NotFound("printer.NOT_FOUND", "Printer not found"));
        }

        // Get pending jobs for this printer with material information
        var jobsResult = await _printJobRepository.GetListByConditionAsync(
            condition: pj => pj.PrinterId == printerId && pj.Status == "pending",
            orderBy: new OrderByOptions<PrintJob>
            {
                Expression = pj => pj.CreatedOn,
                IsDescending = false
            },
            includes: new[]
            {
                new Func<IQueryable<PrintJob>, IQueryable<PrintJob>>(q => q.Include(pj => pj.RequiredMaterial))
            });

        if (!jobsResult.IsSuccess)
        {
            return Result<IEnumerable<JobQueueItemResponse>>.Failure(jobsResult.Errors);
        }

        var response = jobsResult.Value.Select(job => new JobQueueItemResponse
        {
            Id = job.Id,
            StlFilePath = job.StlFilePath,
            GCodeFilePath = job.GCodeFilePath,
            RequiredMaterial = new MaterialResponse
            {
                Id = job.RequiredMaterial.Id,
                Color = job.RequiredMaterial.Color,
                ColorHex = $"#{job.RequiredMaterial.ColorHex.R:X2}{job.RequiredMaterial.ColorHex.G:X2}{job.RequiredMaterial.ColorHex.B:X2}",
                MaterialType = job.RequiredMaterial.MaterialType,
                DensityInGramsPerCm3 = job.RequiredMaterial.DensityInGramsPerCm3,
                DiameterMm = job.RequiredMaterial.DiameterMm
            },
            EstimatedMaterialInGrams = job.EstimatedMaterialInGrams,
            Status = job.Status,
            CreatedOn = job.CreatedOn,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt
        });

        return Result<IEnumerable<JobQueueItemResponse>>.Success(response);
    }

    public async Task<Result> StartJobAsync(int printerId, int jobId)
    {
        // Validate printer exists
        var printerResult = await _printerRepository.GetSingleByConditionAsync(
            p => p.Id == printerId);

        if (!printerResult.IsSuccess)
        {
            return Result.Failure(
                Error.NotFound("printer.NOT_FOUND", "Printer not found"));
        }

        // Get the job
        var jobResult = await _printJobRepository.GetSingleByConditionAsync(
            pj => pj.Id == jobId && pj.PrinterId == printerId);

        if (!jobResult.IsSuccess)
        {
            return Result.Failure(
                Error.NotFound("job.NOT_FOUND", "Print job not found"));
        }

        var job = jobResult.Value;

        // Validate job is in pending status
        if (job.Status != "pending")
        {
            return Result.Failure(
                Error.Validation("job.INVALID_STATUS", $"Job must be in 'pending' status to start. Current status: {job.Status}"));
        }

        // Update job status to printing and set start time
        job.Status = "printing";
        job.StartedAt = DateTimeOffset.UtcNow;

        var updateResult = await _printJobRepository.UpdateAsync(job);
        return updateResult;
    }

    public async Task<Result> FinishJobAsync(int printerId, int jobId, FinishJobRequest request)
    {
        // Validate printer exists
        var printerResult = await _printerRepository.GetSingleByConditionAsync(
            p => p.Id == printerId);

        if (!printerResult.IsSuccess)
        {
            return Result.Failure(
                Error.NotFound("printer.NOT_FOUND", "Printer not found"));
        }

        // Get the job with requester information
        var jobResult = await _printJobRepository.GetSingleByConditionAsync(
            pj => pj.Id == jobId && pj.PrinterId == printerId,
            includes: new[]
            {
                new Func<IQueryable<PrintJob>, IQueryable<PrintJob>>(q => q.Include(pj => pj.Requester))
            });

        if (!jobResult.IsSuccess)
        {
            return Result.Failure(
                Error.NotFound("job.NOT_FOUND", "Print job not found"));
        }

        var job = jobResult.Value;

        // Validate job is in printing status
        if (job.Status != "printing")
        {
            return Result.Failure(
                Error.Validation("job.INVALID_STATUS", $"Job must be in 'printing' status to finish. Current status: {job.Status}"));
        }

        // Update job status and completion time
        job.Status = request.IsSuccess ? "completed" : "failed";
        job.CompletedAt = DateTimeOffset.UtcNow;
        job.ErrorMessage = request.ErrorMessage;

        if (request.ActualMaterialInGrams.HasValue)
        {
            job.ActualMaterialInGrams = request.ActualMaterialInGrams.Value;
        }

        var updateResult = await _printJobRepository.UpdateAsync(job);
        if (!updateResult.IsSuccess)
        {
            return updateResult;
        }

        // If job is completed successfully, update material quantity and send success email
        if (request.IsSuccess)
        {
            // Update printer material quantity
            if (request.ActualMaterialInGrams.HasValue)
            {
                var printerMaterialResult = await _printerMaterialRepository.GetSingleByConditionAsync(
                    pm => pm.PrinterId == printerId && pm.MaterialId == job.RequiredMaterialId);

                if (printerMaterialResult.IsSuccess)
                {
                    var printerMaterial = printerMaterialResult.Value;
                    printerMaterial.QuantityInG -= request.ActualMaterialInGrams.Value;
                    if (printerMaterial.QuantityInG < 0)
                    {
                        printerMaterial.QuantityInG = 0;
                    }
                    await _printerMaterialRepository.UpdateAsync(printerMaterial);
                }
            }

            // Send completion email
            if (job.Requester != null)
            {
                try
                {
                    await _emailService.SendSuccessfulEmailAsync(
                        job.Requester.Email,
                        $"Your print job #{job.Id} has been completed successfully!",
                        "Print Job Completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send completion email for job {job.Id}");
                }
            }
        }
        else
        {
            // Send error email for failed job
            if (job.Requester != null)
            {
                try
                {
                    await _emailService.SendErrorEmailAsync(
                        job.Requester.Email,
                        request.ErrorMessage ?? "Print job failed",
                        $"Print Job #{job.Id} Failed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send error email for job {job.Id}");
                }
            }
        }

        return Result.Success();
    }
}

