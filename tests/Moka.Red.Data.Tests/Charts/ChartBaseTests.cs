using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     The render model every chart shares: it draws again only when something it draws from changed, compares data by
///     value or by version, resolves its look from the theme, keeps colors on the full list of series, and talks to its
///     script only in an interactive render of a chart that needs it.
/// </summary>
public sealed class ChartBaseTests : BunitContext
{
	[Fact]
	public void UnchangedParameters_RenderNothing()
	{
		this.Static();
		double[] values = [1, 2, 3];
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p.Add(x => x.Values, values));
		int renders = cut.Instance.Renders;

		cut.Render(p => p.Add(x => x.Values, values));
		cut.Render(p => p.Add(x => x.Values, [1d, 2, 3]));

		Assert.Equal(renders, cut.Instance.Renders);
		Assert.Equal(1, cut.Instance.Rebuilds);
	}

	[Fact]
	public void AListChangedInPlace_Renders()
	{
		this.Static();
		double[] values = [1, 2, 3];
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p.Add(x => x.Values, values));
		int renders = cut.Instance.Renders;

		values[1] = 20;
		cut.Render(p => p.Add(x => x.Values, values));

		Assert.Equal(renders + 1, cut.Instance.Renders);
		Assert.Equal(2, cut.Instance.Rebuilds);
	}

	[Fact]
	public void WithDataVersion_DataIsComparedByVersion_NotCopied()
	{
		this.Static();
		double[] values = [1, 2, 3];
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p.Add(x => x.Values, values).Add(x => x.DataVersion, 1L));
		int renders = cut.Instance.Renders;

		values[1] = 20;
		cut.Render(p => p.Add(x => x.Values, values).Add(x => x.DataVersion, 1L));
		Assert.Equal(renders, cut.Instance.Renders);

		cut.Render(p => p.Add(x => x.Values, values).Add(x => x.DataVersion, 2L));
		Assert.Equal(renders + 1, cut.Instance.Renders);
		Assert.Equal(2, cut.Instance.Rebuilds);
	}

	[Fact]
	public void AChromeParameter_RendersWithoutRebuilding()
	{
		this.Static();
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p.Add(x => x.Values, [1d]).Add(x => x.Caption, "One"));
		long version = cut.Instance.Base.GeometryVersion;

		cut.Render(p => p.Add(x => x.Values, [1d]).Add(x => x.Caption, "Two"));

		Assert.Contains("Two", cut.Markup, StringComparison.Ordinal);
		Assert.Equal(1, cut.Instance.Rebuilds);
		Assert.Equal(version, cut.Instance.Base.GeometryVersion);
	}

	[Fact]
	public void TheTitleAndTheRootsClass_AreChrome_AndRebuildNothing()
	{
		this.Static();
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p.Add(x => x.Title, "One").Add(x => x.Class, "a"));
		int renders = cut.Instance.Renders;

		cut.Render(p => p.Add(x => x.Title, "Two").Add(x => x.Class, "b"));

		Assert.Equal(renders + 1, cut.Instance.Renders);
		Assert.Equal(1, cut.Instance.Rebuilds);
	}

	[Fact]
	public void EveryRebuild_MovesTheGeometryVersion()
	{
		this.Static();
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p.Add(x => x.Values, [1d]));
		long first = cut.Instance.Base.GeometryVersion;

		cut.Render(p => p.Add(x => x.Values, [2d]));

		Assert.Equal(first + 1, cut.Instance.Base.GeometryVersion);
	}

	[Fact]
	public void TheLook_IsStandard_OnTheOriginalDesign()
	{
		this.Static();
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.AddCascadingValue(MokaTheme.Dark));

		Assert.Equal(nameof(MokaChartLook.Standard), cut.Find(".test-chart").GetAttribute("data-look"));
		Assert.Same(ChartLookSpec.Standard, cut.Instance.Base.Spec);
	}

	[Fact]
	public void TheLook_IsTheParameter_ThenTheTheme_OnTheNewDesign()
	{
		this.Static();
		MokaTheme theme = MokaTheme.Default.WithCharts(MokaChartLook.Bold);

		IRenderedComponent<TestChart> fromTheme = Render<TestChart>(p => p.AddCascadingValue(theme));
		IRenderedComponent<TestChart> fromParameter = Render<TestChart>(p => p
			.Add(x => x.Look, MokaChartLook.Dense)
			.AddCascadingValue(theme));

		Assert.Equal(nameof(MokaChartLook.Bold), fromTheme.Find(".test-chart").GetAttribute("data-look"));
		Assert.Equal(nameof(MokaChartLook.Dense), fromParameter.Find(".test-chart").GetAttribute("data-look"));
		Assert.Equal(MokaAxisSide.End, fromParameter.Instance.Base.Spec.ValueSide);
	}

	[Fact]
	public void TheChartStyleMethods_DoNothingOnTheOriginalDesign()
	{
		MokaTheme dark = MokaTheme.Dark;

		Assert.Same(dark, dark.WithCharts(MokaChartLook.Quiet));
		Assert.Same(dark, dark.WithChartPalette(MokaChartPalette.Reference));
		Assert.Same(dark, dark.WithChartTexture(MokaChartTexture.Hatched));
		Assert.Same(dark, dark.WithChartPrint(MokaChartPrint.Ink));
		Assert.Equal(MokaChartPrint.Ink, MokaTheme.Default.WithChartPrint(MokaChartPrint.Ink).ChartPrint);
	}

	[Fact]
	public void Slots_FollowTheFullList_SoHidingNeverRecolors()
	{
		this.Static();
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p
			.Add(x => x.Keys, ["a", "b", "c"])
			.Add(x => x.HiddenSeries, ["b"]));

		IReadOnlyList<ChartSeriesInfo> series = cut.Instance.Base.SeriesInfo;
		Assert.Equal("moka-chart-c3", series[2].ColorClass);
		Assert.True(series[1].Hidden);
		Assert.False(series[2].Hidden);
	}

	[Fact]
	public void AHighlight_MutesTheOtherSeries()
	{
		this.Static();
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p
			.Add(x => x.Keys, ["a", "b"])
			.Add(x => x.Highlight, "b"));

		Assert.Equal(["moka-chart-muted", "moka-chart-c2"], cut.Instance.Base.SeriesInfo.Select(s => s.ColorClass));
	}

	[Theory]
	[InlineData(1, "moka-chart-c1")]
	[InlineData(8, "moka-chart-c8")]
	[InlineData(9, "moka-chart-c1")]
	[InlineData(0, "moka-chart-c1")]
	public void SeriesClass_FoldsSlotsOntoTheEightColors(int slot, string expected) =>
		Assert.Equal(expected, MokaChartBase.SeriesClass(slot, null, false));

	[Fact]
	public void SeriesClass_AnExplicitColorWins_AndMutedWinsOverIt()
	{
		Assert.Equal("moka-chart-tone-success", MokaChartBase.SeriesClass(2, MokaColor.Success, false));
		Assert.Equal("moka-chart-muted", MokaChartBase.SeriesClass(2, MokaColor.Success, true));
	}

	[Fact]
	public void SeriesStyle_TakesTheSeriesColor_ThenTheChartsColors_AndDropsWhatIsNotAColor()
	{
		this.Static();
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p.Add(x => x.Colors, ["#112233", "red; top: 0"]));
		MokaChartBase chart = cut.Instance.Base;

		Assert.Equal("--moka-chart-color: var(--moka-color-info)", chart.SeriesStyle(1, "var(--moka-color-info)"));
		Assert.Equal("--moka-chart-color: #112233", chart.SeriesStyle(1, null));
		Assert.Null(chart.SeriesStyle(2, null));
		Assert.Equal("--moka-chart-color: #112233", chart.SeriesStyle(3, "not a color"));
	}

	[Fact]
	public void TheState_IsWorkedOutFromTheData_UnlessSet()
	{
		this.Static();
		IRenderedComponent<TestChart> empty = Render<TestChart>();
		IRenderedComponent<TestChart> ready = Render<TestChart>(p => p.Add(x => x.Values, [1d]));
		IRenderedComponent<TestChart> loading = Render<TestChart>(p => p.Add(x => x.State, MokaChartState.Loading));

		Assert.Equal(MokaChartState.Empty, empty.Instance.Base.ResolvedState);
		Assert.Equal(MokaChartState.Ready, ready.Instance.Base.ResolvedState);
		Assert.Equal(MokaChartState.Loading, loading.Instance.Base.ResolvedState);
	}

	[Fact]
	public void AStaticRender_IsNotInteractive_AndCallsNoScript()
	{
		this.Static();
		JSInterop.Mode = JSRuntimeMode.Strict;
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p.Add(x => x.Look, MokaChartLook.Quiet));

		Assert.False(cut.Instance.Base.Interactive);
		Assert.Empty(JSInterop.Invocations);
	}

	[Fact]
	public void AChartThatNeedsNoSurface_NeverReadsTheRenderer()
	{
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p.Add(x => x.Values, [1d]));

		Assert.False(cut.Instance.Base.Interactive);
	}

	[Fact]
	public void AnInteractiveRender_BindsOnce_WithAnOptionsDictionary()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p.Add(x => x.Look, MokaChartLook.Quiet));

		cut.Render(p => p.Add(x => x.Look, MokaChartLook.Quiet).Add(x => x.Values, [1d, 2]));

		JSRuntimeInvocation bind = Assert.Single(module.Invocations, i => i.Identifier == "bind");
		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(bind.Arguments[2]);
		Assert.Equal("crosshair", options["mode"]);
		Assert.Equal(true, options["keys"]);
		Assert.DoesNotContain(module.Invocations, i => i.Identifier == "update");
	}

	[Fact]
	public void ChangedSurfaceOptions_AreSentAgain()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p.Add(x => x.Look, MokaChartLook.Quiet));

		cut.Render(p => p.Add(x => x.Look, MokaChartLook.Quiet).Add(x => x.Tooltip, MokaChartTooltip.Nearest));

		JSRuntimeInvocation update = Assert.Single(module.Invocations, i => i.Identifier == "update");
		Assert.Equal("nearest", Assert.IsType<Dictionary<string, object?>>(update.Arguments[1])["mode"]);
	}

	[Fact]
	public async Task Disposing_UnbindsOnlyWhatWasBound()
	{
		BunitJSModuleInterop module = this.Interactive();
		Render<TestChart>(p => p.Add(x => x.Look, MokaChartLook.Quiet));
		Render<TestChart>(p => p.Add(x => x.Values, [1d]));

		await DisposeComponentsAsync();

		Assert.Single(module.Invocations, i => i.Identifier == "unbind");
	}

	[Fact]
	public void AClassicChart_NeverBinds_EvenInAnInteractiveRender()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p
			.Add(x => x.Classic, true)
			.Add(x => x.Keyboard, true));

		Assert.False(cut.Instance.Base.Interactive);
		Assert.Empty(module.Invocations);
	}

	[Fact]
	public void TodaysCharts_AreClassic_AndCallNoScript_EvenInAnInteractiveRender()
	{
		SetRendererInfo(new RendererInfo("Server", true));
		JSInterop.Mode = JSRuntimeMode.Strict;

		IRenderedComponent<MokaLineChart> line = Render<MokaLineChart>(p => p
			.Add(x => x.Series, [new MokaLineSeries { Name = "A", Values = [1, 2] }]));
		IRenderedComponent<MokaBarChart> bar = Render<MokaBarChart>(p => p
			.Add(x => x.Series, [new MokaBarSeries { Name = "A", Values = [1, 2] }]));

		Assert.True(line.Instance.IsClassic);
		Assert.True(bar.Instance.IsClassic);
		Assert.Empty(JSInterop.Invocations);
	}

	[Fact]
	public async Task TheLegend_HidesThroughTheChart_AndTellsTheParent()
	{
		this.Static();
		IReadOnlyCollection<string>? told = null;
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p
			.Add(x => x.Keys, ["a", "b"])
			.Add(x => x.HiddenSeriesChanged, hidden => told = hidden));

		await cut.InvokeAsync(() => cut.Instance.Base.SetHiddenAsync(["a"]));

		Assert.Equal(["a"], told);
		Assert.True(cut.Instance.Base.IsHidden("a"));

		cut.Render(p => p.Add(x => x.Keys, ["a", "b"]));
		Assert.True(cut.Instance.Base.IsHidden("a"));
	}

	[Fact]
	public void IsolatingTurnsHidingOn()
	{
		this.Static();
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p.Add(x => x.LegendActions, MokaChartLegendActions.Isolate));

		Assert.Equal(MokaChartLegendActions.Isolate | MokaChartLegendActions.Hide, cut.Instance.Base.ResolvedLegendActions);
	}

	[Fact]
	public void ShowLegendOff_WinsOverAPosition()
	{
		this.Static();
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p
			.Add(x => x.Legend, MokaChartLegendPosition.Top)
			.Add(x => x.ShowLegend, false));

		Assert.Equal(MokaChartLegendPosition.None, cut.Instance.Base.ResolvedLegend);
	}

	[Fact]
	public void TheParts_DrawNothingWithoutAChart()
	{
		Assert.Empty(Render<MokaChartFrame>().Markup.Trim());
		Assert.Empty(Render<MokaChartPlot>().Markup.Trim());
		Assert.Empty(Render<MokaChartSurface>().Markup.Trim());
	}

	[Fact]
	public void TheFrame_DrawsTheTitle_TheBody_AndABottomLegend()
	{
		this.Static();
		IRenderedComponent<TestChart> chart = Render<TestChart>(p => p
			.Add(x => x.Keys, ["LAN", "WAN"])
			.Add(x => x.Title, "Traffic"));

		IRenderedComponent<MokaChartFrame> frame = Render<MokaChartFrame>(p => p
			.Add(x => x.Chart, chart.Instance)
			.Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<i>body</i>"))));

		Assert.Equal("Traffic", frame.Find(".moka-chart-frame__title").TextContent);
		Assert.Equal("body", frame.Find(".moka-chart-frame__body i").TextContent);
		Assert.Equal(["LAN", "WAN"], frame.FindAll(".moka-chart-legend__item").Select(i => i.TextContent.Trim()));
	}

	[Fact]
	public void TheGate_DrawsItsContentOnlyWhenTheVersionMoves()
	{
		int drawn = 0;
		RenderFragment content = b =>
		{
			drawn++;
			b.AddContent(0, "x");
		};

		IRenderedComponent<GateHost> cut = Render<GateHost>(p => p.Add(x => x.Version, 1L).Add(x => x.Content, content));
		cut.Render(p => p.Add(x => x.Version, 1L).Add(x => x.Content, content));
		Assert.Equal(1, drawn);

		cut.Render(p => p.Add(x => x.Version, 2L).Add(x => x.Content, content));
		Assert.Equal(2, drawn);
	}

	/// <summary>Renders its content behind a gate, the way the plot does.</summary>
	public sealed class GateHost : ComponentBase
	{
		/// <summary>The gate's version.</summary>
		[Parameter]
		public long Version { get; set; }

		/// <summary>The gated content.</summary>
		[Parameter]
		public RenderFragment? Content { get; set; }

		/// <inheritdoc />
		protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.AddContent(0, ChartGate.For(Version, Content));
		}
	}

	[Fact]
	public void ChartX_ReadsNumbersAndTimes()
	{
		DateTimeOffset at = new(2026, 3, 4, 12, 30, 0, TimeSpan.Zero);
		MokaChartX time = at;
		MokaChartX number = 4.5;

		Assert.True(time.IsTime);
		Assert.Equal(at, time.Time);
		Assert.False(number.IsTime);
		Assert.Equal(4.5, number.Value);
	}

	[Fact]
	public void TheWords_AreEnglishByDefault_AndSayTheBoardsSentences()
	{
		MokaChartText words = MokaChartText.Default;

		Assert.Equal("LAN hidden. 3 of 4 series shown.", words.SeriesHidden("LAN", 3, 4));
		Assert.Equal("LAN shown. 4 of 4 series shown.", words.SeriesShown("LAN", 4, 4));
		Assert.Equal("Showing LAN only. Do it again, or press Show all, to bring the rest back.", words.SeriesIsolated("LAN"));
		Assert.Equal("All 4 series shown.", words.AllShown(4));
		Assert.Equal("Pinned at 14:30.", words.PinnedAt("14:30"));
		Assert.Equal("Paused · 3 new", words.PausedWaiting(3));
		Assert.Equal("12 ms over the 200 ms line", words.OverThreshold("12 ms", "200 ms"));
		Assert.Equal("5th 80 ms to 95th 240 ms", words.Spread("80 ms", "240 ms"));
	}
}
