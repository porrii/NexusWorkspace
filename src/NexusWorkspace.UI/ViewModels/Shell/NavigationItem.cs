using NexusWorkspace.UI.Services;

namespace NexusWorkspace.UI.ViewModels.Shell;

/// <summary>An entry in the primary navigation rail.</summary>
/// <param name="Key">Target section.</param>
/// <param name="Label">Spanish label.</param>
/// <param name="Icon">Material icon kind.</param>
/// <param name="IsImplemented">False renders a "próximamente" placeholder page.</param>
public sealed record NavigationItem(PageKey Key, string Label, string Icon, bool IsImplemented = true);
