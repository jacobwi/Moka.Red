using System.Globalization;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

public partial class MokaLineChart
{
	// Names at the lines' ends: each takes a line this tall, in its own em, and the leader runs this far.
	private const double EndLine = 1.3;
	private static readonly int[] EndHeights = [3, 4, 6, 8, 10, 12, 15, 19, 24, 30, 38, 48];

	/// <summary>A value as the value axis writes it, at its own precision rather than the tick step's.</summary>
	private string Written(ModernModel model, double value) => WriteValue(value, model.Axis);

	// A raw value of an indexed series, in the chart's own words: its Formatter, else its Unit.
	private string RawText(double value) =>
		Formatter?.Invoke(value)
		?? Unit?.Format(value)
		?? value.ToString(TickFormat, CultureInfo.CurrentCulture);

	private string Signed(ModernModel model, double change) =>
		change > 0 ? $"+{Written(model, change)}" : change < 0 ? $"-{Written(model, -change)}" : Written(model, 0);

	private IReadOnlyList<ChartSeriesInfo> SeriesInfoFor(ModernModel model)
	{
		bool values = Spec.LegendValues ?? ShowCurrent;
		ChartMark mark = Stacked ? ChartMark.Area : ChartMark.Line;
		return
		[
			.. model.Tracks.Select(track => new ChartSeriesInfo(track.Key, track.Series.Name, track.Slot, track.ColorClass,
				track.ColorStyle, mark, track.Hidden, track.Muted,
				values && !track.Hidden && LatestValue(model, track) is { } latest ? Written(model, latest) : null))
		];
	}

	// The newest value a series has, as drawn.
	private static double? LatestValue(ModernModel model, Track track)
	{
		for (int i = Math.Min(track.Count, model.End + 1) - 1; i >= 0; i--)
		{
			double value = Drawn(track, model, i);
			if (double.IsFinite(value))
			{
				return value;
			}
		}

		return null;
	}

	// The heading of a point: TooltipLabel, else its time with as much date as the shown span needs, else its label.
	private string HeadingAt(ModernModel model, int index)
	{
		if (TooltipLabel is not null)
		{
			string custom = TooltipLabel(index);
			if (!string.IsNullOrEmpty(custom))
			{
				return custom;
			}
		}

		return model.Frame is { } frame && (model.X is not null || frame.Kind == ChartXKind.Number)
			? frame.Text(model.X is null ? index : model.X[index])
			: CategoryText(index);
	}

	private string NativeTitle(ModernModel model, Track track, int index)
	{
		string heading = HeadingAt(model, index);
		string value = $"{track.Series.Name}: {Written(model, Drawn(track, model, index))}";
		return string.IsNullOrWhiteSpace(heading) ? value : $"{heading} · {value}";
	}

	private ChartActiveInfo? DescribeModern(int panel, int series, int drawn, int? pinnedDrawn)
	{
		ModernModel model = _modern;
		if (panel < 0 || panel >= model.Panels.Count)
		{
			return null;
		}

		ModernPanel shownPanel = model.Panels[panel];
		if (drawn < 0 || drawn >= shownPanel.Union.Length)
		{
			return null;
		}

		int index = shownPanel.Union[drawn];
		int? pinIndex = pinnedDrawn is { } pin && pin >= 0 && pin < shownPanel.Union.Length ? shownPanel.Union[pin] : null;
		string heading = HeadingAt(model, index);
		double? x = model.X is null ? index : model.X[index];
		Track? focusTrack = shownPanel.Only ?? (series >= 0 && series < model.Tracks.Count ? model.Tracks[series] : null);

		List<ChartActiveRow> rows = [];
		List<MokaChartPoint> points = [];
		List<string> notes = [];
		MokaChartPoint? focus = null;
		double total = 0;
		bool anyTotal = false;
		IEnumerable<Track> order = Stacked ? Enumerable.Reverse(model.Tracks) : model.Tracks;
		foreach (Track track in order)
		{
			if (track.Hidden || index >= track.Count)
			{
				continue;
			}

			double raw = track.Series.Values[index];
			double shown = Drawn(track, model, index);
			string valueText = double.IsFinite(shown) ? Written(model, shown) : Words.NoData;
			if (double.IsFinite(raw) && Stacked)
			{
				total += raw;
				anyTotal = true;
			}

			double? since = pinIndex is { } at && double.IsFinite(shown) && Drawn(track, model, at) is var then
			                && double.IsFinite(then)
				? shown - then
				: null;
			string name = IndexTo is not null && double.IsFinite(raw) ? $"{track.Series.Name}, {RawText(raw)}" : track.Series.Name;
			bool emphasis = ReferenceEquals(track, focusTrack);
			rows.Add(new ChartActiveRow(name, track.ColorClass, track.ColorStyle, valueText,
				since is { } change ? Signed(model, change) : null, emphasis));

			double? low = track.HasBand && index < track.Series.Low!.Count ? track.Series.Low[index] : null;
			double? high = track.HasBand && index < track.Series.High!.Count ? track.Series.High[index] : null;
			MokaChartPoint point = new(track.Key, track.Series.Name, track.Position, index, raw,
				double.IsFinite(raw) ? IndexTo is null ? Written(model, raw) : RawText(raw) : valueText)
			{
				Category = Labels is not null && index < Labels.Count ? Labels[index] : null,
				Time = Times is not null && index < Times.Count ? Times[index] : null,
				X = x,
				Low = low,
				High = high,
				SincePin = since
			};
			points.Add(point);
			if (emphasis)
			{
				focus = point;
			}

			if (track.HasBand && (emphasis || focusTrack is null) && low is { } l && high is { } h && double.IsFinite(l)
			    && double.IsFinite(h) && !notes.Any(note => note.StartsWith(track.Series.Name, StringComparison.Ordinal)))
			{
				string spread = Words.Spread(Written(model, Drawn(track, model, index, track.Series.Low)),
					Written(model, Drawn(track, model, index, track.Series.High)));
				notes.Add(model.Tracks.Count(t => t.HasBand && !t.Hidden) > 1 ? $"{track.Series.Name}: {spread}" : spread);
			}
		}

		if (rows.Count == 0)
		{
			return null;
		}

		if (Stacked && anyTotal)
		{
			rows.Add(new ChartActiveRow(Words.Total, "moka-linechart-total", null, Written(model, total), null, false));
			for (int p = 0; p < points.Count; p++)
			{
				points[p] = points[p] with { Total = total };
			}

			focus = focus is null ? null : focus with { Total = total };
		}

		// A gap still names the events and windows at its place.
		Track? noteTrack = focusTrack is { Hidden: false } ? focusTrack : model.Tracks.FirstOrDefault(t => !t.Hidden);
		double noted = noteTrack is null ? double.NaN : Drawn(noteTrack, model, index);
		notes.AddRange(AnnotationNotes(noted, x, HalfStep(model, index)));

		string cell = series >= 0 && focus is not null && rows.FirstOrDefault(row => row.Emphasis) is { } focused
			? $"{focused.Name}, {heading}, {focused.ValueText}"
			: $"{heading}, {string.Join(", ", rows.Select(row => $"{row.Name} {row.ValueText}"))}";
		if (notes.Count > 0)
		{
			cell = $"{cell}. {string.Join(". ", notes)}";
		}

		MokaChartTooltipContext context = new(drawn, heading, points, focus, pinnedDrawn, notes);
		return new ChartActiveInfo(heading, rows, notes, cell, context);
	}

	// Half the step between a point and its neighbours, so an event between two samples is named at the nearer one.
	private static double HalfStep(ModernModel model, int index)
	{
		if (model.X is null)
		{
			return 0.5;
		}

		int before = Math.Max(0, index - 1);
		int after = Math.Min(model.Count - 1, index + 1);
		return after > before ? (model.X[after] - model.X[before]) / (2 * (after - before)) : 0;
	}

	private ChartHoverData HoverFor(ModernPanel panel)
	{
		if (panel.Hover is { } cached)
		{
			return cached;
		}

		ModernModel model = _modern;
		MokaScale? scale = model.Scale;
		bool reverse = model.Axis?.Reverse ?? false;
		float[] x = new float[panel.Union.Length];
		for (int k = 0; k < x.Length; k++)
		{
			x[k] = (float)panel.Shares[k];
		}

		List<ReadOnlyMemory<float>> y = [];
		foreach (Track track in model.Tracks)
		{
			float[] shares = new float[panel.Union.Length];
			bool shown = !track.Hidden && (panel.Only is null || ReferenceEquals(panel.Only, track)) && scale is not null;
			bool aligned = ReferenceEquals(track.Drawn, panel.Union) && track.Tops is not null;
			for (int k = 0; k < shares.Length; k++)
			{
				if (!shown)
				{
					shares[k] = float.NaN;
					continue;
				}

				int index = panel.Union[k];
				double value = aligned ? track.Tops![k]
					: Stacked ? StackedTop(model, track, index)
					: Drawn(track, model, index);
				double share = double.IsFinite(value) ? scale!.Share(value) : double.NaN;
				shares[k] = double.IsFinite(share) ? (float)(reverse ? 1 - share : share) : float.NaN;
			}

			y.Add(shares);
		}

		ChartHoverData data = new(model.Frame?.From ?? 0, model.Frame?.To ?? 1, x, y);
		panel.Hover = data;
		return data;
	}

	// A stacked band's top at a point the stack didn't draw: its own value over every shown band below it.
	private static double StackedTop(ModernModel model, Track track, int index)
	{
		double own = Drawn(track, model, index);
		if (!double.IsFinite(own))
		{
			return double.NaN;
		}

		double sum = own;
		foreach (Track below in model.Tracks)
		{
			if (ReferenceEquals(below, track))
			{
				break;
			}

			if (!below.Hidden && Drawn(below, model, index) is var value && double.IsFinite(value))
			{
				sum += value;
			}
		}

		return sum;
	}

	// The range strip draws the first shown series behind its window, all of it, cut to about a pixel per value.
	private static IReadOnlyList<double>? OverviewFor(ModernModel model)
	{
		if (model.OverviewMade)
		{
			return model.Overview;
		}

		model.OverviewMade = true;
		Track? first = model.Tracks.FirstOrDefault(track => !track.Hidden);
		if (first is null)
		{
			return null;
		}

		IReadOnlyList<double> values = first.Series.Values;
		if (values.Count <= 2 * DefaultPlotWidth)
		{
			model.Overview = values;
			return values;
		}

		double[]? copy = null;
		int[] kept = MokaDownsampler.MinMax(SpanOf(values, ref copy), DefaultPlotWidth / 2);
		model.Overview = [.. kept.Select(i => values[i])];
		return model.Overview;
	}

	private ChartTableModel TableFor(ModernModel model)
	{
		List<string> headers = [XAxis?.Title ?? (model.X is not null ? Words.Time : Words.Name)];
		List<Func<int, (string Display, string Raw)>> cells = [];
		TimeSpan span = model.X is { Length: > 1 } x && model.Count > 1
			? TimeSpan.FromMilliseconds(Math.Max(1, x[model.Count - 1] - x[0]))
			: TimeSpan.FromDays(8);
		MokaTimeTickOptions options = TimeOptions();
		cells.Add(row => Times is not null && row < Times.Count
			? (MokaTimeTicks.TooltipText(Times[row], span, options), Times[row].ToString("o", CultureInfo.InvariantCulture))
			: (CategoryText(row), CategoryText(row)));

		foreach (Track track in model.Tracks.Where(track => !track.Hidden))
		{
			Track one = track;
			headers.Add(one.Series.Name);
			cells.Add(row => Cell(row < one.Count ? one.Series.Values[row] : double.NaN, IndexTo is null ? v => Written(model, v) : RawText));
			if (one.HasBand)
			{
				headers.Add(Words.Low(one.Series.Name));
				cells.Add(row => Cell(row < one.Series.Low!.Count ? one.Series.Low[row] : double.NaN, v => Written(model, v * one.Factor)));
				headers.Add(Words.High(one.Series.Name));
				cells.Add(row => Cell(row < one.Series.High!.Count ? one.Series.High[row] : double.NaN, v => Written(model, v * one.Factor)));
			}

			if (IndexTo is not null)
			{
				headers.Add(Words.Index(one.Series.Name));
				cells.Add(row => Cell(row < one.Count ? one.Series.Values[row] * one.Factor : double.NaN, v => Written(model, v)));
			}
		}

		if (Stacked)
		{
			headers.Add(Words.Total);
			cells.Add(row => Cell(RowTotal(model, row), v => Written(model, v)));
		}

		return new ChartTableModel(headers, model.Count,
			(row, column) => column < cells.Count ? cells[column](row).Display : string.Empty,
			(row, column) => column < cells.Count ? cells[column](row).Raw : string.Empty);
	}

	private static (string Display, string Raw) Cell(double value, Func<double, string> text) =>
		double.IsFinite(value) ? (text(value), value.ToString("R", CultureInfo.InvariantCulture)) : (string.Empty, string.Empty);

	private static double RowTotal(ModernModel model, int row)
	{
		double total = 0;
		bool any = false;
		foreach (Track track in model.Tracks)
		{
			if (!track.Hidden && row < track.Count && track.Series.Values[row] is var value && double.IsFinite(value))
			{
				total += value;
				any = true;
			}
		}

		return any ? total : double.NaN;
	}

	// Which series are named at their ends, and what each says: the look's names for up to four lines, the highlighted
	// series alone when one is highlighted, or what DirectLabels asks for on every line.
	private List<EndLabel> EndsFor(ModernModel model, List<Track> shown)
	{
		bool highlighted = !string.IsNullOrEmpty(Highlight) || HighlightSeries is { Count: > 0 };
		MokaDirectLabels labels = DirectLabels
		                          ?? (highlighted ? MokaDirectLabels.Both
			                          : shown.Count <= LookEndLabelsAtMost ? Spec.DirectLabels : MokaDirectLabels.None);
		if (labels == MokaDirectLabels.None)
		{
			return [];
		}

		List<(Track Track, double Y)> ends = [];
		foreach (Track track in shown)
		{
			if (track.Last < 0 || (DirectLabels is null && highlighted && track.Muted))
			{
				continue;
			}

			ends.Add((track, track.Ys[track.Last] / (double)Units * 100));
		}

		if (ends.Count == 0 || ends.Count > EndLabelsAtMost)
		{
			return [];
		}

		ends.Sort((a, b) => a.Y.CompareTo(b.Y));
		List<EndLabel> placed = [];
		for (int k = 0; k < ends.Count; k++)
		{
			(Track track, double y) = ends[k];
			double value = track.Tops![track.Last];
			string? name = labels is MokaDirectLabels.Names or MokaDirectLabels.Both ? track.Series.Name : null;
			string? figure = labels is MokaDirectLabels.Values or MokaDirectLabels.Both
				? IndexTo is { } index ? Change(value, index) : Written(model, value)
				: null;
			placed.Add(new EndLabel(track.Key, name, figure, y, k));
		}

		return placed;
	}

	// Growth since the start of an indexed series, which is what its end says: "+73%".
	private static string Change(double value, double index)
	{
		double percent = (value / index - 1) * 100;
		string number = Math.Abs(percent).ToString("0.#", CultureInfo.CurrentCulture);
		return percent >= 0.05 ? $"+{number}%" : percent <= -0.05 ? $"-{number}%" : $"{number}%";
	}

	// The ends' box style: each label's lowest place pushed down by the labels above it, in the box's own height, so
	// no script measures anything. --_fK is label K's place after the labels above have pushed it down.
	private static string? EndsStyle(IReadOnlyList<EndLabel> ends)
	{
		if (ends.Count == 0)
		{
			return null;
		}

		StyleBuilder style = new();
		for (int k = 0; k < ends.Count; k++)
		{
			List<string> terms = [Em(EndLine / 2 + k * EndLine)];
			for (int j = 0; j <= k; j++)
			{
				terms.Add($"calc({Cqh(ends[j].Y)} + {Em((k - j) * EndLine)})");
			}

			style.AddStyle($"--_f{Number(k)}", $"max({string.Join(", ", terms)})");
		}

		return style.Build();
	}

	// A label's own style: where it ends up once the labels below have pushed it back up, and where its line ends.
	private static string? EndStyle(IReadOnlyList<EndLabel> ends, EndLabel end)
	{
		int n = ends.Count;
		int k = end.Order;
		List<string> terms = [$"calc(100cqh - {Em(EndLine / 2 + (n - 1 - k) * EndLine)})"];
		for (int m = k; m < n; m++)
		{
			terms.Add($"calc(var(--_f{Number(m)}) - {Em((m - k) * EndLine)})");
		}

		return new StyleBuilder()
			.AddStyle("--_y", Cqh(end.Y))
			.AddStyle("--_b", $"min({string.Join(", ", terms)})")
			.Build();
	}

	// Below this height in its own em the ends' box can't hold every name, and they hide; the legend names the lines.
	private static string EndsClass(IReadOnlyList<EndLabel> ends)
	{
		double needed = ends.Count * EndLine;
		int height = EndHeights.FirstOrDefault(h => h >= needed, EndHeights[^1]);
		return new CssBuilder("moka-linechart__ends")
			.AddClass($"moka-linechart__ends--h{Number(height)}")
			.Build();
	}

	private static string EndSizer(IReadOnlyList<EndLabel> ends) =>
		ends.MaxBy(end => (end.Name?.Length ?? 0) + (end.Value?.Length ?? 0) + (end.Name is not null && end.Value is not null ? 1 : 0)) is { } longest
			? string.Join(' ', new[] { longest.Name, longest.Value }.Where(part => part is not null))
			: string.Empty;

	private static string Em(double value) => $"{value.ToString("0.###", CultureInfo.InvariantCulture)}em";

	private static string Cqh(double value) => $"{value.ToString("0.###", CultureInfo.InvariantCulture)}cqh";

	private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

	/// <summary>A name or value beside a line's end.</summary>
	/// <param name="Key">The series' key.</param>
	/// <param name="Name">Its name, when names are written.</param>
	/// <param name="Value">Its latest value, when values are written.</param>
	/// <param name="Y">Where its line ends, as a percentage of the plot's height from the top.</param>
	/// <param name="Order">Its place among the labels, top first.</param>
	private sealed record EndLabel(string Key, string? Name, string? Value, double Y, int Order);
}
