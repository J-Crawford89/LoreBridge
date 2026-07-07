using System.Text;
using System.Text.RegularExpressions;
using LoreBridge.Application.Notes;
using LoreBridge.Application.Workspaces;
using LoreBridge.Core.Notes;
using LoreBridge.Core.Workspaces;

namespace LoreBridge.Infrastructure.Notes;

public sealed class FileSystemNoteService(
    IWorkspaceService workspaceService,
    MarkdownFrontmatterParser frontmatterParser) : INoteService
{
    private const string ChatImportsPathKey = "chat_imports";

    public async Task<IReadOnlyList<NoteSearchResult>> SearchAsync(
        NoteSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspace = await workspaceService.GetWorkspaceAsync(request.WorkspaceId, cancellationToken);
        var results = new List<NoteSearchResult>();
        var limit = request.Limit > 0 ? request.Limit : 50;

        foreach (var notePath in EnumerateMarkdownFiles(workspace.VaultRootPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var detail = await ReadNoteAsync(workspace, notePath, cancellationToken);
            if (!MatchesSearch(detail, request))
            {
                continue;
            }

            results.Add(new NoteSearchResult(
                detail.NoteId,
                detail.Title,
                detail.RelativePath,
                GetFrontmatterValue(detail.Frontmatter, "type"),
                GetFrontmatterValue(detail.Frontmatter, "subtype"),
                GetFrontmatterValue(detail.Frontmatter, "status"),
                CreateSnippet(detail.Markdown, request.Query),
                detail.Tags,
                detail.LastModifiedUtc));

            if (results.Count >= limit)
            {
                break;
            }
        }

        return results;
    }

    public async Task<NoteDetail> GetNoteAsync(
        string workspaceId,
        string noteIdOrRelativePath,
        CancellationToken cancellationToken = default)
    {
        var workspace = await workspaceService.GetWorkspaceAsync(workspaceId, cancellationToken);

        if (LooksLikeRelativeMarkdownPath(noteIdOrRelativePath))
        {
            var path = ResolveVaultRelativePath(workspace.VaultRootPath, noteIdOrRelativePath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Note was not found: '{noteIdOrRelativePath}'.", path);
            }

            return await ReadNoteAsync(workspace, path, cancellationToken);
        }

        foreach (var notePath in EnumerateMarkdownFiles(workspace.VaultRootPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var detail = await ReadNoteAsync(workspace, notePath, cancellationToken);
            if (string.Equals(detail.NoteId, noteIdOrRelativePath, StringComparison.OrdinalIgnoreCase))
            {
                return detail;
            }
        }

        throw new FileNotFoundException($"Note '{noteIdOrRelativePath}' was not found in workspace '{workspaceId}'.");
    }

    public async Task<CreateNoteResult> CreateStagingNoteAsync(
        CreateStagingNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspace = await workspaceService.GetWorkspaceAsync(request.WorkspaceId, cancellationToken);
        var stagingDirectory = GetConfiguredSubdirectory(
            workspace.VaultRootPath,
            workspace.StagingPaths,
            ChatImportsPathKey,
            "staging_paths.chat_imports");

        EnsureNotCanonPath(workspace, stagingDirectory);
        Directory.CreateDirectory(stagingDirectory);

        var noteId = CreateNoteId(request.Title);
        var fileName = CreateSafeFileName(request.Title);
        var targetPath = GetAvailablePath(stagingDirectory, fileName);
        var relativePath = GetVaultRelativePath(workspace.VaultRootPath, targetPath);
        var markdown = BuildStagingMarkdown(noteId, request);

        await File.WriteAllTextAsync(targetPath, markdown, Encoding.UTF8, cancellationToken);

        return new CreateNoteResult(noteId, relativePath, Created: true);
    }

    internal async Task<NoteDetail> ReadNoteAsync(
        WorkspaceSettings workspace,
        string notePath,
        CancellationToken cancellationToken = default)
    {
        var markdown = await File.ReadAllTextAsync(notePath, cancellationToken);
        var parsed = frontmatterParser.Parse(markdown);
        var relativePath = GetVaultRelativePath(workspace.VaultRootPath, notePath);

        return new NoteDetail(
            GetFrontmatterValue(parsed.Frontmatter, "id") ?? NormalizeRelativePathAsId(relativePath),
            GetFrontmatterValue(parsed.Frontmatter, "title") ?? Path.GetFileNameWithoutExtension(notePath),
            relativePath,
            parsed.MarkdownBody,
            parsed.Frontmatter,
            parsed.Tags,
            File.GetLastWriteTimeUtc(notePath));
    }

    internal static IEnumerable<string> EnumerateMarkdownFiles(string vaultRootPath)
    {
        if (!Directory.Exists(vaultRootPath))
        {
            throw new DirectoryNotFoundException($"Workspace vault root was not found: '{vaultRootPath}'.");
        }

        var pending = new Stack<string>();
        pending.Push(Path.GetFullPath(vaultRootPath));

        while (pending.Count > 0)
        {
            var directory = pending.Pop();

            foreach (var childDirectory in Directory.EnumerateDirectories(directory))
            {
                var name = Path.GetFileName(childDirectory);
                if (string.Equals(name, ".obsidian", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "99_System", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                pending.Push(childDirectory);
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*.md"))
            {
                yield return file;
            }
        }
    }

    internal static string GetConfiguredSubdirectory(
        string vaultRootPath,
        IReadOnlyDictionary<string, string> configuredPaths,
        string key,
        string displayName)
    {
        if (!configuredPaths.TryGetValue(key, out var relativePath) || string.IsNullOrWhiteSpace(relativePath))
        {
            throw new InvalidOperationException($"Workspace config is missing {displayName}.");
        }

        return ResolveVaultRelativePath(vaultRootPath, relativePath);
    }

    internal static string GetVaultRelativePath(string vaultRootPath, string fullPath)
    {
        return Path.GetRelativePath(Path.GetFullPath(vaultRootPath), Path.GetFullPath(fullPath))
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }

    private static bool MatchesSearch(NoteDetail detail, NoteSearchRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Status)
            && !string.Equals(GetFrontmatterValue(detail.Frontmatter, "status"), request.Status, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.Type)
            && !string.Equals(GetFrontmatterValue(detail.Frontmatter, "type"), request.Type, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return true;
        }

        return detail.Title.Contains(request.Query, StringComparison.OrdinalIgnoreCase)
            || detail.Markdown.Contains(request.Query, StringComparison.OrdinalIgnoreCase)
            || detail.RelativePath.Contains(request.Query, StringComparison.OrdinalIgnoreCase)
            || detail.Tags.Any(tag => tag.Contains(request.Query, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetFrontmatterValue(IReadOnlyDictionary<string, string> frontmatter, string key)
    {
        return frontmatter.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private static string CreateSnippet(string markdown, string query)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var normalized = Regex.Replace(markdown.Trim(), @"\s+", " ");
        if (string.IsNullOrWhiteSpace(query))
        {
            return normalized.Length <= 180 ? normalized : normalized[..180];
        }

        var index = normalized.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return normalized.Length <= 180 ? normalized : normalized[..180];
        }

        var start = Math.Max(0, index - 60);
        var length = Math.Min(180, normalized.Length - start);
        return normalized.Substring(start, length);
    }

    private static bool LooksLikeRelativeMarkdownPath(string value)
    {
        return value.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            || value.Contains('/', StringComparison.Ordinal)
            || value.Contains('\\', StringComparison.Ordinal);
    }

    private static string ResolveVaultRelativePath(string vaultRootPath, string relativePath)
    {
        if (Path.IsPathFullyQualified(relativePath))
        {
            throw new InvalidOperationException($"Vault-relative path must not be absolute: '{relativePath}'.");
        }

        var segments = relativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => segment == ".."))
        {
            throw new InvalidOperationException($"Vault-relative path must not contain '..': '{relativePath}'.");
        }

        var root = Path.GetFullPath(vaultRootPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!fullPath.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Resolved path is outside the workspace vault: '{relativePath}'.");
        }

        return fullPath;
    }

    private static void EnsureNotCanonPath(WorkspaceSettings workspace, string targetDirectory)
    {
        var targetFullPath = Path.GetFullPath(targetDirectory);
        foreach (var canonPath in workspace.CanonPaths.Values.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var canonFullPath = ResolveVaultRelativePath(workspace.VaultRootPath, canonPath);
            if (targetFullPath.StartsWith(canonFullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || string.Equals(targetFullPath, canonFullPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("CreateStagingNoteAsync cannot write to a configured Canon path.");
            }
        }
    }

    private static string GetAvailablePath(string directory, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var candidate = Path.Combine(directory, fileName);
        var suffix = 2;

        while (File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{baseName}-{suffix}{extension}");
            suffix++;
        }

        return candidate;
    }

    private static string BuildStagingMarkdown(string noteId, CreateStagingNoteRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("---");
        builder.AppendLine($"id: {EscapeYamlScalar(noteId)}");
        builder.AppendLine($"title: {EscapeYamlScalar(request.Title)}");
        builder.AppendLine("status: staging");
        builder.AppendLine("tags:");

        foreach (var tag in request.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)))
        {
            builder.AppendLine($"  - {EscapeYamlScalar(tag.Trim())}");
        }

        builder.AppendLine("---");
        builder.AppendLine();
        builder.Append(request.Markdown);

        return builder.ToString();
    }

    private static string CreateNoteId(string title)
    {
        return $"staging.{Path.GetFileNameWithoutExtension(CreateSafeFileName(title))}";
    }

    private static string CreateSafeFileName(string title)
    {
        var normalized = title.Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^a-z0-9]+", "_").Trim('_');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = $"untitled_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        }

        return $"{normalized}.md";
    }

    private static string NormalizeRelativePathAsId(string relativePath)
    {
        return Path.ChangeExtension(relativePath, null)
            .Replace('\\', '/')
            .Replace('/', '.')
            .Replace(' ', '_')
            .ToLowerInvariant();
    }

    private static string EscapeYamlScalar(string value)
    {
        return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }
}
