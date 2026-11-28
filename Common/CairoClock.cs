namespace RAQEEB.Common
{
    // RAQEEB works in Egypt, so "today" means today in Cairo, not in UTC.
    // (Between midnight and 2-3 AM Cairo time, UTC is still "yesterday".)
    public static class CairoClock
    {
        private static readonly TimeZoneInfo? CairoTimeZone = FindCairoTimeZone();

        public static DateTime Now =>
            CairoTimeZone == null
                ? DateTime.UtcNow
                : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CairoTimeZone);

        public static DateTime Today => Now.Date;

        // Age in full years on today's Cairo date. Null when the birth date
        // is unknown or in the future.
        public static int? CalculateAge(DateTime? dateOfBirth)
        {
            if (dateOfBirth == null)
            {
                return null;
            }

            var today = Today;
            var birth = dateOfBirth.Value.Date;
            var age = today.Year - birth.Year;

            // Birthday has not happened yet this year.
            if (birth > today.AddYears(-age))
            {
                age--;
            }

            return age < 0 ? null : age;
        }

        // Windows uses "Egypt Standard Time", Linux/macOS use "Africa/Cairo".
        private static TimeZoneInfo? FindCairoTimeZone()
        {
            foreach (var id in new[] { "Africa/Cairo", "Egypt Standard Time" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return null;
        }
    }
}
