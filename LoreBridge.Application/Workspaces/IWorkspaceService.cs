using LoreBridge.Core.Workspaces;

namespace LoreBridge.Application.Workspaces;

public interface IWorkspaceService
{
    Task<IReadOnlyList<WorkspaceSummary>> GetWorkspacesAsync(CancellationToken cancellationToken = default);

    Task<WorkspaceSettings> GetWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default);
}
