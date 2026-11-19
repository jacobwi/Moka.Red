using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Gauge;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// MokaGauge.WarnAt, CriticalAt and LowIsBad: the meter's limits on the gauge, high or low being the bad end.
public class MokaGaugeLimitTests : BunitContext
{
	private const string Stylesheet = "MokaGauge.razor.css";

	private IRenderedComponent<MokaGauge> RenderBattery(double charge, string? warnText = null) => Render<MokaGauge>(p => p
		.Add(x => x.Value, charge)
		.Add(x => x.Label, "Battery")
		.Add(x => x.WarnAt, 0.2)
		.Add(x => x.CriticalAt, 0.05)
		.Add(x => x.LowIsBad, true)
		.Add(x => x.WarnText, warnText));

	private IRenderedComponent<MokaGauge> RenderDisk(double used) => Render<MokaGauge>(p => p
		.Add(x => x.Value, used)
		.Add(x => x.Label, "Disk")
		.Add(x => x.WarnAt, 0.8)
		.Add(x => x.CriticalAt, 0.95));

	[Fact]
	public void LimitsThatAreNotReached_DrawTheGaugeAsItWas()
	{
		IRenderedComponent<MokaGauge> disk = RenderDisk(40);
		IRenderedComponent<MokaGauge> plain = Render<MokaGauge>(p => p.Add(x => x.Value, 40d).Add(x => x.Label, "Disk"));

		disk.MarkupMatches(plain.Markup);
		Assert.Equal("moka-gauge moka-gauge--md moka-gauge--primary", disk.Find(".moka-gauge").ClassName);
		Assert.Null(disk.Find(".moka-gauge").GetAttribute("aria-valuetext"));
		Assert.Empty(disk.FindAll(".moka-gauge-state"));
	}

	[Fact]
	public void ByDefault_AtOrOverWarnAt_SaysNearlyFull_WithTheWarningIcon()
	{
		IRenderedComponent<MokaGauge> cut = RenderDisk(80);

		IElement root = cut.Find(".moka-gauge");
		Assert.Equal("moka-gauge moka-gauge--md moka-gauge--primary moka-gauge--warn", root.ClassName);
		Assert.Equal("Nearly full", cut.Find(".moka-gauge-state").TextContent.Trim());
		Assert.Single(cut.FindAll(".moka-gauge-state svg"));
		Assert.Equal("80, Nearly full", root.GetAttribute("aria-valuetext"));
	}

	[Fact]
	public void ByDefault_AtOrOverCriticalAt_SaysFull_InTheErrorColor()
	{
		IRenderedComponent<MokaGauge> cut = RenderDisk(97);

		IElement root = cut.Find(".moka-gauge");
		Assert.Contains("moka-gauge--critical", root.ClassList);
		Assert.DoesNotContain("moka-gauge--warn", root.ClassList);
		Assert.Equal("Full", cut.Find(".moka-gauge-state").TextContent.Trim());
		Assert.Equal("97, Full", root.GetAttribute("aria-valuetext"));
	}

	[Fact]
	public void ByDefault_UnderWarnAt_NothingChanges_EvenNearEmpty()
	{
		IRenderedComponent<MokaGauge> cut = RenderDisk(2);

		Assert.Equal("moka-gauge moka-gauge--md moka-gauge--primary", cut.Find(".moka-gauge").ClassName);
		Assert.Empty(cut.FindAll(".moka-gauge-state"));
	}

	[Fact]
	public void LowIsBad_AtOrUnderWarnAt_SaysLow_WithTheWarningIcon()
	{
		IRenderedComponent<MokaGauge> cut = RenderBattery(20);

		IElement root = cut.Find(".moka-gauge");
		Assert.Equal("moka-gauge moka-gauge--md moka-gauge--primary moka-gauge--warn", root.ClassName);
		Assert.Equal("Low", cut.Find(".moka-gauge-state").TextContent.Trim());
		Assert.Single(cut.FindAll(".moka-gauge-state svg"));
		Assert.Equal("20, Low", root.GetAttribute("aria-valuetext"));
		Assert.Equal("Battery", cut.Find(".moka-gauge-label").TextContent);
	}

	[Fact]
	public void LowIsBad_AtOrUnderCriticalAt_SaysEmpty_InTheErrorColor()
	{
		IRenderedComponent<MokaGauge> cut = RenderBattery(3);

		IElement root = cut.Find(".moka-gauge");
		Assert.Contains("moka-gauge--critical", root.ClassList);
		Assert.DoesNotContain("moka-gauge--warn", root.ClassList);
		Assert.Equal("Empty", cut.Find(".moka-gauge-state").TextContent.Trim());
		Assert.Equal("3, Empty", root.GetAttribute("aria-valuetext"));
	}

	[Fact]
	public void LowIsBad_OverWarnAt_NothingChanges_EvenNearFull()
	{
		IRenderedComponent<MokaGauge> cut = RenderBattery(99);

		Assert.Equal("moka-gauge moka-gauge--md moka-gauge--primary", cut.Find(".moka-gauge").ClassName);
		Assert.Null(cut.Find(".moka-gauge").GetAttribute("aria-valuetext"));
		Assert.Empty(cut.FindAll(".moka-gauge-state"));
	}

	[Fact]
	public void LowIsBad_TheWordsCanStillBeReplaced()
	{
		IRenderedComponent<MokaGauge> cut = RenderBattery(10, "Charge soon");

		Assert.Equal("Charge soon", cut.Find(".moka-gauge-state").TextContent.Trim());
		Assert.Equal("10, Charge soon", cut.Find(".moka-gauge").GetAttribute("aria-valuetext"));
	}

	[Fact]
	public void TheLimitsAreSharesOfTheRange()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p
			.Add(x => x.Value, 1500d)
			.Add(x => x.Min, 1000d)
			.Add(x => x.Max, 3000d)
			.Add(x => x.WarnAt, 0.3)
			.Add(x => x.LowIsBad, true));

		Assert.Contains("moka-gauge--warn", cut.Find(".moka-gauge").ClassList);
	}

	// The scale grows with the value, but a limit set against Max stays where it was set.
	[Fact]
	public void WithAutoScale_TheLimitsAreSharesOfMax_NotOfTheRaisedScale()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p
			.Add(x => x.Value, 400d)
			.Add(x => x.AutoScale, true)
			.Add(x => x.WarnAt, 0.8));

		Assert.Equal("500", cut.Find(".moka-gauge").GetAttribute("aria-valuemax"));
		Assert.Contains("moka-gauge--warn", cut.Find(".moka-gauge").ClassList);
	}

	[Fact]
	public void TheStateWordFollowsTheWordAndTheDisplay_InTheValueText()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p
			.Add(x => x.Value, 15d)
			.Add(x => x.Display, "D")
			.Add(x => x.Word, "Charging")
			.Add(x => x.WarnAt, 0.2)
			.Add(x => x.LowIsBad, true));

		Assert.Equal("D, 15, Charging, Low", cut.Find(".moka-gauge").GetAttribute("aria-valuetext"));
		IElement center = cut.Find(".moka-gauge-center");
		Assert.Equal(["moka-gauge-value moka-gauge-display", "moka-gauge-figure", "moka-gauge-word moka-gauge-word--primary", "moka-gauge-state"],
			center.Children.Select(c => c.ClassName));
	}

	[Fact]
	public void TheStateWordShows_EvenWithNothingElseInTheCenter()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p
			.Add(x => x.Value, 4d)
			.Add(x => x.ShowValue, false)
			.Add(x => x.CriticalAt, 0.05)
			.Add(x => x.LowIsBad, true));

		Assert.Equal("Empty", cut.Find(".moka-gauge-center > .moka-gauge-state").TextContent.Trim());
		Assert.DoesNotContain("moka-gauge--ring", cut.Find(".moka-gauge").ClassList);
	}

	[Theory]
	[InlineData("warn", "warning")]
	[InlineData("critical", "error")]
	public void TheArcTheNumberAndTheWordTakeTheStateColor(string state, string token)
	{
		string color = $"var(--moka-color-{token})";

		Assert.Equal(color, ScopedCss.Declarations(Stylesheet, $".moka-gauge.moka-gauge--{state} > .moka-gauge-svg > .moka-gauge-fill")["stroke"]);
		Assert.Equal(color, ScopedCss.Declarations(Stylesheet, $".moka-gauge.moka-gauge--{state} > .moka-gauge-center > .moka-gauge-value")["color"]);
		Assert.Equal(color, ScopedCss.Declarations(Stylesheet, $".moka-gauge--{state} > .moka-gauge-center > .moka-gauge-state")["color"]);
	}

	// As specific as the ring's and the dial's number rules, so it has to come after them to win, and before the
	// display's colors, which the app picked.
	[Fact]
	public void TheStateColorRules_SitAfterTheRingAndDialNumbers_AndBeforeTheDisplayColors()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Stylesheet));

		int ring = css.IndexOf(".moka-gauge.moka-gauge--ring > .moka-gauge-center > .moka-gauge-value {", StringComparison.Ordinal);
		int dial = css.IndexOf(".moka-gauge.moka-gauge--dial > .moka-gauge-center > .moka-gauge-value {", StringComparison.Ordinal);
		int warn = css.IndexOf(".moka-gauge.moka-gauge--warn > .moka-gauge-center > .moka-gauge-value {", StringComparison.Ordinal);
		int display = css.IndexOf(".moka-gauge > .moka-gauge-center > .moka-gauge-display.moka-gauge-display--warning", StringComparison.Ordinal);
		int gate = css.IndexOf("/* Gate look.", StringComparison.Ordinal);

		Assert.True(ring > 0 && dial > 0);
		Assert.True(warn > ring && warn > dial);
		Assert.True(warn < display && display < gate);
	}

	[Fact]
	public void AColorStillColorsTheGauge_UntilALimitIsReached()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p
			.Add(x => x.Value, 50d)
			.Add(x => x.Color, MokaColor.Success)
			.Add(x => x.WarnAt, 0.2)
			.Add(x => x.LowIsBad, true));

		Assert.Equal("moka-gauge moka-gauge--md moka-gauge--success", cut.Find(".moka-gauge").ClassName);
	}
}
