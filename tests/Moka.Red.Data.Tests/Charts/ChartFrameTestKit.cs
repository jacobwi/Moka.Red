using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>What the frame, legend and export tests share: the modules they read back, and a chart with a frame.</summary>
internal static class ChartFrameTestKit
{
	/// <summary>Core's fitting module, which the toolbar and a folding legend bind.</summary>
	public const string FitModule = "./_content/Moka.Red.Core/moka-fit.js";

	/// <summary>The export module, imported only when an export is picked.</summary>
	public const string ExportModule = "./_content/Moka.Red.Data/moka-chart-export.js";

	/// <summary>Monday 2026-03-02 at midnight UTC, where the test week starts.</summary>
	public static readonly DateTimeOffset WeekStart = new(2026, 3, 2, 0, 0, 0, TimeSpan.Zero);

	/// <summary>
	///     Renders statically, with every other script answering, since the table and the controls bind theirs after
	///     render even in bUnit's static renderer.
	/// </summary>
	public static void StaticFrame(this BunitContext context)
	{
		ArgumentNullException.ThrowIfNull(context);
		context.Static();
		context.JSInterop.Mode = JSRuntimeMode.Loose;
	}

	/// <summary>Renders as Blazor Server does, with every script answering, which the test can read back.</summary>
	/// <returns>The fitting and export modules.</returns>
	public static (BunitJSModuleInterop Fit, BunitJSModuleInterop Export) InteractiveFrame(this BunitContext context)
	{
		ArgumentNullException.ThrowIfNull(context);
		context.Interactive();
		context.JSInterop.Mode = JSRuntimeMode.Loose;
		BunitJSModuleInterop fit = context.JSInterop.SetupModule(FitModule);
		fit.Mode = JSRuntimeMode.Loose;
		BunitJSModuleInterop export = context.JSInterop.SetupModule(ExportModule);
		export.Mode = JSRuntimeMode.Loose;
		return (fit, export);
	}

	/// <summary>A week of hourly times, one per value.</summary>
	/// <param name="count">How many.</param>
	public static IReadOnlyList<DateTimeOffset> Hours(int count) =>
		[.. Enumerable.Range(0, count).Select(hour => WeekStart.AddHours(hour))];
}

/// <summary>
///     A chart with an x axis built only for tests: it draws its frame around a plain plot, keeps a table of its values,
///     and keeps the zoom the way a chart with an x axis does, recording each span it is asked for.
/// </summary>
public sealed class FrameChart : MokaCartesianChartBase
{
	private readonly List<(double From, double To)> _chosen = [];
	private MokaChartRange? _range;
	private bool _rangeChosen;

	/// <summary>The series' keys, in legend order.</summary>
	[Parameter]
	public IReadOnlyList<string>? Keys { get; set; }

	/// <summary>Each series' values, one list per key.</summary>
	[Parameter]
	public IReadOnlyList<IReadOnlyList<double>>? Data { get; set; }

	/// <summary>Whether the chart keeps no table of its values.</summary>
	[Parameter]
	public bool NoTable { get; set; }

	/// <summary>What the range strip draws behind its window.</summary>
	[Parameter]
	public IReadOnlyList<double>? Overview { get; set; }

	/// <summary>Every span the frame chose, NaN and NaN for a reset.</summary>
	public IReadOnlyList<(double From, double To)> Chosen => _chosen;

	/// <summary>How many times it rendered.</summary>
	public int Renders { get; private set; }

	/// <summary>What the base and the frame read, through the friend assembly.</summary>
	internal MokaChartBase Base => this;

	/// <inheritdoc />
	protected override string RootClass => "frame-chart";

	internal override bool HasData => Data is { Count: > 0 } data && data.Any(values => values.Count > 0);

	internal override int CategoryCount => Data is { Count: > 0 } data ? data.Max(values => values.Count) : 0;

	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo =>
	[
		.. (Keys ?? []).Select((key, i) => new ChartSeriesInfo(key, key, SlotFor(i, null),
			SeriesClass(SlotFor(i, null), null, IsMuted(key)), SeriesStyle(SlotFor(i, null), null), ChartMark.Line,
			IsHidden(key), IsMuted(key), null))
	];

	internal override ChartTableModel? TableModel => NoTable || Keys is null || Data is null
		? null
		: new ChartTableModel(
			[Times is null ? "Point" : "Time", .. Keys],
			CategoryCount,
			(row, column) => column == 0 ? FirstCell(row, false) : Cell(row, column - 1).ToString("N1", CultureInfo.CurrentCulture),
			(row, column) => column == 0 ? FirstCell(row, true) : Cell(row, column - 1).ToString("R", CultureInfo.InvariantCulture));

	internal override IReadOnlyList<double>? OverviewValues => Overview;

	internal override MokaChartRange? CurrentRange => _rangeChosen ? _range : Range;

	internal override MokaChartZoom ZoomGestures => Zoom ?? MokaChartZoom.None;

	internal override void Rebuild()
	{
	}

	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn) => null;

	internal override Task RangeChosenAsync(double from, double to)
	{
		_chosen.Add((from, to));
		_rangeChosen = true;
		_range = double.IsNaN(from) ? null : new MokaChartRange(new MokaChartX(from, Times is not null), new MokaChartX(to, Times is not null));
		Redraw();
		return RangeChanged.InvokeAsync(_range);
	}

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		Renders++;
		builder.OpenElement(0, "div");
		builder.AddAttribute(1, "class", CssClass);
		builder.AddElementReferenceCapture(2, element => RootElement = element);
		builder.OpenComponent<MokaChartFrame>(3);
		builder.AddAttribute(4, nameof(MokaChartFrame.Chart), this);
		builder.AddAttribute(5, nameof(MokaChartFrame.ChildContent), (RenderFragment)(plot =>
		{
			plot.OpenElement(0, "div");
			plot.AddAttribute(1, "class", "test-plot");
			plot.AddContent(2, CategoryCount);
			plot.CloseElement();
		}));
		builder.CloseComponent();
		builder.CloseElement();
	}

	private double Cell(int row, int series) =>
		Data is { } data && series < data.Count && row < data[series].Count ? data[series][row] : double.NaN;

	private string FirstCell(int row, bool raw) => Times is { } times && row < times.Count
		? raw ? times[row].ToString("O", CultureInfo.InvariantCulture) : times[row].ToString("t", CultureInfo.CurrentCulture)
		: row.ToString(CultureInfo.InvariantCulture);
}
