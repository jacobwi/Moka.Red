namespace Moka.Red.Data.Chart;

/// <summary>How a chart says what is under the pointer or the keyboard focus.</summary>
public enum MokaChartTooltip
{
	/// <summary>What the look picks: a crosshair on lines, the column on bars and rows, the nearest point on scatter.</summary>
	Auto,

	/// <summary>The browser's own tooltip on each mark, as charts have always had.</summary>
	Native,

	/// <summary>No tooltip.</summary>
	None,

	/// <summary>A line down the plot at the pointer, listing every series there.</summary>
	Crosshair,

	/// <summary>The single point nearest the pointer.</summary>
	Nearest,

	/// <summary>The values in a strip above the plot, rather than a box that follows the pointer.</summary>
	Readout,

	/// <summary>The whole column under the pointer, each bar with its share of the column.</summary>
	Column
}

/// <summary>What clicking a legend entry does. Combine them.</summary>
[Flags]
public enum MokaChartLegendActions
{
	/// <summary>Entries are names, not controls.</summary>
	None = 0,

	/// <summary>Pointing at an entry dims the other series.</summary>
	Highlight = 1,

	/// <summary>Clicking an entry hides or shows its series.</summary>
	Hide = 2,

	/// <summary>Double clicking an entry shows its series alone. Turns <see cref="Hide" /> on too.</summary>
	Isolate = 4,

	/// <summary>Every action.</summary>
	All = Highlight | Hide | Isolate
}

/// <summary>Where a chart's legend sits.</summary>
public enum MokaChartLegendPosition
{
	/// <summary>Under the chart.</summary>
	Bottom,

	/// <summary>Over the plot.</summary>
	Top,

	/// <summary>Beside the plot, moving under it when the chart is narrow.</summary>
	Right,

	/// <summary>In the title row.</summary>
	TitleRow,

	/// <summary>No legend.</summary>
	None
}

/// <summary>What marks each legend entry.</summary>
public enum MokaChartLegendKey
{
	/// <summary>A dot in the series' color.</summary>
	Dot,

	/// <summary>The series' own mark: a short line, a box or a dot.</summary>
	Mark,

	/// <summary>The whole entry in a tinted pill.</summary>
	Pill
}

/// <summary>The controls a chart's toolbar offers. Combine them.</summary>
[Flags]
public enum MokaChartControls
{
	/// <summary>No toolbar.</summary>
	None = 0,

	/// <summary>A switch between the chart and a table of its values.</summary>
	View = 1,

	/// <summary>Save the chart as a PNG picture.</summary>
	ExportPng = 2,

	/// <summary>Save the chart as an SVG drawing.</summary>
	ExportSvg = 4,

	/// <summary>Save the values as CSV.</summary>
	ExportCsv = 8,

	/// <summary>Every export.</summary>
	Export = ExportPng | ExportSvg | ExportCsv,

	/// <summary>The time range presets.</summary>
	Ranges = 16,

	/// <summary>A button that undoes a zoom, shown while zoomed.</summary>
	ResetZoom = 32,

	/// <summary>The legend's toggles in the toolbar row.</summary>
	Legend = 64,

	/// <summary>Every control.</summary>
	All = View | Export | Ranges | ResetZoom | Legend
}

/// <summary>How a chart can be zoomed. Combine them.</summary>
[Flags]
public enum MokaChartZoom
{
	/// <summary>No zoom.</summary>
	None = 0,

	/// <summary>Drag across the plot to zoom into that span.</summary>
	Brush = 1,

	/// <summary>Ctrl and the wheel, a pinch, or plus and minus while focused.</summary>
	Wheel = 2,

	/// <summary>A range strip under the plot.</summary>
	Strip = 4,

	/// <summary>Every way.</summary>
	All = Brush | Wheel | Strip
}

/// <summary>What a chart shows.</summary>
public enum MokaChartView
{
	/// <summary>The chart.</summary>
	Chart,

	/// <summary>A table of its values.</summary>
	Table
}

/// <summary>What state a chart's data is in.</summary>
public enum MokaChartState
{
	/// <summary>Empty without data, else ready.</summary>
	Auto,

	/// <summary>Drawn.</summary>
	Ready,

	/// <summary>New data is on its way: the last drawing stays, dimmed, with an Updating chip.</summary>
	Loading,

	/// <summary>Nothing to show.</summary>
	Empty,

	/// <summary>The data couldn't be loaded.</summary>
	Error
}

/// <summary>The lines behind a chart's data.</summary>
public enum MokaChartGrid
{
	/// <summary>A line at every tick, the baseline as faint as the rest.</summary>
	Lines,

	/// <summary>Hairlines one step off the surface and a slightly stronger baseline.</summary>
	Hairlines,

	/// <summary>The baseline alone.</summary>
	Baseline,

	/// <summary>Dotted lines at every tick.</summary>
	Dotted,

	/// <summary>No lines.</summary>
	None
}

/// <summary>Where the value axis's numbers sit.</summary>
public enum MokaTickPlacement
{
	/// <summary>Beside the plot.</summary>
	Outside,

	/// <summary>Inside the plot, just above their lines.</summary>
	Inside,

	/// <summary>Inside on a plot under 20em wide, outside otherwise.</summary>
	Auto
}

/// <summary>Which side of the plot the value axis is on.</summary>
public enum MokaAxisSide
{
	/// <summary>The left, before the plot.</summary>
	Start,

	/// <summary>The right, beside the newest values.</summary>
	End
}

/// <summary>How values are spread along an axis.</summary>
public enum MokaAxisScale
{
	/// <summary>What the data suggests: time when there are times, categories when there are labels, else linear.</summary>
	Auto,

	/// <summary>Equal steps for equal differences.</summary>
	Linear,

	/// <summary>Equal steps for equal ratios.</summary>
	Log,

	/// <summary>Points in time.</summary>
	Time,

	/// <summary>Named categories, evenly spaced.</summary>
	Category
}

/// <summary>What a category axis does with labels that don't fit side by side.</summary>
public enum MokaAxisLabelFit
{
	/// <summary>Flat, then staggered, then rotated, then thinned, as the axis narrows.</summary>
	Auto,

	/// <summary>Hide labels, keeping the first, the last and evenly spread ones.</summary>
	Thin,

	/// <summary>Turn them 45 degrees.</summary>
	Rotate,

	/// <summary>Put every other one on a second row.</summary>
	Stagger,

	/// <summary>Break them onto more lines.</summary>
	Wrap,

	/// <summary>Cut them short with an ellipsis, the whole text in a tooltip.</summary>
	Truncate,

	/// <summary>Keep every label whole and let the plot scroll sideways.</summary>
	Scroll
}

/// <summary>What is written beside each line's end.</summary>
public enum MokaDirectLabels
{
	/// <summary>Nothing.</summary>
	None,

	/// <summary>The series' names.</summary>
	Names,

	/// <summary>The latest values.</summary>
	Values,

	/// <summary>The names and the latest values.</summary>
	Both
}

/// <summary>Where a line chart draws a dot on its points.</summary>
public enum MokaChartPoints
{
	/// <summary>Nowhere.</summary>
	None,

	/// <summary>Where you point, and at each line's end.</summary>
	Hover,

	/// <summary>On every point.</summary>
	Always
}

/// <summary>How the area under a line is filled.</summary>
public enum MokaChartArea
{
	/// <summary>Not at all.</summary>
	None,

	/// <summary>A flat tint.</summary>
	Flat,

	/// <summary>A tint that fades to nothing at the bottom.</summary>
	Fade
}

/// <summary>How a line runs between its points.</summary>
public enum MokaLineCurve
{
	/// <summary>Straight segments.</summary>
	Straight,

	/// <summary>A smooth curve that never overshoots a peak.</summary>
	Smooth,

	/// <summary>Each value held until the next.</summary>
	Step
}

/// <summary>How the ends of bars are drawn.</summary>
public enum MokaBarEnds
{
	/// <summary>Rounded at the data end, square on the baseline.</summary>
	Rounded,

	/// <summary>Square at both ends.</summary>
	Square
}

/// <summary>How a long series is reduced before it is drawn.</summary>
public enum MokaDownsample
{
	/// <summary>The lowest and highest value per pixel, which keeps every peak, once a series is twice as long as the plot is wide.</summary>
	Auto,

	/// <summary>The lowest and highest value per pixel.</summary>
	MinMax,

	/// <summary>Largest triangle three buckets: the points that keep the shape.</summary>
	Lttb,

	/// <summary>Every point, however many.</summary>
	None
}

/// <summary>The shape of an event marker.</summary>
public enum MokaChartMarker
{
	/// <summary>A diamond.</summary>
	Diamond,

	/// <summary>A triangle.</summary>
	Triangle,

	/// <summary>A circle.</summary>
	Circle,

	/// <summary>A square.</summary>
	Square
}

/// <summary>The order categories or rows are drawn in.</summary>
public enum MokaChartSort
{
	/// <summary>As given.</summary>
	None,

	/// <summary>Largest first.</summary>
	Descending,

	/// <summary>Smallest first.</summary>
	Ascending
}

/// <summary>Where bars print their values.</summary>
public enum MokaValueLabels
{
	/// <summary>Nowhere.</summary>
	None,

	/// <summary>Just past each bar's end.</summary>
	Tip,

	/// <summary>Inside each bar, where it fits.</summary>
	Inside,

	/// <summary>Only the busiest group's total.</summary>
	Largest,

	/// <summary>What the look picks.</summary>
	Auto
}
