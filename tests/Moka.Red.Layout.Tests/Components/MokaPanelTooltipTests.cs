using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Layout.Panel;

namespace Moka.Red.Layout.Tests.Components;

// The collapse toggle shows only a chevron and had no hover name. It stays named by the panel's title, as a disclosure
// is, with aria-expanded for its state; the tooltip says what a click does, below the header's row and lined up with
// the panel's edge, where the toggle ends that row.
public class MokaPanelTooltipTests : BunitContext
{
	[Fact]
	public async Task TheToggle_SaysWhatAClickDoes()
	{
		IRenderedComponent<MokaPanel> cut = Render<MokaPanel>(p => p
			.Add(x => x.Title, "Details")
			.Add(x => x.Collapsible, true));

		Assert.Equal("Collapse", Popup(cut).TextContent.Trim());
		Assert.Contains("moka-tooltip-popup--bottom-end", Popup(cut).ClassList);

		await Toggle(cut).ClickAsync(new MouseEventArgs());

		Assert.Equal("Expand", Popup(cut).TextContent.Trim());
	}

	[Fact]
	public void TheToggle_KeepsTheTitleAsItsName()
	{
		IRenderedComponent<MokaPanel> cut = Render<MokaPanel>(p => p
			.Add(x => x.Title, "Details")
			.Add(x => x.Collapsible, true));

		IElement toggle = Toggle(cut);
		Assert.False(toggle.HasAttribute("title"));
		Assert.False(toggle.HasAttribute("aria-label"));
		Assert.Equal("Details", cut.Find($"#{toggle.GetAttribute("aria-labelledby")}").TextContent);
		Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
	}

	[Fact]
	public void WithoutATitle_TheToggleIsNamedByItsLabel()
	{
		IRenderedComponent<MokaPanel> cut = Render<MokaPanel>(p => p
			.Add(x => x.Collapsible, true)
			.Add(x => x.Actions, builder => builder.AddContent(0, "actions")));

		IElement toggle = Toggle(cut);
		Assert.Equal("Toggle panel", toggle.GetAttribute("aria-label"));
		Assert.Equal("Collapse", Popup(cut).TextContent.Trim());
	}

	private static IElement Toggle(IRenderedComponent<MokaPanel> cut) => cut.Find(".moka-panel-toggle");

	private static IElement Popup(IRenderedComponent<MokaPanel> cut) =>
		Toggle(cut).ParentElement!.QuerySelector(":scope > .moka-tooltip-popup")!;
}
