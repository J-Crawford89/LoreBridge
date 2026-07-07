namespace LoreBridge.Core.Workspaces;

public sealed record WorkspaceSettings(
    string WorkspaceId,
    string DisplayName,
    string VaultRootPath,
    Dictionary<string, string> Folders,
    Dictionary<string, string> StagingPaths,
    Dictionary<string, string> CanonPaths,
    IReadOnlyList<string> AllowedReadAreas,
    IReadOnlyList<string> AllowedWriteAreas);
