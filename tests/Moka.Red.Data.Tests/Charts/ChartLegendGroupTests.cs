using System.Text.Json;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     A legend on its own with a sync group highlights the series it points at across the group's charts: it binds the
///     chart script's <c>bindLegend</c> only when it stands alone and highlights, and lets it go again. The script's side
///     runs in Node.js over a stand-in for the DOM (<c>Script/legend-group.mjs</c>).
/// </summary>
public sealed class ChartLegendGroupTests : BunitContext
{
	private static readonly Lazy<JsonElement?> Script = new(() => ScriptRunner.Run("legend-group.mjs"));

	private static readonly IReadOnlyList<MokaChartLegendItem> Items = [new("lan", "LAN"), new("wifi", "Wi-Fi")];

	[Fact]
	public void OnItsOwn_WithAGroupAndHighlight_ItBindsToTheGroup()
	{
		BunitJSModuleInterop module = this.Interactive();
		Render<MokaChartLegend>(p => p
			.Add(x => x.Items, Items)
			.Add(x => x.Actions, MokaChartLegendActions.Highlight)
			.Add(x => x.SyncGroup, "net"));

		JSRuntimeInvocation bind = Assert.Single(module.Invocations, call => call.Identifier == "bindLegend");
		Assert.Equal("net", bind.Arguments[1]);
	}

	[Fact]
	public void WithoutHighlight_OrWithoutAGroup_NothingBinds()
	{
		BunitJSModuleInterop module = this.Interactive();
		Render<MokaChartLegend>(p => p.Add(x => x.Items, Items).Add(x => x.SyncGroup, "net"));
		Render<MokaChartLegend>(p => p.Add(x => x.Items, Items).Add(x => x.Actions, MokaChartLegendActions.Highlight));

		Assert.DoesNotContain(module.Invocations, call => call.Identifier == "bindLegend");
	}

	[Fact]
	public async Task ChangingTheGroup_AndDisposing_LetTheOldGroupGo()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<MokaChartLegend> cut = Render<MokaChartLegend>(p => p
			.Add(x => x.Items, Items)
			.Add(x => x.Actions, MokaChartLegendActions.Highlight)
			.Add(x => x.SyncGroup, "net"));

		cut.Render(p => p.Add(x => x.SyncGroup, "other"));
		Assert.Equal(["net", "other"], module.Invocations["bindLegend"].Select(call => call.Arguments[1] as string));
		Assert.Single(module.Invocations["unbindLegend"]);

		await DisposeComponentsAsync();
		Assert.Equal(2, module.Invocations.Count(call => call.Identifier == "unbindLegend"));
	}

	[Fact]
	public void AChartsOwnLegend_NeverBinds_SinceTheChartHighlightsItsGroup()
	{
		BunitJSModuleInterop module = this.Interactive();
		Render<MokaRadarChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.SyncGroup, "net")
			.Add(x => x.Axes, [new MokaRadarAxis("A"), new MokaRadarAxis("B"), new MokaRadarAxis("C")])
			.Add(x => x.Series, [new MokaRadarSeries("LAN", [1, 2, 3]), new MokaRadarSeries("Wi-Fi", [3, 2, 1])]));

		Assert.DoesNotContain(module.Invocations, call => call.Identifier == "bindLegend");
	}

	[Fact]
	public void PointingAtAnEntry_HighlightsItInEveryChartOfTheGroupThatHighlights_AndLeavingClearsIt()
	{
		JsonElement run = Results();

		Assert.Equal("lan", run.GetProperty("over").GetProperty("a").GetString());
		Assert.Equal("lan", run.GetProperty("over").GetProperty("b").GetString());
		Assert.Equal(JsonValueKind.Null, run.GetProperty("over").GetProperty("quiet").ValueKind);
		Assert.Equal(JsonValueKind.Null, run.GetProperty("over").GetProperty("elsewhere").ValueKind);
		Assert.True(run.GetProperty("over").GetProperty("dimmed").GetBoolean());
		Assert.Equal(JsonValueKind.Null, run.GetProperty("out").GetProperty("a").ValueKind);
		Assert.False(run.GetProperty("out").GetProperty("dimmed").GetBoolean());
	}

	[Fact]
	public void FocusingAnEntry_HighlightsItToo_AndUnbindingClearsIt()
	{
		JsonElement run = Results();

		Assert.Equal("wifi", run.GetProperty("focus").GetProperty("a").GetString());
		Assert.Equal(JsonValueKind.Null, run.GetProperty("unbound").GetProperty("a").ValueKind);
		Assert.True(run.GetProperty("unbound").GetProperty("silent").GetBoolean());
	}

	private static JsonElement Results()
	{
		Assert.SkipUnless(Script.Value is not null, "Node.js isn't installed, so the chart script can't be run.");
		return Script.Value!.Value;
	}
}
