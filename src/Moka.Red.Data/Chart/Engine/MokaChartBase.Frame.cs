using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Enums;
using Moka.Red.Data.Common;

namespace Moka.Red.Data.Chart;

public abstract partial class MokaChartBase
{
	private const string ExportModule = "./_content/Moka.Red.Data/moka-chart-export.js";

	// What Windows refuses in a file name, which covers the others.
	private const string NotInFileNames = "\\/:*?\"<>|";

	private static readonly TimeSpan[] TimeSteps =
	[
		TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30),
		TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(15),
		TimeSpan.FromMinutes(30), TimeSpan.FromHours(1), TimeSpan.FromHours(3), TimeSpan.FromHours(6),
		TimeSpan.FromHours(12), TimeSpan.FromDays(1), TimeSpan.FromDays(7), TimeSpan.FromDays(30)
	];

	private static readonly TimeSpan[] LargeTimeSteps =
	[
		TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10), TimeSpan.FromHours(1), TimeSpan.FromHours(6),
		TimeSpan.FromDays(1), TimeSpan.FromDays(7), TimeSpan.FromDays(30), TimeSpan.FromDays(365)
	];

	private IReadOnlyList<ChartSeriesInfo>? _legendSource;
	private IReadOnlyList<MokaChartLegendItem> _legendItems = [];
	private IReadOnlyCollection<string> _hiddenSnapshot = [];
	private string? _presetChosen;
	private string? _lastPresetParameter;
	private MokaChartRange? _presetRange;
	private string? _exportFailure;

	/// <summary>The legend's entries, from the chart's series, in legend order, with the colors the chart draws them in.</summary>
	internal IReadOnlyList<MokaChartLegendItem> LegendItems
	{
		get
		{
			IReadOnlyList<ChartSeriesInfo> source = SeriesInfo;
			if (ReferenceEquals(source, _legendSource))
			{
				return _legendItems;
			}

			_legendSource = source;
			List<MokaChartLegendItem> items = [.. source.Select(ToLegendItem)];
			if (!items.SequenceEqual(_legendItems))
			{
				_legendItems = items;
			}

			return _legendItems;
		}
	}

	/// <summary>The keys the legend hid, as one collection that changes only when they do.</summary>
	internal IReadOnlyCollection<string> HiddenKeys
	{
		get
		{
			if (_hiddenSnapshot.Count != _hidden.Count || !_hiddenSnapshot.All(_hidden.Contains))
			{
				_hiddenSnapshot = [.. _hidden];
			}

			return _hiddenSnapshot;
		}
	}

	/// <summary>The frame's root, which an exported picture is taken from. The frame sets it.</summary>
	internal ElementReference? FrameElement { get; set; }

	/// <summary>What went wrong with the last export, until the next one works or it is put away.</summary>
	internal string? ExportFailure => _exportFailure;

	/// <summary>The exported file's name without its extension: <see cref="ExportName" />, else the title or label as a slug.</summary>
	internal string ExportFileName
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(ExportName))
			{
				string own = new string([.. ExportName.Where(c => !char.IsControl(c) && !NotInFileNames.Contains(c, StringComparison.Ordinal))]).Trim('.', ' ');
				return own.Length == 0 ? "chart" : own;
			}

			string stem = !string.IsNullOrWhiteSpace(Title) ? Title : !string.IsNullOrWhiteSpace(Label) ? Label : "chart";
			StringBuilder slug = new();
			bool gap = false;
			foreach (char c in stem)
			{
				if (char.IsLetterOrDigit(c))
				{
					if (gap && slug.Length > 0)
					{
						slug.Append('-');
					}

					gap = false;
					slug.Append(char.ToLowerInvariant(c));
				}
				else
				{
					gap = true;
				}
			}

			return slug.Length == 0 ? "chart" : slug.ToString();
		}
	}

	/// <summary>The exports the toolbar offers: those asked for, less CSV when the chart has no table of values.</summary>
	internal MokaChartControls ExportFormats
	{
		get
		{
			MokaChartControls formats = ResolvedControls & MokaChartControls.Export;
			return TableModel is null ? formats & ~MokaChartControls.ExportCsv : formats;
		}
	}

	/// <summary>
	///     Whether the frame draws anything a person can use: a toolbar, legend toggles, Try again, the table or the
	///     range strip. A chart whose frame does is a figure, not an image, since an image's contents can't be reached.
	/// </summary>
	internal bool FrameInteractive =>
		!IsClassic
		&& ((ResolvedControls & ~MokaChartControls.Legend) != MokaChartControls.None
		    || Actions is not null
		    || (ResolvedLegend != MokaChartLegendPosition.None && ResolvedLegendActions.HasFlag(MokaChartLegendActions.Hide))
		    || (ResolvedState == MokaChartState.Error && OnRetry.HasDelegate)
		    || CurrentView == MokaChartView.Table
		    || Strip is not null);

	/// <summary>
	///     The values the range strip draws behind its window: the first series the legend shows, across all of the data.
	///     Null draws the strip without them. A chart with an x axis returns its own.
	/// </summary>
	internal virtual IReadOnlyList<double>? OverviewValues => null;

	/// <summary>The span the chart is zoomed into, null when it shows everything. A chart that keeps its own returns it.</summary>
	internal virtual MokaChartRange? CurrentRange => (this as MokaCartesianChartBase)?.Range;

	/// <summary>The range strip under the plot, or null when the chart has none.</summary>
	internal ChartStrip? Strip
	{
		get
		{
			// A chart that can't zoom along its x axis, such as one of rows or bars, has nothing for the strip to move.
			if (this is not MokaCartesianChartBase { DrawsRows: false } chart || !ZoomGestures.HasFlag(MokaChartZoom.Strip))
			{
				return null;
			}

			if (chart.Times is { Count: > 1 } times)
			{
				double first = MokaChartX.FromDateTimeOffset(times[0]).Value;
				double last = MokaChartX.FromDateTimeOffset(times[^1]).Value;
				(double min, double max) = first <= last ? (first, last) : (last, first);
				if (max <= min)
				{
					return null;
				}

				TimeSpan span = TimeSpan.FromMilliseconds(max - min);
				TimeSpan step = TimeSteps.LastOrDefault(s => s.TotalMilliseconds * 120 <= span.TotalMilliseconds, TimeSteps[0]);
				TimeSpan large = LargeTimeSteps.FirstOrDefault(s => s >= step * 10, LargeTimeSteps[^1]);
				return new ChartStrip(min, max, true, step.TotalMilliseconds, large.TotalMilliseconds, OverviewValues);
			}

			int count = chart.CategoryCount;
			return count > 1 ? new ChartStrip(0, count - 1, false, 1, null, OverviewValues) : null;
		}
	}

	/// <summary>The label of the range preset in force, or null.</summary>
	internal string? ShownPreset
	{
		get
		{
			string? parameter = (this as MokaCartesianChartBase)?.RangePreset;
			if (!string.Equals(parameter, _lastPresetParameter, StringComparison.Ordinal))
			{
				_lastPresetParameter = parameter;
				_presetChosen = parameter;
			}

			return _presetChosen;
		}
	}

	/// <summary>The range presets the toolbar offers: only on a time axis.</summary>
	internal IReadOnlyList<MokaChartRangePreset> Presets =>
		this is MokaCartesianChartBase { RangePresets: { Count: > 0 } presets, Times.Count: > 0 } ? presets : [];

	/// <summary>Whether the chart is zoomed by hand, rather than to a preset's range.</summary>
	internal bool Zoomed => CurrentRange is { } range && (_presetRange is not { } preset || !SameRange(range, preset));

	/// <summary>Hides exactly these series, redraws, and tells the parent.</summary>
	/// <param name="hidden">The keys to hide.</param>
	internal Task SetHiddenAsync(IReadOnlyCollection<string> hidden)
	{
		ArgumentNullException.ThrowIfNull(hidden);

		_hidden.Clear();
		foreach (string key in hidden)
		{
			_hidden.Add(key);
		}

		IReadOnlyCollection<string> current = [.. _hidden];
		Redraw();
		return HiddenSeriesChanged.InvokeAsync(current);
	}

	/// <summary>Switches between the chart and the table, redraws, and tells the parent.</summary>
	/// <param name="view">The view to show.</param>
	internal Task SetViewAsync(MokaChartView view)
	{
		if (_view == view)
		{
			return Task.CompletedTask;
		}

		_view = view;
		Redraw(geometry: false);
		return ViewChanged.InvokeAsync(view);
	}

	/// <summary>
	///     Saves the chart as the person sees it: a PNG picture or an SVG drawing of the chart with its title and legend,
	///     or its values as CSV. The toolbar's Export menu calls it, and an app can call it from anywhere. The file is
	///     named after <see cref="ExportName" />, else the title, else the label. A failure shows in the chart's frame;
	///     CSV does nothing on a chart that has no table of values.
	/// </summary>
	/// <param name="format">One of <see cref="MokaChartControls.ExportPng" />, <see cref="MokaChartControls.ExportSvg" /> and <see cref="MokaChartControls.ExportCsv" />.</param>
	/// <returns>A task that completes once the browser has the file.</returns>
	public async Task ExportAsync(MokaChartControls format)
	{
		if (format is not (MokaChartControls.ExportPng or MokaChartControls.ExportSvg or MokaChartControls.ExportCsv))
		{
			throw new ArgumentOutOfRangeException(nameof(format), format, "Only one export at a time.");
		}

		try
		{
			if (format == MokaChartControls.ExportCsv)
			{
				if (TableModel is not { } model)
				{
					return;
				}

				using DotNetStreamReference stream = new(new CsvStream(model));
				await SafeModuleInvokeVoidAsync(ExportModule, "download", stream, ExportFileName + ".csv", "text/csv;charset=utf-8");
			}
			else
			{
				bool png = format == MokaChartControls.ExportPng;
				Dictionary<string, object?> options = new()
				{
					["scale"] = 2,
					["pad"] = 16,
					["skip"] = "[data-moka-export=\"skip\"], .moka-chart-overlay, .moka-visually-hidden"
				};
				await SafeModuleInvokeVoidAsync(ExportModule, "exportPicture", FrameElement ?? RootElement,
					png ? "png" : "svg", ExportFileName + (png ? ".png" : ".svg"), options);
			}

			if (_exportFailure is not null)
			{
				_exportFailure = null;
				Redraw(geometry: false);
			}
		}
		catch (JSException)
		{
			_exportFailure = Words.ExportFailed;
			Redraw(geometry: false);
		}
	}

	/// <summary>Puts the export failure away.</summary>
	internal void ClearExportFailure()
	{
		if (_exportFailure is null)
		{
			return;
		}

		_exportFailure = null;
		Redraw(geometry: false);
	}

	/// <summary>Puts focus back on the chart once the table view closes: its keyboard surface, else the plot's box.</summary>
	/// <param name="fallback">What to focus when the chart takes no keys.</param>
	internal async Task FocusAfterTableAsync(ElementReference fallback)
	{
		if (SurfaceBound)
		{
			await SafeModuleInvokeVoidAsync(ChartModule, "focusSurface", RootElement, 0);
		}
		else
		{
			await SafeFocusAsync(fallback);
		}
	}

	/// <summary>Zooms to the span the range strip was let go at, or shows everything when it covers all of it.</summary>
	/// <param name="from">Where the span starts, along the x axis.</param>
	/// <param name="to">Where it ends.</param>
	internal async Task ChooseRangeAsync(double from, double to)
	{
		await ForgetPresetAsync();
		if (Strip is { } strip && from <= strip.Min && to >= strip.Max)
		{
			await RangeChosenAsync(double.NaN, double.NaN);
		}
		else
		{
			await RangeChosenAsync(Math.Min(from, to), Math.Max(from, to));
		}

		Redraw(geometry: false);
	}

	/// <summary>Shows everything again.</summary>
	internal async Task ResetZoomAsync()
	{
		await ForgetPresetAsync();
		await RangeChosenAsync(double.NaN, double.NaN);
		Redraw(geometry: false);
	}

	/// <summary>
	///     Zooms to the last span a preset names: back from the end of the data, or from now when the data runs past now,
	///     and tells the parent which preset is in force. The parent may load other data meanwhile.
	/// </summary>
	/// <param name="label">The preset's label.</param>
	/// <param name="now">The time now.</param>
	internal async Task ChoosePresetAsync(string? label, DateTimeOffset now)
	{
		if (this is not MokaCartesianChartBase { Times: { Count: > 0 } times } chart
		    || Presets.FirstOrDefault(preset => string.Equals(preset.Label, label, StringComparison.Ordinal)) is not { } chosen)
		{
			return;
		}

		_ = ShownPreset;
		DateTimeOffset end = times[^1] > times[0] ? times[^1] : times[0];
		if (now < end)
		{
			end = now;
		}

		MokaChartRange range = new(end - chosen.Span, end);
		_presetChosen = chosen.Label;
		_presetRange = range;
		await chart.RangePresetChanged.InvokeAsync(chosen.Label);
		await RangeChosenAsync(range.From.Value, range.To.Value);
		Redraw(geometry: false);
	}

	/// <summary>Writes a value along the range strip's axis: a time, a category, or a number.</summary>
	/// <param name="value">The value.</param>
	internal string StripText(double value)
	{
		if (this is not MokaCartesianChartBase chart)
		{
			return value.ToString("G", CultureInfo.CurrentCulture);
		}

		if (Strip is { IsTime: true } strip)
		{
			MokaTimeTickOptions options = new() { Zone = chart.ResolvedTimeZone };
			return MokaTimeTicks.TooltipText(new MokaChartX(value, true).Time, TimeSpan.FromMilliseconds(strip.Max - strip.Min), options);
		}

		int index = (int)Math.Round(value);
		return chart.Labels is { } labels && index >= 0 && index < labels.Count
			? labels[index]
			: Formatter?.Invoke(value) ?? value.ToString("G", CultureInfo.CurrentCulture);
	}

	private async Task ForgetPresetAsync()
	{
		_presetRange = null;
		if (ShownPreset is null)
		{
			return;
		}

		_presetChosen = null;
		if (this is MokaCartesianChartBase chart)
		{
			await chart.RangePresetChanged.InvokeAsync(null);
		}
	}

	private static bool SameRange(MokaChartRange a, MokaChartRange b) =>
		Math.Abs(a.From.Value - b.From.Value) < 0.5 && Math.Abs(a.To.Value - b.To.Value) < 0.5;

	private static MokaChartLegendItem ToLegendItem(ChartSeriesInfo series)
	{
		MokaChartLegendItem item = new(series.Key, series.Name)
		{
			Slot = series.Slot,
			Value = series.CurrentText,
			Mark = series.Mark switch
			{
				ChartMark.Line => MokaChartLegendMark.Line,
				ChartMark.Area or ChartMark.Box => MokaChartLegendMark.Box,
				_ => MokaChartLegendMark.Dot
			}
		};

		const string tone = "moka-chart-tone-";
		const string inline = "--moka-chart-color:";
		if (series.ColorClass == "moka-chart-muted")
		{
			item = item with { ColorValue = "var(--moka-chart-muted)" };
		}
		else if (series.ColorClass.StartsWith(tone, StringComparison.Ordinal)
		         && Enum.TryParse(series.ColorClass[tone.Length..], true, out MokaColor color))
		{
			item = item with { Color = color };
		}

		if (series.ColorStyle is { } style && style.StartsWith(inline, StringComparison.Ordinal))
		{
			item = item with { ColorValue = style[inline.Length..].Trim() };
		}

		return item;
	}

	/// <summary>The range strip: the whole x axis, how far the keys move, and what it draws behind the window.</summary>
	/// <param name="Min">The start of the data along the x axis.</param>
	/// <param name="Max">The end of the data.</param>
	/// <param name="IsTime">Whether the axis is time, in Unix milliseconds.</param>
	/// <param name="Step">How far an arrow key moves a handle.</param>
	/// <param name="LargeStep">How far Shift and an arrow move it, or null for ten steps.</param>
	/// <param name="Overview">The values drawn behind the window.</param>
	internal sealed record ChartStrip(double Min, double Max, bool IsTime, double Step, double? LargeStep, IReadOnlyList<double>? Overview);

	// The values as CSV, written a few rows at a time while the browser reads, so a million rows are never held as one
	// string or sent as one message. Read on the caller's thread, never on the thread pool, since the cells are read
	// from the chart.
	private sealed class CsvStream(ChartTableModel model) : Stream
	{
		private const int ChunkChars = 16 * 1024;

		private readonly StringBuilder _text = new();
		private byte[] _pending = Encoding.UTF8.GetPreamble();
		private int _offset;
		private int _row = -1;

		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

		public override int Read(Span<byte> buffer)
		{
			int written = 0;
			while (written < buffer.Length)
			{
				if (_offset >= _pending.Length && !Fill())
				{
					break;
				}

				int count = Math.Min(buffer.Length - written, _pending.Length - _offset);
				_pending.AsSpan(_offset, count).CopyTo(buffer[written..]);
				_offset += count;
				written += count;
			}

			return written;
		}

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
			cancellationToken.IsCancellationRequested
				? ValueTask.FromCanceled<int>(cancellationToken)
				: ValueTask.FromResult(Read(buffer.Span));

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
			ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

		public override void Flush()
		{
		}

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		private bool Fill()
		{
			if (_row >= model.RowCount)
			{
				return false;
			}

			_text.Clear();
			while (_row < model.RowCount && _text.Length < ChunkChars)
			{
				for (int column = 0; column < model.Headers.Count; column++)
				{
					if (column > 0)
					{
						_text.Append(',');
					}

					_text.Append(MokaCsv.Escape(_row < 0 ? model.Headers[column] : model.Raw(_row, column)));
				}

				_text.Append("\r\n");
				_row++;
			}

			_pending = Encoding.UTF8.GetBytes(_text.ToString());
			_offset = 0;
			return true;
		}
	}
}
