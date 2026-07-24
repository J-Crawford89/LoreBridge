using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using LoreBridge.Core.Notes;
using Markdig;

namespace LoreBridge.SharedUi.Markdown;

public sealed partial class NoteMarkdownRenderer
{
    private readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public string Render(NoteDetail note, IReadOnlyList<NoteSearchResult> vaultNotes)
    {
        var withWikiLinks = ReplaceWikiLinks(note, vaultNotes);
        return SanitizeLinks(Markdig.Markdown.ToHtml(withWikiLinks, pipeline));
    }

    private static string ReplaceWikiLinks(NoteDetail currentNote, IReadOnlyList<NoteSearchResult> vaultNotes)
    {
        var output = new StringBuilder();
        var inFence = false;

        foreach (var line in currentNote.Markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                inFence = !inFence;
                output.AppendLine(line);
                continue;
            }

            output.AppendLine(inFence ? line : ReplaceOutsideInlineCode(line, currentNote, vaultNotes));
        }

        return output.ToString();
    }

    private static string ReplaceOutsideInlineCode(
        string line,
        NoteDetail currentNote,
        IReadOnlyList<NoteSearchResult> vaultNotes)
    {
        var output = new StringBuilder();
        var segmentStart = 0;
        var inCode = false;

        for (var index = 0; index < line.Length; index++)
        {
            if (line[index] != '`')
            {
                continue;
            }

            var segment = line[segmentStart..index];
            output.Append(inCode ? segment : ReplaceLinks(segment, currentNote, vaultNotes));
            output.Append('`');
            inCode = !inCode;
            segmentStart = index + 1;
        }

        var remainder = line[segmentStart..];
        output.Append(inCode ? remainder : ReplaceLinks(remainder, currentNote, vaultNotes));
        return output.ToString();
    }

    private static string ReplaceLinks(
        string text,
        NoteDetail currentNote,
        IReadOnlyList<NoteSearchResult> vaultNotes)
    {
        return WikiLinkRegex().Replace(text, match =>
        {
            var rawTarget = match.Groups["target"].Value.Trim();
            var label = match.Groups["label"].Success
                ? match.Groups["label"].Value.Trim()
                : rawTarget;

            var headingSeparator = rawTarget.IndexOf('#');
            var noteTarget = headingSeparator >= 0 ? rawTarget[..headingSeparator].Trim() : rawTarget;
            var heading = headingSeparator >= 0 ? rawTarget[(headingSeparator + 1)..].Trim() : null;

            NoteSearchResult? targetNote;
            if (string.IsNullOrWhiteSpace(noteTarget))
            {
                targetNote = new NoteSearchResult(
                    currentNote.NoteId,
                    currentNote.Title,
                    currentNote.RelativePath,
                    null,
                    null,
                    null,
                    string.Empty,
                    currentNote.Tags,
                    currentNote.LastModifiedUtc);
            }
            else
            {
                targetNote = FindNote(noteTarget, vaultNotes);
            }

            if (targetNote is null)
            {
                return match.Value;
            }

            var href = $"/notes/{Uri.EscapeDataString(targetNote.NoteId)}";
            if (!string.IsNullOrWhiteSpace(heading))
            {
                href += $"#{CreateHeadingSlug(heading)}";
            }

            return $"[{EscapeLabel(label)}]({href})";
        });
    }

    private static NoteSearchResult? FindNote(string target, IReadOnlyList<NoteSearchResult> notes)
    {
        var normalizedPath = target.Replace('\\', '/');
        return notes.FirstOrDefault(note =>
            string.Equals(note.NoteId, target, StringComparison.OrdinalIgnoreCase)
            || string.Equals(note.Title, target, StringComparison.OrdinalIgnoreCase)
            || string.Equals(note.RelativePath, normalizedPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.ChangeExtension(note.RelativePath, null), normalizedPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetFileNameWithoutExtension(note.RelativePath), target, StringComparison.OrdinalIgnoreCase));
    }

    private static string CreateHeadingSlug(string heading)
    {
        return Regex.Replace(heading.Trim().ToLowerInvariant(), @"[^\p{L}\p{N}\s-]", string.Empty)
            .Replace(' ', '-');
    }

    private static string EscapeLabel(string label) => label
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal)
        .Replace("]", "\\]", StringComparison.Ordinal);

    private static string SanitizeLinks(string html)
    {
        return HtmlHrefRegex().Replace(html, match =>
        {
            var decodedValue = WebUtility.HtmlDecode(match.Groups["value"].Value).Trim();
            if (decodedValue.StartsWith("/", StringComparison.Ordinal)
                    && !decodedValue.StartsWith("//", StringComparison.Ordinal)
                || decodedValue.StartsWith("#", StringComparison.Ordinal)
                || Uri.TryCreate(decodedValue, UriKind.Relative, out _)
                || Uri.TryCreate(decodedValue, UriKind.Absolute, out var uri)
                    && uri.Scheme is "http" or "https" or "mailto")
            {
                return match.Value;
            }

            return string.Empty;
        });
    }

    [GeneratedRegex(@"(?<!!)\[\[(?<target>[^\]|]*?)(?:\|(?<label>[^\]]+))?\]\]")]
    private static partial Regex WikiLinkRegex();

    [GeneratedRegex("\\s+href=\\\"(?<value>[^\\\"]*)\\\"", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlHrefRegex();
}
