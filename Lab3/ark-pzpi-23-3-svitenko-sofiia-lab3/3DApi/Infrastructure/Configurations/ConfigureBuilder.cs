using _3DApi.Infrastructure.DataAccess;
using _3DApi.Infrastructure.DataAccess.Repo;
using _3DApi.Infrastructure.Services.Email;
using _3DApi.Infrastructure.Services.Printer;
using _3DApi.Infrastructure.Services.PrintJob;
using _3DApi.Infrastructure.Services.Slicer;
using _3DApi.Infrastructure.Services.User;
using _3DApi.Infrastructure.Services.Admin;
using _3DApi.Infrastructure.Services.Analytics;
using _3DApi.Infrastructure.Services.Cost;
using _3DApi.Infrastructure.Services.DataManagement;
using _3DApi.Infrastructure.JsonConverters;
using System.Net;
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

        // Register core services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPrintJobService, PrintJobService>();
        services.AddScoped<IPrinterService, PrinterService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISlicerService, SlicerService>();

        // Register business logic services (Lab 3)
        services.AddScoped<ICostCalculationService, CostCalculationService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        // Register administration services (Lab 3)
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IDataExportService, DataExportService>();

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new IPAddressJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new IPAddressNullableJsonConverter());
            });

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
