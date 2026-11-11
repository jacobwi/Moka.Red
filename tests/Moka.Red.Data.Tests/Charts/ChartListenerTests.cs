using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     What an app asks for switches on what it needs, on every look: a click or active-point listener binds the script on
///     a look whose tooltips are the browser's, which then finds the point without drawing a tooltip and lets the pointer
///     through to the marks' titles, and a tooltip template gets a tooltip to draw in. A chart that asks for none of them
///     stays as it was.
/// </summary>
public sealed class ChartListenerTests : BunitContext
{
	private static readonly string[] Regions = ["EU West", "EU North", "US East"];
	private static readonly MokaBarSeries[] Week = [new() { Name = "This week", Values = [412, 128, 506] }];
	private static readonly MokaLineSeries[] Line = [new() { Name = "LAN", Values = [10, 20, 30, 40] }];
	private static readonly string[] Hours = ["00:00", "06:00", "12:00", "18:00"];

	[Fact]
	public async Task AClickListener_OnTheStandardLook_BindsTheScript_AndRaisesOnPointClick()
	{
		BunitJSModuleInterop module = this.Interactive();
		MokaChartPoint? clicked = null;
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Series, Week)
			.Add(x => x.Labels, Regions)
			.Add(x => x.OnPointClick, (MokaChartPoint point) => clicked = point));

		Dictionary<string, object?> options = Options(module);
		Assert.Equal("none", options["mode"]);
		Assert.Equal("column", options["track"]);
		Assert.Equal(true, options["select"]);
		Assert.Equal(true, options["pass"]);
		Assert.Equal(false, options["keys"]);

		await cut.InvokeAsync(() => cut.Instance.OnSelect(0, -1, 2));

		cut.WaitForAssertion(() => Assert.NotNull(clicked));
		Assert.Equal(("This week", 506d, "US East"), (clicked!.SeriesName, clicked.Value, clicked.Category));
	}

	[Fact]
	public void AClickListener_KeepsTheBrowsersTitles_AndLetsThePointerThroughToThem()
	{
		this.Interactive();
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Series, Week)
			.Add(x => x.Labels, Regions)
			.Add(x => x.OnPointClick, (MokaChartPoint _) => { }));

		IElement surface = cut.Find(".moka-chart-surface");
		Assert.Contains("moka-chart-surface--pass", surface.ClassList);
		Assert.Contains("moka-chart-surface--none", surface.ClassList);
		Assert.Empty(cut.FindAll(".moka-chart-tip"));
		Assert.Equal("EU West · This week: 412", cut.FindAll(".moka-chart-cols > .moka-chart-col")[0].GetAttribute("title"));
		Assert.Equal("figure", cut.Find(".moka-barchart").GetAttribute("role"));
	}

	[Fact]
	public void TheSurfaceStylesheet_LetsThePointerPassThroughAPassSurface()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaChartSurface.razor.css"))
			.ReplaceLineEndings("\n");

		Assert.Contains(".moka-chart-surface--pass,\n.moka-chart-surface--pass > .moka-chart-surface__grid {\n\tpointer-events: none;",
			css, StringComparison.Ordinal);
	}

	[Fact]
	public void AnActiveListener_OnAStandardLineChart_TakesItOffTheClassicPath_AndReports()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Series, Line)
			.Add(x => x.Labels, Hours)
			.Add(x => x.ActiveChanged, (MokaChartPoint? _) => { }));

		Assert.False(cut.Instance.IsClassic);
		Dictionary<string, object?> options = Options(module);
		Assert.Equal("none", options["mode"]);
		Assert.Equal("crosshair", options["track"]);
		Assert.Equal(true, options["report"]);
		Assert.Equal(false, options["select"]);
		Assert.Single(cut.FindAll("svg.moka-linechart__marks title"), t => t.TextContent == "06:00 · LAN: 20");
	}

	[Fact]
	public void ALineChart_GoesBackToTheClassicPath_WhenTheListenerGoes()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Series, Line)
			.Add(x => x.Labels, Hours)
			.Add(x => x.OnPointClick, (MokaChartPoint _) => { }));
		Assert.False(cut.Instance.IsClassic);

		cut.Render(p => p.Add(x => x.Series, Line).Add(x => x.Labels, Hours).Add(x => x.OnPointClick, default(EventCallback<MokaChartPoint>)));

		Assert.True(cut.Instance.IsClassic);
		Assert.Single(module.Invocations, i => i.Identifier == "unbind");
		Assert.Empty(cut.FindAll(".moka-chart-surface"));
	}

	[Fact]
	public void AChartThatAsksForNothing_StaysClassic_AndLoadsNoScript()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<MokaLineChart> line = Render<MokaLineChart>(p => p.Add(x => x.Series, Line).Add(x => x.Labels, Hours));
		IRenderedComponent<MokaBarChart> bars = Render<MokaBarChart>(p => p.Add(x => x.Series, Week).Add(x => x.Labels, Regions));

		Assert.True(line.Instance.IsClassic);
		Assert.True(bars.Instance.IsClassic);
		Assert.Empty(module.Invocations);
	}

	[Fact]
	public void AModernChartOnTheStandardLook_WithNoListener_BindsNothing()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Series, Week)
			.Add(x => x.Labels, Regions)
			.Add(x => x.Title, "Traffic"));

		Assert.False(cut.Instance.IsClassic);
		Assert.Empty(cut.FindAll(".moka-chart-surface"));
		Assert.Empty(module.Invocations);
	}

	[Fact]
	public void AStaticRender_WithAListener_DrawsNoSurface()
	{
		this.Static();
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Series, Week)
			.Add(x => x.Labels, Regions)
			.Add(x => x.OnPointClick, (MokaChartPoint _) => { }));

		Assert.Empty(cut.FindAll(".moka-chart-surface"));
		Assert.Equal("EU West · This week: 412", cut.FindAll(".moka-chart-cols > .moka-chart-col")[0].GetAttribute("title"));
	}

	[Fact]
	public async Task ATooltipTemplate_OnTheStandardLook_GetsATooltipToDrawIn()
	{
		BunitJSModuleInterop module = this.Interactive();
		RenderFragment<MokaChartTooltipContext> template = context => builder =>
			builder.AddMarkupContent(0, $"<b class=\"mine\">{context.Heading}: {context.Points[0].ValueText}</b>");
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Series, Line)
			.Add(x => x.Labels, Hours)
			.Add(x => x.TooltipTemplate, template));

		Assert.False(cut.Instance.IsClassic);
		Assert.Equal(MokaChartTooltip.Auto, cut.Instance.ResolvedTooltip);
		Assert.Equal("crosshair", Options(module)["mode"]);
		Assert.DoesNotContain("moka-chart-surface--pass", cut.Find(".moka-chart-surface").ClassList);

		await cut.InvokeAsync(() => cut.Instance.OnActive(0, -1, 1, false));

		Assert.Equal("06:00: 20", cut.Find(".moka-chart-tip .mine").TextContent);
	}

	[Fact]
	public void ATooltipTemplate_OnBars_TakesTheColumnTooltip()
	{
		BunitJSModuleInterop module = this.Interactive();
		RenderFragment<MokaChartTooltipContext> template = context => builder => builder.AddContent(0, context.Heading);
		Render<MokaBarChart>(p => p
			.Add(x => x.Series, Week)
			.Add(x => x.Labels, Regions)
			.Add(x => x.TooltipTemplate, template));

		Assert.Equal("column", Options(module)["mode"]);
	}

	[Fact]
	public void AnExplicitNativeTooltip_StaysTheBrowsers_EvenWithATemplate()
	{
		BunitJSModuleInterop module = this.Interactive();
		RenderFragment<MokaChartTooltipContext> template = context => builder => builder.AddContent(0, context.Heading);
		Render<MokaBarChart>(p => p
			.Add(x => x.Series, Week)
			.Add(x => x.Labels, Regions)
			.Add(x => x.Tooltip, MokaChartTooltip.Native)
			.Add(x => x.TooltipTemplate, template)
			.Add(x => x.OnPointClick, (MokaChartPoint _) => { }));

		Assert.Equal("none", Options(module)["mode"]);
	}

	[Fact]
	public void TheKeyboard_OnTheStandardLook_FindsColumnsAsColumns()
	{
		BunitJSModuleInterop module = this.Interactive();
		Render<MokaBarChart>(p => p
			.Add(x => x.Series, Week)
			.Add(x => x.Labels, Regions)
			.Add(x => x.Keyboard, true));

		Dictionary<string, object?> options = Options(module);
		Assert.Equal("none", options["mode"]);
		Assert.Equal("column", options["track"]);
		Assert.Equal(true, options["keys"]);
	}

	[Fact]
	public void ABrush_OnTheStandardLook_BindsTheScript_AndKeepsTheSurfaceForTheDrag()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Series, Line)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Zoom, MokaChartZoom.Brush));

		Assert.Equal(1, Options(module)["zoom"]);
		Assert.Equal(false, Options(module)["pass"]);
		Assert.DoesNotContain("moka-chart-surface--pass", cut.Find(".moka-chart-surface").ClassList);
	}

	[Fact]
	public void AChartOfParts_WithAClickListener_OnTheStandardLook_Binds()
	{
		BunitJSModuleInterop module = this.Interactive();
		Render<MokaDonutChart>(p => p
			.Add(x => x.Segments, [new MokaDonutSegment { Name = "Web", Value = 3 }, new MokaDonutSegment { Name = "Mail", Value = 1 }])
			.Add(x => x.OnPointClick, (MokaChartPoint _) => { }));

		Dictionary<string, object?> options = Options(module);
		Assert.Equal("none", options["mode"]);
		Assert.Equal(true, options["hit"]);
		Assert.Equal(false, options["pass"]);
		Assert.Equal(true, options["select"]);
	}

	[Fact]
	public void TheScript_FindsThePointWithoutATooltip_AndLetsThePointerThrough()
	{
		string script = File.ReadAllText(ScriptPath()).ReplaceLineEndings("\n");

		Assert.Contains("next.find = next.mode !== 'none' ? next.mode : next.track || 'none';", script, StringComparison.Ordinal);
		Assert.Contains("if (o.find === 'none' || ps.gesture || ps.preview) return;", script, StringComparison.Ordinal);
		Assert.Contains("o.pass ? markSurface(target, true)", script, StringComparison.Ordinal);
		Assert.Contains("state.root.toggleAttribute('data-moka-pick'", script, StringComparison.Ordinal);
	}

	private static string ScriptPath()
	{
		string? directory = AppContext.BaseDirectory;
		while (directory is not null && !File.Exists(Path.Combine(directory, "Moka.Red.slnx")))
		{
			directory = Path.GetDirectoryName(directory);
		}

		return Path.Combine(directory!, "src", "Moka.Red.Data", "wwwroot", "moka-chart.js");
	}

	private static Dictionary<string, object?> Options(BunitJSModuleInterop module) =>
		Assert.IsType<Dictionary<string, object?>>(Assert.Single(module.Invocations, i => i.Identifier == "bind").Arguments[2]);
}
