namespace _3DApi.Infrastructure.Services.Analytics;

using DataAccess.Repo;
using Errors;
using Models;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Implementation of analytics service using statistical methods
/// 
/// Mathematical Methods Justification:
/// 1. Statistical Aggregation: Mean, median, standard deviation for performance metrics
///    - Provides accurate central tendencies and variability measures
///    - Essential for understanding system behavior
/// 
/// 2. Success Rate Calculation: P(success) = successful_jobs / total_jobs
///    - Basic probability theory for reliability metrics
///    - Industry standard for system health monitoring
/// 
/// 3. Mean Time Between Failures (MTBF): total_operating_time / number_of_failures
///    - Reliability engineering metric
///    - Predicts maintenance needs and system reliability
/// 
/// 4. Utilization Rate: active_time / total_available_time
///    - Resource efficiency metric
///    - Identifies underutilized or overutilized resources
/// 
/// 5. Linear Time Estimation: Σ(estimated_job_time) for queue completion
///    - Simple but effective for sequential job processing
///    - Provides user expectations for wait times
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly IGenericRepository<PrintJob> _printJobRepository;
    private readonly IGenericRepository<Printer> _printerRepository;
    private readonly IGenericRepository<User> _userRepository;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        IGenericRepository<PrintJob> printJobRepository,
        IGenericRepository<Printer> printerRepository,
        IGenericRepository<User> userRepository,
        ILogger<AnalyticsService> logger)
    {
        _printJobRepository = printJobRepository;
        _printerRepository = printerRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<SystemStatistics>> GetSystemStatisticsAsync()
    {
        try
        {
            var allJobsResult = await _printJobRepository.GetListByConditionAsync();
            if (!allJobsResult.IsSuccess)
            {
                return Result<SystemStatistics>.Failure(allJobsResult.Errors);
            }

            var jobs = allJobsResult.Value.ToList();
            var completedJobs = jobs.Where(j => j.Status == "completed").ToList();
            var failedJobs = jobs.Where(j => j.Status == "failed").ToList();

            // Statistical calculations
            var totalJobs = jobs.Count;
            var successfulCount = completedJobs.Count;
            var successRate = totalJobs > 0 ? (double)successfulCount / totalJobs * 100 : 0;

            var totalMaterialUsed = completedJobs.Sum(j => j.ActualMaterialInGrams);
            var totalPrintTimeMinutes = completedJobs
                .Where(j => j.StartedAt.HasValue && j.CompletedAt.HasValue)
                .Sum(j => (j.CompletedAt!.Value - j.StartedAt!.Value).TotalMinutes);

            var avgPrintTime = completedJobs.Any() 
                ? completedJobs.Average(j => j.EstimatedPrintTimeMinutes) 
                : 0;
            
            var avgMaterialUsage = completedJobs.Any() 
                ? completedJobs.Average(j => j.ActualMaterialInGrams) 
                : 0;

            var printersResult = await _printerRepository.GetListByConditionAsync();
            var printers = printersResult.IsSuccess ? printersResult.Value.ToList() : new List<Printer>();

            var usersResult = await _userRepository.GetListByConditionAsync();
            var users = usersResult.IsSuccess ? usersResult.Value.Count() : 0;

            var statistics = new SystemStatistics
            {
                TotalPrintJobs = totalJobs,
                CompletedJobs = completedJobs.Count,
                FailedJobs = failedJobs.Count,
                PendingJobs = jobs.Count(j => j.Status == "pending"),
                ActiveJobs = jobs.Count(j => j.Status == "printing"),
                OverallSuccessRate = Math.Round(successRate, 2),
                TotalMaterialUsedGrams = Math.Round(totalMaterialUsed, 2),
                TotalPrintTimeHours = Math.Round(totalPrintTimeMinutes / 60.0, 2),
                AveragePrintTimeMinutes = Math.Round(avgPrintTime, 2),
                AverageMaterialUsageGrams = Math.Round(avgMaterialUsage, 2),
                TotalPrinters = printers.Count,
                OnlinePrinters = printers.Count(p => p.Status == "online"),
                TotalUsers = users
            };

            _logger.LogInformation($"System statistics generated: {statistics.TotalPrintJobs} total jobs, {statistics.OverallSuccessRate}% success rate");

            return Result<SystemStatistics>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating system statistics");
            return Result<SystemStatistics>.Failure(
                Error.InternalServerError("analytics.STATS_FAILED", $"Failed to generate statistics: {ex.Message}"));
        }
    }

    public async Task<Result<PrinterPerformanceMetrics>> GetPrinterPerformanceAsync(int printerId)
    {
        try
        {
            var printerResult = await _printerRepository.GetSingleByConditionAsync(p => p.Id == printerId);
            if (!printerResult.IsSuccess)
            {
                return Result<PrinterPerformanceMetrics>.Failure(
                    Error.NotFound("printer.NOT_FOUND", "Printer not found"));
            }

            var printer = printerResult.Value;

            var jobsResult = await _printJobRepository.GetListByConditionAsync(j => j.PrinterId == printerId);
            if (!jobsResult.IsSuccess)
            {
                return Result<PrinterPerformanceMetrics>.Failure(jobsResult.Errors);
            }

            var jobs = jobsResult.Value.ToList();
            var completedJobs = jobs.Where(j => j.Status == "completed").ToList();
            var failedJobs = jobs.Where(j => j.Status == "failed").ToList();

            // Calculate success rate
            var totalFinished = completedJobs.Count + failedJobs.Count;
            var successRate = totalFinished > 0 ? (double)completedJobs.Count / totalFinished * 100 : 0;

            // Calculate total operating hours
            var totalOperatingMinutes = completedJobs
                .Where(j => j.StartedAt.HasValue && j.CompletedAt.HasValue)
                .Sum(j => (j.CompletedAt!.Value - j.StartedAt!.Value).TotalMinutes);

            var totalOperatingHours = totalOperatingMinutes / 60.0;

            // Calculate average job duration
            var avgJobDuration = completedJobs.Any()
                ? completedJobs.Average(j => j.EstimatedPrintTimeMinutes)
                : 0;

            // Calculate MTBF (Mean Time Between Failures)
            var mtbf = failedJobs.Count > 0 
                ? totalOperatingHours / failedJobs.Count 
                : totalOperatingHours;

            // Calculate utilization rate (simplified - assumes 24/7 availability since first job)
            var firstJobDate = jobs.Any() ? jobs.Min(j => j.CreatedOn) : DateTimeOffset.UtcNow;
            var totalAvailableHours = (DateTimeOffset.UtcNow - firstJobDate).TotalHours;
            var utilizationRate = totalAvailableHours > 0 
                ? (totalOperatingHours / totalAvailableHours) * 100 
                : 0;

            var metrics = new PrinterPerformanceMetrics
            {
                PrinterId = printerId,
                PrinterName = printer.Name,
                TotalJobsCompleted = completedJobs.Count,
                TotalJobsFailed = failedJobs.Count,
                SuccessRate = Math.Round(successRate, 2),
                TotalOperatingHours = Math.Round(totalOperatingHours, 2),
                AverageJobDurationMinutes = Math.Round(avgJobDuration, 2),
                UtilizationRate = Math.Round(utilizationRate, 2),
                LastActiveTime = printer.LastPing,
                MeanTimeBetweenFailures = Math.Round(mtbf, 2)
            };

            _logger.LogInformation(
                $"Printer {printerId} performance: {metrics.SuccessRate}% success, " +
                $"{metrics.TotalOperatingHours}h operating time, MTBF={metrics.MeanTimeBetweenFailures}h");

            return Result<PrinterPerformanceMetrics>.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating printer performance for printer {printerId}");
            return Result<PrinterPerformanceMetrics>.Failure(
                Error.InternalServerError("analytics.PERFORMANCE_FAILED", $"Failed to calculate performance: {ex.Message}"));
        }
    }

    public async Task<Result<UserActivityMetrics>> GetUserActivityAsync(int userId)
    {
        try
        {
            var userResult = await _userRepository.GetSingleByConditionAsync(u => u.Id == userId);
            if (!userResult.IsSuccess)
            {
                return Result<UserActivityMetrics>.Failure(
                    Error.NotFound("user.NOT_FOUND", "User not found"));
            }

            var user = userResult.Value;

            var jobsResult = await _printJobRepository.GetListByConditionAsync(j => j.RequesterId == userId);
            if (!jobsResult.IsSuccess)
            {
                return Result<UserActivityMetrics>.Failure(jobsResult.Errors);
            }

            var jobs = jobsResult.Value.ToList();

            if (!jobs.Any())
            {
                return Result<UserActivityMetrics>.Success(new UserActivityMetrics
                {
                    UserId = userId,
                    UserEmail = user.Email,
                    TotalJobsSubmitted = 0,
                    CompletedJobs = 0,
                    FailedJobs = 0,
                    PendingJobs = 0,
                    TotalMaterialUsedGrams = 0,
                    TotalPrintTimeHours = 0,
                    FirstJobDate = DateTimeOffset.UtcNow,
                    LastJobDate = null
                });
            }

            var completedJobs = jobs.Where(j => j.Status == "completed").ToList();
            var totalMaterial = completedJobs.Sum(j => j.ActualMaterialInGrams);
            var totalTimeMinutes = completedJobs
                .Where(j => j.StartedAt.HasValue && j.CompletedAt.HasValue)
                .Sum(j => (j.CompletedAt!.Value - j.StartedAt!.Value).TotalMinutes);

            var metrics = new UserActivityMetrics
            {
                UserId = userId,
                UserEmail = user.Email,
                TotalJobsSubmitted = jobs.Count,
                CompletedJobs = completedJobs.Count,
                FailedJobs = jobs.Count(j => j.Status == "failed"),
                PendingJobs = jobs.Count(j => j.Status == "pending"),
                TotalMaterialUsedGrams = Math.Round(totalMaterial, 2),
                TotalPrintTimeHours = Math.Round(totalTimeMinutes / 60.0, 2),
                FirstJobDate = jobs.Min(j => j.CreatedOn),
                LastJobDate = jobs.Max(j => j.CreatedOn)
            };

            return Result<UserActivityMetrics>.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating user activity for user {userId}");
            return Result<UserActivityMetrics>.Failure(
                Error.InternalServerError("analytics.ACTIVITY_FAILED", $"Failed to calculate activity: {ex.Message}"));
        }
    }

    public async Task<Result<SuccessRateAnalysis>> AnalyzeSuccessRatesAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        try
        {
            var start = startDate?.ToUniversalTime() ?? DateTimeOffset.UtcNow.AddMonths(-1);
            var end = endDate?.ToUniversalTime() ?? DateTimeOffset.UtcNow;

            var jobsResult = await _printJobRepository.GetListByConditionAsync(
                j => j.CreatedOn >= start && j.CreatedOn <= end,
                includes: [q => q.Include(j => j.RequiredMaterial)]);

            if (!jobsResult.IsSuccess)
            {
                return Result<SuccessRateAnalysis>.Failure(jobsResult.Errors);
            }

            var jobs = jobsResult.Value.ToList();
            var finishedJobs = jobs.Where(j => j.Status == "completed" || j.Status == "failed").ToList();
            var successfulJobs = finishedJobs.Where(j => j.Status == "completed").ToList();
            var failedJobs = finishedJobs.Where(j => j.Status == "failed").ToList();

            var overallSuccessRate = finishedJobs.Any()
                ? (double)successfulJobs.Count / finishedJobs.Count * 100
                : 0;

            // Success rate by material type
            var successByMaterial = finishedJobs
                .GroupBy(j => j.RequiredMaterial.MaterialType)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count(j => j.Status == "completed") / (double)g.Count() * 100
                );

            // Success rate by printer
            var successByPrinter = finishedJobs
                .GroupBy(j => j.PrinterId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count(j => j.Status == "completed") / (double)g.Count() * 100
                );

            // Common failure reasons
            var failureReasons = failedJobs
                .Where(j => !string.IsNullOrEmpty(j.ErrorMessage))
                .GroupBy(j => j.ErrorMessage!)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => $"{g.Key} ({g.Count()} occurrences)")
                .ToList();

            var analysis = new SuccessRateAnalysis
            {
                OverallSuccessRate = Math.Round(overallSuccessRate, 2),
                TotalJobs = finishedJobs.Count,
                SuccessfulJobs = successfulJobs.Count,
                FailedJobs = failedJobs.Count,
                SuccessRateByMaterial = successByMaterial.ToDictionary(
                    kvp => kvp.Key,
                    kvp => Math.Round(kvp.Value, 2)
                ),
                SuccessRateByPrinter = successByPrinter.ToDictionary(
                    kvp => kvp.Key,
                    kvp => Math.Round(kvp.Value, 2)
                ),
                CommonFailureReasons = failureReasons,
                AnalysisPeriodStart = start,
                AnalysisPeriodEnd = end
            };

            _logger.LogInformation(
                $"Success rate analysis: {analysis.OverallSuccessRate}% " +
                $"({analysis.SuccessfulJobs}/{analysis.TotalJobs} jobs)");

            return Result<SuccessRateAnalysis>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing success rates");
            return Result<SuccessRateAnalysis>.Failure(
                Error.InternalServerError("analytics.ANALYSIS_FAILED", $"Failed to analyze success rates: {ex.Message}"));
        }
    }

    public async Task<Result<MaterialConsumptionStats>> GetMaterialConsumptionAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        try
        {
            var start = startDate?.ToUniversalTime() ?? DateTimeOffset.UtcNow.AddMonths(-1);
            var end = endDate?.ToUniversalTime() ?? DateTimeOffset.UtcNow;

            var jobsResult = await _printJobRepository.GetListByConditionAsync(
                j => j.Status == "completed" && j.CreatedOn >= start && j.CreatedOn <= end,
                includes: [q => q.Include(j => j.RequiredMaterial)]);

            if (!jobsResult.IsSuccess)
            {
                return Result<MaterialConsumptionStats>.Failure(jobsResult.Errors);
            }

            var jobs = jobsResult.Value.ToList();

            // Group by material type
            var consumptionByType = jobs
                .GroupBy(j => j.RequiredMaterial.MaterialType)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(j => j.ActualMaterialInGrams)
                );

            var jobCountByType = jobs
                .GroupBy(j => j.RequiredMaterial.MaterialType)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count()
                );

            var totalMaterial = consumptionByType.Values.Sum();
            var avgMaterialPerJob = jobs.Any() ? totalMaterial / jobs.Count : 0;
            var mostUsed = consumptionByType.Any() 
                ? consumptionByType.OrderByDescending(kvp => kvp.Value).First().Key 
                : "N/A";

            var stats = new MaterialConsumptionStats
            {
                ConsumptionByMaterialType = consumptionByType.ToDictionary(
                    kvp => kvp.Key,
                    kvp => Math.Round(kvp.Value, 2)
                ),
                JobCountByMaterialType = jobCountByType,
                TotalMaterialUsedGrams = Math.Round(totalMaterial, 2),
                AverageMaterialPerJobGrams = Math.Round(avgMaterialPerJob, 2),
                MostUsedMaterial = mostUsed,
                PeriodStart = start,
                PeriodEnd = end
            };

            _logger.LogInformation(
                $"Material consumption: {stats.TotalMaterialUsedGrams}g total, " +
                $"most used: {stats.MostUsedMaterial}");

            return Result<MaterialConsumptionStats>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating material consumption");
            return Result<MaterialConsumptionStats>.Failure(
                Error.InternalServerError("analytics.CONSUMPTION_FAILED", $"Failed to calculate consumption: {ex.Message}"));
        }
    }

    public async Task<Result<QueueTimeEstimate>> EstimateQueueCompletionTimeAsync(int printerId)
    {
        try
        {
            var printerResult = await _printerRepository.GetSingleByConditionAsync(p => p.Id == printerId);
            if (!printerResult.IsSuccess)
            {
                return Result<QueueTimeEstimate>.Failure(
                    Error.NotFound("printer.NOT_FOUND", "Printer not found"));
            }

            var printer = printerResult.Value;

            // Get pending jobs sorted by priority (simplified - using creation date)
            var pendingJobsResult = await _printJobRepository.GetListByConditionAsync(
                j => j.PrinterId == printerId && j.Status == "pending");

            if (!pendingJobsResult.IsSuccess)
            {
                return Result<QueueTimeEstimate>.Failure(pendingJobsResult.Errors);
            }

            var pendingJobs = pendingJobsResult.Value.ToList();

            // Check if there's a currently running job
            var currentJobResult = await _printJobRepository.GetSingleByConditionAsync(
                j => j.PrinterId == printerId && j.Status == "printing");

            double currentJobRemainingTime = 0;
            if (currentJobResult.IsSuccess)
            {
                var currentJob = currentJobResult.Value;
                var elapsed = currentJob.StartedAt.HasValue
                    ? (DateTimeOffset.UtcNow - currentJob.StartedAt.Value).TotalMinutes
                    : 0;
                currentJobRemainingTime = Math.Max(0, currentJob.EstimatedPrintTimeMinutes - elapsed);
            }

            // Calculate cumulative time estimates
            var jobEstimates = new List<JobTimeEstimate>();
            double cumulativeTime = currentJobRemainingTime;

            foreach (var job in pendingJobs)
            {
                var estimatedStartTime = cumulativeTime;
                var estimatedDuration = job.EstimatedPrintTimeMinutes;
                
                jobEstimates.Add(new JobTimeEstimate
                {
                    JobId = job.Id,
                    EstimatedStartTimeMinutes = Math.Round(estimatedStartTime, 2),
                    EstimatedDurationMinutes = Math.Round(estimatedDuration, 2),
                    EstimatedStartDate = DateTimeOffset.UtcNow.AddMinutes(estimatedStartTime),
                    EstimatedCompletionDate = DateTimeOffset.UtcNow.AddMinutes(estimatedStartTime + estimatedDuration)
                });

                cumulativeTime += estimatedDuration;
            }

            var estimate = new QueueTimeEstimate
            {
                PrinterId = printerId,
                PrinterName = printer.Name,
                PendingJobsCount = pendingJobs.Count,
                EstimatedCompletionTimeMinutes = Math.Round(cumulativeTime, 2),
                EstimatedCompletionDate = DateTimeOffset.UtcNow.AddMinutes(cumulativeTime),
                JobEstimates = jobEstimates
            };

            _logger.LogInformation(
                $"Queue time estimate for Printer {printerId}: " +
                $"{estimate.PendingJobsCount} jobs, {estimate.EstimatedCompletionTimeMinutes / 60.0:F1}h total");

            return Result<QueueTimeEstimate>.Success(estimate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error estimating queue time for printer {printerId}");
            return Result<QueueTimeEstimate>.Failure(
                Error.InternalServerError("analytics.ESTIMATE_FAILED", $"Failed to estimate queue time: {ex.Message}"));
        }
    }
}