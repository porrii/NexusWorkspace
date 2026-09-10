namespace NexusWorkspace.UI.ViewModels.Shared;

/// <summary>A dropdown option carrying a nullable value struct plus a display label.</summary>
public sealed record FilterOption<T>(string Label, T? Value)
    where T : struct;

/// <summary>Project filter option ("Todos" when <see cref="Id"/> is null).</summary>
public sealed record ProjectOption(string Label, Guid? Id);

/// <summary>Person filter option ("Cualquiera" when <see cref="Id"/> is null).</summary>
public sealed record PersonOption(string Label, Guid? Id);
