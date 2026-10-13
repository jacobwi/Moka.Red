using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;
using Moka.Red.Layout.StatusBar;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// Enter and Space are handled by Core's moka-keys.js for the item itself, so these tests cover the
// wiring: which items are buttons, and when the key listener is bound.
public class MokaStatusBarItemTests : BunitContext
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	[Fact]
	public async Task OnlyAnItemWithOnClick_IsAButtonBoundToTheKeys()
	{
		BunitJSModuleInterop keys = JSInterop.SetupModule(KeysModule);
		keys.SetupVoid("bindActivation", _ => true).SetVoidResult();
		int clicks = 0;

		IRenderedComponent<MokaStatusBarItem> clickable = Render<MokaStatusBarItem>(p => p
			.Add(x => x.Text, "UTF-8")
			.Add(x => x.Tooltip, "Change encoding")
			.Add(x => x.OnClick, _ => clicks++));
		IRenderedComponent<MokaStatusBarItem> plain = Render<MokaStatusBarItem>(p => p.Add(x => x.Text, "Ln 42"));

		IElement item = clickable.Find(".moka-statusbar-item");
		Assert.Equal("button", item.GetAttribute("role"));
		Assert.Equal("0", item.GetAttribute("tabindex"));
		Assert.False(item.HasAttribute("title"));

		JSRuntimeInvocation bind = keys.VerifyInvoke("bindActivation");
		Assert.Equal(item.GetAttribute("blazor:elementReference"), Assert.IsType<ElementReference>(bind.Arguments[0]).Id);
		Assert.Single(bind.Arguments);

		IElement text = plain.Find(".moka-statusbar-item");
		Assert.False(text.HasAttribute("role"));
		Assert.False(text.HasAttribute("tabindex"));

		// The key listener activates the item with click(), so this is the path Enter and Space take.
		await clickable.Find(".moka-statusbar-item").ClickAsync(new MouseEventArgs());
		Assert.Equal(1, clicks);
	}

	// The tooltip was a title, so the browser's own pale, late hint showed instead of Moka's. A status bar sits at the
	// bottom of the window, so the tooltip opens above it.
	[Fact]
	public void AnIconOnlyItem_IsNamedByATooltipAbove()
	{
		IRenderedComponent<MokaStatusBarItem> cut = Render<MokaStatusBarItem>(p => p
			.Add(x => x.Icon, MokaIcons.Status.Bell)
			.Add(x => x.Tooltip, "3 notifications"));

		IconTooltip.AssertNamed(cut.Find(".moka-statusbar-item"), "3 notifications", MokaTooltipPosition.Top);
	}

	// Visible text stays the item's name; the tooltip describes it, as the title did.
	[Fact]
	public void AnItemWithText_KeepsItsTextAsTheName_AndTheTooltipDescribesIt()
	{
		IRenderedComponent<MokaStatusBarItem> cut = Render<MokaStatusBarItem>(p => p
			.Add(x => x.Text, "UTF-8")
			.Add(x => x.Tooltip, "Change encoding"));

		IElement item = cut.Find(".moka-statusbar-item");
		Assert.False(item.HasAttribute("title"));
		Assert.False(item.HasAttribute("aria-label"));
		Assert.Equal("Change encoding", item.GetAttribute("aria-description"));
		// The tooltip's element is what the bar lays out, so it carries a class an app's CSS can reach.
		Assert.Contains("moka-statusbar-item-tooltip", item.ParentElement!.ClassList);
		IElement popup = Assert.Single(item.ParentElement!.Children, c => c.ClassList.Contains("moka-tooltip-popup"));
		Assert.Equal("Change encoding", popup.TextContent.Trim());
		Assert.Contains("moka-tooltip-popup--top", popup.ClassList);
	}

	// A status bar tooltip often says more than a name, so it wraps instead of running off the window.
	[Fact]
	public void ALongTooltip_Wraps()
	{
		IRenderedComponent<MokaStatusBarItem> cut = Render<MokaStatusBarItem>(p => p
			.Add(x => x.Text, "main")
			.Add(x => x.Tooltip, "Branch main, 3 commits ahead of origin/main and 1 behind, last fetched 2 minutes ago"));

		IElement popup = cut.Find(".moka-tooltip-popup");
		Assert.Contains("moka-tooltip-popup--wrap", popup.ClassList);
		Assert.Contains("--moka-tooltip-max-width: 20rem", popup.GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void AnItemWithoutATooltip_HasNoTooltipAroundIt()
	{
		IRenderedComponent<MokaStatusBarItem> cut = Render<MokaStatusBarItem>(p => p.Add(x => x.Text, "Ln 42"));

		Assert.Empty(cut.FindAll(".moka-tooltip-trigger"));
		Assert.False(cut.Find(".moka-statusbar-item").HasAttribute("aria-description"));
	}

	// A tooltip that comes or goes draws the item inside or outside the tooltip's element, which is a new element,
	// so Enter and Space are bound to that one too.
	[Fact]
	public void ATooltipThatComesOrGoes_BindsTheKeysToTheNewItem()
	{
		BunitJSModuleInterop keys = JSInterop.SetupModule(KeysModule);
		keys.SetupVoid("bindActivation", _ => true).SetVoidResult();

		IRenderedComponent<MokaStatusBarItem> cut = Render<MokaStatusBarItem>(p => p
			.Add(x => x.Text, "UTF-8")
			.Add(x => x.OnClick, _ => { }));
		cut.Render(p => p.Add(x => x.Tooltip, "Change encoding"));
		cut.Render(p => p.Add(x => x.Text, "UTF-16"));

		Assert.Equal(2, keys.Invocations["bindActivation"].Count);
	}
}
