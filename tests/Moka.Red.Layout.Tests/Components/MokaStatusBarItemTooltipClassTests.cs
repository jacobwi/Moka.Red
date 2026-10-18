using AngleSharp.Dom;
using Bunit;
using Moka.Red.Layout.StatusBar;

namespace Moka.Red.Layout.Tests.Components;

// With a Tooltip the item is wrapped, and the wrapper is what the status bar's flex layout sees. Class stayed on
// the item inside it, so sizing one item meant :has(> .my-class) selectors on the wrapper.
public class MokaStatusBarItemTooltipClassTests : BunitContext
{
	public MokaStatusBarItemTooltipClassTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void TooltipClassReachesTheWrapper()
	{
		IRenderedComponent<MokaStatusBarItem> cut = Render<MokaStatusBarItem>(p => p
			.Add(x => x.Text, "main")
			.Add(x => x.Tooltip, "Current branch")
			.Add(x => x.TooltipClass, "shell-branch")
			.Add(x => x.TooltipStyle, "min-width: 8rem"));

		IElement wrapper = cut.Find(".moka-tooltip-trigger");
		Assert.Contains("moka-statusbar-item-tooltip", wrapper.ClassList);
		Assert.Contains("shell-branch", wrapper.ClassList);
		Assert.Contains("min-width: 8rem", wrapper.GetAttribute("style"), StringComparison.Ordinal);
	}

	// Class keeps styling the item itself, with or without a tooltip, so neither parameter takes the other's job.
	[Fact]
	public void ClassStaysOnTheItem()
	{
		IRenderedComponent<MokaStatusBarItem> cut = Render<MokaStatusBarItem>(p => p
			.Add(x => x.Text, "main")
			.Add(x => x.Tooltip, "Current branch")
			.Add(x => x.Class, "shell-accent")
			.Add(x => x.TooltipClass, "shell-branch"));

		Assert.Contains("shell-accent", cut.Find(".moka-statusbar-item").ClassList);
		Assert.DoesNotContain("shell-accent", cut.Find(".moka-tooltip-trigger").ClassList);
		Assert.DoesNotContain("shell-branch", cut.Find(".moka-statusbar-item").ClassList);
	}

	// Without a tooltip the item is the flex item, so Class already reaches what the layout sees.
	[Fact]
	public void WithoutATooltipTheItemIsWhatTheLayoutSees()
	{
		IRenderedComponent<MokaStatusBarItem> cut = Render<MokaStatusBarItem>(p => p
			.Add(x => x.Text, "main")
			.Add(x => x.Class, "shell-accent"));

		Assert.Empty(cut.FindAll(".moka-tooltip-trigger"));
		Assert.Contains("shell-accent", cut.Find(".moka-statusbar-item").ClassList);
	}

	// The wrapper keeps its own class when the consumer passes none, so the status bar's own rules still apply.
	[Fact]
	public void TheWrapperKeepsItsOwnClassWithoutOne()
	{
		IRenderedComponent<MokaStatusBarItem> cut = Render<MokaStatusBarItem>(p => p
			.Add(x => x.Text, "main")
			.Add(x => x.Tooltip, "Current branch"));

		Assert.Equal(
			["moka-tooltip-trigger", "moka-statusbar-item-tooltip"],
			cut.Find(".moka-tooltip-trigger").ClassList);
	}
}
