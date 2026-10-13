using System.ComponentModel.DataAnnotations;
using System.Security.Principal;

namespace RAQEEB.DTOs.PatientAssignments
{
    public class CreatePatientAssignmentDto
    {
        [Required]
        public Guid PatientId { get; set; } //identifies the patient's medical profile.

        [Required]
        public string UserId { get; set; } = string.Empty; //dentifies the Identity account of the assigned Doctor, Nurse, or Caregiver
    }
}