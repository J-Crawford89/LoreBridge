namespace LoreBridge.Core.Notes;

public sealed record NoteSearchRequest(
    string WorkspaceId,
    string Query,
    string? Status,
    string? Type,
    int Limit);
