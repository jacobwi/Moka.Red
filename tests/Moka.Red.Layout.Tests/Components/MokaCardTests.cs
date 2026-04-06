using AngleSharp.Dom;
using Bunit;
using Moka.Red.Layout.Card;

namespace Moka.Red.Layout.Tests.Components;

public class MokaCardTests : BunitContext
{
	[Fact]
	public void Renders_RootClass()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>();

		IElement el = cut.Find(".moka-card");
		Assert.NotNull(el);
	}

	[Fact]
	public void DefaultElevation_Is1()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>();

		IElement el = cut.Find(".moka-card");
		Assert.Contains("moka-card--elevation-1", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Elevation_AppliesClass()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Elevation, 3));

		IElement el = cut.Find(".moka-card");
		Assert.Contains("moka-card--elevation-3", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Outlined_AppliesClass()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Outlined, true));

		IElement el = cut.Find(".moka-card");
		Assert.Contains("moka-card--outlined", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Title_RendersInHeader()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Title, "My Card"));

		IElement title = cut.Find(".moka-card-title");
		Assert.Equal("My Card", title.TextContent);
	}

	[Fact]
	public void Subtitle_RendersInHeader()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Title, "Card")
			.Add(x => x.Subtitle, "Subtitle text"));

		IElement subtitle = cut.Find(".moka-card-subtitle");
		Assert.Equal("Subtitle text", subtitle.TextContent);
	}

	[Fact]
	public void Clickable_AddsClass()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Clickable, true));

		IElement el = cut.Find(".moka-card");
		Assert.Contains("moka-card--clickable", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void FullWidth_AddsClass()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.FullWidth, true));

		IElement el = cut.Find(".moka-card");
		Assert.Contains("moka-card--full-width", el.ClassName, StringComparison.Ordinal);
	}
}
