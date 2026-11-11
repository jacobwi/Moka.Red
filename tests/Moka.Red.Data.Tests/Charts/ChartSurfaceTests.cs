using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     The interaction surface: what it renders for the script, the options the script runs on, and the host that
///     renders alone when the point under the pointer changes. JS-to-.NET calls are made the way the script makes them.
/// </summary>
public sealed class ChartSurfaceTests : BunitContext
{
	private static readonly SurfaceSeries[] Two = [new("LAN", [10, 20, 30, 40]), new("WAN", [5, 6, 7, 8])];
	private static readonly string[] Times = ["00:00", "06:00", "12:00", "18:00"];

	[Fact]
	public void AStaticRender_DrawsNoSurface_AndTheRootIsAnImage()
	{
		this.Static();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart(p => p.Add(c => c.ShowLegend, false));

		Assert.Empty(cut.FindAll(".moka-chart-surface"));
		Assert.Equal("img", cut.Find(".surface-test-chart").GetAttribute("role"));
	}

	[Fact]
	public void AnInteractiveRender_DrawsWhatTheScriptReads()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart();

		IElement surface = cut.Find(".moka-chart-surface");
		Assert.Equal("figure", cut.Find(".surface-test-chart").GetAttribute("role"));
		Assert.Equal(cut.Instance.Base.GeometryVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
			surface.GetAttribute("data-moka-version"));
		Assert.Equal("0", surface.GetAttribute("data-moka-panel"));
		Assert.Equal("4", surface.GetAttribute("data-moka-count"));
		Assert.Equal("2", surface.GetAttribute("data-moka-series"));
		Assert.False(surface.HasAttribute("data-moka-x0"));
		Assert.False(surface.HasAttribute("data-moka-measure"));
		Assert.Contains("moka-chart-surface--crosshair", surface.ClassList);

		IElement overlay = cut.Find(".moka-chart-overlay");
		Assert.Equal("true", overlay.GetAttribute("aria-hidden"));
		Assert.Empty(overlay.Children);

		IElement[] keys = [.. cut.FindAll(".moka-chart-surface__series > i")];
		Assert.Equal(["LAN", "WAN"], keys.Select(k => k.GetAttribute("data-moka-key")));
		Assert.Equal(["moka-chart-c1", "moka-chart-c2"], keys.Select(k => k.GetAttribute("class")));
	}

	[Fact]
	public void TheExtentAndTheWidthReport_AreOnlyThereWhenTheChartHasThem()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart(p => p
			.Add(x => x.Extent, (0d, 12.5d))
			.Add(x => x.Measure, true)
			.Add(x => x.Zoom, MokaChartZoom.Brush));

		IElement surface = cut.Find(".moka-chart-surface");
		Assert.Equal("0", surface.GetAttribute("data-moka-x0"));
		Assert.Equal("12.5", surface.GetAttribute("data-moka-x1"));
		Assert.True(surface.HasAttribute("data-moka-measure"));
		Assert.Contains("moka-chart-surface--brush", surface.ClassList);
	}

	[Fact]
	public void TheScript_IsBoundOnce_WithEveryOptionInADictionary()
	{
		BunitJSModuleInterop module = this.Interactive();
		Chart();

		JSRuntimeInvocation bind = Assert.Single(module.Invocations, i => i.Identifier == "bind");
		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(bind.Arguments[2]);
		Assert.Equal("crosshair", options["mode"]);
		Assert.Equal("columns", options["orientation"]);
		Assert.Equal(true, options["pin"]);
		Assert.Equal(true, options["keys"]);
		Assert.Null(options["sync"]);
		Assert.Equal(0, options["zoom"]);
		Assert.Equal(false, options["hold"]);
		Assert.Equal(false, options["measure"]);
		Assert.Equal(32, options["nearestPx"]);
		Assert.Equal(false, options["report"]);
		Assert.Equal(false, options["select"]);
		Assert.Equal(true, options["highlight"]);
		Assert.Null(options["pageStep"]);
		Dictionary<string, object?> words = Assert.IsType<Dictionary<string, object?>>(options["words"]);
		Assert.Equal("Hold Ctrl and scroll to zoom", words["zoomHint"]);
	}

	[Fact]
	public void ADataChange_CallsNoScript()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart();
		int calls = module.Invocations.Count;

		cut.Render(p => Defaults(p, [new SurfaceSeries("LAN", [1, 2, 3, 4])]));

		Assert.Equal(calls, module.Invocations.Count);
		Assert.Equal("1", cut.Find(".moka-chart-surface").GetAttribute("data-moka-series"));
	}

	[Fact]
	public void AChangedOption_IsSentOnce()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart();

		cut.Render(p => Defaults(p).Add(x => x.SyncGroup, "traffic"));
		cut.Render(p => Defaults(p).Add(x => x.SyncGroup, "traffic").Add(x => x.Title, "Now"));

		JSRuntimeInvocation update = Assert.Single(module.Invocations, i => i.Identifier == "update");
		Assert.Equal("traffic", Assert.IsType<Dictionary<string, object?>>(update.Arguments[1])["sync"]);
	}

	[Fact]
	public void TheOptions_FollowTheListeners()
	{
		BunitJSModuleInterop module = this.Interactive();
		Chart(p => p
			.Add(x => x.ActiveChanged, (MokaChartPoint? _) => { })
			.Add(x => x.OnPointClick, (MokaChartPoint _) => { })
			.Add(x => x.Hold, true)
			.Add(x => x.Zoom, MokaChartZoom.Brush | MokaChartZoom.Wheel));

		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(
			Assert.Single(module.Invocations, i => i.Identifier == "bind").Arguments[2]);
		Assert.Equal(true, options["report"]);
		Assert.Equal(true, options["select"]);
		Assert.Equal(true, options["hold"]);
		Assert.Equal(3, options["zoom"]);
	}

	[Fact]
	public async Task AnActivePoint_RendersTheHost_NotTheChart()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart();
		int renders = cut.Instance.Renders;

		await cut.InvokeAsync(() => cut.Instance.OnActive(0, 1, 2, false));

		Assert.Equal(renders, cut.Instance.Renders);
		IElement tip = cut.Find(".moka-chart-tip");
		Assert.Equal("2", tip.GetAttribute("data-moka-for"));
		Assert.Equal("true", tip.GetAttribute("aria-hidden"));
		Assert.Equal("manual", tip.GetAttribute("popover"));
		Assert.Equal("12:00", cut.Find(".moka-chart-tip__head").TextContent);
		Assert.Equal(["30", "7"], cut.FindAll(".moka-chart-tip__value").Select(v => v.TextContent));
		Assert.Equal("WAN", cut.Find(".moka-chart-tip__row--focus .moka-chart-tip__name").TextContent);
		Assert.False(tip.HasAttribute("style"));
		Assert.False(tip.HasAttribute("data-moka-shown"));
	}

	[Fact]
	public async Task ClearingThePoint_EmptiesTheTip()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart();

		await cut.InvokeAsync(() => cut.Instance.OnActive(0, 0, 1, false));
		await cut.InvokeAsync(() => cut.Instance.OnActive(0, -1, -1, false));

		IElement tip = cut.Find(".moka-chart-tip");
		Assert.False(tip.HasAttribute("data-moka-for"));
		Assert.Empty(tip.Children);
	}

	[Fact]
	public async Task ActiveChanged_GetsTheFocusedPoint_AndNullWhenCleared()
	{
		this.Interactive();
		List<MokaChartPoint?> heard = [];
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart(p => p.Add(x => x.ActiveChanged, (MokaChartPoint? point) => heard.Add(point)));

		await cut.InvokeAsync(() => cut.Instance.OnActive(0, 1, 3, true));
		await cut.InvokeAsync(() => cut.Instance.OnActive(0, -1, -1, false));

		cut.WaitForAssertion(() => Assert.Equal(2, heard.Count));
		Assert.Equal(("WAN", 3, 8d), (heard[0]!.SeriesKey, heard[0]!.Index, heard[0]!.Value));
		Assert.Null(heard[1]);
	}

	[Fact]
	public async Task AFailingListener_ReachesTheErrorBoundary()
	{
		this.Interactive();
		IRenderedComponent<ErrorBoundary> boundary = Render<ErrorBoundary>(p => p
			.Add(b => b.ErrorContent, (RenderFragment<Exception>)(error => builder => builder.AddContent(0, $"failed: {error.Message}")))
			.AddChildContent<ChartSurfaceTestChart>(c => Defaults(c)
				.Add(x => x.ActiveChanged, (MokaChartPoint? _) => throw new InvalidOperationException("listener"))));
		ChartSurfaceTestChart chart = boundary.FindComponent<ChartSurfaceTestChart>().Instance;

		await boundary.InvokeAsync(() => chart.OnActive(0, 0, 1, false));

		boundary.WaitForAssertion(() => Assert.Contains("failed: listener", boundary.Markup, StringComparison.Ordinal));
	}

	[Fact]
	public async Task APin_ShowsItsCard_TheChangeSinceIt_AndSaysSo()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart();

		await cut.InvokeAsync(() => cut.Instance.OnPin(0, 1));
		await cut.InvokeAsync(() => cut.Instance.OnActive(0, 0, 3, false));

		IElement card = cut.Find(".moka-chart-pin");
		Assert.Equal("1", card.GetAttribute("data-moka-for"));
		Assert.Equal("06:00", card.QuerySelector(".moka-chart-pin__at")!.TextContent);
		Assert.Equal(["20", "6"], card.QuerySelectorAll(".moka-chart-tip__value").Select(v => v.TextContent));
		Assert.Equal(["+20", "+2"], cut.Find(".moka-chart-tip").QuerySelectorAll(".moka-chart-tip__delta").Select(v => v.TextContent));
		Assert.Equal("Change since 06:00", cut.Find(".moka-chart-tip__note--pin").TextContent);
		Assert.Equal("Pinned at 06:00.", Status(cut));
	}

	[Fact]
	public async Task Unpinning_SaysSo_AndASentenceSaidTwiceStillChangesTheRegion()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart();

		await cut.InvokeAsync(() => cut.Instance.OnPin(0, 1));
		await cut.InvokeAsync(() => cut.Instance.OnPin(0, -1));
		Assert.Equal("Unpinned.", Status(cut));
		Assert.False(cut.Find(".moka-chart-pin").HasAttribute("data-moka-for"));

		await cut.InvokeAsync(() => cut.Instance.OnPin(0, 1));
		await cut.InvokeAsync(() => cut.Instance.OnPin(0, 1));
		Assert.Equal("Pinned at 06:00. ", Status(cut));
	}

	[Fact]
	public async Task TheUnpinButton_LetsGo_TellsTheScript_AndRendersNoChart()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart();
		await cut.InvokeAsync(() => cut.Instance.OnPin(0, 2));
		int renders = cut.Instance.Renders;

		await cut.Find(".moka-chart-pin__unpin").ClickAsync(new MouseEventArgs());

		Assert.Equal(renders, cut.Instance.Renders);
		Assert.Equal("Unpinned.", Status(cut));
		JSRuntimeInvocation setPin = Assert.Single(module.Invocations, i => i.Identifier == "setPin");
		Assert.Equal([0, -1], setPin.Arguments.Skip(1).Cast<int>());
		Assert.Single(module.Invocations, i => i.Identifier == "focusSurface");
	}

	[Fact]
	public void NoPinCard_WhenTheChartDoesNotPin()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart(p => p.Add(x => x.Pinnable, false));

		Assert.Empty(cut.FindAll(".moka-chart-pin"));
	}

	[Fact]
	public async Task OnSelect_RaisesOnPointClick_OnlyWithAListener()
	{
		this.Interactive();
		MokaChartPoint? clicked = null;
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart(p => p.Add(x => x.OnPointClick, (MokaChartPoint point) => clicked = point));
		IRenderedComponent<ChartSurfaceTestChart> quiet = Chart();

		await cut.InvokeAsync(() => cut.Instance.OnSelect(0, 0, 2));
		await quiet.InvokeAsync(() => quiet.Instance.OnSelect(0, 0, 2));

		cut.WaitForAssertion(() => Assert.NotNull(clicked));
		Assert.Equal(("LAN", 30d), (clicked!.SeriesKey, clicked.Value));
	}

	[Fact]
	public async Task OnRange_HearsNullsAsAReset()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart();

		await cut.InvokeAsync(() => cut.Instance.OnRange(1.5, 2.5));
		Assert.Equal((1.5, 2.5), cut.Instance.LastRange);

		await cut.InvokeAsync(() => cut.Instance.OnRange(null, null));
		Assert.True(double.IsNaN(cut.Instance.LastRange!.Value.From));
		Assert.True(double.IsNaN(cut.Instance.LastRange!.Value.To));
	}

	[Fact]
	public async Task OnPlotWidth_RedrawsOncePerBucket()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart();
		int rebuilds = cut.Instance.Rebuilds;

		await cut.InvokeAsync(() => cut.Instance.OnPlotWidth(640));
		await cut.InvokeAsync(() => cut.Instance.OnPlotWidth(640));

		Assert.Equal(rebuilds + 1, cut.Instance.Rebuilds);
		Assert.Equal(640, cut.Instance.Base.PlotWidthBucket);
	}

	[Fact]
	public async Task NewData_WhileAPointIsActive_ShowsTheNewValue()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart();
		await cut.InvokeAsync(() => cut.Instance.OnActive(0, 0, 3, false));

		cut.Render(p => Defaults(p, [new SurfaceSeries("LAN", [10, 20, 30, 99]), Two[1]]));

		Assert.Equal(["99", "8"], cut.FindAll(".moka-chart-tip__value").Select(v => v.TextContent));
	}

	[Fact]
	public async Task TheTemplate_GetsTheContext_AndRendersOnlyWhenThePointChanges()
	{
		this.Interactive();
		int drawn = 0;
		RenderFragment<MokaChartTooltipContext> template = context => builder =>
		{
			drawn++;
			builder.AddMarkupContent(0, $"<b class=\"mine\">{context.Heading} {context.Points.Count} {context.Focus?.SeriesKey}</b>");
		};
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart(p => p.Add(x => x.TooltipTemplate, template).Add(x => x.Title, "Before"));

		await cut.InvokeAsync(() => cut.Instance.OnActive(0, 1, 0, false));
		int afterPoint = drawn;
		cut.Render(p => Defaults(p).Add(x => x.TooltipTemplate, template).Add(x => x.Title, "Changed"));

		Assert.Equal("00:00 2 WAN", cut.Find(".moka-chart-tip .mine").TextContent);
		Assert.Empty(cut.FindAll(".moka-chart-tip__row"));
		Assert.Equal(afterPoint, drawn);
	}

	[Fact]
	public async Task Nearest_ShowsOnlyTheSeriesItIsOn()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart(p => p.Add(x => x.Tooltip, MokaChartTooltip.Nearest));

		await cut.InvokeAsync(() => cut.Instance.OnActive(0, 1, 2, false));

		Assert.Equal(["7"], cut.FindAll(".moka-chart-tip__value").Select(v => v.TextContent));
		Assert.Contains("moka-chart-tip--nearest", cut.Find(".moka-chart-tip").ClassList);
	}

	[Fact]
	public async Task Column_DrawsBoxKeys_AndSaysTheBarKeys()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart(p => p.Add(x => x.Tooltip, MokaChartTooltip.Column));

		await cut.InvokeAsync(() => cut.Instance.OnActive(0, -1, 1, false));

		Assert.Equal("column", Assert.IsType<Dictionary<string, object?>>(
			Assert.Single(module.Invocations, i => i.Identifier == "bind").Arguments[2])["mode"]);
		Assert.All(cut.FindAll(".moka-chart-tip__key"), key => Assert.Contains("moka-chart-tip__key--box", key.ClassList));
		Assert.EndsWith("Left and Right move between bars.", cut.Find("[role=grid]").GetAttribute("aria-label"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task Readout_ShowsTheLatestUntilAPointIsActive_AndHasNoFloatingTip()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart(p => p.Add(x => x.Tooltip, MokaChartTooltip.Readout));

		Assert.Equal("Latest 18:00", cut.Find(".moka-chart-readout__head").TextContent);
		Assert.Equal(["40", "8"], cut.FindAll(".moka-chart-readout__value").Select(v => v.TextContent));
		Assert.Empty(cut.FindAll(".moka-chart-tip"));

		await cut.InvokeAsync(() => cut.Instance.OnActive(0, 0, 1, false));

		Assert.Equal("06:00", cut.Find(".moka-chart-readout__head").TextContent);
	}

	[Fact]
	public async Task TheLiveChip_CountsWhatWaits_AndTheChartCatchesUpWhenLetGo()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart(p => p.Add(x => x.Hold, true));
		Assert.Equal("Live", Chip(cut));
		Assert.Equal("status", cut.Find(".moka-chart-live").GetAttribute("role"));

		await cut.InvokeAsync(() => cut.Instance.OnHold(true));
		Assert.Equal("Paused", Chip(cut));
		Assert.True(cut.Find(".moka-chart-live").HasAttribute("data-moka-paused"));

		SurfaceSeries[] next = [new("LAN", [1, 2, 3, 4]), new("WAN", [9, 9, 9, 9])];
		cut.Render(p => Defaults(p, next).Add(x => x.Hold, true));
		Assert.Equal("Paused · 1 new", Chip(cut));
		Assert.Same(Two, cut.Instance.Drawn);

		await cut.InvokeAsync(() => cut.Instance.OnHold(false));
		Assert.Equal("Live", Chip(cut));
		Assert.Same(next, cut.Instance.Drawn);
		Assert.Equal(0, cut.Instance.Base.Waiting);
	}

	[Fact]
	public async Task SmallMultiples_EachPanelHasItsOwnHost()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart(p => p.Add(x => x.Panels, 2));

		await cut.InvokeAsync(() => cut.Instance.OnActive(1, 0, 2, false));

		IElement[] tips = [.. cut.FindAll(".moka-chart-tip")];
		Assert.Equal(2, tips.Length);
		Assert.False(tips[0].HasAttribute("data-moka-for"));
		Assert.Equal("2", tips[1].GetAttribute("data-moka-for"));
		Assert.Equal(["0", "1"], cut.FindAll(".moka-chart-surface").Select(s => s.GetAttribute("data-moka-panel")));
	}

	[Fact]
	public async Task Disposing_UnbindsOnce()
	{
		BunitJSModuleInterop module = this.Interactive();
		Chart();

		await DisposeComponentsAsync();

		Assert.Single(module.Invocations, i => i.Identifier == "unbind");
	}

	[Fact]
	public void TheModule_ExportsWhatTheChartCalls()
	{
		string script = File.ReadAllText(ScriptPath());

		foreach (string export in new[] { "bind", "update", "unbind", "setPin", "focusSurface" })
		{
			Assert.Contains($"export function {export}(", script, StringComparison.Ordinal);
		}

		foreach (string callback in new[] { "HoverGeometry", "OnActive", "OnPin", "OnSelect", "OnHold", "OnRange", "OnPlotWidth" })
		{
			Assert.Contains($"'{callback}'", script, StringComparison.Ordinal);
		}
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

	private static string Status(IRenderedComponent<ChartSurfaceTestChart> cut) =>
		cut.FindAll(".moka-chart-surface > [role=status]").Single().TextContent;

	private static string Chip(IRenderedComponent<ChartSurfaceTestChart> cut) => cut.Find(".moka-chart-live").TextContent.Trim();

	private static ComponentParameterCollectionBuilder<ChartSurfaceTestChart> Defaults(
		ComponentParameterCollectionBuilder<ChartSurfaceTestChart> p, IReadOnlyList<SurfaceSeries>? series = null) => p
		.Add(x => x.Series, series ?? Two)
		.Add(x => x.Labels, Times)
		.Add(x => x.Look, MokaChartLook.Quiet)
		.Add(x => x.Label, "Traffic today");

	private IRenderedComponent<ChartSurfaceTestChart> Chart(
		Action<ComponentParameterCollectionBuilder<ChartSurfaceTestChart>>? more = null) =>
		Render<ChartSurfaceTestChart>(p =>
		{
			Defaults(p);
			more?.Invoke(p);
		});
}
