using Bunit;
using Moka.Red.Layout.DockLayout;

namespace Moka.Red.Layout.Tests.Components;

// Scrollable="false" is for children that scroll themselves. The modifier classes carry the
// clipping and fill layout, which lives in MokaDockLayout.razor.css.
public class MokaDockScrollableTests : BunitContext
{
	[Fact]
	public void PanelBody_ScrollsByDefault_AndClipsWhenNotScrollable()
	{
		IRenderedComponent<MokaDockPanel> cut = Render<MokaDockPanel>();
		Assert.DoesNotContain("moka-dock-panel-body--no-scroll", cut.Find(".moka-dock-panel-body").ClassList);

		cut.Render(p => p.Add(x => x.Scrollable, false));

		Assert.Contains("moka-dock-panel-body--no-scroll", cut.Find(".moka-dock-panel-body").ClassList);
	}

	[Fact]
	public void ContentArea_ScrollsByDefault_AndClipsWhenNotScrollable()
	{
		IRenderedComponent<MokaDockContent> cut = Render<MokaDockContent>();
		Assert.DoesNotContain("moka-dock-content--no-scroll", cut.Find(".moka-dock-content").ClassList);

		cut.Render(p => p.Add(x => x.Scrollable, false));

		Assert.Contains("moka-dock-content--no-scroll", cut.Find(".moka-dock-content").ClassList);
	}
}
