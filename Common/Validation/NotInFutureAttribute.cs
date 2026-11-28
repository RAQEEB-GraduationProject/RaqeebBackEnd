using System.ComponentModel.DataAnnotations;

namespace RAQEEB.Common.Validation
{
    // Rejects a date that is after today (Cairo date). Null is allowed.
    // Used for DateOfBirth on create / update patient.
    [AttributeUsage(AttributeTargets.Property)]
    public class NotInFutureAttribute : ValidationAttribute
    {
        public NotInFutureAttribute()
            : base("{0} cannot be in the future.")
        {
        }

        public override bool IsValid(object? value)
        {
            return value switch
            {
                null => true,
                DateTime date => date.Date <= CairoClock.Today,
                DateOnly date => date <= DateOnly.FromDateTime(CairoClock.Today),
                _ => false
            };
        }
    }
}
