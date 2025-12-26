namespace _3DApi.Infrastructure.Services.Analytics;

using Models;

/// <summary>
/// Service for statistical analysis and system analytics
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Get system-wide statistics using statistical aggregation methods
    /// </summary>
    Task<Result<SystemStatistics>> GetSystemStatisticsAsync();

    /// <summary>
    /// Get printer performance metrics using statistical analysis
    /// </summary>
    Task<Result<PrinterPerformanceMetrics>> GetPrinterPerformanceAsync(int printerId);

    /// <summary>
    /// Get user activity statistics
    /// </summary>
    Task<Result<UserActivityMetrics>> GetUserActivityAsync(int userId);

    /// <summary>
    /// Calculate success rate and failure analysis
    /// </summary>
    Task<Result<SuccessRateAnalysis>> AnalyzeSuccessRatesAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);

    /// <summary>
    /// Get material consumption statistics and trends
    /// </summary>
    Task<Result<MaterialConsumptionStats>> GetMaterialConsumptionAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);

    /// <summary>
    /// Predict estimated completion time for pending jobs using regression
    /// </summary>
    Task<Result<QueueTimeEstimate>> EstimateQueueCompletionTimeAsync(int printerId);
}

public class SystemStatistics
{
    public int TotalPrintJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
    public int PendingJobs { get; set; }
    public int ActiveJobs { get; set; }
    public double OverallSuccessRate { get; set; }
    public double TotalMaterialUsedGrams { get; set; }
    public double TotalPrintTimeHours { get; set; }
    public double AveragePrintTimeMinutes { get; set; }
    public double AverageMaterialUsageGrams { get; set; }
    public int TotalPrinters { get; set; }
    public int OnlinePrinters { get; set; }
    public int TotalUsers { get; set; }
}

public class PrinterPerformanceMetrics
{
    public int PrinterId { get; set; }
    public string PrinterName { get; set; }
    public int TotalJobsCompleted { get; set; }
    public int TotalJobsFailed { get; set; }
    public double SuccessRate { get; set; }
    public double TotalOperatingHours { get; set; }
    public double AverageJobDurationMinutes { get; set; }
    public double UtilizationRate { get; set; } // % of time printer was active
    public DateTimeOffset LastActiveTime { get; set; }
    public double MeanTimeBetweenFailures { get; set; } // MTBF in hours
}

public class UserActivityMetrics
{
    public int UserId { get; set; }
    public string UserEmail { get; set; }
    public int TotalJobsSubmitted { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
    public int PendingJobs { get; set; }
    public double TotalMaterialUsedGrams { get; set; }
    public double TotalPrintTimeHours { get; set; }
    public DateTimeOffset FirstJobDate { get; set; }
    public DateTimeOffset? LastJobDate { get; set; }
}

public class SuccessRateAnalysis
{
    public double OverallSuccessRate { get; set; }
    public int TotalJobs { get; set; }
    public int SuccessfulJobs { get; set; }
    public int FailedJobs { get; set; }
    public Dictionary<string, double> SuccessRateByMaterial { get; set; }
    public Dictionary<int, double> SuccessRateByPrinter { get; set; }
    public List<string> CommonFailureReasons { get; set; }
    public DateTimeOffset AnalysisPeriodStart { get; set; }
    public DateTimeOffset AnalysisPeriodEnd { get; set; }
}

public class MaterialConsumptionStats
{
    public Dictionary<string, double> ConsumptionByMaterialType { get; set; } // Material type -> grams
    public Dictionary<string, int> JobCountByMaterialType { get; set; }
    public double TotalMaterialUsedGrams { get; set; }
    public double AverageMaterialPerJobGrams { get; set; }
    public string MostUsedMaterial { get; set; }
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
}

public class QueueTimeEstimate
{
    public int PrinterId { get; set; }
    public string PrinterName { get; set; }
    public int PendingJobsCount { get; set; }
    public double EstimatedCompletionTimeMinutes { get; set; }
    public DateTimeOffset EstimatedCompletionDate { get; set; }
    public List<JobTimeEstimate> JobEstimates { get; set; }
}

public class JobTimeEstimate
{
    public int JobId { get; set; }
    public double EstimatedStartTimeMinutes { get; set; }
    public double EstimatedDurationMinutes { get; set; }
    public DateTimeOffset EstimatedStartDate { get; set; }
    public DateTimeOffset EstimatedCompletionDate { get; set; }
}