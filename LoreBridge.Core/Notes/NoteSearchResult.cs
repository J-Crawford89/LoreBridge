namespace LoreBridge.Core.Notes;

public sealed record NoteSearchResult(
    string NoteId,
    string Title,
    string RelativePath,
    string? Type,
    string? Subtype,
    string? Status,
    string Snippet,
    IReadOnlyList<string> Tags,
    DateTimeOffset LastModifiedUtc);
