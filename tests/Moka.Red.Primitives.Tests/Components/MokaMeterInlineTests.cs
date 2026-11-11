using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Meter;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// MokaGate's address zones are buttons with a meter inside, and a div isn't allowed in a button. The meter is built of
// spans since 0.4.2; the dev app's meters were screenshot before and after, dark and light, wide and narrow, and every
// pixel matched.
public sealed class MokaMeterInlineTests : BunitContext
{
	private const string Css = "MokaMeter.razor.css";

	private static readonly MokaMeterSegment[] Zones = [new(0, 60, "var(--moka-color-success)"), new(60, 100, "#ffab40")];

	public static TheoryData<string> Variants => ["plain", "row", "bullet", "segments", "warn", "no header"];

	[Theory]
	[MemberData(nameof(Variants))]
	public void EveryElement_IsPhrasingContent_SoAMeterFitsInAButton(string variant)
	{
		IRenderedComponent<MokaMeter> cut = Render<MokaMeter>(p =>
		{
			p.Add(x => x.Value, 70d).Add(x => x.Label, variant == "no header" ? null : "Office");
			_ = variant switch
			{
				"row" => p.Add(x => x.Layout, MokaMeterLayout.Row),
				"bullet" => p.Add(x => x.Target, 80d).Add(x => x.Segments, Zones),
				"segments" => p.Add(x => x.Segments, Zones),
				"warn" => p.Add(x => x.WarnAt, 0.5),
				"no header" => p.Add(x => x.ShowValue, false),
				_ => p
			};
		});

		IElement root = cut.Find("[role=meter]");
		Assert.Equal("SPAN", root.TagName);
		Assert.All(root.QuerySelectorAll("*").Where(e => e.Closest("svg") is null), e => Assert.Equal("SPAN", e.TagName));
	}

	[Theory]
	[InlineData(".moka-meter__track")]
	[InlineData(".moka-meter__fill")]
	[InlineData(".moka-meter__segments")]
	[InlineData(".moka-meter__bands")]
	[InlineData(".moka-meter__band")]
	[InlineData(".moka-meter__target")]
	public void TheSpansThatHoldNoText_AreBlocks(string selector)
	{
		Assert.Equal("block", ScopedCss.Declarations(Css, selector)["display"]);
	}

	[Fact]
	public void ColorValue_FillsTheBar_InTheAppsColor()
	{
		IRenderedComponent<MokaMeter> cut = Render<MokaMeter>(p => p
			.Add(x => x.Value, 40d)
			.Add(x => x.Color, MokaColor.Info)
			.Add(x => x.ColorValue, " var(--zone) "));

		IElement root = cut.Find(".moka-meter");
		Assert.Equal("moka-meter moka-fill-width moka-meter--md moka-meter--own", root.ClassName);
		Assert.Equal("--_meter-color: var(--zone)", root.GetAttribute("style"));
		Assert.Equal("var(--_meter-color)", ScopedCss.Declarations(Css, ".moka-meter--own .moka-meter__fill")["background"]);
	}

	[Theory]
	[InlineData("red; background: url(x)")]
	[InlineData("not a color")]
	[InlineData("")]
	public void ColorValue_ThatIsNotAColor_IsIgnored(string value)
	{
		IRenderedComponent<MokaMeter> cut = Render<MokaMeter>(p => p
			.Add(x => x.Value, 40d)
			.Add(x => x.ColorValue, value));

		IElement root = cut.Find(".moka-meter");
		Assert.Contains("moka-meter--primary", root.ClassList);
		Assert.Null(root.GetAttribute("style"));
	}

	// The zone's color until it's getting full; past that the amber and red say how full, as MokaGate's override did.
	[Fact]
	public void ColorValue_GivesWayToWarnAndCritical()
	{
		IRenderedComponent<MokaMeter> cut = Render<MokaMeter>(p => p
			.Add(x => x.Value, 0.9)
			.Add(x => x.Max, 1d)
			.Add(x => x.ColorValue, "#00e676")
			.Add(x => x.WarnAt, 0.8));

		Assert.Contains("moka-meter--warn", cut.Find(".moka-meter").ClassList);
		Assert.Equal("var(--moka-color-warning)", ScopedCss.Declarations(Css, ".moka-meter.moka-meter--warn .moka-meter__fill")["background"]);
		Assert.True(Specificity(".moka-meter.moka-meter--warn .moka-meter__fill") > Specificity(".moka-meter--own .moka-meter__fill"));
	}

	[Fact]
	public void ColorValue_ReachesTheBulletsBar()
	{
		IRenderedComponent<MokaMeter> cut = Render<MokaMeter>(p => p
			.Add(x => x.Value, 40d)
			.Add(x => x.Target, 60d)
			.Add(x => x.ColorValue, "oklch(70% 0.1 200)"));

		Assert.Contains("moka-meter--own", cut.Find(".moka-meter").ClassList);
		Assert.Contains("moka-meter__fill", cut.Find(".moka-meter__fill--bullet").ClassList);
	}

	[Fact]
	public void AMeterInAButton_KeepsItsAttributes()
	{
		IRenderedComponent<ContainerFragment> cut = Render(builder =>
		{
			builder.OpenElement(0, "button");
			builder.OpenComponent<MokaMeter>(1);
			builder.AddComponentParameter(2, nameof(MokaMeter.Value), 30d);
			builder.AddComponentParameter(3, nameof(MokaMeter.Figure), "30%");
			builder.CloseComponent();
			builder.CloseElement();
		});

		IElement meter = cut.Find("button > [role=meter]");
		Assert.Equal(("SPAN", "30", "30%"), (meter.TagName, meter.GetAttribute("aria-valuenow"), meter.GetAttribute("aria-valuetext")));
	}

	private static int Specificity(string selector) => selector.Count(c => c == '.');
}
