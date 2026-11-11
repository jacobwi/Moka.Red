using Bunit;
using Bunit.Rendering;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Charts;

// Margin, Padding and Rounded on the modern line chart, which takes them on its own root as the classic one does.
public sealed class MokaLineChartSpacingTests : BunitContext
{
	private static readonly Dictionary<string, SpacingCase> Cases = new()
	{
		["ModernLineChart"] = new(typeof(MokaLineChart),
			"--_plot-max: 14rem | --_s: 0% | --_s: 25% | --_s: 50% | --_s: 75% | --_s: 100% | --_s: 25% | --_s: 50% | --_s: 75% | --_s: 100% | --_s: 0% | --_s: 0%; --_hw: 0.33em | --_s: 100%; --_hw: 0.33em",
			("Look", (MokaChartLook?)MokaChartLook.Quiet),
			("ShowLegend", false),
			("DirectLabels", (MokaDirectLabels?)MokaDirectLabels.None),
			("Series", new[] { new MokaLineSeries { Name = "Revenue", Values = new[] { 10d, 20 } } }))
	};

	public MokaLineChartSpacingTests() => this.Static();

	public static TheoryData<string> Names => [.. Cases.Keys];

	[Theory]
	[MemberData(nameof(Names))]
	public void SpacingParameters_LandOnTheComponent(string name)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: true));

		spacing.AssertSpacing(cut);
	}

	[Theory]
	[MemberData(nameof(Names))]
	public void Styles_AreUnchanged_WithoutSpacingParameters(string name)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: false));

		Assert.Equal(spacing.DefaultStyles, SpacingCase.Styles(cut));
	}
}
