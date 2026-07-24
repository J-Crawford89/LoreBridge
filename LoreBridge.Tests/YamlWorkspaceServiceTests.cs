using LoreBridge.Infrastructure.Configuration;
using LoreBridge.Infrastructure.Workspaces;
using LoreBridge.Tests.TestSupport;

namespace LoreBridge.Tests;

public sealed class YamlWorkspaceServiceTests
{
    [Fact]
    public async Task GetWorkspacesAsync_ReturnsAllConfiguredSummaries()
    {
        using var temp = new TemporaryDirectory();
        var firstRoot = temp.CreateDirectory("First");
        var secondRoot = temp.CreateDirectory("Second");
        var firstConfig = await temp.WriteFileAsync("first.yml", TestServices.CreateWorkspaceYaml("first", "First World", firstRoot));
        var secondConfig = await temp.WriteFileAsync("second.yml", TestServices.CreateWorkspaceYaml("second", "Second World", secondRoot));
        var service = CreateService(firstConfig, secondConfig);

        var summaries = await service.GetWorkspacesAsync();

        Assert.Collection(
            summaries,
            first => Assert.Equal(("first", "First World", firstRoot), (first.WorkspaceId, first.DisplayName, first.VaultRootPath)),
            second => Assert.Equal(("second", "Second World", secondRoot), (second.WorkspaceId, second.DisplayName, second.VaultRootPath)));
    }

    [Fact]
    public async Task GetWorkspaceAsync_MatchesIdCaseInsensitivelyAndMapsPermissions()
    {
        using var temp = new TemporaryDirectory();
        var root = temp.CreateDirectory("Vault");
        var config = await temp.WriteFileAsync("world.yml", TestServices.CreateWorkspaceYaml("arkellion", "Arkellion", root));
        var service = CreateService(config);

        var workspace = await service.GetWorkspaceAsync("ARKELLION");

        Assert.Equal(root, workspace.VaultRootPath);
        Assert.Equal("03_Canon", workspace.Folders["canon"]);
        Assert.Equal(["03_Canon"], workspace.AllowedReadAreas);
        Assert.Equal(["01_Staging"], workspace.AllowedWriteAreas);
    }

    [Fact]
    public async Task GetWorkspaceAsync_RejectsUnknownWorkspace()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetWorkspaceAsync("unknown"));

        Assert.Contains("was not found", exception.Message);
    }

    private static YamlWorkspaceService CreateService(params string[] configPaths) =>
        new(new LoreBridgeOptions { WorkspaceConfigPaths = configPaths }, new WorkspaceConfigLoader());
}
