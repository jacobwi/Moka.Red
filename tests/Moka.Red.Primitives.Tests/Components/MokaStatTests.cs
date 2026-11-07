using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Stat;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaStatTests : BunitContext
{
	private const string Css = "MokaStat.razor.css";

	[Fact]
	public void Stat_ByDefault_IsNotATile()
	{
		IRenderedComponent<MokaStat> cut = Render<MokaStat>(p => p.Add(x => x.Value, "48.2"));

		Assert.Equal("moka-stat moka-stat--md", cut.Find(".moka-stat").ClassName);
	}

	[Fact]
	public void Stat_WithoutTile_DoesNotTintFromColor()
	{
		IRenderedComponent<MokaStat> cut = Render<MokaStat>(p => p
			.Add(x => x.Value, "48.2")
			.Add(x => x.Color, MokaColor.Success));

		Assert.Equal("moka-stat moka-stat--md", cut.Find(".moka-stat").ClassName);
	}

	[Fact]
	public void Tile_ReplacesTheSizeClass()
	{
		IRenderedComponent<MokaStat> cut = Render<MokaStat>(p => p
			.Add(x => x.Value, "14 ms")
			.Add(x => x.Label, "Delay")
			.Add(x => x.Size, MokaSize.Lg)
			.Add(x => x.Tile, true));

		IElement tile = cut.Find(".moka-stat");
		Assert.Equal("moka-stat moka-stat--tile", tile.ClassName);
		Assert.Contains("14 ms", cut.Find(".moka-stat__value").TextContent, StringComparison.Ordinal);
		Assert.Equal("Delay", cut.Find(".moka-stat__label").TextContent);
	}

	[Theory]
	[InlineData(MokaColor.Primary, "primary", "var(--moka-color-primary-light)")]
	[InlineData(MokaColor.Secondary, "secondary", "var(--moka-color-secondary)")]
	[InlineData(MokaColor.Error, "error", "var(--moka-color-error)")]
	[InlineData(MokaColor.Warning, "warning", "var(--moka-color-warning)")]
	[InlineData(MokaColor.Success, "success", "var(--moka-color-success)")]
	[InlineData(MokaColor.Info, "info", "var(--moka-color-info)")]
	[InlineData(MokaColor.Surface, "surface", "var(--moka-color-on-surface)")]
	public void Tile_TintsTheValueFromColor(MokaColor color, string name, string token)
	{
		IRenderedComponent<MokaStat> cut = Render<MokaStat>(p => p
			.Add(x => x.Value, "2")
			.Add(x => x.Tile, true)
			.Add(x => x.Color, color));

		Assert.Equal($"moka-stat moka-stat--tile moka-stat--{name}", cut.Find(".moka-stat").ClassName);
		Assert.Equal(token,
			ScopedCss.Declarations(Css, $".moka-stat--tile.moka-stat--{name} .moka-stat__value")["color"]);
	}

	[Fact]
	public void Tile_ReadsTheApprovedValues()
	{
		IReadOnlyDictionary<string, string> tile = ScopedCss.Declarations(Css, ".moka-stat.moka-stat--tile");
		IReadOnlyDictionary<string, string> value = ScopedCss.Declarations(Css, ".moka-stat.moka-stat--tile .moka-stat__value");
		IReadOnlyDictionary<string, string> label = ScopedCss.Declarations(Css, ".moka-stat.moka-stat--tile .moka-stat__label");

		Assert.Equal("0.5625rem 0.625rem", tile["padding"]);
		Assert.Equal("var(--moka-radius-md)", tile["border-radius"]);
		Assert.Equal("var(--moka-color-surface-variant)", tile["background-color"]);
		Assert.Equal("1.0625rem", value["font-size"]);
		Assert.Equal("var(--moka-font-weight-semibold)", value["font-weight"]);
		Assert.Equal("0.71875rem", label["font-size"]);
		Assert.Equal("var(--moka-color-on-surface-variant)", label["color"]);
		Assert.Equal("1", label["opacity"]);
	}

	[Fact]
	public void BigNumber_OnGate_ReadsTheHeadingTokens()
	{
		IReadOnlyDictionary<string, string> value = ScopedCss.Declarations(Css, ".moka-gate .moka-stat__value");

		Assert.Equal("var(--moka-font-family-display)", value["font-family"]);
		Assert.Equal("var(--moka-font-weight-semibold)", value["font-weight"]);
		Assert.Equal("var(--moka-heading-tracking)", value["letter-spacing"]);
		Assert.Equal("1", value["line-height"]);
		Assert.Equal("var(--moka-font-size-display)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-stat--md .moka-stat__value")["font-size"]);
	}

	[Fact]
	public void Suffix_OnGate_IsAMutedUnit()
	{
		IReadOnlyDictionary<string, string> suffix = ScopedCss.Declarations(Css, ".moka-gate .moka-stat__suffix");

		Assert.Equal("var(--moka-color-on-surface-variant)", suffix["color"]);
		Assert.Equal("var(--moka-font-size-mono)", suffix["font-size"]);
	}

	[Fact]
	public void Suffix_RendersInsideTheValue_SoItSharesTheBaseline()
	{
		IRenderedComponent<MokaStat> cut = Render<MokaStat>(p => p
			.Add(x => x.Value, "48.2")
			.Add(x => x.Suffix, "GB today"));

		Assert.Equal("GB today", cut.Find(".moka-stat__value > .moka-stat__suffix").TextContent);
	}
}
