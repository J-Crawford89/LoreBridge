using LoreBridge.SharedUi.State;
using LoreBridge.Tests.TestSupport;

namespace LoreBridge.Tests;

public sealed class WorkspaceStateTests
{
    [Fact]
    public async Task InitializeAsync_SelectsFirstWorkspaceAndRaisesChanged()
    {
        var service = new StubWorkspaceService(
            StubWorkspaceService.CreateSettings("C:/First", "first", "First"),
            StubWorkspaceService.CreateSettings("C:/Second", "second", "Second"));
        var state = new WorkspaceState(service);
        var changedCount = 0;
        state.Changed += () => changedCount++;

        await state.InitializeAsync();

        Assert.True(state.IsInitialized);
        Assert.Equal(2, state.Workspaces.Count);
        Assert.Equal("first", state.SelectedWorkspaceId);
        Assert.Equal(1, changedCount);
    }

    [Fact]
    public async Task InitializeAsync_IsIdempotentAcrossConcurrentCalls()
    {
        var service = new StubWorkspaceService(StubWorkspaceService.CreateSettings("C:/Vault"));
        var state = new WorkspaceState(service);

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => state.InitializeAsync()));

        Assert.Equal(1, service.GetWorkspacesCallCount);
    }

    [Fact]
    public async Task Select_SwitchesCaseInsensitivelyAndRaisesChanged()
    {
        var service = new StubWorkspaceService(
            StubWorkspaceService.CreateSettings("C:/First", "first", "First"),
            StubWorkspaceService.CreateSettings("C:/Second", "second", "Second"));
        var state = new WorkspaceState(service);
        await state.InitializeAsync();
        var changedCount = 0;
        state.Changed += () => changedCount++;

        var selected = state.Select("SECOND");

        Assert.True(selected);
        Assert.Equal("second", state.SelectedWorkspaceId);
        Assert.Equal(1, changedCount);
    }

    [Fact]
    public async Task Select_CurrentWorkspaceSucceedsWithoutExtraChangeNotification()
    {
        var service = new StubWorkspaceService(StubWorkspaceService.CreateSettings("C:/Vault", "current"));
        var state = new WorkspaceState(service);
        await state.InitializeAsync();
        var changedCount = 0;
        state.Changed += () => changedCount++;

        var selected = state.Select("current");

        Assert.True(selected);
        Assert.Equal(0, changedCount);
    }

    [Fact]
    public async Task Select_UnknownWorkspaceLeavesSelectionUnchanged()
    {
        var service = new StubWorkspaceService(StubWorkspaceService.CreateSettings("C:/Vault", "current"));
        var state = new WorkspaceState(service);
        await state.InitializeAsync();

        var selected = state.Select("missing");

        Assert.False(selected);
        Assert.Equal("current", state.SelectedWorkspaceId);
    }

    [Fact]
    public async Task InitializeAsync_HandlesNoConfiguredWorkspaces()
    {
        var state = new WorkspaceState(new StubWorkspaceService());

        await state.InitializeAsync();

        Assert.True(state.IsInitialized);
        Assert.Empty(state.Workspaces);
        Assert.Null(state.SelectedWorkspace);
    }
}
