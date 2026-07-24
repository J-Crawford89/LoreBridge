using LoreBridge.Infrastructure.Notes;

namespace LoreBridge.Tests;

public sealed class MarkdownFrontmatterParserTests
{
    private readonly MarkdownFrontmatterParser parser = new();

    [Fact]
    public void Parse_WithoutFrontmatter_PreservesEntireDocument()
    {
        const string markdown = "# A title\n\nBody text.";

        var result = parser.Parse(markdown);

        Assert.Empty(result.Frontmatter);
        Assert.Empty(result.Tags);
        Assert.Equal(markdown, result.MarkdownBody);
    }

    [Fact]
    public void Parse_WithFrontmatter_ExtractsCaseInsensitiveValuesAndBody()
    {
        const string markdown = """
            ---
            id: npc.aline
            title: Aline Soyer
            type: npc
            ---
            # Biography

            Body text.
            """;

        var result = parser.Parse(markdown);

        Assert.Equal("npc.aline", result.Frontmatter["ID"]);
        Assert.Equal("Aline Soyer", result.Frontmatter["title"]);
        Assert.StartsWith("# Biography", result.MarkdownBody);
    }

    [Fact]
    public void Parse_WithYamlTagList_ExtractsAllTags()
    {
        const string markdown = """
            ---
            tags:
              - arkellion
              - npc
            ---
            Body
            """;

        var result = parser.Parse(markdown);

        Assert.Equal(["arkellion", "npc"], result.Tags);
        Assert.Equal("arkellion, npc", result.Frontmatter["tags"]);
    }

    [Fact]
    public void Parse_WithCommaSeparatedTags_TrimsAndRemovesEmptyValues()
    {
        const string markdown = "---\ntags: arkellion, npc, , mage\n---\nBody";

        var result = parser.Parse(markdown);

        Assert.Equal(["arkellion", "npc", "mage"], result.Tags);
    }

    [Fact]
    public void Parse_WithUnterminatedFrontmatter_TreatsDocumentAsMarkdown()
    {
        const string markdown = "---\ntitle: Aline\nBody";

        var result = parser.Parse(markdown);

        Assert.Empty(result.Frontmatter);
        Assert.Equal(markdown, result.MarkdownBody);
    }

    [Fact]
    public void Parse_IgnoresNullFrontmatterValues()
    {
        const string markdown = "---\ntitle:\ntype: npc\n---\nBody";

        var result = parser.Parse(markdown);

        Assert.False(result.Frontmatter.ContainsKey("title"));
        Assert.Equal("npc", result.Frontmatter["type"]);
    }
}
