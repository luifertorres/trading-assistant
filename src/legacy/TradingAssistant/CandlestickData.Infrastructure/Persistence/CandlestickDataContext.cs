using CandlestickData.Domain;
using Microsoft.EntityFrameworkCore;

namespace CandlestickData.Infrastructure.Persistence;

public sealed class CandlestickDataContext(DbContextOptions<CandlestickDataContext> options)
    : DbContext(options)
{
    public DbSet<CandlestickRecord> Candlesticks => Set<CandlestickRecord>();
    public DbSet<SyncCheckpoint> SyncCheckpoints => Set<SyncCheckpoint>();
    public DbSet<SymbolIntegrity> SymbolIntegrities => Set<SymbolIntegrity>();
    public DbSet<SyncJob> SyncJobs => Set<SyncJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CandlestickRecord>(entity =>
        {
            entity.HasKey(c => new { c.Symbol, c.TimeFrame, c.OpenTime });
            entity.HasIndex(c => new { c.Symbol, c.TimeFrame, c.OpenTime });

            entity.Property(c => c.Symbol).HasMaxLength(20).IsRequired();
            entity.Property(c => c.TimeFrame).HasConversion<string>().HasMaxLength(20);
            entity.Property(c => c.OpenPrice).HasPrecision(18, 8);
            entity.Property(c => c.HighPrice).HasPrecision(18, 8);
            entity.Property(c => c.LowPrice).HasPrecision(18, 8);
            entity.Property(c => c.ClosePrice).HasPrecision(18, 8);
            entity.Property(c => c.Volume).HasPrecision(18, 8);
        });

        modelBuilder.Entity<SyncCheckpoint>(entity =>
        {
            entity.HasKey(s => new { s.Symbol, s.TimeFrame });

            entity.Property(s => s.Symbol).HasMaxLength(20).IsRequired();
            entity.Property(s => s.TimeFrame).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<SymbolIntegrity>(entity =>
        {
            entity.HasKey(i => new { i.Symbol, i.TimeFrame });

            entity.Property(i => i.Symbol).HasMaxLength(20).IsRequired();
            entity.Property(i => i.TimeFrame).HasConversion<string>().HasMaxLength(20);
            entity.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(i => i.Reason).HasConversion<string>().HasMaxLength(30);
        });

        modelBuilder.Entity<SyncJob>(entity =>
        {
            entity.HasKey(j => j.Id);

            entity.Property(j => j.State).HasConversion<string>().HasMaxLength(20);
            entity.Property(j => j.FailureReason).HasMaxLength(500);
        });
    }
}
