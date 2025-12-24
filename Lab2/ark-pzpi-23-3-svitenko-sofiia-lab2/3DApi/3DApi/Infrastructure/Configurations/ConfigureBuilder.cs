using _3DApi.Infrastructure.DataAccess;
using _3DApi.Infrastructure.DataAccess.Repo;
using _3DApi.Infrastructure.Services.Email;
using _3DApi.Infrastructure.Services.Printer;
using _3DApi.Infrastructure.Services.PrintJob;
using _3DApi.Infrastructure.Services.Slicer;
using _3DApi.Infrastructure.Services.User;
using Microsoft.EntityFrameworkCore;

namespace _3DApi.Infrastructure.Configurations;

public static class ConfigureBuilder
{
    public static void Configure(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddDbContext<MainDbContext>(opts => { opts.UseNpgsql(builder.Configuration.GetConnectionString("DataContext")); });

        // Register repositories
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // Register services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPrintJobService, PrintJobService>();
        services.AddScoped<IPrinterService, PrinterService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISlicerService, SlicerService>();

        services.AddControllers();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddProblemDetails();
    }
}
