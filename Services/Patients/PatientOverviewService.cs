using Microsoft.EntityFrameworkCore;
using RAQEEB.Data;
using RAQEEB.DTOs.PatientDetails;

namespace RAQEEB.Services.Patients
{
    public class PatientOverviewService : IPatientOverviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPatientAccessService _patientAccess;

        public PatientOverviewService(
            ApplicationDbContext context,
            IPatientAccessService patientAccess)
        {
            _context = context;
            _patientAccess = patientAccess;
        }

        public async Task<PatientOverviewResponseDto?> GetOverviewAsync(
            Guid patientId)
        {
            // 1. Find the patient (only if the current user can access it).
            var patient = await _patientAccess.GetAccessiblePatients()
                .Where(p => p.Id == patientId)
                .Select(p => new
                {
                    p.Id,
                    p.HospitalId,
                    p.MedicalRecordNumber,
                    UserName = p.User.UserName,
                    FullName = p.User.FullName,
                    Phone = p.User.PhoneNumber,
                    p.DateOfBirth,
                    p.Gender,
                    p.BloodGroup,
                    p.RiskLevel,
                    p.Diagnosis,
                    p.Stage,
                    p.Protocol
                })
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return null;
            }

            // 2. Get the active Doctors and Caregivers assigned to this patient.
            //    The earliest assignment wins (treating doctor / primary caregiver).
            var staff = await (
                from a in _context.PatientAssignments.AsNoTracking()
                join u in _context.Users.AsNoTracking()
                    on a.UserId equals u.Id
                join ur in _context.UserRoles.AsNoTracking()
                    on u.Id equals ur.UserId
                join r in _context.Roles.AsNoTracking()
                    on ur.RoleId equals r.Id
                where a.PatientId == patientId
                    && a.IsActive
                    && u.IsActive
                    && u.HospitalId == patient.HospitalId
                    && (r.Name == "Doctor" || r.Name == "Caregiver")
                orderby a.AssignedAt
                select new
                {
                    UserId = u.Id,
                    u.UserName,
                    u.FullName,
                    RoleName = r.Name
                }
            ).ToListAsync();

            var doctor = staff.FirstOrDefault(s => s.RoleName == "Doctor");
            var caregiver = staff.FirstOrDefault(s => s.RoleName == "Caregiver");

            // 3. Build the response.
            return new PatientOverviewResponseDto
            {
                PatientHeader = new PatientHeaderDto
                {
                    PatientId = patient.Id,
                    PatientCode = patient.MedicalRecordNumber,
                    FullName = GetDisplayName(patient.FullName, patient.UserName),
                    Age = CalculateAge(patient.DateOfBirth),
                    Gender = patient.Gender,
                    BloodGroup = patient.BloodGroup,
                    RiskLevel = patient.RiskLevel
                },

                PersonalInfo = new PatientPersonalInfoDto
                {
                    Phone = patient.Phone,
                    PrimaryCaregiver = caregiver == null
                        ? null
                        : GetDisplayName(caregiver.FullName, caregiver.UserName)
                },

                ClinicalOverview = new PatientClinicalOverviewDto
                {
                    CancerType = patient.Diagnosis,
                    Stage = patient.Stage,
                    Protocol = patient.Protocol,

                    TreatingDoctor = doctor == null
                        ? null
                        : new TreatingDoctorDto
                        {
                            DoctorId = doctor.UserId,
                            Name = GetDisplayName(doctor.FullName, doctor.UserName)
                        }

                    // TreatmentStatus, CurrentCycle, TotalCycles and CurrentDay
                    // stay null until the Chemotherapy module (Module 2).
                }
            };
        }

        // Use the full name when we have it, otherwise fall back to the username.
        private static string GetDisplayName(string? fullName, string? userName)
        {
            return !string.IsNullOrWhiteSpace(fullName)
                ? fullName
                : userName ?? string.Empty;
        }

        private static int? CalculateAge(DateTime? dateOfBirth)
        {
            if (dateOfBirth == null)
            {
                return null;
            }

            var today = DateTime.UtcNow.Date;
            var birth = dateOfBirth.Value.Date;

            var age = today.Year - birth.Year;

            // Birthday has not happened yet this year.
            if (birth > today.AddYears(-age))
            {
                age--;
            }

            return age < 0 ? null : age;
        }
    }
}
