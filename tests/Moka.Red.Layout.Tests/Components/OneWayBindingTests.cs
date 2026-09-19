using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Layout.Panel;

namespace Moka.Red.Layout.Tests.Components;

// MokaPanel used to write the user's change into its own Collapsed parameter. Blazor passes every
// parameter again whenever the parent renders, so with a one-way value the parent's next render put
// the old value back. Without CollapsedChanged it did not even re-render after the click.
public class OneWayBindingTests : BunitContext
{
	[Fact]
	public async Task Panel_StaysExpanded_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaPanel> cut = RenderCollapsedPanel();

		await cut.Find(".moka-panel-toggle").ClickAsync(new MouseEventArgs());
		Assert.DoesNotContain("moka-panel--collapsed", cut.Find(".moka-panel").ClassList);

		cut.Render(p => p.Add(x => x.Collapsed, true));

		Assert.DoesNotContain("moka-panel--collapsed", cut.Find(".moka-panel").ClassList);
	}

	[Fact]
	public async Task Panel_FollowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaPanel> cut = RenderCollapsedPanel();

		await cut.Find(".moka-panel-toggle").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Collapsed, false));
		cut.Render(p => p.Add(x => x.Collapsed, true));

		Assert.Contains("moka-panel--collapsed", cut.Find(".moka-panel").ClassList);
	}

	private IRenderedComponent<MokaPanel> RenderCollapsedPanel() => Render<MokaPanel>(p => p
		.Add(x => x.Title, "Filters")
		.Add(x => x.Collapsible, true)
		.Add(x => x.Collapsed, true)
		.AddChildContent("<p>Body</p>"));
}
