using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

public partial class MokaLineChart
{
	// The marks are drawn in a square of this many units each way, stretched over the plot box. Whole units are a
	// ten-thousandth of the plot, finer than a pixel on any screen, and write as short integers.
	private const int Units = 10_000;

	// Downsampling starts past twice the plot's width in points; without a width from the script the plot counts as
	// this wide, and the script measures it once a series is longer than MeasureAbove.
	private const int DefaultPlotWidth = 800;
	private const int MeasureAbove = 1600;

	// Past this many drawn values a line gets dots only where the pointer is: 400 rings already cover a 600 px line.
	private const int DotsAtMost = 400;

	// Names at the lines' ends: how many at most, and how many a look names on its own.
	private const int EndLabelsAtMost = 8;
	private const int LookEndLabelsAtMost = 4;

	private static readonly MokaChartUnit PlainUnit = MokaChartUnit.Of(string.Empty, string.Empty);

	private ModernModel _modern = ModernModel.Empty;
	private int _count;
	private double[]? _x;
	private IReadOnlyList<DateTimeOffset>? _xSource;
	private long? _xVersion;

	/// <inheritdoc />
	internal override MokaChartAxis? ValueAxisOptions => _classic ? YAxis : _modern.Axis;

	/// <inheritdoc />
	/// <remarks>
	///     Past twice a plot's width of values with no labels and no times, the x axis counts the values like numbers
	///     rather than laying out a label for each of thousands of categories.
	/// </remarks>
	internal override (double Min, double Max)? NumericXRange(int panel) =>
		!_classic && Labels is null && Times is null && _count > MeasureAbove ? (0, _count - 1) : null;

	/// <inheritdoc />
	internal override MokaChartZoom ZoomGestures => Zoom ?? MokaChartZoom.None;

	/// <inheritdoc />
	internal override bool LiveHold => Live && PauseWhilePointing;

	/// <inheritdoc />
	internal override bool MeasuresWidth => !_classic && _modern.Longest > MeasureAbove;

	/// <inheritdoc />
	internal override int? KeyPageStep => _modern.PageStep;

	/// <inheritdoc />
	internal override int DrawnCount(int panel) =>
		panel >= 0 && panel < _modern.Panels.Count ? _modern.Panels[panel].Union.Length : 0;

	/// <inheritdoc />
	internal override int LatestDrawn(int panel) =>
		panel >= 0 && panel < _modern.Panels.Count ? _modern.Panels[panel].Latest : -1;

	/// <inheritdoc />
	/// <remarks>A time axis pinned past its data by <see cref="MokaChartAxis.Min" /> or <see cref="MokaChartAxis.Max" /> zooms out to the pinned ends.</remarks>
	internal override (double From, double To)? XExtent(int panel) =>
		_modern.Count > 1 && _modern.XLast > _modern.XFirst
			? _modern.X is not null && TimeAxisSpan() is { } axis ? axis : (_modern.XFirst, _modern.XLast)
			: null;

	/// <inheritdoc />
	internal override ChartHoverData? HoverData(int panel) =>
		_classic || panel < 0 || panel >= _modern.Panels.Count ? null : HoverFor(_modern.Panels[panel]);

	/// <inheritdoc />
	internal override IReadOnlyList<double>? OverviewValues => _classic ? null : OverviewFor(_modern);

	/// <inheritdoc />
	internal override ChartTableModel? TableModel => _classic ? null : _modern.Table ??= TableFor(_modern);

	private readonly List<RenderFragment<ChartActiveState>> _panelValues = [];

	private IReadOnlyList<ChartSeriesInfo> ModernSeriesInfo => _modern.SeriesInfo;

	// The readout strip over the plot, when the tooltip is the readout.
	private RenderFragment? ReadoutStrip => SurfaceMode == MokaChartTooltip.Readout ? ReadoutContent(0) : null;

	// Panels side by side up to SplitColumns, none narrower than 16em, so a phone shows one column.
	private string? PanelsStyle => new StyleBuilder()
		.AddStyle("--_columns", SplitColumns is > 0 and var columns ? columns.ToString(CultureInfo.InvariantCulture) : null)
		.Build();

	private static string? PanelKey(ModernPanel panel, int position) =>
		panel.Only?.Key ?? position.ToString(CultureInfo.InvariantCulture);

	private string? SeriesKey(Track track) => _modern.SharedKeys.Contains(track.Key) ? null : track.Key;

	private string? EndKey(EndLabel end) => _modern.SharedKeys.Contains(end.Key) ? null : end.Key;

	private static string SeriesGroupClass(Track track) => new CssBuilder("moka-linechart__series")
		.AddClass(track.ColorClass)
		.AddClass("moka-linechart__series--muted", track.Muted)
		.Build();

	private string AreaClass(bool fade) => new CssBuilder("moka-linechart__area")
		.AddClass(fade ? "moka-linechart__area--fade" : "moka-linechart__area--flat")
		.AddClass("moka-linechart__area--stacked", Stacked)
		.Build();

	// The series a panel draws, muted ones first so the rest sit on top of them.
	private IEnumerable<Track> DrawOrder(int panel)
	{
		if (panel < 0 || panel >= _modern.Panels.Count)
		{
			return [];
		}

		if (_modern.Panels[panel].Only is { } only)
		{
			return [only];
		}

		return _modern.Tracks.Where(track => !track.Hidden).OrderBy(track => track.Muted ? 0 : 1);
	}

	// The design every chart parameter can ask for. A Standard look with none of them keeps today's markup.
	private bool ClassicNow()
	{
		if (ResolvedLook != MokaChartLook.Standard
		    || Tooltip is not (null or MokaChartTooltip.Native)
		    || Keyboard == true || Pinnable == true || !string.IsNullOrEmpty(SyncGroup)
		    || Times is not null || XAxis is not null || YAxis is not null || Unit is not null || LabelFit is not null
		    || Grid is not null || TickLabels is not null || ValueSide is not null
		    || Annotations is { Count: > 0 } || TargetLow is not null || TargetHigh is not null
		    || Range is not null || CurrentRange is not null || Zoom is not null || RangePresets is { Count: > 0 }
		    || RangePreset is not null
		    || Stacked || Split || IndexTo is not null || DirectLabels is not null || BreakGapsOver is not null
		    || PauseWhilePointing
		    || !string.IsNullOrEmpty(Highlight) || HighlightSeries is { Count: > 0 }
		    || Points is not null || Area is not null || Curve is MokaLineCurve.Straight or MokaLineCurve.Step
		    || Title is not null || Subtitle is not null || TitleContent is not null || Actions is not null
		    || Controls is not null || Legend is not null || LegendKey is not null || LegendActions is not null
		    || HiddenSeries is not null || State != MokaChartState.Auto || View != MokaChartView.Chart
		    || Colors is { Count: > 0 } || Texture == true || Downsample != MokaDownsample.Auto
		    || TooltipTemplate is not null || ListensForPoints)
		{
			return false;
		}

		foreach (MokaLineSeries series in Series ?? [])
		{
			if (series.Low is not null || series.High is not null || series.Slot is not null || series.ColorValue is not null
			    || series.Curve is not null)
			{
				return false;
			}
		}

		return true;
	}

	private void BuildModern(MokaLineSeries[] previous)
	{
		if (Holding && LiveHold && _modern.Built && _valuesChanged)
		{
			CountWaiting(Arrived(previous));
			return;
		}

		ModernModel model = new() { Built = true };
		List<Track> tracks = [];
		int longest = 0;
		for (int position = 0; position < _seriesSnapshot.Length; position++)
		{
			MokaLineSeries series = _seriesSnapshot[position];
			if (series.Values.Count == 0)
			{
				continue;
			}

			string key = series.Key ?? series.Name;
			int slot = SlotFor(position, series.Slot);
			bool muted = IsMuted(key);
			tracks.Add(new Track(series, key, tracks.Count, slot, SeriesClass(slot, series.Color, muted),
				muted ? null : SeriesStyle(slot, series.ColorValue), IsHidden(key), muted,
				series.Curve ?? Curve ?? (Smooth ? MokaLineCurve.Smooth : Spec.Curve)));
			longest = Math.Max(longest, series.Values.Count);
		}

		int count = longest;
		double[]? x = null;
		if (Times is { Count: > 0 } times)
		{
			x = TimesAsX(times);
			count = Math.Min(count, x.Length);
		}

		_count = count;
		model.SharedKeys = SharedKeysOf(tracks);
		model.Count = count;
		model.Longest = longest;
		model.Tracks = tracks;
		model.X = x;
		model.XFirst = x is null ? 0 : count > 0 ? x[0] : 0;
		model.XLast = x is null ? Math.Max(0, count - 1) : count > 0 ? x[count - 1] : 0;
		model.Points = Points ?? (ShowPoints ? Spec.Points : MokaChartPoints.None);
		model.Area = Area ?? (Spec.AreaAlways || ShowArea || Stacked ? Spec.Area : MokaChartArea.None);

		(int start, int end) = x is not null ? XWindow(x.AsSpan(0, count)) : CategoryWindow(count);
		model.Start = start;
		model.End = end;
		model.Frame = CreateXFrame();

		List<Track> shown = [.. tracks.Where(track => !track.Hidden)];
		PrepareValues(model, shown);
		PickDrawn(model, shown);
		model.Axis = EffectiveAxis(model);
		model.Scale = ValueScale(model.Axis, model.ValueMin, model.ValueMax);
		Place(model, shown);
		model.Panels = PanelsFor(model, shown);
		PanelCount = Math.Max(1, model.Panels.Count);
		model.SeriesInfo = SeriesInfoFor(model);
		model.Ends = Split ? [] : EndsFor(model, shown);
		model.PeakText = ShowPeak && model.HasValues ? Written(model, model.Peak) : null;
		model.PageStep = PageStepFor(model);
		_modern = model;
		SetModernSlide(previous, model);
	}

	// Keys two series share can't key their groups: Blazor throws on a repeated key.
	private static HashSet<string> SharedKeysOf(List<Track> tracks)
	{
		HashSet<string> seen = new(StringComparer.Ordinal);
		HashSet<string> shared = new(StringComparer.Ordinal);
		foreach (Track track in tracks)
		{
			if (!seen.Add(track.Key))
			{
				shared.Add(track.Key);
			}
		}

		return shared;
	}

	// What waits while a live chart holds still: the samples that came in, or one window's move.
	private int Arrived(MokaLineSeries[] previous)
	{
		int before = previous.Length > 0 ? previous.Max(s => s.Values.Count) : 0;
		int after = _seriesSnapshot.Length > 0 ? _seriesSnapshot.Max(s => s.Values.Count) : 0;
		return after > before ? after - before : 1;
	}

	// The times as Unix milliseconds, worked out again only when they changed.
	private double[] TimesAsX(IReadOnlyList<DateTimeOffset> times)
	{
		bool same = DataVersion is { } version
			? ReferenceEquals(times, _xSource) && _xVersion == version && _x?.Length == times.Count
			: _x is not null && _xSource is not null && _x.Length == times.Count && ReferenceEquals(_xSource, _timesSnapshot);
		if (same)
		{
			return _x!;
		}

		_x = TimeValues();
		_xSource = DataVersion is null ? _timesSnapshot : times;
		_xVersion = DataVersion;
		return _x;
	}

	// Indexes, stacks and gaps, in the units drawn: what each line's value is at each point it may draw.
	private void PrepareValues(ModernModel model, List<Track> shown)
	{
		int count = model.Count;
		List<(double From, double To)> windows = BreakingWindows();
		double gap = BreakGapsOver is { } over && model.X is not null ? over.TotalMilliseconds : double.NaN;
		model.Breaks = windows.Count > 0 || double.IsFinite(gap)
			? BreakMarks(model, windows, gap)
			: null;

		foreach (Track track in model.Tracks)
		{
			track.Factor = IndexTo is { } index && double.IsFinite(index) ? IndexFactor(track.Series.Values, index) : 1;
			track.HasBand = !Stacked && track.Series.Low is { Count: > 0 } && track.Series.High is { Count: > 0 };
			track.Count = Math.Min(count, track.Series.Values.Count);
		}

		model.HasValues = shown.Any(track => HasFinite(track, model));
	}

	private static bool HasFinite(Track track, ModernModel model)
	{
		for (int i = model.Start; i <= model.End && i < track.Count; i++)
		{
			if (double.IsFinite(track.Series.Values[i]))
			{
				return true;
			}
		}

		return false;
	}

	private List<(double From, double To)> BreakingWindows()
	{
		List<(double From, double To)> windows = [];
		foreach (MokaChartAnnotation annotation in Annotations ?? [])
		{
			if (annotation is MokaChartWindow { BreakLine: true } window)
			{
				windows.Add((Math.Min(window.From.Value, window.To.Value), Math.Max(window.From.Value, window.To.Value)));
			}
		}

		return windows;
	}

	// For each point, whether the line may not reach it from the one before: it sits in a window that breaks the line,
	// or the time since the last sample is longer than BreakGapsOver. 1 is "is a gap", 2 is "breaks before".
	private static byte[] BreakMarks(ModernModel model, List<(double From, double To)> windows, double gap)
	{
		byte[] marks = new byte[model.Count];
		for (int i = model.Start; i <= model.End; i++)
		{
			double at = model.X is null ? i : model.X[i];
			foreach ((double from, double to) in windows)
			{
				if (at >= from && at <= to)
				{
					marks[i] = 1;
					break;
				}
			}

			if (marks[i] == 0 && double.IsFinite(gap) && i > 0 && model.X is not null && model.X[i] - model.X[i - 1] > gap)
			{
				marks[i] = 2;
			}
		}

		return marks;
	}

	private static double IndexFactor(IReadOnlyList<double> values, double index)
	{
		foreach (double value in values)
		{
			if (double.IsFinite(value))
			{
				return value != 0 ? index / value : double.NaN;
			}
		}

		return double.NaN;
	}

	// A raw value as drawn: indexed, or a gap where a window breaks the line.
	private static double Drawn(Track track, ModernModel model, int index, IReadOnlyList<double>? values = null)
	{
		values ??= track.Series.Values;
		if (index < 0 || index >= track.Count || index >= values.Count || model.Breaks is { } marks && marks[index] == 1)
		{
			return double.NaN;
		}

		double value = values[index];
		return track.Factor == 1 ? value : value * track.Factor;
	}

	// Which points each line draws: everything in the window, or, past twice the plot's width, the lowest and highest
	// value of each pixel (or the points that keep its shape, with Lttb). A stacked chart draws every band at the same
	// points, so the bands meet.
	private void PickDrawn(ModernModel model, List<Track> shown)
	{
		int start = model.Start;
		int length = model.End - start + 1;
		int buckets = Math.Max(1, PlotWidthBucket ?? DefaultPlotWidth);
		bool reduce = length > 2 * buckets && Downsample != MokaDownsample.None;
		model.Reduced = reduce;

		foreach (Track track in model.Tracks)
		{
			int own = Math.Max(0, Math.Min(track.Count, model.End + 1) - start);
			if (own <= 0 || track.Hidden)
			{
				track.Drawn = [];
				continue;
			}

			if (!reduce || own <= 2 * buckets)
			{
				track.Drawn = Enumerable.Range(start, own).ToArray();
				continue;
			}

			List<int[]> picks = [Pick(track.Series.Values, model.X, start, own, buckets)];
			if (track.HasBand)
			{
				picks.Add(Pick(track.Series.Low!, model.X, start, Math.Min(own, track.Series.Low!.Count - start), buckets));
				picks.Add(Pick(track.Series.High!, model.X, start, Math.Min(own, track.Series.High!.Count - start), buckets));
			}

			track.Drawn = picks.Count == 1 ? picks[0] : MokaDownsampler.Union(picks);
		}

		if (Stacked)
		{
			int[] union = MokaDownsampler.Union([.. shown.Select(track => track.Drawn)]);
			foreach (Track track in shown)
			{
				track.Drawn = union;
			}
		}

		Measure(model, shown);
	}

	private int[] Pick(IReadOnlyList<double> values, double[]? x, int start, int length, int buckets)
	{
		if (length <= 0)
		{
			return [];
		}

		double[]? copy = null;
		ReadOnlySpan<double> all = SpanOf(values, ref copy);
		ReadOnlySpan<double> window = all.Slice(start, Math.Min(length, all.Length - start));
		int[] picked = Downsample == MokaDownsample.Lttb
			? x is null ? MokaDownsampler.Lttb(window, 2 * buckets) : MokaDownsampler.Lttb(x.AsSpan(start, window.Length), window, 2 * buckets)
			: x is null ? MokaDownsampler.MinMax(window, buckets) : MokaDownsampler.MinMax(x.AsSpan(start, window.Length), window, buckets);
		for (int i = 0; i < picked.Length; i++)
		{
			picked[i] += start;
		}

		return picked;
	}

	// Arrays and lists are read where they are; anything else is copied once.
	private static ReadOnlySpan<double> SpanOf(IReadOnlyList<double> values, ref double[]? copy) => values switch
	{
		double[] array => array,
		List<double> list => CollectionsMarshal.AsSpan(list),
		_ => copy ??= [.. values]
	};

	// The values each shown line draws at its points, the stacks, the bands, and the range the value axis has to reach.
	private void Measure(ModernModel model, List<Track> shown)
	{
		double min = double.NaN;
		double max = double.NaN;
		double peak = double.NaN;
		double[]? below = null;
		foreach (Track track in model.Tracks)
		{
			int length = track.Drawn.Length;
			track.Tops = new double[length];
			track.Bases = null;
			track.Lows = null;
			track.Highs = null;
			bool shownTrack = !track.Hidden;
			if (Stacked && shownTrack)
			{
				track.Bases = new double[length];
				below ??= new double[length];
			}

			if (track.HasBand && shownTrack)
			{
				track.Lows = new double[length];
				track.Highs = new double[length];
			}

			for (int k = 0; k < length; k++)
			{
				int index = track.Drawn[k];
				double value = Drawn(track, model, index);
				if (track.Bases is not null && below is not null)
				{
					double under = below[k];
					track.Bases[k] = under;
					double top = double.IsFinite(value) ? under + value : double.NaN;
					below[k] = double.IsFinite(value) ? under + value : under;
					track.Tops[k] = top;
					Reach(ref min, ref max, under);
				}
				else
				{
					track.Tops[k] = value;
				}

				if (track.Lows is not null && track.Highs is not null)
				{
					track.Lows[k] = Drawn(track, model, index, track.Series.Low);
					track.Highs[k] = Drawn(track, model, index, track.Series.High);
					if (shownTrack)
					{
						Reach(ref min, ref max, track.Lows[k]);
						Reach(ref min, ref max, track.Highs[k]);
					}
				}

				if (shownTrack && double.IsFinite(track.Tops[k]))
				{
					Reach(ref min, ref max, track.Tops[k]);
					peak = double.IsNaN(peak) ? track.Tops[k] : Math.Max(peak, track.Tops[k]);
				}
			}

			track.BreakBefore = BreaksBetween(track, model);
		}

		model.ValueMin = min;
		model.ValueMax = max;
		model.Peak = peak;
	}

	private static void Reach(ref double min, ref double max, double value)
	{
		if (!double.IsFinite(value))
		{
			return;
		}

		min = double.IsNaN(min) ? value : Math.Min(min, value);
		max = double.IsNaN(max) ? value : Math.Max(max, value);
	}

	// Whether the line breaks between each drawn point and the one before it: a gap or a break among the values it
	// skipped, so a reduced line still breaks where the data does.
	private static bool[]? BreaksBetween(Track track, ModernModel model)
	{
		int[] drawn = track.Drawn;
		if (drawn.Length < 2)
		{
			return null;
		}

		bool reduced = drawn[^1] - drawn[0] + 1 > drawn.Length;
		if (!reduced && model.Breaks is null)
		{
			return null;
		}

		bool[]? breaks = null;
		IReadOnlyList<double> values = track.Series.Values;
		for (int k = 1; k < drawn.Length; k++)
		{
			bool broken = false;
			for (int i = drawn[k - 1] + 1; i <= drawn[k] && !broken; i++)
			{
				byte mark = model.Breaks is { } marks ? marks[i] : (byte)0;
				broken = mark == 2 || (i < drawn[k] && (mark == 1 || i >= values.Count || !double.IsFinite(values[i])));
			}

			if (broken)
			{
				breaks ??= new bool[drawn.Length];
				breaks[k] = true;
			}
		}

		return breaks;
	}

	// The value axis's options with the chart's own parameters folded in: AutoScale rounds the ends, SuggestedMax and
	// ScaleUnits as they always did, and an indexed chart counts in plain numbers from just under its lowest value.
	private MokaChartAxis? EffectiveAxis(ModernModel model)
	{
		MokaChartAxis? axis = YAxis;
		if (AutoScale && axis?.Nice is null)
		{
			axis = (axis ?? new MokaChartAxis()) with { Nice = true };
		}

		if (SuggestedMax is { } suggested && axis?.SuggestedMax is null)
		{
			axis = (axis ?? new MokaChartAxis()) with { SuggestedMax = suggested };
		}

		if (ScaleUnits is { Count: > 0 } units && Formatter is not null && Unit is null && axis?.Unit is null
		    && BareUnit(units) is { } bare)
		{
			axis = (axis ?? new MokaChartAxis()) with { Unit = bare };
		}

		if (IndexTo is not null)
		{
			MokaChartAxis indexed = axis ?? new MokaChartAxis();
			double? floor = indexed.Min ?? Min;
			if (floor is null && double.IsFinite(model.ValueMin) && indexed.Scale != MokaAxisScale.Log)
			{
				MokaScale lower = MokaNiceScale.Linear(model.ValueMin, model.ValueMax, new MokaScaleOptions
				{
					IncludeZero = false,
					TickCount = Math.Max(1, indexed.TickCount ?? YTicks),
					Unit = PlainUnit
				});
				floor = lower.Min;
			}

			axis = indexed with
			{
				Min = floor,
				Unit = indexed.Unit ?? PlainUnit,
				Formatter = indexed.Formatter ?? IndexText,
				Nice = indexed.Nice ?? true
			};
		}

		return axis;
	}

	// Units the old ScaleUnits named as bare factors: the steps the axis rounds in, while the Formatter writes them.
	private static MokaChartUnit? BareUnit(IReadOnlyList<double> units)
	{
		List<double> factors = [.. units.Where(u => double.IsFinite(u) && u > 0).Distinct().Order()];
		if (factors.Count == 0)
		{
			return null;
		}

		if (factors[0] != 1)
		{
			factors.Insert(0, 1);
		}

		return new MokaChartUnit { Steps = [.. factors.Select(f => new MokaUnitStep(f, string.Empty))], Separator = string.Empty };
	}

	private static string IndexText(double value) => value.ToString("#,##0.##", CultureInfo.CurrentCulture);

	// Where each drawn point sits in the marks' square, and the paths through them.
	private void Place(ModernModel model, List<Track> shown)
	{
		MokaScale scale = model.Scale!;
		bool reverse = model.Axis?.Reverse ?? false;
		double baseline = scale.Kind != MokaScaleKind.Log && scale.Min <= 0 && scale.Max >= 0 ? 0 : scale.Min;
		model.BaselineY = YUnits(scale, baseline, reverse);
		foreach (Track track in model.Tracks)
		{
			int length = track.Drawn.Length;
			track.Xs = new int[length];
			track.Ys = new int[length];
			track.BaseYs = track.Bases is null ? null : new int[length];
			track.LowYs = track.Lows is null ? null : new int[length];
			track.HighYs = track.Highs is null ? null : new int[length];
			if (track.Hidden)
			{
				continue;
			}

			for (int k = 0; k < length; k++)
			{
				int index = track.Drawn[k];
				track.Xs[k] = XUnits(model, index);
				track.Ys[k] = YUnits(scale, track.Tops![k], reverse);
				if (track.BaseYs is not null)
				{
					track.BaseYs[k] = YUnits(scale, track.Bases![k], reverse);
				}

				if (track.LowYs is not null && track.HighYs is not null)
				{
					track.LowYs[k] = YUnits(scale, track.Lows![k], reverse);
					track.HighYs[k] = YUnits(scale, track.Highs![k], reverse);
				}
			}

			track.Last = LastInside(track);
		}

		foreach (Track track in shown)
		{
			track.Paths = PathsFor(model, track);
		}
	}

	private static int XUnits(ModernModel model, int index)
	{
		double share = model.Frame!.Share(model.X is null ? index : model.X[index]);
		return double.IsFinite(share) ? (int)Math.Round(Math.Clamp(share, -1000, 1001) * Units) : int.MinValue;
	}

	// A value's height in the marks' square, from the top; int.MinValue for a gap or a value the axis can't show.
	private static int YUnits(MokaScale scale, double value, bool reverse)
	{
		if (!double.IsFinite(value))
		{
			return int.MinValue;
		}

		double share = scale.Share(value);
		if (!double.IsFinite(share))
		{
			return int.MinValue;
		}

		share = Math.Clamp(reverse ? 1 - share : share, 0, 1);
		return (int)Math.Round((1 - share) * Units);
	}

	// The last drawn value inside the plot, for the end dot and the name beside it.
	private static int LastInside(Track track)
	{
		for (int k = track.Drawn.Length - 1; k >= 0; k--)
		{
			if (track.Ys[k] != int.MinValue && track.Xs[k] != int.MinValue && track.Xs[k] >= -1 && track.Xs[k] <= Units + 1)
			{
				return k;
			}
		}

		return -1;
	}

	private List<ModernPanel> PanelsFor(ModernModel model, List<Track> shown)
	{
		if (Split && shown.Count > 0)
		{
			return [.. shown.Select(track => PanelOf(model, [track], track))];
		}

		return [PanelOf(model, shown, null)];
	}

	private static ModernPanel PanelOf(ModernModel model, List<Track> tracks, Track? only)
	{
		int[] union = tracks.Count switch
		{
			0 => [],
			1 => tracks[0].Drawn,
			_ => MokaDownsampler.Union([.. tracks.Select(track => track.Drawn)])
		};
		double[] shares = new double[union.Length];
		int latest = -1;
		for (int k = 0; k < union.Length; k++)
		{
			shares[k] = model.Frame!.Share(model.X is null ? union[k] : model.X[union[k]]);
			if (shares[k] >= -1e-6 && shares[k] <= 1 + 1e-6)
			{
				latest = k;
			}
		}

		return new ModernPanel(union, shares, latest, only);
	}

	// Page Up and Page Down step about a major tick on a time axis: the axis's own step at a middling width.
	private static int? PageStepFor(ModernModel model)
	{
		if (model.X is null || model.Panels.Count == 0 || model.Panels[0].Union.Length < 20 || model.End <= model.Start)
		{
			return null;
		}

		double span = model.X[model.End] - model.X[model.Start];
		if (!(span > 0))
		{
			return null;
		}

		TimeSpan major = MokaTimeTicks.StepFor(TimeSpan.FromMilliseconds(span), 40);
		int steps = (int)Math.Round(model.Panels[0].Union.Length * major.TotalMilliseconds / span);
		return Math.Clamp(steps, 1, model.Panels[0].Union.Length);
	}

	// A live window slides one step left over the interval when it kept its length, as the classic chart does.
	private void SetModernSlide(MokaLineSeries[] previous, ModernModel model)
	{
		bool slide = Live && _built && _valuesChanged && model.Count > 1 && CurrentRange is null
		             && previous.Length > 0 && previous.Max(s => s.Values.Count) == model.Longest;
		if (!slide)
		{
			_slideClass = null;
			_slideStyle = null;
			return;
		}

		double step = model.X is null
			? (double)Units / (model.Count - 1)
			: (model.Frame!.Share(model.X[model.Count - 1]) - model.Frame.Share(model.X[model.Count - 2])) * Units;
		if (!double.IsFinite(step) || step <= 0)
		{
			_slideClass = null;
			_slideStyle = null;
			return;
		}

		_slideClass = _slideClass == "moka-linechart-slide--a" ? "moka-linechart-slide--b" : "moka-linechart-slide--a";
		double milliseconds = Math.Max(0, LiveInterval.TotalMilliseconds);
		_slideStyle = new StyleBuilder()
			.AddStyle("--_slide-shift", $"{step.ToString("0.##", CultureInfo.InvariantCulture)}px")
			.AddStyle("--_slide-time", $"{milliseconds.ToString("0", CultureInfo.InvariantCulture)}ms")
			.Build();
	}

	/// <summary>One series as the modern chart draws it.</summary>
	private sealed class Track(
		MokaLineSeries series,
		string key,
		int position,
		int slot,
		string colorClass,
		string? colorStyle,
		bool hidden,
		bool muted,
		MokaLineCurve curve)
	{
		public MokaLineSeries Series { get; } = series;

		public string Key { get; } = key;

		public int Position { get; } = position;

		public int Slot { get; } = slot;

		public string ColorClass { get; } = colorClass;

		public string? ColorStyle { get; } = colorStyle;

		public bool Hidden { get; } = hidden;

		public bool Muted { get; } = muted;

		public MokaLineCurve Curve { get; } = curve;

		public double Factor { get; set; } = 1;

		public bool HasBand { get; set; }

		public int Count { get; set; }

		public int[] Drawn { get; set; } = [];

		public double[]? Tops { get; set; }

		public double[]? Bases { get; set; }

		public double[]? Lows { get; set; }

		public double[]? Highs { get; set; }

		public bool[]? BreakBefore { get; set; }

		public int[] Xs { get; set; } = [];

		public int[] Ys { get; set; } = [];

		public int[]? BaseYs { get; set; }

		public int[]? LowYs { get; set; }

		public int[]? HighYs { get; set; }

		public int Last { get; set; } = -1;

		public TrackPaths Paths { get; set; } = TrackPaths.None;
	}

	/// <summary>A panel: the points its surface steps through, where they sit across, and the series it shows alone.</summary>
	private sealed record ModernPanel(int[] Union, double[] Shares, int Latest, Track? Only)
	{
		public ChartHoverData? Hover { get; set; }
	}

	/// <summary>Everything one modern build worked out, kept until the next.</summary>
	private sealed class ModernModel
	{
		public static ModernModel Empty { get; } = new();

		public bool Built { get; init; }

		public int Count { get; set; }

		public int Longest { get; set; }

		public double[]? X { get; set; }

		public double XFirst { get; set; }

		public double XLast { get; set; }

		public int Start { get; set; }

		public int End { get; set; } = -1;

		public ChartXFrame? Frame { get; set; }

		public byte[]? Breaks { get; set; }

		public bool Reduced { get; set; }

		public List<Track> Tracks { get; set; } = [];

		public MokaChartPoints Points { get; set; } = MokaChartPoints.Always;

		public MokaChartArea Area { get; set; }

		public bool HasValues { get; set; }

		public double ValueMin { get; set; } = double.NaN;

		public double ValueMax { get; set; } = double.NaN;

		public double Peak { get; set; } = double.NaN;

		public string? PeakText { get; set; }

		public MokaChartAxis? Axis { get; set; }

		public MokaScale? Scale { get; set; }

		public int BaselineY { get; set; } = Units;

		public List<ModernPanel> Panels { get; set; } = [];

		public IReadOnlyList<ChartSeriesInfo> SeriesInfo { get; set; } = [];

		public IReadOnlyList<EndLabel> Ends { get; set; } = [];

		public HashSet<string> SharedKeys { get; set; } = new(StringComparer.Ordinal);

		public int? PageStep { get; set; }

		public ChartTableModel? Table { get; set; }

		public IReadOnlyList<double>? Overview { get; set; }

		public bool OverviewMade { get; set; }
	}
}
