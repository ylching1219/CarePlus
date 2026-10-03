using CarePlus.API.Models.Common;
using CarePlus.API.Models.User;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace CarePlus.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditTrail();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateAuditTrail();
            return base.SaveChanges();
        }

        private void UpdateAuditTrail()
        {
            var entries = ChangeTracker.Entries<AuditTrail>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreateDate = DateTime.UtcNow;
                    entry.Entity.CreatedBy = "System"; // Replace with actual user
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdateDate = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = "System"; // Replace with actual user
                }
                else if (entry.State == EntityState.Deleted)
                {
                    entry.Entity.DeleteDate = DateTime.UtcNow;
                    entry.Entity.DeletedBy = "System"; // Replace with actual user
                }
            }
        }
    }
}
