namespace HikingLog.Application.Stages.Commands;

using FluentValidation;
using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using HikingLog.Domain.Entities;
using HikingLog.Domain.Enums;
using OneOf;

/// <summary>Command to create a new stage on an existing route.</summary>
/// <param name="RouteId">The primary key of the parent route.</param>
/// <param name="Number">The sequence number within the route.</param>
/// <param name="Name">The stage name.</param>
/// <param name="StartPoint">The start location name.</param>
/// <param name="EndPoint">The end location name.</param>
/// <param name="DistanceKm">The length in kilometres.</param>
/// <param name="ElevationDifferenceM">The elevation difference in metres.</param>
/// <param name="Difficulty">The difficulty level.</param>
public record AddStage(int RouteId, int Number, string Name, string StartPoint, string EndPoint,
    decimal DistanceKm, decimal ElevationDifferenceM, Difficulty Difficulty);

/// <summary>Result returned after a stage is successfully created.</summary>
/// <param name="Id">The primary key of the newly created stage.</param>
public record AddStageResult(int Id);

/// <summary>Validates the <see cref="AddStage"/> command.</summary>
internal sealed class AddStageValidator : AbstractValidator<AddStage>
{
    /// <summary>Initializes a new instance of <see cref="AddStageValidator"/> with all rules.</summary>
    public AddStageValidator()
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

/// <summary>Handles the <see cref="AddStage"/> command by persisting a new stage entity.</summary>
public sealed class AddStageHandler(IHikingLogDataContext db, IValidator<AddStage> validator)
    : ICommandHandler<AddStage, OneOf<AddStageResult, ValidationFailed, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<AddStageResult, ValidationFailed, NotFound>> Handle(AddStage command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return new ValidationFailed(validation.Errors);
        }

        // Business rule: the referenced route must exist before a stage can be added to it.
        var parentRoute = await db.Routes.FindAsync([command.RouteId], ct);
        if (parentRoute is null)
        {
            return new NotFound();
        }

        var stage = new Stage
        {
            RouteId = command.RouteId,
            Number = command.Number,
            Name = command.Name,
            StartPoint = command.StartPoint,
            EndPoint = command.EndPoint,
            DistanceKm = command.DistanceKm,
            ElevationDifferenceM = command.ElevationDifferenceM,
            Difficulty = command.Difficulty
        };
        db.Stages.Add(stage);
        await db.SaveChangesAsync(ct);
        return new AddStageResult(stage.Id);
    }
}
