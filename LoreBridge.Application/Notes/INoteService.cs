using LoreBridge.Core.Notes;

namespace LoreBridge.Application.Notes;

public interface INoteService
{
    Task<IReadOnlyList<NoteSearchResult>> SearchAsync(NoteSearchRequest request, CancellationToken cancellationToken = default);

    Task<NoteDetail> GetNoteAsync(string workspaceId, string noteIdOrRelativePath, CancellationToken cancellationToken = default);

    Task<CreateNoteResult> CreateStagingNoteAsync(CreateStagingNoteRequest request, CancellationToken cancellationToken = default);
}
