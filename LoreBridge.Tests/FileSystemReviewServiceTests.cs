using LoreBridge.Infrastructure.Review;
using LoreBridge.Tests.TestSupport;

namespace LoreBridge.Tests;

public sealed class FileSystemReviewServiceTests
{
    [Fact]
    public async Task GetReviewQueueAsync_ReturnsEmptyWhenStagingDirectoryDoesNotExist()
    {
        using var temp = new TemporaryDirectory();
        var workspaceService = new StubWorkspaceService(StubWorkspaceService.CreateSettings(temp.Path));
        var service = new FileSystemReviewService(workspaceService, TestServices.CreateNoteService(workspaceService));

        var result = await service.GetReviewQueueAsync("test");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetReviewQueueAsync_FindsNestedStagingNotesAndMapsReviewItems()
    {
        using var temp = new TemporaryDirectory();
        await temp.WriteFileAsync("01_Staging/Chat_Imports/one.md", "---\nid: staging.one\ntitle: First Idea\n---\nBody");
        await temp.WriteFileAsync("01_Staging/Chat_Imports/Nested/two.md", "---\nid: staging.two\ntitle: Second Idea\n---\nBody");
        await temp.WriteFileAsync("01_Staging/Other/ignored.md", "---\nid: ignored\ntitle: Ignored\n---\nBody");
        var workspaceService = new StubWorkspaceService(StubWorkspaceService.CreateSettings(temp.Path));
        var service = new FileSystemReviewService(workspaceService, TestServices.CreateNoteService(workspaceService));

        var result = await service.GetReviewQueueAsync("test");

        Assert.Equal(2, result.Count);
        var first = Assert.Single(result, item => item.SourceNoteId == "staging.one");
        Assert.Equal("review.staging.one", first.ReviewItemId);
        Assert.Equal("test", first.WorkspaceId);
        Assert.Equal("First Idea", first.Title);
        Assert.Equal("needs_review", first.ReviewStatus);
        Assert.Equal("01_Staging/Chat_Imports/one.md", first.SourceRelativePath);
    }

    [Fact]
    public async Task GetReviewQueueAsync_RequiresChatImportsConfiguration()
    {
        using var temp = new TemporaryDirectory();
        var settings = StubWorkspaceService.CreateSettings(temp.Path, stagingPaths: []);
        var workspaceService = new StubWorkspaceService(settings);
        var service = new FileSystemReviewService(workspaceService, TestServices.CreateNoteService(workspaceService));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetReviewQueueAsync("test"));
    }
}
