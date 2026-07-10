using System.Net;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace OpenShock.Activity.Api.Problems;

/// <summary>Problem returned for model-validation failures, mirroring the main OpenShock API's shape.</summary>
public sealed class ValidationProblem : OpenShockProblem
{
    public ValidationProblem(ModelStateDictionary state)
        : base("Validation.Error", "One or more validation errors occurred", HttpStatusCode.BadRequest)
    {
        Errors = CreateErrorDictionary(state);
    }

    public IDictionary<string, string[]> Errors { get; set; }

    private static IDictionary<string, string[]> CreateErrorDictionary(ModelStateDictionary modelState)
    {
        var errors = new Dictionary<string, string[]>(modelState.Count, StringComparer.Ordinal);

        foreach (var (key, entry) in modelState)
        {
            if (entry.Errors is not { Count: > 0 }) continue;
            errors.Add(key, entry.Errors.Select(GetErrorMessage).ToArray());
        }

        return errors;

        static string GetErrorMessage(ModelError error)
            => string.IsNullOrEmpty(error.ErrorMessage) ? "The input was not valid" : error.ErrorMessage;
    }
}
