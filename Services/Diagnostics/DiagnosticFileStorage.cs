namespace RAQEEB.Services.Diagnostics
{
    public class DiagnosticFileStorage : IDiagnosticFileStorage
    {
        private readonly string _rootPath;

        public DiagnosticFileStorage(
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            // Default: <ContentRoot>/App_Data/diagnostics (not under wwwroot, git-ignored).
            var configured = configuration["DiagnosticsStorage:RootPath"];

            var root = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(environment.ContentRootPath, "App_Data", "diagnostics")
                : Path.Combine(environment.ContentRootPath, configured);

            _rootPath = Path.GetFullPath(root);
        }

        public string? GetFullPath(string storedFileName)
        {
            if (string.IsNullOrWhiteSpace(storedFileName))
            {
                return null;
            }

            var fullPath = Path.GetFullPath(
                Path.Combine(_rootPath, storedFileName));

            // Block path traversal (e.g. "../appsettings.json").
            var rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar)
                ? _rootPath
                : _rootPath + Path.DirectorySeparatorChar;

            return fullPath.StartsWith(rootWithSeparator, StringComparison.Ordinal)
                ? fullPath
                : null;
        }

        public async Task SaveAsync(string storedFileName, byte[] content)
        {
            var fullPath = GetFullPath(storedFileName)
                ?? throw new InvalidOperationException(
                    "Invalid diagnostic file path.");

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            await File.WriteAllBytesAsync(fullPath, content);
        }
    }
}
