namespace RAQEEB.Services.Diagnostics
{
    // Private local storage for diagnostic documents.
    // Files live outside wwwroot and are never served as static files.
    public interface IDiagnosticFileStorage
    {
        // Returns the absolute path for a stored file name,
        // or null when the name would resolve outside the storage root.
        string? GetFullPath(string storedFileName);

        Task SaveAsync(string storedFileName, byte[] content);
    }
}
