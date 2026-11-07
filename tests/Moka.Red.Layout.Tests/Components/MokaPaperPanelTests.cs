using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Layout.Paper;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

public class MokaPaperPanelTests : BunitContext
{
	[Fact]
	public void Default_KeepsItsClassesAndRadius()
	{
		IElement paper = Render<MokaPaper>().Find(".moka-paper");

		Assert.Equal("moka-paper moka-paper--elevation-0 moka-paper--rounded", paper.ClassName);
		Assert.Equal("border-radius: var(--moka-radius-md)", paper.GetAttribute("style"));
	}

	[Fact]
	public void Panel_AddsItsClass()
	{
		IElement paper = Render<MokaPaper>(p => p.Add(x => x.Panel, true)).Find(".moka-paper");

		Assert.Contains("moka-paper--panel", paper.ClassList);
	}

	// A default radius written inline would beat the panel's corner in its stylesheet.
	[Fact]
	public void Panel_WritesNoDefaultRadius()
	{
		IElement paper = Render<MokaPaper>(p => p.Add(x => x.Panel, true)).Find(".moka-paper");

		Assert.Null(paper.GetAttribute("style"));
		Assert.DoesNotContain("moka-paper--rounded", paper.ClassList);
	}

	[Fact]
	public void Panel_KeepsAnExplicitRadius()
	{
		IElement paper = Render<MokaPaper>(p => p
			.Add(x => x.Panel, true)
			.Add(x => x.Rounded, MokaRounding.None)).Find(".moka-paper");

		Assert.Equal("border-radius: 0", paper.GetAttribute("style"));
	}

	[Fact]
	public void PanelRule_ReadsItsTokens()
	{
		IReadOnlyDictionary<string, string> panel = ScopedCss.Declarations("MokaPaper.razor.css", ".moka-paper--panel");

		Assert.Equal("var(--moka-color-surface-variant)", panel["background"]);
		Assert.Equal("var(--moka-radius-lg)", panel["border-radius"]);
		Assert.Equal("var(--moka-panel-padding)", panel["padding"]);
		Assert.Equal("none", panel["box-shadow"]);
	}
}
