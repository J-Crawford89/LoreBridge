namespace LoreBridge.Core.Notes;

public sealed record CreateStagingNoteRequest(
    string WorkspaceId,
    string Title,
    string Markdown,
    IReadOnlyList<string> Tags);
