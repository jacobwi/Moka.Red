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

	// The number tokens read the heading's font and tracking unless MokaTheme.WithNumberFont says otherwise.
	[Fact]
	public void BigNumber_OnGate_ReadsTheNumberTokens()
	{
		IReadOnlyDictionary<string, string> value = ScopedCss.Declarations(Css, ".moka-gate .moka-stat__value");

		Assert.Equal("var(--moka-font-family-number)", value["font-family"]);
		Assert.Equal("var(--moka-font-weight-semibold)", value["font-weight"]);
		Assert.Equal("var(--moka-number-tracking)", value["letter-spacing"]);
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

	[Fact]
	public void WithoutVisual_TheValueSitsDirectlyInTheStat()
	{
		IRenderedComponent<MokaStat> cut = Render<MokaStat>(p => p.Add(x => x.Value, "48.2"));

		Assert.Single(cut.FindAll(".moka-stat > .moka-stat__value"));
		Assert.Empty(cut.FindAll(".moka-stat__row, .moka-stat__visual"));
	}

	[Fact]
	public void Visual_SharesTheValueRow()
	{
		IRenderedComponent<MokaStat> cut = Render<MokaStat>(p => p
			.Add(x => x.Value, "48.2")
			.Add(x => x.Suffix, "GB")
			.Add(x => x.Label, "Today")
			.Add(x => x.Visual, "<span class=\"spark\"></span>"));

		IElement row = cut.Find(".moka-stat > .moka-stat__row");
		Assert.Equal(["moka-stat__value", "moka-stat__visual"], row.Children.Select(c => c.ClassName));
		Assert.Single(cut.FindAll(".moka-stat__visual > .spark"));
		Assert.Equal("GB", cut.Find(".moka-stat__row > .moka-stat__value > .moka-stat__suffix").TextContent);
		Assert.Equal("Today", cut.Find(".moka-stat > .moka-stat__label").TextContent);
	}

	[Fact]
	public void Visual_OnATile_StaysInTheRow()
	{
		IRenderedComponent<MokaStat> cut = Render<MokaStat>(p => p
			.Add(x => x.Value, "62%")
			.Add(x => x.Tile, true)
			.Add(x => x.Visual, "<span class=\"meter\"></span>"));

		Assert.Equal("moka-stat moka-stat--tile", cut.Find(".moka-stat").ClassName);
		Assert.Single(cut.FindAll(".moka-stat--tile > .moka-stat__row > .moka-stat__visual > .meter"));
	}

	[Fact]
	public void Visual_TakesWhatTheValueLeaves_AndWrapsUnderItWhenThatIsTooLittle()
	{
		IReadOnlyDictionary<string, string> row = ScopedCss.Declarations(Css, ".moka-stat__row");
		IReadOnlyDictionary<string, string> visual = ScopedCss.Declarations(Css, ".moka-stat__visual");

		Assert.Equal("flex", row["display"]);
		Assert.Equal("wrap", row["flex-wrap"]);
		Assert.Equal("center", row["align-items"]);
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-stat__row > .moka-stat__value")["flex"]);
		Assert.Equal("1 1 4rem", visual["flex"]);
		Assert.Equal("0", visual["min-width"]);
		Assert.Equal("minmax(0, 1fr)", visual["grid-template-columns"]);
	}

	[Fact]
	public void Mono_AddsItsClass_OnATileToo()
	{
		IRenderedComponent<MokaStat> plain = Render<MokaStat>(p => p
			.Add(x => x.Value, "1 024")
			.Add(x => x.Mono, true));
		IRenderedComponent<MokaStat> tile = Render<MokaStat>(p => p
			.Add(x => x.Value, "1 024")
			.Add(x => x.Mono, true)
			.Add(x => x.Tile, true));

		Assert.Equal("moka-stat moka-stat--md moka-stat--mono", plain.Find(".moka-stat").ClassName);
		Assert.Equal("moka-stat moka-stat--tile moka-stat--mono", tile.Find(".moka-stat").ClassName);
	}

	[Fact]
	public void Mono_DrawsTheValueInTheMonoFontWithTabularFigures()
	{
		IReadOnlyDictionary<string, string> value = ScopedCss.Declarations(Css, ".moka-stat.moka-stat--mono .moka-stat__value");

		Assert.Equal("var(--moka-font-family-mono)", value["font-family"]);
		Assert.Equal("tabular-nums", value["font-variant-numeric"]);
		Assert.Equal("normal", value["letter-spacing"]);
	}

	// The tile rule has the same specificity and sets the UI font, so the mono rule has to come after it.
	[Fact]
	public void Mono_ComesAfterTheTileValueRule()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css));

		Assert.True(css.IndexOf(".moka-stat.moka-stat--mono .moka-stat__value", StringComparison.Ordinal)
		            > css.IndexOf(".moka-stat.moka-stat--tile .moka-stat__value", StringComparison.Ordinal));
	}
}
