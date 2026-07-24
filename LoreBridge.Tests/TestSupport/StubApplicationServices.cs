using LoreBridge.Application.Notes;
using LoreBridge.Application.Review;
using LoreBridge.Application.Vault;
using LoreBridge.Core.Notes;
using LoreBridge.Core.Review;
using LoreBridge.Core.Vault;

namespace LoreBridge.Tests.TestSupport;

public sealed class StubNoteService : INoteService
{
    public IReadOnlyList<NoteSearchResult> SearchResults { get; set; } = [];
    public NoteDetail? NoteResult { get; set; }
    public CreateNoteResult CreateResult { get; set; } = new("staging.created", "01_Staging/created.md", true);
    public NoteSearchRequest? LastSearchRequest { get; private set; }
    public CreateStagingNoteRequest? LastCreateRequest { get; private set; }

    public Task<IReadOnlyList<NoteSearchResult>> SearchAsync(NoteSearchRequest request, CancellationToken cancellationToken = default)
    {
        LastSearchRequest = request;
        return Task.FromResult(SearchResults);
    }

    public Task<NoteDetail> GetNoteAsync(string workspaceId, string noteIdOrRelativePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(NoteResult ?? throw new FileNotFoundException(noteIdOrRelativePath));

    public Task<CreateNoteResult> CreateStagingNoteAsync(CreateStagingNoteRequest request, CancellationToken cancellationToken = default)
    {
        LastCreateRequest = request;
        return Task.FromResult(CreateResult);
    }
}

public sealed class StubReviewService : IReviewService
{
    public IReadOnlyList<ReviewItem> Items { get; set; } = [];

    public Task<IReadOnlyList<ReviewItem>> GetReviewQueueAsync(string workspaceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items);
}

public sealed class StubVaultService : IVaultService
{
    public VaultSnapshot Snapshot { get; set; } = new(
        0,
        new Dictionary<string, int>(),
        new Dictionary<string, int>(),
        new Dictionary<string, int>(),
        [],
        [],
        new VaultFolderNode("Vault", string.Empty, [], []));

    public Task<VaultSnapshot> GetSnapshotAsync(string workspaceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Snapshot);
}
