namespace HikingLog.Application.Stages.Commands;

using HikingLog.Application.Common;
using HikingLog.Application.Data.Contracts;
using OneOf;

/// <summary>Command to delete a stage by its primary key.</summary>
/// <param name="Id">The primary key of the stage to delete.</param>
public record DeleteStage(int Id);

/// <summary>Handles the <see cref="DeleteStage"/> command by removing the stage entity from the database.</summary>
public sealed class DeleteStageHandler(IHikingLogDataContext db)
    : ICommandHandler<DeleteStage, OneOf<Success, NotFound>>
{
    /// <inheritdoc/>
    public async Task<OneOf<Success, NotFound>> Handle(DeleteStage command, CancellationToken ct)
    {
        var stage = await db.Stages.FindAsync([command.Id], ct);
        if (stage is null)
        {
            return new NotFound();
        }

        db.Stages.Remove(stage);
        await db.SaveChangesAsync(ct);
        return new Success();
    }
}
