using GolBet.Entities;
using GolBet.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace GolBet.Repositories.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        //Section DBSets
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<Match> Matches => Set<Match>();
        public DbSet<Bet> Bets => Set<Bet>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Team names must be unique (case- and accent-insensitive)
            modelBuilder.Entity<Team>()
                .Property(t => t.Name)
                .UseCollation("SQL_Latin1_General_CP1_CI_AI"); //Esta parte me indica el Sensitive Case

            modelBuilder.Entity<Team>()
                .HasIndex(t => t.Name)
                .IsUnique();

            // Double relationship Match -> Team: convention cannot resolve it
            modelBuilder.Entity<Match>()
                .HasOne(m => m.HomeTeam)
                .WithMany()
                .HasForeignKey(m => m.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.AwayTeam)
                .WithMany()
                .HasForeignKey(m => m.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // A match with bets cannot be deleted
            modelBuilder.Entity<Bet>()
                .HasOne(b => b.Match)
                .WithMany(m => m.Bets)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void StampAudit()
        {
            var utcNow = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedDate = utcNow;
                    entry.Entity.ModifiedDate = null;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.ModifiedDate = utcNow;
                    entry.Property(e => e.CreatedDate).IsModified = false;
                }
            }
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            StampAudit();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            StampAudit();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
