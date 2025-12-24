namespace _3DApi.Infrastructure.Services.Email;

public interface IEmailService
{
    public Task SendSuccessfulEmailAsync(string email, string message, string subject);

    public Task SendErrorEmailAsync(string email, string message, string subject);
}