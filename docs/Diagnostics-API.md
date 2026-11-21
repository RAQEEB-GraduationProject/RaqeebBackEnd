# Diagnostics API (Labs & Imaging) — Frontend guide

Read-only endpoints. All require `Authorization: Bearer <JWT>` from `POST /api/Auth/login`.

| Method | URL | Returns |
|---|---|---|
| GET | `/api/Patients/{patientId}/diagnostics/labs` | `ApiResponse<LabResultResponseDto[]>` (newest first) |
| GET | `/api/Patients/{patientId}/diagnostics/imaging` | `ApiResponse<ImagingResponseDto[]>` (newest first) |
| GET | `/api/Patients/{patientId}/diagnostics/files/{fileId}` | The PDF (`application/pdf`). Add `?download=true` to download instead of view |

`patientId` = the patient profile id (`Patient.Id`, also `patientId` in the login response for Patient users) — not a UserId.

## Access rules (same as the other patient endpoints)

- **Admin**: patients of their own hospital.
- **Doctor / Nurse / Caregiver**: only patients with an active assignment to them.
- **Patient**: only their own profile.

No access and unknown patient both return **404** (`PATIENT_NOT_FOUND`), so ids cannot be probed.

## Responses

Labs (200):

```json
{
  "success": true,
  "data": [
    {
      "labResultId": "c1a50001-0000-4000-8000-000000000000",
      "patientId": "3f6c2b1a-0001-4d1e-8a00-000000000001",
      "testName": "Hemoglobin",
      "resultValue": "10.8",
      "unit": "g/dL",
      "flag": "Low",
      "recordedAt": "2026-11-18T08:30:00",
      "fileName": "hemoglobin_2026-11-18.pdf",
      "contentType": "application/pdf",
      "fileUrl": "/api/Patients/3f6c2b1a-0001-4d1e-8a00-000000000001/diagnostics/files/f1a50001-0000-4000-8000-000000000000"
    }
  ],
  "message": "Lab results retrieved successfully.",
  "errorCode": null,
  "errors": []
}
```

Imaging (200):

```json
{
  "success": true,
  "data": [
    {
      "imagingId": "c2b60005-0000-4000-8000-000000000000",
      "patientId": "3f6c2b1a-0001-4d1e-8a00-000000000001",
      "studyName": "Brain MRI",
      "studyDate": "2026-11-10T15:45:00",
      "finding": "No enhancing intracranial lesions. No midline shift or hydrocephalus.",
      "fileName": "brain_mri_2026-11-10.pdf",
      "contentType": "application/pdf",
      "fileUrl": "/api/Patients/3f6c2b1a-0001-4d1e-8a00-000000000001/diagnostics/files/f2b60005-0000-4000-8000-000000000000"
    }
  ],
  "message": "Imaging records retrieved successfully.",
  "errorCode": null,
  "errors": []
}
```

- A patient with no records returns **200** with `"data": []`.
- `flag` is `Normal`, `High` or `Low`. Dates are UTC (serialized without `Z`, like the rest of the API).
- `fileName`, `contentType` and `fileUrl` are `null` when a record has no PDF.

Errors:

| Status | Body | When |
|---|---|---|
| 401 | empty | Missing, invalid or expired token |
| 404 | `{"success": false, "errorCode": "PATIENT_NOT_FOUND", ...}` | Patient does not exist or caller has no access |
| 404 | `{"success": false, "errorCode": "FILE_NOT_FOUND", ...}` | File id is not a PDF of this patient's labs/imaging (or missing on disk) |

## Opening / downloading PDFs

`fileUrl` is relative to the API base URL and needs the JWT header, so a plain `<a href>` or `<iframe src>` will get 401. Fetch it as a blob:

```js
async function openDiagnosticPdf(item, { download = false } = {}) {
  const url = `${API_BASE}${item.fileUrl}${download ? "?download=true" : ""}`;
  const res = await fetch(url, { headers: { Authorization: `Bearer ${token}` } });
  if (!res.ok) throw new Error(`PDF request failed: ${res.status}`);

  const blobUrl = URL.createObjectURL(await res.blob());

  if (download) {
    const a = document.createElement("a");
    a.href = blobUrl;
    a.download = item.fileName;
    a.click();
  } else {
    window.open(blobUrl, "_blank");
  }

  setTimeout(() => URL.revokeObjectURL(blobUrl), 60_000);
}
```

Response headers: `Content-Type: application/pdf`, `Content-Disposition: inline|attachment; filename=...` (exposed via CORS), `Cache-Control: no-store`.

## Development demo data (Development environment only)

Seeded automatically on startup when `ASPNETCORE_ENVIRONMENT=Development`. Never seeded in Production.

| UserName | Password | Role | Notes |
|---|---|---|---|
| `admin` | existing seeded admin password | Admin | unchanged |
| `demo.doctor` | `Demo@12345` | Doctor | assigned to demo patient |
| `demo.nurse` | `Demo@12345` | Nurse | assigned to demo patient |
| `demo.caregiver` | `Demo@12345` | Caregiver | assigned to demo patient |
| `demo.patient` | `Demo@12345` | Patient | PatientId `3f6c2b1a-0001-4d1e-8a00-000000000001` (10 labs, 5 imaging, 15 PDFs) |
| `demo.patient2` | `Demo@12345` | Patient | PatientId `3f6c2b1a-0002-4d1e-8a00-000000000002` (no diagnostics, no assignments) |

The demo password can be overridden with the `DemoSeed:Password` configuration value.
PDFs are generated into `App_Data/diagnostics/` (git-ignored, private; change with `DiagnosticsStorage:RootPath`).

## Swagger

1. Run the API in Development and open `/swagger`.
2. `POST /api/Auth/login` with e.g. `{"userName": "demo.doctor", "password": "Demo@12345"}`; copy `data.token`.
3. Click **Authorize**, paste the token.
4. Call the three `PatientDiagnostics` endpoints with the demo PatientId; the file endpoint shows a "Download file" link.
