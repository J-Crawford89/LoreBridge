using Bunit;
using LoreBridge.Application.Notes;
using LoreBridge.Application.Review;
using LoreBridge.Application.Vault;
using LoreBridge.Core.Notes;
using LoreBridge.Core.Review;
using LoreBridge.Core.Vault;
using LoreBridge.SharedUi.Markdown;
using LoreBridge.SharedUi.State;
using LoreBridge.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LoreBridge.Tests;

public sealed class SharedUiPageTests
{
    [Fact]
    public void Home_RendersSelectedWorkspaceDashboard()
    {
        using var context = new BunitContext();
        AddWorkspace(context);
        context.Services.AddSingleton<IVaultService>(new StubVaultService
        {
            Snapshot = Snapshot([Summary("npc.aline", "Aline")])
        });
        context.Services.AddSingleton<IReviewService>(new StubReviewService());

        var page = context.Render<LoreBridge.SharedUi.Pages.Home>();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("Arkellion", page.Markup);
            Assert.Contains("Everything happening", page.Markup);
            Assert.Contains("Total articles", page.Markup);
        });
    }

    [Fact]
    public void Browse_RendersVaultHierarchyAndArticleCount()
    {
        using var context = new BunitContext();
        AddWorkspace(context);
        var note = Summary("npc.aline", "Aline");
        context.Services.AddSingleton<IVaultService>(new StubVaultService
        {
            Snapshot = new VaultSnapshot(
                1,
                new Dictionary<string, int> { ["canon"] = 1 },
                new Dictionary<string, int> { ["npc"] = 1 },
                new Dictionary<string, int> { ["03_Canon"] = 1 },
                [note],
                [note],
                new VaultFolderNode("Arkellion", "", [new VaultFolderNode("NPCs", "03_Canon/NPCs", [], [note])], []))
        });

        var page = context.Render<LoreBridge.SharedUi.Pages.Browse>();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("1 articles", page.Markup);
            Assert.Contains("NPCs", page.Markup);
            Assert.Contains("Aline", page.Markup);
        });
    }

    [Fact]
    public void Review_RendersSelectedWorkspaceQueue()
    {
        using var context = new BunitContext();
        AddWorkspace(context);
        context.Services.AddSingleton<IReviewService>(new StubReviewService
        {
            Items = [new ReviewItem("review.one", "arkellion", "Review Me", "staging.one", "01_Staging/one.md", "needs_review", DateTimeOffset.UtcNow)]
        });

        var page = context.Render<LoreBridge.SharedUi.Pages.Review>();

        page.WaitForAssertion(() =>
        {
            Assert.Contains("Review queue", page.Markup);
            Assert.Contains("Review Me", page.Markup);
        });
    }

    [Fact]
    public void CreateStagingNote_SubmitsAndNavigatesToCreatedNote()
    {
        using var context = new BunitContext();
        AddWorkspace(context);
        var noteService = new StubNoteService
        {
            CreateResult = new CreateNoteResult("staging.new_idea", "01_Staging/new_idea.md", true)
        };
        context.Services.AddSingleton<INoteService>(noteService);
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        var page = context.Render<LoreBridge.SharedUi.Pages.CreateStagingNote>();

        page.Find("#staging-title").Change("New Idea");
        page.Find("#staging-markdown").Change("Draft");
        page.Find("button").Click();

        Assert.EndsWith("/notes/staging.new_idea", navigation.Uri);
    }

    [Fact]
    public void NoteDetail_RendersMarkdownMetadataAndTags()
    {
        using var context = new BunitContext();
        AddWorkspace(context);
        var note = new NoteDetail(
            "npc.aline",
            "Aline Soyer",
            "03_Canon/NPCs/aline.md",
            "# Biography\n\nA powerful **mage**.",
            new Dictionary<string, string> { ["type"] = "npc", ["status"] = "canon" },
            ["arkellion", "mage"],
            DateTimeOffset.UtcNow);
        context.Services.AddSingleton<INoteService>(new StubNoteService { NoteResult = note });
        context.Services.AddSingleton<IVaultService>(new StubVaultService { Snapshot = Snapshot([Summary("npc.aline", "Aline Soyer")]) });
        context.Services.AddSingleton(new NoteMarkdownRenderer());

        var page = context.Render<LoreBridge.SharedUi.Pages.NoteDetail>(parameters => parameters
            .Add(detail => detail.NoteId, "npc.aline"));

        page.WaitForAssertion(() =>
        {
            Assert.Contains("Aline Soyer", page.Markup);
            Assert.Contains("<h1 id=\"biography\">Biography</h1>", page.Markup);
            Assert.Contains("<strong>mage</strong>", page.Markup);
            Assert.Contains("arkellion", page.Markup);
            Assert.Contains("03_Canon/NPCs/aline.md", page.Markup);
        });
    }

    private static void AddWorkspace(BunitContext context)
    {
        context.Services.AddSingleton(new WorkspaceState(new StubWorkspaceService(
            StubWorkspaceService.CreateSettings("C:/Vault", "arkellion", "Arkellion"))));
    }

    private static VaultSnapshot Snapshot(IReadOnlyList<NoteSearchResult> notes) =>
        new(
            notes.Count,
            new Dictionary<string, int> { ["canon"] = notes.Count },
            new Dictionary<string, int> { ["npc"] = notes.Count },
            new Dictionary<string, int> { ["03_Canon"] = notes.Count },
            notes,
            notes,
            new VaultFolderNode("Arkellion", "", [], notes));

    private static NoteSearchResult Summary(string id, string title) =>
        new(id, title, $"03_Canon/{title}.md", "npc", null, "canon", "Summary", ["arkellion"], DateTimeOffset.UtcNow);
}
