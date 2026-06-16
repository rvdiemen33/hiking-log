namespace HikingLog.Application.Routes.Commands;

using FluentValidation;
using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using HikingLog.Domain.Entities;
using OneOf;

/// <summary>Command to create a new route.</summary>
/// <param name="Name">The full name of the route.</param>
/// <param name="Code">The abbreviation code (e.g. "LAW 1", "GR5").</param>
/// <param name="Country">The country or region.</param>
/// <param name="TotalDistanceKm">The total distance in kilometres.</param>
/// <param name="Description">An optional description of the route.</param>
public record AddRoute(string Name, string Code, string Country, decimal TotalDistanceKm, string? Description);

/// <summary>Result returned after a route is successfully created.</summary>
/// <param name="Id">The primary key of the newly created route.</param>
public record AddRouteResult(int Id);

/// <summary>Validates the <see cref="AddRoute"/> command.</summary>
internal sealed class AddRouteValidator : AbstractValidator<AddRoute>
{
    /// <summary>Initializes a new instance of <see cref="AddRouteValidator"/> with all rules.</summary>
    public AddRouteValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TotalDistanceKm).GreaterThan(0);
    }
}

/// <summary>Handles the <see cref="AddRoute"/> command by persisting a new route entity.</summary>
public sealed class AddRouteHandler(IHikingLogDataContext db, IValidator<AddRoute> validator)
    : ICommandHandler<AddRoute, OneOf<AddRouteResult, ValidationFailed>>
{
    /// <inheritdoc/>
    public async Task<OneOf<AddRouteResult, ValidationFailed>> Handle(AddRoute command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return new ValidationFailed(validation.Errors);
        }

        var route = new Route
        {
            Name = command.Name,
            Code = command.Code,
            Country = command.Country,
            TotalDistanceKm = command.TotalDistanceKm,
            Description = command.Description
        };
        db.Routes.Add(route);
        await db.SaveChangesAsync(ct);
        return new AddRouteResult(route.Id);
    }
}
