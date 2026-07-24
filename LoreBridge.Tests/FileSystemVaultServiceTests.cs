using LoreBridge.Infrastructure.Vault;
using LoreBridge.Tests.TestSupport;

namespace LoreBridge.Tests;

public sealed class FileSystemVaultServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_AggregatesCountsRecentNotesAndFolderTree()
    {
        using var temp = new TemporaryDirectory();
        var alinePath = await temp.WriteFileAsync("03_Canon/NPCs/aline.md", Note("npc.aline", "Aline", "npc", "canon", "# Aline\nBiography"));
        var harborPath = await temp.WriteFileAsync("03_Canon/Places/harbor.md", Note("place.harbor", "Harbor", "location", "canon", "A busy harbor"));
        var ideaPath = await temp.WriteFileAsync("01_Staging/idea.md", Note("idea.one", "New Idea", null, "staging", "Draft"));
        temp.CreateDirectory("04_Sources", "Empty");
        File.SetLastWriteTimeUtc(alinePath, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(harborPath, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(ideaPath, new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var workspaceService = new StubWorkspaceService(StubWorkspaceService.CreateSettings(temp.Path));
        var service = new FileSystemVaultService(workspaceService, TestServices.CreateNoteService(workspaceService));

        var snapshot = await service.GetSnapshotAsync("test");

        Assert.Equal(3, snapshot.TotalNotes);
        Assert.Equal(2, snapshot.StatusCounts["canon"]);
        Assert.Equal(1, snapshot.StatusCounts["staging"]);
        Assert.Equal(1, snapshot.TypeCounts["npc"]);
        Assert.Equal(1, snapshot.TypeCounts["location"]);
        Assert.Equal(1, snapshot.TypeCounts["Unspecified"]);
        Assert.Equal(2, snapshot.TopLevelFolderCounts["03_Canon"]);
        Assert.Equal(1, snapshot.TopLevelFolderCounts["01_Staging"]);
        Assert.Equal("Harbor", snapshot.RecentNotes.First().Title);
        Assert.DoesNotContain("# Aline", snapshot.Notes.Single(note => note.NoteId == "npc.aline").Snippet);
        Assert.NotNull(FindFolder(snapshot.RootFolder, "04_Sources/Empty"));
        Assert.Contains(FindFolder(snapshot.RootFolder, "03_Canon/NPCs")!.Notes, note => note.NoteId == "npc.aline");
    }

    [Fact]
    public async Task GetSnapshotAsync_ExcludesObsidianAndSystemContent()
    {
        using var temp = new TemporaryDirectory();
        await temp.WriteFileAsync("03_Canon/visible.md", Note("visible", "Visible", "location", "canon", "Body"));
        await temp.WriteFileAsync(".obsidian/plugin.md", Note("plugin", "Plugin", "system", "canon", "Body"));
        await temp.WriteFileAsync("99_System/template.md", Note("template", "Template", "template", "canon", "Body"));
        var workspaceService = new StubWorkspaceService(StubWorkspaceService.CreateSettings(temp.Path));
        var service = new FileSystemVaultService(workspaceService, TestServices.CreateNoteService(workspaceService));

        var snapshot = await service.GetSnapshotAsync("test");

        Assert.Equal(1, snapshot.TotalNotes);
        Assert.DoesNotContain(snapshot.RootFolder.Folders, folder => folder.Name is ".obsidian" or "99_System");
    }

    [Fact]
    public async Task GetSnapshotAsync_HandlesEmptyVault()
    {
        using var temp = new TemporaryDirectory();
        var workspaceService = new StubWorkspaceService(StubWorkspaceService.CreateSettings(temp.Path));
        var service = new FileSystemVaultService(workspaceService, TestServices.CreateNoteService(workspaceService));

        var snapshot = await service.GetSnapshotAsync("test");

        Assert.Equal(0, snapshot.TotalNotes);
        Assert.Empty(snapshot.Notes);
        Assert.Empty(snapshot.StatusCounts);
        Assert.Empty(snapshot.RecentNotes);
    }

    [Fact]
    public async Task GetSnapshotAsync_LimitsRecentNotesToSix()
    {
        using var temp = new TemporaryDirectory();
        for (var index = 0; index < 8; index++)
        {
            var path = await temp.WriteFileAsync($"03_Canon/{index}.md", Note($"note.{index}", $"Note {index}", "entry", "canon", "Body"));
            File.SetLastWriteTimeUtc(path, new DateTime(2025, 1, 1, 0, index, 0, DateTimeKind.Utc));
        }
        var workspaceService = new StubWorkspaceService(StubWorkspaceService.CreateSettings(temp.Path));
        var service = new FileSystemVaultService(workspaceService, TestServices.CreateNoteService(workspaceService));

        var snapshot = await service.GetSnapshotAsync("test");

        Assert.Equal(6, snapshot.RecentNotes.Count);
        Assert.Equal("note.7", snapshot.RecentNotes[0].NoteId);
        Assert.Equal("note.2", snapshot.RecentNotes[^1].NoteId);
    }

    private static LoreBridge.Core.Vault.VaultFolderNode? FindFolder(
        LoreBridge.Core.Vault.VaultFolderNode folder,
        string relativePath)
    {
        if (folder.RelativePath == relativePath) return folder;
        return folder.Folders.Select(child => FindFolder(child, relativePath)).FirstOrDefault(result => result is not null);
    }

    private static string Note(string id, string title, string? type, string status, string body) =>
        $$"""
        ---
        id: {{id}}
        title: {{title}}
        {{(type is null ? string.Empty : $"type: {type}")}}
        status: {{status}}
        ---
        {{body}}
        """;
}
