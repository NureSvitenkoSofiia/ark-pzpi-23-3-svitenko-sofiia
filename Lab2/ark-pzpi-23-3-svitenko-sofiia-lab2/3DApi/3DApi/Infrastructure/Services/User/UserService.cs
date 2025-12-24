namespace _3DApi.Infrastructure.Services.User;

using Errors;
using Microsoft.EntityFrameworkCore;
using DataAccess.Repo;
using Models;
using Models.DTOs;

public class UserService : IUserService
{
    private readonly IGenericRepository<User> _userRepository;
    private readonly IGenericRepository<PrintJob> _printJobRepository;

    public UserService(
        IGenericRepository<User> userRepository,
        IGenericRepository<PrintJob> printJobRepository)
    {
        _userRepository = userRepository;
        _printJobRepository = printJobRepository;
    }

    public async Task<Result<RegisterUserResponse>> RegisterAsync(RegisterUserRequest request)
    {
        // Check if user with this email already exists
        var existingUserResult = await _userRepository.GetSingleByConditionAsync(
            u => u.Email == request.Email);

        if (existingUserResult.IsSuccess)
        {
            return Result<RegisterUserResponse>.Failure(
                Error.Conflict("user.EMAIL_ALREADY_EXISTS", "User with this email already exists"));
        }

        var user = new User
        {
            Email = request.Email
        };

        var addResult = await _userRepository.AddAsync(user);
        if (!addResult.IsSuccess)
        {
            return Result<RegisterUserResponse>.Failure(addResult.Errors);
        }

        return Result<RegisterUserResponse>.Success(new RegisterUserResponse
        {
            Id = user.Id,
            Email = user.Email
        });
    }

    public async Task<Result<IEnumerable<PrintJobResponse>>> GetUserJobsAsync(int userId)
    {
        var jobsResult = await _printJobRepository.GetListByConditionAsync(
            condition: pj => pj.RequesterId == userId,
            includes: new[]
            {
                q => q.Include(pj => pj.RequiredMaterial),
                q => q.Include(pj => pj.Printer),
                new Func<IQueryable<PrintJob>, IQueryable<PrintJob>>(q => q.Include(pj => pj.Requester))
            },
            orderBy: new OrderByOptions<PrintJob>
            {
                Expression = pj => pj.CreatedOn,
                IsDescending = true
            }
        );

        if (!jobsResult.IsSuccess)
        {
            return Result<IEnumerable<PrintJobResponse>>.Failure(jobsResult.Errors);
        }

        var response = jobsResult.Value.Select(job => new PrintJobResponse
        {
            Id = job.Id,
            StlFilePath = job.StlFilePath,
            GCodeFilePath = job.GCodeFilePath,
            RequiredMaterial = new MaterialInfo
            {
                Id = job.RequiredMaterial.Id,
                Color = job.RequiredMaterial.Color,
                MaterialType = job.RequiredMaterial.MaterialType
            },
            EstimatedMaterialInGrams = job.EstimatedMaterialInGrams,
            ActualMaterialInGrams = job.ActualMaterialInGrams,
            Printer = new PrinterInfo
            {
                Id = job.Printer.Id,
                Name = job.Printer.Name,
                Status = job.Printer.Status
            },
            Status = job.Status,
            ErrorMessage = job.ErrorMessage,
            CreatedOn = job.CreatedOn,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt == default ? null : job.CompletedAt
        });

        return Result<IEnumerable<PrintJobResponse>>.Success(response);
    }
}

