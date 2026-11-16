using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     A fragment that reads only its parent's fields is an equal delegate every render while what it draws has changed,
///     so a chart that compared it as a value kept the first text. A chart now draws again whenever its parent hands a
///     fragment over again, as Blazor renders any component that takes one, and rebuilds nothing for it: the geometry
///     version stays and the marks behind it don't render.
/// </summary>
public sealed class ChartFragmentTests : BunitContext
{
	private static readonly string[] Hours = ["00:00", "06:00", "12:00", "18:00"];
	private static readonly MokaLineSeries[] Line = [new() { Name = "LAN", Values = [10, 20, 30, 40] }];

	[Fact]
	public void AFragmentThatReadsOnlyItsParentsFields_IsAnEqualDelegateEveryRender()
	{
		Page page = new();

		Assert.Equal((RenderFragment)page.Title, (RenderFragment)page.Title);
	}

	[Theory]
	[InlineData(nameof(MokaChartBase.TitleContent))]
	[InlineData(nameof(MokaChartBase.Actions))]
	[InlineData(nameof(MokaChartBase.EmptyContent))]
	[InlineData(nameof(MokaChartBase.ErrorContent))]
	[InlineData(nameof(MokaChartBase.ToolbarControl))]
	public void TheFramesFragments_DrawWhatTheParentHasNow_WithoutARebuild(string fragment)
	{
		this.StaticFrame();
		Page page = new();
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => WithFragment(p, fragment, page));
		Assert.Equal("one", cut.Find(".mine").TextContent);
		long version = cut.Instance.GeometryVersion;

		page.Text = "two";
		cut.Render(p => WithFragment(p, fragment, page));

		Assert.Equal("two", cut.Find(".mine").TextContent);
		Assert.Equal(version, cut.Instance.GeometryVersion);
	}

	[Fact]
	public async Task TheTooltipTemplate_WhileItShows_DrawsWhatTheParentHasNow()
	{
		this.Interactive();
		Page page = new();
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => Tooltip(p, page));
		await cut.InvokeAsync(() => cut.Instance.OnActive(0, -1, 1, false));
		Assert.Equal("one 06:00", cut.Find(".moka-chart-tip .mine").TextContent);
		long version = cut.Instance.GeometryVersion;

		page.Text = "two";
		cut.Render(p => Tooltip(p, page));

		Assert.Equal("two 06:00", cut.Find(".moka-chart-tip .mine").TextContent);
		Assert.Equal(version, cut.Instance.GeometryVersion);
	}

	[Fact]
	public async Task TheTooltipTemplate_InThePinCard_DrawsWhatTheParentHasNow()
	{
		this.Interactive();
		Page page = new();
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => Tooltip(p, page).Add(x => x.Pinnable, true));
		await cut.InvokeAsync(() => cut.Instance.OnPin(0, 2));
		Assert.Equal("one 12:00", cut.Find(".moka-chart-pin .mine").TextContent);

		page.Text = "two";
		cut.Render(p => Tooltip(p, page).Add(x => x.Pinnable, true));

		Assert.Equal("two 12:00", cut.Find(".moka-chart-pin .mine").TextContent);
	}

	[Fact]
	public void TheTooltipTemplate_WhileNothingShows_IsNotDrawnByAParentRender()
	{
		this.Interactive();
		Page page = new();
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => Tooltip(p, page));
		int drawn = page.Tips;

		page.Text = "two";
		cut.Render(p => Tooltip(p, page));

		Assert.Equal(drawn, page.Tips);
	}

	[Fact]
	public void TheDotPlotsNameTemplate_DrawsWhatTheParentHasNow_WithoutARebuild()
	{
		this.Static();
		Page page = new();
		MokaDotPlotRow[] rows = [new("Router", 99.9), new("DNS", 99.5)];
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p
			.Add(x => x.Rows, rows)
			.Add(x => x.NameTemplate, (RenderFragment<MokaDotPlotRow>)page.Name));
		Assert.Equal(["one Router", "one DNS"], cut.FindAll(".moka-dotplot__name .mine").Select(name => name.TextContent));
		long version = cut.Instance.GeometryVersion;

		page.Text = "two";
		cut.Render(p => p
			.Add(x => x.Rows, rows)
			.Add(x => x.NameTemplate, (RenderFragment<MokaDotPlotRow>)page.Name));

		Assert.Equal(["two Router", "two DNS"], cut.FindAll(".moka-dotplot__name .mine").Select(name => name.TextContent));
		Assert.Equal(version, cut.Instance.GeometryVersion);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void TheDonutsCenter_WithADataVersion_DrawsAgainWhenTheParentDoes(bool modern)
	{
		this.Static();
		Page page = new();
		MokaDonutSegment[] segments = [new() { Name = "Web", Value = 60 }, new() { Name = "Mail", Value = 40 }];
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => Donut(p, segments, modern, page));
		long version = cut.Instance.GeometryVersion;

		page.Text = "two";
		cut.Render(p => Donut(p, segments, modern, page));

		Assert.Equal("two", cut.Find(".moka-donutchart-center .mine").TextContent);
		Assert.Equal(version, cut.Instance.GeometryVersion);
	}

	[Fact]
	public void AParentRender_WithEveryFragment_RebuildsNothing_AndLeavesTheMarksAlone()
	{
		this.InteractiveFrame();
		Page page = new();
		MokaLineSeries[] series = [new() { Name = "LAN", Values = [.. Enumerable.Range(0, 10_000).Select(i => (double)(i % 97))] }];
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => Everything(p, series, page));
		long version = cut.Instance.GeometryVersion;
		IReadOnlyList<IRenderedComponent<ChartGate>> gates = cut.FindComponents<ChartGate>();
		int[] gateRenders = [.. gates.Select(gate => gate.RenderCount)];
		Assert.NotEmpty(gates);

		for (int i = 0; i < 5; i++)
		{
			page.Text = $"render {i}";
			cut.Render(p => Everything(p, series, page));
		}

		Assert.Equal("render 4", cut.Find(".moka-chart-frame__title .mine").TextContent);
		Assert.Equal(version, cut.Instance.GeometryVersion);
		Assert.Equal(gateRenders, gates.Select(gate => gate.RenderCount));
	}

	[Fact]
	public void TheTestChart_DrawsAgainForAFragment_AndRebuildsNothing()
	{
		this.Static();
		Page page = new();
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p
			.Add(x => x.Values, [1d, 2])
			.Add(x => x.TitleContent, (RenderFragment)page.Title)
			.Add(x => x.TooltipTemplate, (RenderFragment<MokaChartTooltipContext>)page.Tip));
		(int rebuilds, int renders) = (cut.Instance.Rebuilds, cut.Instance.Renders);

		cut.Render(p => p
			.Add(x => x.Values, [1d, 2])
			.Add(x => x.TitleContent, (RenderFragment)page.Title)
			.Add(x => x.TooltipTemplate, (RenderFragment<MokaChartTooltipContext>)page.Tip));

		Assert.Equal((rebuilds, renders + 1), (cut.Instance.Rebuilds, cut.Instance.Renders));
	}

	[Fact]
	public void WithoutFragments_AnUnchangedParentRenderStillDrawsNothing()
	{
		this.Static();
		IRenderedComponent<TestChart> test = Render<TestChart>(p => NoFragments(p));
		(int rebuilds, int renders) = (test.Instance.Rebuilds, test.Instance.Renders);
		IRenderedComponent<MokaLineChart> line = Render<MokaLineChart>(p => p.Add(x => x.Series, Line).Add(x => x.Labels, Hours)
			.Add(x => x.Title, "Traffic"));
		int lineRenders = line.RenderCount;

		test.Render(p => NoFragments(p));
		line.Render(p => p.Add(x => x.Series, Line).Add(x => x.Labels, Hours).Add(x => x.Title, "Traffic"));

		Assert.Equal((rebuilds, renders), (test.Instance.Rebuilds, test.Instance.Renders));
		Assert.Equal(lineRenders, line.RenderCount);
	}

	[Fact]
	public void AClassicChart_DoesntDrawAgain_ForAFragmentItDoesntDraw()
	{
		this.Static();
		Page page = new();
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p.Add(x => x.Series, Line).Add(x => x.Labels, Hours)
			.Add(x => x.EmptyContent, (RenderFragment)page.Title));
		Assert.True(cut.Instance.IsClassic);
		int renders = cut.RenderCount;

		cut.Render(p => p.Add(x => x.Series, Line).Add(x => x.Labels, Hours).Add(x => x.EmptyContent, (RenderFragment)page.Title));

		Assert.Equal(renders, cut.RenderCount);
	}

	[Fact]
	public void AFragmentThatComesOrGoes_StillDecidesThePath()
	{
		this.Static();
		Page page = new();
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => Titled(p, null));
		Assert.True(cut.Instance.IsClassic);

		cut.Render(p => Titled(p, page.Title));
		Assert.False(cut.Instance.IsClassic);
		Assert.Equal("one", cut.Find(".moka-chart-frame__title .mine").TextContent);

		cut.Render(p => Titled(p, null));
		Assert.True(cut.Instance.IsClassic);
	}

	private static ComponentParameterCollectionBuilder<MokaLineChart> Titled(ComponentParameterCollectionBuilder<MokaLineChart> p,
		RenderFragment? title) => p
		.Add(x => x.Series, Line)
		.Add(x => x.Labels, Hours)
		.Add(x => x.TitleContent, title);

	private static ComponentParameterCollectionBuilder<TestChart> NoFragments(ComponentParameterCollectionBuilder<TestChart> p) => p
		.Add(x => x.Values, [1d, 2])
		.Add(x => x.TitleContent, (RenderFragment?)null)
		.Add(x => x.Actions, (RenderFragment?)null)
		.Add(x => x.EmptyContent, (RenderFragment?)null)
		.Add(x => x.ErrorContent, (RenderFragment?)null)
		.Add(x => x.ToolbarControl, (RenderFragment<MokaChartControlContext>?)null)
		.Add(x => x.TooltipTemplate, (RenderFragment<MokaChartTooltipContext>?)null);

	private static ComponentParameterCollectionBuilder<MokaLineChart> WithFragment(ComponentParameterCollectionBuilder<MokaLineChart> p,
		string fragment, Page page)
	{
		p.Add(x => x.Series, Line).Add(x => x.Labels, Hours).Add(x => x.Title, "Traffic");
		return fragment switch
		{
			nameof(MokaChartBase.TitleContent) => p.Add(x => x.TitleContent, (RenderFragment)page.Title),
			nameof(MokaChartBase.Actions) => p.Add(x => x.Controls, MokaChartControls.View).Add(x => x.Actions, (RenderFragment)page.Title),
			nameof(MokaChartBase.EmptyContent) => p.Add(x => x.State, MokaChartState.Empty).Add(x => x.EmptyContent, (RenderFragment)page.Title),
			nameof(MokaChartBase.ErrorContent) => p.Add(x => x.State, MokaChartState.Error).Add(x => x.ErrorContent, (RenderFragment)page.Title),
			_ => p.Add(x => x.Controls, MokaChartControls.View).Add(x => x.ToolbarControl, (RenderFragment<MokaChartControlContext>)page.Control)
		};
	}

	private static ComponentParameterCollectionBuilder<MokaLineChart> Tooltip(ComponentParameterCollectionBuilder<MokaLineChart> p,
		Page page) => p
		.Add(x => x.Series, Line)
		.Add(x => x.Labels, Hours)
		.Add(x => x.TooltipTemplate, (RenderFragment<MokaChartTooltipContext>)page.Tip);

	private static ComponentParameterCollectionBuilder<MokaDonutChart> Donut(ComponentParameterCollectionBuilder<MokaDonutChart> p,
		MokaDonutSegment[] segments, bool modern, Page page) => p
		.Add(x => x.Segments, segments)
		.Add(x => x.DataVersion, 1)
		.Add(x => x.Title, modern ? "Traffic" : null)
		.Add(x => x.ChildContent, (RenderFragment)page.Title);

	private static ComponentParameterCollectionBuilder<MokaLineChart> Everything(ComponentParameterCollectionBuilder<MokaLineChart> p,
		MokaLineSeries[] series, Page page) => p
		.Add(x => x.Series, series)
		.Add(x => x.TitleContent, (RenderFragment)page.Title)
		.Add(x => x.Actions, (RenderFragment)page.Title)
		.Add(x => x.Controls, MokaChartControls.View)
		.Add(x => x.ToolbarControl, (RenderFragment<MokaChartControlContext>)page.Control)
		.Add(x => x.EmptyContent, (RenderFragment)page.Title)
		.Add(x => x.ErrorContent, (RenderFragment)page.Title)
		.Add(x => x.TooltipTemplate, (RenderFragment<MokaChartTooltipContext>)page.Tip);

	// The parent of a chart: its fragments are method groups on it, so each render hands over a new delegate equal to the
	// last, reading the field as it is then.
	private sealed class Page
	{
		public string Text { get; set; } = "one";

		public int Tips { get; private set; }

		public void Title(RenderTreeBuilder builder) => Mark(builder, Text);

		public RenderFragment Control(MokaChartControlContext context) => builder => Mark(builder, Text);

		public RenderFragment Tip(MokaChartTooltipContext context) => builder =>
		{
			Tips++;
			Mark(builder, $"{Text} {context.Heading}");
		};

		public RenderFragment Name(MokaDotPlotRow row) => builder => Mark(builder, $"{Text} {row.Name}");

		private static void Mark(RenderTreeBuilder builder, string text)
		{
			builder.OpenElement(0, "b");
			builder.AddAttribute(1, "class", "mine");
			builder.AddContent(2, text);
			builder.CloseElement();
		}
	}
}
