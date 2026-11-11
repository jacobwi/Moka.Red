using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     What every Moka.Red chart shares: its name and title, its look and colors, the legend, tooltips, keyboard and
///     toolbar options, the hidden series, its state and view, and the render model that draws again only when
///     something it draws from has changed. Only Moka.Red's own charts derive from it.
/// </summary>
public abstract partial class MokaChartBase : MokaVisualComponentBase
{
	private const int SlotCount = 8;

	private static readonly HashSet<string> ChromeParameters = new(StringComparer.Ordinal)
	{
		nameof(Title), nameof(Subtitle), nameof(TitleContent), nameof(Actions), nameof(View), nameof(ViewChanged),
		nameof(State), nameof(EmptyContent), nameof(ErrorContent), nameof(ErrorText), nameof(OnRetry), nameof(Controls),
		nameof(ToolbarControl), nameof(ExportName), nameof(HiddenSeriesChanged), nameof(ActiveChanged),
		nameof(OnPointClick), nameof(TooltipTemplate), nameof(Class), nameof(Style), nameof(Id),
		nameof(AdditionalAttributes), nameof(Margin), nameof(MarginValue), nameof(Padding), nameof(PaddingValue),
		nameof(Rounded), nameof(RoundedValue)
	};

	private readonly HashSet<string> _hidden = new(StringComparer.Ordinal);
	private IReadOnlyDictionary<string, object?>? _lastParameters;
	private IReadOnlyCollection<string>? _lastHiddenParameter;
	private MokaChartView _view;
	private MokaChartView? _lastViewParameter;
	private bool _dirty = true;
	private bool _geometryDirty = true;
	private bool _geometryKept;

	/// <summary>What the chart shows, for assistive technology.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>A title over the chart.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>A line under the title.</summary>
	[Parameter]
	public string? Subtitle { get; set; }

	/// <summary>Drawn in place of the title text.</summary>
	[Parameter]
	public RenderFragment? TitleContent { get; set; }

	/// <summary>Controls at the end of the header.</summary>
	[Parameter]
	public RenderFragment? Actions { get; set; }

	/// <summary>
	///     A fixed height for the plot and its labels, as a CSS length such as "230px", whatever the width. Without it
	///     the chart keeps its proportion, no taller than its <c>Size</c> allows.
	/// </summary>
	[Parameter]
	public string? Height { get; set; }

	/// <summary>Whether the series are listed in a legend. Defaults to true. False wins over <see cref="Legend" />.</summary>
	[Parameter]
	public bool ShowLegend { get; set; } = true;

	/// <summary>How the chart is drawn. Wins over <see cref="MokaTheme.Charts" />. The original design always draws Standard.</summary>
	[Parameter]
	public MokaChartLook? Look { get; set; }

	/// <summary>
	///     The chart's own colors, taken in turn by its series in place of the palette. Each has to be a CSS color or token;
	///     anything else is skipped.
	/// </summary>
	[Parameter]
	public IReadOnlyList<string>? Colors { get; set; }

	/// <summary>Whether marks are hatched. Wins over <see cref="MokaTheme.ChartTexture" />.</summary>
	[Parameter]
	public bool? Texture { get; set; }

	/// <summary>Where the legend sits. Null takes the look's.</summary>
	[Parameter]
	public MokaChartLegendPosition? Legend { get; set; }

	/// <summary>What marks each legend entry. Null takes the look's.</summary>
	[Parameter]
	public MokaChartLegendKey? LegendKey { get; set; }

	/// <summary>What the legend's entries do. Null takes the look's.</summary>
	[Parameter]
	public MokaChartLegendActions? LegendActions { get; set; }

	/// <summary>The keys of the series the legend hid. Bind it to keep them across pages or share them between charts.</summary>
	[Parameter]
	public IReadOnlyCollection<string>? HiddenSeries { get; set; }

	/// <summary>Raised with the new set whenever the legend hides or shows a series.</summary>
	[Parameter]
	public EventCallback<IReadOnlyCollection<string>> HiddenSeriesChanged { get; set; }

	/// <summary>One series' key to draw in its color, the rest muted, named at its end.</summary>
	[Parameter]
	public string? Highlight { get; set; }

	/// <summary>Several series' keys to draw in their colors, the rest muted.</summary>
	[Parameter]
	public IReadOnlyCollection<string>? HighlightSeries { get; set; }

	/// <summary>How the chart says what is under the pointer. Null takes the look's.</summary>
	[Parameter]
	public MokaChartTooltip? Tooltip { get; set; }

	/// <summary>Draws the tooltip in place of the chart's own. Rendered only when the point under the pointer changes.</summary>
	[Parameter]
	public RenderFragment<MokaChartTooltipContext>? TooltipTemplate { get; set; }

	/// <summary>Whether a click or Enter pins the tooltip, showing the change since the pin. Null takes the look's.</summary>
	[Parameter]
	public bool? Pinnable { get; set; }

	/// <summary>Whether the chart takes one tab stop and reads its points with the arrow keys. Null takes the look's.</summary>
	[Parameter]
	public bool? Keyboard { get; set; }

	/// <summary>Charts with the same group share their crosshair.</summary>
	[Parameter]
	public string? SyncGroup { get; set; }

	/// <summary>Raised when a point is clicked or Enter is pressed on it.</summary>
	[Parameter]
	public EventCallback<MokaChartPoint> OnPointClick { get; set; }

	/// <summary>Raised with the point under the pointer or the focus, and null when there is none.</summary>
	[Parameter]
	public EventCallback<MokaChartPoint?> ActiveChanged { get; set; }

	/// <summary>The toolbar's controls. Null takes the look's.</summary>
	[Parameter]
	public MokaChartControls? Controls { get; set; }

	/// <summary>Replaces or wraps a toolbar control, drawing its <see cref="MokaChartControlContext.Default" /> to keep it.</summary>
	[Parameter]
	public RenderFragment<MokaChartControlContext>? ToolbarControl { get; set; }

	/// <summary>Whether the chart or a table of its values shows. Defaults to the chart.</summary>
	[Parameter]
	public MokaChartView View { get; set; }

	/// <summary>Raised when the toolbar switches between the chart and the table.</summary>
	[Parameter]
	public EventCallback<MokaChartView> ViewChanged { get; set; }

	/// <summary>The state of the chart's data. <see cref="MokaChartState.Auto" /> is empty without data, else ready.</summary>
	[Parameter]
	public MokaChartState State { get; set; }

	/// <summary>Text drawn in the middle of the plot while there are no values, such as "Waiting for traffic".</summary>
	[Parameter]
	public string? EmptyText { get; set; }

	/// <summary>Drawn in place of the empty state.</summary>
	[Parameter]
	public RenderFragment? EmptyContent { get; set; }

	/// <summary>Drawn in place of the error state.</summary>
	[Parameter]
	public RenderFragment? ErrorContent { get; set; }

	/// <summary>What the error state says.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Raised by the error state's Try again button, which shows only when this has a listener.</summary>
	[Parameter]
	public EventCallback OnRetry { get; set; }

	/// <summary>
	///     Writes a value as text, for the ticks, the tooltips and the legend, such as
	///     <c>v =&gt; $"{v / 1e6:0.#} MB/s"</c>. Keep it a field or a method: a lambda that captures a local is new every
	///     render, and the chart then draws again each time.
	/// </summary>
	[Parameter]
	public Func<double, string>? Formatter { get; set; }

	/// <summary>Every word the chart writes. Null is English.</summary>
	[Parameter]
	public MokaChartText? Text { get; set; }

	/// <summary>
	///     When set, the chart compares its data by reference and this version instead of copying it, so a long series
	///     costs nothing to compare. Change it whenever the data changes, in place or not.
	/// </summary>
	[Parameter]
	public long? DataVersion { get; set; }

	/// <summary>The exported file's name, without its extension. Null uses the title, then the label, then "chart".</summary>
	[Parameter]
	public string? ExportName { get; set; }

	[CascadingParameter]
	private MokaTheme? CascadedTheme { get; set; }

	/// <summary>The look's defaults, resolved from <see cref="Look" /> and the theme.</summary>
	internal ChartLookSpec Spec { get; private set; } = ChartLookSpec.Standard;

	/// <summary>How many panels the chart draws; small multiples set more.</summary>
	internal int PanelCount { get; set; } = 1;

	/// <summary>Moves on every rebuild, so the script knows when the geometry it holds is stale.</summary>
	internal long GeometryVersion { get; private set; }

	/// <summary>The words, English unless <see cref="Text" /> is set.</summary>
	internal MokaChartText Words => Text ?? MokaChartText.Default;

	/// <summary>The theme the chart reads its look, texture and print from.</summary>
	internal MokaTheme? ChartTheme => CascadedTheme ?? Theme;

	/// <summary>The look in force: Standard on the original design.</summary>
	internal MokaChartLook ResolvedLook => ChartTheme?.Look == MokaLook.Matrix
		? MokaChartLook.Standard
		: Look ?? ChartTheme?.Charts ?? MokaChartLook.Standard;

	/// <summary>Whether marks are hatched on screen.</summary>
	internal bool Textured => ChartTheme?.Look != MokaLook.Matrix
	                          && (Texture ?? ChartTheme?.ChartTexture == MokaChartTexture.Hatched);

	/// <summary>How the chart prints: as shown on the original design.</summary>
	internal MokaChartPrint Print => ChartTheme is { Look: not MokaLook.Matrix } theme ? theme.ChartPrint : MokaChartPrint.AsShown;

	/// <summary>The tooltip in force.</summary>
	internal MokaChartTooltip ResolvedTooltip => Tooltip ?? Spec.Tooltip;

	/// <summary>Whether the keyboard reads the chart.</summary>
	internal bool ResolvedKeyboard => Keyboard ?? Spec.Keyboard;

	/// <summary>Whether a click pins the tooltip.</summary>
	internal bool ResolvedPinnable => Pinnable ?? Spec.Pinnable;

	/// <summary>The toolbar's controls in force.</summary>
	internal MokaChartControls ResolvedControls => Controls ?? Spec.Controls;

	/// <summary>Where the legend sits; none when <see cref="ShowLegend" /> is off.</summary>
	internal MokaChartLegendPosition ResolvedLegend => ShowLegend ? Legend ?? Spec.Legend : MokaChartLegendPosition.None;

	/// <summary>What marks each legend entry.</summary>
	internal MokaChartLegendKey ResolvedLegendKey => LegendKey ?? Spec.LegendKey;

	/// <summary>What the legend's entries do. Isolating needs hiding.</summary>
	internal MokaChartLegendActions ResolvedLegendActions =>
		(LegendActions ?? Spec.LegendActions) is var actions && actions.HasFlag(MokaChartLegendActions.Isolate)
			? actions | MokaChartLegendActions.Hide
			: actions;

	/// <summary>
	///     Whether the chart talks to its script: an interactive render of a modern chart whose tooltip or keyboard needs it.
	///     A static render and a classic chart never bind.
	/// </summary>
	internal bool Interactive => !IsClassic
	                             && (ResolvedTooltip is not (MokaChartTooltip.Native or MokaChartTooltip.None) || ResolvedKeyboard)
	                             && RendererInfo.IsInteractive;

	/// <summary>The state in force: <see cref="MokaChartState.Auto" /> worked out from the data.</summary>
	internal MokaChartState ResolvedState => State == MokaChartState.Auto
		? HasData ? MokaChartState.Ready : MokaChartState.Empty
		: State;

	/// <summary>The view showing.</summary>
	internal MokaChartView CurrentView => _view;

	/// <summary>The accessible name: <see cref="Label" />, else what the chart calls itself.</summary>
	internal string AccessibleLabel => Label ?? DefaultLabel;

	/// <summary>The classes a modern chart puts on its root: the look, and the texture and print when they apply.</summary>
	internal string ModernRootClasses => new CssBuilder("moka-chart")
		.AddClass(LookClass(ResolvedLook))
		.AddClass("moka-chart-texture", Textured)
		.AddClass(PrintClass(Print))
		.Build();

	/// <summary>Whether there is anything to draw.</summary>
	internal abstract bool HasData { get; }

	/// <summary>The series in legend order, with their slots and keys.</summary>
	internal abstract IReadOnlyList<ChartSeriesInfo> SeriesInfo { get; }

	/// <summary>Where a panel's drawn points sit, for the script; null when the marks carry <c>data-i</c> instead.</summary>
	internal virtual ChartHoverData? HoverData(int panel) => null;

	/// <summary>The table view and CSV, or null when the chart has none.</summary>
	internal virtual ChartTableModel? TableModel => null;

	/// <summary>Whether the chart draws exactly what it always has: no frame, no surface, no script.</summary>
	internal virtual bool IsClassic => false;

	/// <summary>
	///     Whether the chart lists its series itself, such as a donut's share list, so the frame draws no legend of its own.
	///     Its <see cref="SeriesInfo" /> still names them for hiding, highlighting and the keyboard.
	/// </summary>
	internal virtual bool DrawsOwnLegend => false;

	/// <summary>What the chart calls itself when it has no <see cref="Label" />.</summary>
	internal virtual string DefaultLabel => "Chart";

	/// <summary>Builds the geometry from the parameters. Called before a render whenever something it reads changed.</summary>
	internal abstract void Rebuild();

	/// <summary>What the tooltip and the keyboard cell say for a drawn point.</summary>
	/// <param name="panel">The panel.</param>
	/// <param name="series">The series' position in legend order, or -1 for a whole column.</param>
	/// <param name="drawn">The drawn point's position.</param>
	/// <param name="pinnedDrawn">The pinned point's position, if one is pinned.</param>
	internal abstract ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn);

	/// <summary>
	///     Whether a change to this parameter changes the geometry. The chrome the parts read as they render (the title,
	///     the state, the view, the toolbar, the callbacks) and the root's class, style and spacing don't; everything
	///     else does unless a chart says otherwise.
	/// </summary>
	internal virtual bool AffectsGeometry(string parameterName) => !ChromeParameters.Contains(parameterName);

	/// <summary>
	///     Whether a parameter's new value is the same as what the chart last drew from. Data parameters compare by value
	///     against a snapshot, so a list changed in place still counts. Not asked while <see cref="DataVersion" /> is set.
	/// </summary>
	internal virtual bool SameValue(string name, object? before, object? after) => Equals(before, after);

	/// <summary>Draws again, rebuilding the geometry first unless only chrome changed.</summary>
	internal void Redraw(bool geometry = true)
	{
		if (geometry)
		{
			RebuildNow();
		}

		_dirty = true;
		StateHasChanged();
	}

	/// <summary>A slot counting from 1, folded onto the palette's eight.</summary>
	internal static int PaletteSlot(int slot) => ((Math.Max(1, slot) - 1) % SlotCount) + 1;

	/// <summary>The class that colors a series: muted, an explicit tone, or its palette slot.</summary>
	internal static string SeriesClass(int slot, MokaColor? color, bool muted) => muted
		? "moka-chart-muted"
		: color is { } tone
			? $"moka-chart-tone-{MokaEnumHelpers.ToCssClass(tone)}"
			: $"moka-chart-c{PaletteSlot(slot).ToString(CultureInfo.InvariantCulture)}";

	/// <summary>
	///     An inline <c>--moka-chart-color</c> when a series' color isn't a class: its own color value, or the chart's
	///     <see cref="Colors" /> at its slot. Null otherwise, and for anything that isn't a CSS color.
	/// </summary>
	internal string? SeriesStyle(int slot, string? colorValue)
	{
		string? color = CssValues.IsColor(colorValue) ? colorValue.Trim() : null;
		if (color is null && Colors is { Count: > 0 } colors && colors[(Math.Max(1, slot) - 1) % colors.Count] is var own
		    && CssValues.IsColor(own))
		{
			color = own.Trim();
		}

		return color is null ? null : new StyleBuilder().AddStyle("--moka-chart-color", color).Build();
	}

	/// <summary>A series' slot: the one it pins, else its position in the full list counting from 1, so hiding never recolors.</summary>
	internal static int SlotFor(int index, int? explicitSlot) => explicitSlot is >= 1 ? explicitSlot.Value : index + 1;

	/// <summary>Whether the legend hid this series.</summary>
	internal bool IsHidden(string key) => _hidden.Contains(key);

	/// <summary>Whether a highlight elsewhere mutes this series.</summary>
	internal bool IsMuted(string key)
	{
		bool single = !string.IsNullOrEmpty(Highlight);
		bool several = HighlightSeries is { Count: > 0 };
		if (!single && !several)
		{
			return false;
		}

		return !(single && string.Equals(Highlight, key, StringComparison.Ordinal))
		       && !(several && HighlightSeries!.Contains(key));
	}

	/// <inheritdoc />
	/// <remarks>
	///     A parent that renders every second hands the chart its parameters every second. They are compared with the last
	///     set, data by value against a snapshot (or by reference and <see cref="DataVersion" />), and the chart draws again
	///     only when something differs, so an unchanged parent render costs no render and no diff.
	/// </remarks>
	public override Task SetParametersAsync(ParameterView parameters)
	{
		IReadOnlyDictionary<string, object?> next = parameters.ToDictionary();
		Compare(next);
		_lastParameters = next;
		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		Spec = ChartLookSpec.For(ResolvedLook);
		TakeHidden();
		TakeView();
		if (_geometryDirty)
		{
			_geometryDirty = false;
			RebuildNow();
		}
	}

	/// <inheritdoc />
	protected override bool ShouldRender()
	{
		bool render = _dirty;
		_dirty = false;
		return render;
	}

	// Blazor doesn't ask ShouldRender before the first render, so that render is marked as drawn here.
	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);
		if (firstRender)
		{
			_dirty = false;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (Interactive)
		{
			if (!SurfaceBound)
			{
				await BindAsync();
			}
			else if (SurfaceOptionsChanged())
			{
				await UpdateSurfaceAsync();
			}
		}
		else if (SurfaceBound)
		{
			await UnbindAsync();
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (SurfaceBound)
		{
			await UnbindAsync();
		}

		ReleaseSurfaceReference();
		await base.DisposeAsyncCore();
	}

	private void Compare(IReadOnlyDictionary<string, object?> next)
	{
		if (_lastParameters is null || _lastParameters.Count != next.Count)
		{
			_dirty = true;
			_geometryDirty = true;
			return;
		}

		bool versioned = next.TryGetValue(nameof(DataVersion), out object? version) && version is not null;
		foreach ((string name, object? value) in next)
		{
			if (!_lastParameters.TryGetValue(name, out object? previous))
			{
				_dirty = true;
				_geometryDirty = true;
				return;
			}

			bool same = versioned ? Equals(previous, value) : SameValue(name, previous, value);
			if (same)
			{
				continue;
			}

			_dirty = true;
			if (AffectsGeometry(name))
			{
				_geometryDirty = true;
				return;
			}
		}
	}

	private static string LookClass(MokaChartLook look) => look switch
	{
		MokaChartLook.Quiet => "moka-chart-look-quiet",
		MokaChartLook.Dense => "moka-chart-look-dense",
		MokaChartLook.Soft => "moka-chart-look-soft",
		MokaChartLook.Bold => "moka-chart-look-bold",
		_ => "moka-chart-look-standard"
	};

	private static string? PrintClass(MokaChartPrint print) => print switch
	{
		MokaChartPrint.Hatched => "moka-chart-print-hatched",
		MokaChartPrint.Ink => "moka-chart-print-ink",
		_ => null
	};

	// A held live chart keeps the geometry it showed, so the version stays and the script keeps what it pulled.
	private void RebuildNow()
	{
		_geometryKept = false;
		Rebuild();
		if (!_geometryKept)
		{
			GeometryVersion++;
		}

		_geometryKept = false;
	}

	// The parent's set is copied in only when it hands over a different one, so a legend click isn't undone by the next
	// parent render (gotcha 9).
	private void TakeHidden()
	{
		if (ReferenceEquals(HiddenSeries, _lastHiddenParameter))
		{
			return;
		}

		_lastHiddenParameter = HiddenSeries;
		_hidden.Clear();
		foreach (string key in HiddenSeries ?? [])
		{
			_hidden.Add(key);
		}
	}

	private void TakeView()
	{
		if (_lastViewParameter == View)
		{
			return;
		}

		_lastViewParameter = View;
		_view = View;
	}
}
