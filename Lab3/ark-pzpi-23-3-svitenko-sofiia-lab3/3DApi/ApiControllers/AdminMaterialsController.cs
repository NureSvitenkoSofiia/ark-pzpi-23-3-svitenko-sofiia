using _3DApi.Infrastructure.ApiResult;
using _3DApi.Infrastructure.Services.Admin;
using _3DApi.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace _3DApi.ApiControllers;

/// <summary>
/// Admin endpoints for material management
/// </summary>
[ApiController]
[Route("api/admin/materials")]
public class AdminMaterialsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminMaterialsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllMaterials()
    {
        var result = await _adminService.GetAllMaterialsAsync();
        return result.Match(StatusCodes.Status200OK);
    }

    [HttpPost]
    public async Task<IActionResult> CreateMaterial([FromBody] CreateMaterialRequest request)
    {
        var result = await _adminService.CreateMaterialAsync(request);
        return result.Match(StatusCodes.Status201Created);
    }

    [HttpPut("{materialId}")]
    public async Task<IActionResult> UpdateMaterial(int materialId, [FromBody] UpdateMaterialRequest request)
    {
        var result = await _adminService.UpdateMaterialAsync(materialId, request);
        return result.Match(StatusCodes.Status200OK);
    }

    [HttpDelete("{materialId}")]
    public async Task<IActionResult> DeleteMaterial(int materialId)
    {
        var result = await _adminService.DeleteMaterialAsync(materialId);
        return result.MatchNoData(StatusCodes.Status200OK);
    }
}

