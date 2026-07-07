using LoreBridge.Application.Workspaces;
using LoreBridge.Core.Workspaces;
using LoreBridge.Infrastructure.Configuration;

namespace LoreBridge.Infrastructure.Workspaces;

public sealed class YamlWorkspaceService(
    LoreBridgeOptions options,
    WorkspaceConfigLoader workspaceConfigLoader) : IWorkspaceService
{
    public async Task<IReadOnlyList<WorkspaceSummary>> GetWorkspacesAsync(CancellationToken cancellationToken = default)
    {
        var workspaces = await LoadWorkspaceSettingsAsync(cancellationToken);

        return workspaces
            .Select(workspace => new WorkspaceSummary(
                workspace.WorkspaceId,
                workspace.DisplayName,
                workspace.VaultRootPath))
            .ToList();
    }

    public async Task<WorkspaceSettings> GetWorkspaceAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        var workspaces = await LoadWorkspaceSettingsAsync(cancellationToken);
        var workspace = workspaces.FirstOrDefault(candidate =>
            string.Equals(candidate.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase));

        return workspace
            ?? throw new InvalidOperationException($"Workspace '{workspaceId}' was not found in configured workspace config paths.");
    }

    private async Task<IReadOnlyList<WorkspaceSettings>> LoadWorkspaceSettingsAsync(CancellationToken cancellationToken)
    {
        var configs = await workspaceConfigLoader.LoadAsync(options.WorkspaceConfigPaths, cancellationToken);

        return configs
            .Select(config => new WorkspaceSettings(
                config.WorkspaceId,
                config.DisplayName,
                config.Vault.RootPath,
                config.Folders,
                config.StagingPaths,
                config.CanonPaths,
                config.McpPermissions.CanRead,
                config.McpPermissions.CanWrite))
            .ToList();
    }
}
