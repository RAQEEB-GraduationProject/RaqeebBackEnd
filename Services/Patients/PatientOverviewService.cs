using Microsoft.EntityFrameworkCore;
using RAQEEB.Data;
using RAQEEB.DTOs.PatientDetails;
using RAQEEB.DTOs.Patients;

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
            // 1. Find the patient (only if the current user can access it),
            //    using the SAME mapping as GET /api/Patients and GET /api/Patients/{id}.
            var patient = await _patientAccess.GetAccessiblePatients()
                .Where(p => p.Id == patientId)
                .Select(PatientResponseDto.Projection)
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return null;
            }

            // 2. Get the active Doctors, Nurses and Caregivers assigned to this patient.
            //    The earliest assignment of each role wins.
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
                    && (r.Name == "Doctor" || r.Name == "Nurse" || r.Name == "Caregiver")
                orderby a.AssignedAt
                select new
                {
                    UserId = u.Id,
                    u.UserName,
                    u.FullName,
                    u.PhoneNumber,
                    RoleName = r.Name,
                    a.AssignedAt
                }
            ).ToListAsync();

            var doctor = staff.FirstOrDefault(s => s.RoleName == "Doctor");
            var nurse = staff.FirstOrDefault(s => s.RoleName == "Nurse");
            var caregiver = staff.FirstOrDefault(s => s.RoleName == "Caregiver");

            // 3. Build the response.
            return new PatientOverviewResponseDto
            {
                // ----- NEW structure -----
                Patient = patient,
                CareTeam = new PatientCareTeamDto
                {
                    TreatingDoctor = doctor == null ? null : Member(doctor.UserId, doctor.UserName, doctor.FullName, doctor.PhoneNumber, doctor.RoleName, doctor.AssignedAt),
                    Nurse = nurse == null ? null : Member(nurse.UserId, nurse.UserName, nurse.FullName, nurse.PhoneNumber, nurse.RoleName, nurse.AssignedAt),
                    PrimaryCaregiver = caregiver == null ? null : Member(caregiver.UserId, caregiver.UserName, caregiver.FullName, caregiver.PhoneNumber, caregiver.RoleName, caregiver.AssignedAt)
                },
                // No treatment cycles in the database yet.
                TreatmentProgress = null,

                // ----- OLD structure (deprecated, unchanged values) -----
                PatientHeader = new PatientHeaderDto
                {
                    PatientId = patient.Id,
                    PatientCode = patient.MedicalRecordNumber,
                    FullName = GetDisplayName(patient.FullName, patient.UserName),
                    Age = patient.Age,
                    Gender = patient.Gender,
                    BloodGroup = patient.BloodGroup,
                    RiskLevel = patient.RiskLevel
                },
                PersonalInfo = new PatientPersonalInfoDto
                {
                    Phone = patient.PhoneNumber,
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

        private static CareTeamMemberDto Member(
            string userId,
            string? userName,
            string? fullName,
            string? phoneNumber,
            string? role,
            DateTime assignedAt)
        {
            return new CareTeamMemberDto
            {
                UserId = userId,
                UserName = userName ?? string.Empty,
                FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName,
                PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber,
                Role = role ?? string.Empty,
                AssignedAt = assignedAt
            };
        }

        // Use the full name when we have it, otherwise fall back to the username.
        private static string GetDisplayName(string? fullName, string? userName)
        {
            return !string.IsNullOrWhiteSpace(fullName)
                ? fullName
                : userName ?? string.Empty;
        }
    }
}
