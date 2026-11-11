using Bunit;
using Bunit.Rendering;
using Moka.Red.Data.Chart;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Charts;

// Margin, Padding and Rounded on the components the frame package adds. The chart parts take none: they are chrome
// inside a chart, which takes them on its own root.
public sealed class ChartFrameSpacingTests : BunitContext
{
	private static readonly Dictionary<string, SpacingCase> Cases = new()
	{
		["ChartLegend"] = new(typeof(MokaChartLegend), "",
			("Items", new[] { new MokaChartLegendItem("lan", "LAN"), new MokaChartLegendItem("wifi", "Wi-Fi") }))
	};

	public ChartFrameSpacingTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<string> Names => [.. Cases.Keys];

	[Theory]
	[MemberData(nameof(Names))]
	public void SpacingParameters_LandOnTheComponent(string name)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: true));
		spacing.OpenIfNeeded(cut);

		spacing.AssertSpacing(cut);
	}

	[Theory]
	[MemberData(nameof(Names))]
	public void Styles_AreUnchanged_WithoutSpacingParameters(string name)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: false));
		spacing.OpenIfNeeded(cut);

		Assert.Equal(spacing.DefaultStyles, SpacingCase.Styles(cut));
	}
}
