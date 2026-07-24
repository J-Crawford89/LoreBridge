using LoreBridge.Application.Workspaces;
using LoreBridge.Core.Workspaces;

namespace LoreBridge.SharedUi.State;

public sealed class WorkspaceState(IWorkspaceService workspaceService)
{
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private IReadOnlyList<WorkspaceSummary> workspaces = [];

    public event Action? Changed;

    public bool IsInitialized { get; private set; }

    public IReadOnlyList<WorkspaceSummary> Workspaces => workspaces;

    public WorkspaceSummary? SelectedWorkspace { get; private set; }

    public string? SelectedWorkspaceId => SelectedWorkspace?.WorkspaceId;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return;
        }

        await initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (IsInitialized)
            {
                return;
            }

            workspaces = await workspaceService.GetWorkspacesAsync(cancellationToken);
            SelectedWorkspace = workspaces.FirstOrDefault();
            IsInitialized = true;
            Changed?.Invoke();
        }
        finally
        {
            initializationLock.Release();
        }
    }

    public bool Select(string workspaceId)
    {
        var workspace = workspaces.FirstOrDefault(candidate =>
            string.Equals(candidate.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase));
        if (workspace is null)
        {
            return false;
        }

        if (string.Equals(SelectedWorkspace?.WorkspaceId, workspace.WorkspaceId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        SelectedWorkspace = workspace;
        Changed?.Invoke();
        return true;
    }
}
