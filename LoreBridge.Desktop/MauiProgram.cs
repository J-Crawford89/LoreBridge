using LoreBridge.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LoreBridge.SharedUi.Markdown;
using LoreBridge.SharedUi.State;

namespace LoreBridge.Desktop
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false);
            builder.Services.AddLoreBridgeInfrastructure(builder.Configuration);
            builder.Services.AddSingleton<WorkspaceState>();
            builder.Services.AddSingleton<NoteMarkdownRenderer>();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
