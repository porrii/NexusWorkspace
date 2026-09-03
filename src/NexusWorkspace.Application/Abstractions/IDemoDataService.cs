namespace NexusWorkspace.Application.Abstractions;

/// <summary>Creates or removes an optional demo workspace. Fully reversible.</summary>
public interface IDemoDataService
{
    Task<bool> IsPresentAsync(CancellationToken cancellationToken = default);

    Task SeedAsync(CancellationToken cancellationToken = default);

    Task RemoveAsync(CancellationToken cancellationToken = default);
}
