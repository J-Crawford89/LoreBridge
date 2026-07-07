using LoreBridge.Application.Review;
using LoreBridge.Application.Workspaces;
using LoreBridge.Core.Review;
using LoreBridge.Infrastructure.Notes;

namespace LoreBridge.Infrastructure.Review;

public sealed class FileSystemReviewService(
    IWorkspaceService workspaceService,
    FileSystemNoteService noteService) : IReviewService
{
    private const string ChatImportsPathKey = "chat_imports";

    public async Task<IReadOnlyList<ReviewItem>> GetReviewQueueAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        var workspace = await workspaceService.GetWorkspaceAsync(workspaceId, cancellationToken);
        var stagingDirectory = FileSystemNoteService.GetConfiguredSubdirectory(
            workspace.VaultRootPath,
            workspace.StagingPaths,
            ChatImportsPathKey,
            "staging_paths.chat_imports");

        if (!Directory.Exists(stagingDirectory))
        {
            return [];
        }

        var reviewItems = new List<ReviewItem>();
        foreach (var notePath in Directory.EnumerateFiles(stagingDirectory, "*.md", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var note = await noteService.ReadNoteAsync(workspace, notePath, cancellationToken);
            reviewItems.Add(new ReviewItem(
                ReviewItemId: $"review.{note.NoteId}",
                WorkspaceId: workspace.WorkspaceId,
                Title: note.Title,
                SourceNoteId: note.NoteId,
                SourceRelativePath: note.RelativePath,
                ReviewStatus: "needs_review",
                CreatedUtc: note.LastModifiedUtc));
        }

        return reviewItems;
    }
}
