using _3DApi.Infrastructure.ApiResult;
using _3DApi.Infrastructure.Services.Printer;
using _3DApi.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace _3DApi.ApiControllers;

[ApiController]
[Route("api/[controller]")]
public class PrinterController : ControllerBase
{
    private readonly IPrinterService _printerService;

    public PrinterController(IPrinterService printerService)
    {
        _printerService = printerService;
    }

    [HttpPost("{printerId}/ping")]
    public async Task<IActionResult> Ping(int printerId, [FromBody] PingRequest request)
    {
        var result = await _printerService.PingAsync(printerId, request);
        return result.MatchNoData(StatusCodes.Status200OK);
    }

    [HttpGet("{printerId}/queue")]
    public async Task<IActionResult> PollJobQueue(int printerId)
    {
        var result = await _printerService.PollJobQueueAsync(printerId);
        return result.Match(StatusCodes.Status200OK);
    }

    [HttpPost("{printerId}/jobs/{jobId}/start")]
    public async Task<IActionResult> StartJob(int printerId, int jobId)
    {
        var result = await _printerService.StartJobAsync(printerId, jobId);
        return result.MatchNoData(StatusCodes.Status200OK);
    }

    [HttpPost("{printerId}/jobs/{jobId}/finish")]
    public async Task<IActionResult> FinishJob(int printerId, int jobId, [FromBody] FinishJobRequest request)
    {
        var result = await _printerService.FinishJobAsync(printerId, jobId, request);
        return result.MatchNoData(StatusCodes.Status200OK);
    }
}

