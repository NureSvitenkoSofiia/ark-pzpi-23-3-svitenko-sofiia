namespace _3DApi.Infrastructure.Errors;

public class EmailError
{
    public static Error EmailNotSent()
    {
        return Error.NotFound("common.EMAIL_NOT_SENT", "Email cannot be send to user");
    }

    public static Error InvalidOrExpiredToken()
    {
        return Error.NotFound("common.EMAIL_INVALID_OR_EXPIRED_TOKEN",
            "Token to activate email is either invalid or expired");
    }
}