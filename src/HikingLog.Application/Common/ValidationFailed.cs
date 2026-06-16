namespace HikingLog.Application.Common;

using FluentValidation.Results;

/// <summary>Represents a failed validation result containing one or more errors.</summary>
public sealed class ValidationFailed
{
    /// <summary>Initializes a new instance of <see cref="ValidationFailed"/> with the given errors.</summary>
    /// <param name="errors">The validation errors.</param>
    public ValidationFailed(IEnumerable<ValidationFailure> errors) => Errors = errors;

    /// <summary>Gets the collection of validation errors.</summary>
    public IEnumerable<ValidationFailure> Errors { get; }
}
