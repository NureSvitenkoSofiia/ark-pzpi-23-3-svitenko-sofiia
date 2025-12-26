using _3DApi.Infrastructure.ApiResult;
using _3DApi.Infrastructure.Services.Cost;
using Microsoft.AspNetCore.Mvc;

namespace _3DApi.ApiControllers;

/// <summary>
/// Admin endpoints for cost calculation and pricing
/// </summary>
[ApiController]
[Route("api/admin/cost")]
public class AdminCostController : ControllerBase
{
    private readonly ICostCalculationService _costService;

    public AdminCostController(ICostCalculationService costService)
    {
        _costService = costService;
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> CalculateCost([FromBody] CalculateCostRequest request)
    {
        var result = await _costService.CalculatePrintJobCostAsync(
            request.MaterialInGrams,
            request.PrintTimeMinutes,
            request.MaterialType);
        return result.Match(StatusCodes.Status200OK);
    }

    [HttpGet("jobs/{jobId}/pricing")]
    public async Task<IActionResult> GetOptimalPricing(int jobId, [FromQuery] double profitMarginPercent = 30.0)
    {
        var result = await _costService.CalculateOptimalPricingAsync(jobId, profitMarginPercent);
        return result.Match(StatusCodes.Status200OK);
    }
}

public class CalculateCostRequest
{
    public double MaterialInGrams { get; set; }
    public double PrintTimeMinutes { get; set; }
    public string MaterialType { get; set; }
}

