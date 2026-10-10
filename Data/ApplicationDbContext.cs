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

        public DbSet<Patient> Patients => Set<Patient>();

        public DbSet<PatientAssignment> PatientAssignments
        => Set<PatientAssignment>();

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

            builder.Entity<Patient>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.HasIndex(p => p.UserId)
                    .IsUnique();

                entity.HasIndex(p => new
                {
                    p.HospitalId,
                    p.MedicalRecordNumber
                })
                .IsUnique()
                .HasFilter("[MedicalRecordNumber] IS NOT NULL"); //Within the same hospital,
                                                                 //two patient records cannot have
                                                                 //the same non-null medical record number.

                entity.Property(p => p.Gender)
                    .HasMaxLength(30);

                entity.Property(p => p.MedicalRecordNumber)
                    .HasMaxLength(100);

                entity.Property(p => p.IsActive)
                    .HasDefaultValue(true);

                entity.Property(p => p.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(p => p.User)
                    .WithOne()
                    .HasForeignKey<Patient>(p => p.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Hospital)
                    .WithMany()
                    .HasForeignKey(p => p.HospitalId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PatientAssignment>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.HasOne(a => a.Patient)
                    .WithMany()
                    .HasForeignKey(a => a.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.User)
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(a => a.AssignedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(a => a.IsActive)
                    .HasDefaultValue(true);

                entity.HasIndex(a => new
                {
                    a.PatientId,
                    a.UserId
                })
                .IsUnique()
                .HasFilter("[IsActive] = 1");
            });
        }
    }
}