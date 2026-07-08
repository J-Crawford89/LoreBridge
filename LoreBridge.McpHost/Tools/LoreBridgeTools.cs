using System.ComponentModel;
using LoreBridge.Application.Notes;
using LoreBridge.Core.Notes;
using ModelContextProtocol.Server;

namespace LoreBridge.McpHost.Tools;

public sealed class LoreBridgeTools(
    INoteService noteService,
    ILogger<LoreBridgeTools> logger)
{
    [McpServerTool(Name = "search_lorebridge_notes", Title = "Search LoreBridge Notes", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Search Markdown notes in a configured LoreBridge workspace. Requires workspaceId.")]
    public async Task<IReadOnlyList<NoteSearchResult>> SearchLoreBridgeNotesAsync(
        [Description("Configured LoreBridge workspace id.")] string workspaceId,
        [Description("Text query to search for. Use an empty string to list matching notes by filters.")] string query,
        [Description("Optional frontmatter status filter, such as canon or staging.")] string? status = null,
        [Description("Optional frontmatter type filter, such as npc or location.")] string? type = null,
        [Description("Maximum number of notes to return.")] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "MCP tool search_lorebridge_notes called for workspace {WorkspaceId} with query {Query}, status {Status}, type {Type}, limit {Limit}.",
            workspaceId,
            query,
            status,
            type,
            limit);

        return await noteService.SearchAsync(
            new NoteSearchRequest(workspaceId, query, status, type, limit),
            cancellationToken);
    }

    [McpServerTool(Name = "read_lorebridge_note", Title = "Read LoreBridge Note", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Read a single Markdown note in a configured LoreBridge workspace by frontmatter id or relative path. Requires workspaceId.")]
    public async Task<NoteDetail> ReadLoreBridgeNoteAsync(
        [Description("Configured LoreBridge workspace id.")] string workspaceId,
        [Description("Frontmatter id or vault-relative Markdown path for the note.")] string noteIdOrRelativePath,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "MCP tool read_lorebridge_note called for workspace {WorkspaceId} and note {NoteIdOrRelativePath}.",
            workspaceId,
            noteIdOrRelativePath);

        return await noteService.GetNoteAsync(workspaceId, noteIdOrRelativePath, cancellationToken);
    }

    [McpServerTool(Name = "create_lorebridge_staging_note", Title = "Create LoreBridge Staging Note", ReadOnly = false, Destructive = false, OpenWorld = false)]
    [Description("Create a new staging note in the configured chat imports staging path. This tool cannot overwrite Canon or delete notes.")]
    public async Task<CreateNoteResult> CreateLoreBridgeStagingNoteAsync(
        [Description("Configured LoreBridge workspace id.")] string workspaceId,
        [Description("Title for the new staging note.")] string title,
        [Description("Markdown body for the new staging note.")] string markdown,
        [Description("Optional tags to add to note frontmatter.")] IReadOnlyList<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "MCP tool create_lorebridge_staging_note called for workspace {WorkspaceId} with title {Title}.",
            workspaceId,
            title);

        return await noteService.CreateStagingNoteAsync(
            new CreateStagingNoteRequest(workspaceId, title, markdown, tags ?? []),
            cancellationToken);
    }
}
