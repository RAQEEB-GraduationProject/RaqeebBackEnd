using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RAQEEB.Common
{
    // When a body misses [JsonRequired] properties (e.g. PUT /api/Patients/{id} with {}),
    // .NET reports them in ONE message and cuts the list after ~50 characters
    // ("...including: 'dateOfBirth', 'gender', ...").
    // This filter re-reads the body, finds EVERY missing required property and puts
    // one validation error per field, so the frontend sees the full list.
    // Runs before the automatic 400 response of [ApiController] (Order -2000).
    public class JsonRequiredFieldsFilter : IAsyncActionFilter, IOrderedFilter
    {
        public int Order => -3000;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var modelState = context.ModelState;

            var missingKeys = modelState
                .Where(e => e.Value != null && e.Value.Errors.Any(err =>
                    ((string.IsNullOrEmpty(err.ErrorMessage) ? err.Exception?.Message : err.ErrorMessage) ?? string.Empty)
                        .Contains("missing required properties", StringComparison.OrdinalIgnoreCase)))
                .Select(e => e.Key)
                .ToList();

            var bodyParameter = context.ActionDescriptor.Parameters
                .FirstOrDefault(p => p.BindingInfo?.BindingSource == BindingSource.Body);

            if (missingKeys.Count > 0 && bodyParameter != null && context.HttpContext.Request.Body.CanSeek)
            {
                var missing = await FindMissingAsync(context.HttpContext.Request, bodyParameter.ParameterType);

                if (missing != null && missing.Count > 0)
                {
                    foreach (var key in missingKeys)
                    {
                        modelState.Remove(key);
                    }

                    // Also drop the generic "The dto field is required." for the whole body.
                    modelState.Remove(bodyParameter.Name);

                    foreach (var name in missing)
                    {
                        modelState.AddModelError(name, "This field is required (send null to clear it).");
                    }
                }
            }

            await next();
        }

        // Returns the JSON names of the [JsonRequired] properties that are not in the body,
        // or null when the body cannot be read as a JSON object.
        private static async Task<List<string>?> FindMissingAsync(HttpRequest request, Type bodyType)
        {
            try
            {
                request.Body.Position = 0;
                using var document = await JsonDocument.ParseAsync(request.Body);

                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                // MVC binds JSON property names case-insensitively, so compare the same way.
                var present = new HashSet<string>(
                    document.RootElement.EnumerateObject().Select(p => p.Name),
                    StringComparer.OrdinalIgnoreCase);

                return bodyType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttribute<JsonRequiredAttribute>() != null)
                    .Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                                 ?? JsonNamingPolicy.CamelCase.ConvertName(p.Name))
                    .Where(name => !present.Contains(name))
                    .ToList();
            }
            catch (JsonException)
            {
                return null;
            }
            finally
            {
                request.Body.Position = 0;
            }
        }
    }
}
