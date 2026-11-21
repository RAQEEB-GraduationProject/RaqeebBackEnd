using Microsoft.EntityFrameworkCore;
using RAQEEB.Common;
using RAQEEB.Data;
using RAQEEB.DTOs.PatientDetails;
using RAQEEB.Entities;

namespace RAQEEB.Services.Patients
{
    public class PatientJournalService : IPatientJournalService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPatientAccessService _patientAccess;
        private readonly ICurrentUser _currentUser;

        public PatientJournalService(
            ApplicationDbContext context,
            IPatientAccessService patientAccess,
            ICurrentUser currentUser)
        {
            _context = context;
            _patientAccess = patientAccess;
            _currentUser = currentUser;
        }

        public async Task<JournalResponseDto?> GetJournalAsync(Guid patientId)
        {
            // 1. Make sure the patient exists and the current user can access it.
            var canAccess = await _patientAccess.GetAccessiblePatients()
                .AnyAsync(p => p.Id == patientId);

            if (!canAccess)
            {
                return null;
            }

            // 2. Get the active entries (newest first).
            var entries = await _context.PatientJournalEntries
                .AsNoTracking()
                .Where(e => e.PatientId == patientId && e.IsActive)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.Id,
                    e.EntryType,
                    e.Content,
                    e.AuthorUserId,
                    AuthorUserName = e.Author.UserName,
                    AuthorFullName = e.Author.FullName,
                    e.CreatedAt
                })
                .ToListAsync();

            // 3. Get the role of each author (one query for all authors).
            var authorIds = entries
                .Select(e => e.AuthorUserId)
                .Distinct()
                .ToList();

            var authorRoles = await (
                from ur in _context.UserRoles.AsNoTracking()
                join r in _context.Roles.AsNoTracking()
                    on ur.RoleId equals r.Id
                where authorIds.Contains(ur.UserId)
                select new { ur.UserId, RoleName = r.Name }
            ).ToListAsync();

            var roleByUser = authorRoles
                .Where(x => x.RoleName != null)
                .GroupBy(x => x.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.RoleName!).OrderBy(n => n).First());

            // 4. Build the response.
            return new JournalResponseDto
            {
                Entries = entries
                    .Select(e => new JournalEntryItemDto
                    {
                        EntryId = e.Id,
                        EntryType = e.EntryType.ToString(),
                        Content = e.Content,
                        Author = new JournalAuthorDto
                        {
                            UserId = e.AuthorUserId,
                            Name = GetDisplayName(e.AuthorFullName, e.AuthorUserName),
                            Role = roleByUser.GetValueOrDefault(e.AuthorUserId)
                        },
                        CreatedAt = DateTime.SpecifyKind(
                            e.CreatedAt,
                            DateTimeKind.Utc)
                    })
                    .ToList(),

                TotalCount = entries.Count
            };
        }

        public async Task<(CreateJournalEntryStatus Status, JournalEntryItemDto? Entry)>
            CreateEntryAsync(Guid patientId, CreateJournalEntryDto dto)
        {
            // 1. Find the patient (only if the current user can access it).
            var patient = await _patientAccess.GetAccessiblePatients()
                .Where(p => p.Id == patientId)
                .Select(p => new { p.Id, p.IsActive })
                .FirstOrDefaultAsync();

            var authorId = _currentUser.UserId;

            if (patient == null || authorId == null)
            {
                return (CreateJournalEntryStatus.PatientNotFound, null);
            }

            // 2. Do not add entries to a deactivated patient.
            if (!patient.IsActive)
            {
                return (CreateJournalEntryStatus.PatientInactive, null);
            }

            // 3. Create the entry (the author is the current user).
            var entry = new PatientJournalEntry
            {
                PatientId = patient.Id,
                EntryType = Enum.Parse<JournalEntryType>(
                    dto.EntryType,
                    ignoreCase: true),
                Content = dto.Content.Trim(),
                AuthorUserId = authorId
            };

            _context.PatientJournalEntries.Add(entry);

            await _context.SaveChangesAsync();

            // 4. Return the created entry.
            var author = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == authorId)
                .Select(u => new { u.UserName, u.FullName })
                .FirstOrDefaultAsync();

            return (
                CreateJournalEntryStatus.Created,
                new JournalEntryItemDto
                {
                    EntryId = entry.Id,
                    EntryType = entry.EntryType.ToString(),
                    Content = entry.Content,
                    Author = new JournalAuthorDto
                    {
                        UserId = authorId,
                        Name = GetDisplayName(author?.FullName, author?.UserName),
                        Role = _currentUser.Role
                    },
                    CreatedAt = entry.CreatedAt
                });
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
