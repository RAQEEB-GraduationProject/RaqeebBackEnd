using Microsoft.EntityFrameworkCore;
using RAQEEB.Data;
using RAQEEB.DTOs.Diagnostics;
using RAQEEB.Services.Diagnostics;

namespace RAQEEB.Services.Patients
{
    public class PatientDiagnosticsService : IPatientDiagnosticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPatientAccessService _patientAccess;
        private readonly IDiagnosticFileStorage _fileStorage;

        public PatientDiagnosticsService(
            ApplicationDbContext context,
            IPatientAccessService patientAccess,
            IDiagnosticFileStorage fileStorage)
        {
            _context = context;
            _patientAccess = patientAccess;
            _fileStorage = fileStorage;
        }

        public async Task<List<LabResultResponseDto>?> GetLabResultsAsync(
            Guid patientId)
        {
            // 1. Make sure the patient exists and the current user can access it.
            if (!await CanAccessPatientAsync(patientId))
            {
                return null;
            }

            // 2. Active results, newest first.
            return await _context.LabResults
                .AsNoTracking()
                .Where(l => l.PatientId == patientId && l.IsActive)
                .OrderByDescending(l => l.RecordedAt)
                .ThenBy(l => l.TestName)
                .Select(l => new LabResultResponseDto
                {
                    LabResultId = l.Id,
                    PatientId = l.PatientId,
                    TestName = l.TestName,
                    ResultValue = l.ResultValue,
                    Unit = l.Unit,
                    Flag = l.Flag,
                    RecordedAt = l.RecordedAt,
                    FileName = l.DiagnosticFile != null ? l.DiagnosticFile.FileName : null,
                    ContentType = l.DiagnosticFile != null ? l.DiagnosticFile.ContentType : null,
                    FileUrl = l.DiagnosticFileId != null
                        ? BuildFileUrl(l.PatientId, l.DiagnosticFileId.Value)
                        : null
                })
                .ToListAsync();
        }

        public async Task<List<ImagingResponseDto>?> GetImagingAsync(
            Guid patientId)
        {
            // 1. Make sure the patient exists and the current user can access it.
            if (!await CanAccessPatientAsync(patientId))
            {
                return null;
            }

            // 2. Active studies, newest first.
            return await _context.ImagingRecords
                .AsNoTracking()
                .Where(i => i.PatientId == patientId && i.IsActive)
                .OrderByDescending(i => i.StudyDate)
                .ThenBy(i => i.StudyName)
                .Select(i => new ImagingResponseDto
                {
                    ImagingId = i.Id,
                    PatientId = i.PatientId,
                    StudyName = i.StudyName,
                    StudyDate = i.StudyDate,
                    Finding = i.Finding,
                    FileName = i.DiagnosticFile != null ? i.DiagnosticFile.FileName : null,
                    ContentType = i.DiagnosticFile != null ? i.DiagnosticFile.ContentType : null,
                    FileUrl = i.DiagnosticFileId != null
                        ? BuildFileUrl(i.PatientId, i.DiagnosticFileId.Value)
                        : null
                })
                .ToListAsync();
        }

        public async Task<(DiagnosticFileStatus Status, DiagnosticFileResult? File)>
            GetFileAsync(Guid patientId, Guid fileId)
        {
            // 1. Same access rule as the lab / imaging lists.
            if (!await CanAccessPatientAsync(patientId))
            {
                return (DiagnosticFileStatus.PatientNotFound, null);
            }

            // 2. The file must belong to this patient AND be attached to
            //    one of this patient's active lab results or imaging records.
            var file = await _context.DiagnosticFiles
                .AsNoTracking()
                .Where(f => f.Id == fileId && f.PatientId == patientId)
                .Where(f =>
                    _context.LabResults.Any(l =>
                        l.DiagnosticFileId == f.Id &&
                        l.PatientId == patientId &&
                        l.IsActive) ||
                    _context.ImagingRecords.Any(i =>
                        i.DiagnosticFileId == f.Id &&
                        i.PatientId == patientId &&
                        i.IsActive))
                .Select(f => new
                {
                    f.FileName,
                    f.ContentType,
                    f.StoredFileName
                })
                .FirstOrDefaultAsync();

            if (file == null)
            {
                return (DiagnosticFileStatus.FileNotFound, null);
            }

            // 3. Resolve the physical file inside the private storage root.
            var fullPath = _fileStorage.GetFullPath(file.StoredFileName);

            if (fullPath == null || !File.Exists(fullPath))
            {
                return (DiagnosticFileStatus.FileNotFound, null);
            }

            return (
                DiagnosticFileStatus.Found,
                new DiagnosticFileResult
                {
                    FullPath = fullPath,
                    FileName = file.FileName,
                    ContentType = file.ContentType
                });
        }

        public static string BuildFileUrl(Guid patientId, Guid fileId)
        {
            return $"/api/Patients/{patientId}/diagnostics/files/{fileId}";
        }

        private Task<bool> CanAccessPatientAsync(Guid patientId)
        {
            return _patientAccess.GetAccessiblePatients()
                .AnyAsync(p => p.Id == patientId);
        }
    }
}
