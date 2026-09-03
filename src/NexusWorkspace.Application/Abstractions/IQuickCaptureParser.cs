using NexusWorkspace.Application.QuickCapture;

namespace NexusWorkspace.Application.Abstractions;

/// <summary>
/// Interprets a quick-capture line locally, by rules — no network, no AI. The
/// interface is intentionally AI-ready: a future implementation could call a
/// local model, but the contract (a non-destructive set of suggestions) stays.
/// </summary>
public interface IQuickCaptureParser
{
    QuickCaptureParseResult Parse(string input, IReadOnlyCollection<ProjectNameRef>? knownProjects = null);
}
