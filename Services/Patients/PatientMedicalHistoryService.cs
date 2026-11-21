using Microsoft.EntityFrameworkCore;
using RAQEEB.Data;
using RAQEEB.DTOs.PatientDetails;
using RAQEEB.Entities;

namespace RAQEEB.Services.Patients
{
    public class PatientMedicalHistoryService : IPatientMedicalHistoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPatientAccessService _patientAccess;

        public PatientMedicalHistoryService(
            ApplicationDbContext context,
            IPatientAccessService patientAccess)
        {
            _context = context;
            _patientAccess = patientAccess;
        }

        public async Task<MedicalHistoryResponseDto?> GetMedicalHistoryAsync(
            Guid patientId)
        {
            // 1. Make sure the patient exists and the current user can access it.
            var canAccess = await _patientAccess.GetAccessiblePatients()
                .AnyAsync(p => p.Id == patientId);

            if (!canAccess)
            {
                return null;
            }

            // 2. Get the active records (dated ones first, newest first).
            var entries = await _context.PatientMedicalHistoryEntries
                .AsNoTracking()
                .Where(e => e.PatientId == patientId && e.IsActive)
                .OrderByDescending(e => e.EventDate)
                .ThenBy(e => e.CreatedAt)
                .ToListAsync();

            // 3. Group them by type.
            return new MedicalHistoryResponseDto
            {
                Diseases = entries
                    .Where(e => e.Type == MedicalHistoryType.Disease)
                    .Select(ToItem)
                    .ToList(),

                Allergies = entries
                    .Where(e => e.Type == MedicalHistoryType.Allergy)
                    .Select(ToItem)
                    .ToList(),

                Surgeries = entries
                    .Where(e => e.Type == MedicalHistoryType.Surgery)
                    .Select(ToItem)
                    .ToList(),

                FamilyHistory = entries
                    .Where(e => e.Type == MedicalHistoryType.FamilyHistory)
                    .Select(ToItem)
                    .ToList(),

                TotalCount = entries.Count
            };
        }

        public async Task<(CreateMedicalHistoryStatus Status, MedicalHistoryEntryResponseDto? Entry)>
            CreateEntryAsync(Guid patientId, CreateMedicalHistoryEntryDto dto)
        {
            // 1. Find the patient (only if the current user can access it).
            var patient = await _patientAccess.GetAccessiblePatients()
                .Where(p => p.Id == patientId)
                .Select(p => new { p.Id, p.IsActive })
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return (CreateMedicalHistoryStatus.PatientNotFound, null);
            }

            // 2. Do not add records to a deactivated patient.
            if (!patient.IsActive)
            {
                return (CreateMedicalHistoryStatus.PatientInactive, null);
            }

            // 3. Create the record.
            var entry = new PatientMedicalHistoryEntry
            {
                PatientId = patient.Id,
                Type = Enum.Parse<MedicalHistoryType>(dto.Type, ignoreCase: true),
                Name = dto.Name.Trim(),
                EventDate = dto.Date,
                Notes = string.IsNullOrWhiteSpace(dto.Notes)
                    ? null
                    : dto.Notes.Trim()
            };

            _context.PatientMedicalHistoryEntries.Add(entry);

            await _context.SaveChangesAsync();

            // 4. Return the created record.
            return (
                CreateMedicalHistoryStatus.Created,
                new MedicalHistoryEntryResponseDto
                {
                    Id = entry.Id,
                    PatientId = entry.PatientId,
                    Type = entry.Type.ToString(),
                    Name = entry.Name,
                    Date = entry.EventDate,
                    Notes = entry.Notes,
                    CreatedAt = entry.CreatedAt
                });
        }

        private static MedicalHistoryItemDto ToItem(
            PatientMedicalHistoryEntry entry)
        {
            return new MedicalHistoryItemDto
            {
                Id = entry.Id,
                Name = entry.Name,
                Date = entry.EventDate,
                Notes = entry.Notes
            };
        }
    }
}
