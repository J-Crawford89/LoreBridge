using YamlDotNet.Serialization;

namespace LoreBridge.Infrastructure.Configuration;

public sealed class WorkspaceConfigDto
{
    [YamlMember(Alias = "workspace_id")]
    public string WorkspaceId { get; set; } = string.Empty;

    [YamlMember(Alias = "display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [YamlMember(Alias = "status")]
    public string Status { get; set; } = string.Empty;

    [YamlMember(Alias = "vault")]
    public WorkspaceVaultConfigDto Vault { get; set; } = new();

    [YamlMember(Alias = "folders")]
    public Dictionary<string, string> Folders { get; set; } = [];

    [YamlMember(Alias = "staging_paths")]
    public Dictionary<string, string> StagingPaths { get; set; } = [];

    [YamlMember(Alias = "canon_paths")]
    public Dictionary<string, string> CanonPaths { get; set; } = [];

    [YamlMember(Alias = "mcp_permissions")]
    public WorkspaceMcpPermissionsDto McpPermissions { get; set; } = new();
}

public sealed class WorkspaceVaultConfigDto
{
    [YamlMember(Alias = "root_path")]
    public string RootPath { get; set; } = string.Empty;
}

public sealed class WorkspaceMcpPermissionsDto
{
    [YamlMember(Alias = "can_read")]
    public List<string> CanRead { get; set; } = [];

    [YamlMember(Alias = "can_write")]
    public List<string> CanWrite { get; set; } = [];
}
