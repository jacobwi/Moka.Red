using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Moka.Red.Data.Chart;
using Moka.Red.Icons;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Charts;

// Margin, Padding and Rounded on the state timeline land on its root, once, beside the axis's start and scale.
public sealed class MokaStateTimelineSpacingTests : BunitContext
{
	private static readonly DateTimeOffset Start = new(2026, 11, 11, 1, 0, 0, TimeSpan.Zero);

	private static readonly SpacingCase Case = new(typeof(MokaStateTimeline), "",
		("Rows", new[] { new MokaStateRow("a", [new MokaStateSpan(Start, MokaStateTimeline.UpKey)]) }),
		("Now", Start.AddHours(1)), ("TimeZone", TimeZoneInfo.Utc));

	public MokaStateTimelineSpacingTests() => this.Static();

	[Fact]
	public void SpacingParameters_LandOnTheComponent()
	{
		IRenderedComponent<ContainerFragment> cut = Render(Case.Render(withSpacing: true));

		Case.AssertSpacing(cut);
		Assert.Contains("--_st-k:", cut.Find(".moka-state-timeline").GetAttribute("style"), StringComparison.Ordinal);
	}
}

// A legend entry that names a state shows the state's icon in its color in place of the key.
public sealed class ChartLegendIconTests : BunitContext
{
	public ChartLegendIconTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void AnEntryWithAnIcon_DrawsItInPlaceOfTheKey()
	{
		IRenderedComponent<MokaChartLegend> cut = Render<MokaChartLegend>(p => p.Add(x => x.Key, MokaChartLegendKey.Mark).Add(x => x.Items,
		[
			new MokaChartLegendItem("down", "Down") { Icon = MokaIcons.Status.Error, Mark = MokaChartLegendMark.Box },
			new MokaChartLegendItem("lan", "LAN") { Mark = MokaChartLegendMark.Box }
		]));

		IReadOnlyList<IElement> keys = cut.FindAll(".moka-chart-legend__key");
		Assert.Contains("moka-chart-legend__key--icon", keys[0].ClassList);
		Assert.DoesNotContain("moka-chart-legend__key--box", keys[0].ClassList);
		Assert.NotNull(keys[0].QuerySelector("svg"));
		Assert.Contains("moka-chart-legend__key--box", keys[1].ClassList);
		Assert.Empty(keys[1].Children);
	}
}
