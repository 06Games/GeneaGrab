using Avalonia;
using GeneaGrab.Core.Models;
using GeneaGrab.Helpers;
using GeneaGrab.Models.Indexing;
using Microsoft.EntityFrameworkCore;

namespace GeneaGrab.Services;

public class DatabaseContext : DbContext
{
    public DbSet<Registry> Registries { get; set; } = null!;
    public DbSet<Frame> Frames { get; set; } = null!;
    public DbSet<Record> Records { get; set; } = null!;
    public DbSet<Person> Persons { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={LocalData.AppData}/data.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Record>(e =>
        {
            e.Property(b => b.Position)
                .HasConversion(
                    rect => rect == null ? null : rect.ToString(),
                    rectStr => rectStr == null ? null : Rect.Parse(rectStr));
            e.HasOne(r => r.Frame).WithMany().HasForeignKey(r => new { r.ProviderId, r.RegistryId, r.FrameNumber });
        });
        modelBuilder.Entity<Registry>(e =>
        {
            e.HasMany(r => r.Frames).WithOne(f => f.Registry).HasForeignKey(r => new { r.ProviderId, r.RegistryId });
        });
    }
}
