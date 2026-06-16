namespace HikingLog.Api.Extensions;

using HikingLog.Application.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>Extension methods for converting <see cref="ValidationFailed"/> to ASP.NET Core types.</summary>
internal static class ValidationFailedExtensions
{
    /// <summary>Converts a <see cref="ValidationFailed"/> to a <see cref="ModelStateDictionary"/> for use with <c>ValidationProblem</c>.</summary>
    /// <param name="failed">The validation failed result containing one or more errors.</param>
    /// <returns>A <see cref="ModelStateDictionary"/> populated with all validation errors.</returns>
    internal static ModelStateDictionary ToModelStateDictionary(this ValidationFailed failed)
    {
        var modelState = new ModelStateDictionary();
        foreach (var error in failed.Errors)
        {
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return modelState;
    }
}
