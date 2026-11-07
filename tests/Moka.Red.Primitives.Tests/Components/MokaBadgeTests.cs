using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Badge;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaBadgeTests : BunitContext
{
	[Fact]
	public void Renders_RootClass()
	{
		IRenderedComponent<MokaBadge> cut = Render<MokaBadge>(p => p
			.Add(x => x.Content, "5")
			.AddChildContent("<span>Item</span>"));

		IElement div = cut.Find("div");
		Assert.Contains("moka-badge", div.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Shows_Content_InIndicator()
	{
		IRenderedComponent<MokaBadge> cut = Render<MokaBadge>(p => p
			.Add(x => x.Content, "42")
			.AddChildContent("<span>Item</span>"));

		IElement indicator = cut.Find(".moka-badge__indicator");
		Assert.Equal("42", indicator.TextContent.Trim());
	}

	[Fact]
	public void Truncates_CountAboveMax()
	{
		IRenderedComponent<MokaBadge> cut = Render<MokaBadge>(p => p
			.Add(x => x.Content, "150")
			.Add(x => x.MaxCount, 99)
			.AddChildContent("<span>Item</span>"));

		IElement indicator = cut.Find(".moka-badge__indicator");
		Assert.Equal("99+", indicator.TextContent.Trim());
	}

	[Fact]
	public void Dot_Mode_HidesContent()
	{
		IRenderedComponent<MokaBadge> cut = Render<MokaBadge>(p => p
			.Add(x => x.Dot, true)
			.Add(x => x.Content, "5")
			.AddChildContent("<span>Item</span>"));

		IElement indicator = cut.Find(".moka-badge__indicator");
		Assert.Contains("moka-badge__indicator--dot", indicator.ClassName, StringComparison.Ordinal);
		Assert.Empty(indicator.TextContent.Trim());
	}

	[Fact]
	public void Standalone_WhenNoChildContent()
	{
		IRenderedComponent<MokaBadge> cut = Render<MokaBadge>(p => p
			.Add(x => x.Content, "3"));

		IElement indicator = cut.Find(".moka-badge__indicator");
		Assert.Contains("moka-badge__indicator--standalone", indicator.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Invisible_WhenVisibleFalse()
	{
		IRenderedComponent<MokaBadge> cut = Render<MokaBadge>(p => p
			.Add(x => x.Content, "3")
			.Add(x => x.Visible, false)
			.AddChildContent("<span>Item</span>"));

		Assert.Empty(cut.FindAll(".moka-badge__indicator"));
	}

	[Fact]
	public void DefaultColor_IsError()
	{
		IRenderedComponent<MokaBadge> cut = Render<MokaBadge>(p => p
			.Add(x => x.Content, "1")
			.AddChildContent("<span>Item</span>"));

		IElement div = cut.Find("div");
		Assert.Contains("moka-badge--error", div.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void AppliesCustomColor()
	{
		IRenderedComponent<MokaBadge> cut = Render<MokaBadge>(p => p
			.Add(x => x.Content, "1")
			.Add(x => x.Color, MokaColor.Success)
			.AddChildContent("<span>Item</span>"));

		IElement div = cut.Find("div");
		Assert.Contains("moka-badge--success", div.ClassName, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(MokaBadgeTone.Count, "moka-badge--tone-count")]
	[InlineData(MokaBadgeTone.Limited, "moka-badge--tone-limited")]
	[InlineData(MokaBadgeTone.NeedsYou, "moka-badge--tone-needs-you")]
	[InlineData(MokaBadgeTone.Ready, "moka-badge--tone-ready")]
	public void Tone_ReplacesTheColor(MokaBadgeTone tone, string expected)
	{
		IRenderedComponent<MokaBadge> cut = Render<MokaBadge>(p => p
			.Add(x => x.Content, "1")
			.Add(x => x.Tone, tone)
			.Add(x => x.Color, MokaColor.Success));

		IElement root = cut.Find(".moka-badge");
		Assert.Contains(expected, root.ClassName, StringComparison.Ordinal);
		Assert.DoesNotContain("moka-badge--success", root.ClassName, StringComparison.Ordinal);
	}

	// The same blue on every preset. With the accent it would be a second red beside NeedsYou on the default theme.
	[Fact]
	public void TheReadyTone_ReadsInfo_NotTheAccent()
	{
		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations(
			"MokaBadge.razor.css", ".moka-badge.moka-badge--tone-ready .moka-badge__indicator");

		Assert.Equal("var(--moka-color-info-bg)", rule["background-color"]);
		Assert.Equal("var(--moka-color-info-ink)", rule["color"]);
	}

	// A count sits inside a button in MokaTabs, where a div isn't allowed.
	[Fact]
	public void Standalone_IsASpan_AndAroundContentADiv()
	{
		IRenderedComponent<MokaBadge> alone = Render<MokaBadge>(p => p.Add(x => x.Content, "3"));
		IRenderedComponent<MokaBadge> around = Render<MokaBadge>(p => p
			.Add(x => x.Content, "3")
			.AddChildContent("<span>Item</span>"));

		Assert.Equal("SPAN", alone.Find(".moka-badge").TagName);
		Assert.Equal("DIV", around.Find(".moka-badge").TagName);
	}

	[Fact]
	public void Position_TopRight_ByDefault()
	{
		IRenderedComponent<MokaBadge> cut = Render<MokaBadge>(p => p
			.Add(x => x.Content, "1")
			.AddChildContent("<span>Item</span>"));

		IElement indicator = cut.Find(".moka-badge__indicator");
		Assert.Contains("moka-badge__indicator--top-right", indicator.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Position_BottomLeft()
	{
		IRenderedComponent<MokaBadge> cut = Render<MokaBadge>(p => p
			.Add(x => x.Content, "1")
			.Add(x => x.Position, MokaBadgePosition.BottomLeft)
			.AddChildContent("<span>Item</span>"));

		IElement indicator = cut.Find(".moka-badge__indicator");
		Assert.Contains("moka-badge__indicator--bottom-left", indicator.ClassName, StringComparison.Ordinal);
	}
}
