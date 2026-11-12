using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Moka.Red.Data.Chart;

public abstract partial class MokaChartBase
{
	private const string ChartModule = "./_content/Moka.Red.Data/moka-chart.js";
	private const int NearestPixels = 32;

	private static int _surfaceIds;

	private readonly Dictionary<int, (int Series, int Drawn, bool Keyboard)> _active = [];
	private readonly Dictionary<int, int> _pinned = [];
	private readonly Dictionary<int, string> _said = [];
	private readonly List<ChartActiveHost> _hosts = [];
	private DotNetObjectReference<MokaChartBase>? _surfaceReference;
	private string? _surfaceOptionsKey;
	private string? _surfaceId;

	/// <summary>The chart's root element, which a modern chart captures with <c>@ref</c> for the script.</summary>
	internal ElementReference RootElement { get; set; }

	/// <summary>Whether the script is bound to this chart.</summary>
	internal bool SurfaceBound { get; private set; }

	/// <summary>Whether a pointer or the focus is holding a live chart still.</summary>
	internal bool Holding { get; private set; }

	/// <summary>How many samples arrived while a live chart held still, which it draws when it lets go.</summary>
	internal int Waiting { get; private set; }

	/// <summary>The plot's width, rounded up to 64 pixels, as the script last measured it; null before it has.</summary>
	internal int? PlotWidthBucket { get; private set; }

	/// <summary>
	///     What <see cref="MokaChartTooltip.Auto" /> means on this chart: the crosshair on lines. Bars and rows say
	///     <see cref="MokaChartTooltip.Column" />, scatter <see cref="MokaChartTooltip.Nearest" />.
	/// </summary>
	internal virtual MokaChartTooltip AutoTooltip => MokaChartTooltip.Crosshair;

	/// <summary>Whether the chart's categories run down in rows, so Up and Down move between them.</summary>
	internal virtual bool SurfaceRows => false;

	/// <summary>
	///     Whether the point under the pointer is the mark under it, as on a chart of parts (a donut, a treemap, a
	///     sankey), whose marks have no x axis to find them by. The surface then lets the pointer through to the marks,
	///     which keep their own hover and clicks, and takes only the keys.
	/// </summary>
	internal virtual bool HitsMarks => false;

	/// <summary>
	///     How the chart zooms along its x axis: the gestures the script listens for, and with
	///     <see cref="MokaChartZoom.Strip" /> the frame's range strip. A chart that zooms reads them from its <c>Zoom</c>;
	///     one that can't, such as a chart of bars or rows, keeps none.
	/// </summary>
	internal virtual MokaChartZoom ZoomGestures => MokaChartZoom.None;

	/// <summary>Whether the chart holds still while pointed at or focused, counting the samples that wait (I7A).</summary>
	internal virtual bool LiveHold => false;

	/// <summary>Whether the script reports the plot's width, for a chart that draws fewer points than it has.</summary>
	internal virtual bool MeasuresWidth => false;

	/// <summary>Whether the chart can share its crosshair by x value. A log x axis can't, so its chart says false.</summary>
	internal virtual bool SyncsByX => true;

	/// <summary>Whether the chart's zoom follows, and leads, the zoom of the charts in its sync group that link theirs too.</summary>
	internal virtual bool LinksZoom => false;

	/// <summary>
	///     What the chart's x axis counts in, for linked zoom: charts follow each other's zoom only when theirs say the same,
	///     time with time, numbers with numbers, and categories only when the labels match. Null when the chart doesn't link.
	/// </summary>
	internal virtual string? ZoomLinkKey => null;

	/// <summary>The x values at the plot's two edges as it is drawn now, which a linked zoom stretches from; null when it doesn't link.</summary>
	/// <param name="panel">The panel.</param>
	internal virtual (double From, double To)? ShownWindow(int panel) => null;

	/// <summary>How many key steps Page Up and Page Down take, such as a major tick on a time axis. Null is a tenth.</summary>
	internal virtual int? KeyPageStep => null;

	/// <summary>How many points, columns or rows a panel draws: the grid's columns and the script's slots.</summary>
	/// <param name="panel">The panel.</param>
	internal virtual int DrawnCount(int panel) => HoverData(panel) is { } data ? data.X.Length : 0;

	/// <summary>The newest drawn point, which a readout shows while nothing is active.</summary>
	/// <param name="panel">The panel.</param>
	internal virtual int LatestDrawn(int panel) => DrawnCount(panel) - 1;

	/// <summary>The x values the data spans, which zooming and panning stay inside. Null when the chart can't zoom.</summary>
	/// <param name="panel">The panel.</param>
	internal virtual (double From, double To)? XExtent(int panel) => null;

	/// <summary>The tooltip the surface runs: <see cref="MokaChartTooltip.Auto" /> resolved for this chart.</summary>
	internal MokaChartTooltip SurfaceMode => ResolvedTooltip == MokaChartTooltip.Auto ? AutoTooltip : ResolvedTooltip;

	/// <summary>
	///     Whether the surface draws no tooltip of its own: the browser's titles, or none. It still finds the point under
	///     the pointer, the way <see cref="MokaChartTooltip.Auto" /> would, for a click, a listener or the keyboard.
	/// </summary>
	internal bool SurfaceQuiet => SurfaceMode is MokaChartTooltip.Native or MokaChartTooltip.None;

	/// <summary>
	///     Whether the surface lets the pointer through to the marks: on a chart of parts, and on one whose tooltips are the
	///     browser's, so the marks' own titles still show, unless a brush or the wheel needs the surface to catch the drag.
	/// </summary>
	internal bool PassesPointer => HitsMarks
	                               || (SurfaceQuiet && (ZoomGestures & (MokaChartZoom.Brush | MokaChartZoom.Wheel)) == 0);

	/// <summary>Whether the drawn columns sit at their own places along the x axis, such as bars at their times, rather than in even slots.</summary>
	internal virtual bool PlacesColumns => false;

	/// <summary>
	///     Where the parts of a chart of parts sit by row, such as a heatmap's cells or a state timeline's spans, so the
	///     arrow keys step along a row and between rows. Null for a chart whose parts are in one line.
	/// </summary>
	/// <param name="panel">The panel.</param>
	internal virtual ChartPartLayout? PartLayout(int panel) => null;

	/// <summary>How the keys work on this chart, when it says so itself; null takes the words for its kind of plot.</summary>
	internal virtual string? OwnKeysText => null;

	/// <summary>How the keys work here, read after the chart's name.</summary>
	internal string KeysText => OwnKeysText ?? (SurfaceRows
		? Words.RowKeys
		: HitsMarks
			? Words.PartKeys
			: SurfaceMode == MokaChartTooltip.Column
				? Words.BarKeys
				: Words.LineKeys);

	/// <summary>
	///     The root's role: a figure holding the keyboard grid once the chart is interactive, since an image can't hold a
	///     control; an image named by its label otherwise.
	/// </summary>
	internal virtual string RootRole => Interactive || FrameInteractive ? "figure" : "img";

	/// <summary>
	///     What the surface reads from the chart that moves no geometry, so its host knows when to draw again. The
	///     <see cref="TooltipTemplate" /> isn't in it: a template written in markup is a new delegate on every parent
	///     render, and it is meant to render only when the point changes.
	/// </summary>
	internal object SurfaceKey => (AccessibleLabel, Words, SurfaceMode, ResolvedKeyboard, ResolvedPinnable, LiveHold,
		TooltipTemplate is null, SurfaceRows, HitsMarks);

	/// <summary>The chart's own prefix for element ids, unique on the page.</summary>
	internal string SurfaceId => _surfaceId ??=
		$"moka-chart-{Interlocked.Increment(ref _surfaceIds).ToString(CultureInfo.InvariantCulture)}";

	/// <summary>The id of a panel's keyboard grid.</summary>
	/// <param name="panel">The panel.</param>
	internal string GridId(int panel) => $"{SurfaceId}-p{panel.ToString(CultureInfo.InvariantCulture)}";

	/// <summary>The id of the one grid cell, which changes with the point so a screen reader announces the move.</summary>
	/// <param name="panel">The panel.</param>
	/// <param name="series">The series, or -1 for a whole column.</param>
	/// <param name="drawn">The drawn point.</param>
	internal string CellId(int panel, int series, int drawn) =>
		$"{GridId(panel)}-s{(series + 1).ToString(CultureInfo.InvariantCulture)}-i{drawn.ToString(CultureInfo.InvariantCulture)}";

	/// <summary>What the surface shows for a panel's active point, with the pin if one is set; null when nothing is active.</summary>
	/// <param name="panel">The panel.</param>
	internal ChartActiveInfo? Active(int panel) =>
		_active.TryGetValue(panel, out (int Series, int Drawn, bool Keyboard) at) && InRange(panel, at.Drawn)
			? DescribeActive(panel, at.Series, at.Drawn, _pinned.TryGetValue(panel, out int pinned) ? pinned : null)
			: null;

	/// <summary>The newest point's whole column, for a readout or a small multiple's header.</summary>
	/// <param name="panel">The panel.</param>
	internal ChartActiveInfo? LatestInfo(int panel) =>
		LatestDrawn(panel) is var latest and >= 0 ? DescribeActive(panel, -1, latest, null) : null;

	/// <summary>Everything a panel's hosts draw right now.</summary>
	/// <param name="panel">The panel.</param>
	internal ChartActiveState SurfaceState(int panel)
	{
		int? pinned = _pinned.TryGetValue(panel, out int pin) && InRange(panel, pin) ? pin : null;
		ChartActiveInfo? info = null;
		int series = -1;
		int drawn = -1;
		bool keyboard = false;
		if (_active.TryGetValue(panel, out (int Series, int Drawn, bool Keyboard) at) && InRange(panel, at.Drawn))
		{
			info = DescribeActive(panel, at.Series, at.Drawn, pinned);
			if (info is not null)
			{
				(series, drawn, keyboard) = at;
			}
		}

		return new ChartActiveState(info, pinned, Waiting)
		{
			Series = series,
			Drawn = drawn,
			Keyboard = keyboard,
			PinInfo = pinned is { } pinnedDrawn ? DescribeActive(panel, -1, pinnedDrawn, null) : null,
			Latest = info is null && SurfaceMode == MokaChartTooltip.Readout ? LatestInfo(panel) : null,
			LatestWord = Words.Latest,
			Holding = Holding,
			LiveText = LiveHold ? Holding ? Waiting > 0 ? Words.PausedWaiting(Waiting) : Words.Paused : Words.Live : null,
			Said = _said.TryGetValue(panel, out string? said) ? said : null
		};
	}

	/// <summary>
	///     A readout strip for a panel: the active point's values in a line, the newest while nothing is active. The plot
	///     places it above the plot box when the tooltip is <see cref="MokaChartTooltip.Readout" />.
	/// </summary>
	/// <param name="panel">The panel.</param>
	internal RenderFragment ReadoutContent(int panel) => ChartActiveHost.For(this, panel, MokaChartSurface.ReadoutTemplate);

	/// <summary>
	///     The live chip of a chart that holds still while pointed at: Live, Paused, or how many samples wait. The frame
	///     places it in the header, beside the title, so it never covers the marks.
	/// </summary>
	internal RenderFragment LiveContent => ChartActiveHost.For(this, 0, MokaChartSurface.LiveTemplate);

	/// <summary>
	///     A held live chart counts samples here instead of rebuilding; it draws them all when it lets go. Called from
	///     <see cref="Rebuild" />, it keeps the geometry version where it was, since the geometry didn't move.
	/// </summary>
	/// <param name="samples">How many new samples arrived.</param>
	internal void CountWaiting(int samples)
	{
		_geometryKept = true;
		if (samples <= 0)
		{
			return;
		}

		Waiting += samples;
		RefreshHosts(null);
	}

	/// <summary>Lets the pin go from the pin card's own button, and gives the keyboard grid the focus back.</summary>
	/// <param name="panel">The panel.</param>
	internal async Task UnpinAsync(int panel)
	{
		await OnPin(panel, -1);
		if (SurfaceBound)
		{
			await SafeModuleInvokeVoidAsync(ChartModule, "setPin", RootElement, panel, -1);
			await SafeModuleInvokeVoidAsync(ChartModule, "focusSurface", RootElement, panel);
		}
	}

	/// <summary>Registers a host, which the chart tells when what it shows changes.</summary>
	internal void AttachHost(ChartActiveHost host)
	{
		if (!_hosts.Contains(host))
		{
			_hosts.Add(host);
		}
	}

	/// <summary>Forgets a host that went away.</summary>
	internal void DetachHost(ChartActiveHost host) => _hosts.Remove(host);

	/// <summary>Binds the script to the chart's root, once, on its first interactive render.</summary>
	internal async ValueTask BindAsync()
	{
		Dictionary<string, object?> options = SurfaceOptions();
		_surfaceReference ??= DotNetObjectReference.Create<MokaChartBase>(this);
		await SafeModuleInvokeVoidAsync(ChartModule, "bind", RootElement, _surfaceReference, options);
		_surfaceOptionsKey = OptionsKey(options);
		SurfaceBound = true;
	}

	/// <summary>Sends the script the surface options again after they changed. Data changes send nothing.</summary>
	internal async ValueTask UpdateSurfaceAsync()
	{
		Dictionary<string, object?> options = SurfaceOptions();
		await SafeModuleInvokeVoidAsync(ChartModule, "update", RootElement, options);
		_surfaceOptionsKey = OptionsKey(options);
	}

	/// <summary>Unbinds the script, only when it was bound.</summary>
	internal async ValueTask UnbindAsync()
	{
		if (!SurfaceBound)
		{
			return;
		}

		SurfaceBound = false;
		_surfaceOptionsKey = null;
		_active.Clear();
		await SafeModuleInvokeVoidAsync(ChartModule, "unbind", RootElement);
	}

	/// <summary>Whether the options the script runs on differ from those it was last sent.</summary>
	internal bool SurfaceOptionsChanged() => OptionsKey(SurfaceOptions()) != _surfaceOptionsKey;

	/// <summary>
	///     The options the script runs on. A dictionary rather than an anonymous object, which the trimmer can't see
	///     into.
	/// </summary>
	internal Dictionary<string, object?> SurfaceOptions() => new()
	{
		["mode"] = ModeName(SurfaceMode),
		["track"] = SurfaceQuiet ? ModeName(AutoTooltip) : null,
		["orientation"] = SurfaceRows ? "rows" : "columns",
		["hit"] = HitsMarks,
		["pass"] = PassesPointer && !HitsMarks,
		["placed"] = PlacesColumns,
		["pin"] = ResolvedPinnable,
		["keys"] = ResolvedKeyboard,
		["sync"] = string.IsNullOrEmpty(SyncGroup) || !SyncsByX ? null : SyncGroup,
		["link"] = LinksZoom ? ZoomLinkKey : null,
		["zoom"] = (int)ZoomGestures,
		["hold"] = LiveHold,
		["measure"] = MeasuresWidth,
		["nearestPx"] = NearestPixels,
		["report"] = ActiveChanged.HasDelegate,
		["select"] = OnPointClick.HasDelegate,
		["highlight"] = ResolvedLegendActions.HasFlag(MokaChartLegendActions.Highlight),
		["pageStep"] = KeyPageStep,
		["words"] = new Dictionary<string, object?> { ["zoomHint"] = Words.ZoomHint }
	};

	/// <summary>Gives back the reference the script called the chart through.</summary>
	internal void ReleaseSurfaceReference()
	{
		_surfaceReference?.Dispose();
		_surfaceReference = null;
	}

	/// <summary>The encoded geometry of a panel's drawn points, asked for the first time a pointer arrives after it moved.</summary>
	/// <param name="panel">The panel.</param>
	/// <param name="version">
	///     The geometry version the surface showed when the script asked. The answer carries the version it was built
	///     at, which is newer when the chart rebuilt in between, so the script never needs it back.
	/// </param>
	/// <returns>
	///     The encoded geometry, the part layout of a chart whose parts are listed row by row, or null when the chart has
	///     neither to give (its marks carry <c>data-i</c> and its rows are regular).
	/// </returns>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public byte[]? HoverGeometry(int panel, long version) => HoverData(panel) is { } data
		? ChartHoverEncoding.Encode(data, GeometryVersion)
		: PartLayout(panel) is { Order: ChartPartOrder.Listed } layout
			? ChartHoverEncoding.EncodeParts(layout, GeometryVersion)
			: null;

	/// <summary>The point under the pointer or the focus changed; -1 clears it.</summary>
	/// <param name="panel">The panel.</param>
	/// <param name="series">The series, in legend order, or -1 for a whole column.</param>
	/// <param name="drawn">The drawn point.</param>
	/// <param name="keyboard">Whether the keyboard moved it.</param>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Task OnActive(int panel, int series, int drawn, bool keyboard)
	{
		if (drawn < 0)
		{
			_active.Remove(panel);
		}
		else
		{
			_active[panel] = (series, drawn, keyboard);
		}

		ChartActiveState state = SurfaceState(panel);
		Show(panel, state);
		if (ActiveChanged.HasDelegate)
		{
			MokaChartPoint? point = state.Info is { Context: var context } ? context.Focus ?? FirstPoint(context) : null;
			RunDetached(() => ActiveChanged.InvokeAsync(point));
		}

		return Task.CompletedTask;
	}

	/// <summary>A click or Enter pinned a point; -1 lets the pin go.</summary>
	/// <param name="panel">The panel.</param>
	/// <param name="drawn">The drawn point.</param>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Task OnPin(int panel, int drawn)
	{
		if (drawn < 0)
		{
			if (!_pinned.Remove(panel))
			{
				return Task.CompletedTask;
			}

			Say(panel, Words.Unpinned);
		}
		else
		{
			_pinned[panel] = drawn;
			ChartActiveInfo? pinned = InRange(panel, drawn) ? DescribeActive(panel, -1, drawn, null) : null;
			Say(panel, Words.PinnedAt(pinned?.Heading ?? (drawn + 1).ToString(CultureInfo.CurrentCulture)));
		}

		Show(panel, SurfaceState(panel));
		return Task.CompletedTask;
	}

	/// <summary>A point was clicked, or Enter pressed on it.</summary>
	/// <param name="panel">The panel.</param>
	/// <param name="series">The series, in legend order, or -1 for a whole column.</param>
	/// <param name="drawn">The drawn point.</param>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Task OnSelect(int panel, int series, int drawn)
	{
		if (OnPointClick.HasDelegate && InRange(panel, drawn)
		    && DescribeActive(panel, series, drawn, null) is { Context: var context }
		    && (context.Focus ?? FirstPoint(context)) is { } point)
		{
			RunDetached(() => OnPointClick.InvokeAsync(point));
		}

		return Task.CompletedTask;
	}

	/// <summary>A pointer or the focus entered or left a chart that holds still while pointed at.</summary>
	/// <param name="holding">Whether it is holding.</param>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Task OnHold(bool holding)
	{
		if (Holding == holding)
		{
			return Task.CompletedTask;
		}

		Holding = holding;
		if (!holding && Waiting > 0)
		{
			Waiting = 0;
			Redraw();
		}

		RefreshHosts(null);
		return Task.CompletedTask;
	}

	/// <summary>
	///     A brush, wheel zoom or pan was let go; null and null reset the zoom. Nullable because JSON writes NaN as
	///     null, which a plain double can't take; the chart hears NaN and NaN.
	/// </summary>
	/// <param name="from">Where the span starts, along the x axis.</param>
	/// <param name="to">Where it ends.</param>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Task OnRange(double? from, double? to) => RangeChosenAsync(from ?? double.NaN, to ?? double.NaN);

	/// <summary>The plot's width moved into another 64 pixel bucket.</summary>
	/// <param name="bucketPx">The width, rounded up to 64 pixels.</param>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Task OnPlotWidth(int bucketPx)
	{
		if (PlotWidthBucket != bucketPx)
		{
			PlotWidthBucket = bucketPx;
			Redraw();
		}

		return Task.CompletedTask;
	}

	/// <summary>What a chart does with a span the script chose. A chart with an x axis zooms to it.</summary>
	/// <param name="from">Where the span starts.</param>
	/// <param name="to">Where it ends.</param>
	internal virtual Task RangeChosenAsync(double from, double to) => Task.CompletedTask;

	private static string ModeName(MokaChartTooltip mode) => mode switch
	{
		MokaChartTooltip.Crosshair => "crosshair",
		MokaChartTooltip.Nearest => "nearest",
		MokaChartTooltip.Readout => "readout",
		MokaChartTooltip.Column => "column",
		_ => "none"
	};

	private static MokaChartPoint? FirstPoint(MokaChartTooltipContext context) =>
		context.Points.Count > 0 ? context.Points[0] : null;

	private bool InRange(int panel, int drawn) =>
		drawn >= 0 && (DrawnCount(panel) is var count && (count <= 0 || drawn < count));

	// A status region announces only a change, so a sentence said twice in a row gets a no-break space the second time.
	private void Say(int panel, string sentence) =>
		_said[panel] = _said.TryGetValue(panel, out string? last) && last == sentence ? sentence + " " : sentence;

	private void Show(int panel, ChartActiveState state)
	{
		foreach (ChartActiveHost host in _hosts.ToArray())
		{
			if (host.Panel == panel)
			{
				host.Show(state);
			}
		}
	}

	private void RefreshHosts(int? panel)
	{
		foreach (ChartActiveHost host in _hosts.ToArray())
		{
			if (panel is null || host.Panel == panel)
			{
				host.Show(SurfaceState(host.Panel));
			}
		}
	}

	private static string OptionsKey(Dictionary<string, object?> options) =>
		string.Join('|', options.Select(option => option.Value switch
		{
			Dictionary<string, object?> nested => $"{option.Key}={OptionsKey(nested)}",
			IFormattable formattable => $"{option.Key}={formattable.ToString(null, CultureInfo.InvariantCulture)}",
			_ => $"{option.Key}={option.Value}"
		}));
}
