using _3DApi.Infrastructure.Configurations;
using _3DApi.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace _3DApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
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

        await app.RunAsync();
    }
}