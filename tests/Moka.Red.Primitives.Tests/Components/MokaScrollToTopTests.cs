using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Primitives.Utility;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaScrollToTopTests : BunitContext
{
	private const string ModulePath = "./_content/Moka.Red.Primitives/Utility/MokaScrollToTop.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaScrollToTopTests()
	{
		_module = JSInterop.SetupModule(ModulePath);
		_module.Setup<int>("init", _ => true).SetResult(7);
		_module.SetupVoid("scrollToTop", _ => true).SetVoidResult();
		_module.SetupVoid("dispose", _ => true).SetVoidResult();
	}

	[Fact]
	public void WatchesTheWindow_ByDefault()
	{
		Render<MokaScrollToTop>();

		JSRuntimeInvocation init = Assert.Single(_module.Invocations["init"]);
		Assert.Equal(200, init.Arguments[1]);
		Assert.Null(init.Arguments[2]);
	}

	// The component only ever watched the window, so it never showed in a layout that scrolls an
	// inner element.
	[Fact]
	public void ScrollContainerSelector_IsWhatTheModuleWatches()
	{
		Render<MokaScrollToTop>(p => p.Add(x => x.ScrollContainerSelector, ".app-main"));

		JSRuntimeInvocation init = Assert.Single(_module.Invocations["init"]);
		Assert.Equal(".app-main", init.Arguments[2]);
	}

	[Fact]
	public void ChangingTheSelector_MovesTheListener()
	{
		IRenderedComponent<MokaScrollToTop> cut = Render<MokaScrollToTop>(p => p.Add(x => x.ScrollContainerSelector, ".one"));

		cut.Render(p => p.Add(x => x.ScrollContainerSelector, ".two"));

		JSRuntimeInvocation dispose = Assert.Single(_module.Invocations["dispose"]);
		Assert.Equal(7, dispose.Arguments[0]);
		Assert.Equal([".one", ".two"], _module.Invocations["init"].Select(i => i.Arguments[2]));
	}

	[Fact]
	public void ReRenderingWithTheSameSettings_KeepsOneListener()
	{
		IRenderedComponent<MokaScrollToTop> cut = Render<MokaScrollToTop>();

		cut.Render(p => p.Add(x => x.ShowAfter, 200));
		cut.Render(p => p.Add(x => x.Smooth, false));

		Assert.Single(_module.Invocations["init"]);
		Assert.Empty(_module.Invocations["dispose"]);
	}

	[Fact]
	public async Task Click_ScrollsTheWatchedTarget()
	{
		IRenderedComponent<MokaScrollToTop> cut = Render<MokaScrollToTop>();
		await cut.InvokeAsync(() => cut.Instance.OnScrollChanged(true));

		await cut.Find("button").ClickAsync(new MouseEventArgs());

		JSRuntimeInvocation scroll = Assert.Single(_module.Invocations["scrollToTop"]);
		Assert.Equal(7, scroll.Arguments[0]);
		Assert.True(scroll.Arguments[1] is true);
	}

	// The inherited Disabled parameter used to do nothing: the button stayed enabled and scrolled.
	[Fact]
	public async Task Disabled_DisablesTheButton_AndDoesNotScroll()
	{
		IRenderedComponent<MokaScrollToTop> cut = Render<MokaScrollToTop>(p => p.Add(x => x.Disabled, true));
		await cut.InvokeAsync(() => cut.Instance.OnScrollChanged(true));

		IElement button = cut.Find("button");
		await button.ClickAsync(new MouseEventArgs());

		Assert.True(button.HasAttribute("disabled"));
		Assert.Empty(_module.Invocations["scrollToTop"]);
	}

	[Fact]
	public async Task Label_NamesTheButton()
	{
		IRenderedComponent<MokaScrollToTop> cut = Render<MokaScrollToTop>(p => p.Add(x => x.Label, "Nach oben"));
		await cut.InvokeAsync(() => cut.Instance.OnScrollChanged(true));

		Assert.Equal("Nach oben", cut.Find("button").GetAttribute("title"));
	}

	[Fact]
	public async Task Label_DefaultsToScrollToTop()
	{
		IRenderedComponent<MokaScrollToTop> cut = Render<MokaScrollToTop>();
		await cut.InvokeAsync(() => cut.Instance.OnScrollChanged(true));

		Assert.Equal("Scroll to top", cut.Find("button").GetAttribute("title"));
	}

	[Fact]
	public async Task Dispose_RemovesTheListener()
	{
		IRenderedComponent<MokaScrollToTop> cut = Render<MokaScrollToTop>();

		await cut.Instance.DisposeAsync();

		JSRuntimeInvocation dispose = Assert.Single(_module.Invocations["dispose"]);
		Assert.Equal(7, dispose.Arguments[0]);
	}
}
