using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using Moka.Red.Forms.RangeBar;
using static Moka.Red.Data.Tests.Charts.MokaLineChartTestKit;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>What .NET tells the script, and hears from it, when charts link their zoom with <c>SyncZoom</c>.</summary>
public sealed class ChartLinkedZoomTests : BunitContext
{
	private static readonly string[] Hours = ["00:00", "01:00", "02:00", "03:00"];
	private static readonly DateTimeOffset Start = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);
	private static readonly DateTimeOffset[] Times = [Start, Start.AddHours(1), Start.AddHours(2), Start.AddHours(3)];

	private readonly BunitJSModuleInterop _module;

	public ChartLinkedZoomTests()
	{
		_module = this.Interactive();
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void ALinkedChart_TellsTheScriptWhatItsXAxisCountsIn()
	{
		Linked(p => p.Add(x => x.Times, Times));

		Dictionary<string, object?> options = BindOptions();
		Assert.Equal("time", options["link"]);
		Assert.Equal("together", options["sync"]);
	}

	[Fact]
	public void WithoutASyncGroup_OrWithoutSyncZoom_NothingLinks()
	{
		Render<MokaLineChart>(p => p.Add(x => x.Look, MokaChartLook.Quiet).Add(x => x.SyncZoom, true).Add(x => x.Times, Times)
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]));
		Render<MokaLineChart>(p => p.Add(x => x.Look, MokaChartLook.Quiet).Add(x => x.SyncGroup, "together").Add(x => x.Times, Times)
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]));

		Assert.All(_module.Invocations.Where(i => i.Identifier == "bind"),
			bind => Assert.Null(Assert.IsType<Dictionary<string, object?>>(bind.Arguments[2])["link"]));
	}

	[Fact]
	public void Categories_LinkOnlyWithTheSameLabels()
	{
		IRenderedComponent<MokaLineChart> a = Linked(p => p.Add(x => x.Labels, Hours));
		IRenderedComponent<MokaLineChart> b = Linked(p => p.Add(x => x.Labels, Hours));
		IRenderedComponent<MokaLineChart> c = Linked(p => p.Add(x => x.Labels, ["Mon", "Tue", "Wed", "Thu"]));

		Assert.StartsWith("category:4:", a.Instance.ZoomLinkKey, StringComparison.Ordinal);
		Assert.Equal(a.Instance.ZoomLinkKey, b.Instance.ZoomLinkKey);
		Assert.NotEqual(a.Instance.ZoomLinkKey, c.Instance.ZoomLinkKey);
	}

	[Fact]
	public void AReversedAxis_LinksOnlyWithReversedOnes()
	{
		IRenderedComponent<MokaLineChart> cut = Linked(p => p.Add(x => x.Times, Times).Add(x => x.XAxis, new MokaChartAxis { Reverse = true }));

		Assert.Equal("time:reverse", cut.Instance.ZoomLinkKey);
	}

	[Fact]
	public void ALinkedChart_BindsWithoutGesturesOfItsOwn_AndWritesTheSpanItShows()
	{
		IRenderedComponent<MokaLineChart> cut = Linked(p => p.Add(x => x.Times, Times), MokaChartLook.Standard);

		Assert.Single(_module.Invocations, i => i.Identifier == "bind");
		IElement surface = cut.Find(".moka-chart-surface");
		Assert.Equal(surface.GetAttribute("data-moka-x0"), surface.GetAttribute("data-moka-w0"));
		Assert.Equal(surface.GetAttribute("data-moka-x1"), surface.GetAttribute("data-moka-w1"));
	}

	[Fact]
	public void AChartThatDoesntLink_WritesNoSpan()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p.Add(x => x.Look, MokaChartLook.Quiet).Add(x => x.Times, Times)
			.Add(x => x.Zoom, MokaChartZoom.Brush).Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]));

		Assert.False(cut.Find(".moka-chart-surface").HasAttribute("data-moka-w0"));
	}

	[Fact]
	public async Task TheStrip_AndResetZoom_TellTheScriptToShareTheRange()
	{
		IRenderedComponent<MokaLineChart> cut = Linked(p => p.Add(x => x.Times, Times).Add(x => x.Zoom, MokaChartZoom.Strip));
		MokaRangeBar strip = cut.FindComponent<MokaRangeBar>().Instance;
		double from = Times[1].ToUnixTimeMilliseconds();
		double to = Times[2].ToUnixTimeMilliseconds();

		await cut.InvokeAsync(() => strip.ValueChanged.InvokeAsync(new MokaRange(from, to)));
		JSRuntimeInvocation shared = Assert.Single(_module.Invocations, i => i.Identifier == "shareZoom");
		Assert.Equal(from, shared.Arguments[1]);
		Assert.Equal(to, shared.Arguments[2]);

		await cut.InvokeAsync(() => cut.Instance.ResetZoomAsync());
		JSRuntimeInvocation reset = _module.Invocations.Last(i => i.Identifier == "shareZoom");
		Assert.Null(reset.Arguments[1]);
		Assert.Null(reset.Arguments[2]);
	}

	[Fact]
	public async Task AZoomTheScriptChose_IsNeverSentBack()
	{
		IRenderedComponent<MokaLineChart> cut = Linked(p => p.Add(x => x.Times, Times));

		await cut.InvokeAsync(() => cut.Instance.OnRange(Times[1].ToUnixTimeMilliseconds(), Times[2].ToUnixTimeMilliseconds()));

		Assert.DoesNotContain(_module.Invocations, i => i.Identifier == "shareZoom");
		Assert.NotNull(cut.Instance.CurrentRange);
	}

	[Fact]
	public async Task AChartNotLinked_SharesNothing()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Times, Times).Add(x => x.Zoom, MokaChartZoom.Strip).Add(x => x.SyncGroup, "together")
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]));

		await cut.InvokeAsync(() => cut.Instance.ResetZoomAsync());

		Assert.DoesNotContain(_module.Invocations, i => i.Identifier == "shareZoom");
	}

	[Fact]
	public async Task ARangeTheParentAlreadyPassed_RaisesNoChange_SoBindingSeveralChartsCantLoop()
	{
		MokaChartRange shown = new(Times[1], Times[2]);
		int changes = 0;
		IRenderedComponent<MokaLineChart> cut = Linked(p => p.Add(x => x.Times, Times).Add(x => x.Range, shown)
			.Add(x => x.RangeChanged, (MokaChartRange? _) => changes++));

		await cut.InvokeAsync(() => cut.Instance.OnRange(shown.From.Value, shown.To.Value));
		Assert.Equal(0, changes);

		await cut.InvokeAsync(() => cut.Instance.OnRange(Times[0].ToUnixTimeMilliseconds(), Times[2].ToUnixTimeMilliseconds()));
		cut.WaitForAssertion(() => Assert.Equal(1, changes));
	}

	private IRenderedComponent<MokaLineChart> Linked(Action<ComponentParameterCollectionBuilder<MokaLineChart>> more,
		MokaChartLook look = MokaChartLook.Quiet) =>
		Render<MokaLineChart>(p =>
		{
			p.Add(x => x.Look, look)
				.Add(x => x.SyncGroup, "together")
				.Add(x => x.SyncZoom, true)
				.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]);
			more(p);
		});

	private Dictionary<string, object?> BindOptions() =>
		Assert.IsType<Dictionary<string, object?>>(Assert.Single(_module.Invocations, i => i.Identifier == "bind").Arguments[2]);
}
