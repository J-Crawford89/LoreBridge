using LoreBridge.Infrastructure.Configuration;
using LoreBridge.Tests.TestSupport;

namespace LoreBridge.Tests;

public sealed class WorkspaceConfigLoaderTests
{
    private readonly WorkspaceConfigLoader loader = new();

    [Fact]
    public async Task LoadAsync_ParsesWorkspaceConfiguration()
    {
        using var temp = new TemporaryDirectory();
        var vaultPath = temp.CreateDirectory("Vault");
        var configPath = await temp.WriteFileAsync(
            "world.workspace.yml",
            TestServices.CreateWorkspaceYaml("arkellion", "Arkellion", vaultPath));

        var result = Assert.Single(await loader.LoadAsync([configPath]));

        Assert.Equal("arkellion", result.WorkspaceId);
        Assert.Equal("Arkellion", result.DisplayName);
        Assert.Equal(vaultPath, result.Vault.RootPath);
        Assert.Equal("01_Staging/Chat_Imports", result.StagingPaths["chat_imports"]);
        Assert.Equal(["03_Canon"], result.McpPermissions.CanRead);
        Assert.Equal(["01_Staging"], result.McpPermissions.CanWrite);
    }

    [Fact]
    public async Task LoadAsync_SkipsBlankPaths()
    {
        var result = await loader.LoadAsync(["", "   "]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadAsync_RejectsRelativeConfigPaths()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            loader.LoadAsync(["relative.workspace.yml"]));

        Assert.Contains("must be absolute", exception.Message);
    }

    [Fact]
    public async Task LoadAsync_RejectsMissingConfigFiles()
    {
        using var temp = new TemporaryDirectory();
        var missingPath = temp.GetPath("missing.workspace.yml");

        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() => loader.LoadAsync([missingPath]));

        Assert.Equal(missingPath, exception.FileName);
    }

    [Theory]
    [InlineData("display_name: Missing ID\nvault:\n  root_path: C:/Vault", "workspace_id")]
    [InlineData("workspace_id: missing-root\nvault: {}", "vault.root_path")]
    public async Task LoadAsync_RejectsRequiredValuesThatAreMissing(string yaml, string expectedMessage)
    {
        using var temp = new TemporaryDirectory();
        var configPath = await temp.WriteFileAsync("invalid.workspace.yml", yaml);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => loader.LoadAsync([configPath]));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public async Task LoadAsync_HonorsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => loader.LoadAsync(["ignored"], cancellation.Token));
    }
}
