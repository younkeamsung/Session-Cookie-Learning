using Microsoft.EntityFrameworkCore;
using MyAuth.Mvc.Entities;
using MyAuth.Mvc.Models;

namespace MyAuth.Mvc.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<DatabaseProbe> DatabaseProbes => Set<DatabaseProbe>();
        public DbSet<AppUser> Users => Set<AppUser>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DatabaseProbe>(entity =>
            {
                entity.ToTable("DatabaseProbes");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Message)
                    .HasMaxLength(200)
                    .IsRequired();
            });
        }
    }
}
