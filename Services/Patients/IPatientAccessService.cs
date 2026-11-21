using RAQEEB.Entities;

namespace RAQEEB.Services.Patients
{
    public interface IPatientAccessService
    {
        // Returns only the patients the current user is allowed to see.
        // Callers add their own filters/projection on top of it
        // (e.g. .Where(p => p.Id == patientId)).
        IQueryable<Patient> GetAccessiblePatients();
    }
}
