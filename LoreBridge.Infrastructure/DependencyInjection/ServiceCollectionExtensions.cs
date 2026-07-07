using LoreBridge.Application.Notes;
using LoreBridge.Application.Review;
using LoreBridge.Application.Workspaces;
using LoreBridge.Infrastructure.Configuration;
using LoreBridge.Infrastructure.Notes;
using LoreBridge.Infrastructure.Review;
using LoreBridge.Infrastructure.Workspaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoreBridge.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLoreBridgeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var loreBridgeSection = configuration.GetSection("LoreBridge");
        var options = new LoreBridgeOptions
        {
            WorkspaceConfigPaths = loreBridgeSection
                .GetSection(nameof(LoreBridgeOptions.WorkspaceConfigPaths))
                .GetChildren()
                .Select(config => config.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToList()
        };

        services.AddSingleton(options);
        services.AddSingleton<WorkspaceConfigLoader>();
        services.AddSingleton<MarkdownFrontmatterParser>();
        services.AddSingleton<YamlWorkspaceService>();
        services.AddSingleton<IWorkspaceService>(provider => provider.GetRequiredService<YamlWorkspaceService>());
        services.AddSingleton<FileSystemNoteService>();
        services.AddSingleton<INoteService>(provider => provider.GetRequiredService<FileSystemNoteService>());
        services.AddSingleton<IReviewService, FileSystemReviewService>();

        return services;
    }
}
