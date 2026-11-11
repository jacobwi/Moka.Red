using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Data.Chart;
using Moka.Red.Feedback.Alert;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     Export: a PNG or SVG of the chart as drawn, built in the lazily loaded export module from the frame's own
///     element, and the values as CSV, written in .NET a few rows at a time and streamed to the browser.
/// </summary>
public sealed class ChartExportTests : BunitContext
{
	[Fact]
	public void NothingImportsTheExportModule_UntilAnExportIsPicked()
	{
		this.InteractiveFrame();
		Chart(p => p.Add(x => x.Controls, MokaChartControls.Export));

		Assert.DoesNotContain(JSInterop.Invocations, invocation => invocation.Identifier == "import"
		                                                            && invocation.Arguments[0] as string == ChartFrameTestKit.ExportModule);
	}

	[Theory]
	[InlineData(MokaChartControls.ExportPng, "png", "traffic-today.png")]
	[InlineData(MokaChartControls.ExportSvg, "svg", "traffic-today.svg")]
	public async Task APicture_IsTakenFromTheFrame_ByTheExportModule(MokaChartControls format, string kind, string file)
	{
		(_, BunitJSModuleInterop export) = this.InteractiveFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Title, "Traffic today"));

		await cut.InvokeAsync(() => cut.Instance.ExportAsync(format));

		JSRuntimeInvocation call = Assert.Single(export.Invocations, invocation => invocation.Identifier == "exportPicture");
		Assert.IsType<ElementReference>(call.Arguments[0]);
		Assert.Equal(kind, call.Arguments[1]);
		Assert.Equal(file, call.Arguments[2]);
		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(call.Arguments[3]);
		Assert.Equal(2, options["scale"]);
	}

	[Fact]
	public void ThePicture_IsTheFramesElement_NotTheChartsRoot()
	{
		this.InteractiveFrame();
		IRenderedComponent<FrameChart> cut = Chart();

		Assert.NotNull(cut.Instance.Base.FrameElement);
	}

	[Fact]
	public async Task Csv_IsStreamed_WithItsFormulasGuarded()
	{
		(_, BunitJSModuleInterop export) = this.InteractiveFrame();
		IRenderedComponent<FrameChart> cut = Render<FrameChart>(p => p
			.Add(x => x.Keys, ["=cmd", "Wi-Fi, 5 GHz"])
			.Add(x => x.Data, [[1.5, -2], [3, 4]])
			.Add(x => x.ExportName, "my export"));

		await cut.InvokeAsync(() => cut.Instance.ExportAsync(MokaChartControls.ExportCsv));

		JSRuntimeInvocation call = Assert.Single(export.Invocations, invocation => invocation.Identifier == "download");
		DotNetStreamReference stream = Assert.IsType<DotNetStreamReference>(call.Arguments[0]);
		Assert.Equal("my export.csv", call.Arguments[1]);
		Assert.Equal("text/csv;charset=utf-8", call.Arguments[2]);

		using MemoryStream copy = new();
		await stream.Stream.CopyToAsync(copy, Xunit.TestContext.Current.CancellationToken);
		byte[] bytes = copy.ToArray();
		Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
		Assert.Equal("Point,'=cmd,\"Wi-Fi, 5 GHz\"\r\n0,1.5,3\r\n1,-2,4\r\n", Encoding.UTF8.GetString(bytes[3..]));
	}

	[Fact]
	public async Task Csv_OfManyRows_ArrivesInPieces_AndWhole()
	{
		(_, BunitJSModuleInterop export) = this.InteractiveFrame();
		double[] values = [.. Enumerable.Range(0, 20_000).Select(i => (double)i)];
		IRenderedComponent<FrameChart> cut = Render<FrameChart>(p => p.Add(x => x.Keys, ["A"]).Add(x => x.Data, [values]));

		await cut.InvokeAsync(() => cut.Instance.ExportAsync(MokaChartControls.ExportCsv));

		DotNetStreamReference stream = Assert.IsType<DotNetStreamReference>(
			Assert.Single(export.Invocations, invocation => invocation.Identifier == "download").Arguments[0]);
		byte[] buffer = new byte[4096];
		int first = await stream.Stream.ReadAsync(buffer, Xunit.TestContext.Current.CancellationToken);
		Assert.Equal(4096, first);
		using StreamReader reader = new(stream.Stream);
		string rest = await reader.ReadToEndAsync(Xunit.TestContext.Current.CancellationToken);
		Assert.EndsWith("19999,19999\r\n", rest, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Csv_DoesNothing_OnAChartWithoutATable()
	{
		(_, BunitJSModuleInterop export) = this.InteractiveFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.NoTable, true));

		await cut.InvokeAsync(() => cut.Instance.ExportAsync(MokaChartControls.ExportCsv));

		Assert.Empty(export.Invocations);
	}

	[Fact]
	public async Task OnlyOneFormat_IsExportedAtATime()
	{
		this.InteractiveFrame();
		IRenderedComponent<FrameChart> cut = Chart();

		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
			cut.InvokeAsync(() => cut.Instance.ExportAsync(MokaChartControls.Export)));
	}

	[Fact]
	public void PickingAnExport_InTheMenu_RunsIt()
	{
		(_, BunitJSModuleInterop export) = this.InteractiveFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Controls, MokaChartControls.ExportSvg).Add(x => x.Label, "Load"));

		cut.Find(".moka-chart-frame__icon-button").Click();
		cut.Find(".moka-chart-frame__export-item").Click();

		JSRuntimeInvocation call = Assert.Single(export.Invocations, invocation => invocation.Identifier == "exportPicture");
		Assert.Equal("load.svg", call.Arguments[2]);
	}

	[Fact]
	public async Task AFailedExport_ShowsInTheFrame_UntilPutAway()
	{
		(_, BunitJSModuleInterop export) = this.InteractiveFrame();
		export.SetupVoid("exportPicture", _ => true).SetException(new JSException("No canvas"));
		IRenderedComponent<FrameChart> cut = Chart();

		await cut.InvokeAsync(() => cut.Instance.ExportAsync(MokaChartControls.ExportPng));

		IRenderedComponent<MokaAlert> alert = cut.FindComponent<MokaAlert>();
		Assert.Contains("Couldn't save the chart", alert.Markup, StringComparison.Ordinal);

		await cut.InvokeAsync(() => cut.Instance.Base.ClearExportFailure());
		Assert.Empty(cut.FindComponents<MokaAlert>());
	}

	[Theory]
	[InlineData(null, "Traffic today", null, "traffic-today")]
	[InlineData(null, null, "Disk use, by host", "disk-use-by-host")]
	[InlineData(null, null, null, "chart")]
	[InlineData("Report: 2026/11", "Traffic", null, "Report 202611")]
	[InlineData("...", null, null, "chart")]
	public void TheFileName_IsTheExportName_ElseTheTitleOrLabel(string? exportName, string? title, string? label, string expected)
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.ExportName, exportName)
			.Add(x => x.Title, title)
			.Add(x => x.Label, label));

		Assert.Equal(expected, cut.Instance.Base.ExportFileName);
	}

	private IRenderedComponent<FrameChart> Chart(Action<ComponentParameterCollectionBuilder<FrameChart>>? more = null) =>
		Render<FrameChart>(p =>
		{
			p.Add(x => x.Keys, ["LAN"]).Add(x => x.Data, [[1d, 2]]);
			more?.Invoke(p);
		});
}
