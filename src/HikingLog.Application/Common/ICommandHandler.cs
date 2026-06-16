namespace HikingLog.Application.Common;

/// <summary>Defines the contract for a command handler.</summary>
/// <typeparam name="TCommand">The type of the command.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
public interface ICommandHandler<TCommand, TResult>
{
    /// <summary>Handles the specified command.</summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>The result of handling the command.</returns>
    Task<TResult> Handle(TCommand command, CancellationToken ct);
}
