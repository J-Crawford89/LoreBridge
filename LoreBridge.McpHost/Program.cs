using LoreBridge.Infrastructure.DependencyInjection;
using LoreBridge.McpHost.Tools;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:3001");
builder.Logging.AddConsole();

builder.Services.AddLoreBridgeInfrastructure(builder.Configuration);
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "LoreBridge.McpHost",
            Title = "LoreBridge MCP Host",
            Version = "0.1.0"
        };
        options.ServerInstructions = "Use these tools to search and read LoreBridge notes, and to create staging notes only.";
    })
    .WithHttpTransport()
    .WithTools<LoreBridgeTools>();

var app = builder.Build();

app.MapGet("/", () => "LoreBridge MCP host is running. MCP endpoint: /mcp");
app.MapMcp("/mcp");

app.Run();
