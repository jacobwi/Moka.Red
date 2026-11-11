using Moka.Red.Core.Theming;

namespace Moka.Red.Data.Chart;

/// <summary>How a chart's title is written.</summary>
internal enum ChartTitleStyle
{
	Sentence,
	Micro,
	Large
}

/// <summary>
///     The defaults a chart look sets. A chart parameter that is set always wins over these. <see cref="Area" /> is
///     the fill an area takes, drawn only when the chart asks for one unless <see cref="AreaAlways" />;
///     <see cref="LegendValues" /> null leaves it to the chart's <c>ShowCurrent</c>; <see cref="BarEnds" /> null is the
///     classic svg bars; <see cref="RingThickness" /> null is the donut's own <c>Thickness</c>; <see cref="RowHeight" />
///     is exact pixels, which <c>Virtualize</c> needs.
/// </summary>
internal sealed record ChartLookSpec(
	MokaChartLook Look,
	MokaChartGrid Grid,
	bool NiceTicks,
	double? CompactAbove,
	MokaAxisSide ValueSide,
	MokaTickPlacement TickLabels,
	MokaChartLegendPosition Legend,
	MokaChartLegendKey LegendKey,
	MokaChartLegendActions LegendActions,
	bool? LegendValues,
	MokaDirectLabels DirectLabels,
	MokaChartPoints Points,
	MokaChartArea Area,
	bool AreaAlways,
	MokaLineCurve Curve,
	MokaBarEnds? BarEnds,
	MokaChartTooltip Tooltip,
	bool Pinnable,
	bool Keyboard,
	MokaChartControls Controls,
	double RowHeight,
	double? RingThickness,
	double RingGap,
	bool RoundRingEnds,
	ChartTitleStyle TitleStyle)
{
	/// <summary>Today's charts.</summary>
	public static ChartLookSpec Standard { get; } = new(MokaChartLook.Standard, MokaChartGrid.Lines, false, null,
		MokaAxisSide.Start, MokaTickPlacement.Outside, MokaChartLegendPosition.Bottom, MokaChartLegendKey.Dot,
		MokaChartLegendActions.None, null, MokaDirectLabels.None, MokaChartPoints.Always, MokaChartArea.Flat, false,
		MokaLineCurve.Straight, null, MokaChartTooltip.Native, false, false, MokaChartControls.None, 28, null, 0, false,
		ChartTitleStyle.Sentence);

	/// <summary>S1A.</summary>
	public static ChartLookSpec Quiet { get; } = new(MokaChartLook.Quiet, MokaChartGrid.Hairlines, true, 10_000,
		MokaAxisSide.Start, MokaTickPlacement.Outside, MokaChartLegendPosition.Top, MokaChartLegendKey.Mark,
		MokaChartLegendActions.All, false, MokaDirectLabels.Names, MokaChartPoints.Hover, MokaChartArea.Flat, false,
		MokaLineCurve.Straight, MokaBarEnds.Rounded, MokaChartTooltip.Auto, true, true, MokaChartControls.None, 28, 14, 2,
		false, ChartTitleStyle.Sentence);

	/// <summary>S1B.</summary>
	public static ChartLookSpec Dense { get; } = new(MokaChartLook.Dense, MokaChartGrid.Hairlines, true, 10_000,
		MokaAxisSide.End, MokaTickPlacement.Outside, MokaChartLegendPosition.TitleRow, MokaChartLegendKey.Mark,
		MokaChartLegendActions.All, true, MokaDirectLabels.None, MokaChartPoints.Hover, MokaChartArea.Flat, false,
		MokaLineCurve.Straight, MokaBarEnds.Rounded, MokaChartTooltip.Readout, true, true, MokaChartControls.None, 22, 10,
		1, false, ChartTitleStyle.Micro);

	/// <summary>S1C.</summary>
	public static ChartLookSpec Soft { get; } = new(MokaChartLook.Soft, MokaChartGrid.Hairlines, true, 10_000,
		MokaAxisSide.Start, MokaTickPlacement.Outside, MokaChartLegendPosition.Top, MokaChartLegendKey.Pill,
		MokaChartLegendActions.All, false, MokaDirectLabels.None, MokaChartPoints.Hover, MokaChartArea.Fade, true,
		MokaLineCurve.Smooth, MokaBarEnds.Rounded, MokaChartTooltip.Auto, false, true, MokaChartControls.None, 32, 22, 4,
		true, ChartTitleStyle.Sentence);

	/// <summary>S1D.</summary>
	public static ChartLookSpec Bold { get; } = new(MokaChartLook.Bold, MokaChartGrid.Hairlines, true, 10_000,
		MokaAxisSide.Start, MokaTickPlacement.Outside, MokaChartLegendPosition.Top, MokaChartLegendKey.Mark,
		MokaChartLegendActions.All, false, MokaDirectLabels.Values, MokaChartPoints.Hover, MokaChartArea.Flat, false,
		MokaLineCurve.Straight, MokaBarEnds.Rounded, MokaChartTooltip.Auto, false, true, MokaChartControls.None, 36, 30, 2,
		false, ChartTitleStyle.Large);

	/// <summary>The spec of a look.</summary>
	public static ChartLookSpec For(MokaChartLook look) => look switch
	{
		MokaChartLook.Quiet => Quiet,
		MokaChartLook.Dense => Dense,
		MokaChartLook.Soft => Soft,
		MokaChartLook.Bold => Bold,
		_ => Standard
	};
}
