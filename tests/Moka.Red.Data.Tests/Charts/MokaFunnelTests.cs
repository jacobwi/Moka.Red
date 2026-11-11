using AngleSharp.Dom;
using Bunit;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

public sealed class MokaFunnelTests : BunitContext
{
	private static readonly MokaFunnelStep[] Setup =
	[
		new("Visited", 1000), new("Signed up", 620), new("Signed in", 310), new("Named a device", 200), new("Trusted", 120),
		new("Pinned", 30)
	];

	public MokaFunnelTests() => this.Static();

	[Fact]
	public void DrawsABarPerStep_FromOneStart_InOneHueAStage()
	{
		IRenderedComponent<MokaFunnel> cut = Render<MokaFunnel>(p => p.Add(x => x.Steps, Setup));

		IReadOnlyList<IElement> lanes = cut.FindAll(".moka-funnel__lane");
		Assert.Equal(6, lanes.Count);
		Assert.All(lanes, lane => Assert.Equal("0%", RowChartKit.Var(lane, "--_fn-from")));
		Assert.Equal(["var(--moka-chart-ord-1)", "var(--moka-chart-ord-2)", "var(--moka-chart-ord-3)", "var(--moka-chart-ord-4)",
			"var(--moka-chart-ord-5)", "var(--moka-chart-ord-5)"], lanes.Select(lane => RowChartKit.Var(lane, "--moka-chart-color")));
		double first = RowChartKit.Percent(RowChartKit.Var(lanes[0], "--_fn-len"));
		Assert.Equal(first * 0.62, RowChartKit.Percent(RowChartKit.Var(lanes[1], "--_fn-len")), 2);
		Assert.Equal("1,000", cut.Find(".moka-funnel__figure").TextContent);
		Assert.All(cut.FindAll(".moka-funnel__bar"), bar => Assert.Contains("moka-chart-hatch", bar.ClassList));
	}

	[Fact]
	public void TheShareThatWentOn_IsWrittenUnderEachBarButTheLast()
	{
		IRenderedComponent<MokaFunnel> cut = Render<MokaFunnel>(p => p.Add(x => x.Steps, Setup));

		Assert.Equal(["62% went on", "50% went on", "65% went on", "60% went on", "25% went on"],
			cut.FindAll(".moka-funnel__on").Select(on => on.TextContent));
		Assert.Contains("--_row-h: 39px", cut.Find(".moka-chart-plot").GetAttribute("style"), StringComparison.Ordinal);

		cut.Render(p => p.Add(x => x.ShowConversion, false).Add(x => x.ConversionText, share => $"{share} weiter"));
		Assert.Empty(cut.FindAll(".moka-funnel__on"));
		Assert.Contains("--_row-h: 28px", cut.Find(".moka-chart-plot").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void TheTooltip_SaysTheShareOfTheStepBefore_AndOfAll()
	{
		IRenderedComponent<MokaFunnel> cut = Render<MokaFunnel>(p => p.Add(x => x.Steps, Setup));

		Assert.Empty(cut.Instance.DescribeActive(0, -1, 0, null)!.Notes);
		Assert.Equal(["62% of Visited"], cut.Instance.DescribeActive(0, -1, 1, null)!.Notes);
		ChartActiveInfo signedIn = cut.Instance.DescribeActive(0, -1, 2, null)!;
		Assert.Equal(["50% of Signed up", "31% of all"], signedIn.Notes);
		Assert.Equal("Signed in, Value 310, 50% of Signed up, 31% of all", signedIn.CellText);
		Assert.Equal(0.31, signedIn.Context.Points[0].Share!.Value, 6);
	}

	[Fact]
	public void TheWords_CanBeReplaced()
	{
		IRenderedComponent<MokaFunnel> cut = Render<MokaFunnel>(p => p
			.Add(x => x.Steps, Setup)
			.Add(x => x.ShareText, (share, of) => $"{share} von {of}")
			.Add(x => x.AllText, "allen")
			.Add(x => x.ConversionText, share => $"{share} weiter"));

		Assert.Equal(["50% von Signed up", "31% von allen"], cut.Instance.DescribeActive(0, -1, 2, null)!.Notes);
		Assert.Equal("62% weiter", cut.Find(".moka-funnel__on").TextContent);
	}

	[Fact]
	public void TheTable_HasTheShareOfAllAndOfTheStepBefore()
	{
		IRenderedComponent<MokaFunnel> cut = Render<MokaFunnel>(p => p.Add(x => x.Steps, Setup));

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal(["Name", "Value", "Share", "Went on"], table.Headers);
		Assert.Equal("", table.Display(0, 3));
		Assert.Equal("62%", table.Display(1, 3));
		Assert.Equal("0.31", table.Raw(2, 2));
	}

	[Fact]
	public void Highlight_MutesTheOtherSteps()
	{
		IRenderedComponent<MokaFunnel> cut = Render<MokaFunnel>(p => p.Add(x => x.Steps, Setup).Add(x => x.Highlight, "Signed in"));

		IReadOnlyList<IElement> lanes = cut.FindAll(".moka-funnel__lane");
		Assert.DoesNotContain("moka-chart-muted", lanes[2].ClassList);
		Assert.Contains("moka-chart-muted", lanes[0].ClassList);
		Assert.Null(RowChartKit.Var(lanes[0], "--moka-chart-color"));
	}
}
