using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RAQEEB.Entities;

namespace RAQEEB.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Hospital> Hospitals => Set<Hospital>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Hospital>(entity =>
            {
                entity.HasKey(h => h.Id);

                entity.Property(h => h.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(h => h.Address)
                    .HasMaxLength(500);

                entity.Property(h => h.IsActive)
                    .HasDefaultValue(true);

                entity.Property(h => h.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne(u => u.Hospital)
                    .WithMany(h => h.Users)
                    .HasForeignKey(u => u.HospitalId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(u => u.IsActive)
                    .HasDefaultValue(true);

                entity.Property(u => u.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}