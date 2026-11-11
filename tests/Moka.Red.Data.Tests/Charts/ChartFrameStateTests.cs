using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Data.Chart;
using Moka.Red.Feedback.Alert;
using Moka.Red.Feedback.EmptyState;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     Loading, empty and error, drawn inside the frame at the chart's own size: loading keeps the last drawing dimmed
///     with an Updating chip, empty says so, and an error offers Try again when the app listens for it.
/// </summary>
public sealed class ChartFrameStateTests : BunitContext
{
	private static readonly IReadOnlyList<string> Keys = ["LAN"];
	private static readonly IReadOnlyList<IReadOnlyList<double>> Data = [[1, 2]];

	[Fact]
	public void Ready_DrawsNoStateLayer()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart();

		Assert.Null(cut.Find(".moka-chart-frame").GetAttribute("data-moka-state"));
		Assert.Empty(cut.FindAll(".moka-chart-frame__state, .moka-chart-frame__chip"));
		Assert.Equal(string.Empty, Status(cut));
	}

	[Fact]
	public void Loading_KeepsTheDrawing_Dimmed_WithAnUpdatingChip()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.State, MokaChartState.Loading));

		Assert.Contains("moka-chart-frame--loading", cut.Find(".moka-chart-frame").ClassName, StringComparison.Ordinal);
		Assert.NotNull(cut.Find(".moka-chart-frame__chart .test-plot"));
		Assert.DoesNotContain("--hidden", cut.Find(".moka-chart-frame__chart").ClassName, StringComparison.Ordinal);
		IElement chip = cut.Find(".moka-chart-frame__chip");
		Assert.Equal("Updating", chip.TextContent.Trim());
		Assert.Equal("true", chip.GetAttribute("aria-hidden"));
		Assert.Equal("Updating", Status(cut));
	}

	[Fact]
	public void Empty_WithoutData_SaysSo_AtTheChartsOwnSize()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Render<FrameChart>(p => p.Add(x => x.Keys, ["LAN"]).Add(x => x.Data, [[]]));

		Assert.Equal("empty", cut.Find(".moka-chart-frame").GetAttribute("data-moka-state"));
		Assert.Contains("moka-chart-frame__chart--hidden", cut.Find(".moka-chart-frame__chart").ClassName, StringComparison.Ordinal);
		Assert.True(cut.FindComponent<MokaEmptyState>().Instance.Compact);
		Assert.Equal("No data", cut.FindComponent<MokaEmptyState>().Instance.Title);
		Assert.Equal("No data", Status(cut));
	}

	[Fact]
	public void Empty_SaysTheEmptyText_OrDrawsTheEmptyContent()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> said = Chart(p => p
			.Add(x => x.State, MokaChartState.Empty)
			.Add(x => x.EmptyText, "No traffic in this range"));
		IRenderedComponent<FrameChart> drawn = Chart(p => p
			.Add(x => x.State, MokaChartState.Empty)
			.Add(x => x.EmptyContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p class=\"own\">Nothing yet</p>"))));

		Assert.Equal("No traffic in this range", said.FindComponent<MokaEmptyState>().Instance.Title);
		Assert.Equal("Nothing yet", drawn.Find(".moka-chart-frame__state .own").TextContent);
		Assert.Empty(drawn.FindComponents<MokaEmptyState>());
	}

	[Fact]
	public void Error_SaysWhatFailed_WithoutTryAgain_WhenNothingListens()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.State, MokaChartState.Error)
			.Add(x => x.ErrorText, "The router didn't answer."));

		IRenderedComponent<MokaAlert> alert = cut.FindComponent<MokaAlert>();
		Assert.Contains("The router didn't answer.", alert.Markup, StringComparison.Ordinal);
		Assert.Null(alert.Instance.FixAction);
		Assert.Contains("moka-chart-frame__chart--hidden", cut.Find(".moka-chart-frame__chart").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Error_HasADefaultSentence()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.State, MokaChartState.Error));

		Assert.Contains("Couldn't load the data.", cut.FindComponent<MokaAlert>().Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void TryAgain_CallsOnRetry()
	{
		this.StaticFrame();
		int retried = 0;
		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.State, MokaChartState.Error)
			.Add(x => x.OnRetry, () => retried++));

		IElement button = cut.Find(".moka-chart-frame__state .moka-alert-fix button");
		Assert.Equal("Try again", button.TextContent.Trim());
		button.Click();

		Assert.Equal(1, retried);
	}

	[Fact]
	public void ErrorContent_TakesTheAlertsPlace_AndTheStatusSaysTheError()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.State, MokaChartState.Error)
			.Add(x => x.ErrorText, "Offline")
			.Add(x => x.ErrorContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p class=\"own\">Down</p>"))));

		Assert.Empty(cut.FindComponents<MokaAlert>());
		Assert.Equal("Down", cut.Find(".moka-chart-frame__state .own").TextContent);
		Assert.Equal("Offline", Status(cut));
	}

	[Fact]
	public void TheStateWords_ComeFromText()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.State, MokaChartState.Loading)
			.Add(x => x.Text, MokaChartText.Default with { Updating = "Lädt" }));

		Assert.Equal("Lädt", cut.Find(".moka-chart-frame__chip").TextContent.Trim());
	}

	[Fact]
	public void AStateChange_RendersWithoutRebuilding()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.State, MokaChartState.Ready));
		long version = cut.Instance.Base.GeometryVersion;

		cut.Render(p => p.Add(x => x.Keys, Keys).Add(x => x.Data, Data).Add(x => x.State, MokaChartState.Loading));

		Assert.Equal(version, cut.Instance.Base.GeometryVersion);
		Assert.NotNull(cut.Find(".moka-chart-frame__chip"));
	}

	private static string Status(IRenderedComponent<FrameChart> cut) =>
		cut.Find(".moka-chart-frame > [role='status']").TextContent;

	private IRenderedComponent<FrameChart> Chart(Action<ComponentParameterCollectionBuilder<FrameChart>>? more = null) =>
		Render<FrameChart>(p =>
		{
			p.Add(x => x.Keys, Keys).Add(x => x.Data, Data);
			more?.Invoke(p);
		});
}
