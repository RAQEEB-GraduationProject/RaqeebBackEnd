using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RAQEEB.Common.Validation;

namespace RAQEEB.DTOs.Patients
{
    // Body of: PUT /api/Patients/{id}  (full replacement)
    //
    // [JsonRequired] = the property MUST be present in the JSON body:
    //   - missing property        -> 400 VALIDATION_ERROR (nothing is changed)
    //   - "property": null        -> clears the value on purpose
    //   - "property": "value"     -> saves the value (send the current value to keep it)
    // So an incomplete body (even {}) can no longer wipe the patient profile.
    public class UpdatePatientDto
    {
        [JsonRequired]
        [NotInFuture]
        public DateTime? DateOfBirth { get; set; }

        [JsonRequired]
        [StringLength(30)]
        public string? Gender { get; set; }

        [JsonRequired]
        [StringLength(100)]
        public string? MedicalRecordNumber { get; set; }

        [JsonRequired]
        [StringLength(200)]
        public string? Diagnosis { get; set; }

        [JsonRequired]
        [StringLength(100)]
        public string? Stage { get; set; }

        [JsonRequired]
        [StringLength(200)]
        public string? Treatment { get; set; }

        [JsonRequired]
        [StringLength(200)]
        public string? Protocol { get; set; }

        [JsonRequired]
        [StringLength(50)]
        public string? RiskLevel { get; set; }

        [JsonRequired]
        [RegularExpression(@"^\s*(A|B|AB|O)[+-]\s*$",
            ErrorMessage = "Blood group must be one of: A+, A-, B+, B-, AB+, AB-, O+, O-.")]
        public string? BloodGroup { get; set; }
    }
}
