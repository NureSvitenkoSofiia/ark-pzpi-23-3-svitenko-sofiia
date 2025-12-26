using _3DApi.Infrastructure.ApiResult;
using _3DApi.Infrastructure.Services.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace _3DApi.ApiControllers;

/// <summary>
/// Admin endpoints for analytics and statistics
/// </summary>
[ApiController]
[Route("api/admin/analytics")]
public class AdminAnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AdminAnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("statistics/system")]
    public async Task<IActionResult> GetSystemStatistics()
    {
        var result = await _analyticsService.GetSystemStatisticsAsync();
        return result.Match(StatusCodes.Status200OK);
    }

    [HttpGet("statistics/printers/{printerId}")]
    public async Task<IActionResult> GetPrinterPerformance(int printerId)
    {
        var result = await _analyticsService.GetPrinterPerformanceAsync(printerId);
        return result.Match(StatusCodes.Status200OK);
    }

    [HttpGet("statistics/users/{userId}")]
    public async Task<IActionResult> GetUserActivity(int userId)
    {
        var result = await _analyticsService.GetUserActivityAsync(userId);
        return result.Match(StatusCodes.Status200OK);
    }

    [HttpGet("success-rates")]
    public async Task<IActionResult> GetSuccessRateAnalysis([FromQuery] DateTimeOffset? startDate, [FromQuery] DateTimeOffset? endDate)
    {
        var result = await _analyticsService.AnalyzeSuccessRatesAsync(startDate, endDate);
        return result.Match(StatusCodes.Status200OK);
    }

    [HttpGet("material-consumption")]
    public async Task<IActionResult> GetMaterialConsumption([FromQuery] DateTimeOffset? startDate, [FromQuery] DateTimeOffset? endDate)
    {
        var result = await _analyticsService.GetMaterialConsumptionAsync(startDate, endDate);
        return result.Match(StatusCodes.Status200OK);
    }

    [HttpGet("queue/{printerId}/estimate")]
    public async Task<IActionResult> GetQueueEstimate(int printerId)
    {
        var result = await _analyticsService.EstimateQueueCompletionTimeAsync(printerId);
        return result.Match(StatusCodes.Status200OK);
    }
}

