using Microsoft.Extensions.DependencyInjection;
using NexusWorkspace.Application.Abstractions;

namespace NexusWorkspace.Application.Common;

/// <inheritdoc cref="IUnitOfWorkRunner" />
internal sealed class ScopedUnitOfWorkRunner(IServiceScopeFactory scopeFactory) : IUnitOfWorkRunner
{
    public async Task<T> RunAsync<T>(
        Func<IServiceProvider, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        return await operation(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
    }

    public async Task RunAsync(
        Func<IServiceProvider, CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        await operation(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
    }
}
