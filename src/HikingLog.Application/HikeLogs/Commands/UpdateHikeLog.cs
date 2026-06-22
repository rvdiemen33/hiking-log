namespace HikingLog.Application.HikeLogs.Commands;

using FluentValidation;
using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using OneOf;

/// <summary>Command to update an existing hike log entry.</summary>
/// <param name="Id">The primary key of the hike log to update.</param>
/// <param name="StageId">The primary key of the completed stage.</param>
/// <param name="DateHiked">The date on which the stage was hiked.</param>
/// <param name="DurationMinutes">The total duration of the hike in minutes.</param>
/// <param name="Weather">A description of the weather conditions during the hike.</param>
/// <param name="Notes">An optional personal note about the hike.</param>
/// <param name="Rating">The hiker's rating for this stage, on a scale from 1 to 5.</param>
public record UpdateHikeLog(int Id, int StageId, DateOnly DateHiked, int DurationMinutes, string Weather, string? Notes, int Rating);

/// <summary>Result returned after a hike log is successfully updated.</summary>
/// <param name="Id">The primary key of the updated hike log.</param>
public record UpdateHikeLogResult(int Id);

/// <summary>Validates the <see cref="UpdateHikeLog"/> command.</summary>
internal sealed class UpdateHikeLogValidator : AbstractValidator<UpdateHikeLog>
{
    /// <summary>Initializes a new instance of <see cref="UpdateHikeLogValidator"/> with all rules.</summary>
    public UpdateHikeLogValidator()
    {
        RuleFor(x => x.StageId).GreaterThan(0);
        RuleFor(x => x.DurationMinutes).GreaterThan(0);
        RuleFor(x => x.Weather).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
    }
}

/// <summary>Handles the <see cref="UpdateHikeLog"/> command by updating an existing hike log entry.</summary>
public sealed class UpdateHikeLogHandler(IHikingLogDataContext db, IValidator<UpdateHikeLog> validator)
    : ICommandHandler<UpdateHikeLog, OneOf<UpdateHikeLogResult, ValidationFailed, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<UpdateHikeLogResult, ValidationFailed, NotFound>> Handle(UpdateHikeLog command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return new ValidationFailed(validation.Errors);
        }

        var hikeLog = await db.HikeLogs.FindAsync([command.Id], ct);
        if (hikeLog is null)
        {
            return new NotFound();
        }

        // Business rule: when reparenting, the referenced stage must exist.
        var parentStage = await db.Stages.FindAsync([command.StageId], ct);
        if (parentStage is null)
        {
            return new NotFound();
        }

        hikeLog.StageId = command.StageId;
        hikeLog.DateHiked = command.DateHiked;
        hikeLog.DurationMinutes = command.DurationMinutes;
        hikeLog.Weather = command.Weather;
        hikeLog.Notes = command.Notes;
        hikeLog.Rating = command.Rating;
        await db.SaveChangesAsync(ct);
        return new UpdateHikeLogResult(hikeLog.Id);
    }
}
