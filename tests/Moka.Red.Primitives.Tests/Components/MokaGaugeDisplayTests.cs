using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Gauge;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// MokaGauge.Display: the app's own text large in the center, such as a letter grade, with the number small under it.
public class MokaGaugeDisplayTests : BunitContext
{
	private const string Stylesheet = "MokaGauge.razor.css";

	[Fact]
	public void Display_TakesTheNumbersPlace_AndTheNumberIsTheSmallLine()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p
			.Add(x => x.Value, 92d)
			.Add(x => x.Display, "A")
			.Add(x => x.Color, MokaColor.Success));

		IElement center = cut.Find(".moka-gauge-center");
		Assert.Equal(2, center.Children.Length);
		Assert.Equal("moka-gauge-value moka-gauge-display", center.Children[0].ClassName);
		Assert.Equal("A", center.Children[0].TextContent);
		Assert.Equal("moka-gauge-figure", center.Children[1].ClassName);
		Assert.Equal("92", center.Children[1].TextContent);
	}

	[Fact]
	public void Display_MakesARing_LikeAWord()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p.Add(x => x.Value, 92d).Add(x => x.Display, "A"));

		Assert.Contains("moka-gauge--ring", cut.Find(".moka-gauge").ClassList);
		Assert.Contains(" 0 1 1 ", cut.Find(".moka-gauge-fill").GetAttribute("d"), StringComparison.Ordinal);
	}

	[Fact]
	public void Display_KeepsTheAngles_WhenTheyAreSet_AsAWordDoes()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p
			.Add(x => x.Value, 92d)
			.Add(x => x.Display, "A")
			.Add(x => x.StartAngle, -120d)
			.Add(x => x.EndAngle, 120d));

		Assert.Equal(Render<MokaGauge>(p => p
				.Add(x => x.Value, 92d)
				.Add(x => x.Word, "Good")
				.Add(x => x.StartAngle, -120d)
				.Add(x => x.EndAngle, 120d)).Find(".moka-gauge-fill").GetAttribute("d"),
			cut.Find(".moka-gauge-fill").GetAttribute("d"));
		Assert.Equal("A", cut.Find(".moka-gauge-display").TextContent);
	}

	[Fact]
	public void Format_WritesTheSmallLine()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p
			.Add(x => x.Value, 92d)
			.Add(x => x.Display, "A")
			.Add(x => x.Format, "0' of 100'"));

		Assert.Equal("92 of 100", cut.Find(".moka-gauge-figure").TextContent);
	}

	[Fact]
	public void ShowValueOff_LeavesOnlyTheDisplay()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p
			.Add(x => x.Value, 92d)
			.Add(x => x.Display, "A")
			.Add(x => x.ShowValue, false));

		Assert.Single(cut.Find(".moka-gauge-center").Children);
		Assert.Empty(cut.FindAll(".moka-gauge-figure"));
	}

	[Fact]
	public void AWord_GoesUnderTheNumber()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p
			.Add(x => x.Value, 92d)
			.Add(x => x.Display, "A")
			.Add(x => x.Word, "Good"));

		string[] classes = cut.Find(".moka-gauge-center").Children.Select(child => child.ClassList[0]!).ToArray();
		Assert.Equal(["moka-gauge-value", "moka-gauge-figure", "moka-gauge-word"], classes);
	}

	[Fact]
	public void AScreenReader_HearsTheDisplay_ThenTheValue_ThenTheWord()
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		try
		{
			Assert.Equal("A, 92", Render<MokaGauge>(p => p.Add(x => x.Value, 92d).Add(x => x.Display, "A"))
				.Find(".moka-gauge").GetAttribute("aria-valuetext"));
			Assert.Equal("A, 92, Good", Render<MokaGauge>(p => p.Add(x => x.Value, 92d).Add(x => x.Display, "A").Add(x => x.Word, "Good"))
				.Find(".moka-gauge").GetAttribute("aria-valuetext"));
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
		}
	}

	[Fact]
	public void TheLabel_OnlyNamesARingWithADisplay()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p
			.Add(x => x.Value, 92d)
			.Add(x => x.Display, "A")
			.Add(x => x.Label, "Security grade"));

		Assert.Empty(cut.FindAll(".moka-gauge-label"));
		Assert.Equal("Security grade", cut.Find(".moka-gauge").GetAttribute("aria-label"));
	}

	[Fact]
	public void OnADial_TheLabelStillShows()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p
			.Add(x => x.Value, 412d)
			.Add(x => x.AutoScale, true)
			.Add(x => x.Display, "Fast")
			.Add(x => x.Label, "Mbit/s down"));

		Assert.Contains("moka-gauge--dial", cut.Find(".moka-gauge").ClassList);
		Assert.Equal("Fast", cut.Find(".moka-gauge-display").TextContent);
		Assert.Equal("Mbit/s down", cut.Find(".moka-gauge-label").TextContent);
	}

	[Fact]
	public void DisplayColor_AddsItsClass_OnlyWhenSet()
	{
		Assert.Equal("moka-gauge-value moka-gauge-display",
			Render<MokaGauge>(p => p.Add(x => x.Display, "A")).Find(".moka-gauge-display").ClassName);
		Assert.Equal("moka-gauge-value moka-gauge-display moka-gauge-display--warning",
			Render<MokaGauge>(p => p.Add(x => x.Display, "C").Add(x => x.DisplayColor, MokaColor.Warning)).Find(".moka-gauge-display").ClassName);
	}

	[Fact]
	public void AnEmptyDisplay_DrawsTheNumber()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p.Add(x => x.Value, 40d).Add(x => x.Display, ""));

		Assert.Equal("moka-gauge moka-gauge--md moka-gauge--primary", cut.Find(".moka-gauge").ClassName);
		Assert.Empty(cut.FindAll(".moka-gauge-display"));
		Assert.Equal("moka-gauge-value", cut.Find(".moka-gauge-center").Children[0].ClassName);
	}

	// The display shares the number's class, so it takes the ring's number font, which is the theme's number font.
	[Fact]
	public void OnARing_TheDisplayIsInTheNumberFont_AndTheFigureInTheFigureFont()
	{
		Assert.Equal("var(--moka-font-family-number)",
			ScopedCss.Declarations(Stylesheet, ".moka-gauge.moka-gauge--ring > .moka-gauge-center > .moka-gauge-value")["font-family"]);
		Assert.Equal("var(--moka-font-family-figure)", ScopedCss.Declarations(Stylesheet, ".moka-gauge-figure")["font-family"]);
		Assert.Equal("tabular-nums", ScopedCss.Declarations(Stylesheet, ".moka-gauge-figure")["font-variant-numeric"]);
		Assert.Equal("max(var(--moka-font-size-xxs), calc(9.09cqi * var(--moka-font-scale)))",
			ScopedCss.Declarations(Stylesheet, ".moka-gauge--ring > .moka-gauge-center > .moka-gauge-figure")["font-size"]);
	}

	[Fact]
	public void DisplayColor_WinsOverTheRingsNumberColor()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Stylesheet)).ReplaceLineEndings("\n");

		int ring = css.IndexOf(".moka-gauge.moka-gauge--ring > .moka-gauge-center > .moka-gauge-value {", StringComparison.Ordinal);
		int dial = css.IndexOf(".moka-gauge.moka-gauge--dial > .moka-gauge-center > .moka-gauge-value {", StringComparison.Ordinal);
		int color = css.IndexOf(".moka-gauge > .moka-gauge-center > .moka-gauge-display.moka-gauge-display--warning", StringComparison.Ordinal);
		Assert.True(ring > 0 && dial > 0 && color > ring && color > dial);
		Assert.Equal("var(--moka-color-warning)",
			ScopedCss.Declarations(Stylesheet, ".moka-gauge > .moka-gauge-center > .moka-gauge-display.moka-gauge-display--warning")["color"]);
	}
}
