using System.Drawing;
using System.Net;
using _3DApi.Models;

namespace _3DApi.Infrastructure.DataAccess;

public class DbSeeder
{
    private readonly MainDbContext _context;

    public DbSeeder(MainDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        // Check if data already exists
        if (_context.Materials.Any())
        {
            Console.WriteLine("Database already seeded.");
            return;
        }

        Console.WriteLine("Seeding database...");

        var now = DateTimeOffset.UtcNow;

        // Create Materials
        var materials = new List<Material>
        {
            new Material
            {
                Color = "Red",
                ColorHex = Color.FromArgb(255, 0, 0),
                MaterialType = "PLA",
                DensityInGramsPerCm3 = 1.24,
                DiameterMm = 1.75,
                CreatedOn = now,
                LastModifiedOn = now
            },
            new Material
            {
                Color = "Blue",
                ColorHex = Color.FromArgb(0, 0, 255),
                MaterialType = "PLA",
                DensityInGramsPerCm3 = 1.24,
                DiameterMm = 1.75,
                CreatedOn = now,
                LastModifiedOn = now
            },
            new Material
            {
                Color = "White",
                ColorHex = Color.FromArgb(255, 255, 255),
                MaterialType = "PLA",
                DensityInGramsPerCm3 = 1.24,
                DiameterMm = 1.75,
                CreatedOn = now,
                LastModifiedOn = now
            },
            new Material
            {
                Color = "Black",
                ColorHex = Color.FromArgb(0, 0, 0),
                MaterialType = "ABS",
                DensityInGramsPerCm3 = 1.04,
                DiameterMm = 1.75,
                CreatedOn = now,
                LastModifiedOn = now
            },
            new Material
            {
                Color = "Natural",
                ColorHex = Color.FromArgb(240, 234, 214),
                MaterialType = "PETG",
                DensityInGramsPerCm3 = 1.27,
                DiameterMm = 1.75,
                CreatedOn = now,
                LastModifiedOn = now
            },
            new Material
            {
                Color = "Green",
                ColorHex = Color.FromArgb(0, 255, 0),
                MaterialType = "PLA",
                DensityInGramsPerCm3 = 1.24,
                DiameterMm = 1.75,
                CreatedOn = now,
                LastModifiedOn = now
            }
        };

        await _context.Materials.AddRangeAsync(materials);
        await _context.SaveChangesAsync();
        Console.WriteLine($"Seeded {materials.Count} materials.");

        // Create Printer
        var printer = new Printer
        {
            Name = "Ender 3 V2",
            Status = "idle",
            LastPing = now,
            Ip = IPAddress.Parse("192.168.1.100"),
            CreatedOn = now,
            LastModifiedOn = now
        };

        await _context.Printers.AddAsync(printer);
        await _context.SaveChangesAsync();
        Console.WriteLine($"Seeded printer: {printer.Name}");

        // Create Printer-Material relationships
        var printerMaterials = new List<PrinterMaterial>
        {
            new PrinterMaterial
            {
                PrinterId = printer.Id,
                MaterialId = materials[0].Id, // Red PLA
                QuantityInG = 1000.0,
                CreatedOn = now,
                LastModifiedOn = now
            },
            new PrinterMaterial
            {
                PrinterId = printer.Id,
                MaterialId = materials[1].Id, // Blue PLA
                QuantityInG = 750.0,
                CreatedOn = now,
                LastModifiedOn = now
            },
            new PrinterMaterial
            {
                PrinterId = printer.Id,
                MaterialId = materials[2].Id, // White PLA
                QuantityInG = 500.0,
                CreatedOn = now,
                LastModifiedOn = now
            },
            new PrinterMaterial
            {
                PrinterId = printer.Id,
                MaterialId = materials[3].Id, // Black ABS
                QuantityInG = 800.0,
                CreatedOn = now,
                LastModifiedOn = now
            },
            new PrinterMaterial
            {
                PrinterId = printer.Id,
                MaterialId = materials[5].Id, // Green PLA
                QuantityInG = 900.0,
                CreatedOn = now,
                LastModifiedOn = now
            }
        };

        await _context.PrinterMaterials.AddRangeAsync(printerMaterials);
        await _context.SaveChangesAsync();
        Console.WriteLine($"Seeded {printerMaterials.Count} printer-material relationships.");

        // Create Test User
        var user = new User
        {
            Email = "test@example.com",
            CreatedOn = now,
            LastModifiedOn = now
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        Console.WriteLine($"Seeded user: {user.Email}");

        Console.WriteLine("Database seeding completed successfully!");
    }
}

