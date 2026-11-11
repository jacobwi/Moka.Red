using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>A series of the surface test chart.</summary>
/// <param name="Key">Its key.</param>
/// <param name="Values">Its values.</param>
public sealed record SurfaceSeries(string Key, IReadOnlyList<double> Values);

/// <summary>
///     A modern chart built only for the surface tests: a plot box with one <see cref="MokaChartSurface" /> per panel,
///     hover geometry from its values, and tooltips that say which point they describe. It counts its renders and
///     rebuilds, so a test can tell the chart's renders from the host's.
/// </summary>
public sealed class ChartSurfaceTestChart : MokaChartBase
{
	/// <summary>The series, compared by reference.</summary>
	[Parameter]
	public IReadOnlyList<SurfaceSeries>? Series { get; set; }

	/// <summary>The headings, one per point.</summary>
	[Parameter]
	public IReadOnlyList<string>? Labels { get; set; }

	/// <summary>How many panels it draws, each with its own surface.</summary>
	[Parameter]
	public int Panels { get; set; } = 1;

	/// <summary>Whether its categories run down in rows.</summary>
	[Parameter]
	public bool Rows { get; set; }

	/// <summary>Whether it holds still while pointed at.</summary>
	[Parameter]
	public bool Hold { get; set; }

	/// <summary>Its zoom gestures.</summary>
	[Parameter]
	public MokaChartZoom Zoom { get; set; }

	/// <summary>Whether it asks for the plot's width.</summary>
	[Parameter]
	public bool Measure { get; set; }

	/// <summary>The x values its data spans.</summary>
	[Parameter]
	public (double From, double To)? Extent { get; set; }

	/// <summary>A tooltip note on every point.</summary>
	[Parameter]
	public string? Note { get; set; }

	/// <summary>How many times it rendered.</summary>
	public int Renders { get; private set; }

	/// <summary>How many times the geometry was rebuilt.</summary>
	public int Rebuilds { get; private set; }

	/// <summary>The span the script last chose.</summary>
	public (double From, double To)? LastRange { get; private set; }

	/// <summary>The values it drew last, which a held chart keeps.</summary>
	public IReadOnlyList<SurfaceSeries> Drawn { get; private set; } = [];

	/// <summary>The base, through the friend assembly.</summary>
	internal MokaChartBase Base => this;

	/// <inheritdoc />
	protected override string RootClass => "surface-test-chart";

	internal override bool HasData => Drawn.Any(s => s.Values.Count > 0);

	internal override bool SurfaceRows => Rows;

	internal override bool LiveHold => Hold;

	internal override MokaChartZoom ZoomGestures => Zoom;

	internal override bool MeasuresWidth => Measure;

	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo =>
	[
		.. Drawn.Select((series, i) => new ChartSeriesInfo(series.Key, series.Key, SlotFor(i, null),
			SeriesClass(SlotFor(i, null), null, IsMuted(series.Key)), SeriesStyle(SlotFor(i, null), null), ChartMark.Line,
			IsHidden(series.Key), IsMuted(series.Key), null))
	];

	internal override int DrawnCount(int panel) => Drawn.Count == 0 ? 0 : Drawn.Max(s => s.Values.Count);

	internal override (double From, double To)? XExtent(int panel) => Extent;

	internal override void Rebuild()
	{
		Rebuilds++;
		if (Holding && Hold && Drawn.Count > 0)
		{
			CountWaiting(1);
			return;
		}

		Drawn = Series ?? [];
		PanelCount = Math.Max(1, Panels);
	}

	internal override ChartHoverData? HoverData(int panel)
	{
		int count = DrawnCount(panel);
		if (count == 0)
		{
			return null;
		}

		double max = Math.Max(1, Drawn.SelectMany(s => s.Values).Where(double.IsFinite).DefaultIfEmpty(1).Max());
		float[] x = [.. Enumerable.Range(0, count).Select(i => count == 1 ? 0.5f : (float)i / (count - 1))];
		List<ReadOnlyMemory<float>> y =
		[
			.. Drawn.Select(series => (ReadOnlyMemory<float>)Enumerable.Range(0, count)
				.Select(i => i < series.Values.Count && !IsHidden(series.Key) ? (float)(series.Values[i] / max) : float.NaN)
				.ToArray())
		];
		return new ChartHoverData(0, count - 1, x, y);
	}

	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn)
	{
		if (drawn < 0 || drawn >= DrawnCount(panel))
		{
			return null;
		}

		string heading = Labels is not null && drawn < Labels.Count ? Labels[drawn] : $"#{drawn.ToString(CultureInfo.InvariantCulture)}";
		List<ChartActiveRow> rows = [];
		List<MokaChartPoint> points = [];
		MokaChartPoint? focus = null;
		for (int index = 0; index < Drawn.Count; index++)
		{
			SurfaceSeries one = Drawn[index];
			if (IsHidden(one.Key) || drawn >= one.Values.Count)
			{
				continue;
			}

			double value = one.Values[drawn];
			string text = Formatter?.Invoke(value) ?? value.ToString("0.##", CultureInfo.InvariantCulture);
			double? since = pinnedDrawn is { } pin && pin < one.Values.Count ? value - one.Values[pin] : null;
			string? delta = since is { } change ? change.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture) : null;
			int slot = SlotFor(index, null);
			rows.Add(new ChartActiveRow(one.Key, SeriesClass(slot, null, false), SeriesStyle(slot, null), text, delta,
				index == series));
			MokaChartPoint point = new(one.Key, one.Key, index, drawn, value, text) { Category = heading, SincePin = since };
			points.Add(point);
			if (index == series)
			{
				focus = point;
			}
		}

		string cell = series >= 0 && focus is not null
			? $"{focus.SeriesName}, {heading}, {focus.ValueText}"
			: $"{heading}, {string.Join(", ", rows.Select(r => $"{r.Name} {r.ValueText}"))}";
		IReadOnlyList<string> notes = Note is null ? [] : [Note];
		return new ChartActiveInfo(heading, rows, notes, cell,
			new MokaChartTooltipContext(drawn, heading, points, focus, pinnedDrawn, notes));
	}

	internal override Task RangeChosenAsync(double from, double to)
	{
		LastRange = (from, to);
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		Renders++;
		builder.OpenElement(0, "div");
		builder.AddAttribute(1, "class", $"{CssClass} {ModernRootClasses}");
		builder.AddAttribute(2, "role", RootRole);
		builder.AddAttribute(3, "aria-label", AccessibleLabel);
		builder.AddElementReferenceCapture(4, element => RootElement = element);
		if (Interactive && LiveHold)
		{
			builder.AddContent(5, LiveContent);
		}

		for (int panel = 0; panel < PanelCount; panel++)
		{
			builder.OpenRegion(6);
			if (SurfaceMode == MokaChartTooltip.Readout)
			{
				builder.AddContent(0, ReadoutContent(panel));
			}

			builder.OpenElement(1, "div");
			builder.AddAttribute(2, "class", "test-plot-box");
			builder.AddAttribute(3, "style", "position: relative; height: 200px");
			builder.OpenComponent<MokaChartSurface>(4);
			builder.AddComponentParameter(5, nameof(MokaChartSurface.Chart), this);
			builder.AddComponentParameter(6, nameof(MokaChartSurface.Panel), panel);
			builder.CloseComponent();
			builder.CloseElement();
			builder.CloseRegion();
		}

		builder.CloseElement();
	}
}
