using LoreBridge.Application.Workspaces;
using LoreBridge.Core.Workspaces;

namespace LoreBridge.Tests.TestSupport;

public sealed class StubWorkspaceService(params WorkspaceSettings[] workspaces) : IWorkspaceService
{
    private readonly IReadOnlyList<WorkspaceSettings> configuredWorkspaces = workspaces;

    public int GetWorkspacesCallCount { get; private set; }

    public Task<IReadOnlyList<WorkspaceSummary>> GetWorkspacesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GetWorkspacesCallCount++;
        return Task.FromResult<IReadOnlyList<WorkspaceSummary>>(configuredWorkspaces
            .Select(workspace => new WorkspaceSummary(workspace.WorkspaceId, workspace.DisplayName, workspace.VaultRootPath))
            .ToList());
    }

    public Task<WorkspaceSettings> GetWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(configuredWorkspaces.FirstOrDefault(workspace =>
            string.Equals(workspace.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Workspace '{workspaceId}' was not found."));
    }

    public static WorkspaceSettings CreateSettings(
        string rootPath,
        string workspaceId = "test",
        string displayName = "Test World",
        Dictionary<string, string>? stagingPaths = null,
        Dictionary<string, string>? canonPaths = null) =>
        new(
            workspaceId,
            displayName,
            rootPath,
            new Dictionary<string, string>
            {
                ["inbox"] = "00_Inbox",
                ["staging"] = "01_Staging",
                ["review"] = "02_Review",
                ["canon"] = "03_Canon"
            },
            stagingPaths ?? new Dictionary<string, string> { ["chat_imports"] = "01_Staging/Chat_Imports" },
            canonPaths ?? new Dictionary<string, string> { ["root"] = "03_Canon" },
            ["00_Inbox", "01_Staging", "02_Review", "03_Canon"],
            ["01_Staging", "02_Review"]);
}
