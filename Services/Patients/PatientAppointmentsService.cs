using Microsoft.EntityFrameworkCore;
using RAQEEB.Common;
using RAQEEB.Data;
using RAQEEB.DTOs.PatientDetails;
using RAQEEB.Entities;

namespace RAQEEB.Services.Patients
{
    public class PatientAppointmentsService : IPatientAppointmentsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPatientAccessService _patientAccess;
        private readonly ICurrentUser _currentUser;

        public PatientAppointmentsService(
            ApplicationDbContext context,
            IPatientAccessService patientAccess,
            ICurrentUser currentUser)
        {
            _context = context;
            _patientAccess = patientAccess;
            _currentUser = currentUser;
        }

        public async Task<AppointmentsResponseDto?> GetAppointmentsAsync(
            Guid patientId,
            bool upcomingOnly)
        {
            // 1. Make sure the patient exists and the current user can access it.
            var canAccess = await _patientAccess.GetAccessiblePatients()
                .AnyAsync(p => p.Id == patientId);

            if (!canAccess)
            {
                return null;
            }

            // 2. Build the query (active appointments only).
            var query = _context.PatientAppointments
                .AsNoTracking()
                .Where(a => a.PatientId == patientId && a.IsActive);

            // Upcoming = today and later (calculated in Cairo time).
            if (upcomingOnly)
            {
                var today = GetTodayInCairo();

                query = query.Where(a => a.AppointmentDate >= today);
            }

            // 3. Get the appointments in chronological order.
            var appointments = await query
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.AppointmentDate,
                    a.AppointmentTime,
                    a.Location,
                    ProviderId = a.ProviderUserId,
                    ProviderUserName = a.Provider.UserName,
                    ProviderFullName = a.Provider.FullName
                })
                .ToListAsync();

            // 4. Build the response.
            return new AppointmentsResponseDto
            {
                Appointments = appointments
                    .Select(a => new AppointmentItemDto
                    {
                        AppointmentId = a.Id,
                        Title = a.Title,
                        AppointmentDate = a.AppointmentDate,
                        AppointmentTime = a.AppointmentTime,
                        Provider = new AppointmentProviderDto
                        {
                            DoctorId = a.ProviderId,
                            Name = GetDisplayName(
                                a.ProviderFullName,
                                a.ProviderUserName)
                        },
                        Location = a.Location
                    })
                    .ToList(),

                TotalCount = appointments.Count
            };
        }

        public async Task<(CreateAppointmentStatus Status, AppointmentItemDto? Appointment)>
            CreateAppointmentAsync(Guid patientId, CreateAppointmentDto dto)
        {
            // 1. Find the patient (only if the current user can access it).
            var patient = await _patientAccess.GetAccessiblePatients()
                .Where(p => p.Id == patientId)
                .Select(p => new { p.Id, p.IsActive, p.HospitalId })
                .FirstOrDefaultAsync();

            var createdById = _currentUser.UserId;

            if (patient == null || createdById == null)
            {
                return (CreateAppointmentStatus.PatientNotFound, null);
            }

            // 2. Do not add appointments to a deactivated patient.
            if (!patient.IsActive)
            {
                return (CreateAppointmentStatus.PatientInactive, null);
            }

            // 3. The provider must be an active Doctor in the same hospital.
            var provider = await (
                from u in _context.Users.AsNoTracking()
                join ur in _context.UserRoles.AsNoTracking()
                    on u.Id equals ur.UserId
                join r in _context.Roles.AsNoTracking()
                    on ur.RoleId equals r.Id
                where u.Id == dto.ProviderId
                    && u.IsActive
                    && u.HospitalId == patient.HospitalId
                    && r.Name == "Doctor"
                select new { u.Id, u.UserName, u.FullName }
            ).FirstOrDefaultAsync();

            if (provider == null)
            {
                return (CreateAppointmentStatus.InvalidProvider, null);
            }

            // 4. Create the appointment.
            var appointment = new PatientAppointment
            {
                PatientId = patient.Id,
                Title = dto.Title.Trim(),
                AppointmentDate = dto.AppointmentDate!.Value,
                AppointmentTime = dto.AppointmentTime!.Value,
                ProviderUserId = provider.Id,
                Location = string.IsNullOrWhiteSpace(dto.Location)
                    ? null
                    : dto.Location.Trim(),
                CreatedByUserId = createdById
            };

            _context.PatientAppointments.Add(appointment);

            await _context.SaveChangesAsync();

            // 5. Return the created appointment.
            return (
                CreateAppointmentStatus.Created,
                new AppointmentItemDto
                {
                    AppointmentId = appointment.Id,
                    Title = appointment.Title,
                    AppointmentDate = appointment.AppointmentDate,
                    AppointmentTime = appointment.AppointmentTime,
                    Provider = new AppointmentProviderDto
                    {
                        DoctorId = provider.Id,
                        Name = GetDisplayName(provider.FullName, provider.UserName)
                    },
                    Location = appointment.Location
                });
        }

        // Appointments are stored as local (Egypt) date/time,
        // so "today" is calculated in Cairo time, not UTC.
        private static DateOnly GetTodayInCairo()
        {
            foreach (var id in new[] { "Africa/Cairo", "Egypt Standard Time" })
            {
                try
                {
                    var timeZone = TimeZoneInfo.FindSystemTimeZoneById(id);

                    return DateOnly.FromDateTime(
                        TimeZoneInfo.ConvertTimeFromUtc(
                            DateTime.UtcNow,
                            timeZone));
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            // Fallback: UTC+2
            return DateOnly.FromDateTime(DateTime.UtcNow.AddHours(2));
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
