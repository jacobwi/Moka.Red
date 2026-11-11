using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

public partial class MokaBarChart : IChartColumns
{
	private const string PositiveKey = "positive";
	private const string NegativeKey = "negative";
	private const int MeasureAbove = 100;
	private const double PixelsPerColumn = 3;
	private const double Headroom = 0.08;

	// How much of its column a printed value needs when it may reach over half of each shorter neighbour, over the whole
	// of each, and when every other column is shorter and prints nothing.
	private const double Reach = 0.5;
	private const double FarReach = 0.34;
	private const double LoneReach = 0.25;

	private List<ChartSeriesInfo> _modernInfo = [];
	private bool _seriesHaveOwnColors;
	private List<Shown> _shown = [];
	private int _rawCount;
	private int _columnCount;
	private int _bucket = 1;
	private int[] _order = [];
	private double[][] _drawn = [];
	private double[] _positive = [];
	private double[] _negative = [];
	private double _grandTotal;
	private (double Min, double Max) _valueRange = (double.NaN, double.NaN);
	private MokaScale? _scale;
	private bool _reverse;
	private double _baseline;
	private MokaValueLabels _labels;
	private bool _labelOutsideTarget;
	private int _largestColumn = -1;
	private int _largestLane = -1;
	private ChartColumnsModel? _model;
	private ChartTableModel? _table;
	private RenderFragment? _columnMarks;
	private RenderFragment<int>? _rowMarks;
	private double[]? _columnAt;
	private double[]? _columnWidth;
	private double _timePad;
	private ChartHoverData? _hover;

	/// <summary>A series that is drawn, with what it is drawn in.</summary>
	private sealed record Shown(MokaBarSeries Series, int Legend, string Key, string ColorClass, string? ColorStyle, bool Muted);

	/// <inheritdoc />
	/// <remarks>Bars on a time axis stand at their times, each as wide as the step to its neighbours allows.</remarks>
	internal override bool PlacesColumns => !_classic && _columnAt is not null;

	/// <inheritdoc />
	internal override double TimeEdgePad => _timePad;

	/// <inheritdoc />
	internal override ChartHoverData? HoverData(int panel) => PlacesColumns ? _hover ??= PlacedHover() : null;

	/// <inheritdoc />
	internal override bool DrawsRows => !_classic && Horizontal;

	/// <inheritdoc />
	internal override MokaChartTooltip AutoTooltip => MokaChartTooltip.Column;

	/// <inheritdoc />
	internal override bool SurfaceRows => !_classic && Horizontal;

	/// <inheritdoc />
	internal override bool MeasuresWidth => !_classic && !Horizontal && !Scrolls && _rawCount > MeasureAbove;

	/// <inheritdoc />
	internal override MokaChartRange? CurrentRange => null;

	/// <inheritdoc />
	/// <remarks>Shares are always read in percent; <see cref="MokaCartesianChartBase.Unit" /> then writes the values in the tooltip and the table.</remarks>
	internal override MokaChartUnit? ValueAxisUnit => Percent ? MokaChartUnit.Percent : Unit;

	/// <inheritdoc />
	internal override Func<double, string>? ValueAxisFormatter => Percent ? null : Formatter;

	/// <inheritdoc />
	internal override ChartTableModel? TableModel => _classic ? null : _table ??= BuildTable();

	private bool Stacks => Stacked || Percent;

	private bool Scrolls => (XAxis?.Fit ?? LabelFit) == MokaAxisLabelFit.Scroll;

	private MokaChartAxis? ValueAxis => Horizontal ? XAxis : YAxis;

	private bool RoundedEnds => (BarEnds ?? Spec.BarEnds ?? MokaBarEnds.Rounded) == MokaBarEnds.Rounded;

	private RenderFragment ColumnMarks => _columnMarks ??= builder =>
	{
		builder.OpenComponent<MokaChartColumns>(0);
		builder.AddComponentParameter(1, nameof(MokaChartColumns.Chart), this);
		builder.CloseComponent();
	};

	private RenderFragment<int> RowMarks => _rowMarks ??= row => _model is { } model ? MokaChartColumns.Row(model, row) : _ => { };

	/// <inheritdoc />
	internal override (double Min, double Max) ValueRange(int panel) => _classic ? base.ValueRange(panel) : _valueRange;

	/// <inheritdoc />
	internal override double CategoryShare(int index, int count) =>
		_classic ? base.CategoryShare(index, count) : (index + 0.5) / Math.Max(1, count);

	/// <inheritdoc />
	/// <remarks>A merged column is labelled by its first category along the axis; its tooltip names the whole span.</remarks>
	internal override string CategoryText(int index) =>
		_classic
			? base.CategoryText(index)
			: index >= 0 && index < _columnCount ? RawLabel(_order[index * _bucket]) : string.Empty;

	/// <inheritdoc />
	internal override int DrawnCount(int panel) => _classic ? 0 : _columnCount;

	/// <inheritdoc />
	ChartColumnsModel? IChartColumns.Columns(int panel) => _model;

	/// <inheritdoc />
	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn)
	{
		if (_classic || drawn < 0 || drawn >= _columnCount)
		{
			return null;
		}

		string heading = ColumnLabel(drawn);
		int? pin = pinnedDrawn is { } p && p >= 0 && p < _columnCount ? p : null;
		double total = _positive[drawn] + _negative[drawn];
		List<ChartActiveRow> rows = [];
		List<MokaChartPoint> points = [];
		for (int k = 0; k < _shown.Count; k++)
		{
			double value = _drawn[k][drawn];
			if (!double.IsFinite(value))
			{
				continue;
			}

			Shown shown = _shown[k];
			string text = Percent ? ChartWholes.Percent(value / 100) : ValueLabel(value);
			double? share = Percent ? value / 100 : Share(value, drawn);
			double? since = pin is { } at && double.IsFinite(_drawn[k][at]) ? value - _drawn[k][at] : null;
			string? delta = since is { } change
				? Signed(change)
				: Percent
					? RawText(RawValue(k, drawn))
					: _shown.Count > 1 && share is { } part ? ChartWholes.Percent(part) : null;
			(string colorClass, string? colorStyle) = Diverging ? DirectionColor(value) : (shown.ColorClass, BarStyle(k, drawn));
			// One series either side of zero reads by its direction: "-9 ms Faster".
			string name = Diverging && _shown.Count == 1 ? _modernInfo[value >= 0 ? 0 : 1].Name : shown.Series.Name;
			rows.Add(new ChartActiveRow(name, colorClass, colorStyle, text, delta, false));
			int legend = Diverging ? value >= 0 ? 0 : 1 : shown.Legend;
			int first = _order[drawn * _bucket];
			points.Add(new MokaChartPoint(shown.Key, shown.Series.Name, legend, drawn, value, text)
			{
				Category = heading,
				X = _columnAt is null ? first : MokaChartX.FromDateTimeOffset(Times![first]).Value,
				Time = _columnAt is null ? null : Times![first],
				Total = Percent ? 100 : total,
				Share = share,
				SincePin = since
			});
		}

		List<string> notes = [];
		if (rows.Count == 1 && !Percent && _grandTotal > 0 && double.IsFinite(points[0].Value) && points[0].Value >= 0)
		{
			string share = SmallPercent(points[0].Value / _grandTotal);
			string whole = ValueLabel(_grandTotal);
			notes.Add(ShareText?.Invoke(share, whole) ?? $"{share} of {whole}");
		}
		else if (rows.Count > 1 && Stacks && !Percent)
		{
			notes.Add($"{Words.Total} {ValueLabel(total)}");
		}

		// Thresholds and the target speak of the value; events and windows of the column's place, whether it has one.
		double noted = Percent || rows.Count == 0 ? double.NaN : Stacks ? total : points[0].Value;
		(double x, double reach) = _columnAt is { } middles && _columnWidth is { } widths
			? (middles[drawn], widths[drawn] / 2)
			: (drawn, 0.5);
		notes.AddRange(AnnotationNotes(noted, x, reach));

		string cell = rows.Count == 0
			? heading
			: $"{heading}, {string.Join(", ", rows.Select(row => $"{row.Name} {row.ValueText}"))}";
		if (notes.Count > 0)
		{
			cell = $"{cell}, {string.Join(", ", notes)}";
		}

		MokaChartPoint? focus = series >= 0 ? points.FirstOrDefault(point => point.Series == series) : null;
		return new ChartActiveInfo(heading, rows, notes, cell, new MokaChartTooltipContext(drawn, heading, points, focus, pin, notes));
	}

	private void ClearModern()
	{
		_modernInfo = [];
		_shown = [];
		_rawCount = 0;
		_columnCount = 0;
		_bucket = 1;
		_order = [];
		_drawn = [];
		_positive = [];
		_negative = [];
		_grandTotal = 0;
		_valueRange = (double.NaN, double.NaN);
		_scale = null;
		_model = null;
		_table = null;
		_columnAt = null;
		_columnWidth = null;
		_timePad = 0;
		_hover = null;
	}

	private void RebuildModern()
	{
		_table = null;
		_hover = null;
		IReadOnlyList<MokaBarSeries> all = Series ?? [];
		List<ChartSeriesInfo> info = [];
		List<Shown> shown = [];
		int raw = 0;
		for (int i = 0; i < all.Count; i++)
		{
			MokaBarSeries series = all[i];
			if (series.Values.Count == 0)
			{
				continue;
			}

			raw = Math.Max(raw, series.Values.Count);
			string key = series.Key ?? series.Name;
			int slot = SlotFor(i, series.Slot);
			bool muted = IsMuted(key);
			string colorClass = SeriesClass(slot, series.Color, muted);
			string? colorStyle = muted ? null : SeriesStyle(slot, series.ColorValue);
			bool hidden = !Diverging && IsHidden(key);
			if (!hidden)
			{
				shown.Add(new Shown(series, info.Count, key, colorClass, colorStyle, muted));
			}

			info.Add(new ChartSeriesInfo(key, series.Name, slot, colorClass, colorStyle, ChartMark.Box, hidden, muted, null));
		}

		if (Diverging)
		{
			info =
			[
				DirectionInfo(PositiveKey, PositiveText ?? "Above zero", 1, UpIsGood),
				DirectionInfo(NegativeKey, NegativeText ?? "Below zero", 2, !UpIsGood)
			];
		}

		// Bars with times stand at them, so they line up with the time axis's ticks; a value past the last time has no place.
		bool byTime = !Horizontal && (Sort ?? MokaChartSort.None) == MokaChartSort.None && Times is { Count: > 0 }
		              && XAxis?.Scale != MokaAxisScale.Category;
		if (byTime)
		{
			raw = Math.Min(raw, Times!.Count);
		}

		_modernInfo = info;
		_shown = shown;
		_rawCount = raw;
		_order = SortedOrder(raw);
		_bucket = !Horizontal && !Scrolls && raw > ColumnTarget ? (int)Math.Ceiling(raw / (double)ColumnTarget) : 1;
		_columnCount = raw == 0 ? 0 : (int)Math.Ceiling(raw / (double)_bucket);
		_timePad = 0;
		(_columnAt, _columnWidth) = byTime && _columnCount > 0 ? TimePlaces() : (null, null);
		_drawn = [.. shown.Select((_, k) => DrawnValues(k))];
		if (Percent)
		{
			ToShares();
		}

		Totals();
		_grandTotal = GrandTotal();
		_labels = ResolveLabels();
		_valueRange = Extent();
		_scale = ValueScale(ValueAxis, _valueRange.Min, _valueRange.Max);
		_reverse = ValueAxis?.Reverse ?? false;
		_baseline = _scale.Kind != MokaScaleKind.Log && _scale.Min <= 0 && _scale.Max >= 0 ? 0 : _scale.Min;
		FindLargest();

		_model = _columnCount == 0
			? null
			: new ChartColumnsModel(Horizontal ? "moka-chart-cols moka-chart-cols--rows" : "moka-chart-cols", null, _columnCount,
				BuildColumn);
	}

	private ChartSeriesInfo DirectionInfo(string key, string name, int slot, bool up)
	{
		string style = new StyleBuilder()
			.AddStyle("--moka-chart-color", up ? "var(--moka-chart-up)" : "var(--moka-chart-down)")
			.Build()!;
		return new ChartSeriesInfo(key, name, slot, SeriesClass(slot, null, false), style, ChartMark.Box, IsHidden(key), false, null);
	}

	private int ColumnTarget
	{
		get
		{
			int most = Math.Max(1, MaxColumns);
			return PlotWidthBucket is { } width && width > 0 ? Math.Clamp((int)(width / PixelsPerColumn), 1, most) : most;
		}
	}

	private int[] SortedOrder(int raw)
	{
		MokaChartSort sort = Sort ?? (Horizontal ? MokaChartSort.Descending : MokaChartSort.None);
		int[] order = new int[raw];
		for (int i = 0; i < raw; i++)
		{
			order[i] = i;
		}

		if (sort == MokaChartSort.None || raw < 2)
		{
			return order;
		}

		double[] keys = new double[raw];
		for (int i = 0; i < raw; i++)
		{
			keys[i] = SortKey(i);
		}

		IEnumerable<int> sorted = sort == MokaChartSort.Descending
			? order.OrderByDescending(i => double.IsFinite(keys[i]) ? keys[i] : double.NegativeInfinity)
			: order.OrderBy(i => double.IsFinite(keys[i]) ? keys[i] : double.PositiveInfinity);
		return [.. sorted];
	}

	// Rows of shares sort by the first series' share; everything else by what the category adds up to.
	private double SortKey(int category)
	{
		if (Percent)
		{
			double whole = 0;
			foreach (Shown shown in _shown)
			{
				whole += Math.Max(0, Finite(Value(shown.Series, category)));
			}

			return whole > 0 && _shown.Count > 0 ? Math.Max(0, Finite(Value(_shown[0].Series, category))) / whole : double.NaN;
		}

		double sum = 0;
		bool any = false;
		foreach (Shown shown in _shown)
		{
			double value = Value(shown.Series, category);
			if (double.IsFinite(value))
			{
				sum += value;
				any = true;
			}
		}

		return any ? sum : double.NaN;
	}

	private double[] DrawnValues(int lane)
	{
		MokaBarSeries series = _shown[lane].Series;
		double[] drawn = new double[_columnCount];
		if (_bucket == 1)
		{
			for (int c = 0; c < _columnCount; c++)
			{
				drawn[c] = Value(series, _order[c]);
			}

			return drawn;
		}

		double[] sorted = new double[_rawCount];
		for (int i = 0; i < _rawCount; i++)
		{
			sorted[i] = Value(series, _order[i]);
		}

		for (int c = 0; c < _columnCount; c++)
		{
			int start = c * _bucket;
			int length = Math.Min(_bucket, _rawCount - start);
			drawn[c] = MokaDownsampler.Buckets(sorted.AsSpan(start, length), 1, ColumnAggregate) is [var merged] ? merged : double.NaN;
		}

		return drawn;
	}

	private void ToShares()
	{
		for (int c = 0; c < _columnCount; c++)
		{
			double whole = 0;
			foreach (double[] lane in _drawn)
			{
				whole += Math.Max(0, Finite(lane[c]));
			}

			foreach (double[] lane in _drawn)
			{
				lane[c] = whole > 0 && double.IsFinite(lane[c]) ? Math.Max(0, lane[c]) / whole * 100 : double.NaN;
			}
		}
	}

	private void Totals()
	{
		_positive = new double[_columnCount];
		_negative = new double[_columnCount];
		for (int c = 0; c < _columnCount; c++)
		{
			foreach (double[] lane in _drawn)
			{
				double value = lane[c];
				if (value > 0)
				{
					_positive[c] += value;
				}
				else if (value < 0)
				{
					_negative[c] += value;
				}
			}
		}
	}

	// What one bar is a share of when the chart has one series: everything it draws above zero.
	private double GrandTotal()
	{
		if (_shown.Count != 1)
		{
			return 0;
		}

		double sum = 0;
		foreach (double value in _drawn[0])
		{
			if (value > 0)
			{
				sum += value;
			}
		}

		return sum;
	}

	private MokaValueLabels ResolveLabels()
	{
		_labelOutsideTarget = false;
		if (ValueLabels is { } chosen && chosen != MokaValueLabels.Auto)
		{
			return Percent && chosen == MokaValueLabels.Tip ? MokaValueLabels.Inside : chosen;
		}

		if (Percent)
		{
			return MokaValueLabels.Inside;
		}

		if (Horizontal || Diverging)
		{
			return MokaValueLabels.Tip;
		}

		if (TargetLow is not null || TargetHigh is not null)
		{
			_labelOutsideTarget = true;
			return MokaValueLabels.None;
		}

		return Spec.DirectLabels switch
		{
			MokaDirectLabels.Values or MokaDirectLabels.Both => MokaValueLabels.Tip,
			MokaDirectLabels.Names => MokaValueLabels.Largest,
			_ => MokaValueLabels.None
		};
	}

	// A row prints a value that doesn't fit past its bar inside its end instead, so only columns need the room. Only a
	// rounded axis takes it: on today's equal divisions it would put an odd number at the top, and the value hides instead.
	private bool PrintsPastEnds => !Horizontal && (ValueAxis?.Nice ?? NiceValueAxis)
	                               && (_labels is MokaValueLabels.Tip or MokaValueLabels.Largest || _labelOutsideTarget);

	private (double Min, double Max) Extent()
	{
		if (_columnCount == 0 || _shown.Count == 0)
		{
			return (double.NaN, double.NaN);
		}

		if (Percent)
		{
			return (0, 100);
		}

		double min = double.PositiveInfinity;
		double max = double.NegativeInfinity;
		for (int c = 0; c < _columnCount; c++)
		{
			if (Stacks)
			{
				min = Math.Min(min, _negative[c]);
				max = Math.Max(max, _positive[c]);
				continue;
			}

			foreach (double[] lane in _drawn)
			{
				if (double.IsFinite(lane[c]))
				{
					min = Math.Min(min, lane[c]);
					max = Math.Max(max, lane[c]);
				}
			}
		}

		if (!double.IsFinite(min) || !double.IsFinite(max))
		{
			return (double.NaN, double.NaN);
		}

		if (Diverging)
		{
			double reach = Math.Max(Math.Abs(min), Math.Abs(max));
			(min, max) = (-reach, reach);
		}

		// Values printed past the bars' ends need room in the plot, so the axis reaches a little further.
		if (PrintsPastEnds)
		{
			double span = Math.Max(Math.Abs(max), Math.Abs(min));
			if (max > 0)
			{
				max += span * Headroom;
			}

			if (min < 0)
			{
				min -= span * Headroom;
			}
		}

		return (min, max);
	}

	private void FindLargest()
	{
		_largestColumn = -1;
		_largestLane = -1;
		double best = double.NegativeInfinity;
		for (int c = 0; c < _columnCount; c++)
		{
			if (Stacks)
			{
				if (_positive[c] > best)
				{
					best = _positive[c];
					_largestColumn = c;
					_largestLane = 0;
				}

				continue;
			}

			for (int k = 0; k < _drawn.Length; k++)
			{
				double value = Diverging ? Math.Abs(_drawn[k][c]) : _drawn[k][c];
				if (double.IsFinite(value) && value > best)
				{
					best = value;
					_largestColumn = c;
					_largestLane = k;
				}
			}
		}
	}

	private ChartColumn BuildColumn(int column)
	{
		int lanes = Stacks ? 1 : Math.Max(1, _shown.Count);
		List<ChartLane> built = [];
		if (Stacks)
		{
			built.Add(StackLane(column));
		}
		else
		{
			for (int k = 0; k < _shown.Count; k++)
			{
				built.Add(BarLane(column, k, lanes));
			}
		}

		string share = lanes > 1 || Stacks ? "72%" : Horizontal ? "46%" : "60%";
		string? style = Horizontal
			? new StyleBuilder().AddStyle("--_gh", share).Build()
			: new StyleBuilder().AddStyle(SlotStyle(column)).AddStyle("--_gw", share).Build();
		string classes = Horizontal ? "moka-chart-col moka-chart-col--row" : "moka-chart-col";
		return new ChartColumn(column, classes, style, NativeTitles ? NativeTitle(column) : null, built);
	}

	// The browser's titles, where the chart draws no tooltip of its own: a static page, or a look whose tooltips are the
	// browser's, which keeps them when a listener or the keyboard binds the script.
	private bool NativeTitles => !Interactive || ResolvedTooltip == MokaChartTooltip.Native;

	// An even slot, or the column's own span on a time axis, cut to the plot where the axis is pinned narrower.
	private string? SlotStyle(int column)
	{
		if (_columnAt is not { } at || _columnWidth is not { } width)
		{
			return ChartColumns.SlotStyle(column, _columnCount);
		}

		ChartXFrame frame = XFrame();
		double a = frame.Share(at[column] - (width[column] / 2));
		double b = frame.Share(at[column] + (width[column] / 2));
		double start = Math.Clamp(Math.Min(a, b), 0, 1);
		double end = Math.Clamp(Math.Max(a, b), 0, 1);
		return new StyleBuilder()
			.AddStyle("--_cs", ChartColumns.Css(start))
			.AddStyle("--_cw", ChartColumns.Css(Math.Max(0, end - start)))
			.Build();
	}

	// Each column's middle and width on a time axis, in Unix milliseconds. A column is as wide as the usual step between
	// times, and never wider than the gap to either neighbour, so bars never overlap; a merged column reaches from its
	// first time to its last. The axis reaches half a column past the ends, so the end bars sit whole in the plot.
	private (double[] At, double[] Width) TimePlaces()
	{
		IReadOnlyList<DateTimeOffset> times = Times!;
		double[] t = new double[_rawCount];
		for (int i = 0; i < t.Length; i++)
		{
			t[i] = MokaChartX.FromDateTimeOffset(times[i]).Value;
		}

		double step = UsualGap(t);
		if (!double.IsFinite(step))
		{
			// One time, or all alike: a narrow bar, a 24th of the axis, since there is no step to go by.
			_timePad = 0;
			(double from, double to) = MeasureTimeSpan();
			step = (to - from) / 24;
		}

		double[] at = new double[_columnCount];
		double[] width = new double[_columnCount];
		for (int c = 0; c < _columnCount; c++)
		{
			int start = c * _bucket;
			int end = Math.Min(_rawCount, start + _bucket) - 1;
			double low = Math.Min(t[start], t[end]);
			double high = Math.Max(t[start], t[end]);
			double before = start > 0 ? Math.Abs(t[start] - t[start - 1]) : double.NaN;
			double after = end < _rawCount - 1 ? Math.Abs(t[end + 1] - t[end]) : double.NaN;
			at[c] = (low + high) / 2;
			width[c] = (high - low) + Least(step, before, after);
		}

		_timePad = Math.Max(width[0], width[^1]) / 2;
		return (at, width);
	}

	// The middle of the gaps between neighbouring times; NaN when there are none.
	private static double UsualGap(double[] times)
	{
		List<double> gaps = [];
		for (int i = 1; i < times.Length; i++)
		{
			double gap = Math.Abs(times[i] - times[i - 1]);
			if (gap > 0 && double.IsFinite(gap))
			{
				gaps.Add(gap);
			}
		}

		if (gaps.Count == 0)
		{
			return double.NaN;
		}

		gaps.Sort();
		return gaps[gaps.Count / 2];
	}

	private static double Least(double step, double before, double after)
	{
		double least = step;
		foreach (double gap in (ReadOnlySpan<double>)[before, after])
		{
			if (gap > 0 && double.IsFinite(gap) && gap < least)
			{
				least = gap;
			}
		}

		return least;
	}

	private ChartHoverData PlacedHover()
	{
		ChartXFrame frame = XFrame();
		float[] x = new float[_columnCount];
		for (int c = 0; c < x.Length; c++)
		{
			x[c] = (float)frame.Share(_columnAt![c]);
		}

		return new ChartHoverData(frame.From, frame.To, x, []);
	}

	// A bar's own color from its series' Colors, else its series' color. A merged column has no one value to take it from.
	private string? BarStyle(int lane, int column)
	{
		Shown shown = _shown[lane];
		if (_bucket != 1 || shown.Muted || shown.Series.Colors is not { Count: > 0 } colors || column < 0 || column >= _order.Length)
		{
			return shown.ColorStyle;
		}

		int category = _order[column];
		return category < colors.Count && colors[category] is { } own && CssValues.IsColor(own)
			? new StyleBuilder().AddStyle("--moka-chart-color", own.Trim()).Build()
			: shown.ColorStyle;
	}

	private ChartLane BarLane(int column, int lane, int lanes)
	{
		double value = _drawn[lane][column];
		if (!double.IsFinite(value) || (Diverging && IsHidden(value >= 0 ? PositiveKey : NegativeKey)))
		{
			return new ChartLane([], []);
		}

		double from = AxisShare(_baseline);
		double to = AxisShare(value);
		double low = Math.Min(from, to);
		double high = Math.Max(from, to);
		bool upward = to >= from;
		ChartBoxEnds ends = RoundedEnds ? upward ? ChartBoxEnds.RoundHigh : ChartBoxEnds.RoundLow : ChartBoxEnds.None;
		Shown shown = _shown[lane];
		ChartBox mark = Diverging
			? ChartColumns.Mark("bar", low, high, null, null, value >= 0 ? PositiveKey : NegativeKey, ends,
				$"moka-chart-hatch {DirectionClass(value)}")
			: ChartColumns.Mark("bar", low, high, shown.ColorClass, BarStyle(lane, column), shown.Key, ends, "moka-chart-hatch");

		List<ChartTip> tips = [];
		bool largest = _labels == MokaValueLabels.Largest && column == _largestColumn && lane == _largestLane;
		bool outside = _labelOutsideTarget && OutsideTarget(value);
		string text = ValueLabel(value);
		if (_labels == MokaValueLabels.Tip)
		{
			AddTip(tips, column, text, low, high, !upward, lanes);
		}
		else if (largest || outside)
		{
			AddTip(tips, column, text, low, high, !upward, largest ? LoneReach : ReachOver(column, value, c => _drawn[lane][c], lanes));
		}
		else if (_labels == MokaValueLabels.Inside)
		{
			mark = ChartColumns.WithLabel(mark, text);
		}

		return new ChartLane([mark], tips);
	}

	private ChartLane StackLane(int column)
	{
		int lastUp = -1;
		int lastDown = -1;
		for (int k = 0; k < _drawn.Length; k++)
		{
			double value = _drawn[k][column];
			if (value > 0)
			{
				lastUp = k;
			}
			else if (value < 0)
			{
				lastDown = k;
			}
		}

		List<ChartBox> marks = [];
		double up = 0;
		double down = 0;
		bool upStarted = false;
		bool downStarted = false;
		for (int k = 0; k < _drawn.Length; k++)
		{
			double value = _drawn[k][column];
			if (!double.IsFinite(value) || value == 0)
			{
				continue;
			}

			bool positive = value > 0;
			double from = positive ? up : down;
			double to = from + value;
			ChartBoxEnds ends = ChartBoxEnds.None;
			if (positive)
			{
				up = to;
				ends |= upStarted ? ChartBoxEnds.GapLow : ChartBoxEnds.None;
				ends |= RoundedEnds && k == lastUp ? ChartBoxEnds.RoundHigh : ChartBoxEnds.None;
				upStarted = true;
			}
			else
			{
				down = to;
				ends |= downStarted ? ChartBoxEnds.GapHigh : ChartBoxEnds.None;
				ends |= RoundedEnds && k == lastDown ? ChartBoxEnds.RoundLow : ChartBoxEnds.None;
				downStarted = true;
			}

			double a = AxisShare(from);
			double b = AxisShare(to);
			Shown shown = _shown[k];
			ChartBox mark = Diverging
				? ChartColumns.Mark("bar", Math.Min(a, b), Math.Max(a, b), null, null, positive ? PositiveKey : NegativeKey, ends,
					$"moka-chart-hatch {DirectionClass(value)}")
				: ChartColumns.Mark("bar", Math.Min(a, b), Math.Max(a, b), shown.ColorClass, BarStyle(k, column), shown.Key, ends,
					"moka-chart-hatch");
			if (_labels == MokaValueLabels.Inside)
			{
				mark = ChartColumns.WithLabel(mark, Percent ? ChartWholes.Percent(value / 100) : ValueLabel(value));
			}

			marks.Add(mark);
		}

		List<ChartTip> tips = [];
		bool largest = _labels == MokaValueLabels.Largest && column == _largestColumn;
		double total = _positive[column] + _negative[column];
		bool outside = _labelOutsideTarget && OutsideTarget(total);
		if (!Percent && (_labels == MokaValueLabels.Tip || largest || outside))
		{
			double width = largest ? LoneReach : outside ? ReachOver(column, total, c => _positive[c] + _negative[c], 1) : 1;
			double zero = AxisShare(0);
			if (_positive[column] > 0)
			{
				double top = AxisShare(_positive[column]);
				AddTip(tips, column, ValueLabel(_positive[column]), Math.Min(zero, top), Math.Max(zero, top), top < zero, width);
			}

			if (_negative[column] < 0 && !largest)
			{
				double bottom = AxisShare(_negative[column]);
				AddTip(tips, column, ValueLabel(_negative[column]), Math.Min(zero, bottom), Math.Max(zero, bottom), bottom < zero, 1);
			}
		}

		return new ChartLane(marks, tips);
	}

	private void AddTip(List<ChartTip> tips, int column, string text, double low, double high, bool before, double share)
	{
		int edge = column == 0 ? -1 : column == _columnCount - 1 ? 1 : 0;
		if (Horizontal)
		{
			ChartColumns.RowTips(tips, text, low, high, before);
		}
		else if (ChartColumns.ColumnTip(text, before ? low : high, before, share, _columnCount > 1 ? edge : 0) is { } tip)
		{
			tips.Add(tip);
		}
	}

	// How many of its column's widths a value printed over the target needs. It may reach over neighbours that print
	// nothing, since they are inside the target, so shorter, and it sits above them: half of each when the next one along
	// prints, the whole of each when nothing within two columns does. A value under the target, or one beside another
	// printed value, has its own column only.
	private double ReachOver(int column, double value, Func<int, double> valueAt, double own)
	{
		if (TargetHigh is not { } high || !(value > high) || Printed(column, 1, valueAt))
		{
			return own;
		}

		return Printed(column, 2, valueAt) ? Reach : FarReach;
	}

	private bool Printed(int column, int distance, Func<int, double> valueAt)
	{
		foreach (int neighbour in (ReadOnlySpan<int>)[column - distance, column + distance])
		{
			if (neighbour >= 0 && neighbour < _columnCount && OutsideTarget(valueAt(neighbour)))
			{
				return true;
			}
		}

		return false;
	}

	private string NativeTitle(int column)
	{
		string heading = ColumnLabel(column);
		List<string> parts = [];
		for (int k = 0; k < _shown.Count; k++)
		{
			double value = _drawn[k][column];
			if (double.IsFinite(value))
			{
				parts.Add($"{_shown[k].Series.Name}: {(Percent ? ChartWholes.Percent(value / 100) : ValueLabel(value))}");
			}
		}

		return parts.Count == 1 ? $"{heading} · {parts[0]}" : $"{heading}\n{string.Join("\n", parts)}";
	}

	private ChartTableModel BuildTable()
	{
		List<string> headers = [_columnAt is null ? Words.Name : XAxis?.Title ?? Words.Time];
		foreach (Shown shown in _shown)
		{
			headers.Add(shown.Series.Name);
			if (Percent)
			{
				headers.Add($"{shown.Series.Name} {Words.Share}");
			}
		}

		int per = Percent ? 2 : 1;
		return new ChartTableModel(headers, _rawCount,
			(row, column) => TableCell(row, column, per, false),
			(row, column) => TableCell(row, column, per, true));
	}

	private string TableCell(int row, int column, int per, bool raw)
	{
		int category = row < _order.Length ? _order[row] : row;
		if (column == 0)
		{
			return TableLabel(category, raw);
		}

		int lane = (column - 1) / per;
		if (lane >= _shown.Count)
		{
			return string.Empty;
		}

		double value = Value(_shown[lane].Series, category);
		if (Percent && (column - 1) % per == 1)
		{
			double whole = _shown.Sum(shown => Math.Max(0, Finite(Value(shown.Series, category))));
			double share = whole > 0 && double.IsFinite(value) ? Math.Max(0, value) / whole : double.NaN;
			return !double.IsFinite(share)
				? string.Empty
				: raw ? share.ToString(CultureInfo.InvariantCulture) : ChartWholes.Percent(share);
		}

		return !double.IsFinite(value)
			? string.Empty
			: raw ? value.ToString(CultureInfo.InvariantCulture) : RawText(value);
	}

	private string ColumnLabel(int column)
	{
		if (column < 0 || column >= _columnCount)
		{
			return string.Empty;
		}

		int start = column * _bucket;
		if (_bucket == 1)
		{
			return RawLabel(_order[start]);
		}

		int end = Math.Min(_rawCount, start + _bucket) - 1;
		return end > start ? Words.Between(RawLabel(_order[start]), RawLabel(_order[end])) : RawLabel(_order[start]);
	}

	// A category's name: its time on a time axis, written as the axis's tooltips write one, else its label.
	private string RawLabel(int category) =>
		_columnAt is not null && Times is { } times && category >= 0 && category < times.Count
			? XFrame().Text(MokaChartX.FromDateTimeOffset(times[category]).Value)
			: Labels is { } labels && category >= 0 && category < labels.Count
				? labels[category]
				: category.ToString(CultureInfo.CurrentCulture);

	// A category's name in the table: its time in full, and in ISO 8601 for CSV, on a time axis; else its label.
	private string TableLabel(int category, bool raw)
	{
		if (_columnAt is null || Times is not { } times || category < 0 || category >= times.Count)
		{
			return RawLabel(category);
		}

		ChartXFrame frame = XFrame();
		return raw
			? times[category].ToString("o", CultureInfo.InvariantCulture)
			: MokaTimeTicks.TooltipText(times[category], TimeSpan.FromMilliseconds(Math.Max(1, frame.To - frame.From)), TimeOptions());
	}

	// The value of a drawn column as given, before it became a share.
	private double RawValue(int lane, int column)
	{
		if (_bucket == 1)
		{
			return Value(_shown[lane].Series, _order[column]);
		}

		int start = column * _bucket;
		int length = Math.Min(_bucket, _rawCount - start);
		double[] values = new double[length];
		for (int i = 0; i < length; i++)
		{
			values[i] = Value(_shown[lane].Series, _order[start + i]);
		}

		return MokaDownsampler.Buckets(values, 1, ColumnAggregate) is [var merged] ? merged : double.NaN;
	}

	private double? Share(double value, int column)
	{
		double whole = _positive[column];
		return value >= 0 && whole > 0 ? value / whole : null;
	}

	private bool OutsideTarget(double value) =>
		double.IsFinite(value) && ((TargetLow is { } low && value < low) || (TargetHigh is { } high && value > high));

	private (string Class, string? Style) DirectionColor(double value) =>
		(SeriesClass(value >= 0 ? 1 : 2, null, false), new StyleBuilder()
			.AddStyle("--moka-chart-color", (value >= 0) == UpIsGood ? "var(--moka-chart-up)" : "var(--moka-chart-down)")
			.Build());

	private string DirectionClass(double value) =>
		(value >= 0) == UpIsGood ? "moka-chart-mark--up" : "moka-chart-mark--down";

	private double AxisShare(double value)
	{
		if (_scale is null)
		{
			return 0;
		}

		double share = _scale.Share(value);
		share = double.IsFinite(share) ? Math.Clamp(share, 0, 1) : 0;
		return _reverse ? 1 - share : share;
	}

	// At the value's own precision, so a printed value or a tooltip never rounds to the axis's step.
	private string ValueLabel(double value) => WriteValue(value, ValueAxis);

	// A value as given: as the axis writes it, except on a chart of shares, whose axis is in percent.
	private string RawText(double value) => !Percent
		? ValueLabel(value)
		: Formatter?.Invoke(value) ?? Unit?.Format(value, null, CultureInfo.CurrentCulture)
		  ?? value.ToString(TickFormat, CultureInfo.CurrentCulture);

	private string Signed(double change)
	{
		string text = Percent ? ChartWholes.Percent(Math.Abs(change) / 100) : ValueLabel(Math.Abs(change));
		return change < 0 ? $"-{text}" : $"+{text}";
	}

	private static double Value(MokaBarSeries series, int category) =>
		category >= 0 && category < series.Values.Count ? series.Values[category] : double.NaN;

	private static double Finite(double value) => double.IsFinite(value) ? value : 0;

	// A share of a long list is often under one percent, which a whole percent would round to nothing.
	private static string SmallPercent(double share) => share switch
	{
		>= 0.1 or <= 0 => ChartWholes.Percent(share),
		>= 0.001 => share.ToString("0.#%", CultureInfo.CurrentCulture),
		_ => $"<{0.001.ToString("0.#%", CultureInfo.CurrentCulture)}"
	};
}
