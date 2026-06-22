using Microsoft.EntityFrameworkCore;

namespace TradingAssistant
{
    public class TradingContext : DbContext
    {
        private readonly string _dbPath;

        public DbSet<OpenPosition> OpenPositions { get; set; }

        public TradingContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);

            _dbPath = Path.Join(path, "trading.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={_dbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OpenPosition>().HasIndex(nameof(OpenPosition.Symbol));

            base.OnModelCreating(modelBuilder);
        }
    }
}
