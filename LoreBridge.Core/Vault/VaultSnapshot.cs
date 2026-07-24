using LoreBridge.Core.Notes;

namespace LoreBridge.Core.Vault;

public sealed record VaultSnapshot(
    int TotalNotes,
    IReadOnlyDictionary<string, int> StatusCounts,
    IReadOnlyDictionary<string, int> TypeCounts,
    IReadOnlyDictionary<string, int> TopLevelFolderCounts,
    IReadOnlyList<NoteSearchResult> RecentNotes,
    IReadOnlyList<NoteSearchResult> Notes,
    VaultFolderNode RootFolder);
