namespace LoreBridge.Core.Notes;

public sealed record CreateNoteResult(
    string NoteId,
    string RelativePath,
    bool Created);
