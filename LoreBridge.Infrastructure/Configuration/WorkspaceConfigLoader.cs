using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LoreBridge.Infrastructure.Configuration;

public sealed class WorkspaceConfigLoader
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public async Task<IReadOnlyList<WorkspaceConfigDto>> LoadAsync(
        IReadOnlyList<string> workspaceConfigPaths,
        CancellationToken cancellationToken = default)
    {
        var workspaces = new List<WorkspaceConfigDto>();

        foreach (var configPath in workspaceConfigPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(configPath))
            {
                continue;
            }

            if (!Path.IsPathFullyQualified(configPath))
            {
                throw new InvalidOperationException($"Workspace config path must be absolute: '{configPath}'.");
            }

            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"Workspace config file was not found: '{configPath}'.", configPath);
            }

            var yaml = await File.ReadAllTextAsync(configPath, cancellationToken);
            var workspace = _deserializer.Deserialize<WorkspaceConfigDto>(yaml)
                ?? throw new InvalidOperationException($"Workspace config file is empty or invalid: '{configPath}'.");

            if (string.IsNullOrWhiteSpace(workspace.WorkspaceId))
            {
                throw new InvalidOperationException($"Workspace config file is missing workspace_id: '{configPath}'.");
            }

            if (string.IsNullOrWhiteSpace(workspace.Vault.RootPath))
            {
                throw new InvalidOperationException($"Workspace config '{workspace.WorkspaceId}' is missing vault.root_path.");
            }

            workspaces.Add(workspace);
        }

        return workspaces;
    }
}
