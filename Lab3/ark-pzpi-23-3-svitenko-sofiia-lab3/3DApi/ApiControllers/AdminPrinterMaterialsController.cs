using _3DApi.Infrastructure.ApiResult;
using _3DApi.Infrastructure.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace _3DApi.ApiControllers;

/// <summary>
/// Admin endpoints for printer-material assignment management
/// </summary>
[ApiController]
[Route("api/admin/printers/{printerId}/materials")]
public class AdminPrinterMaterialsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminPrinterMaterialsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpPost]
    public async Task<IActionResult> AssignMaterialToPrinter(int printerId, [FromBody] AssignMaterialRequest request)
    {
        var result = await _adminService.AssignMaterialToPrinterAsync(printerId, request.MaterialId, request.QuantityInGrams);
        return result.MatchNoData(StatusCodes.Status200OK);
    }

    [HttpPut("{materialId}")]
    public async Task<IActionResult> UpdatePrinterMaterialQuantity(
        int printerId,
        int materialId,
        [FromBody] UpdateMaterialQuantityRequest request)
    {
        var result = await _adminService.UpdatePrinterMaterialQuantityAsync(printerId, materialId, request.QuantityInGrams);
        return result.MatchNoData(StatusCodes.Status200OK);
    }

    [HttpDelete("{materialId}")]
    public async Task<IActionResult> RemoveMaterialFromPrinter(int printerId, int materialId)
    {
        var result = await _adminService.RemoveMaterialFromPrinterAsync(printerId, materialId);
        return result.MatchNoData(StatusCodes.Status200OK);
    }
}

public class AssignMaterialRequest
{
    public int MaterialId { get; set; }
    public double QuantityInGrams { get; set; }
}

public class UpdateMaterialQuantityRequest
{
    public double QuantityInGrams { get; set; }
}

