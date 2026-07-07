namespace LoreBridge.Core.Workspaces;

public sealed record WorkspaceSummary(
    string WorkspaceId,
    string DisplayName,
    string VaultRootPath);
