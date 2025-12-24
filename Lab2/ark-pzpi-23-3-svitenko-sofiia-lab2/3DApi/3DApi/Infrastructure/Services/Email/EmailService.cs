using System.Net;
using System.Net.Mail;
using _3DApi.Infrastructure.Errors;

namespace _3DApi.Infrastructure.Services.Email;
//TODO
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly string _smtpEmail;
    private readonly string _smtpPassword;
    private readonly string _smtpHost;
    private readonly int _smtpHostPort;
    private readonly ILogger<IEmailService> _logger;
    public EmailService(IConfiguration config, ILogger<IEmailService> logger)
    {
        _config = config;
        _smtpEmail = config["SmtpSettings:Email"];
        _smtpPassword = config["SmtpSettings:Password"];
        _smtpHost = config["SmtpSettings:Host"];
        _smtpHostPort = int.Parse(config["SmtpSettings:HostPort"]!);
        _logger = logger;
    }
    
    public async Task SendSuccessfulEmailAsync(string email, string message, string subject)
    {
        var body = $"{message}<br/><br/>We’re glad to have you with us! 🎉";
        var htmlMessage = BuildHtmlMessage(subject, body, "Thank you for being part of our community.");

        await SendSmtpEmailAsync(email, subject, htmlMessage);
    }

    public async Task SendErrorEmailAsync(string email, string message, string subject)
    {
        var body =
            $"We encountered an error while processing your request:<br/><br/><strong>{message}</strong><br/><br/>" +
            $"Please try again or contact support if the issue persists.";

        var htmlMessage = BuildHtmlMessage(subject, body,
            "This is an automated email, please do not reply directly.");

        await SendSmtpEmailAsync(email, subject, htmlMessage);
    }

    private async Task<Result> SendSmtpEmailAsync(string recipientEmail, string subject, string htmlBody)
    {
        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpEmail, "Lexi"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(recipientEmail);

            using var client = new SmtpClient(_smtpHost, _smtpHostPort)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_smtpEmail, _smtpPassword),
                Timeout = 3000
            };

            await client.SendMailAsync(mailMessage);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending email to {recipientEmail} with subject: {subject}.");
            return Result.Failure(EmailError.EmailNotSent());
        }
    }

    private string BuildHtmlMessage(string title, string body, string footer = null)
    {
        return $@"
        <html>
          <head>
            <style>
              body {{
                font-family: Arial, sans-serif;
                background-color: #f9f9f9;
                padding: 20px;
                color: #333;
              }}
              .container {{
                max-width: 600px;
                background-color: #ffffff;
                border-radius: 10px;
                box-shadow: 0 4px 10px rgba(0,0,0,0.1);
                padding: 30px;
              }}
              h1 {{
                font-size: 20px;
                margin-bottom: 20px;
                color: #2c3e50;
              }}
              p {{
                line-height: 1.6;
              }}
              a.button {{
                display: inline-block;
                padding: 10px 20px;
                margin-top: 20px;
                background-color: #3498db;
                color: #fff !important;
                text-decoration: none;
                border-radius: 5px;
              }}
              .footer {{
                margin-top: 30px;
                font-size: 12px;
                color: #999;
              }}
            </style>
          </head>
          <body>
            <div class='container'>
              <h1>{title}</h1>
              <p>{body}</p>
              {(footer != null ? $"<div class='footer'>{footer}</div>" : "")}
            </div>
          </body>
        </html>";
    }
}