using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RAQEEB.Entities;
using RAQEEB.Services.Diagnostics;

namespace RAQEEB.Data
{
    // Development-only demo data (FAKE, not real patients).
    // Called from Program.cs only when ASPNETCORE_ENVIRONMENT = Development.
    // Idempotent: every record has a fixed Id and is inserted only if missing;
    // existing records are never updated or overwritten.
    public static partial class DbSeeder
    {
        // Demo accounts (UserName / role)
        private const string DemoDoctorUserId = "5d1c0a2e-0001-4c6b-9a10-000000000001";
        private const string DemoNurseUserId = "5d1c0a2e-0002-4c6b-9a10-000000000002";
        private const string DemoCaregiverUserId = "5d1c0a2e-0003-4c6b-9a10-000000000003";
        private const string DemoPatientUserId = "5d1c0a2e-0004-4c6b-9a10-000000000004";
        private const string DemoPatient2UserId = "5d1c0a2e-0005-4c6b-9a10-000000000005";

        // Demo patient profiles
        public static readonly Guid DemoPatientId =
            Guid.Parse("3f6c2b1a-0001-4d1e-8a00-000000000001");

        // Second patient: no assignments, no labs, no imaging (for negative tests).
        public static readonly Guid DemoPatient2Id =
            Guid.Parse("3f6c2b1a-0002-4d1e-8a00-000000000002");

        private const string PdfContentType = "application/pdf";

        public static async Task SeedDevelopmentDataAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IDiagnosticFileStorage fileStorage,
            string demoPassword)
        {
            // 1. Demo hospital = the seeded admin's hospital (falls back to the first hospital).
            var admin = await userManager.FindByEmailAsync("admin@raqeeb.com");

            var hospitalId = admin?.HospitalId
                ?? await context.Hospitals
                    .Select(h => (Guid?)h.Id)
                    .FirstOrDefaultAsync();

            if (hospitalId == null)
            {
                return;
            }

            // 2. Demo accounts (reused if the username or email already exists).
            var doctor = await EnsureDemoUserAsync(userManager, hospitalId.Value,
                DemoDoctorUserId, "demo.doctor", "demo.doctor@raqeeb.demo",
                "Dr. Demo Doctor", "Doctor", demoPassword);

            var nurse = await EnsureDemoUserAsync(userManager, hospitalId.Value,
                DemoNurseUserId, "demo.nurse", "demo.nurse@raqeeb.demo",
                "Demo Nurse", "Nurse", demoPassword);

            var caregiver = await EnsureDemoUserAsync(userManager, hospitalId.Value,
                DemoCaregiverUserId, "demo.caregiver", "demo.caregiver@raqeeb.demo",
                "Demo Caregiver", "Caregiver", demoPassword);

            var patientUser = await EnsureDemoUserAsync(userManager, hospitalId.Value,
                DemoPatientUserId, "demo.patient", "demo.patient@raqeeb.demo",
                "Demo Patient", "Patient", demoPassword);

            var patient2User = await EnsureDemoUserAsync(userManager, hospitalId.Value,
                DemoPatient2UserId, "demo.patient2", "demo.patient2@raqeeb.demo",
                "Demo Patient Two", "Patient", demoPassword);

            // 3. Demo patient profiles (step 2 of the two-step registration).
            var patientId = await EnsureDemoPatientAsync(context, patientUser,
                hospitalId.Value, DemoPatientId, "DEMO-0001", new Patient
                {
                    DateOfBirth = new DateTime(1978, 4, 15),
                    Gender = "Female",
                    BloodGroup = "A+",
                    Diagnosis = "Breast Cancer (Invasive Ductal Carcinoma)",
                    Stage = "IIB",
                    Treatment = "Chemotherapy",
                    Protocol = "AC-T",
                    RiskLevel = "Medium"
                });

            await EnsureDemoPatientAsync(context, patient2User,
                hospitalId.Value, DemoPatient2Id, "DEMO-0002", new Patient
                {
                    DateOfBirth = new DateTime(1965, 9, 2),
                    Gender = "Male",
                    BloodGroup = "O+",
                    Diagnosis = "Colorectal Cancer",
                    Stage = "III",
                    Treatment = "Chemotherapy",
                    Protocol = "FOLFOX",
                    RiskLevel = "Low"
                });

            if (patientId == null)
            {
                return;
            }

            // 4. Active staff assignments for the first demo patient only.
            await EnsureDemoAssignmentAsync(context, patientId.Value, doctor,
                Guid.Parse("8b2e4d10-0001-4f3a-b100-000000000001"));
            await EnsureDemoAssignmentAsync(context, patientId.Value, nurse,
                Guid.Parse("8b2e4d10-0002-4f3a-b100-000000000002"));
            await EnsureDemoAssignmentAsync(context, patientId.Value, caregiver,
                Guid.Parse("8b2e4d10-0003-4f3a-b100-000000000003"));

            await context.SaveChangesAsync();

            // 5. Labs, imaging and their PDF reports.
            await SeedDemoDiagnosticsAsync(context, fileStorage, patientId.Value);
        }

        private static async Task<ApplicationUser?> EnsureDemoUserAsync(
            UserManager<ApplicationUser> userManager,
            Guid hospitalId,
            string id,
            string userName,
            string email,
            string fullName,
            string role,
            string password)
        {
            var user = await userManager.FindByNameAsync(userName)
                ?? await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    Id = id,
                    UserName = userName,
                    Email = email,
                    FullName = fullName,
                    HospitalId = hospitalId,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description));

                    throw new Exception(
                        $"Failed to create demo user '{userName}': {errors}");
                }
            }

            // Never touch an account that belongs to another hospital.
            if (user.HospitalId != hospitalId)
            {
                return null;
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }

            return user;
        }

        private static async Task<Guid?> EnsureDemoPatientAsync(
            ApplicationDbContext context,
            ApplicationUser? user,
            Guid hospitalId,
            Guid patientId,
            string medicalRecordNumber,
            Patient details)
        {
            if (user == null)
            {
                return null;
            }

            // Reuse the existing profile of this account, if any.
            var existingId = await context.Patients
                .Where(p => p.UserId == user.Id)
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync();

            if (existingId != null)
            {
                return existingId;
            }

            if (await context.Patients.AnyAsync(p => p.Id == patientId))
            {
                return null;
            }

            var mrnTaken = await context.Patients.AnyAsync(p =>
                p.HospitalId == hospitalId &&
                p.MedicalRecordNumber == medicalRecordNumber);

            details.Id = patientId;
            details.UserId = user.Id;
            details.HospitalId = hospitalId;
            details.MedicalRecordNumber = mrnTaken ? null : medicalRecordNumber;
            details.IsActive = true;
            details.CreatedAt = DateTime.UtcNow;

            context.Patients.Add(details);

            await context.SaveChangesAsync();

            return patientId;
        }

        private static async Task EnsureDemoAssignmentAsync(
            ApplicationDbContext context,
            Guid patientId,
            ApplicationUser? staff,
            Guid assignmentId)
        {
            if (staff == null)
            {
                return;
            }

            // Any existing row (active or deactivated on purpose) is left as is.
            var exists = await context.PatientAssignments.AnyAsync(a =>
                a.Id == assignmentId ||
                (a.PatientId == patientId && a.UserId == staff.Id));

            if (exists)
            {
                return;
            }

            context.PatientAssignments.Add(new PatientAssignment
            {
                Id = assignmentId,
                PatientId = patientId,
                UserId = staff.Id,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        private record DemoLab(
            Guid Id, Guid FileId, string TestName, string ResultValue,
            string Unit, string Flag, string ReferenceRange, DateTime RecordedAt);

        private record DemoImaging(
            Guid Id, Guid FileId, string StudyName, DateTime StudyDate,
            string Finding, string Impression);

        private static async Task SeedDemoDiagnosticsAsync(
            ApplicationDbContext context,
            IDiagnosticFileStorage fileStorage,
            Guid patientId)
        {
            var labs = new List<DemoLab>
            {
                new(G("c1a50001"), G("f1a50001"), "Hemoglobin", "10.8", "g/dL", "Low", "12.0 - 15.5", D(2026, 11, 18, 8, 30)),
                new(G("c1a50002"), G("f1a50002"), "Platelets", "165", "x10^3/uL", "Normal", "150 - 400", D(2026, 11, 18, 8, 30)),
                new(G("c1a50003"), G("f1a50003"), "WBC", "3.1", "x10^3/uL", "Low", "4.0 - 11.0", D(2026, 11, 18, 8, 30)),
                new(G("c1a50004"), G("f1a50004"), "RBC", "4.2", "x10^6/uL", "Normal", "3.9 - 5.0", D(2026, 11, 18, 8, 30)),
                new(G("c1a50005"), G("f1a50005"), "Creatinine", "0.9", "mg/dL", "Normal", "0.6 - 1.1", D(2026, 11, 15, 9, 0)),
                new(G("c1a50006"), G("f1a50006"), "ALT", "68", "U/L", "High", "7 - 35", D(2026, 11, 15, 9, 0)),
                new(G("c1a50007"), G("f1a50007"), "AST", "52", "U/L", "High", "10 - 40", D(2026, 11, 15, 9, 0)),
                new(G("c1a50008"), G("f1a50008"), "Glucose", "132", "mg/dL", "High", "70 - 99 (fasting)", D(2026, 11, 12, 7, 45)),
                new(G("c1a50009"), G("f1a50009"), "Albumin", "3.8", "g/dL", "Normal", "3.5 - 5.0", D(2026, 11, 12, 7, 45)),
                new(G("c1a5000a"), G("f1a5000a"), "Bilirubin", "0.7", "mg/dL", "Normal", "0.1 - 1.2", D(2026, 11, 12, 7, 45))
            };

            var studies = new List<DemoImaging>
            {
                new(G("c2b60001"), G("f2b60001"), "Chest CT", D(2026, 10, 5, 10, 0),
                    "No evidence of pulmonary metastases. A 4 mm nodule in the right lower lobe is likely benign.",
                    "No metastatic disease in the chest. Follow-up CT in 6 months for the small nodule."),
                new(G("c2b60002"), G("f2b60002"), "Breast MRI", D(2026, 10, 12, 11, 30),
                    "2.1 cm enhancing mass in the left breast, upper outer quadrant (previously 2.8 cm).",
                    "Partial response to neoadjuvant chemotherapy."),
                new(G("c2b60003"), G("f2b60003"), "PET Scan", D(2026, 10, 20, 9, 15),
                    "Mild FDG uptake in the known left breast lesion (SUVmax 3.2). No distant hypermetabolic foci.",
                    "No evidence of distant metastatic disease."),
                new(G("c2b60004"), G("f2b60004"), "Abdominal Ultrasound", D(2026, 11, 2, 13, 0),
                    "Normal liver size and echotexture. No focal hepatic lesions. Gallbladder and kidneys unremarkable.",
                    "Normal abdominal ultrasound."),
                new(G("c2b60005"), G("f2b60005"), "Brain MRI", D(2026, 11, 10, 15, 45),
                    "No enhancing intracranial lesions. No midline shift or hydrocephalus.",
                    "No intracranial metastatic disease.")
            };

            foreach (var lab in labs)
            {
                var date = lab.RecordedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var fileName = $"{Slug(lab.TestName)}_{date}.pdf";

                var pdf = SimplePdfGenerator.Create(
                    "RAQEEB Demo Hospital - Laboratory Report",
                    DemoHeader(patientId).Concat(new[]
                    {
                        $"Test:             {lab.TestName}",
                        $"Result:           {lab.ResultValue} {lab.Unit}",
                        $"Reference range:  {lab.ReferenceRange} {lab.Unit}",
                        $"Flag:             {lab.Flag}",
                        $"Collected at:     {lab.RecordedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC",
                        "",
                        "Verified by: Demo Laboratory (fake)"
                    }));

                await EnsureDemoFileAsync(context, fileStorage, patientId,
                    lab.FileId, fileName, pdf);

                if (!await context.LabResults.AnyAsync(l => l.Id == lab.Id))
                {
                    context.LabResults.Add(new LabResult
                    {
                        Id = lab.Id,
                        PatientId = patientId,
                        TestName = lab.TestName,
                        ResultValue = lab.ResultValue,
                        Unit = lab.Unit,
                        Flag = lab.Flag,
                        RecordedAt = lab.RecordedAt,
                        DiagnosticFileId = lab.FileId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            foreach (var study in studies)
            {
                var date = study.StudyDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var fileName = $"{Slug(study.StudyName)}_{date}.pdf";

                var pdf = SimplePdfGenerator.Create(
                    "RAQEEB Demo Hospital - Imaging Report",
                    DemoHeader(patientId).Concat(new[]
                    {
                        $"Study:       {study.StudyName}",
                        $"Study date:  {study.StudyDate.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC",
                        "",
                        "Findings:",
                        study.Finding,
                        "",
                        "Impression:",
                        study.Impression,
                        "",
                        "Reported by: Demo Radiology (fake)"
                    }));

                await EnsureDemoFileAsync(context, fileStorage, patientId,
                    study.FileId, fileName, pdf);

                if (!await context.ImagingRecords.AnyAsync(i => i.Id == study.Id))
                {
                    context.ImagingRecords.Add(new Imaging
                    {
                        Id = study.Id,
                        PatientId = patientId,
                        StudyName = study.StudyName,
                        StudyDate = study.StudyDate,
                        Finding = study.Finding,
                        DiagnosticFileId = study.FileId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task EnsureDemoFileAsync(
            ApplicationDbContext context,
            IDiagnosticFileStorage fileStorage,
            Guid patientId,
            Guid fileId,
            string fileName,
            byte[] pdf)
        {
            var existing = await context.DiagnosticFiles
                .AsNoTracking()
                .Where(f => f.Id == fileId)
                .Select(f => new { f.PatientId, f.StoredFileName })
                .FirstOrDefaultAsync();

            if (existing != null && existing.PatientId != patientId)
            {
                return;
            }

            var storedFileName = existing?.StoredFileName
                ?? $"{patientId}/{fileId}.pdf";

            // (Re)create the PDF on disk if it is missing on this machine.
            var fullPath = fileStorage.GetFullPath(storedFileName);

            if (fullPath != null && !File.Exists(fullPath))
            {
                await fileStorage.SaveAsync(storedFileName, pdf);
            }

            if (existing == null)
            {
                context.DiagnosticFiles.Add(new DiagnosticFile
                {
                    Id = fileId,
                    PatientId = patientId,
                    FileName = fileName,
                    ContentType = PdfContentType,
                    StoredFileName = storedFileName,
                    FileSizeBytes = pdf.LongLength,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        private static IEnumerable<string> DemoHeader(Guid patientId)
        {
            return new[]
            {
                "*** FAKE DEMONSTRATION DATA - NOT A REAL PATIENT ***",
                "",
                "Patient:   Demo Patient (MRN DEMO-0001)",
                $"PatientId: {patientId}",
                ""
            };
        }

        private static Guid G(string prefix) =>
            Guid.Parse($"{prefix}-0000-4000-8000-000000000000");

        private static DateTime D(int year, int month, int day, int hour, int minute) =>
            new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

        private static string Slug(string name) =>
            name.ToLowerInvariant().Replace(' ', '_');
    }
}
