using Microsoft.EntityFrameworkCore;

namespace SanatorioHMS.Infrastructure.Data;

/// <summary>
/// Shared foundation context. Bounded-context entities are added by their
/// respective infrastructure configurations in later work units.
/// </summary>
public sealed class HmsDbContext(DbContextOptions<HmsDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("patient");

        modelBuilder.Entity<MigrationMarker>(entity =>
        {
            entity.ToTable("MigrationMarkers", "patient");
            entity.HasKey(marker => marker.Id);
            entity.Property(marker => marker.Name).HasMaxLength(200).IsRequired();
        });
    }
}

public sealed class MigrationMarker
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
