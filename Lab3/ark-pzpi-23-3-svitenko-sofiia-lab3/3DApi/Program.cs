using _3DApi.Infrastructure.Configurations;
using _3DApi.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace _3DApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Configure Serilog
        var logPath = builder.Configuration["Logging:FilePath"] ?? @"C:\3d\logs";
        Directory.CreateDirectory(logPath);
        
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(logPath, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {MachineName} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
        
        builder.Host.UseSerilog();
        
        builder.Configure();

        var app = builder.Build();

        // Seed database if no printers exist
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            
            // Ensure database is created and migrations are applied
            await context.Database.MigrateAsync();
            
            // Check if we need to seed
            if (!await context.Printers.AnyAsync())
            {
                Console.WriteLine("No printers found. Seeding database...");
                var seeder = new DbSeeder(context);
                await seeder.SeedAsync();
            }
        }

        await app.Configure();

        try
        {
            Log.Information("Starting 3D API application");
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}