using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Data.Chart;
using Moka.Red.Feedback.Tooltip;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     The frame's header: the title, the toolbar with each control on its own flag, replaced or wrapped through
///     <c>ToolbarControl</c>, the app's own actions, and a bar that folds at a narrow width rather than overflowing.
/// </summary>
public sealed class ChartFrameToolbarTests : BunitContext
{
	private static readonly IReadOnlyList<string> Keys = ["LAN", "Wi-Fi"];
	private static readonly IReadOnlyList<IReadOnlyList<double>> Data = [[1, 2, 3], [4, 5, 6]];

	[Fact]
	public void WithoutControlsOrActions_ThereIsNoToolbar()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Title, "Traffic"));

		Assert.Equal("Traffic", cut.Find(".moka-chart-frame__title").TextContent);
		Assert.Empty(cut.FindAll(".moka-chart-frame__tools"));
	}

	[Fact]
	public void EveryControl_SitsInTheToolbar_InItsOwnPlace()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.Controls, MokaChartControls.All)
			.Add(x => x.Times, ChartFrameTestKit.Hours(3))
			.Add(x => x.RangePresets, [new MokaChartRangePreset("1 h", TimeSpan.FromHours(1))]));

		Assert.Equal(["legend", "ranges", "reset-zoom", "view", "export"],
			cut.FindAll(".moka-chart-frame__control").Select(control => control.GetAttribute("data-moka-control")));
		Assert.Equal("toolbar", cut.Find(".moka-chart-frame__tools .moka-toolbar").GetAttribute("role"));
	}

	[Theory]
	[InlineData(MokaChartControls.View, "view")]
	[InlineData(MokaChartControls.ExportPng, "export")]
	[InlineData(MokaChartControls.ResetZoom, "reset-zoom")]
	[InlineData(MokaChartControls.Legend, "legend")]
	public void EachFlag_TurnsOnItsControlAlone(MokaChartControls flag, string control)
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Controls, flag));

		IElement only = Assert.Single(cut.FindAll(".moka-chart-frame__control"));
		Assert.Equal(control, only.GetAttribute("data-moka-control"));
	}

	[Fact]
	public void AChartWithoutATable_OffersNeitherTheViewNorCsv()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.Controls, MokaChartControls.View | MokaChartControls.ExportCsv)
			.Add(x => x.NoTable, true));

		Assert.Empty(cut.FindAll(".moka-chart-frame__tools"));
	}

	[Fact]
	public void RangePresets_ShowOnlyOnATimeAxis()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.Controls, MokaChartControls.Ranges)
			.Add(x => x.RangePresets, [new MokaChartRangePreset("1 h", TimeSpan.FromHours(1))]));

		Assert.Empty(cut.FindAll("[data-moka-control='ranges']"));
	}

	[Fact]
	public void TheControlsThatMustStay_OutrankTheRest()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Controls, MokaChartControls.View | MokaChartControls.Export | MokaChartControls.Legend));

		Assert.Equal("2", cut.Find("[data-moka-control='view']").GetAttribute("data-moka-priority"));
		Assert.Equal("2", cut.Find("[data-moka-control='export']").GetAttribute("data-moka-priority"));
		Assert.Equal("-1", cut.Find("[data-moka-control='legend']").GetAttribute("data-moka-priority"));
	}

	[Fact]
	public void ToolbarControl_IsAskedForEachControl_AndItsAnswerTakesItsPlace()
	{
		this.StaticFrame();
		List<MokaChartControlContext> asked = [];
		RenderFragment<MokaChartControlContext> replace = context => builder =>
		{
			asked.Add(context);
			builder.OpenElement(0, "b");
			builder.AddAttribute(1, "class", "mine");
			builder.AddContent(2, context.Which.ToString());
			builder.CloseElement();
		};

		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.Controls, MokaChartControls.View | MokaChartControls.Export)
			.Add(x => x.ToolbarControl, replace));

		Assert.Equal(["View", "Export"], cut.FindAll(".mine").Select(mine => mine.TextContent));
		Assert.Empty(cut.FindAll(".moka-chart-frame__icon-button"));
		Assert.Contains(asked, context => context.Which == MokaChartControls.View && context.Run is not null);
		Assert.Contains(asked, context => context.Which == MokaChartControls.Export && context.Run is null);
	}

	[Fact]
	public void ToolbarControl_CanKeepTheDefault_OrDropAControl()
	{
		this.StaticFrame();
		RenderFragment<MokaChartControlContext> some = context => builder =>
		{
			if (context.Which != MokaChartControls.View)
			{
				builder.AddContent(0, context.Default);
			}
		};

		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.Controls, MokaChartControls.View | MokaChartControls.ExportPng)
			.Add(x => x.ToolbarControl, some));

		Assert.Empty(cut.Find("[data-moka-control='view']").Children);
		Assert.NotNull(cut.Find("[data-moka-control='export'] .moka-chart-frame__icon-button"));
	}

	[Fact]
	public async Task ARunHandedToToolbarControl_DoesWhatTheControlDoes()
	{
		this.StaticFrame();
		Func<Task>? run = null;
		RenderFragment<MokaChartControlContext> keep = context => builder =>
		{
			if (context.Which == MokaChartControls.View)
			{
				run = context.Run;
			}

			builder.AddContent(0, context.Default);
		};

		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.Controls, MokaChartControls.View)
			.Add(x => x.ToolbarControl, keep));

		await cut.InvokeAsync(run!);

		Assert.Equal(MokaChartView.Table, cut.Instance.Base.CurrentView);
	}

	[Fact]
	public void TheAppsActions_SitAfterTheControls_InTheToolbar()
	{
		this.StaticFrame();
		RenderFragment actions = builder =>
		{
			builder.OpenElement(0, "button");
			builder.AddAttribute(1, "class", "own");
			builder.AddContent(2, "Refresh");
			builder.CloseElement();
		};

		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.Controls, MokaChartControls.View)
			.Add(x => x.Actions, actions));

		IElement items = cut.Find(".moka-chart-frame__tools .moka-toolbar");
		Assert.Equal("own", items.Children[^1].ClassName);
	}

	[Fact]
	public void AnInteractiveToolbar_FoldsWhatDoesntFit()
	{
		(BunitJSModuleInterop fit, _) = this.InteractiveFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Controls, MokaChartControls.View | MokaChartControls.Export));

		Assert.Contains("moka-toolbar--expandable", cut.Find(".moka-chart-frame__tools .moka-toolbar").ClassName, StringComparison.Ordinal);
		Assert.Contains(fit.Invocations, invocation => invocation.Identifier == "bindFit");
	}

	[Fact]
	public void AStaticToolbar_Wraps_AndLoadsNoScript()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Controls, MokaChartControls.View));

		Assert.DoesNotContain("moka-toolbar--expandable", cut.Find(".moka-chart-frame__tools .moka-toolbar").ClassName, StringComparison.Ordinal);
		Assert.DoesNotContain(JSInterop.Invocations, invocation => invocation.Identifier == "import"
		                                                            && invocation.Arguments[0] as string == ChartFrameTestKit.FitModule);
	}

	[Fact]
	public void TheExportButton_IsNamed_AndSitsInATooltip()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Controls, MokaChartControls.Export));

		IElement button = cut.Find(".moka-chart-frame__icon-button");
		Assert.Equal("Export", button.GetAttribute("aria-label"));
		Assert.Null(button.GetAttribute("title"));
		Assert.Contains(cut.FindComponents<MokaTooltip>(), tooltip => tooltip.Instance.Text == "Export");
	}

	[Fact]
	public void TheExportMenu_OffersEachFormatAskedFor()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Controls, MokaChartControls.ExportPng | MokaChartControls.ExportCsv));

		cut.Find(".moka-chart-frame__icon-button").Click();

		Assert.Equal(["PNG image", "CSV data"], cut.FindAll(".moka-chart-frame__export-text").Select(item => item.TextContent));
	}

	[Fact]
	public void TheViewSwitch_ShowsTheTable_AndTellsTheParent()
	{
		this.StaticFrame();
		MokaChartView? told = null;
		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.Controls, MokaChartControls.View)
			.Add(x => x.ViewChanged, view => told = view));

		IElement group = cut.Find("[data-moka-control='view'] [role='radiogroup']");
		Assert.Equal("Show as", group.GetAttribute("aria-label"));
		cut.FindAll("[data-moka-control='view'] input[type='radio']")[1].Change(true);

		Assert.Equal(MokaChartView.Table, told);
		Assert.NotNull(cut.Find(".moka-chart-frame__table"));
	}

	[Fact]
	public void TheWords_ComeFromText()
	{
		this.StaticFrame();
		MokaChartText german = MokaChartText.Default with { ChartView = "Diagramm", TableView = "Tabelle", Export = "Exportieren" };
		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.Controls, MokaChartControls.View | MokaChartControls.ExportPng)
			.Add(x => x.Text, german));

		Assert.Equal(["Diagramm", "Tabelle"], cut.FindAll(".moka-segment__text").Select(text => text.TextContent));
		Assert.Equal("Exportieren", cut.Find(".moka-chart-frame__icon-button").GetAttribute("aria-label"));
	}

	[Theory]
	[InlineData(MokaChartControls.View, true)]
	[InlineData(MokaChartControls.None, false)]
	public void AFrameWithControls_IsInteractive_SoTheChartIsAFigure(MokaChartControls controls, bool interactive)
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Controls, controls));

		Assert.Equal(interactive, cut.Instance.Base.FrameInteractive);
	}

	private IRenderedComponent<FrameChart> Chart(Action<ComponentParameterCollectionBuilder<FrameChart>> more) =>
		Render<FrameChart>(p =>
		{
			p.Add(x => x.Keys, Keys).Add(x => x.Data, Data);
			more(p);
		});
}
