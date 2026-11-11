using System.Globalization;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>A chart whose marks <see cref="MokaChartColumns" /> draws: columns or rows of bars, histogram bins or candles.</summary>
internal interface IChartColumns
{
	/// <summary>A panel's columns, or null when there is nothing to draw.</summary>
	/// <param name="panel">The panel.</param>
	ChartColumnsModel? Columns(int panel);
}

/// <summary>Which ends of a mark are rounded, and which keep a gap from the mark before it in a stack.</summary>
[Flags]
internal enum ChartBoxEnds
{
	None = 0,
	RoundLow = 1,
	RoundHigh = 2,
	GapLow = 4,
	GapHigh = 8
}

/// <summary>A box between two places along the value axis, as shares of it: a bar, a stacked segment, a wick or a body.</summary>
/// <param name="Class">Its classes: what it is, its color and its ends.</param>
/// <param name="Style">Where it starts and ends, and its own color when that isn't a class.</param>
/// <param name="Key">The series it belongs to, which the legend dims and hides by.</param>
/// <param name="Label">A value printed inside it, shown only where it fits.</param>
/// <param name="LabelClass">The label's classes, which hide it in a mark too small for it.</param>
internal sealed record ChartBox(string Class, string? Style, string? Key, string? Label = null, string? LabelClass = null);

/// <summary>A value printed past a mark's end or inside it, shown only where the plot has room for it.</summary>
/// <param name="Text">What it reads.</param>
/// <param name="Class">Its classes: where it sits and the widths it needs.</param>
/// <param name="Style">The place along the value axis it is printed from.</param>
internal sealed record ChartTip(string Text, string Class, string? Style);

/// <summary>A lane of a column: one series' bar, a stack, or a candle.</summary>
/// <param name="Marks">Its marks, drawn in order.</param>
/// <param name="Tips">The values printed beside them.</param>
internal sealed record ChartLane(IReadOnlyList<ChartBox> Marks, IReadOnlyList<ChartTip> Tips);

/// <summary>A drawn column, or a row when the chart runs across.</summary>
/// <param name="Index">Its drawn position, which the script and the tooltip know it by.</param>
/// <param name="Class">Its classes.</param>
/// <param name="Style">Its slot along the category axis and the share of it its marks take.</param>
/// <param name="Title">The native tooltip, when the chart draws no tooltip of its own.</param>
/// <param name="Lanes">Its lanes, side by side.</param>
internal sealed record ChartColumn(int Index, string Class, string? Style, string? Title, IReadOnlyList<ChartLane> Lanes);

/// <summary>
///     The columns of a panel. A column is built when it is drawn, so a long list of rows that scrolls builds only the
///     rows in view.
/// </summary>
internal sealed class ChartColumnsModel
{
	private readonly Func<int, ChartColumn> _column;

	/// <summary>Builds a model.</summary>
	/// <param name="class">The classes of the box the columns fill.</param>
	/// <param name="style">Its variables.</param>
	/// <param name="count">How many columns.</param>
	/// <param name="column">Builds a column.</param>
	public ChartColumnsModel(string @class, string? style, int count, Func<int, ChartColumn> column)
	{
		Class = @class;
		Style = style;
		Count = Math.Max(0, count);
		_column = column;
	}

	/// <summary>The classes of the box the columns fill.</summary>
	public string Class { get; }

	/// <summary>Its variables.</summary>
	public string? Style { get; }

	/// <summary>How many columns.</summary>
	public int Count { get; }

	/// <summary>A column, built now.</summary>
	/// <param name="index">Its drawn position.</param>
	public ChartColumn Column(int index) => _column(index);
}

/// <summary>
///     What the column charts share to build their marks: places as CSS shares, mark classes, and the classes that show
///     a printed value only where it fits. Fitting is decided by container queries in <see cref="MokaChartColumns" />'s
///     stylesheet, so nothing is measured by script: C# works out the narrowest column, lane or mark a value needs, in
///     the plot's em, and names it with a width from <see cref="Widths" />.
/// </summary>
internal static class ChartColumns
{
	/// <summary>The widths, in em, the stylesheet has a query for.</summary>
	internal static readonly double[] Widths = [1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5, 5, 6, 7, 8, 9, 10, 11, 12, 13.5, 15, 17, 19, 24, 30, 38, 48, 60, 75, 95, 120];

	/// <summary>The space kept between a value and its bar, and around a value inside one, in em.</summary>
	internal const double GapEm = 0.4;

	/// <summary>The height a printed value needs, in em.</summary>
	internal const double LineEm = 1.3;

	/// <summary>The class every mark carries.</summary>
	internal const string MarkClass = "moka-chart-mark";

	/// <summary>The smallest width the stylesheet knows that is at least this many em; null when none is.</summary>
	/// <param name="em">The width needed.</param>
	internal static double? Snap(double em)
	{
		if (!double.IsFinite(em))
		{
			return null;
		}

		foreach (double width in Widths)
		{
			if (width >= em)
			{
				return width;
			}
		}

		return null;
	}

	/// <summary>A share as a CSS percentage.</summary>
	/// <param name="share">The share, 0 to 1.</param>
	internal static string Css(double share) => ChartAxisLayout.Percent(double.IsFinite(share) ? share : 0);

	/// <summary>A vertical column's slot: where it starts along the plot's width and how wide it is.</summary>
	/// <param name="index">The column's drawn position.</param>
	/// <param name="count">How many columns share the plot.</param>
	internal static string? SlotStyle(int index, int count) => new StyleBuilder()
		.AddStyle("--_cs", Css(count > 0 ? (double)index / count : 0))
		.AddStyle("--_cw", Css(count > 0 ? 1d / count : 1))
		.Build();

	/// <summary>A mark from one place along the value axis to another.</summary>
	/// <param name="kind">What it is: <c>bar</c>, <c>wick</c>, <c>body</c> or <c>volume</c>.</param>
	/// <param name="low">Where it starts, as a share of the axis.</param>
	/// <param name="high">Where it ends.</param>
	/// <param name="colorClass">The class that colors it.</param>
	/// <param name="colorStyle">An inline color, when its color isn't a class.</param>
	/// <param name="key">The series it belongs to.</param>
	/// <param name="ends">Its rounded ends and stacking gaps.</param>
	/// <param name="extraClass">Anything else it is, such as hatched or hollow.</param>
	internal static ChartBox Mark(string kind, double low, double high, string? colorClass, string? colorStyle, string? key,
		ChartBoxEnds ends, string? extraClass = null) => new(
		MarkClasses(kind, colorClass, ends, extraClass, false),
		MarkStyle(low, high, colorStyle),
		key);

	/// <summary>A mark with a value printed inside it, which hides in a mark too small for it.</summary>
	/// <param name="mark">The mark.</param>
	/// <param name="label">The value.</param>
	internal static ChartBox WithLabel(ChartBox mark, string label)
	{
		ArgumentNullException.ThrowIfNull(mark);
		if (Snap(ChartAxisLayout.TextEm(label, null) + (2 * GapEm)) is not { } width)
		{
			return mark;
		}

		return mark with
		{
			Class = $"{mark.Class} moka-chart-mark--labelled",
			Label = label,
			LabelClass = $"moka-chart-mark__label moka-chart-mark__label--s{Number(width)}"
		};
	}

	/// <summary>
	///     A value past the end of a vertical bar, or under it for one that hangs below the baseline. It shows only when
	///     its column is wide enough for it, and the plot is tall enough to have a line's room past the bar.
	/// </summary>
	/// <param name="text">The value.</param>
	/// <param name="at">Where the bar ends, as a share of the axis.</param>
	/// <param name="below">Whether it goes under the bar.</param>
	/// <param name="share">
	///     How many values' widths the column has to hold: the number of lanes when every bar prints one, or less than one
	///     for a value that may reach over shorter neighbours that print nothing.
	/// </param>
	/// <param name="edge">
	///     For a value wider than its column, whether the column is the plot's first (-1) or last (1): the value then runs
	///     from its bar's outer edge inwards, so it doesn't spill past the plot.
	/// </param>
	internal static ChartTip? ColumnTip(string text, double at, bool below, double share, int edge = 0)
	{
		double room = below ? at : 1 - at;
		if (Snap((ChartAxisLayout.TextEm(text, null) + GapEm) * Math.Max(0.1, share)) is not { } width
		    || room <= 0.001 || Snap(LineEm / room) is not { } height)
		{
			return null;
		}

		string from = share >= 1 || edge == 0 ? string.Empty : edge < 0 ? " moka-chart-value--from-start" : " moka-chart-value--from-end";
		return new ChartTip(text,
			$"moka-chart-value moka-chart-value--{(below ? "lo" : "hi")} moka-chart-value--w{Number(width)} moka-chart-value--h{Number(height)}{from}",
			AtStyle(at));
	}

	/// <summary>
	///     A value for a bar in a row: past its end where the lane has room, else inside its end where the bar is long
	///     enough, else nowhere. Both are decided by the lane's width, so exactly one shows at any width that fits one.
	/// </summary>
	/// <param name="tips">Where the tips go.</param>
	/// <param name="text">The value.</param>
	/// <param name="low">Where the bar starts, as a share of the axis.</param>
	/// <param name="high">Where it ends.</param>
	/// <param name="before">Whether the value reads at the start, for a bar that runs left of zero.</param>
	internal static void RowTips(List<ChartTip> tips, string text, double low, double high, bool before)
	{
		ArgumentNullException.ThrowIfNull(tips);

		double textEm = ChartAxisLayout.TextEm(text, null);
		double room = before ? low : 1 - high;
		double length = high - low;
		double? outside = room > 0.001 ? Snap((textEm + GapEm) / room) : null;
		double? inside = length > 0.001 ? Snap((textEm + (2 * GapEm)) / length) : null;
		double at = before ? low : high;

		if (outside is { } past)
		{
			tips.Add(new ChartTip(text, $"moka-chart-value moka-chart-value--{(before ? "lo" : "hi")} moka-chart-value--lt{Number(past)}",
				AtStyle(at)));
		}

		if (inside is { } within && (outside is null || within < outside))
		{
			string from = outside is { } limit ? $" moka-chart-value--ge{Number(limit)}" : string.Empty;
			tips.Add(new ChartTip(text,
				$"moka-chart-value moka-chart-value--in-{(before ? "lo" : "hi")} moka-chart-value--lt{Number(within)}{from}", AtStyle(at)));
		}
	}

	/// <summary>The classes of a mark.</summary>
	/// <param name="kind">What it is.</param>
	/// <param name="colorClass">The class that colors it.</param>
	/// <param name="ends">Its rounded ends and stacking gaps.</param>
	/// <param name="extraClass">Anything else it is.</param>
	/// <param name="labelled">Whether a value is printed inside it.</param>
	internal static string MarkClasses(string kind, string? colorClass, ChartBoxEnds ends, string? extraClass, bool labelled) =>
		new CssBuilder(MarkClass)
			.AddClass($"moka-chart-mark--{kind}")
			.AddClass(colorClass)
			.AddClass("moka-chart-mark--round-lo", ends.HasFlag(ChartBoxEnds.RoundLow))
			.AddClass("moka-chart-mark--round-hi", ends.HasFlag(ChartBoxEnds.RoundHigh))
			.AddClass("moka-chart-mark--gap-lo", ends.HasFlag(ChartBoxEnds.GapLow))
			.AddClass("moka-chart-mark--gap-hi", ends.HasFlag(ChartBoxEnds.GapHigh))
			.AddClass("moka-chart-mark--labelled", labelled)
			.AddClass(extraClass)
			.Build();

	/// <summary>Where a mark starts and ends along the value axis, and its own color.</summary>
	/// <param name="low">Where it starts.</param>
	/// <param name="high">Where it ends.</param>
	/// <param name="colorStyle">An inline color, already checked.</param>
	internal static string? MarkStyle(double low, double high, string? colorStyle) => new StyleBuilder()
		.AddStyle("--_lo", Css(Math.Clamp(Math.Min(low, high), 0, 1)))
		.AddStyle("--_hi", Css(Math.Clamp(Math.Max(low, high), 0, 1)))
		.AddStyle(colorStyle)
		.Build();

	/// <summary>Where a value is printed from along the value axis.</summary>
	/// <param name="at">The place, as a share of the axis.</param>
	internal static string? AtStyle(double at) => new StyleBuilder()
		.AddStyle("--_at", Css(Math.Clamp(at, 0, 1)))
		.Build();

	/// <summary>A width as a class names it: 2.5 em is <c>2_5</c>.</summary>
	/// <param name="width">A width from <see cref="Widths" />.</param>
	internal static string Number(double width) =>
		width.ToString(CultureInfo.InvariantCulture).Replace(".", "_", StringComparison.Ordinal);
}
