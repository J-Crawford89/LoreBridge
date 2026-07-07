namespace LoreBridge.Core.Notes;

public sealed record NoteDetail(
    string NoteId,
    string Title,
    string RelativePath,
    string Markdown,
    IReadOnlyDictionary<string, string> Frontmatter,
    IReadOnlyList<string> Tags,
    DateTimeOffset LastModifiedUtc);
