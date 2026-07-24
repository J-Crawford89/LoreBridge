using LoreBridge.Core.Notes;
using LoreBridge.SharedUi.Markdown;

namespace LoreBridge.Tests;

public sealed class NoteMarkdownRendererTests
{
    private readonly NoteMarkdownRenderer renderer = new();

    [Fact]
    public void Render_ConvertsWikiLinksAliasesAndHeadings()
    {
        var current = CreateDetail("place.capital", "Capital", "# Districts\n\nSee [[Aline Soyer#Early Life|Aline's history]].");
        var aline = CreateSummary("npc.aline", "Aline Soyer", "03_Canon/NPCs/aline-soyer.md");

        var html = renderer.Render(current, [aline]);

        Assert.Contains("href=\"/notes/npc.aline#early-life\"", html);
        Assert.Contains(">Aline's history</a>", html);
    }

    [Fact]
    public void Render_ConvertsSameNoteHeadingLinks()
    {
        var current = CreateDetail("place.capital", "Capital", "# Districts\n\nSee [[#Districts]].");

        var html = renderer.Render(current, []);

        Assert.Contains("href=\"/notes/place.capital#districts\"", html);
        Assert.Contains("id=\"districts\"", html);
    }

    [Fact]
    public void Render_LeavesEmbedsAndCodeWikiSyntaxUntouched()
    {
        var current = CreateDetail(
            "place.capital",
            "Capital",
            "![[Map.png]]\n\n`[[Aline Soyer]]`\n\n```text\n[[Aline Soyer]]\n```");
        var aline = CreateSummary("npc.aline", "Aline Soyer", "03_Canon/NPCs/aline-soyer.md");

        var html = renderer.Render(current, [aline]);

        Assert.DoesNotContain("href=\"/notes/npc.aline\"", html);
        Assert.Contains("![[Map.png]]", html);
        Assert.Contains("[[Aline Soyer]]", html);
    }

    [Fact]
    public void Render_DisablesRawHtml()
    {
        var current = CreateDetail("place.capital", "Capital", "<script>alert('no')</script>");

        var html = renderer.Render(current, []);

        Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public void Render_RemovesUnsafeLinkProtocols()
    {
        var current = CreateDetail("place.capital", "Capital", "[unsafe](javascript:alert('no'))");

        var html = renderer.Render(current, []);

        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_PreservesSafeStandardLinksAndMarkdownExtensions()
    {
        var current = CreateDetail(
            "place.capital",
            "Capital",
            "[Relative](../People/aline.md) [Web](https://example.com)\n\n| Name | Role |\n| --- | --- |\n| Aline | Mage |");

        var html = renderer.Render(current, []);

        Assert.Contains("href=\"../People/aline.md\"", html);
        Assert.Contains("href=\"https://example.com\"", html);
        Assert.Contains("<table>", html);
    }

    [Theory]
    [InlineData("npc.aline")]
    [InlineData("Aline Soyer")]
    [InlineData("03_Canon/NPCs/aline-soyer.md")]
    [InlineData("03_Canon/NPCs/aline-soyer")]
    [InlineData("aline-soyer")]
    public void Render_ResolvesSupportedWikiLinkIdentities(string target)
    {
        var current = CreateDetail("place.capital", "Capital", $"See [[{target}]].");
        var aline = CreateSummary("npc.aline", "Aline Soyer", "03_Canon/NPCs/aline-soyer.md");

        var html = renderer.Render(current, [aline]);

        Assert.Contains("href=\"/notes/npc.aline\"", html);
    }

    [Fact]
    public void Render_LeavesUnresolvedWikiLinksVisible()
    {
        var current = CreateDetail("place.capital", "Capital", "See [[Missing Note]].");

        var html = renderer.Render(current, []);

        Assert.Contains("[[Missing Note]]", html);
        Assert.DoesNotContain("href=", html);
    }

    private static NoteDetail CreateDetail(string id, string title, string markdown) =>
        new(id, title, $"03_Canon/{title}.md", markdown, new Dictionary<string, string>(), [], DateTimeOffset.UtcNow);

    private static NoteSearchResult CreateSummary(string id, string title, string path) =>
        new(id, title, path, "npc", null, "canon", string.Empty, [], DateTimeOffset.UtcNow);
}
