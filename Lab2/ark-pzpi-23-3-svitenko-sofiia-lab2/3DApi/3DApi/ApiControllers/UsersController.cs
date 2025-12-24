using _3DApi.Infrastructure.ApiResult;
using _3DApi.Infrastructure.Services.User;
using _3DApi.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace _3DApi.ApiControllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
    {
        var result = await _userService.RegisterAsync(request);
        return result.Match(StatusCodes.Status201Created);
    }

    /// <summary>
    /// Get list of user's own jobs with statuses
    /// </summary>
    [HttpGet("{userId}/jobs")]
    public async Task<IActionResult> GetUserJobs(int userId)
    {
        var result = await _userService.GetUserJobsAsync(userId);
        return result.Match(StatusCodes.Status200OK);
    }
}

