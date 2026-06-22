namespace HikingLog.Application.HikeLogs.Commands;

using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using OneOf;

/// <summary>Command to delete a hike log entry by its primary key.</summary>
/// <param name="Id">The primary key of the hike log to delete.</param>
public record DeleteHikeLog(int Id);

/// <summary>Handles the <see cref="DeleteHikeLog"/> command by removing the hike log entry from the database.</summary>
public sealed class DeleteHikeLogHandler(IHikingLogDataContext db)
    : ICommandHandler<DeleteHikeLog, OneOf<Success, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<Success, NotFound>> Handle(DeleteHikeLog command, CancellationToken ct)
    {
        var hikeLog = await db.HikeLogs.FindAsync([command.Id], ct);
        if (hikeLog is null)
        {
            return new NotFound();
        }

        db.HikeLogs.Remove(hikeLog);
        await db.SaveChangesAsync(ct);
        return new Success();
    }
}
