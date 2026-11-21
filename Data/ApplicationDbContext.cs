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

        public DbSet<PatientMedicalHistoryEntry> PatientMedicalHistoryEntries
        => Set<PatientMedicalHistoryEntry>();

        public DbSet<PatientJournalEntry> PatientJournalEntries
        => Set<PatientJournalEntry>();

        public DbSet<PatientAppointment> PatientAppointments
        => Set<PatientAppointment>();

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

                entity.Property(u => u.FullName)
                    .HasMaxLength(200);
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

                entity.Property(p => p.BloodGroup)
                    .HasMaxLength(10);

                entity.Property(p => p.MedicalRecordNumber)
                    .HasMaxLength(100);

                entity.Property(p => p.Diagnosis)
                    .HasMaxLength(200);

                entity.Property(p => p.Stage)
                    .HasMaxLength(100);

                entity.Property(p => p.Treatment)
                    .HasMaxLength(200);

                entity.Property(p => p.Protocol)
                    .HasMaxLength(200);

                entity.Property(p => p.RiskLevel)
                    .HasMaxLength(50);

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

            builder.Entity<PatientMedicalHistoryEntry>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Type)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Notes)
                    .HasMaxLength(1000);

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Patient)
                    .WithMany()
                    .HasForeignKey(e => e.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.PatientId, e.Type });
            });

            builder.Entity<PatientJournalEntry>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.EntryType)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                entity.Property(e => e.Content)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Patient)
                    .WithMany()
                    .HasForeignKey(e => e.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Author)
                    .WithMany()
                    .HasForeignKey(e => e.AuthorUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.PatientId, e.CreatedAt });
            });

            builder.Entity<PatientAppointment>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(a => a.Location)
                    .HasMaxLength(300);

                entity.Property(a => a.IsActive)
                    .HasDefaultValue(true);

                entity.Property(a => a.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(a => a.Patient)
                    .WithMany()
                    .HasForeignKey(a => a.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Provider)
                    .WithMany()
                    .HasForeignKey(a => a.ProviderUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.CreatedBy)
                    .WithMany()
                    .HasForeignKey(a => a.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(a => new
                {
                    a.PatientId,
                    a.AppointmentDate,
                    a.AppointmentTime
                });
            });
        }
    }
}