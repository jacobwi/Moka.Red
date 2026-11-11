using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     The row plot: names down and values across, the value axis on top, names whole on one line with the whole name in
///     a tooltip, names above their rows on a narrow chart, and long lists drawn through <c>Virtualize</c> with every row
///     exactly as tall as the item size it is given.
/// </summary>
public sealed class ChartRowsTests : BunitContext
{
	private static readonly string[] Domains =
	[
		"telemetry.microsoft.com", "i.ytimg.com", "graph.facebook.com", "api.github.com", "time.cloudflare.com",
		"s3.eu-central-1.amazonaws.com"
	];

	[Fact]
	public void ShortLists_DrawEveryRow_AtTheLooksExactHeight_InOneSubgrid()
	{
		this.Static();
		IRenderedComponent<ChartAxisTestChart> cut = Rows(Domains);

		Assert.Equal(6, cut.FindAll(".moka-chart-plot__row").Count);
		IElement plot = cut.Find(".moka-chart-plot");
		Assert.Contains("--_row-h: 28px", plot.GetAttribute("style"), StringComparison.Ordinal);
		Assert.DoesNotContain("moka-chart-plot--virtual", plot.ClassName, StringComparison.Ordinal);
		Assert.Empty(cut.FindComponents<Virtualize<int>>());
		Assert.Contains("grid-template-columns: fit-content(40%) minmax(0, 1fr) auto;", ChartAxisPlotTests.ScopedCssText(),
			StringComparison.Ordinal);
	}

	[Fact]
	public void Names_AreOneLine_AndKeepTheWholeNameInATitle()
	{
		this.Static();
		IRenderedComponent<ChartAxisTestChart> cut = Rows(Domains);

		Assert.All(cut.FindAll(".moka-chart-plot__rname"), name => Assert.Equal(name.TextContent, name.GetAttribute("title")));
		Assert.Contains(".moka-chart-plot__rname {\n\tgrid-column: 1;\n\tmin-width: 0;\n\toverflow: hidden;", ChartAxisPlotTests.ScopedCssText(),
			StringComparison.Ordinal);
	}

	// The longest name is 29 characters, 19.1 em; kept beside its row it may take 40% of the chart, so names move above
	// on a chart under 48 em. Short names never move above until 24 em.
	[Fact]
	public void Names_MoveAbove_WhenTheChartIsTooNarrowToKeepThemWholeBeside()
	{
		this.Static();
		Assert.Contains("moka-chart-plot--above-w48", Rows(Domains).Find(".moka-chart-plot").ClassName, StringComparison.Ordinal);
		Assert.Contains("moka-chart-plot--above-w24", Rows(["LAN", "Guest", "IoT"]).Find(".moka-chart-plot").ClassName, StringComparison.Ordinal);
		Assert.Contains("@container moka-chart-rows (width < 48em)", ChartAxisPlotTests.ScopedCssText(), StringComparison.Ordinal);
	}

	[Fact]
	public void RowHeight_AndTheLook_SetTheExactHeight()
	{
		this.Static();
		Assert.Contains("--_row-h: 40px", Rows(Domains, p => p.Add(x => x.RowHeight, 40)).Find(".moka-chart-plot").GetAttribute("style"),
			StringComparison.Ordinal);
		IRenderedComponent<ChartAxisTestChart> dense = Rows(Domains, p => p.Add(x => x.Look, MokaChartLook.Dense).AddCascadingValue(MokaTheme.Default));
		Assert.Contains("--_row-h: 22px", dense.Find(".moka-chart-plot").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void LongLists_GoThroughVirtualize_WithTheRowHeightAsItsItemSize_AndOverscan6()
	{
		this.Interactive();
		JSInterop.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<ChartAxisTestChart> cut = Rows([.. Enumerable.Range(0, 500).Select(i => $"device-{i}.lan")]);

		IElement plot = cut.Find(".moka-chart-plot");
		Assert.Contains("moka-chart-plot--virtual", plot.ClassName, StringComparison.Ordinal);
		Assert.Contains("--_name-w: min(40%,", plot.GetAttribute("style"), StringComparison.Ordinal);
		Virtualize<int> virtualize = cut.FindComponent<Virtualize<int>>().Instance;
		Assert.Equal(28f, virtualize.ItemSize);
		Assert.Equal(6, virtualize.OverscanCount);
		Assert.Equal(500, virtualize.Items!.Count);
		Assert.True(cut.FindAll(".moka-chart-plot__row").Count < 500);
		Assert.Contains(".moka-chart-plot--virtual .moka-chart-plot__row {\n\tdisplay: block;", ChartAxisPlotTests.ScopedCssText()
			.Replace(".moka-chart-plot--virtual .moka-chart-plot__rbody,\n", "", StringComparison.Ordinal), StringComparison.Ordinal);
	}

	[Fact]
	public void AStaticRender_DrawsEveryRow_SinceVirtualizeNeedsScript()
	{
		this.Static();
		IRenderedComponent<ChartAxisTestChart> cut = Rows([.. Enumerable.Range(0, 300).Select(i => $"device-{i}.lan")]);

		Assert.Equal(300, cut.FindAll(".moka-chart-plot__row").Count);
		Assert.DoesNotContain("moka-chart-plot--virtual", cut.Find(".moka-chart-plot").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void VirtualizeFalse_KeepsEveryRow_AndTrue_VirtualizesAShortList()
	{
		this.Interactive();
		JSInterop.Mode = JSRuntimeMode.Loose;
		Assert.Equal(300, Rows([.. Enumerable.Range(0, 300).Select(i => $"d{i}")], p => p.Add(x => x.Virtualize, false))
			.FindAll(".moka-chart-plot__row").Count);
		Assert.Contains("moka-chart-plot--virtual", Rows(Domains, p => p.Add(x => x.Virtualize, true)).Find(".moka-chart-plot").ClassName,
			StringComparison.Ordinal);
	}

	[Fact]
	public void Figures_HaveAColumn_AndRowsWithoutThemHaveTwo()
	{
		this.Static();
		IRenderedComponent<ChartAxisTestChart> withFigures = Rows(Domains, p => p.Add(x => x.Figures, Domains.Select(d => $"{d.Length} q").ToArray()));
		Assert.Equal("23 q", withFigures.Find(".moka-chart-plot__rfig").TextContent);
		Assert.DoesNotContain("moka-chart-plot--no-figure", withFigures.Find(".moka-chart-plot").ClassName, StringComparison.Ordinal);

		Assert.Contains("moka-chart-plot--no-figure", Rows(Domains).Find(".moka-chart-plot").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void NameAndEndTemplates_DrawTheCells()
	{
		this.Static();
		RenderFragment<int> name = row => builder => builder.AddMarkupContent(0, $"<i>row {row}</i>");
		RenderFragment<int> end = row => builder => builder.AddMarkupContent(0, $"<b>{row * 10}</b>");
		IRenderedComponent<MokaChartPlot> plot = Render<ChartAxisTestChart>(p => p.Add(x => x.Values, [1d, 2]).Add(x => x.Labels, ["a", "b"])
			.Add(x => x.Rows, true)).FindComponent<MokaChartPlot>();

		plot.Render(p => p.Add(x => x.Chart, plot.Instance.Chart).Add(x => x.Row, row => builder => { })
			.Add(x => x.RowName, name).Add(x => x.RowEnd, end));

		Assert.Equal(["row 0", "row 1"], plot.FindAll(".moka-chart-plot__rname i").Select(i => i.TextContent));
		Assert.Equal(["0", "10"], plot.FindAll(".moka-chart-plot__rfig b").Select(b => b.TextContent));
		Assert.Equal("a", plot.FindAll(".moka-chart-plot__rname")[0].GetAttribute("title"));
	}

	[Fact]
	public void TheValueAxis_IsOnTop_ThinsAcross_AndItsGridLinesHideWithTheirLabels()
	{
		this.Static();
		IRenderedComponent<ChartAxisTestChart> cut = Rows(Domains);

		IElement head = cut.Find(".moka-chart-plot__rhead");
		Assert.Equal(["0", "1.25", "2.5", "3.75", "5"], head.QuerySelectorAll(".moka-chart-plot__xtick").Select(t => t.TextContent));
		Assert.Contains("position: sticky;", ChartAxisPlotTests.ScopedCssText(), StringComparison.Ordinal);
		IReadOnlyList<IElement> lines = cut.FindAll(".moka-chart-plot__rlines .moka-chart-plot__vgrid:not(.moka-chart-plot__gline--base)");
		List<IElement> labels = [.. head.QuerySelectorAll(".moka-chart-plot__xtick").Skip(1)];
		for (int i = 0; i < lines.Count; i++)
		{
			string? width = labels[i].ClassList.FirstOrDefault(c => c.StartsWith("moka-chart-plot__xtick--w", StringComparison.Ordinal));
			Assert.Equal(width is not null, width is not null && lines[i].ClassList.Contains(width));
		}

		Assert.Single(cut.FindAll(".moka-chart-plot__gline--base"));
	}

	[Fact]
	public void MaxHeight_MakesTheRowsScroll_UnderTheStickyAxis()
	{
		this.Static();
		IRenderedComponent<ChartAxisTestChart> cut = Rows(Domains, p => p.Add(x => x.MaxHeight, "160px"));

		Assert.Contains("moka-chart-plot--scroller", cut.Find(".moka-chart-plot").ClassName, StringComparison.Ordinal);
		Assert.Contains("max-height: 160px", cut.Find(".moka-chart-plot__rscroll").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void ATimeSpanAcross_StepsByTheClock_WithNoBaseline()
	{
		this.Static();
		DateTimeOffset from = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);
		IRenderedComponent<ChartAxisTestChart> cut = Rows(["Backup", "Index"], p => p.Add(x => x.TimeSpanAcross, (from, from.AddHours(6)))
			.Add(x => x.TimeZone, TimeZoneInfo.Utc));

		Assert.Contains(cut.FindAll(".moka-chart-plot__rhead .moka-chart-plot__xtick"), t => t.ClassList.Contains("moka-chart-plot__xtick--day"));
		Assert.Empty(cut.FindAll(".moka-chart-plot__gline--base"));
	}

	[Fact]
	public void ThresholdsAndBands_RunDownTheRows()
	{
		this.Static();
		IRenderedComponent<ChartAxisTestChart> cut = Rows(Domains, p => p
			.Add(x => x.Annotations, [new MokaChartThreshold(4) { Label = "Quota" }, new MokaChartBand(1, 2)]));

		Assert.Equal("Quota", cut.Find(".moka-chart-plot__vthreshold").TextContent.Trim());
		Assert.Single(cut.FindAll(".moka-chart-plot__vband"));
	}

	private IRenderedComponent<ChartAxisTestChart> Rows(string[] names, Action<ComponentParameterCollectionBuilder<ChartAxisTestChart>>? set = null) =>
		Render<ChartAxisTestChart>(p =>
		{
			p.Add(x => x.Values, [.. names.Select((_, i) => (double)(i % 5) + 1)]);
			p.Add(x => x.Labels, names);
			p.Add(x => x.Rows, true);
			set?.Invoke(p);
		});
}
