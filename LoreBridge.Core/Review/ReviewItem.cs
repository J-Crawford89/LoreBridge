namespace LoreBridge.Core.Review;

public sealed record ReviewItem(
    string ReviewItemId,
    string WorkspaceId,
    string Title,
    string SourceNoteId,
    string SourceRelativePath,
    string ReviewStatus,
    DateTimeOffset CreatedUtc);
