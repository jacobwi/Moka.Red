using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Gauge;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaGaugeTests : BunitContext
{
	private const string Stylesheet = "MokaGauge.razor.css";

	private IRenderedComponent<MokaGauge> RenderRing(double value = 84) => Render<MokaGauge>(p => p
		.Add(x => x.Value, value)
		.Add(x => x.Word, "Good")
		.Add(x => x.Color, MokaColor.Success));

	private IRenderedComponent<MokaGauge> RenderDial(double value = 412) => Render<MokaGauge>(p => p
		.Add(x => x.Value, value)
		.Add(x => x.AutoScale, true)
		.Add(x => x.Label, "Mbit/s down"));

	[Fact]
	public void NoNewParameter_DrawsTheArcAsItDid()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p.Add(x => x.Value, 40d));

		IElement root = cut.Find(".moka-gauge");
		Assert.Equal("moka-gauge moka-gauge--md moka-gauge--primary", root.ClassName);
		Assert.Null(root.GetAttribute("style"));
		Assert.Null(root.GetAttribute("aria-valuetext"));
		Assert.Equal("Gauge", root.GetAttribute("aria-label"));
		Assert.Equal("100", root.GetAttribute("aria-valuemax"));
		Assert.Equal("0 0 200 200", cut.Find("svg").GetAttribute("viewBox"));

		IElement fill = cut.Find(".moka-gauge-fill");
		Assert.Equal("16", fill.GetAttribute("stroke-width"));
		Assert.Equal("M 34.95 165.05 A 92.00 92.00 0 1 1 165.05 165.05", fill.GetAttribute("d"));
		Assert.Equal("433.54", fill.GetAttribute("stroke-dasharray"));
		Assert.Equal("260.12", fill.GetAttribute("stroke-dashoffset"));

		Assert.Empty(cut.FindAll(".moka-gauge-word"));
		Assert.Empty(cut.FindAll(".moka-gauge-scale"));
	}

	[Fact]
	public void Label_IsStillTheWordUnderTheNumber_WithoutAWord()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p.Add(x => x.Value, 40d).Add(x => x.Label, "CPU"));

		Assert.Equal("CPU", cut.Find(".moka-gauge-label").TextContent);
		Assert.Equal("CPU", cut.Find(".moka-gauge").GetAttribute("aria-label"));
	}

	[Fact]
	public void SizeValue_SetsTheWidthAndHeight()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p.Add(x => x.SizeValue, "6rem"));

		IElement root = cut.Find(".moka-gauge");
		Assert.Equal("width: 6rem; height: 6rem", root.GetAttribute("style"));
		Assert.Contains("moka-gauge--sized", root.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void SizeValue_ReadsABareNumberAsPixels()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p.Add(x => x.SizeValue, "132"));

		Assert.Equal("width: 132px; height: 132px", cut.Find(".moka-gauge").GetAttribute("style"));
	}

	[Fact]
	public void SizeValue_DropsAValueThatCouldEndItsDeclaration()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p.Add(x => x.SizeValue, "10px; position: fixed"));

		Assert.Null(cut.Find(".moka-gauge").GetAttribute("style"));
	}

	[Fact]
	public void SizeValue_SitsBesideTheSpacingAndTheConsumersStyle()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p
			.Add(x => x.SizeValue, "6rem")
			.Add(x => x.MarginValue, "1px")
			.Add(x => x.Style, "color: red"));

		Assert.Equal("margin: 1px; width: 6rem; height: 6rem; color: red", cut.Find(".moka-gauge").GetAttribute("style"));
	}

	[Fact]
	public void Word_DrawsUnderTheNumber()
	{
		IRenderedComponent<MokaGauge> cut = RenderRing();

		IElement center = cut.Find(".moka-gauge-center");
		Assert.Equal("84", center.Children[0].TextContent);
		Assert.Equal("Good", center.Children[1].TextContent);
		Assert.Contains("moka-gauge-word", center.Children[1].ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Word_MakesTheGaugeAFullRing()
	{
		IRenderedComponent<MokaGauge> cut = RenderRing();

		Assert.Contains("moka-gauge--ring", cut.Find(".moka-gauge").ClassName, StringComparison.Ordinal);
		IElement fill = cut.Find(".moka-gauge-fill");
		Assert.Equal("M 100.00 8.00 A 92.00 92.00 0 1 1 100.00 192.00 A 92.00 92.00 0 1 1 100.00 8.00", fill.GetAttribute("d"));
		Assert.Equal("578.05", fill.GetAttribute("stroke-dasharray"));
		Assert.Equal("92.49", fill.GetAttribute("stroke-dashoffset"));
	}

	[Fact]
	public void Word_LeavesAnArcTheConsumerAskedFor()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p
			.Add(x => x.Word, "Good")
			.Add(x => x.StartAngle, -90d)
			.Add(x => x.EndAngle, 90d));

		Assert.Equal("M 8.00 100.00 A 92.00 92.00 0 0 1 192.00 100.00", cut.Find(".moka-gauge-fill").GetAttribute("d"));
	}

	[Fact]
	public void Word_TakesTheGaugesColor_UnlessGivenOne()
	{
		Assert.Contains("moka-gauge-word--success", RenderRing().Find(".moka-gauge-word").ClassName, StringComparison.Ordinal);

		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p
			.Add(x => x.Word, "Poor")
			.Add(x => x.Color, MokaColor.Success)
			.Add(x => x.WordColor, MokaColor.Warning));

		Assert.Contains("moka-gauge-word--warning", cut.Find(".moka-gauge-word").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Word_IsSaidWithTheValue()
	{
		Assert.Equal("84, Good", RenderRing().Find(".moka-gauge").GetAttribute("aria-valuetext"));
	}

	[Fact]
	public void Label_OnARingOnlyNamesIt()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p.Add(x => x.Word, "Good").Add(x => x.Label, "Score"));

		Assert.Empty(cut.FindAll(".moka-gauge-label"));
		Assert.Equal("Score", cut.Find(".moka-gauge").GetAttribute("aria-label"));
	}

	[Fact]
	public void Word_ShowsWithoutTheNumber()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p.Add(x => x.Word, "Good").Add(x => x.ShowValue, false));

		Assert.Empty(cut.FindAll(".moka-gauge-value"));
		Assert.Equal("Good", cut.Find(".moka-gauge-word").TextContent);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void NoWord_IsNoRing(string? word)
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p.Add(x => x.Word, word));

		Assert.Empty(cut.FindAll(".moka-gauge-word"));
		Assert.DoesNotContain("moka-gauge--ring", cut.Find(".moka-gauge").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void FullCircleAngles_DrawARing()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p.Add(x => x.StartAngle, 0d).Add(x => x.EndAngle, 360d));

		Assert.Equal("M 100.00 8.00 A 92.00 92.00 0 1 1 100.00 192.00 A 92.00 92.00 0 1 1 100.00 8.00", cut.Find(".moka-gauge-fill").GetAttribute("d"));
		Assert.Equal("578.05", cut.Find(".moka-gauge-fill").GetAttribute("stroke-dasharray"));
	}

	[Fact]
	public void Ring_Is132PxWithA38PxNumber()
	{
		IReadOnlyDictionary<string, string> root = ScopedCss.Declarations(Stylesheet, ".moka-gauge.moka-gauge--ring.moka-gauge--md");
		Assert.Equal("8.25rem", root["width"]);
		Assert.Equal("8.25rem", root["height"]);

		IReadOnlyDictionary<string, string> number = ScopedCss.Declarations(Stylesheet, ".moka-gauge.moka-gauge--ring > .moka-gauge-center > .moka-gauge-value");
		Assert.Contains("28.79cqi", number["font-size"], StringComparison.Ordinal);
		Assert.Equal("var(--moka-color-on-surface)", number["color"]);
		Assert.Equal("inline-size", ScopedCss.Declarations(Stylesheet, ".moka-gauge.moka-gauge--ring")["container-type"]);
		Assert.Equal("var(--moka-font-weight-semibold)", ScopedCss.Declarations(Stylesheet, ".moka-gauge-word")["font-weight"]);
	}

	[Theory]
	[InlineData("primary", "primary")]
	[InlineData("secondary", "secondary")]
	[InlineData("error", "error")]
	[InlineData("warning", "warning")]
	[InlineData("success", "success")]
	[InlineData("info", "info")]
	[InlineData("surface", "on-surface")]
	public void WordColors_ReadTokens(string color, string token)
	{
		Assert.Equal($"var(--moka-color-{token})", ScopedCss.Declarations(Stylesheet, $".moka-gauge-word--{color}")["color"]);
	}

	[Theory]
	[InlineData(412, 500)]
	[InlineData(500, 500)]
	[InlineData(501, 1000)]
	[InlineData(1001, 2500)]
	[InlineData(2500, 2500)]
	[InlineData(101, 250)]
	[InlineData(7, 10)]
	[InlineData(9.5, 10)]
	[InlineData(0.3, 0.5)]
	[InlineData(100000, 100000)]
	[InlineData(100001, 250000)]
	public void NextRound_IsOneTwoAndAHalfOrFiveTimesAPowerOfTen(double value, double expected)
	{
		Assert.Equal(expected, MokaGauge.NextRound(value), 6);
	}

	[Fact]
	public void AutoScale_IsOffByDefault()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p.Add(x => x.Value, 412d));

		Assert.Equal("100", cut.Find(".moka-gauge").GetAttribute("aria-valuemax"));
		Assert.DoesNotContain("moka-gauge--dial", cut.Find(".moka-gauge").ClassName, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll(".moka-gauge-scale"));
	}

	[Fact]
	public void AutoScale_RaisesTheScaleToTheNextRoundNumber()
	{
		IRenderedComponent<MokaGauge> cut = RenderDial();

		Assert.Equal("500", cut.Find(".moka-gauge").GetAttribute("aria-valuemax"));
		IReadOnlyList<IElement> ends = cut.FindAll(".moka-gauge-scale span");
		Assert.Equal("0", ends[0].TextContent);
		Assert.Equal("500", ends[1].TextContent);
		Assert.Equal("66.35", cut.Find(".moka-gauge-fill").GetAttribute("stroke-dashoffset"));
	}

	[Fact]
	public void AutoScale_DrawsAHalfDial()
	{
		IRenderedComponent<MokaGauge> cut = RenderDial();

		Assert.Contains("moka-gauge--dial", cut.Find(".moka-gauge").ClassName, StringComparison.Ordinal);
		Assert.Equal("0 0 280 150", cut.Find("svg").GetAttribute("viewBox"));
		IElement fill = cut.Find(".moka-gauge-fill");
		Assert.Equal("12", fill.GetAttribute("stroke-width"));
		Assert.Equal("M 20.00 140.00 A 120.00 120.00 0 0 1 260.00 140.00", fill.GetAttribute("d"));
		Assert.Equal("376.99", fill.GetAttribute("stroke-dasharray"));
	}

	[Fact]
	public void AutoScale_DrawsTheLabelUnderTheNumber()
	{
		IRenderedComponent<MokaGauge> cut = RenderDial();

		IElement center = cut.Find(".moka-gauge-center");
		Assert.Equal("412", center.Children[0].TextContent);
		Assert.Equal("Mbit/s down", center.Children[1].TextContent);
	}

	[Fact]
	public void AutoScale_KeepsMaxWhileTheValueFits()
	{
		IRenderedComponent<MokaGauge> cut = RenderDial(80);

		Assert.Equal("100", cut.Find(".moka-gauge").GetAttribute("aria-valuemax"));
		Assert.Equal("100", cut.FindAll(".moka-gauge-scale span")[1].TextContent);
	}

	[Fact]
	public void AutoScale_StaysUpWhenTheValueFalls()
	{
		IRenderedComponent<MokaGauge> cut = RenderDial();

		cut.Render(p => p.Add(x => x.Value, 12d));

		Assert.Equal("500", cut.Find(".moka-gauge").GetAttribute("aria-valuemax"));
		Assert.Equal("12", cut.Find(".moka-gauge-value").TextContent);
	}

	[Fact]
	public void AutoScale_KeepsGrowing()
	{
		IRenderedComponent<MokaGauge> cut = RenderDial();

		cut.Render(p => p.Add(x => x.Value, 600d));

		Assert.Equal("1000", cut.Find(".moka-gauge").GetAttribute("aria-valuemax"));
	}

	[Fact]
	public void AutoScale_StartsOver_WhenMaxChanges()
	{
		IRenderedComponent<MokaGauge> cut = RenderDial();

		cut.Render(p => p.Add(x => x.Max, 50d).Add(x => x.Value, 10d));

		Assert.Equal("50", cut.Find(".moka-gauge").GetAttribute("aria-valuemax"));
	}

	[Fact]
	public void AutoScale_IgnoresAValueThatIsNotANumber()
	{
		IRenderedComponent<MokaGauge> cut = RenderDial(double.PositiveInfinity);

		Assert.Equal("100", cut.Find(".moka-gauge").GetAttribute("aria-valuemax"));
	}

	[Fact]
	public void AutoScale_PrintsAScaleWithAFraction_ThoughTheFormatHasNone()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p
			.Add(x => x.Value, 2d)
			.Add(x => x.Max, 1d)
			.Add(x => x.AutoScale, true));

		Assert.Equal(2.5.ToString(CultureInfo.CurrentCulture), cut.FindAll(".moka-gauge-scale span")[1].TextContent);
	}

	[Fact]
	public void AutoScale_LeavesTheHeightToTheWidth()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p.Add(x => x.AutoScale, true).Add(x => x.SizeValue, "200px"));

		Assert.Equal("width: 200px", cut.Find(".moka-gauge").GetAttribute("style"));
	}

	[Fact]
	public void Dial_IsAtMost280PxWideWithA40PxNumber()
	{
		IReadOnlyDictionary<string, string> root = ScopedCss.Declarations(Stylesheet, ".moka-gauge.moka-gauge--dial");
		Assert.Equal("17.5rem", root["width"]);
		Assert.Equal("100%", root["max-width"]);

		IReadOnlyDictionary<string, string> number = ScopedCss.Declarations(Stylesheet, ".moka-gauge.moka-gauge--dial > .moka-gauge-center > .moka-gauge-value");
		Assert.Contains("14.29cqi", number["font-size"], StringComparison.Ordinal);
	}

	[Fact]
	public void TheNewRules_SitAboveTheGateBlock()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Stylesheet));

		Assert.True(css.IndexOf(".moka-gauge-scale", StringComparison.Ordinal) < css.IndexOf("/* Gate look.", StringComparison.Ordinal));
	}
}
