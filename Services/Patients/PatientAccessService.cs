using Microsoft.EntityFrameworkCore;
using RAQEEB.Common;
using RAQEEB.Data;
using RAQEEB.Entities;

namespace RAQEEB.Services.Patients
{
    public class PatientAccessService : IPatientAccessService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUser _currentUser;

        public PatientAccessService(
            ApplicationDbContext context,
            ICurrentUser currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public IQueryable<Patient> GetAccessiblePatients()
        {
            var hospitalId = _currentUser.HospitalId;
            var userId = _currentUser.UserId;
            var role = _currentUser.Role;

            // Missing claims: return an empty result (the caller will answer 404).
            if (hospitalId == null || userId == null)
            {
                return _context.Patients
                    .AsNoTracking()
                    .Where(p => false);
            }

            // Every user is limited to their own hospital.
            var query = _context.Patients
                .AsNoTracking()
                .Where(p => p.HospitalId == hospitalId.Value);

            // Admin: all patients in the hospital.
            if (role == "Admin")
            {
                return query;
            }

            // Patient: only their own record.
            if (role == "Patient")
            {
                return query.Where(p => p.UserId == userId);
            }

            // Doctor / Nurse / Caregiver: only patients with an active assignment.
            return query.Where(p =>
                _context.PatientAssignments.Any(a =>
                    a.PatientId == p.Id &&
                    a.UserId == userId &&
                    a.IsActive));
        }
    }
}
