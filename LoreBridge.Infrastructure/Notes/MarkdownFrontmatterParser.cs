using YamlDotNet.Serialization;

namespace LoreBridge.Infrastructure.Notes;

public sealed class MarkdownFrontmatterParser
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .Build();

    public ParsedMarkdownNote Parse(string markdown)
    {
        if (!TrySplitFrontmatter(markdown, out var yaml, out var body))
        {
            return new ParsedMarkdownNote(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), markdown, []);
        }

        var frontmatter = ParseFrontmatter(yaml);
        var tags = ExtractTags(frontmatter, yaml);

        return new ParsedMarkdownNote(frontmatter, body, tags);
    }

    private static bool TrySplitFrontmatter(string markdown, out string yaml, out string body)
    {
        yaml = string.Empty;
        body = markdown;

        using var reader = new StringReader(markdown);
        var firstLine = reader.ReadLine();
        if (!string.Equals(firstLine, "---", StringComparison.Ordinal))
        {
            return false;
        }

        var yamlLines = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.Equals(line, "---", StringComparison.Ordinal))
            {
                yaml = string.Join(Environment.NewLine, yamlLines);
                body = reader.ReadToEnd();
                return true;
            }

            yamlLines.Add(line);
        }

        return false;
    }

    private Dictionary<string, string> ParseFrontmatter(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var raw = _deserializer.Deserialize<Dictionary<object, object?>>(yaml);
        var frontmatter = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (raw is null)
        {
            return frontmatter;
        }

        foreach (var (key, value) in raw)
        {
            if (key is null || value is null)
            {
                continue;
            }

            frontmatter[key.ToString() ?? string.Empty] = ConvertYamlValue(value);
        }

        return frontmatter;
    }

    private IReadOnlyList<string> ExtractTags(IReadOnlyDictionary<string, string> frontmatter, string yaml)
    {
        if (!frontmatter.TryGetValue("tags", out var tagsValue) || string.IsNullOrWhiteSpace(tagsValue))
        {
            return [];
        }

        var tags = tagsValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .ToList();

        if (tags.Count > 0)
        {
            return tags;
        }

        var raw = _deserializer.Deserialize<Dictionary<object, object?>>(yaml);
        if (raw is null || !raw.TryGetValue("tags", out var rawTags) || rawTags is not IEnumerable<object> tagList)
        {
            return [];
        }

        return tagList
            .Select(tag => tag.ToString())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag!)
            .ToList();
    }

    private static string ConvertYamlValue(object value)
    {
        if (value is not string && value is IEnumerable<object> list)
        {
            return string.Join(", ", list.Select(item => item.ToString()).Where(item => !string.IsNullOrWhiteSpace(item)));
        }

        return value.ToString() ?? string.Empty;
    }
}

public sealed record ParsedMarkdownNote(
    IReadOnlyDictionary<string, string> Frontmatter,
    string MarkdownBody,
    IReadOnlyList<string> Tags);
