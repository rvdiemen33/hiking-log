namespace HikingLog.Application.Routes.Commands;

using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using OneOf;

/// <summary>Command to delete a route by its primary key.</summary>
/// <param name="Id">The primary key of the route to delete.</param>
public record DeleteRoute(int Id);

/// <summary>Handles the <see cref="DeleteRoute"/> command by removing the route entity from the database.</summary>
public sealed class DeleteRouteHandler(IHikingLogDataContext db)
    : ICommandHandler<DeleteRoute, OneOf<Success, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<Success, NotFound>> Handle(DeleteRoute command, CancellationToken ct)
    {
        var route = await db.Routes.FindAsync([command.Id], ct);
        if (route is null)
        {
            return new NotFound();
        }

        db.Routes.Remove(route);
        await db.SaveChangesAsync(ct);
        return new Success();
    }
}
