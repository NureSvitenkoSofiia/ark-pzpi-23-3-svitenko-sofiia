using _3DApi.Infrastructure.Errors;
using Microsoft.AspNetCore.Mvc;

namespace _3DApi.Infrastructure.ApiResult;

public static class BaseApiResults
{
    public static ProblemDetails ToProblemDetailsObject(Error error)
    {
        return new ProblemDetails
        {
            Status = GetStatusCode(error.Type),
            Title = error.Code,
            Type = GetType(error.Type),
            Detail = error.Description,
            Extensions = new Dictionary<string, object?>
            {
                { "error", error }
            }
        };
    }
    
    public static IActionResult ToProblemDetails(Error error)
    {
        var problemDetails = ToProblemDetailsObject(error);

        return new ObjectResult(problemDetails);
    }
    
    public static IActionResult ToProblemDetails(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException();
        }

        var firstError = result.Errors.FirstOrDefault();
        
        return ToProblemDetails(firstError);
    }

    public static int GetStatusCode(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError,
        };

    public static string GetType(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3", //403
            ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            ErrorType.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1", // 401 Unauthorized
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1", // 500 Internal Server Error
        };
}