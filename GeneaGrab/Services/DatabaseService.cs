using Avalonia;
using GeneaGrab.Helpers;
using GeneaGrab.Models.Indexing;
using Microsoft.EntityFrameworkCore;

namespace GeneaGrab.Services;

public class DatabaseContext : DbContext
{
    public DbSet<Record> Records { get; set; } = null!;
    public DbSet<Person> Persons { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={LocalData.AppData}/data.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Record>()
            .Property(b => b.Position)
            .HasConversion(
                rect => rect == null ? null : rect.ToString(),
                rectStr => rectStr == null ? null : Rect.Parse(rectStr));
    }
}
