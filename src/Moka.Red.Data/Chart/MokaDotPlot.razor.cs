using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     A dot per row on a hairline, for values that don't start at zero, such as uptime between 99% and 100% where bars
///     would all look full. <see cref="Stems" /> turns it into a lollipop chart, and a <see cref="MokaDotPlotRow.Before" />
///     value draws a dumbbell: a pale dot, a dark dot and the line between, for before and after.
/// </summary>
/// <remarks>
///     Rows are HTML, positioned by percent on each row's track, and past 200 rows they are drawn only as they scroll
///     into view. Names stay whole on one line, with the whole name in a tooltip, and move above their rows when the
///     chart is narrow.
/// </remarks>
public partial class MokaDotPlot : MokaCartesianChartBase
{
	/// <summary>The key of a dumbbell's "before" dots, for <see cref="MokaChartBase.HiddenSeries" />.</summary>
	public const string BeforeKey = "before";

	/// <summary>The key of the dots that show each row's value, for <see cref="MokaChartBase.HiddenSeries" />.</summary>
	public const string ValueKey = "value";

	private MokaDotPlotRow[]? _snapshot;
	private Dot[] _dots = [];
	private List<ChartSeriesInfo> _series = [];
	private bool _dumbbell;

	/// <summary>The rows, in the order they are drawn unless <see cref="Sort" /> says otherwise.</summary>
	[Parameter]
	public IReadOnlyList<MokaDotPlotRow>? Rows { get; set; }

	/// <summary>Draws a stem from <see cref="Baseline" /> to each dot, which makes a lollipop chart.</summary>
	[Parameter]
	public bool Stems { get; set; }

	/// <summary>Where the stems start. Defaults to 0; the axis reaches it while <see cref="Stems" /> are drawn.</summary>
	[Parameter]
	public double Baseline { get; set; }

	/// <summary>
	///     Whether the axis starts at zero. Defaults to false: the axis spans the data with a little room on round
	///     numbers, so 99% to 100% uptime fills the row.
	/// </summary>
	[Parameter]
	public bool ZeroBased { get; set; }

	/// <summary>How the rows are ordered by value. Null keeps them as given.</summary>
	[Parameter]
	public MokaChartSort? Sort { get; set; }

	/// <summary>What a dumbbell's pale dots are called, in the legend, the tooltip and the table. Defaults to "Before".</summary>
	[Parameter]
	public string BeforeLabel { get; set; } = "Before";

	/// <summary>What a dumbbell's dark dots are called. Defaults to "After".</summary>
	[Parameter]
	public string AfterLabel { get; set; } = "After";

	/// <summary>The table's heading for the change between before and after. Defaults to "Change".</summary>
	[Parameter]
	public string ChangeLabel { get; set; } = "Change";

	/// <summary>Whether a higher value is better, which decides which rows got worse. Defaults to true.</summary>
	[Parameter]
	public bool UpIsGood { get; set; } = true;

	/// <summary>Draws the rows that got worse in the accent and the rest muted, so they stand out without a second color.</summary>
	[Parameter]
	public bool MarkWorse { get; set; }

	/// <summary>The tooltip's word for a row that got better. Defaults to "better".</summary>
	[Parameter]
	public string BetterText { get; set; } = "better";

	/// <summary>The tooltip's word for a row that got worse. Defaults to "worse".</summary>
	[Parameter]
	public string WorseText { get; set; } = "worse";

	/// <summary>Draws a row's name in place of its text, which still names it in the tooltip and the table.</summary>
	[Parameter]
	public RenderFragment<MokaDotPlotRow>? NameTemplate { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-dotplot";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(ModernRootClasses)
		.AddClass("moka-dotplot--dumbbell", _dumbbell)
		.AddClass("moka-dotplot--stems", Stems)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	internal override string DefaultLabel => _dots.Length == 0
		? "Dot plot"
		: $"Dot plot: {string.Join(", ", _dots.Take(6).Select(dot => $"{dot.Row.Name} {dot.Figure}"))}{(_dots.Length > 6 ? ", ..." : "")}";

	/// <inheritdoc />
	internal override bool HasData => _dots.Length > 0;

	/// <inheritdoc />
	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => _series;

	/// <inheritdoc />
	internal override bool DrawsRows => true;

	/// <inheritdoc />
	internal override bool SurfaceRows => true;

	/// <inheritdoc />
	internal override MokaChartTooltip AutoTooltip => MokaChartTooltip.Column;

	/// <inheritdoc />
	internal override int RowCount => _dots.Length;

	/// <inheritdoc />
	internal override int CategoryCount => _dots.Length;

	/// <inheritdoc />
	internal override int DrawnCount(int panel) => _dots.Length;

	/// <inheritdoc />
	internal override int LatestDrawn(int panel) => _dots.Length > 0 ? 0 : -1;

	/// <inheritdoc />
	internal override string RowLabel(int index) => index >= 0 && index < _dots.Length ? _dots[index].Row.Name : string.Empty;

	/// <inheritdoc />
	internal override string? RowFigure(int index) => index >= 0 && index < _dots.Length ? _dots[index].Figure : null;

	/// <inheritdoc />
	internal override ChartTableModel? TableModel => _dumbbell
		? new ChartTableModel([Words.Name, BeforeLabel, AfterLabel, ChangeLabel], _dots.Length,
			(row, column) => column switch
			{
				0 => _dots[row].Row.Name,
				1 => Write(_dots[row].Row.Before),
				2 => Write(_dots[row].Row.Value),
				_ => ChangeText(_dots[row].Row)
			},
			(row, column) => column switch
			{
				0 => _dots[row].Row.Name,
				1 => Raw(_dots[row].Row.Before),
				2 => Raw(_dots[row].Row.Value),
				_ => Raw(_dots[row].Row.Value - (_dots[row].Row.Before ?? double.NaN))
			})
		: new ChartTableModel([Words.Name, ValueName], _dots.Length,
			(row, column) => column == 0 ? _dots[row].Row.Name : Write(_dots[row].Row.Value),
			(row, column) => column == 0 ? _dots[row].Row.Name : Raw(_dots[row].Row.Value));

	private string ValueName => XAxis?.Title is { Length: > 0 } title ? title : Words.Value;

	private bool NativeTooltips => ResolvedTooltip == MokaChartTooltip.Native;

	private bool ShowBefore => _dumbbell && !IsHidden(BeforeKey);

	private bool ShowValue => !_dumbbell || !IsHidden(ValueKey);

	/// <summary>A drawn row: its record, where it came from, its colors and what it reads at its end.</summary>
	private sealed record Dot(int Source, MokaDotPlotRow Row, string ColorClass, string? ColorStyle, bool Worse, string Figure);

	/// <inheritdoc />
	internal override bool AffectsGeometry(string parameterName) =>
		parameterName != nameof(NameTemplate) && base.AffectsGeometry(parameterName);

	/// <inheritdoc />
	internal override bool SameValue(string name, object? before, object? after) => name == nameof(Rows)
		? ChartSnapshot.SameItems(after as IReadOnlyList<MokaDotPlotRow>, _snapshot)
		: base.SameValue(name, before, after);

	/// <inheritdoc />
	internal override MokaScaleOptions ValueScaleOptions(MokaScaleOptions options) =>
		ZeroBased ? options : options with { IncludeZero = false, Nice = true };

	/// <inheritdoc />
	internal override (double Min, double Max) ValueRange(int panel)
	{
		double min = double.PositiveInfinity;
		double max = double.NegativeInfinity;
		foreach (Dot dot in _dots)
		{
			if (ShowValue)
			{
				Take(dot.Row.Value, ref min, ref max);
			}

			if (ShowBefore && dot.Row.Before is { } before)
			{
				Take(before, ref min, ref max);
			}
		}

		if (Stems && min <= max)
		{
			Take(Baseline, ref min, ref max);
		}

		return min <= max ? (min, max) : (double.NaN, double.NaN);
	}

	/// <inheritdoc />
	internal override void Rebuild()
	{
		_snapshot = DataVersion is null ? ChartSnapshot.Copy(Rows) : null;
		IReadOnlyList<MokaDotPlotRow> rows = Rows ?? [];
		_dumbbell = rows.Any(row => row.Before is not null);

		string slotClass = SeriesClass(1, null, false);
		string? slotStyle = SeriesStyle(1, null);
		bool markWorse = MarkWorse && _dumbbell;
		int[] order = ChartRows.Order(rows.Count, i => rows[i].Value, Sort ?? MokaChartSort.None);
		Dot[] dots = new Dot[order.Length];
		for (int position = 0; position < order.Length; position++)
		{
			MokaDotPlotRow row = rows[order[position]];
			bool worse = row.Before is { } before && double.IsFinite(before) && double.IsFinite(row.Value)
			             && (UpIsGood ? row.Value < before : row.Value > before);
			bool muted = markWorse ? !worse : IsMuted(row.Key ?? row.Name);
			MokaColor? tone = markWorse && worse ? row.Color ?? MokaColor.Primary : row.Color;
			string colorClass = SeriesClass(1, tone, muted);
			string? colorStyle = muted || tone is not null ? null : slotStyle;
			dots[position] = new Dot(order[position], row, colorClass, colorStyle, worse, FigureOf(row));
		}

		_dots = dots;
		string legendClass = markWorse ? ChartRows.ToneClass(MokaColor.Primary) : slotClass;
		string? legendStyle = markWorse ? null : slotStyle;
		_series = _dumbbell
			?
			[
				new ChartSeriesInfo(BeforeKey, BeforeLabel, 1, legendClass, ChartRows.PaleStyle(ChartRows.BaseColor(legendClass, legendStyle)),
					ChartMark.Dot, IsHidden(BeforeKey), false, null),
				new ChartSeriesInfo(ValueKey, AfterLabel, 1, legendClass, legendStyle, ChartMark.Dot, IsHidden(ValueKey), false, null)
			]
			: [];
	}

	/// <inheritdoc />
	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn)
	{
		if (drawn < 0 || drawn >= _dots.Length)
		{
			return null;
		}

		Dot dot = _dots[drawn];
		MokaDotPlotRow row = dot.Row;
		List<ChartActiveRow> rows = [];
		List<MokaChartPoint> points = [];
		List<string> notes = [];
		if (ShowBefore && row.Before is { } before)
		{
			string text = Write(before);
			rows.Add(new ChartActiveRow(BeforeLabel, dot.ColorClass, ChartRows.PaleStyle(ChartRows.BaseColor(dot.ColorClass, dot.ColorStyle)),
				text, null, false));
			points.Add(new MokaChartPoint(BeforeKey, BeforeLabel, 0, dot.Source, before, text) { Category = row.Name });
		}

		if (ShowValue)
		{
			string text = Write(row.Value);
			string name = _dumbbell ? AfterLabel : ValueName;
			rows.Add(new ChartActiveRow(name, dot.ColorClass, dot.ColorStyle, text, null, false));
			points.Insert(0, new MokaChartPoint(ValueKey, name, _dumbbell ? 1 : 0, dot.Source, row.Value, text) { Category = row.Name });
		}

		if (_dumbbell && row.Before is not null && ChangeText(row) is { Length: > 0 } change)
		{
			notes.Add(dot.Worse ? $"{change}, {WorseText}" : IsBetter(row) ? $"{change}, {BetterText}" : change);
		}

		if (!string.IsNullOrEmpty(row.Detail))
		{
			notes.Add(row.Detail);
		}

		if (ShowValue)
		{
			notes.AddRange(AnnotationNotes(row.Value));
		}

		return ChartRows.Describe(drawn, row.Name, rows, notes, points);
	}

	private string LaneClass(Dot dot) => new CssBuilder("moka-dotplot__lane")
		.AddClass(dot.ColorClass)
		.AddClass("moka-dotplot__lane--worse", dot.Worse && MarkWorse)
		.Build();

	// Where the marks sit on the track, as variables the stylesheet places them by.
	private string? LaneStyle(Dot dot)
	{
		double value = ShowValue ? Share(dot.Row.Value) : double.NaN;
		double before = ShowBefore && dot.Row.Before is { } b ? Share(b) : double.NaN;
		double from = double.NaN;
		double to = double.NaN;
		if (double.IsFinite(before) && double.IsFinite(value))
		{
			(from, to) = (Math.Min(before, value), Math.Max(before, value));
		}
		else if (Stems && double.IsFinite(value) && Share(Baseline) is var baseline && double.IsFinite(baseline))
		{
			(from, to) = (Math.Min(baseline, value), Math.Max(baseline, value));
		}

		return new StyleBuilder()
			.AddStyle(dot.ColorStyle)
			.AddStyle("--_dp-v", double.IsFinite(value) ? ChartRows.Css(value) : null)
			.AddStyle("--_dp-b", double.IsFinite(before) ? ChartRows.Css(before) : null)
			.AddStyle("--_dp-from", double.IsFinite(from) ? ChartRows.Css(from) : null)
			.AddStyle("--_dp-len", double.IsFinite(from) ? ChartRows.Css(to - from) : null)
			.Build();
	}

	private string? NativeTitle(Dot dot) => NativeTooltips
		? dot.Row.Before is not null && _dumbbell
			? $"{dot.Row.Name}: {BeforeLabel} {Write(dot.Row.Before)}, {AfterLabel} {Write(dot.Row.Value)}"
			: $"{dot.Row.Name}: {Write(dot.Row.Value)}"
		: null;

	private double Share(double value)
	{
		if (RowPlotModel().Scale is not { } scale || !double.IsFinite(value))
		{
			return double.NaN;
		}

		double share = scale.Share(value);
		return XAxis?.Reverse == true ? 1 - share : share;
	}

	private string FigureOf(MokaDotPlotRow row)
	{
		bool before = ShowBefore && row.Before is not null;
		return before && ShowValue
			? $"{Write(row.Before)} → {Write(row.Value)}"
			: before
				? Write(row.Before)
				: Write(row.Value);
	}

	private string ChangeText(MokaDotPlotRow row)
	{
		if (row.Before is not { } before || !double.IsFinite(before) || !double.IsFinite(row.Value))
		{
			return string.Empty;
		}

		double change = row.Value - before;
		string text = ChartRows.Signed(Write(Math.Abs(change)), change);
		return before != 0
			? $"{text} ({ChartRows.Signed(ChartWholes.Percent(Math.Abs(change / before)), change)})"
			: text;
	}

	private bool IsBetter(MokaDotPlotRow row) => row.Before is { } before && (UpIsGood ? row.Value > before : row.Value < before);

	private string Write(double? value) => value is { } v ? ChartRows.Write(this, v) : string.Empty;

	private static string Raw(double? value) =>
		value is { } v && double.IsFinite(v) ? v.ToString(CultureInfo.InvariantCulture) : string.Empty;

	private static void Take(double value, ref double min, ref double max)
	{
		if (!double.IsFinite(value))
		{
			return;
		}

		min = Math.Min(min, value);
		max = Math.Max(max, value);
	}
}
