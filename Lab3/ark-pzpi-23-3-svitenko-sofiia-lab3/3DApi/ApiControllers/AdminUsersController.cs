using _3DApi.Infrastructure.ApiResult;
using _3DApi.Infrastructure.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace _3DApi.ApiControllers;

/// <summary>
/// Admin endpoints for user management
/// </summary>
[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminUsersController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var result = await _adminService.GetAllUsersAsync();
        return result.Match(StatusCodes.Status200OK);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserById(int userId)
    {
        var result = await _adminService.GetUserByIdAsync(userId);
        return result.Match(StatusCodes.Status200OK);
    }

    [HttpPut("{userId}/role")]
    public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateUserRoleRequest request)
    {
        var result = await _adminService.UpdateUserRoleAsync(userId, request.Role);
        return result.Match(StatusCodes.Status200OK);
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        var result = await _adminService.DeleteUserAsync(userId);
        return result.MatchNoData(StatusCodes.Status200OK);
    }
}

public class UpdateUserRoleRequest
{
    public string Role { get; set; }
}

