namespace HikingLog.Application.HikeLogs.Commands;

using FluentValidation;
using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using HikingLog.Domain.Entities;
using OneOf;

/// <summary>Command to log a completed hiking stage.</summary>
/// <param name="StageId">The primary key of the completed stage.</param>
/// <param name="DateHiked">The date on which the stage was hiked.</param>
/// <param name="DurationMinutes">The total duration of the hike in minutes.</param>
/// <param name="Weather">A description of the weather conditions during the hike.</param>
/// <param name="Notes">An optional personal note about the hike.</param>
/// <param name="Rating">The hiker's rating for this stage, on a scale from 1 to 5.</param>
public record AddHikeLog(int StageId, DateOnly DateHiked, int DurationMinutes, string Weather, string? Notes, int Rating);

/// <summary>Result returned after a hike log is successfully created.</summary>
/// <param name="Id">The primary key of the newly created hike log.</param>
public record AddHikeLogResult(int Id);

/// <summary>Validates the <see cref="AddHikeLog"/> command.</summary>
internal sealed class AddHikeLogValidator : AbstractValidator<AddHikeLog>
{
    /// <summary>Initializes a new instance of <see cref="AddHikeLogValidator"/> with all rules.</summary>
    public AddHikeLogValidator()
    {
        RuleFor(x => x.StageId).GreaterThan(0);
        RuleFor(x => x.DurationMinutes).GreaterThan(0);
        RuleFor(x => x.Weather).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
    }
}

/// <summary>Handles the <see cref="AddHikeLog"/> command by persisting a new hike log entry.</summary>
public sealed class AddHikeLogHandler(IHikingLogDataContext db, IValidator<AddHikeLog> validator)
    : ICommandHandler<AddHikeLog, OneOf<AddHikeLogResult, ValidationFailed, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<AddHikeLogResult, ValidationFailed, NotFound>> Handle(AddHikeLog command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return new ValidationFailed(validation.Errors);
        }

        // Business rule: the referenced stage must exist before a hike log can be added to it.
        var parentStage = await db.Stages.FindAsync([command.StageId], ct);
        if (parentStage is null)
        {
            return new NotFound();
        }

        var hikeLog = new HikeLog
        {
            StageId = command.StageId,
            DateHiked = command.DateHiked,
            DurationMinutes = command.DurationMinutes,
            Weather = command.Weather,
            Notes = command.Notes,
            Rating = command.Rating
        };
        db.HikeLogs.Add(hikeLog);
        await db.SaveChangesAsync(ct);
        return new AddHikeLogResult(hikeLog.Id);
    }
}
