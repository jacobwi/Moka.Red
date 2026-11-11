using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Utilities;
using Moka.Red.Data.Table;
using Moka.Red.Forms.RangeBar;

namespace Moka.Red.Data.Chart;

/// <summary>
///     Shared chart chrome around the plot: the header with the title, the toolbar and the actions, the legend in its
///     place, the loading, empty and error states drawn over the plot at its own size, the table view, the range strip
///     and an export failure. Every modern chart draws itself inside one, passing itself as <see cref="Chart" />; the
///     frame reads the rest from the chart. Not meant to be used on its own, and draws nothing without a chart.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed partial class MokaChartFrame : ComponentBase
{
	private static readonly IReadOnlyList<int> TablePageSizes = [25, 50, 100];

	private readonly EventCallback<IReadOnlyCollection<string>> _hiddenChanged;
	private readonly EventCallback<MouseEventArgs> _exportPng;
	private readonly EventCallback<MouseEventArgs> _exportSvg;
	private readonly EventCallback<MouseEventArgs> _exportCsv;
	private readonly EventCallback<KeyboardEventArgs> _escapeTable;
	private readonly Func<Task> _resetZoom;
	private readonly Func<Task> _toggleView;

	private ElementReference _root;
	private ElementReference _main;
	private MokaTable<int>? _table;
	private bool _exportOpen;
	private bool _focusAfterTable;
	private long _tableVersion = -1;
	private long _columnsVersion = -1;
	private bool[] _numeric = [];
	private (long Version, string? Column, MokaSortDirection Direction, int[] Order)? _sorted;

	/// <summary>Builds the handlers once, so a render hands the same ones to the controls.</summary>
	public MokaChartFrame()
	{
		_hiddenChanged = EventCallback.Factory.Create<IReadOnlyCollection<string>>(this, HiddenChosenAsync);
		_exportPng = EventCallback.Factory.Create<MouseEventArgs>(this, () => ExportAsync(MokaChartControls.ExportPng));
		_exportSvg = EventCallback.Factory.Create<MouseEventArgs>(this, () => ExportAsync(MokaChartControls.ExportSvg));
		_exportCsv = EventCallback.Factory.Create<MouseEventArgs>(this, () => ExportAsync(MokaChartControls.ExportCsv));
		_escapeTable = EventCallback.Factory.Create<KeyboardEventArgs>(this, EscapeTableAsync);
		_resetZoom = ResetZoomAsync;
		_toggleView = ToggleViewAsync;
	}

	/// <summary>The chart this frame belongs to.</summary>
	[Parameter]
	public MokaChartBase? Chart { get; set; }

	/// <summary>The chart's body: its plot.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Drawn under the body, above a bottom legend.</summary>
	[Parameter]
	public RenderFragment? Below { get; set; }

	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	private bool Interactive => RendererInfo.IsInteractive;

	private MokaChartState State => Chart?.ResolvedState ?? MokaChartState.Ready;

	private string? StateAttribute => State switch
	{
		MokaChartState.Loading => "loading",
		MokaChartState.Empty => "empty",
		MokaChartState.Error => "error",
		_ => null
	};

	private string FrameClass => new CssBuilder("moka-chart-frame")
		.AddClass("moka-chart-frame--table", ShowsTable)
		.AddClass($"moka-chart-frame--{StateAttribute}", StateAttribute is not null)
		.Build();

	private string TitleClass => new CssBuilder("moka-chart-frame__title")
		.AddClass(Chart?.Spec.TitleStyle switch
		{
			ChartTitleStyle.Micro => "moka-chart-frame__title--micro moka-micro-label",
			ChartTitleStyle.Large => "moka-chart-frame__title--large",
			_ => null
		})
		.Build();

	private string BodyClass => new CssBuilder("moka-chart-frame__body")
		.AddClass("moka-chart-frame__body--right", LegendAt == MokaChartLegendPosition.Right)
		.Build();

	private string ChartClass => new CssBuilder("moka-chart-frame__chart")
		.AddClass("moka-chart-frame__chart--hidden", ShowsTable || State is MokaChartState.Empty or MokaChartState.Error)
		.Build();

	private bool HasTitles => Chart is not null
	                          && (Chart.TitleContent is not null || !string.IsNullOrEmpty(Chart.Title)
	                              || !string.IsNullOrEmpty(Chart.Subtitle));

	private bool HasTools => Chart is not null
	                         && (Chart.Actions is not null
	                             || (Chart.ResolvedControls.HasFlag(MokaChartControls.Legend) && LegendItemsShown.Count > 0)
	                             || (Chart.ResolvedControls.HasFlag(MokaChartControls.Ranges) && Chart.Presets.Count > 0)
	                             || (Chart.ResolvedControls.HasFlag(MokaChartControls.ResetZoom) && Chart is MokaCartesianChartBase)
	                             || (Chart.ResolvedControls.HasFlag(MokaChartControls.View) && Chart.TableModel is not null)
	                             || Chart.ExportFormats != MokaChartControls.None);

	private bool HasHeader => HasTitles || HasTools || ShowsLive || LegendAt == MokaChartLegendPosition.TitleRow;

	private bool ShowsLive => Chart is { Interactive: true, LiveHold: true };

	// The legend goes where the chart says, unless the toolbar holds it.
	private MokaChartLegendPosition LegendAt =>
		Chart is null || Chart.ResolvedControls.HasFlag(MokaChartControls.Legend) || LegendItemsShown.Count == 0
			? MokaChartLegendPosition.None
			: Chart.ResolvedLegend;

	// A legend that can't show a hidden series again leaves it out, as the chart does. A chart that lists its own
	// series has no frame legend, and one series is named by the title, so it has none either unless the app asked
	// for a legend, the entry shows a value, or the series is hidden and the legend is the way back.
	private IReadOnlyList<MokaChartLegendItem> LegendItemsShown
	{
		get
		{
			if (Chart is null || Chart.ResolvedLegend == MokaChartLegendPosition.None || Chart.DrawsOwnLegend)
			{
				return [];
			}

			IReadOnlyList<MokaChartLegendItem> items = Chart.LegendItems;
			if (items is [{ Value: null } only] && Chart.Legend is null && !Chart.IsHidden(only.Key))
			{
				return [];
			}

			return Chart.ResolvedLegendActions.HasFlag(MokaChartLegendActions.Hide)
				? items
				: [.. items.Where(item => !Chart.IsHidden(item.Key))];
		}
	}

	private bool ShowsTable => Chart is { CurrentView: MokaChartView.Table, TableModel: not null };

	private string TableName => $"{Chart?.AccessibleLabel}, {Chart?.Words.TableView}";

	private IReadOnlyList<string> TableHeaders => Chart?.TableModel?.Headers ?? [];

	private string ViewValue => Chart?.CurrentView == MokaChartView.Table ? "table" : "chart";

	private string EmptyText => Chart?.EmptyText ?? Chart?.Words.NoData ?? string.Empty;

	private string ErrorText => Chart?.ErrorText ?? Chart?.Words.LoadFailed ?? MokaChartText.Default.LoadFailed;

	private MokaChartBase.ChartStrip? Strip =>
		Chart is { CurrentView: MokaChartView.Chart } chart && !chart.IsClassic ? chart.Strip : null;

	private EventCallback<KeyboardEventArgs> MainKeyDown => ShowsTable ? _escapeTable : default;

	// Said once when the state changes. The error state's alert speaks for itself.
	private string StateSentence => State switch
	{
		MokaChartState.Loading => Chart?.Words.Updating ?? string.Empty,
		MokaChartState.Empty => EmptyText,
		MokaChartState.Error when Chart?.ErrorContent is not null => ErrorText,
		_ => string.Empty
	};

	private ChartSeriesInfo? OverviewSeries => Chart?.SeriesInfo.FirstOrDefault(series => !series.Hidden);

	private string OverviewClass => new CssBuilder("moka-chart-frame__overview-host")
		.AddClass(OverviewSeries is { } series ? MokaChartBase.SeriesClass(series.Slot, null, false) : "moka-chart-c1")
		.Build();

	private string? OverviewStyle => OverviewSeries?.ColorStyle;

	// Handlers here change the chart, and the chart redraws itself; the frame renders for an event only when it asks.
	Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (Chart is null)
		{
			return;
		}

		Chart.FrameElement = _root;

		if (_focusAfterTable)
		{
			_focusAfterTable = false;
			await Chart.FocusAfterTableAsync(_main);
		}

		if (!ShowsTable)
		{
			_tableVersion = -1;
			return;
		}

		// The table loads itself when it first shows; after that it is asked again whenever the chart's data moves.
		long version = Chart.GeometryVersion;
		if (_tableVersion < 0)
		{
			_tableVersion = version;
		}
		else if (_tableVersion != version && _table is not null)
		{
			_tableVersion = version;
			await _table.RefreshAsync();
		}
	}

	private RenderFragment Control(MokaChartControls which, bool enabled, Func<Task>? run, RenderFragment builtIn) =>
		Chart?.ToolbarControl is { } custom
			? custom(new MokaChartControlContext(which, enabled, run, builtIn))
			: builtIn;

	private Task HiddenChosenAsync(IReadOnlyCollection<string> hidden) =>
		Chart?.SetHiddenAsync(hidden) ?? Task.CompletedTask;

	private Task ViewChosen(string? value) =>
		Chart?.SetViewAsync(value == "table" ? MokaChartView.Table : MokaChartView.Chart) ?? Task.CompletedTask;

	private Task ToggleViewAsync() =>
		Chart?.SetViewAsync(Chart.CurrentView == MokaChartView.Table ? MokaChartView.Chart : MokaChartView.Table)
		?? Task.CompletedTask;

	private async Task EscapeTableAsync(KeyboardEventArgs e)
	{
		if (e.Key != "Escape" || Chart is null)
		{
			return;
		}

		_focusAfterTable = true;
		await Chart.SetViewAsync(MokaChartView.Chart);
	}

	private Task ExportAsync(MokaChartControls format) => Chart?.ExportAsync(format) ?? Task.CompletedTask;

	private void ExportOpenChanged(bool open)
	{
		_exportOpen = open;
		StateHasChanged();
	}

	private Task PresetChosen(string? label)
	{
		TimeProvider clock = Services.GetService<TimeProvider>() ?? TimeProvider.System;
		return Chart?.ChoosePresetAsync(label, clock.GetUtcNow()) ?? Task.CompletedTask;
	}

	private Task ResetZoomAsync() => Chart?.ResetZoomAsync() ?? Task.CompletedTask;

	private Task RetryAsync() => Chart?.OnRetry.InvokeAsync() ?? Task.CompletedTask;

	private double StripStart(MokaChartBase.ChartStrip strip) =>
		Chart?.CurrentRange is { } range ? Math.Clamp(Math.Min(range.From.Value, range.To.Value), strip.Min, strip.Max) : strip.Min;

	private double StripEnd(MokaChartBase.ChartStrip strip) =>
		Chart?.CurrentRange is { } range ? Math.Clamp(Math.Max(range.From.Value, range.To.Value), strip.Min, strip.Max) : strip.Max;

	private Task StripChanged(MokaRange range) => Chart?.ChooseRangeAsync(range.Start, range.End) ?? Task.CompletedTask;

	private string StripText(double value) => Chart?.StripText(value) ?? value.ToString(CultureInfo.CurrentCulture);

	private bool IsNumeric(int column)
	{
		if (Chart?.TableModel is not { } model)
		{
			return false;
		}

		if (_columnsVersion != Chart.GeometryVersion || _numeric.Length != model.Headers.Count)
		{
			_columnsVersion = Chart.GeometryVersion;
			_numeric = new bool[model.Headers.Count];
			for (int c = 0; c < _numeric.Length; c++)
			{
				_numeric[c] = ReadsAsNumbers(model, c);
			}
		}

		return column < _numeric.Length && _numeric[column];
	}

	private static bool ReadsAsNumbers(ChartTableModel model, int column)
	{
		int looked = 0;
		for (int row = 0; row < model.RowCount && looked < 8; row++)
		{
			string raw = model.Raw(row, column);
			if (string.IsNullOrWhiteSpace(raw))
			{
				continue;
			}

			looked++;
			if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
			{
				return false;
			}
		}

		return looked > 0;
	}

	private string CellText(int row, int column) =>
		Chart?.TableModel is { } model && row < model.RowCount && column < model.Headers.Count
			? model.Display(row, column)
			: string.Empty;

	// The rows are indices into the chart's table, a page at a time, so a million rows cost one page of DOM. A sort
	// orders the indices once per sort and data version.
	private Task<MokaTableResult<int>> LoadRowsAsync(MokaTableState state)
	{
		if (Chart?.TableModel is not { } model)
		{
			return Task.FromResult(new MokaTableResult<int> { Items = [], TotalItems = 0 });
		}

		int[]? order = Order(model, state.SortColumn, state.SortDirection);
		int size = Math.Max(1, state.PageSize);
		int start = Math.Clamp((Math.Max(1, state.Page) - 1) * size, 0, model.RowCount);
		int count = Math.Min(size, model.RowCount - start);
		int[] page = new int[count];
		for (int i = 0; i < count; i++)
		{
			page[i] = order is null ? start + i : order[start + i];
		}

		return Task.FromResult(new MokaTableResult<int> { Items = page, TotalItems = model.RowCount });
	}

	private int[]? Order(ChartTableModel model, string? column, MokaSortDirection direction)
	{
		int index = column is null ? -1 : IndexOf(model.Headers, column);
		if (index < 0 || direction == MokaSortDirection.None || Chart is null)
		{
			return null;
		}

		long version = Chart.GeometryVersion;
		if (_sorted is { } kept && kept.Version == version && kept.Column == column && kept.Direction == direction
		    && kept.Order.Length == model.RowCount)
		{
			return kept.Order;
		}

		int[] order = new int[model.RowCount];
		for (int i = 0; i < order.Length; i++)
		{
			order[i] = i;
		}

		if (IsNumeric(index))
		{
			double[] keys = new double[order.Length];
			for (int i = 0; i < keys.Length; i++)
			{
				keys[i] = double.TryParse(model.Raw(i, index), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
					? value
					: double.NaN;
			}

			Array.Sort(keys, order);
		}
		else
		{
			string[] keys = new string[order.Length];
			for (int i = 0; i < keys.Length; i++)
			{
				keys[i] = model.Raw(i, index);
			}

			Array.Sort(keys, order, StringComparer.CurrentCulture);
		}

		if (direction == MokaSortDirection.Descending)
		{
			Array.Reverse(order);
		}

		_sorted = (version, column, direction, order);
		return order;
	}

	private static int IndexOf(IReadOnlyList<string> headers, string column)
	{
		for (int i = 0; i < headers.Count; i++)
		{
			if (string.Equals(headers[i], column, StringComparison.Ordinal))
			{
				return i;
			}
		}

		return -1;
	}
}
