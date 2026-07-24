using Bunit;
using LoreBridge.Application.Notes;
using LoreBridge.Application.Review;
using LoreBridge.Application.Vault;
using LoreBridge.Core.Notes;
using LoreBridge.Core.Review;
using LoreBridge.Core.Vault;
using LoreBridge.SharedUi.Components;
using LoreBridge.SharedUi.State;
using LoreBridge.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LoreBridge.Tests;

public sealed class SharedUiComponentTests
{
    [Fact]
    public void CreateStagingNoteForm_ValidatesRequiredTitle()
    {
        using var context = new BunitContext();
        var noteService = new StubNoteService();
        context.Services.AddSingleton<INoteService>(noteService);
        var component = context.Render<CreateStagingNoteForm>(parameters => parameters
            .Add(form => form.WorkspaceId, "arkellion"));

        component.Find("button").Click();

        Assert.Contains("Title is required", component.Markup);
        Assert.Null(noteService.LastCreateRequest);
    }

    [Fact]
    public void CreateStagingNoteForm_SubmitsValuesAndRaisesCreatedCallback()
    {
        using var context = new BunitContext();
        var noteService = new StubNoteService
        {
            CreateResult = new CreateNoteResult("staging.idea", "01_Staging/idea.md", true)
        };
        context.Services.AddSingleton<INoteService>(noteService);
        CreateNoteResult? callbackResult = null;
        var component = context.Render<CreateStagingNoteForm>(parameters => parameters
            .Add(form => form.WorkspaceId, "arkellion")
            .Add(form => form.OnNoteCreated, result => callbackResult = result));

        component.Find("#staging-title").Change("New Idea");
        component.Find("#staging-tags").Change("npc, mage");
        component.Find("#staging-markdown").Change("# Draft");
        component.Find("button").Click();

        Assert.Equal("New Idea", noteService.LastCreateRequest!.Title);
        Assert.Equal(["npc", "mage"], noteService.LastCreateRequest.Tags);
        Assert.Equal("# Draft", noteService.LastCreateRequest.Markdown);
        Assert.Same(noteService.CreateResult, callbackResult);
    }

    [Fact]
    public void ReviewQueue_RendersEmptyState()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<IReviewService>(new StubReviewService());

        var component = context.Render<ReviewQueue>(parameters => parameters
            .Add(queue => queue.WorkspaceId, "arkellion"));

        Assert.Contains("Queue clear", component.Markup);
    }

    [Fact]
    public void ReviewQueue_CompactModeLimitsItemsAndNavigatesToSourceNote()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<IReviewService>(new StubReviewService
        {
            Items =
            [
                Review("one", "First", DateTimeOffset.UtcNow),
                Review("two", "Second", DateTimeOffset.UtcNow.AddMinutes(-1))
            ]
        });
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        var component = context.Render<ReviewQueue>(parameters => parameters
            .Add(queue => queue.WorkspaceId, "arkellion")
            .Add(queue => queue.Compact, true)
            .Add(queue => queue.ItemLimit, 1));

        var item = Assert.Single(component.FindAll(".review-list button"));
        item.Click();

        Assert.EndsWith("/notes/one", navigation.Uri);
        Assert.DoesNotContain("Second", component.Markup);
    }

    [Fact]
    public void VaultFolderTree_NavigatesWhenNoteIsClicked()
    {
        using var context = new BunitContext();
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        var note = Summary("npc.aline", "Aline");
        var root = new VaultFolderNode(
            "Vault",
            "",
            [new VaultFolderNode("NPCs", "03_Canon/NPCs", [], [note])],
            []);
        var component = context.Render<VaultFolderTree>(parameters => parameters
            .Add(tree => tree.Folder, root)
            .Add(tree => tree.IsRoot, true));

        component.Find(".tree-note").Click();

        Assert.EndsWith("/notes/npc.aline", navigation.Uri);
        Assert.Contains("NPCs", component.Markup);
    }

    [Fact]
    public void WorkspaceSwitcher_ListsWorkspacesAndReturnsToDashboardAfterSwitch()
    {
        using var context = new BunitContext();
        var workspaceService = new StubWorkspaceService(
            StubWorkspaceService.CreateSettings("C:/First", "first", "First World"),
            StubWorkspaceService.CreateSettings("C:/Second", "second", "Second World"));
        var state = new WorkspaceState(workspaceService);
        context.Services.AddSingleton(state);
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/review");
        var component = context.Render<WorkspaceSwitcher>();

        component.Find(".workspace-trigger").Click();
        component.FindAll(".workspace-option")[1].Click();

        Assert.Equal("second", state.SelectedWorkspaceId);
        Assert.Equal("http://localhost/", navigation.Uri);
    }

    [Fact]
    public void GlobalNoteSearch_SearchesCurrentWorkspaceAndNavigatesToResult()
    {
        using var context = new BunitContext();
        var noteService = new StubNoteService { SearchResults = [Summary("npc.aline", "Aline Soyer")] };
        var workspaceState = new WorkspaceState(new StubWorkspaceService(
            StubWorkspaceService.CreateSettings("C:/Vault", "arkellion", "Arkellion")));
        context.Services.AddSingleton<INoteService>(noteService);
        context.Services.AddSingleton(workspaceState);
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        var component = context.Render<GlobalNoteSearch>();

        component.Find("input").Input("aline");
        component.WaitForAssertion(() => Assert.Contains("Aline Soyer", component.Markup), TimeSpan.FromSeconds(2));
        component.Find(".search-result").Click();

        Assert.Equal("arkellion", noteService.LastSearchRequest!.WorkspaceId);
        Assert.Equal("aline", noteService.LastSearchRequest.Query);
        Assert.Equal(8, noteService.LastSearchRequest.Limit);
        Assert.EndsWith("/notes/npc.aline", navigation.Uri);
    }

    [Fact]
    public void WorkspaceDashboard_RendersVaultMetricsReviewAndFolderDistribution()
    {
        using var context = new BunitContext();
        var recent = Summary("npc.aline", "Aline Soyer");
        var vaultService = new StubVaultService
        {
            Snapshot = new VaultSnapshot(
                12,
                new Dictionary<string, int> { ["canon"] = 8, ["staging"] = 4 },
                new Dictionary<string, int> { ["npc"] = 6 },
                new Dictionary<string, int> { ["03_Canon"] = 8, ["01_Staging"] = 4 },
                [recent],
                [recent],
                new VaultFolderNode("Arkellion", "", [], [recent]))
        };
        var workspaceState = new WorkspaceState(new StubWorkspaceService(
            StubWorkspaceService.CreateSettings("C:/Vault", "arkellion", "Arkellion")));
        context.Services.AddSingleton<IVaultService>(vaultService);
        context.Services.AddSingleton<IReviewService>(new StubReviewService
        {
            Items = [Review("review.one", "Review Me", DateTimeOffset.UtcNow)]
        });
        context.Services.AddSingleton(workspaceState);

        var component = context.Render<WorkspaceDashboard>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Total articles", component.Markup);
            Assert.Contains(">12<", component.Markup);
            Assert.Contains("03_Canon", component.Markup);
            Assert.Contains("Review Me", component.Markup);
            Assert.Contains("Aline Soyer", component.Markup);
        });
    }

    private static ReviewItem Review(string id, string title, DateTimeOffset created) =>
        new($"review.{id}", "arkellion", title, id, $"01_Staging/{id}.md", "needs_review", created);

    private static NoteSearchResult Summary(string id, string title) =>
        new(id, title, $"03_Canon/{title}.md", "npc", null, "canon", "Summary", [], DateTimeOffset.UtcNow);
}
