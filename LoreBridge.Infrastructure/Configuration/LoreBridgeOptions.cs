namespace LoreBridge.Infrastructure.Configuration;

public sealed class LoreBridgeOptions
{
    public IReadOnlyList<string> WorkspaceConfigPaths { get; init; } = [];
}
