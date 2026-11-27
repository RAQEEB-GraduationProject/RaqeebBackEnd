using System.Linq.Expressions;
using RAQEEB.Entities;

namespace RAQEEB.DTOs.Patients
{
    public class PatientResponseDto
    {
        public Guid Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        // Full name of the patient's account (falls back to the username when empty).
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public Guid HospitalId { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? MedicalRecordNumber { get; set; }

        public string? Diagnosis { get; set; }

        public string? Stage { get; set; }

        public string? Treatment { get; set; }

        public string? Protocol { get; set; }

        public string? RiskLevel { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? BloodGroup { get; set; }

        // One shared mapping used by every patient query (EF Core translates it to SQL),
        // so a new field only has to be added here once.
        public static readonly Expression<Func<Patient, PatientResponseDto>> Projection =
            p => new PatientResponseDto
            {
                Id = p.Id,
                UserId = p.UserId,
                UserName = p.User.UserName ?? string.Empty,
                FullName = p.User.FullName ?? p.User.UserName ?? string.Empty,
                Email = p.User.Email ?? string.Empty,
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
