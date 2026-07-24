using LoreBridge.Application.Notes;
using LoreBridge.Core.Notes;
using LoreBridge.McpHost.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoreBridge.Tests;

public sealed class LoreBridgeToolsTests
{
    [Fact]
    public async Task SearchLoreBridgeNotesAsync_ForwardsAllSearchParameters()
    {
        var noteService = new RecordingNoteService
        {
            SearchResult = [CreateSummary("npc.aline")]
        };
        var tools = CreateTools(noteService);

        var result = await tools.SearchLoreBridgeNotesAsync("arkellion", "aline", "canon", "npc", 7);

        Assert.Same(noteService.SearchResult, result);
        Assert.Equal(new NoteSearchRequest("arkellion", "aline", "canon", "npc", 7), noteService.LastSearchRequest);
    }

    [Fact]
    public async Task ReadLoreBridgeNoteAsync_ForwardsWorkspaceAndIdentity()
    {
        var detail = CreateDetail("npc.aline");
        var noteService = new RecordingNoteService { DetailResult = detail };
        var tools = CreateTools(noteService);

        var result = await tools.ReadLoreBridgeNoteAsync("arkellion", "03_Canon/aline.md");

        Assert.Same(detail, result);
        Assert.Equal(("arkellion", "03_Canon/aline.md"), noteService.LastReadRequest);
    }

    [Fact]
    public async Task CreateLoreBridgeStagingNoteAsync_UsesEmptyTagsWhenOmitted()
    {
        var createResult = new CreateNoteResult("staging.idea", "01_Staging/idea.md", true);
        var noteService = new RecordingNoteService { CreateResult = createResult };
        var tools = CreateTools(noteService);

        var result = await tools.CreateLoreBridgeStagingNoteAsync("arkellion", "Idea", "Body");

        Assert.Same(createResult, result);
        Assert.Equal("arkellion", noteService.LastCreateRequest!.WorkspaceId);
        Assert.Equal("Idea", noteService.LastCreateRequest.Title);
        Assert.Equal("Body", noteService.LastCreateRequest.Markdown);
        Assert.Empty(noteService.LastCreateRequest.Tags);
    }

    [Fact]
    public async Task CreateLoreBridgeStagingNoteAsync_ForwardsProvidedTags()
    {
        var noteService = new RecordingNoteService();
        var tools = CreateTools(noteService);

        await tools.CreateLoreBridgeStagingNoteAsync("arkellion", "Idea", "Body", ["npc", "draft"]);

        Assert.Equal(["npc", "draft"], noteService.LastCreateRequest!.Tags);
    }

    private static LoreBridgeTools CreateTools(INoteService noteService) =>
        new(noteService, NullLogger<LoreBridgeTools>.Instance);

    private static NoteSearchResult CreateSummary(string id) =>
        new(id, "Aline", "03_Canon/aline.md", "npc", null, "canon", "", [], DateTimeOffset.UtcNow);

    private static NoteDetail CreateDetail(string id) =>
        new(id, "Aline", "03_Canon/aline.md", "Body", new Dictionary<string, string>(), [], DateTimeOffset.UtcNow);

    private sealed class RecordingNoteService : INoteService
    {
        public IReadOnlyList<NoteSearchResult> SearchResult { get; init; } = [];
        public NoteDetail DetailResult { get; init; } = CreateDetail("default");
        public CreateNoteResult CreateResult { get; init; } = new("staging.default", "01_Staging/default.md", true);
        public NoteSearchRequest? LastSearchRequest { get; private set; }
        public (string WorkspaceId, string Identity)? LastReadRequest { get; private set; }
        public CreateStagingNoteRequest? LastCreateRequest { get; private set; }

        public Task<IReadOnlyList<NoteSearchResult>> SearchAsync(NoteSearchRequest request, CancellationToken cancellationToken = default)
        {
            LastSearchRequest = request;
            return Task.FromResult(SearchResult);
        }

        public Task<NoteDetail> GetNoteAsync(string workspaceId, string noteIdOrRelativePath, CancellationToken cancellationToken = default)
        {
            LastReadRequest = (workspaceId, noteIdOrRelativePath);
            return Task.FromResult(DetailResult);
        }

        public Task<CreateNoteResult> CreateStagingNoteAsync(CreateStagingNoteRequest request, CancellationToken cancellationToken = default)
        {
            LastCreateRequest = request;
            return Task.FromResult(CreateResult);
        }
    }
}
