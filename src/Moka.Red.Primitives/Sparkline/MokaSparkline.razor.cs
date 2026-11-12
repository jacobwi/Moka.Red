using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     A small inline line chart, the kind that sits beside a number in a dashboard card. Draws a
///     polyline over its values with an optional area fill and a dot on the last point.
/// </summary>
/// <remarks>
///     Lives in Moka.Red.Primitives under the namespace it always had, so <c>MokaStat</c> can draw one; Moka.Red.Data
///     forwards the type. A series longer than 480 values is drawn from the lowest and the highest value of each of 240
///     equal runs, which keeps every peak and trough at a fraction of the markup.
/// </remarks>
public partial class MokaSparkline : MokaVisualComponentBase
{
	// The viewBox the line is drawn in; the root element is sized by the size classes and the svg
	// fills it, so a sparkline scales with the text around it.
	private const double ViewWidth = 120;
	private const double ViewHeight = 36;

	// Past this many values the line is drawn from MinMax buckets, two points each at most.
	private const int MaxDrawn = 480;

	private int[]? _drawn;
	private double[]? _drawnValues;

	/// <summary>
	///     The values to draw. Null or fewer than two values renders an empty box. A <c>double.NaN</c> is a
	///     gap: the line and the area break there.
	/// </summary>
	[Parameter]
	public IReadOnlyList<double>? Values { get; set; }

	/// <summary>Whether the area under the line is filled. Defaults to true.</summary>
	[Parameter]
	public bool ShowArea { get; set; } = true;

	/// <summary>Whether a dot marks the last value that isn't a gap. Defaults to true.</summary>
	[Parameter]
	public bool ShowLastPoint { get; set; } = true;

	/// <summary>
	///     Stroke width of the line, in pixels whatever the box's size, since the line doesn't scale with it. The dot is
	///     twice this wide, and at least 6. Defaults to 2.
	/// </summary>
	[Parameter]
	public double StrokeWidth { get; set; } = 2;

	/// <summary>What the chart shows, for assistive technology. Defaults to "Sparkline".</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>
	///     The height of the box, as a CSS length such as <c>24px</c>, or a <c>calc()</c>, <c>var()</c>, <c>min()</c>,
	///     <c>max()</c> or <c>clamp()</c> of lengths. Null or anything else leaves it to <c>Size</c>.
	/// </summary>
	[Parameter]
	public string? Height { get; set; }

	/// <summary>
	///     What the line, area and dot are drawn in: a CSS color, or a token such as
	///     <c>var(--moka-color-success)</c>. Wins over <c>Color</c>. Anything that isn't a color is ignored.
	/// </summary>
	[Parameter]
	public string? ColorValue { get; set; }

	/// <summary>The value at the bottom of the box. Lower values sit on the bottom edge. Null = the lowest value.</summary>
	[Parameter]
	public double? Min { get; set; }

	/// <summary>The value at the top of the box. Higher values sit on the top edge. Null = the highest value.</summary>
	[Parameter]
	public double? Max { get; set; }

	/// <summary>
	///     Draws the line and the area in the chart's muted gray, and only the last stretch and the last point in the
	///     color, so a tile full of them reads as background with the latest value standing out.
	/// </summary>
	[Parameter]
	public bool Muted { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-sparkline";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-sparkline--{SizeToKebab(Size)}")
		.AddClass(Color.HasValue ? $"moka-chart-color-{ColorToKebab(Color.Value)}" : null)
		.AddClass("moka-sparkline--muted", Muted)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("height", CssValues.IsLengthExpression(Height) ? CssValues.WithPixels(Height.Trim()) : null)
		.AddStyle("--moka-chart-color", CssValues.IsColor(ColorValue) ? ColorValue.Trim() : null)
		.AddStyle(Style)
		.Build();

	private bool HasLine => Values is not null && Values.Count(double.IsFinite) >= 2;

	private IReadOnlyList<(int First, int Last)> Runs =>
		_drawnValues is null ? MokaSeriesRuns.Of(Values!) : MokaSeriesRuns.Of(_drawnValues);

	private string LinePath
	{
		get
		{
			(double floor, double span) = Scale;
			return string.Join(' ', Runs.Select(run => RunPath(run, floor, span)));
		}
	}

	// Each stretch of the line closed along the bottom: the area fill.
	private string AreaPath
	{
		get
		{
			(double floor, double span) = Scale;
			return string.Join(' ', Runs.Select(run => string.Create(CultureInfo.InvariantCulture,
				$"M {X(run.First):0.##} {ViewHeight:0.##} {RunPath(run, floor, span)[2..]} L {X(run.Last):F2} {ViewHeight:0.##} Z")));
		}
	}

	// The last stretch of the line, from the point before the last, drawn in the color over a muted line.
	private string? TailPath
	{
		get
		{
			(int first, int last) = Runs[^1];
			if (first == last)
			{
				return null;
			}

			(double floor, double span) = Scale;
			StringBuilder sb = new("M ");
			AppendPoint(sb, last - 1, floor, span);
			sb.Append(" L ");
			AppendPoint(sb, last, floor, span);
			return sb.ToString();
		}
	}

	private int LastIndex => Runs[^1].Last;

	// The dot is a zero-length line with round caps, as MokaLineChart draws its points: the box stretches the viewBox,
	// and a stroke that doesn't scale stays round where a circle would turn into an ellipse.
	private (string X, string Y) LastPointText
	{
		get
		{
			(double floor, double span) = Scale;
			return (Num(X(LastIndex)), Num(Y(ValueAt(LastIndex), floor, span)));
		}
	}

	private double DotSize => 2 * Math.Max(StrokeWidth, 3);

	private double RingSize => DotSize + 4;

	private (double Floor, double Span) Scale
	{
		get
		{
			double lowest = double.MaxValue;
			double highest = double.MinValue;
			foreach (double value in Values!)
			{
				if (double.IsFinite(value))
				{
					lowest = Math.Min(lowest, value);
					highest = Math.Max(highest, value);
				}
			}

			double floor = Min ?? lowest;
			return (floor, (Max ?? highest) - floor);
		}
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		if (Values is { Count: > MaxDrawn } values)
		{
			double[] all = values as double[] ?? [.. values];
			_drawn = MokaDownsampler.MinMax(all, MaxDrawn / 2);
			_drawnValues = [.. _drawn.Select(index => all[index])];
		}
		else
		{
			_drawn = null;
			_drawnValues = null;
		}
	}

	private double ValueAt(int drawn) => _drawnValues?[drawn] ?? Values![drawn];

	private string RunPath((int First, int Last) run, double floor, double span)
	{
		StringBuilder sb = new();
		for (int i = run.First; i <= run.Last; i++)
		{
			sb.Append(i == run.First ? "M " : " L ");
			AppendPoint(sb, i, floor, span);
		}

		// A lone value between two gaps needs a segment for the round cap to draw.
		if (run.First == run.Last)
		{
			sb.Append(" L ");
			AppendPoint(sb, run.First, floor, span);
		}

		return sb.ToString();
	}

	private void AppendPoint(StringBuilder sb, int drawn, double floor, double span)
	{
		sb.Append(X(drawn).ToString("F2", CultureInfo.InvariantCulture));
		sb.Append(' ');
		sb.Append(Y(ValueAt(drawn), floor, span).ToString("F2", CultureInfo.InvariantCulture));
	}

	// A drawn point sits where its value sits in the whole series, so a reduced line keeps its shape along x.
	private double X(int drawn) => (_drawn?[drawn] ?? drawn) * ViewWidth / (Values!.Count - 1);

	private static string Num(double value) => value.ToString("F2", CultureInfo.InvariantCulture);

	// A flat series has no span to share out. A bound the app set says where it sits: on the floor at or below Min, at the
	// top at or above Max alone. With neither it sits in the middle.
	private double FlatShare(double value) => Min is { } min
		? value <= min ? 0 : 1
		: Max is not null ? 1 : 0.5;

	private double Y(double value, double floor, double span)
	{
		double norm = span > 0 ? Math.Clamp((value - floor) / span, 0, 1) : FlatShare(value);
		return StrokeWidth / 2 + (1 - norm) * (ViewHeight - StrokeWidth);
	}
}
