using System.Globalization;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

public partial class MokaBarChart
{
	private const string TotalKey = "total";
	private const int IncreaseLegend = 0;
	private const int DecreaseLegend = 1;
	private const int TotalLegend = 2;

	// No font is small enough for a value or a join to show in a column narrower than this many pixels.
	private const double NarrowColumn = 10;

	// The width an unmeasured plot is taken to have, as the line chart takes it.
	private const double UnmeasuredWidth = 800;

	/// <inheritdoc />
	/// <remarks>
	///     A waterfall prints its changes at their tips by default, so its axis is always nice and leaves room above the
	///     highest running total, as the histogram's and candlestick's are; the Standard look's quarter ticks end at the
	///     highest bar and leave its value nowhere to go.
	/// </remarks>
	internal override bool NiceValueAxis => Waterfall || base.NiceValueAxis;

	private MokaBarSeries? _stepSeries;
	private int _givenSteps;
	private Step[] _steps = [];
	private (int First, int Last)[] _spans = [];
	private Step[] _stepColumns = [];
	private bool _narrowSteps;

	private enum StepKind
	{
		Gap,
		Increase,
		Decrease,
		Total
	}

	/// <summary>
	///     One bar of a waterfall: a change from the running total before it to the one after it, or a total, a level from
	///     zero. A gap is a value that isn't a number, which changes nothing and draws nothing.
	/// </summary>
	/// <param name="Kind">What it is.</param>
	/// <param name="Change">The change, or a total's level.</param>
	/// <param name="From">The running total before it; zero for a total.</param>
	/// <param name="To">The running total after it.</param>
	private readonly record struct Step(StepKind Kind, double Change, double From, double To);

	private void ClearSteps()
	{
		_stepSeries = null;
		_givenSteps = 0;
		_steps = [];
		_spans = [];
		_stepColumns = [];
		_narrowSteps = false;
	}

	private void RebuildSteps()
	{
		ClearModern();
		MokaBarSeries? series = Series is { Count: > 0 } all ? all[0] : null;
		IReadOnlyList<double> values = series?.Values ?? [];
		_stepSeries = series;
		_givenSteps = values.Count;
		_steps = StepsOf(values);
		_rawCount = _steps.Length;
		_spans = Spans(_rawCount);
		_columnCount = _spans.Length;
		_stepColumns = [.. _spans.Select(span => Merged(span.First, span.Last))];
		_modernInfo = StepInfo();
		_narrowSteps = NarrowSteps();
		_labels = ValueLabels is { } chosen && chosen != MokaValueLabels.Auto ? chosen
			: _narrowSteps ? MokaValueLabels.None
			: MokaValueLabels.Tip;
		_labelOutsideTarget = false;
		_valueRange = StepExtent();
		_scale = ValueScale(ValueAxis, _valueRange.Min, _valueRange.Max);
		_reverse = ValueAxis?.Reverse ?? false;
		_baseline = _scale.Kind != MokaScaleKind.Log && _scale.Min <= 0 && _scale.Max >= 0 ? 0 : _scale.Min;
		FindLargestStep();
		_model = _columnCount == 0
			? null
			: new ChartColumnsModel(Horizontal ? "moka-chart-cols moka-chart-cols--rows" : "moka-chart-cols", null, _columnCount,
				StepColumn);
	}

	// A total is a level when it has one, so a start balance or a subtotal from the books stands as given and the changes
	// after it count on from it; without one it is the running total.
	private Step[] StepsOf(IReadOnlyList<double> values)
	{
		int given = values.Count;
		if (given == 0)
		{
			return [];
		}

		HashSet<int> totals = TotalsAt is { Count: > 0 } at ? [.. at] : [];
		Step[] steps = new Step[given + (ShowTotal ? 1 : 0)];
		double running = 0;
		for (int i = 0; i < given; i++)
		{
			double value = values[i];
			if (totals.Contains(i))
			{
				double level = double.IsFinite(value) ? value : running;
				steps[i] = new Step(StepKind.Total, level, 0, level);
				running = level;
			}
			else if (!double.IsFinite(value))
			{
				steps[i] = new Step(StepKind.Gap, double.NaN, running, running);
			}
			else
			{
				steps[i] = new Step(value >= 0 ? StepKind.Increase : StepKind.Decrease, value, running, running + value);
				running += value;
			}
		}

		if (ShowTotal)
		{
			steps[given] = new Step(StepKind.Total, running, 0, running);
		}

		return steps;
	}

	// Columns past what the plot holds merge the changes between totals, and a total always stands alone, so the running
	// total still reads at every total and a merged column still runs from where its first change starts to where its
	// last one ends.
	private (int First, int Last)[] Spans(int raw)
	{
		int bucket = !Horizontal && !Scrolls && raw > ColumnTarget ? (int)Math.Ceiling(raw / (double)ColumnTarget) : 1;
		List<(int First, int Last)> spans = new(bucket == 1 ? raw : (raw / bucket) + 1);
		int first = 0;
		while (first < raw)
		{
			int last = first;
			if (_steps[first].Kind != StepKind.Total)
			{
				while (last + 1 < raw && last + 1 - first < bucket && _steps[last + 1].Kind != StepKind.Total)
				{
					last++;
				}
			}

			spans.Add((first, last));
			first = last + 1;
		}

		return [.. spans];
	}

	// A long waterfall merged to a few pixels a column would send a value and two joins for every column, all hidden.
	private bool NarrowSteps() =>
		_columnCount > 0 && (PlotWidthBucket is > 0 and var width
			? width / (double)_columnCount < NarrowColumn
			: _columnCount < _rawCount && UnmeasuredWidth / _columnCount < NarrowColumn);

	private Step Merged(int first, int last)
	{
		if (first == last)
		{
			return _steps[first];
		}

		double sum = 0;
		bool any = false;
		for (int i = first; i <= last; i++)
		{
			if (double.IsFinite(_steps[i].Change))
			{
				sum += _steps[i].Change;
				any = true;
			}
		}

		Step from = _steps[first];
		Step to = _steps[last];
		return any
			? new Step(sum >= 0 ? StepKind.Increase : StepKind.Decrease, sum, from.From, to.To)
			: new Step(StepKind.Gap, double.NaN, from.From, to.To);
	}

	// The legend names what the bars are rather than the series: the two directions, as diverging bars do, and the
	// totals once there are any.
	private List<ChartSeriesInfo> StepInfo()
	{
		List<ChartSeriesInfo> info =
		[
			DirectionInfo(PositiveKey, PositiveText ?? Words.Increase, 1, UpIsGood),
			DirectionInfo(NegativeKey, NegativeText ?? Words.Decrease, 2, !UpIsGood)
		];
		if (_steps.Any(step => step.Kind == StepKind.Total))
		{
			(string colorClass, string? colorStyle) = TotalKeyPaint();
			info.Add(new ChartSeriesInfo(TotalKey, Words.Total, 3, colorClass, colorStyle, ChartMark.Box, IsHidden(TotalKey), false, null));
		}

		return info;
	}

	// The series' own color, when it names one, is the totals'. Its first slot would be one of the directions' colors on
	// most palettes, so without one the totals take a neutral.
	private bool TotalsOwnColor => _stepSeries is { } series
	                               && (series.Color is not null || series.Slot is not null || CssValues.IsColor(series.ColorValue));

	private (string Class, string? Style) TotalKeyPaint()
	{
		if (TotalsOwnColor)
		{
			int slot = SlotFor(0, _stepSeries!.Slot);
			return (SeriesClass(slot, _stepSeries.Color, false), SeriesStyle(slot, _stepSeries.ColorValue));
		}

		return (SeriesClass(3, null, false), new StyleBuilder()
			.AddStyle("--moka-chart-color", "var(--moka-color-on-surface-tertiary)")
			.Build());
	}

	private (double Min, double Max) StepExtent()
	{
		double min = 0;
		double max = 0;
		bool any = false;
		foreach (Step step in _stepColumns)
		{
			if (step.Kind == StepKind.Gap)
			{
				continue;
			}

			min = Math.Min(min, Math.Min(step.From, step.To));
			max = Math.Max(max, Math.Max(step.From, step.To));
			any = true;
		}

		if (!any)
		{
			return (double.NaN, double.NaN);
		}

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

	private void FindLargestStep()
	{
		_largestColumn = -1;
		_largestLane = 0;
		double best = double.NegativeInfinity;
		foreach (bool totals in (ReadOnlySpan<bool>)[false, true])
		{
			for (int c = 0; c < _stepColumns.Length; c++)
			{
				Step step = _stepColumns[c];
				bool isTotal = step.Kind == StepKind.Total;
				if (step.Kind != StepKind.Gap && isTotal == totals && Math.Abs(step.Change) > best)
				{
					best = Math.Abs(step.Change);
					_largestColumn = c;
				}
			}

			if (_largestColumn >= 0)
			{
				return;
			}
		}
	}

	private ChartColumn StepColumn(int column)
	{
		Step step = _stepColumns[column];
		List<ChartBox> marks = [];
		List<ChartTip> tips = [];
		if (step.Kind != StepKind.Gap && !IsHidden(StepKey(step.Kind)))
		{
			double from = AxisShare(step.From);
			double to = AxisShare(step.To);
			double low = Math.Min(from, to);
			double high = Math.Max(from, to);
			bool upward = to >= from;

			// A change floats, so neither of its ends stands on the baseline: both stay square, and the joins meet them flush.
			ChartBoxEnds ends = step.Kind == StepKind.Total && RoundedEnds
				? upward ? ChartBoxEnds.RoundHigh : ChartBoxEnds.RoundLow
				: ChartBoxEnds.None;
			ChartBox mark = StepMark(step, low, high, ends);
			string text = StepText(step);
			if (_labels == MokaValueLabels.Tip)
			{
				AddTip(tips, column, text, low, high, !upward, 1);
			}
			else if (_labels == MokaValueLabels.Largest && column == _largestColumn)
			{
				AddTip(tips, column, text, low, high, !upward, LoneReach);
			}
			else if (_labels == MokaValueLabels.Inside)
			{
				mark = ChartColumns.WithLabel(mark, text);
			}

			marks.Add(mark);
		}

		List<ChartLink> links = new(2);
		if (!_narrowSteps && column > 0 && Joined(column - 1))
		{
			links.Add(ChartColumns.Link(AxisShare(_stepColumns[column - 1].To), true));
		}

		if (!_narrowSteps && column < _columnCount - 1 && Joined(column))
		{
			links.Add(ChartColumns.Link(AxisShare(step.To), false));
		}

		string? style = Horizontal
			? new StyleBuilder().AddStyle("--_gh", "46%").Build()
			: new StyleBuilder().AddStyle(ChartColumns.SlotStyle(column, _columnCount)).AddStyle("--_gw", "60%").Build();
		string classes = Horizontal ? "moka-chart-col moka-chart-col--row" : "moka-chart-col";
		return new ChartColumn(column, classes, style, NativeTitles ? StepTitle(column) : null, [new ChartLane(marks, tips)], links);
	}

	private bool Joined(int column) =>
		_stepColumns[column].Kind != StepKind.Gap && _stepColumns[column + 1].Kind != StepKind.Gap;

	private ChartBox StepMark(Step step, double low, double high, ChartBoxEnds ends)
	{
		string key = StepKey(step.Kind);
		if (step.Kind != StepKind.Total)
		{
			return ChartColumns.Mark("bar", low, high, SeriesClass(step.Kind == StepKind.Increase ? 1 : 2, null, false), null, key, ends,
				$"moka-chart-hatch {DirectionClass(step.Change)}");
		}

		if (TotalsOwnColor)
		{
			(string colorClass, string? colorStyle) = TotalKeyPaint();
			return ChartColumns.Mark("bar", low, high, colorClass, colorStyle, key, ends, "moka-chart-hatch");
		}

		return ChartColumns.Mark("bar", low, high, SeriesClass(3, null, false), null, key, ends, "moka-chart-hatch moka-chart-mark--total");
	}

	private static string StepKey(StepKind kind) => kind switch
	{
		StepKind.Increase => PositiveKey,
		StepKind.Decrease => NegativeKey,
		_ => TotalKey
	};

	private static int StepLegend(StepKind kind) => kind switch
	{
		StepKind.Increase => IncreaseLegend,
		StepKind.Decrease => DecreaseLegend,
		_ => TotalLegend
	};

	// A change carries its sign, so a printed value never reads by its color alone; a total is a level and has none.
	private string StepText(Step step) => step.Kind switch
	{
		StepKind.Gap => string.Empty,
		StepKind.Total => ValueLabel(step.Change),
		_ => step.Change == 0 ? ValueLabel(0) : Signed(step.Change)
	};

	private string StepLabel(int index) => index >= _givenSteps ? TotalLabel ?? Words.Total : RawLabel(index);

	private string StepHeading(int column)
	{
		(int first, int last) = _spans[column];
		return first == last ? StepLabel(first) : Words.Between(StepLabel(first), StepLabel(last));
	}

	private string StepTitle(int column)
	{
		string heading = StepHeading(column);
		Step step = _stepColumns[column];
		if (step.Kind == StepKind.Gap)
		{
			return heading;
		}

		string line = $"{heading} · {_modernInfo[StepLegend(step.Kind)].Name} {StepText(step)}";
		return step.Kind == StepKind.Total ? line : $"{line}\n{Words.Total} {ValueLabel(step.To)}";
	}

	// A pin elsewhere says how far the running total has moved since, which reads better than a change in a change.
	private ChartActiveInfo DescribeStep(int series, int drawn, int? pinnedDrawn)
	{
		string heading = StepHeading(drawn);
		Step step = _stepColumns[drawn];
		int? pin = pinnedDrawn is { } p && p >= 0 && p < _columnCount ? p : null;
		List<ChartActiveRow> rows = [];
		List<MokaChartPoint> points = [];
		List<string> notes = [];
		if (step.Kind != StepKind.Gap)
		{
			int legend = StepLegend(step.Kind);
			ChartSeriesInfo kind = _modernInfo[legend];
			string text = StepText(step);
			double? since = pin is { } at ? step.To - _stepColumns[at].To : null;
			rows.Add(new ChartActiveRow(kind.Name, kind.ColorClass, kind.ColorStyle, text, since is { } moved ? Signed(moved) : null, false));
			points.Add(new MokaChartPoint(kind.Key, kind.Name, legend, drawn, step.Change, text)
			{
				Category = heading,
				X = _spans[drawn].First,
				Total = step.To,
				SincePin = since
			});
			if (step.Kind != StepKind.Total)
			{
				notes.Add($"{Words.Total} {ValueLabel(step.To)}");
			}
		}

		notes.AddRange(AnnotationNotes(step.Kind == StepKind.Gap ? double.NaN : step.To, drawn, 0.5));

		string cell = rows.Count == 0 ? heading : $"{heading}, {rows[0].Name} {rows[0].ValueText}";
		if (notes.Count > 0)
		{
			cell = $"{cell}, {string.Join(", ", notes)}";
		}

		MokaChartPoint? focus = series >= 0 ? points.FirstOrDefault(point => point.Series == series) : null;
		return new ChartActiveInfo(heading, rows, notes, cell, new MokaChartTooltipContext(drawn, heading, points, focus, pin, notes));
	}

	// One row a category, whatever was merged to draw it.
	private ChartTableModel StepTable() => new(
		[Words.Name, Words.StepType, Words.Change, Words.Total],
		_steps.Length,
		(row, column) => StepCell(row, column, false),
		(row, column) => StepCell(row, column, true));

	private string StepCell(int row, int column, bool raw)
	{
		if (row < 0 || row >= _steps.Length)
		{
			return string.Empty;
		}

		Step step = _steps[row];
		bool change = step.Kind is StepKind.Increase or StepKind.Decrease;
		return column switch
		{
			0 => StepLabel(row),
			1 => step.Kind == StepKind.Gap ? string.Empty : _modernInfo[StepLegend(step.Kind)].Name,
			2 when change => raw ? step.Change.ToString(CultureInfo.InvariantCulture) : StepText(step),
			3 => raw ? step.To.ToString(CultureInfo.InvariantCulture) : ValueLabel(step.To),
			_ => string.Empty
		};
	}
}
