using LoreBridge.Application.Notes;
using LoreBridge.Application.Review;
using LoreBridge.Application.Vault;
using LoreBridge.Application.Workspaces;
using LoreBridge.Infrastructure.Configuration;
using LoreBridge.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoreBridge.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddLoreBridgeInfrastructure_RegistersAllApplicationServicesAndOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LoreBridge:WorkspaceConfigPaths:0"] = "C:/one.yml",
                ["LoreBridge:WorkspaceConfigPaths:1"] = "",
                ["LoreBridge:WorkspaceConfigPaths:2"] = "C:/two.yml"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddLoreBridgeInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IWorkspaceService>());
        Assert.NotNull(provider.GetRequiredService<INoteService>());
        Assert.NotNull(provider.GetRequiredService<IReviewService>());
        Assert.NotNull(provider.GetRequiredService<IVaultService>());
        Assert.Equal(["C:/one.yml", "C:/two.yml"], provider.GetRequiredService<LoreBridgeOptions>().WorkspaceConfigPaths);
    }

    [Fact]
    public void AddLoreBridgeInfrastructure_UsesSingletonLifetimesForLocalServices()
    {
        var services = new ServiceCollection();
        services.AddLoreBridgeInfrastructure(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();

        Assert.Same(provider.GetRequiredService<INoteService>(), provider.GetRequiredService<INoteService>());
        Assert.Same(provider.GetRequiredService<IVaultService>(), provider.GetRequiredService<IVaultService>());
    }
}
