using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     A candlestick chart: where a price opened and closed in each period, and how high and low it went, for prices and
///     rates. Up periods are hollow and down periods filled, so the shape reads without color.
/// </summary>
/// <remarks>
///     A candle is HTML, a hairline wick behind a body. When there are more periods than the plot has room for at three
///     pixels each, neighbours merge: the first open, the highest high, the lowest low, the last close. The value axis
///     doesn't reach down to zero.
/// </remarks>
public partial class MokaCandlestickChart : MokaCartesianChartBase, IChartColumns
{
	private const string UpKey = "up";
	private const string DownKey = "down";
	private const int MeasureAbove = 100;
	private const double PixelsPerCandle = 3;
	private const double DefaultWidth = 800;
	private const double Pad = 0.04;
	private const double VolumeShare = 0.2;
	private const int MostDecimals = 6;

	private MokaCandle[]? _snapshot;
	private List<MokaCandle> _candles = [];
	private List<Period?> _periods = [];
	private int _slots;
	private TimeSpan _step;
	private int _decimals;
	private double _volumeTop;
	private (double Min, double Max) _valueRange = (double.NaN, double.NaN);
	private MokaScale? _scale;
	private ChartColumnsModel? _model;
	private List<ChartSeriesInfo> _info = [];

	/// <summary>The periods, in time order.</summary>
	[Parameter]
	public IReadOnlyList<MokaCandle>? Candles { get; set; }

	/// <summary>
	///     Whether periods without a candle, such as weekends, take no room. Defaults to true. False keeps the time between
	///     candles: a missing period is an empty slot.
	/// </summary>
	[Parameter]
	public bool Gapless { get; set; } = true;

	/// <summary>Whether thin bars along the bottom fifth of the plot show how much traded in each period.</summary>
	[Parameter]
	public bool ShowVolume { get; set; }

	/// <summary>The words the chart writes beyond the shared ones. Null is English.</summary>
	[Parameter]
	public MokaCandleText? CandleText { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-candlestick";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-candlestick--{SizeToKebab(Size)}")
		.AddClass(ModernRootClasses)
		.AddClass(Class)
		.Build();

	/// <summary>A drawn candle: one period, or several merged.</summary>
	private sealed record Period(DateTimeOffset From, DateTimeOffset To, double Open, double High, double Low, double Close,
		double? Volume, int Count)
	{
		public bool Up => Close >= Open;
	}

	/// <inheritdoc />
	internal override string DefaultLabel => "Candlestick chart";

	/// <inheritdoc />
	internal override bool HasData => _candles.Count > 0;

	/// <inheritdoc />
	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => _info;

	/// <inheritdoc />
	internal override MokaChartTooltip AutoTooltip => MokaChartTooltip.Column;

	/// <inheritdoc />
	internal override bool MeasuresWidth => _slots > MeasureAbove;

	/// <inheritdoc />
	internal override MokaChartRange? CurrentRange => null;

	/// <inheritdoc />
	internal override bool ValueAxisReachesZero => false;

	/// <inheritdoc />
	/// <remarks>Prices need round steps written with every decimal they have, whatever the look.</remarks>
	internal override bool NiceValueAxis => true;

	/// <inheritdoc />
	internal override int CategoryCount => _periods.Count;

	/// <inheritdoc />
	internal override ChartTableModel? TableModel
	{
		get
		{
			List<string> headers = [CandleWords.Time, CandleWords.Open, CandleWords.High, CandleWords.Low, CandleWords.Close];
			if (ShowVolume)
			{
				headers.Add(CandleWords.Volume);
			}

			return new ChartTableModel(headers, _candles.Count, (row, column) => TableCell(row, column, false),
				(row, column) => TableCell(row, column, true));
		}
	}

	private MokaCandleText CandleWords => CandleText ?? MokaCandleText.Default;

	/// <inheritdoc />
	internal override (double Min, double Max) ValueRange(int panel) => _valueRange;

	/// <inheritdoc />
	internal override double CategoryShare(int index, int count) => (index + 0.5) / Math.Max(1, count);

	/// <inheritdoc />
	internal override string CategoryText(int index) =>
		index >= 0 && index < _periods.Count && _periods[index] is { } period ? Short(period.From) : string.Empty;

	/// <inheritdoc />
	internal override int DrawnCount(int panel) => _periods.Count;

	/// <inheritdoc />
	ChartColumnsModel? IChartColumns.Columns(int panel) => _model;

	/// <inheritdoc />
	internal override bool SameValue(string name, object? before, object? after) => name == nameof(Candles)
		? ChartSnapshot.SameItems(after as IReadOnlyList<MokaCandle>, _snapshot)
		: base.SameValue(name, before, after);

	/// <inheritdoc />
	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn)
	{
		if (drawn < 0 || drawn >= _periods.Count || _periods[drawn] is not { } period)
		{
			return null;
		}

		MokaCandleText words = CandleWords;
		string heading = Heading(period);
		ChartSeriesInfo direction = _info[period.Up ? 0 : 1];
		int? pin = pinnedDrawn is { } p && p >= 0 && p < _periods.Count && _periods[p] is not null ? p : null;
		List<ChartActiveRow> rows =
		[
			new(words.Open, direction.ColorClass, direction.ColorStyle, Price(period.Open), null, false),
			new(words.High, direction.ColorClass, direction.ColorStyle, Price(period.High), null, false),
			new(words.Low, direction.ColorClass, direction.ColorStyle, Price(period.Low), null, false),
			new(words.Close, direction.ColorClass, direction.ColorStyle, Price(period.Close),
				pin is { } at ? Signed(period.Close - _periods[at]!.Close) : null, true)
		];
		if (ShowVolume && period.Volume is { } volume)
		{
			rows.Add(new ChartActiveRow(words.Volume, "moka-chart-muted", null, VolumeText(volume), null, false));
		}

		double change = period.Close - period.Open;
		string percent = period.Open != 0
			? (change / Math.Abs(period.Open)).ToString("+0.##%;-0.##%;0%", CultureInfo.CurrentCulture)
			: string.Empty;
		List<string> notes = [percent.Length > 0 ? $"{Signed(change)} ({percent})" : Signed(change)];
		notes.AddRange(AnnotationNotes(period.Close, drawn, 0.5));
		MokaChartPoint point = new(direction.Key, direction.Name, period.Up ? 0 : 1, drawn, period.Close, Price(period.Close))
		{
			Category = heading,
			Time = period.From,
			X = MokaChartX.FromDateTimeOffset(period.From).Value,
			Low = period.Low,
			High = period.High,
			SincePin = pin is { } since ? period.Close - _periods[since]!.Close : null
		};
		string cell = $"{heading}, {string.Join(", ", rows.Select(row => $"{row.Name} {row.ValueText}"))}, {notes[0]}";
		return new ChartActiveInfo(heading, rows, notes, cell, new MokaChartTooltipContext(drawn, heading, [point], point, pin, notes));
	}

	/// <inheritdoc />
	internal override void Rebuild()
	{
		_snapshot = DataVersion is null ? Candles?.ToArray() : null;
		MokaCandleText words = CandleWords;
		_info =
		[
			Direction(UpKey, words.Up, 1, "var(--moka-chart-up)"),
			Direction(DownKey, words.Down, 2, "var(--moka-chart-down)")
		];

		_candles = [.. (Candles ?? []).Where(c => double.IsFinite(c.Open) && double.IsFinite(c.High) && double.IsFinite(c.Low)
		                                         && double.IsFinite(c.Close))];
		_decimals = Decimals(_candles);
		(int[] slotOf, int slots, TimeSpan step) = Slots(_candles);
		int target = Math.Max(1, (int)((PlotWidthBucket is { } width && width > 0 ? width : DefaultWidth) / PixelsPerCandle));
		int merge = slots > target ? (int)Math.Ceiling(slots / (double)target) : 1;
		_slots = slots;
		_step = step * merge;
		_periods = Merge(slotOf, slots, merge);

		double low = double.PositiveInfinity;
		double high = double.NegativeInfinity;
		_volumeTop = 0;
		foreach (Period? period in _periods)
		{
			if (period is null || IsHidden(period.Up ? UpKey : DownKey))
			{
				continue;
			}

			low = Math.Min(low, period.Low);
			high = Math.Max(high, period.High);
			_volumeTop = Math.Max(_volumeTop, period.Volume ?? 0);
		}

		if (double.IsFinite(low) && double.IsFinite(high))
		{
			double pad = high > low ? (high - low) * Pad : Math.Max(Math.Abs(high) * 0.01, 1e-9);
			_valueRange = (low - pad, high + pad);
		}
		else
		{
			_valueRange = (double.NaN, double.NaN);
		}

		_scale = ValueScale(YAxis, _valueRange.Min, _valueRange.Max);
		_model = _periods.Count == 0 ? null : new ChartColumnsModel("moka-chart-cols moka-chart-cols--candles", null, _periods.Count, Column);
	}

	private ChartSeriesInfo Direction(string key, string name, int slot, string color) =>
		new(key, name, slot, SeriesClass(slot, null, false), new StyleBuilder().AddStyle("--moka-chart-color", color).Build(),
			ChartMark.Box, IsHidden(key), false, null);

	// Each candle's slot along the axis: its position, or its time over the usual step between candles, where a gap
	// leaves an empty slot. Times too far apart to slot sensibly fall back to positions.
	private (int[] SlotOf, int Slots, TimeSpan Step) Slots(List<MokaCandle> candles)
	{
		int count = candles.Count;
		int[] positions = [.. Enumerable.Range(0, count)];
		TimeSpan step = UsualStep(candles);
		if (Gapless || count < 2 || step <= TimeSpan.Zero)
		{
			return (positions, count, step);
		}

		DateTimeOffset first = candles[0].At;
		double span = (candles[^1].At - first).TotalMilliseconds;
		long slots = (long)Math.Round(span / step.TotalMilliseconds) + 1;
		if (slots < count || slots > 4L * count)
		{
			return (positions, count, step);
		}

		int[] slotOf = new int[count];
		for (int i = 0; i < count; i++)
		{
			slotOf[i] = (int)Math.Clamp(Math.Round((candles[i].At - first).TotalMilliseconds / step.TotalMilliseconds), 0, slots - 1);
		}

		return (slotOf, (int)slots, step);
	}

	private static TimeSpan UsualStep(List<MokaCandle> candles)
	{
		if (candles.Count < 2)
		{
			return TimeSpan.FromDays(1);
		}

		List<double> gaps = [];
		for (int i = 1; i < candles.Count; i++)
		{
			double gap = (candles[i].At - candles[i - 1].At).TotalMilliseconds;
			if (gap > 0)
			{
				gaps.Add(gap);
			}
		}

		if (gaps.Count == 0)
		{
			return TimeSpan.FromDays(1);
		}

		gaps.Sort();
		return TimeSpan.FromMilliseconds(gaps[(gaps.Count - 1) / 2]);
	}

	private List<Period?> Merge(int[] slotOf, int slots, int merge)
	{
		int columns = slots == 0 ? 0 : (int)Math.Ceiling(slots / (double)merge);
		List<Period?> periods = new(columns);
		int next = 0;
		for (int column = 0; column < columns; column++)
		{
			int end = (column + 1) * merge;
			Period? period = null;
			while (next < _candles.Count && slotOf[next] < end)
			{
				MokaCandle candle = _candles[next];
				period = period is null
					? new Period(candle.At, candle.At, candle.Open, candle.High, candle.Low, candle.Close, candle.Volume, 1)
					: period with
					{
						To = candle.At,
						High = Math.Max(period.High, candle.High),
						Low = Math.Min(period.Low, candle.Low),
						Close = candle.Close,
						Volume = period.Volume is null && candle.Volume is null ? null : (period.Volume ?? 0) + (candle.Volume ?? 0),
						Count = period.Count + 1
					};
				next++;
			}

			periods.Add(period);
		}

		return periods;
	}

	private ChartColumn Column(int index)
	{
		Period? period = _periods[index];
		List<ChartBox> marks = [];
		if (period is not null && !IsHidden(period.Up ? UpKey : DownKey))
		{
			string key = period.Up ? UpKey : DownKey;
			string direction = period.Up ? "moka-chart-mark--up" : "moka-chart-mark--down";
			if (ShowVolume && period.Volume is { } volume && volume > 0 && _volumeTop > 0)
			{
				marks.Add(ChartColumns.Mark("volume", 0, VolumeShare * volume / _volumeTop, null, null, key, ChartBoxEnds.None));
			}

			marks.Add(ChartColumns.Mark("wick", Share(period.Low), Share(period.High), null, null, key, ChartBoxEnds.None, direction));
			double open = Share(period.Open);
			double close = Share(period.Close);
			marks.Add(ChartColumns.Mark("body", Math.Min(open, close), Math.Max(open, close), null, null, key, ChartBoxEnds.None,
				period.Up ? $"{direction} moka-chart-mark--hollow" : direction));
		}

		string? title = period is not null && !Interactive
			? $"{Heading(period)}: {CandleWords.Open} {Price(period.Open)}, {CandleWords.High} {Price(period.High)}, {CandleWords.Low} {Price(period.Low)}, {CandleWords.Close} {Price(period.Close)}"
			: null;
		return new ChartColumn(index, "moka-chart-col moka-chart-col--candle", ChartColumns.SlotStyle(index, _periods.Count), title,
			[new ChartLane(marks, [])]);
	}

	private double Share(double value)
	{
		double share = _scale?.Share(value) is { } s && double.IsFinite(s) ? Math.Clamp(s, 0, 1) : 0;
		return YAxis?.Reverse ?? false ? 1 - share : share;
	}

	private string TableCell(int row, int column, bool raw)
	{
		MokaCandle candle = _candles[row];
		if (column == 0)
		{
			return raw ? candle.At.ToString("o", CultureInfo.InvariantCulture) : Heading(new Period(candle.At, candle.At,
				candle.Open, candle.High, candle.Low, candle.Close, candle.Volume, 1));
		}

		double? value = column switch
		{
			1 => candle.Open,
			2 => candle.High,
			3 => candle.Low,
			4 => candle.Close,
			_ => candle.Volume
		};
		if (value is not { } number)
		{
			return string.Empty;
		}

		return raw ? number.ToString(CultureInfo.InvariantCulture) : column == 5 ? VolumeText(number) : Price(number);
	}

	private string Price(double value) =>
		YAxis?.Formatter is not null || Formatter is not null || (YAxis?.Unit ?? Unit) is not null
			? WriteValue(value, YAxis)
			: value.ToString($"N{_decimals.ToString(CultureInfo.InvariantCulture)}", CultureInfo.CurrentCulture);

	private string Signed(double change) => change < 0 ? $"-{Price(-change)}" : $"+{Price(change)}";

	private static string VolumeText(double volume) => MokaChartUnit.Compact.Format(volume, null, CultureInfo.CurrentCulture);

	// As many decimals as the prices are written with, up to six, so 1.0812 doesn't read as 1.08.
	private static int Decimals(List<MokaCandle> candles)
	{
		int decimals = 0;
		foreach (MokaCandle candle in candles.Take(500))
		{
			foreach (double value in (ReadOnlySpan<double>)[candle.Open, candle.High, candle.Low, candle.Close])
			{
				while (decimals < MostDecimals)
				{
					double scaled = value * Math.Pow(10, decimals);
					if (Math.Abs(scaled - Math.Round(scaled)) <= 1e-6 * Math.Max(1, Math.Abs(scaled)))
					{
						break;
					}

					decimals++;
				}
			}
		}

		return decimals;
	}

	// A period's start along the axis: the time within a day, else the day, else the month; with the year once the
	// candles span more than one.
	private string Short(DateTimeOffset at)
	{
		DateTimeOffset local = TimeZoneInfo.ConvertTime(at, ResolvedTimeZone);
		CultureInfo culture = CultureInfo.CurrentCulture;
		return local.ToString(XAxis?.TimeFormat ?? ShortPattern(culture), culture);
	}

	private string ShortPattern(CultureInfo culture) => _step < TimeSpan.FromDays(1)
		? culture.DateTimeFormat.ShortTimePattern
		: _step < TimeSpan.FromDays(28) && !SpansYears
			? MonthDay(culture)
			: YearMonth(culture);

	private string Heading(Period period)
	{
		string Long(DateTimeOffset at)
		{
			DateTimeOffset local = TimeZoneInfo.ConvertTime(at, ResolvedTimeZone);
			CultureInfo culture = CultureInfo.CurrentCulture;
			string day = SpansYears ? $"{MonthDay(culture)} yyyy" : MonthDay(culture);
			string pattern = _step < TimeSpan.FromDays(1)
				? $"{day} {culture.DateTimeFormat.ShortTimePattern}"
				: _step < TimeSpan.FromDays(28)
					? $"ddd {day}"
					: YearMonth(culture);
			return local.ToString(pattern, culture);
		}

		return period.Count > 1 && period.To != period.From ? Words.Between(Long(period.From), Long(period.To)) : Long(period.From);
	}

	private bool SpansYears => _candles.Count > 1 && (_candles[^1].At - _candles[0].At).Duration() > TimeSpan.FromDays(330);

	private static string MonthDay(CultureInfo culture) =>
		culture.DateTimeFormat.MonthDayPattern.Replace("MMMM", "MMM", StringComparison.Ordinal);

	private static string YearMonth(CultureInfo culture) =>
		culture.DateTimeFormat.YearMonthPattern.Replace("MMMM", "MMM", StringComparison.Ordinal);
}
