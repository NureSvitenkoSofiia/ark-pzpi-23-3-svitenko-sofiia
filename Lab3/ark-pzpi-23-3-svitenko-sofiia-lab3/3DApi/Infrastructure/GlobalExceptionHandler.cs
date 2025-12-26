using _3DApi.Infrastructure.ApiResult;
using _3DApi.Infrastructure.Errors;
using Microsoft.AspNetCore.Diagnostics;

namespace _3DApi.Infrastructure;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, 
            "Unhandled exception occurred. Path: {Path}, Method: {Method}", 
            httpContext.Request.Path, 
            httpContext.Request.Method);

        var result = BaseApiResults.ToProblemDetailsObject(ApplicationErrors.ApplicationError);

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = BaseApiResults.GetStatusCode(ApplicationErrors.ApplicationError.Type);

        await httpContext.Response.WriteAsJsonAsync(result, cancellationToken);

        return true;
    }
}