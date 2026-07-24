using LoreBridge.Core.Notes;

namespace LoreBridge.Core.Vault;

public sealed record VaultFolderNode(
    string Name,
    string RelativePath,
    IReadOnlyList<VaultFolderNode> Folders,
    IReadOnlyList<NoteSearchResult> Notes);
