using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     The names and colors of a chart's series. Every modern chart draws one; on its own it is one row of toggles for
///     a whole dashboard: bind <see cref="Hidden" /> and pass the same set to each chart's <c>HiddenSeries</c>, and
///     give a series the same <see cref="MokaChartLegendItem.Slot" /> everywhere so it keeps its color. With
///     <see cref="MokaChartLegendActions.Hide" /> each entry is a toggle button, a double click, Alt click or Alt Enter
///     shows that series alone, and a Show all button brings everything back; each change is said in a polite status
///     region. More entries than <see cref="FoldAbove" /> keep to one row and fold what doesn't fit behind a "+N"
///     button that opens the legend onto more rows.
/// </summary>
public sealed partial class MokaChartLegend : MokaVisualComponentBase
{
	private const string FitModule = "./_content/Moka.Red.Core/moka-fit.js";

	private static readonly Dictionary<string, object?> FitLayout = new()
	{
		["sides"] = new[] { ":scope > .moka-chart-legend__items" },
		["more"] = ":scope > .moka-chart-legend__more [data-moka-more]"
	};

	private readonly Dictionary<string, (EventCallback<MouseEventArgs> Click, EventCallback<KeyboardEventArgs> KeyDown)>
		_handlers = new(StringComparer.Ordinal);

	private readonly HashSet<string> _hidden = new(StringComparer.Ordinal);

	// By key rather than on each entry: Blazor captures an element once, when it is made, and entries are rebuilt.
	private readonly Dictionary<string, ElementReference> _elements = new(StringComparer.Ordinal);

	private int _dropped;
	private DotNetObjectReference<MokaChartLegend>? _dotNetRef;
	private List<LegendEntry> _entries = [];
	private bool _expanded;
	private bool _fitBound;
	private bool _focusFirst;
	private string? _isolated;
	private (string Key, string[] Hidden, string? Isolated)? _beforeClick;
	private IReadOnlyCollection<string>? _lastHidden;
	private IReadOnlyDictionary<string, object?>? _lastParameters;
	private bool _parametersMoved = true;
	private bool _keyboardIsolated;
	private ElementReference _root;
	private string _said = string.Empty;
	private bool _stateMoved;

	/// <summary>The entries, in order.</summary>
	[Parameter]
	public IReadOnlyList<MokaChartLegendItem>? Items { get; set; }

	/// <summary>The keys of the hidden series. Two-way bindable; pass a new collection to change it from outside.</summary>
	[Parameter]
	public IReadOnlyCollection<string>? Hidden { get; set; }

	/// <summary>Raised with the new set of hidden keys whenever an entry hides or shows a series.</summary>
	[Parameter]
	public EventCallback<IReadOnlyCollection<string>> HiddenChanged { get; set; }

	/// <summary>What the entries do. Defaults to nothing: they are names. Isolating turns hiding on too.</summary>
	[Parameter]
	public MokaChartLegendActions Actions { get; set; }

	/// <summary>What marks each entry. Defaults to a dot.</summary>
	[Parameter]
	public MokaChartLegendKey Key { get; set; }

	/// <summary>A row that wraps, or a column. Defaults to a row.</summary>
	[Parameter]
	public MokaDirection Orientation { get; set; } = MokaDirection.Row;

	/// <summary>
	///     Charts with the same <c>SyncGroup</c> dim their other series while an entry is pointed at, when
	///     <see cref="Actions" /> has <see cref="MokaChartLegendActions.Highlight" />.
	/// </summary>
	[Parameter]
	public string? SyncGroup { get; set; }

	/// <summary>The words the legend writes and says. Null is English.</summary>
	[Parameter]
	public MokaChartText? Text { get; set; }

	/// <summary>The legend's name for screen readers. Null takes <see cref="Title" />, else <see cref="MokaChartText.Series" />.</summary>
	[Parameter]
	public string? AriaLabel { get; set; }

	/// <summary>A small label before the entries, such as "Services" over a dashboard's toggles.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>
	///     With more entries than this, a row keeps to one line and what doesn't fit drops behind a "+N" button that
	///     opens the legend onto more rows. Defaults to 6. Zero lets the entries wrap however many there are. Needs an
	///     interactive render; without one the entries wrap.
	/// </summary>
	[Parameter]
	public int FoldAbove { get; set; } = 6;

	/// <summary>Names the "+N" button from how many entries it holds. Null takes <see cref="MokaChartText.MoreSeries" />.</summary>
	[Parameter]
	public Func<int, string>? MoreLabel { get; set; }

	/// <summary>Names the button that folds the legend back. Null takes <see cref="MokaChartText.FewerSeries" />.</summary>
	[Parameter]
	public string? LessLabel { get; set; }

	/// <summary>Monospace names in small capitals with their values beside them, for a title row.</summary>
	[Parameter]
	public bool Dense { get; set; }

	/// <summary>Hatches box keys, as a chart with texture hatches its marks.</summary>
	[Parameter]
	public bool Texture { get; set; }

	/// <summary>Classes added to every entry.</summary>
	[Parameter]
	public string? ItemClass { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-chart-legend";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-chart-legend--column", Orientation is MokaDirection.Column or MokaDirection.ColumnReverse)
		.AddClass("moka-chart-legend--toggles", Toggles)
		.AddClass("moka-chart-legend--pills", Key == MokaChartLegendKey.Pill)
		.AddClass("moka-chart-legend--dense", Dense)
		.AddClass("moka-chart-legend--folds", Folds)
		.AddClass("moka-chart-texture", Texture)
		.AddClass(Class)
		.Build();

	private MokaChartText Words => Text ?? MokaChartText.Default;

	private MokaChartLegendActions ResolvedActions =>
		Actions.HasFlag(MokaChartLegendActions.Isolate) ? Actions | MokaChartLegendActions.Hide : Actions;

	private bool Toggles => ResolvedActions.HasFlag(MokaChartLegendActions.Hide);

	private bool Isolates => ResolvedActions.HasFlag(MokaChartLegendActions.Isolate);

	private string? HighlightAttribute => ResolvedActions.HasFlag(MokaChartLegendActions.Highlight) ? string.Empty : null;

	private bool Folds => FoldAbove > 0 && _entries.Count > FoldAbove
	                      && Orientation is not (MokaDirection.Column or MokaDirection.ColumnReverse);

	private string? ExpandedAttribute => _expanded ? "true" : null;

	private string GroupName => !string.IsNullOrWhiteSpace(AriaLabel) ? AriaLabel
		: !string.IsNullOrWhiteSpace(Title) ? Title
		: Words.Series;

	private string MoreText => "+" + _dropped.ToString(CultureInfo.CurrentCulture);

	private string LessName => LessLabel ?? Words.FewerSeries;

	private string MoreName => _expanded
		? LessName
		: MoreLabel?.Invoke(_dropped) ?? Words.MoreSeries(_dropped);

	/// <summary>Called by the legend's script when the number of entries that didn't fit changes.</summary>
	/// <param name="count">How many entries are folded away.</param>
	/// <returns>A task that completes once the legend has been asked to redraw.</returns>
	[JSInvokable]
	public Task OnDroppedChanged(int count) =>
		InvokeAsync(() =>
		{
			_dropped = count;
			_expanded = _expanded && count > 0;
			Changed();
		});

	/// <inheritdoc />
	/// <remarks>
	///     A chart draws its legend every time it draws, so the parameters are compared with the last set and the legend
	///     draws again only when one differs or it changed itself.
	/// </remarks>
	public override Task SetParametersAsync(ParameterView parameters)
	{
		IReadOnlyDictionary<string, object?> next = parameters.ToDictionary();
		_parametersMoved = _parametersMoved || _lastParameters is null || !SameParameters(_lastParameters, next);
		_lastParameters = next;
		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		TakeHidden();
		if (_parametersMoved)
		{
			BuildEntries();
		}
	}

	/// <inheritdoc />
	protected override bool ShouldRender()
	{
		bool render = _parametersMoved || _stateMoved;
		_parametersMoved = false;
		_stateMoved = false;
		return render;
	}

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);
		if (firstRender)
		{
			_parametersMoved = false;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (_focusFirst)
		{
			_focusFirst = false;
			if (_entries.Count > 0)
			{
				if (_elements.TryGetValue(_entries[0].Key, out ElementReference first))
				{
					await SafeFocusAsync(first);
				}
			}
		}

		bool fit = Folds && RendererInfo.IsInteractive;
		if (fit == _fitBound)
		{
			return;
		}

		_fitBound = fit;
		if (fit)
		{
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await SafeModuleInvokeVoidAsync(FitModule, "bindFit", _root, _dotNetRef, FitLayout);
		}
		else
		{
			_dropped = 0;
			_expanded = false;
			await SafeModuleInvokeVoidAsync(FitModule, "unbindFit", _root);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_fitBound)
		{
			await SafeModuleInvokeVoidAsync(FitModule, "unbindFit", _root);
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	private bool IsHidden(string key) => _hidden.Contains(key);

	// The legend's own handlers change what it shows and say why, and draw it only when they did.
	async Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg)
	{
		await callback.InvokeAsync(arg);
		if (_stateMoved)
		{
			StateHasChanged();
		}
	}

	private void Changed()
	{
		_stateMoved = true;
		StateHasChanged();
	}

	private void TakeHidden()
	{
		if (ReferenceEquals(Hidden, _lastHidden))
		{
			return;
		}

		_lastHidden = Hidden;
		_hidden.Clear();
		foreach (string key in Hidden ?? [])
		{
			_hidden.Add(key);
		}
	}

	private void BuildEntries()
	{
		List<LegendEntry> entries = [];
		HashSet<string> seen = new(StringComparer.Ordinal);
		IReadOnlyList<MokaChartLegendItem> items = Items ?? [];
		for (int i = 0; i < items.Count; i++)
		{
			MokaChartLegendItem item = items[i];
			if (item is null || string.IsNullOrEmpty(item.Key) || !seen.Add(item.Key))
			{
				continue;
			}

			entries.Add(Entry(item, MokaChartBase.SlotFor(i, item.Slot)));
		}

		_entries = entries;
		foreach (string stale in _handlers.Keys.Where(key => !seen.Contains(key)).ToList())
		{
			_handlers.Remove(stale);
			_elements.Remove(stale);
		}
	}

	private LegendEntry Entry(MokaChartLegendItem item, int slot)
	{
		string key = item.Key;
		if (!_handlers.TryGetValue(key, out (EventCallback<MouseEventArgs> Click, EventCallback<KeyboardEventArgs> KeyDown) handlers))
		{
			handlers = (EventCallback.Factory.Create<MouseEventArgs>(this, e => ClickAsync(key, e)),
				EventCallback.Factory.Create<KeyboardEventArgs>(this, e => KeyDownAsync(key, e)));
			_handlers[key] = handlers;
		}

		// Only isolating reads keys, and a listener is a round trip for every key pressed on an entry.
		EventCallback<KeyboardEventArgs> keyDown = Isolates ? handlers.KeyDown : default;

		string color = MokaChartBase.SeriesClass(slot, item.Color, false);
		string? style = CssValues.IsColor(item.ColorValue)
			? new StyleBuilder().AddStyle("--moka-chart-color", item.ColorValue.Trim()).Build()
			: null;

		// A dashed line tells a comparison from the series it is drawn in the color of, so it shows whatever the key.
		MokaChartLegendMark mark = Key == MokaChartLegendKey.Mark || item.Mark == MokaChartLegendMark.Dash
			? item.Mark
			: MokaChartLegendMark.Dot;
		string keyClasses = new CssBuilder("moka-chart-legend__key")
			.AddClass(item.Icon is not null ? "moka-chart-legend__key--icon" : mark switch
			{
				MokaChartLegendMark.Line => "moka-chart-legend__key--line",
				MokaChartLegendMark.Box => "moka-chart-legend__key--box moka-chart-hatch",
				MokaChartLegendMark.Dash => "moka-chart-legend__key--dash",
				_ => "moka-chart-legend__key--dot"
			})
			.Build();

		string classes = new CssBuilder("moka-chart-legend__item")
			.AddClass(color)
			.AddClass(ItemClass)
			.Build();

		return new LegendEntry(key, item.Name, item.Value, classes, style, keyClasses,
			MokaChartBase.PaletteSlot(slot).ToString(CultureInfo.InvariantCulture), handlers.Click, keyDown)
		{
			Icon = item.Icon
		};
	}

	private string NameOf(string key) => _entries.Find(entry => entry.Key == key)?.Name ?? key;

	private async Task ClickAsync(string key, MouseEventArgs e)
	{
		// Alt Enter was handled on its keydown; a browser that also clicks for it sends a click with no pointer.
		if (_keyboardIsolated && e.Detail == 0)
		{
			_keyboardIsolated = false;
			return;
		}

		_keyboardIsolated = false;
		if (Isolates && (e.AltKey || e.Detail == 2))
		{
			// A double click's first click toggled the entry; put that back so the double click acts on what was there
			// before it, and a second double click on an isolated entry brings the rest back as Alt Enter does.
			if (e.Detail == 2 && _beforeClick is { } before && before.Key == key)
			{
				_hidden.Clear();
				_hidden.UnionWith(before.Hidden);
				_isolated = before.Isolated;
			}

			_beforeClick = null;
			await IsolateAsync(key);
			return;
		}

		_beforeClick = Isolates ? (key, [.. _hidden], _isolated) : null;
		await ToggleAsync(key);
	}

	private async Task KeyDownAsync(string key, KeyboardEventArgs e)
	{
		_keyboardIsolated = false;
		if (Isolates && e.AltKey && e.Key == "Enter" && !e.Repeat)
		{
			_keyboardIsolated = true;
			await IsolateAsync(key);
		}
	}

	private Task ToggleAsync(string key)
	{
		_isolated = null;
		bool hide = _hidden.Add(key);
		if (!hide)
		{
			_hidden.Remove(key);
		}

		int total = _entries.Count;
		int shown = _entries.Count(entry => !_hidden.Contains(entry.Key));
		Say(hide ? Words.SeriesHidden(NameOf(key), shown, total) : Words.SeriesShown(NameOf(key), shown, total));
		return CommitAsync();
	}

	private Task IsolateAsync(string key)
	{
		int total = _entries.Count;
		if (_isolated == key && !_hidden.Contains(key) && _hidden.Count == total - 1)
		{
			_isolated = null;
			_hidden.Clear();
			Say(Words.AllShown(total));
			return CommitAsync();
		}

		_isolated = key;
		_hidden.Clear();
		foreach (LegendEntry entry in _entries.Where(entry => entry.Key != key))
		{
			_hidden.Add(entry.Key);
		}

		Say(Words.SeriesIsolated(NameOf(key)));
		return CommitAsync();
	}

	private Task ShowAllAsync()
	{
		_isolated = null;
		_hidden.Clear();
		_focusFirst = true;
		Say(Words.AllShown(_entries.Count));
		return CommitAsync();
	}

	private void Say(string sentence)
	{
		_said = sentence;
		_stateMoved = true;
	}

	private Task CommitAsync()
	{
		_stateMoved = true;
		IReadOnlyCollection<string> hidden = [.. _hidden];
		return HiddenChanged.InvokeAsync(hidden);
	}

	private void ToggleExpanded()
	{
		_expanded = !_expanded;
		_stateMoved = true;
	}

	private static bool SameParameters(IReadOnlyDictionary<string, object?> before, IReadOnlyDictionary<string, object?> after)
	{
		if (before.Count != after.Count)
		{
			return false;
		}

		foreach ((string name, object? value) in after)
		{
			if (!before.TryGetValue(name, out object? previous))
			{
				return false;
			}

			bool same = name == nameof(Items)
				? ReferenceEquals(previous, value)
				  || (previous is IReadOnlyList<MokaChartLegendItem> a && value is IReadOnlyList<MokaChartLegendItem> b
				                                                    && a.SequenceEqual(b))
				: Equals(previous, value);
			if (!same)
			{
				return false;
			}
		}

		return true;
	}

	private sealed class LegendEntry(
		string key,
		string name,
		string? value,
		string classes,
		string? style,
		string keyClasses,
		string slotText,
		EventCallback<MouseEventArgs> click,
		EventCallback<KeyboardEventArgs> keyDown)
	{
		public string Key { get; } = key;

		public string Name { get; } = name;

		public string? Value { get; } = value;

		public string Classes { get; } = classes;

		public string? Style { get; } = style;

		public string KeyClasses { get; } = keyClasses;

		public string SlotText { get; } = slotText;

		public EventCallback<MouseEventArgs> Click { get; } = click;

		public EventCallback<KeyboardEventArgs> KeyDown { get; } = keyDown;

		public MokaIconDefinition? Icon { get; init; }
	}
}
