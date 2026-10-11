using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Layout.SplitPane;

namespace Moka.Red.Layout.Tests.Components;

// The split measured its first pane once, on first render, so one that was laid out hidden kept a
// stale size that every later drag started from. The drag itself ran a .NET handler per pointermove,
// which re-rendered both panes and everything in them on every move.
public class MokaSplitPaneDragTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Layout/SplitPane/MokaSplitPane.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaSplitPaneDragTests()
	{
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void TheRootIsWatchedForSizeChanges()
	{
		IRenderedComponent<MokaSplitPane> cut = Render<MokaSplitPane>();

		cut.WaitForAssertion(() => Assert.Single(_module.Invocations["observeSize"]));
	}

	// What the watch is for: a split measured while it had no size has to take the real one later.
	[Fact]
	public async Task MeasuringAgain_TakesTheNewSize()
	{
		_module.Setup<double>("measureFirstPane", _ => true).SetResult(320);
		IRenderedComponent<MokaSplitPane> cut = Render<MokaSplitPane>();

		await cut.InvokeAsync(() => cut.Instance.OnRootResized());

		Assert.Contains("--moka-split-size: 320px", cut.Find(".moka-split-pane").GetAttribute("style"), StringComparison.Ordinal);
	}

	// A drag in progress owns the size; a resize in the middle of it must not pull the pane back.
	[Fact]
	public async Task AResizeDuringADrag_IsIgnored()
	{
		_module.Setup<double>("measureFirstPane", _ => true).SetResult(200);
		_module.Setup<bool>("beginDrag", _ => true).SetResult(true);
		IRenderedComponent<MokaSplitPane> cut = Render<MokaSplitPane>();
		cut.WaitForAssertion(() => Assert.Single(_module.Invocations["measureFirstPane"]));

		await cut.Find(".moka-split-pane-handle").PointerDownAsync(new PointerEventArgs { ClientX = 200 });
		int measured = _module.Invocations["measureFirstPane"].Count;
		await cut.InvokeAsync(() => cut.Instance.OnRootResized());

		Assert.Equal(measured, _module.Invocations["measureFirstPane"].Count);
	}

	[Fact]
	public async Task PointerDown_HandsTheDragToTheBrowser()
	{
		_module.Setup<double>("measureFirstPane", _ => true).SetResult(240);
		_module.Setup<bool>("beginDrag", _ => true).SetResult(true);
		IRenderedComponent<MokaSplitPane> cut = Render<MokaSplitPane>();
		cut.WaitForAssertion(() => Assert.Single(_module.Invocations["measureFirstPane"]));

		await cut.Find(".moka-split-pane-handle").PointerDownAsync(new PointerEventArgs { ClientX = 240 });

		JSRuntimeInvocation begin = Assert.Single(_module.Invocations["beginDrag"]);
		Assert.Equal(240d, begin.Arguments[2]);
		Assert.Equal(240d, begin.Arguments[3]);
		Assert.Contains("moka-split-pane--dragging", cut.Find(".moka-split-pane").ClassList);
	}

	// A handler on an element re-renders the component whatever it does, so the only way a pointer
	// move costs nothing is for there to be no handler. None while idle, and none while the browser
	// has the drag.
	[Fact]
	public async Task NoPointerHandlerWhileIdle()
	{
		IRenderedComponent<MokaSplitPane> cut = Render<MokaSplitPane>();

		await Assert.ThrowsAsync<MissingEventHandlerException>(
			() => cut.Find(".moka-split-pane").PointerMoveAsync(new PointerEventArgs { ClientX = 400 }));
	}

	[Fact]
	public async Task WhileTheBrowserDrags_NoPointerMoveReachesDotNet()
	{
		_module.Setup<double>("measureFirstPane", _ => true).SetResult(240);
		_module.Setup<bool>("beginDrag", _ => true).SetResult(true);
		IRenderedComponent<MokaSplitPane> cut = Render<MokaSplitPane>();
		cut.WaitForAssertion(() => Assert.Single(_module.Invocations["measureFirstPane"]));
		await cut.Find(".moka-split-pane-handle").PointerDownAsync(new PointerEventArgs { ClientX = 240 });

		await Assert.ThrowsAsync<MissingEventHandlerException>(
			() => cut.Find(".moka-split-pane").PointerMoveAsync(new PointerEventArgs { ClientX = 400 }));
		Assert.Contains("--moka-split-size: 240px", cut.Find(".moka-split-pane").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task TheDragEnding_SetsTheSizeOnce()
	{
		_module.Setup<double>("measureFirstPane", _ => true).SetResult(240);
		_module.Setup<bool>("beginDrag", _ => true).SetResult(true);
		IRenderedComponent<MokaSplitPane> cut = Render<MokaSplitPane>();
		cut.WaitForAssertion(() => Assert.Single(_module.Invocations["measureFirstPane"]));
		await cut.Find(".moka-split-pane-handle").PointerDownAsync(new PointerEventArgs { ClientX = 240 });

		await cut.InvokeAsync(() => cut.Instance.OnDragFinished(415));

		IElement root = cut.Find(".moka-split-pane");
		Assert.Contains("--moka-split-size: 415px", root.GetAttribute("style"), StringComparison.Ordinal);
		Assert.DoesNotContain("moka-split-pane--dragging", root.ClassList);
	}

	// Without the module the old path still has to work, or a drag does nothing at all.
	[Fact]
	public async Task WithoutTheModule_DotNetStillDragsTheHandle()
	{
		_module.Setup<double>("measureFirstPane", _ => true).SetResult(240);
		_module.Setup<bool>("beginDrag", _ => true).SetResult(false);
		IRenderedComponent<MokaSplitPane> cut = Render<MokaSplitPane>();
		cut.WaitForAssertion(() => Assert.Single(_module.Invocations["measureFirstPane"]));

		await cut.Find(".moka-split-pane-handle").PointerDownAsync(new PointerEventArgs { ClientX = 240 });
		await cut.Find(".moka-split-pane").PointerMoveAsync(new PointerEventArgs { ClientX = 300 });

		Assert.Contains("--moka-split-size: 300px", cut.Find(".moka-split-pane").GetAttribute("style"), StringComparison.Ordinal);
	}
}
