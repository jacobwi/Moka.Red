using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     An axis carries its tick template inside the record, and the chart compared the record as a value. A template
///     written inline that reads a local is a new delegate every parent render, so the chart rebuilt its geometry each
///     time; one that reads only its page's fields is an equal delegate, so the ticks kept their first text. The axis is
///     now compared without its template, and an axis handed over with one draws the ticks again, rebuilding nothing.
/// </summary>
public sealed class ChartAxisTemplateTests : BunitContext
{
	private static readonly string[] Hours = ["00:00", "06:00", "12:00", "18:00"];
	private static readonly double[] Values = [10, 20, 30, 40];

	private static readonly MokaLineSeries[] Long =
		[new() { Name = "LAN", Values = [.. Enumerable.Range(0, 10_000).Select(i => (double)(i % 97))] }];

	[Fact]
	public void AnInlineTemplateThatReadsALocal_IsANewDelegateEveryRender()
	{
		Assert.NotEqual(Inline("one"), Inline("one"));
	}

	[Theory]
	[InlineData(Placement.ValueTicks, false)]
	[InlineData(Placement.ValueTicks, true)]
	[InlineData(Placement.BottomLabels, false)]
	[InlineData(Placement.RowAxis, false)]
	public void ATemplateThatReadsOnlyItsPagesFields_DrawsWhatThePageHasNow_WithoutARebuild(Placement placement, bool keptAxis)
	{
		this.Static();
		Page page = new();
		MokaChartAxis kept = new() { TickTemplate = page.Tick };
		IRenderedComponent<ChartAxisTestChart> cut = Render<ChartAxisTestChart>(p =>
			Placed(p, placement, keptAxis ? kept : new MokaChartAxis { TickTemplate = page.Tick }));
		Assert.All(Ticks(cut, placement), text => Assert.StartsWith("one ", text, StringComparison.Ordinal));
		int rebuilds = cut.Instance.Rebuilds;

		page.Text = "two";
		cut.Render(p => Placed(p, placement, keptAxis ? kept : new MokaChartAxis { TickTemplate = page.Tick }));

		Assert.NotEmpty(Ticks(cut, placement));
		Assert.All(Ticks(cut, placement), text => Assert.StartsWith("two ", text, StringComparison.Ordinal));
		Assert.Equal(rebuilds, cut.Instance.Rebuilds);
	}

	[Fact]
	public void AnInlineTemplate_OnA10000PointLine_RedrawsTheTicks_AndLeavesTheGeometryAndMarksAlone()
	{
		this.InteractiveFrame();
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => LongLine(p, new MokaChartAxis { TickTemplate = Inline("render 0") }));
		long version = cut.Instance.GeometryVersion;
		IReadOnlyList<IRenderedComponent<ChartGate>> gates = cut.FindComponents<ChartGate>();
		int[] gateRenders = [.. gates.Select(gate => gate.RenderCount)];
		Assert.NotEmpty(gates);

		for (int i = 1; i <= 5; i++)
		{
			string text = $"render {i}";
			cut.Render(p => LongLine(p, new MokaChartAxis { TickTemplate = Inline(text) }));
		}

		Assert.All(cut.FindAll(".moka-chart-plot__ytick .mine"), tick => Assert.StartsWith("render 5 ", tick.TextContent, StringComparison.Ordinal));
		Assert.Equal(version, cut.Instance.GeometryVersion);
		Assert.Equal(gateRenders, gates.Select(gate => gate.RenderCount));
	}

	[Fact]
	public void AChangedAxis_WithATemplate_StillRebuildsEveryTime()
	{
		this.InteractiveFrame();
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => LongLine(p, new MokaChartAxis { TickTemplate = Inline("render 0") }));
		long version = cut.Instance.GeometryVersion;
		IReadOnlyList<IRenderedComponent<ChartGate>> gates = cut.FindComponents<ChartGate>();
		int[] gateRenders = [.. gates.Select(gate => gate.RenderCount)];

		for (int i = 1; i <= 5; i++)
		{
			string text = $"render {i}";
			cut.Render(p => LongLine(p, new MokaChartAxis { Min = -10 * i, TickTemplate = Inline(text) }));
		}

		Assert.Equal(version + 5, cut.Instance.GeometryVersion);
		Assert.All(gates.Zip(gateRenders), gate => Assert.True(gate.First.RenderCount > gate.Second));
		Assert.Equal("render 5 -50", cut.FindAll(".moka-chart-plot__ytick .mine")[0].TextContent);
	}

	[Fact]
	public void ATemplateThatComesOrGoes_DrawsTheTicks_WithoutARebuild()
	{
		this.Static();
		Page page = new();
		IRenderedComponent<ChartAxisTestChart> cut = Render<ChartAxisTestChart>(p => Placed(p, Placement.ValueTicks, new MokaChartAxis { Min = 0 }));
		int rebuilds = cut.Instance.Rebuilds;

		cut.Render(p => Placed(p, Placement.ValueTicks, new MokaChartAxis { Min = 0, TickTemplate = page.Tick }));
		Assert.Equal("one 40", cut.FindAll(".moka-chart-plot__ytick")[^1].TextContent);

		cut.Render(p => Placed(p, Placement.ValueTicks, new MokaChartAxis { Min = 0 }));
		Assert.Empty(cut.FindAll(".moka-chart-plot__ytick .mine"));
		Assert.Equal("40", cut.FindAll(".moka-chart-plot__ytick")[^1].TextContent);
		Assert.Equal(rebuilds, cut.Instance.Rebuilds);
	}

	[Fact]
	public void WithADataVersion_AnInlineTemplate_StillRebuildsNothing()
	{
		this.Static();
		IRenderedComponent<ChartAxisTestChart> cut = Render<ChartAxisTestChart>(p =>
			Placed(p, Placement.ValueTicks, new MokaChartAxis { TickTemplate = Inline("one") }).Add(x => x.DataVersion, 1));
		int rebuilds = cut.Instance.Rebuilds;

		cut.Render(p => Placed(p, Placement.ValueTicks, new MokaChartAxis { TickTemplate = Inline("two") }).Add(x => x.DataVersion, 1));

		Assert.Equal("two 40", cut.FindAll(".moka-chart-plot__ytick")[^1].TextContent);
		Assert.Equal(rebuilds, cut.Instance.Rebuilds);
	}

	[Fact]
	public void AnAxisBuiltAgainEveryRender_WithoutATemplate_DrawsNothing()
	{
		this.Static();
		IRenderedComponent<ChartAxisTestChart> cut = Render<ChartAxisTestChart>(p => Placed(p, Placement.ValueTicks, Ticked([0, 20, 40])));
		(int rebuilds, int renders) = (cut.Instance.Rebuilds, cut.RenderCount);

		cut.Render(p => Placed(p, Placement.ValueTicks, Ticked([0, 20, 40])));

		Assert.Equal((rebuilds, renders), (cut.Instance.Rebuilds, cut.RenderCount));
	}

	[Fact]
	public void ChangedTickValues_Rebuild()
	{
		this.Static();
		IRenderedComponent<ChartAxisTestChart> cut = Render<ChartAxisTestChart>(p => Placed(p, Placement.ValueTicks, Ticked([0, 20, 40])));
		int rebuilds = cut.Instance.Rebuilds;

		cut.Render(p => Placed(p, Placement.ValueTicks, Ticked([0, 10, 40])));

		Assert.Equal(rebuilds + 1, cut.Instance.Rebuilds);
		Assert.Equal(["0", "10", "40"], cut.FindAll(".moka-chart-plot__ytick").Select(tick => tick.TextContent));
	}

	[Fact]
	public void AHiddenAxisTemplate_DoesntDrawTheChartAgain()
	{
		this.Static();
		Page page = new();
		IRenderedComponent<ChartAxisTestChart> cut = Render<ChartAxisTestChart>(p =>
			Placed(p, Placement.ValueTicks, new MokaChartAxis { Visible = false, TickTemplate = page.Tick }));
		int renders = cut.RenderCount;

		cut.Render(p => Placed(p, Placement.ValueTicks, new MokaChartAxis { Visible = false, TickTemplate = page.Tick }));

		Assert.Equal(renders, cut.RenderCount);
	}

	private static RenderFragment<MokaAxisTick> Inline(string text) => tick => builder => Mark(builder, $"{text} {tick.Text}");

	private static MokaChartAxis Ticked(double[] ticks) => new() { Title = "ms", TickValues = ticks };

	private static ComponentParameterCollectionBuilder<ChartAxisTestChart> Placed(ComponentParameterCollectionBuilder<ChartAxisTestChart> p,
		Placement placement, MokaChartAxis axis)
	{
		p.Add(x => x.Values, Values).Add(x => x.Labels, Hours);
		return placement switch
		{
			Placement.ValueTicks => p.Add(x => x.YAxis, axis),
			Placement.BottomLabels => p.Add(x => x.XAxis, axis),
			_ => p.Add(x => x.Rows, true).Add(x => x.XAxis, axis)
		};
	}

	private static ComponentParameterCollectionBuilder<MokaLineChart> LongLine(ComponentParameterCollectionBuilder<MokaLineChart> p,
		MokaChartAxis axis) => p.Add(x => x.Series, Long).Add(x => x.YAxis, axis);

	private static IEnumerable<string> Ticks(IRenderedComponent<ChartAxisTestChart> cut, Placement placement) => cut.FindAll(placement switch
	{
		Placement.ValueTicks => ".moka-chart-plot__ytick .mine",
		Placement.BottomLabels => ".moka-chart-plot__x .mine",
		_ => ".moka-chart-plot__rhead .mine"
	}).Select(tick => tick.TextContent);

	private static void Mark(RenderTreeBuilder builder, string text)
	{
		builder.OpenElement(0, "b");
		builder.AddAttribute(1, "class", "mine");
		builder.AddContent(2, text);
		builder.CloseElement();
	}

	/// <summary>Where a template draws ticks.</summary>
	public enum Placement
	{
		/// <summary>The value ticks up a column plot's y axis.</summary>
		ValueTicks,

		/// <summary>The labels along a column plot's bottom.</summary>
		BottomLabels,

		/// <summary>The value ticks across the top of a row plot.</summary>
		RowAxis
	}

	// The page holding a chart: its template is a method group on it, so each render hands over a new delegate equal to the
	// last, reading the field as it is then.
	private sealed class Page
	{
		public string Text { get; set; } = "one";

		public RenderFragment Tick(MokaAxisTick tick) => builder => Mark(builder, $"{Text} {tick.Text}");
	}
}
