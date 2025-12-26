using System.Drawing;
using System.Net;
using _3DApi.Models;
using Microsoft.EntityFrameworkCore;

namespace _3DApi.Infrastructure.DataAccess;

public class MainDbContext : DbContext
{
    public MainDbContext(DbContextOptions<MainDbContext> opts) : base(opts)
    {
    }
    
    public DbSet<Material> Materials { get; set; }
    
    public DbSet<Printer> Printers { get; set; }
    
    public DbSet<PrinterMaterial> PrinterMaterials { get; set; }
    
    public DbSet<PrintJob> PrintJobs { get; set; }
    
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        ConfigureBaseEntity<Material>(builder);
        ConfigureBaseEntity<Printer>(builder);
        ConfigureBaseEntity<PrinterMaterial>(builder);
        ConfigureBaseEntity<PrintJob>(builder);
        ConfigureBaseEntity<User>(builder);
        
        builder.Entity<Material>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).UseIdentityColumn();
            
            entity.Property(m => m.Color)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(m => m.ColorHex)
                .HasConversion(
                    c => c.ToArgb(),
                    i => Color.FromArgb(i));
            
            entity.Property(m => m.MaterialType)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(m => m.DensityInGramsPerCm3)
                .IsRequired()
                .HasPrecision(10, 4);
            
            entity.Property(m => m.DiameterMm)
                .IsRequired()
                .HasPrecision(10, 2);
            
            entity.HasMany(m => m.Printers)
                .WithOne(pm => pm.Material)
                .HasForeignKey(pm => pm.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasMany<PrintJob>()
                .WithOne(pj => pj.RequiredMaterial)
                .HasForeignKey(pj => pj.RequiredMaterialId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(m => m.MaterialType);
        });
        
        builder.Entity<Printer>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).UseIdentityColumn();
            
            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(p => p.Status)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(p => p.LastPing)
                .IsRequired();
            
            entity.Property(p => p.Ip)
                .IsRequired()
                .HasConversion(
                    ip => ip.ToString(),
                    s => IPAddress.Parse(s));
            
            entity.HasMany(p => p.Materials)
                .WithOne(pm => pm.Printer)
                .HasForeignKey(pm => pm.PrinterId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany<PrintJob>()
                .WithOne(pj => pj.Printer)
                .HasForeignKey(pj => pj.PrinterId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(p => p.Status);
            entity.HasIndex(p => p.Ip);
        });
        
        builder.Entity<PrinterMaterial>(entity =>
        {
            entity.HasKey(pm => pm.Id);
            entity.Property(pm => pm.Id).UseIdentityColumn();
            
            entity.Property(pm => pm.QuantityInG)
                .IsRequired()
                .HasPrecision(10, 2);
            
            entity.HasOne(pm => pm.Printer)
                .WithMany(p => p.Materials)
                .HasForeignKey(pm => pm.PrinterId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(pm => pm.Material)
                .WithMany(m => m.Printers)
                .HasForeignKey(pm => pm.MaterialId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(pm => new { pm.PrinterId, pm.MaterialId })
                .IsUnique();
        });
        
        builder.Entity<PrintJob>(entity =>
        {
            entity.HasKey(pj => pj.Id);
            entity.Property(pj => pj.Id).UseIdentityColumn();
            
            entity.Property(pj => pj.StlFilePath)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(pj => pj.GCodeFilePath)
                .HasMaxLength(500);
            
            entity.Property(pj => pj.EstimatedMaterialInGrams)
                .IsRequired()
                .HasPrecision(10, 2);
            
            entity.Property(pj => pj.ActualMaterialInGrams)
                .HasPrecision(10, 2);
            
            entity.Property(pj => pj.Status)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(pj => pj.ErrorMessage)
                .HasMaxLength(1000);
            
            entity.HasOne(pj => pj.RequiredMaterial)
                .WithMany()
                .HasForeignKey(pj => pj.RequiredMaterialId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(pj => pj.Printer)
                .WithMany()
                .HasForeignKey(pj => pj.PrinterId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(pj => pj.Requester)
                .WithMany()
                .HasForeignKey(pj => pj.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(pj => pj.Status);
            entity.HasIndex(pj => pj.RequesterId);
            entity.HasIndex(pj => pj.PrinterId);
        });
        
        builder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).UseIdentityColumn();
            
            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.HasMany<PrintJob>()
                .WithOne(pj => pj.Requester)
                .HasForeignKey(pj => pj.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(u => u.Email)
                .IsUnique();
        });
    }
    
    private void ConfigureBaseEntity<T>(ModelBuilder builder) where T : Base
    {
        builder.Entity<T>(entity =>
        {
            entity.Property(e => e.CreatedOn)
                .IsRequired();
            
            entity.Property(e => e.LastModifiedOn)
                .IsRequired();
        });
    }
}