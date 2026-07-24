using LoreBridge.Infrastructure.Notes;

namespace LoreBridge.Tests.TestSupport;

public static class TestServices
{
    public static FileSystemNoteService CreateNoteService(StubWorkspaceService workspaceService) =>
        new(workspaceService, new MarkdownFrontmatterParser());

    public static string CreateWorkspaceYaml(
        string workspaceId,
        string displayName,
        string rootPath) =>
        $$"""
        workspace_id: {{workspaceId}}
        display_name: {{displayName}}
        status: active
        vault:
          root_path: '{{rootPath.Replace("'", "''")}}'
        folders:
          canon: 03_Canon
        staging_paths:
          chat_imports: 01_Staging/Chat_Imports
        canon_paths:
          root: 03_Canon
        mcp_permissions:
          can_read:
            - 03_Canon
          can_write:
            - 01_Staging
        """;
}
