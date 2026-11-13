using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Layout.GridBackground;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// The content was a block as tall as what was in it, so a welcome pitch couldn't sit in the middle of the background
// (an app made the root a grid of its own). FillContent, Justify and Align place it; today's block stays the default.
public class MokaGridBackgroundFillTests : BunitContext
{
	private const string Css = "MokaGridBackground.razor.css";

	public MokaGridBackgroundFillTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void TheContent_IsAPlainBlockByDefault()
	{
		IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p.AddChildContent("<p>Hi</p>"));

		Assert.DoesNotContain("moka-grid-bg--fill", cut.Find(".moka-grid-bg").ClassList);
		IElement content = cut.Find(".moka-grid-bg__content");
		Assert.Equal("moka-grid-bg__content", content.ClassName);
		Assert.False(content.HasAttribute("style"));
	}

	[Fact]
	public void FillContent_MakesTheContentFillTheBackground_AsAColumn()
	{
		IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p.Add(x => x.FillContent, true));

		Assert.Contains("moka-grid-bg--fill", cut.Find(".moka-grid-bg").ClassList);
		Assert.False(cut.Find(".moka-grid-bg__content").HasAttribute("style"));

		IReadOnlyDictionary<string, string> root = ScopedCss.Declarations(Css, ".moka-grid-bg--fill");
		Assert.Equal("flex", root["display"]);
		Assert.Equal("column", root["flex-direction"]);

		IReadOnlyDictionary<string, string> content = ScopedCss.Declarations(Css, ".moka-grid-bg--fill > .moka-grid-bg__content");
		Assert.Equal("flex", content["display"]);
		Assert.Equal("1 1 auto", content["flex"]);
		Assert.Equal("column", content["flex-direction"]);
	}

	[Theory]
	[InlineData(MokaJustify.Center, "justify-content: center")]
	[InlineData(MokaJustify.End, "justify-content: flex-end")]
	[InlineData(MokaJustify.SpaceBetween, "justify-content: space-between")]
	public void Justify_PlacesTheChildrenFromTopToBottom_AndFills(MokaJustify justify, string expected)
	{
		IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p.Add(x => x.Justify, justify));

		Assert.Contains("moka-grid-bg--fill", cut.Find(".moka-grid-bg").ClassList);
		Assert.Equal(expected, cut.Find(".moka-grid-bg__content").GetAttribute("style"));
	}

	[Theory]
	[InlineData(MokaAlign.Center, "align-items: center")]
	[InlineData(MokaAlign.Start, "align-items: flex-start")]
	[InlineData(MokaAlign.Stretch, "align-items: stretch")]
	public void Align_PlacesTheChildrenFromSideToSide_AndFills(MokaAlign align, string expected)
	{
		IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p.Add(x => x.Align, align));

		Assert.Contains("moka-grid-bg--fill", cut.Find(".moka-grid-bg").ClassList);
		Assert.Equal(expected, cut.Find(".moka-grid-bg__content").GetAttribute("style"));
	}

	[Fact]
	public void BothTogether_CenterAPitch()
	{
		IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p
			.Add(x => x.Justify, MokaJustify.Center)
			.Add(x => x.Align, MokaAlign.Center)
			.Add(x => x.MinHeight, "20rem"));

		Assert.Equal("justify-content: center; align-items: center", cut.Find(".moka-grid-bg__content").GetAttribute("style"));
		Assert.Contains("min-height: 20rem", cut.Find(".moka-grid-bg").GetAttribute("style"), StringComparison.Ordinal);
	}

	// A background can hold another, and the outer one's fill must not reach the inner one's content.
	[Fact]
	public void TheFill_ReachesOnlyItsOwnContent() =>
		Assert.True(ScopedCss.Declarations(Css, ".moka-grid-bg--fill .moka-grid-bg__content").Count == 0);
}
