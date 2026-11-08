using AngleSharp.Dom;
using Bunit;
using Moka.Red.Layout.StatusBar;

namespace Moka.Red.Layout.Tests.Components;

// The rule between items: an app draws it with the component instead of styling the items' neighbours itself.
public class MokaStatusBarSeparatorTests : BunitContext
{
	public MokaStatusBarSeparatorTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void Renders_AVerticalSeparatorWithNoTextOrTabStop()
	{
		IRenderedComponent<MokaStatusBarSeparator> cut = Render<MokaStatusBarSeparator>();

		IElement rule = cut.Find(".moka-statusbar-separator");
		Assert.Equal("separator", rule.GetAttribute("role"));
		Assert.Equal("vertical", rule.GetAttribute("aria-orientation"));
		Assert.Empty(rule.TextContent);
		Assert.False(rule.HasAttribute("tabindex"));
	}

	[Fact]
	public void Takes_ClassAndStyle_LikeEveryComponent()
	{
		IRenderedComponent<MokaStatusBarSeparator> cut = Render<MokaStatusBarSeparator>(parameters => parameters
			.Add(p => p.Class, "app-rule")
			.Add(p => p.Style, "margin: 0 4px"));

		IElement rule = cut.Find(".moka-statusbar-separator");
		Assert.Contains("app-rule", rule.ClassName, StringComparison.Ordinal);
		Assert.Equal("margin: 0 4px", rule.GetAttribute("style"));
	}

	[Fact]
	public void Sits_BetweenItemsInTheBar()
	{
		IRenderedComponent<MokaStatusBar> bar = Render<MokaStatusBar>(parameters => parameters.AddChildContent(
			builder =>
			{
				builder.AddContent(0, "4 hosts");
				builder.OpenComponent(1, typeof(MokaStatusBarSeparator));
				builder.CloseComponent();
				builder.AddContent(2, "no sessions");
			}));

		// The rule sits in the bar's own flex row between the two texts, not before or after them.
		IElement start = bar.Find(".moka-statusbar-start");
		string shape = string.Concat(start.ChildNodes.Select(node => node.NodeType == AngleSharp.Dom.NodeType.Text ? "t" : "r"));
		Assert.Equal("trt", shape);
	}
}
