using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Layout.AppBar;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

public class MokaAppBarTests : BunitContext
{
	public MokaAppBarTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void Fixed_IsStickyAtTheTop()
	{
		IRenderedComponent<MokaAppBar> cut = Render<MokaAppBar>(p => p.Add(x => x.Fixed, true));

		Assert.Contains("moka-appbar--fixed", cut.Find("header.moka-appbar").ClassList);
	}

	// What it reports is the page's top scroll padding. Without it a link to part of a page landed under the bar.
	[Fact]
	public void Fixed_ReportsTheRoomItTakes_Once()
	{
		(BunitJSModuleInterop module, _) = SpaceModule();
		IRenderedComponent<MokaAppBar> cut = Render<MokaAppBar>(p => p.Add(x => x.Fixed, true));
		string? bar = cut.Find("header").GetAttribute("blazor:elementReference");

		cut.Render(p => p.Add(x => x.Title, "Devices"));

		JSRuntimeInvocation watch = Assert.Single(module.Invocations["watchSpace"]);
		Assert.Equal(bar, Assert.IsType<ElementReference>(watch.Arguments[0]).Id);
		Assert.Equal("--moka-appbar-space", watch.Arguments[1]);
		Assert.False(Assert.IsType<bool>(watch.Arguments[2]));
	}

	[Fact]
	public void NotFixed_ItTakesNoRoom()
	{
		(BunitJSModuleInterop module, _) = SpaceModule();

		Render<MokaAppBar>();

		Assert.Empty(module.Invocations["watchSpace"]);
	}

	[Fact]
	public void Unfixing_GivesTheRoomBack_AndFixingAgainWatchesAgain()
	{
		(BunitJSModuleInterop module, BunitJSModuleInterop space) = SpaceModule();
		IRenderedComponent<MokaAppBar> cut = Render<MokaAppBar>(p => p.Add(x => x.Fixed, true));

		cut.Render(p => p.Add(x => x.Fixed, false));
		Assert.Single(space.Invocations["dispose"]);
		cut.Render(p => p.Add(x => x.Fixed, true));

		Assert.Equal(2, module.Invocations["watchSpace"].Count);
	}

	[Fact]
	public async Task Disposing_GivesTheRoomBack()
	{
		(_, BunitJSModuleInterop space) = SpaceModule();
		IRenderedComponent<MokaAppBar> cut = Render<MokaAppBar>(p => p.Add(x => x.Fixed, true));

		await cut.Instance.DisposeAsync();

		Assert.Single(space.Invocations["dispose"]);
	}

	// The page's scroll padding counts a fixed bar as out of view, so focusing a control in it scrolled the page by
	// half a window. Its controls count as sitting just below it instead.
	[Fact]
	public void ItsControls_CountAsInView()
	{
		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations("MokaAppBar.razor.css", ".moka-appbar--fixed ::deep *");

		Assert.Equal("calc(-1 * var(--moka-appbar-space, 0px))", rule["scroll-margin-top"]);
		Assert.Equal("var(--moka-appbar-space, 0px)", rule["scroll-margin-bottom"]);
	}

	private (BunitJSModuleInterop Module, BunitJSModuleInterop Space) SpaceModule()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Moka.Red.Core/moka-space.js");
		BunitJSModuleInterop space = module.SetupModule(invocation => invocation.Identifier == "watchSpace");
		space.Mode = JSRuntimeMode.Loose;
		return (module, space);
	}
}
