namespace _3DApi.Infrastructure.Errors;

public enum ErrorType
{
    Unauthorized = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    InternalServer = 4,
    Forbidden = 5,
    None = -1
}

public record Error
{
    private Error(string code, string description, ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }

    public string Code { get; init; }
    public string Description { get; init; }
    public ErrorType Type { get; init; }

    public static Error InternalServerError(string code, string description) => new(code, description, ErrorType.InternalServer);
    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);
    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);
    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string description) => new(code, description, ErrorType.Forbidden);
    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
    public static Error None(string code, string description) => new(code, description, ErrorType.None);
}