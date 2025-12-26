namespace _3DApi.Infrastructure.Errors;

public static class ApplicationErrors
{
    public static readonly Error ApplicationError =
        Error.InternalServerError("common.APPLICATION_ERROR", "Something went wrong");
}