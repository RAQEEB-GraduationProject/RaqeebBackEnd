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

        public DbSet<DiagnosticFile> DiagnosticFiles => Set<DiagnosticFile>();

        public DbSet<LabResult> LabResults => Set<LabResult>();

        public DbSet<Imaging> ImagingRecords => Set<Imaging>();

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

            builder.Entity<DiagnosticFile>(entity =>
            {
                entity.HasKey(f => f.Id);

                entity.Property(f => f.FileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(f => f.ContentType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(f => f.StoredFileName)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(f => f.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(f => f.Patient)
                    .WithMany()
                    .HasForeignKey(f => f.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(f => f.PatientId);
            });

            builder.Entity<LabResult>(entity =>
            {
                entity.HasKey(l => l.Id);

                entity.Property(l => l.TestName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(l => l.ResultValue)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(l => l.Unit)
                    .HasMaxLength(50);

                entity.Property(l => l.Flag)
                    .HasMaxLength(20);

                entity.Property(l => l.IsActive)
                    .HasDefaultValue(true);

                entity.Property(l => l.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(l => l.Patient)
                    .WithMany()
                    .HasForeignKey(l => l.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.DiagnosticFile)
                    .WithMany()
                    .HasForeignKey(l => l.DiagnosticFileId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(l => new { l.PatientId, l.RecordedAt });
            });

            builder.Entity<Imaging>(entity =>
            {
                entity.ToTable("ImagingRecords");

                entity.HasKey(i => i.Id);

                entity.Property(i => i.StudyName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(i => i.Finding)
                    .HasMaxLength(2000);

                entity.Property(i => i.IsActive)
                    .HasDefaultValue(true);

                entity.Property(i => i.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(i => i.Patient)
                    .WithMany()
                    .HasForeignKey(i => i.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.DiagnosticFile)
                    .WithMany()
                    .HasForeignKey(i => i.DiagnosticFileId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(i => new { i.PatientId, i.StudyDate });
            });
        }
    }
}