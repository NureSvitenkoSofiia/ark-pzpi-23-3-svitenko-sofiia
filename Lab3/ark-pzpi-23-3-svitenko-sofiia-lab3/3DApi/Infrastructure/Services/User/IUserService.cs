using _3DApi.Models.DTOs;

namespace _3DApi.Infrastructure.Services.User;

public interface IUserService
{
    Task<Result<RegisterUserResponse>> RegisterAsync(RegisterUserRequest request);
    
    Task<Result<IEnumerable<PrintJobResponse>>> GetUserJobsAsync(int userId);
}

