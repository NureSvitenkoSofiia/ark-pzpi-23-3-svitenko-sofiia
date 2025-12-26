using _3DApi.Infrastructure.ApiResult;
using _3DApi.Infrastructure.Services.Admin;
using _3DApi.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace _3DApi.ApiControllers;

/// <summary>
/// Admin endpoints for printer management
/// </summary>
[ApiController]
[Route("api/admin/printers")]
public class AdminPrintersController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminPrintersController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPrinters()
    {
        var result = await _adminService.GetAllPrintersAsync();
        return result.Match(StatusCodes.Status200OK);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePrinter([FromBody] CreatePrinterRequest request)
    {
        var result = await _adminService.CreatePrinterAsync(request);
        return result.Match(StatusCodes.Status201Created);
    }

    [HttpPut("{printerId}")]
    public async Task<IActionResult> UpdatePrinter(int printerId, [FromBody] UpdatePrinterRequest request)
    {
        var result = await _adminService.UpdatePrinterAsync(printerId, request);
        return result.Match(StatusCodes.Status200OK);
    }

    [HttpDelete("{printerId}")]
    public async Task<IActionResult> DeletePrinter(int printerId)
    {
        var result = await _adminService.DeletePrinterAsync(printerId);
        return result.MatchNoData(StatusCodes.Status200OK);
    }
}

