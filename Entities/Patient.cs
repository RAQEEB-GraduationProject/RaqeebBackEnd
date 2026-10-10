namespace RAQEEB.Entities
{
    public class Patient
    {
        public Guid Id { get; set; } = Guid.NewGuid(); //Id identifies the patient profile.




        // Link and assign the patient profile to their login account (refrence)
        public string UserId { get; set; } = string.Empty; //UserId identifies the associated login account. && Foreign key relationship

        public ApplicationUser User { get; set; } = null!; //The ! is the null-forgiving operator && Object level access to the associated login acc




        // Hospital that manages this patient
        public Guid HospitalId { get; set; } //This stores the ID of the hospital responsible for the patient.

        public Hospital Hospital { get; set; } = null!; //This lets Entity Framework Core navigate from the patient to the hospital.(navigation property)




        // Patient information
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? MedicalRecordNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}