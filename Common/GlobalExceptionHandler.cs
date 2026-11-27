using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RAQEEB.DTOs.Common;

namespace RAQEEB.Common
{
    // Catches any exception that a controller did not handle and returns
    // the normal ApiResponse JSON instead of an HTML error page / raw 500.
    public class GlobalExceptionHandler : IExceptionHandler
    {
        // SQL Server error numbers for "duplicate key in a unique index".
        private const int UniqueIndexViolation = 2601;
        private const int UniqueConstraintViolation = 2627;

        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger,
            IHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // Example: two admins save the same medical record number at the same
            // moment. Both pass the "already exists?" check, the database blocks
            // the second insert. That is a conflict (409), not a server crash.
            if (IsDuplicateKey(exception))
            {
                _logger.LogWarning(exception, "Duplicate value rejected by the database.");

                httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                await httpContext.Response.WriteAsJsonAsync(
                    ApiResponse<object>.FailureResponse(
                        "DUPLICATE_VALUE",
                        "This record already exists (a value that must be unique is already in use)."),
                    cancellationToken);
                return true;
            }

            _logger.LogError(exception, "Unhandled exception.");

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(
                ApiResponse<object>.FailureResponse(
                    "INTERNAL_ERROR",
                    // Show the real error only while developing; never to real users.
                    _environment.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred. Please try again later."),
                cancellationToken);
            return true;
        }

        private static bool IsDuplicateKey(Exception exception)
        {
            return exception is DbUpdateException
            {
                InnerException: SqlException
                {
                    Number: UniqueIndexViolation or UniqueConstraintViolation
                }
            };
        }
    }
}
