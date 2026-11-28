using System.Linq.Expressions;
using RAQEEB.Common;
using RAQEEB.Entities;

namespace RAQEEB.DTOs.Patients
{
    // The ONE patient shape (canonical patient record) used by:
    //   GET  /api/Patients            GET /api/Patients/{id}
    //   POST /api/Patients (response) PUT /api/Patients/{id} (response)
    //   GET  /api/Patients/{id}/overview  -> "patient" property
    //
    // Account fields come from the linked user (ApplicationUser) and are returned
    // as stored: no field replaces another. Empty text is returned as null.
    // Display name rule is the frontend's job: fullName -> userName -> email.
    public class PatientResponseDto
    {
        // ---------- Account information (from the user) ----------

        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        // Real name as stored (null if not set). No fallback to userName.
        public string? FullName { get; set; }

        public string? Email { get; set; }

        // Stored only on the user, never copied into Patient.
        public string? PhoneNumber { get; set; }

        // ---------- Patient profile ----------

        public Guid Id { get; set; }

        public Guid HospitalId { get; set; }

        public DateTime? DateOfBirth { get; set; }

        // Calculated from dateOfBirth (Cairo date). Not stored in the database.
        public int? Age => CairoClock.CalculateAge(DateOfBirth);

        public string? Gender { get; set; }

        public string? BloodGroup { get; set; }

        // Null until a real number is registered (no auto-generated numbers).
        public string? MedicalRecordNumber { get; set; }

        public string? Diagnosis { get; set; }

        public string? Stage { get; set; }

        public string? Treatment { get; set; }

        public string? Protocol { get; set; }

        public string? RiskLevel { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        // One shared mapping used by every patient query (EF Core translates it to SQL),
        // so a new field only has to be added here once.
        public static readonly Expression<Func<Patient, PatientResponseDto>> Projection =
            p => new PatientResponseDto
            {
                UserId = p.UserId,
                UserName = p.User.UserName ?? string.Empty,
                FullName = p.User.FullName == null || p.User.FullName.Trim() == ""
                    ? null
                    : p.User.FullName,
                Email = p.User.Email == null || p.User.Email.Trim() == ""
                    ? null
                    : p.User.Email,
                PhoneNumber = p.User.PhoneNumber == null || p.User.PhoneNumber.Trim() == ""
                    ? null
                    : p.User.PhoneNumber,
                Id = p.Id,
                HospitalId = p.HospitalId,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender,
                BloodGroup = p.BloodGroup,
                MedicalRecordNumber = p.MedicalRecordNumber,
                Diagnosis = p.Diagnosis,
                Stage = p.Stage,
                Treatment = p.Treatment,
                Protocol = p.Protocol,
                RiskLevel = p.RiskLevel,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            };
    }
}
