namespace NexusWorkspace.Application.Abstractions;

/// <summary>
/// Runs a use case inside a fresh DI scope, so each operation gets its own
/// short-lived <see cref="IApplicationDbContext"/> (no change-tracker bloat, no
/// stale data). ViewModels use this instead of holding a long-lived context.
/// </summary>
public interface IUnitOfWorkRunner
{
    Task<T> RunAsync<T>(
        Func<IServiceProvider, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);

    Task RunAsync(
        Func<IServiceProvider, CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
