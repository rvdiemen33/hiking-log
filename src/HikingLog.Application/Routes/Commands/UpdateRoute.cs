namespace HikingLog.Application.Routes.Commands;

using FluentValidation;
using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using OneOf;

/// <summary>Command to update an existing route.</summary>
/// <param name="Id">The primary key of the route to update.</param>
/// <param name="Name">The new full name of the route.</param>
/// <param name="Code">The new abbreviation code.</param>
/// <param name="Country">The new country or region.</param>
/// <param name="TotalDistanceKm">The new total distance in kilometres.</param>
/// <param name="Description">The new optional description.</param>
public record UpdateRoute(int Id, string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);

/// <summary>Result returned after a route is successfully updated.</summary>
/// <param name="Id">The primary key of the updated route.</param>
public record UpdateRouteResult(int Id);

/// <summary>Validates the <see cref="UpdateRoute"/> command.</summary>
internal sealed class UpdateRouteValidator : AbstractValidator<UpdateRoute>
{
    /// <summary>Initializes a new instance of <see cref="UpdateRouteValidator"/> with all rules.</summary>
    public UpdateRouteValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TotalDistanceKm).GreaterThan(0);
    }
}

/// <summary>Handles the <see cref="UpdateRoute"/> command by updating an existing route entity.</summary>
public sealed class UpdateRouteHandler(IHikingLogDataContext db, IValidator<UpdateRoute> validator)
    : ICommandHandler<UpdateRoute, OneOf<UpdateRouteResult, ValidationFailed, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<UpdateRouteResult, ValidationFailed, NotFound>> Handle(UpdateRoute command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return new ValidationFailed(validation.Errors);
        }

        var route = await db.Routes.FindAsync([command.Id], ct);
        if (route is null)
        {
            return new NotFound();
        }

        route.Name = command.Name;
        route.Code = command.Code;
        route.Country = command.Country;
        route.TotalDistanceKm = command.TotalDistanceKm;
        route.Description = command.Description;
        await db.SaveChangesAsync(ct);
        return new UpdateRouteResult(route.Id);
    }
}
