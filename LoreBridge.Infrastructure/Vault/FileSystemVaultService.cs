using LoreBridge.Application.Vault;
using LoreBridge.Application.Workspaces;
using LoreBridge.Core.Notes;
using LoreBridge.Core.Vault;
using LoreBridge.Infrastructure.Notes;

namespace LoreBridge.Infrastructure.Vault;

public sealed class FileSystemVaultService(
    IWorkspaceService workspaceService,
    FileSystemNoteService noteService) : IVaultService
{
    public async Task<VaultSnapshot> GetSnapshotAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        var workspace = await workspaceService.GetWorkspaceAsync(workspaceId, cancellationToken);
        var notes = new List<NoteSearchResult>();

        foreach (var notePath in FileSystemNoteService.EnumerateMarkdownFiles(workspace.VaultRootPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var detail = await noteService.ReadNoteAsync(workspace, notePath, cancellationToken);
            notes.Add(ToSummary(detail));
        }

        var orderedNotes = notes
            .OrderBy(note => note.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rootFolder = BuildFolder(
            workspace.VaultRootPath,
            workspace.VaultRootPath,
            workspace.DisplayName,
            orderedNotes,
            cancellationToken);

        return new VaultSnapshot(
            orderedNotes.Count,
            CountBy(orderedNotes, note => note.Status),
            CountBy(orderedNotes, note => note.Type),
            orderedNotes
                .GroupBy(GetTopLevelFolder, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase),
            orderedNotes
                .OrderByDescending(note => note.LastModifiedUtc)
                .Take(6)
                .ToList(),
            orderedNotes,
            rootFolder);
    }

    private static NoteSearchResult ToSummary(NoteDetail detail)
    {
        return new NoteSearchResult(
            detail.NoteId,
            detail.Title,
            detail.RelativePath,
            GetFrontmatterValue(detail, "type"),
            GetFrontmatterValue(detail, "subtype"),
            GetFrontmatterValue(detail, "status"),
            CreateSummary(detail.Markdown),
            detail.Tags,
            detail.LastModifiedUtc);
    }

    private static VaultFolderNode BuildFolder(
        string vaultRootPath,
        string directoryPath,
        string name,
        IReadOnlyList<NoteSearchResult> allNotes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var relativePath = Path.GetRelativePath(vaultRootPath, directoryPath)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
        relativePath = relativePath == "." ? string.Empty : relativePath;

        var folders = Directory.EnumerateDirectories(directoryPath)
            .Where(path => !ShouldSkipDirectory(path))
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .Select(path => BuildFolder(
                vaultRootPath,
                path,
                Path.GetFileName(path),
                allNotes,
                cancellationToken))
            .ToList();

        var notes = allNotes
            .Where(note => string.Equals(
                GetParentPath(note.RelativePath),
                relativePath,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new VaultFolderNode(name, relativePath, folders, notes);
    }

    private static IReadOnlyDictionary<string, int> CountBy(
        IEnumerable<NoteSearchResult> notes,
        Func<NoteSearchResult, string?> selector)
    {
        return notes
            .GroupBy(note => string.IsNullOrWhiteSpace(selector(note)) ? "Unspecified" : selector(note)!, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private static string? GetFrontmatterValue(NoteDetail detail, string key)
    {
        return detail.Frontmatter.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private static string CreateSummary(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var summary = string.Join(' ', markdown
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !line.StartsWith('#')));
        return summary.Length <= 180 ? summary : summary[..180];
    }

    private static string GetTopLevelFolder(NoteSearchResult note)
    {
        var separator = note.RelativePath.IndexOf('/');
        return separator < 0 ? "Vault root" : note.RelativePath[..separator];
    }

    private static string GetParentPath(string relativePath)
    {
        var separator = relativePath.LastIndexOf('/');
        return separator < 0 ? string.Empty : relativePath[..separator];
    }

    private static bool ShouldSkipDirectory(string directoryPath)
    {
        var name = Path.GetFileName(directoryPath);
        return string.Equals(name, ".obsidian", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "99_System", StringComparison.OrdinalIgnoreCase);
    }
}
