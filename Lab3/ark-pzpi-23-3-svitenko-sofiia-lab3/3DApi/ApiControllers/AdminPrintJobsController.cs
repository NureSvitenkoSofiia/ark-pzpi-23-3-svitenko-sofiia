using _3DApi.Infrastructure.ApiResult;
using _3DApi.Infrastructure.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace _3DApi.ApiControllers;

/// <summary>
/// Admin endpoints for print job management
/// </summary>
[ApiController]
[Route("api/admin/jobs")]
public class AdminPrintJobsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminPrintJobsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPrintJobs([FromQuery] string? status = null)
    {
        var result = await _adminService.GetAllPrintJobsAsync(status);
        return result.Match(StatusCodes.Status200OK);
    }

    [HttpGet("{jobId}")]
    public async Task<IActionResult> GetPrintJobById(int jobId)
    {
        var result = await _adminService.GetPrintJobByIdAsync(jobId);
        return result.Match(StatusCodes.Status200OK);
    }

    [HttpPost("{jobId}/cancel")]
    public async Task<IActionResult> CancelPrintJob(int jobId, [FromBody] CancelJobRequest request)
    {
        var result = await _adminService.CancelPrintJobAsync(jobId, request.Reason);
        return result.MatchNoData(StatusCodes.Status200OK);
    }

    [HttpPost("{jobId}/reassign")]
    public async Task<IActionResult> ReassignPrintJob(int jobId, [FromBody] ReassignJobRequest request)
    {
        var result = await _adminService.ReassignPrintJobAsync(jobId, request.NewPrinterId);
        return result.MatchNoData(StatusCodes.Status200OK);
    }
}

public class CancelJobRequest
{
    public string Reason { get; set; }
}

public class ReassignJobRequest
{
    public int NewPrinterId { get; set; }
}

