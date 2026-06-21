namespace HikingLog.Application.Stages.Commands;

using FluentValidation;
using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using HikingLog.Domain.Enums;
using OneOf;

/// <summary>Command to update an existing stage.</summary>
/// <param name="Id">The primary key of the stage to update.</param>
/// <param name="RouteId">The primary key of the parent route.</param>
/// <param name="Number">The new sequence number within the route.</param>
/// <param name="Name">The new stage name.</param>
/// <param name="StartPoint">The new start location name.</param>
/// <param name="EndPoint">The new end location name.</param>
/// <param name="DistanceKm">The new length in kilometres.</param>
/// <param name="ElevationDifferenceM">The new elevation difference in metres.</param>
/// <param name="Difficulty">The new difficulty level.</param>
public record UpdateStage(int Id, int RouteId, int Number, string Name, string StartPoint, string EndPoint,
    decimal DistanceKm, decimal ElevationDifferenceM, Difficulty Difficulty);

/// <summary>Result returned after a stage is successfully updated.</summary>
/// <param name="Id">The primary key of the updated stage.</param>
public record UpdateStageResult(int Id);

/// <summary>Validates the <see cref="UpdateStage"/> command.</summary>
internal sealed class UpdateStageValidator : AbstractValidator<UpdateStage>
{
    /// <summary>Initializes a new instance of <see cref="UpdateStageValidator"/> with all rules.</summary>
    public UpdateStageValidator()
    {
        RuleFor(x => x.RouteId).GreaterThan(0);
        RuleFor(x => x.Number).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartPoint).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndPoint).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DistanceKm).GreaterThan(0);
        RuleFor(x => x.ElevationDifferenceM).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Difficulty).IsInEnum();
    }
}

/// <summary>Handles the <see cref="UpdateStage"/> command by updating an existing stage entity.</summary>
public sealed class UpdateStageHandler(IHikingLogDataContext db, IValidator<UpdateStage> validator)
    : ICommandHandler<UpdateStage, OneOf<UpdateStageResult, ValidationFailed, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<UpdateStageResult, ValidationFailed, NotFound>> Handle(UpdateStage command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return new ValidationFailed(validation.Errors);
        }

        var stage = await db.Stages.FindAsync([command.Id], ct);
        if (stage is null)
        {
            return new NotFound();
        }

        // Business rule: when reparenting, the referenced route must exist.
        var parentRoute = await db.Routes.FindAsync([command.RouteId], ct);
        if (parentRoute is null)
        {
            return new NotFound();
        }

        stage.RouteId = command.RouteId;
        stage.Number = command.Number;
        stage.Name = command.Name;
        stage.StartPoint = command.StartPoint;
        stage.EndPoint = command.EndPoint;
        stage.DistanceKm = command.DistanceKm;
        stage.ElevationDifferenceM = command.ElevationDifferenceM;
        stage.Difficulty = command.Difficulty;
        await db.SaveChangesAsync(ct);
        return new UpdateStageResult(stage.Id);
    }
}
