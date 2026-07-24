using LoreBridge.Core.Notes;
using LoreBridge.Infrastructure.Notes;
using LoreBridge.Tests.TestSupport;

namespace LoreBridge.Tests;

public sealed class FileSystemNoteServiceTests
{
    [Fact]
    public async Task SearchAsync_MatchesTitleBodyPathAndTagsCaseInsensitively()
    {
        using var temp = new TemporaryDirectory();
        await temp.WriteFileAsync("03_Canon/NPCs/aline.md", Note("npc.aline", "Aline Soyer", "npc", "canon", "A royal MAGE.", "arkellion"));
        await temp.WriteFileAsync("03_Canon/Places/harbor.md", Note("place.harbor", "Old Harbor", "location", "canon", "Ships arrive daily.", "coastal"));
        var service = CreateService(temp);

        Assert.Equal("npc.aline", Assert.Single(await Search(service, "ALINE")).NoteId);
        Assert.Equal("npc.aline", Assert.Single(await Search(service, "mage")).NoteId);
        Assert.Equal("place.harbor", Assert.Single(await Search(service, "Places")).NoteId);
        Assert.Equal("place.harbor", Assert.Single(await Search(service, "COASTAL")).NoteId);
    }

    [Fact]
    public async Task SearchAsync_AppliesStatusTypeAndLimit()
    {
        using var temp = new TemporaryDirectory();
        await temp.WriteFileAsync("03_Canon/one.md", Note("one", "One", "npc", "canon", "Body"));
        await temp.WriteFileAsync("03_Canon/two.md", Note("two", "Two", "location", "canon", "Body"));
        await temp.WriteFileAsync("01_Staging/three.md", Note("three", "Three", "npc", "staging", "Body"));
        var service = CreateService(temp);

        var filtered = await service.SearchAsync(new NoteSearchRequest("test", "", "CANON", "NPC", 50));
        var limited = await service.SearchAsync(new NoteSearchRequest("test", "", null, null, 2));

        Assert.Equal("one", Assert.Single(filtered).NoteId);
        Assert.Equal(2, limited.Count);
    }

    [Fact]
    public async Task SearchAsync_SkipsObsidianAndSystemDirectories()
    {
        using var temp = new TemporaryDirectory();
        await temp.WriteFileAsync("03_Canon/visible.md", Note("visible", "Visible", "location", "canon", "Body"));
        await temp.WriteFileAsync(".obsidian/hidden.md", Note("hidden.obsidian", "Hidden", "system", "canon", "Body"));
        await temp.WriteFileAsync("99_System/hidden.md", Note("hidden.system", "Hidden", "system", "canon", "Body"));
        var service = CreateService(temp);

        var results = await Search(service, "");

        Assert.Equal("visible", Assert.Single(results).NoteId);
    }

    [Fact]
    public async Task SearchAsync_CreatesFocusedSnippet()
    {
        using var temp = new TemporaryDirectory();
        var body = new string('a', 100) + " dragon " + new string('b', 150);
        await temp.WriteFileAsync("03_Canon/dragon.md", Note("dragon", "Creature", "monster", "canon", body));
        var service = CreateService(temp);

        var result = Assert.Single(await Search(service, "dragon"));

        Assert.Contains("dragon", result.Snippet);
        Assert.True(result.Snippet.Length <= 180);
    }

    [Fact]
    public async Task GetNoteAsync_ReadsByIdAndRelativePath()
    {
        using var temp = new TemporaryDirectory();
        await temp.WriteFileAsync("03_Canon/NPCs/aline.md", Note("npc.aline", "Aline", "npc", "canon", "Biography", "mage"));
        var service = CreateService(temp);

        var byId = await service.GetNoteAsync("test", "NPC.ALINE");
        var byPath = await service.GetNoteAsync("test", "03_Canon/NPCs/aline.md");

        Assert.Equal(byId.NoteId, byPath.NoteId);
        Assert.Equal(byId.RelativePath, byPath.RelativePath);
        Assert.Equal(byId.Markdown, byPath.Markdown);
        Assert.Equal("Biography", byId.Markdown);
        Assert.Equal(["mage"], byId.Tags);
    }

    [Fact]
    public async Task GetNoteAsync_UsesSafeFallbackIdentityAndTitle()
    {
        using var temp = new TemporaryDirectory();
        await temp.WriteFileAsync("Loose Notes/My Idea.md", "# Heading\n\nBody");
        var service = CreateService(temp);

        var result = await service.GetNoteAsync("test", "Loose Notes/My Idea.md");

        Assert.Equal("loose_notes.my_idea", result.NoteId);
        Assert.Equal("My Idea", result.Title);
    }

    [Theory]
    [InlineData("../outside.md")]
    [InlineData("03_Canon/../../outside.md")]
    public async Task GetNoteAsync_RejectsTraversalPaths(string path)
    {
        using var temp = new TemporaryDirectory();
        var service = CreateService(temp);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetNoteAsync("test", path));

        Assert.Contains("must not contain '..'", exception.Message);
    }

    [Fact]
    public async Task GetNoteAsync_RejectsAbsolutePaths()
    {
        using var temp = new TemporaryDirectory();
        var service = CreateService(temp);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetNoteAsync("test", temp.GetPath("note.md")));

        Assert.Contains("must not be absolute", exception.Message);
    }

    [Fact]
    public async Task GetNoteAsync_ReportsMissingNotes()
    {
        using var temp = new TemporaryDirectory();
        var service = CreateService(temp);

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.GetNoteAsync("test", "missing.id"));
        await Assert.ThrowsAsync<FileNotFoundException>(() => service.GetNoteAsync("test", "03_Canon/missing.md"));
    }

    [Fact]
    public async Task CreateStagingNoteAsync_WritesExpectedFrontmatterAndBody()
    {
        using var temp = new TemporaryDirectory();
        var service = CreateService(temp);

        var result = await service.CreateStagingNoteAsync(new CreateStagingNoteRequest(
            "test", "Aline's New Idea", "# Details\n\nDraft body.", ["npc", "mage"]));
        var created = await service.GetNoteAsync("test", result.RelativePath);

        Assert.True(result.Created);
        Assert.Equal("staging.aline_s_new_idea", result.NoteId);
        Assert.Equal("01_Staging/Chat_Imports/aline_s_new_idea.md", result.RelativePath);
        Assert.Equal("staging", created.Frontmatter["status"]);
        Assert.Equal(["npc", "mage"], created.Tags);
        Assert.Equal("# Details\n\nDraft body.", created.Markdown.TrimStart('\r', '\n').Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task CreateStagingNoteAsync_NeverOverwritesAndAddsNumericSuffix()
    {
        using var temp = new TemporaryDirectory();
        var service = CreateService(temp);
        var request = new CreateStagingNoteRequest("test", "Repeated Idea", "Body", []);

        var first = await service.CreateStagingNoteAsync(request);
        var second = await service.CreateStagingNoteAsync(request);

        Assert.Equal("01_Staging/Chat_Imports/repeated_idea.md", first.RelativePath);
        Assert.Equal("01_Staging/Chat_Imports/repeated_idea-2.md", second.RelativePath);
        Assert.True(File.Exists(temp.GetPath(first.RelativePath.Replace('/', Path.DirectorySeparatorChar))));
        Assert.True(File.Exists(temp.GetPath(second.RelativePath.Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public async Task CreateStagingNoteAsync_RejectsConfiguredCanonDestination()
    {
        using var temp = new TemporaryDirectory();
        var settings = StubWorkspaceService.CreateSettings(
            temp.Path,
            stagingPaths: new Dictionary<string, string> { ["chat_imports"] = "03_Canon/Chat_Imports" });
        var service = TestServices.CreateNoteService(new StubWorkspaceService(settings));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateStagingNoteAsync(
            new CreateStagingNoteRequest("test", "Unsafe", "Body", [])));

        Assert.Contains("cannot write", exception.Message);
        Assert.False(Directory.Exists(temp.GetPath("03_Canon", "Chat_Imports")));
    }

    [Fact]
    public async Task CreateStagingNoteAsync_RequiresConfiguredChatImportsPath()
    {
        using var temp = new TemporaryDirectory();
        var settings = StubWorkspaceService.CreateSettings(temp.Path, stagingPaths: []);
        var service = TestServices.CreateNoteService(new StubWorkspaceService(settings));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateStagingNoteAsync(
            new CreateStagingNoteRequest("test", "Idea", "Body", [])));

        Assert.Contains("staging_paths.chat_imports", exception.Message);
    }

    [Fact]
    public async Task SearchAsync_ReportsMissingVaultRoot()
    {
        using var temp = new TemporaryDirectory();
        var missingRoot = temp.GetPath("missing");
        var workspaceService = new StubWorkspaceService(StubWorkspaceService.CreateSettings(missingRoot));
        var service = TestServices.CreateNoteService(workspaceService);

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => Search(service, ""));
    }

    private static FileSystemNoteService CreateService(TemporaryDirectory temp)
    {
        var workspaceService = new StubWorkspaceService(StubWorkspaceService.CreateSettings(temp.Path));
        return TestServices.CreateNoteService(workspaceService);
    }

    private static Task<IReadOnlyList<NoteSearchResult>> Search(FileSystemNoteService service, string query) =>
        service.SearchAsync(new NoteSearchRequest("test", query, null, null, 50));

    private static string Note(
        string id,
        string title,
        string type,
        string status,
        string body,
        params string[] tags) =>
        $$"""
        ---
        id: {{id}}
        title: {{title}}
        type: {{type}}
        status: {{status}}
        tags:
        {{string.Join(Environment.NewLine, tags.Select(tag => $"  - {tag}"))}}
        ---
        {{body}}
        """;
}
