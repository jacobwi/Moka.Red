using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     The legend, in a chart and on its own: names and keys, toggles that hide and isolate with the board's sentences,
///     Show all, the set of hidden series both ways, and a long legend folding behind "+N".
/// </summary>
public sealed class MokaChartLegendTests : BunitContext
{
	private static readonly IReadOnlyList<MokaChartLegendItem> Three =
	[
		new("lan", "LAN") { Value = "412 Mbps" },
		new("wifi", "Wi-Fi") { Mark = MokaChartLegendMark.Line },
		new("guest", "Guest") { Mark = MokaChartLegendMark.Box }
	];

	private static readonly string[] Sides = [":scope > .moka-chart-legend__items"];

	public MokaChartLegendTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void WithoutActions_TheEntriesAreNames_NotControls()
	{
		IRenderedComponent<MokaChartLegend> cut = Render<MokaChartLegend>(p => p.Add(x => x.Items, Three));

		Assert.Empty(cut.FindAll("button"));
		Assert.Equal(["LAN", "Wi-Fi", "Guest"], cut.FindAll(".moka-chart-legend__name").Select(name => name.TextContent));
		Assert.Equal("412 Mbps", cut.Find(".moka-chart-legend__value").TextContent);
		IElement group = cut.Find(".moka-chart-legend");
		Assert.Equal("group", group.GetAttribute("role"));
		Assert.Equal("Series", group.GetAttribute("aria-label"));
	}

	[Fact]
	public void ColorsFollowTheSlot_ThenTheTone_ThenTheValue()
	{
		IReadOnlyList<MokaChartLegendItem> items =
		[
			new("a", "A"),
			new("b", "B") { Slot = 5 },
			new("c", "C") { Color = MokaColor.Success },
			new("d", "D") { ColorValue = "#112233" },
			new("e", "E") { ColorValue = "red; top: 0" }
		];

		IRenderedComponent<MokaChartLegend> cut = Render<MokaChartLegend>(p => p.Add(x => x.Items, items));

		IReadOnlyList<IElement> entries = cut.FindAll(".moka-chart-legend__item");
		Assert.Contains("moka-chart-c1", entries[0].ClassName, StringComparison.Ordinal);
		Assert.Contains("moka-chart-c5", entries[1].ClassName, StringComparison.Ordinal);
		Assert.Contains("moka-chart-tone-success", entries[2].ClassName, StringComparison.Ordinal);
		Assert.Equal("--moka-chart-color: #112233", entries[3].GetAttribute("style"));
		Assert.Null(entries[4].GetAttribute("style"));
	}

	[Theory]
	[InlineData(MokaChartLegendKey.Dot, "dot", "dot", "dot")]
	[InlineData(MokaChartLegendKey.Mark, "dot", "line", "box")]
	public void TheKey_IsADot_OrTheSeriesOwnMark(MokaChartLegendKey key, string first, string second, string third)
	{
		IRenderedComponent<MokaChartLegend> cut = Render<MokaChartLegend>(p => p.Add(x => x.Items, Three).Add(x => x.Key, key));

		Assert.Equal([first, second, third], cut.FindAll(".moka-chart-legend__key")
			.Select(k => k.ClassList.First(c => c.StartsWith("moka-chart-legend__key--", StringComparison.Ordinal))["moka-chart-legend__key--".Length..]));
	}

	[Fact]
	public void PillKeys_DrawEachEntryAsAPill()
	{
		IRenderedComponent<MokaChartLegend> cut = Render<MokaChartLegend>(p => p.Add(x => x.Items, Three).Add(x => x.Key, MokaChartLegendKey.Pill));

		Assert.Contains("moka-chart-legend--pills", cut.Find(".moka-chart-legend").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void WithHide_EachEntryIsAToggle_PressedWhileShown()
	{
		IRenderedComponent<MokaChartLegend> cut = Toggles(MokaChartLegendActions.Hide);

		Assert.Equal(["true", "true", "true"], cut.FindAll("button.moka-chart-legend__item").Select(b => b.GetAttribute("aria-pressed")));
		Assert.All(cut.FindAll("button.moka-chart-legend__item"), b => Assert.Equal("button", b.GetAttribute("type")));
	}

	[Fact]
	public void AClick_HidesTheSeries_SaysSo_AndTellsTheParent()
	{
		IReadOnlyCollection<string>? told = null;
		IRenderedComponent<MokaChartLegend> cut = Toggles(MokaChartLegendActions.Hide, p => p.Add(x => x.HiddenChanged, hidden => told = hidden));

		Entry(cut, "lan").Click(new MouseEventArgs { Detail = 1 });

		Assert.Equal(["lan"], told);
		Assert.Equal("false", Entry(cut, "lan").GetAttribute("aria-pressed"));
		Assert.Equal("LAN hidden. 2 of 3 series shown.", Said(cut));

		Entry(cut, "lan").Click(new MouseEventArgs { Detail = 1 });
		Assert.Empty(told!);
		Assert.Equal("LAN shown. 3 of 3 series shown.", Said(cut));
	}

	[Fact]
	public void ADoubleClick_ShowsTheSeriesAlone_AndAgainBringsTheRestBack()
	{
		IReadOnlyCollection<string>? told = null;
		IRenderedComponent<MokaChartLegend> cut = Toggles(MokaChartLegendActions.Isolate, p => p.Add(x => x.HiddenChanged, hidden => told = hidden));

		Entry(cut, "wifi").Click(new MouseEventArgs { Detail = 1 });
		Entry(cut, "wifi").Click(new MouseEventArgs { Detail = 2 });

		Assert.Equal(["guest", "lan"], told!.Order(StringComparer.Ordinal));
		Assert.Equal("Showing Wi-Fi only. Do it again, or press Show all, to bring the rest back.", Said(cut));

		Entry(cut, "wifi").Click(new MouseEventArgs { Detail = 2 });
		Assert.Empty(told!);
		Assert.Equal("All 3 series shown.", Said(cut));
	}

	[Fact]
	public void AnAltClick_Isolates()
	{
		IRenderedComponent<MokaChartLegend> cut = Toggles(MokaChartLegendActions.Isolate);

		Entry(cut, "guest").Click(new MouseEventArgs { Detail = 1, AltKey = true });

		Assert.Equal(["false", "false", "true"], cut.FindAll("button.moka-chart-legend__item").Select(b => b.GetAttribute("aria-pressed")));
	}

	[Fact]
	public void AltEnter_Isolates_AndTheClickItMakes_IsIgnored()
	{
		IRenderedComponent<MokaChartLegend> cut = Toggles(MokaChartLegendActions.Isolate);

		Entry(cut, "lan").KeyDown(new KeyboardEventArgs { Key = "Enter", AltKey = true });
		Entry(cut, "lan").Click(new MouseEventArgs { Detail = 0, AltKey = true });

		Assert.Equal(["true", "false", "false"], cut.FindAll("button.moka-chart-legend__item").Select(b => b.GetAttribute("aria-pressed")));
		Assert.Equal("Showing LAN only. Do it again, or press Show all, to bring the rest back.", Said(cut));
	}

	[Fact]
	public void PlainEnter_StillToggles()
	{
		IRenderedComponent<MokaChartLegend> cut = Toggles(MokaChartLegendActions.Isolate);

		Entry(cut, "lan").KeyDown(new KeyboardEventArgs { Key = "Enter" });
		Entry(cut, "lan").Click(new MouseEventArgs { Detail = 0 });

		Assert.Equal("false", Entry(cut, "lan").GetAttribute("aria-pressed"));
	}

	[Fact]
	public void WithoutIsolate_TheEntriesListenForNoKeys()
	{
		IRenderedComponent<MokaChartLegend> cut = Toggles(MokaChartLegendActions.Hide);

		Assert.False(Entry(cut, "lan").HasAttribute("blazor:onkeydown"));
		Assert.True(Entry(cut, "lan").HasAttribute("blazor:onclick"));
	}

	[Fact]
	public void ShowAll_AppearsWhileAnythingIsHidden_AndBringsEverythingBack()
	{
		IRenderedComponent<MokaChartLegend> cut = Toggles(MokaChartLegendActions.Hide);
		Assert.Empty(cut.FindAll(".moka-chart-legend__show-all"));

		Entry(cut, "lan").Click(new MouseEventArgs { Detail = 1 });
		Entry(cut, "guest").Click(new MouseEventArgs { Detail = 1 });
		cut.Find(".moka-chart-legend__show-all").Click();

		Assert.All(cut.FindAll("button.moka-chart-legend__item"), b => Assert.Equal("true", b.GetAttribute("aria-pressed")));
		Assert.Empty(cut.FindAll(".moka-chart-legend__show-all"));
		Assert.Equal("All 3 series shown.", Said(cut));
	}

	[Fact]
	public void TheHiddenSet_ComesFromTheParent_OnlyWhenItHandsOverANewOne()
	{
		IReadOnlyCollection<string> hidden = ["wifi"];
		IRenderedComponent<MokaChartLegend> cut = Toggles(MokaChartLegendActions.Hide, p => p.Add(x => x.Hidden, hidden));
		Assert.Equal("false", Entry(cut, "wifi").GetAttribute("aria-pressed"));

		Entry(cut, "lan").Click(new MouseEventArgs { Detail = 1 });
		cut.Render(p => p.Add(x => x.Items, Three).Add(x => x.Actions, MokaChartLegendActions.Hide).Add(x => x.Hidden, hidden));
		Assert.Equal("false", Entry(cut, "lan").GetAttribute("aria-pressed"));

		cut.Render(p => p.Add(x => x.Items, Three).Add(x => x.Actions, MokaChartLegendActions.Hide).Add(x => x.Hidden, ["guest"]));
		Assert.Equal(["true", "true", "false"], cut.FindAll("button.moka-chart-legend__item").Select(b => b.GetAttribute("aria-pressed")));
	}

	[Fact]
	public void AHiddenSeries_InANameOnlyLegend_IsMarked()
	{
		IRenderedComponent<MokaChartLegend> cut = Render<MokaChartLegend>(p => p.Add(x => x.Items, Three).Add(x => x.Hidden, ["lan"]));

		Assert.True(cut.Find("[data-moka-series='lan']").HasAttribute("data-moka-hidden"));
	}

	[Fact]
	public void TheChartsScript_FindsTheEntriesAndTheirGroup()
	{
		IRenderedComponent<MokaChartLegend> cut = Render<MokaChartLegend>(p => p
			.Add(x => x.Items, Three)
			.Add(x => x.Actions, MokaChartLegendActions.Highlight)
			.Add(x => x.SyncGroup, "net"));

		IElement root = cut.Find(".moka-chart-legend");
		Assert.True(root.HasAttribute("data-moka-legend"));
		Assert.True(root.HasAttribute("data-moka-legend-highlight"));
		Assert.Equal("net", root.GetAttribute("data-moka-sync"));
		Assert.Equal(["1", "2", "3"], cut.FindAll("[data-moka-series]").Select(e => e.GetAttribute("data-moka-slot")));
	}

	[Fact]
	public void ATitle_NamesTheGroup()
	{
		IRenderedComponent<MokaChartLegend> cut = Render<MokaChartLegend>(p => p.Add(x => x.Items, Three).Add(x => x.Title, "Services"));

		Assert.Equal("Services", cut.Find(".moka-chart-legend__title").TextContent);
		Assert.Equal("Services", cut.Find(".moka-chart-legend").GetAttribute("aria-label"));
	}

	[Fact]
	public void RepeatedKeys_AreDrawnOnce()
	{
		IReadOnlyList<MokaChartLegendItem> items = [new("a", "A"), new("a", "Again"), new("b", "B")];

		IRenderedComponent<MokaChartLegend> cut = Render<MokaChartLegend>(p => p.Add(x => x.Items, items));

		Assert.Equal(["A", "B"], cut.FindAll(".moka-chart-legend__name").Select(name => name.TextContent));
	}

	[Fact]
	public void TheSameParameters_DrawNothingAgain()
	{
		IRenderedComponent<MokaChartLegend> cut = Render<MokaChartLegend>(p => p.Add(x => x.Items, Three));
		int renders = cut.RenderCount;

		cut.Render(p => p.Add(x => x.Items, [.. Three]));

		Assert.Equal(renders, cut.RenderCount);
	}

	[Fact]
	public async Task ALongLegend_Folds_InAnInteractiveRender()
	{
		SetRendererInfo(new RendererInfo("Server", true));
		BunitJSModuleInterop fit = JSInterop.SetupModule("./_content/Moka.Red.Core/moka-fit.js");
		fit.Mode = JSRuntimeMode.Loose;
		IReadOnlyList<MokaChartLegendItem> nine = [.. Enumerable.Range(1, 9).Select(i => new MokaChartLegendItem($"k{i}", $"Series {i}"))];

		IRenderedComponent<MokaChartLegend> cut = Render<MokaChartLegend>(p => p.Add(x => x.Items, nine));

		JSRuntimeInvocation bind = Assert.Single(fit.Invocations, invocation => invocation.Identifier == "bindFit");
		Dictionary<string, object?> layout = Assert.IsType<Dictionary<string, object?>>(bind.Arguments[2]);
		Assert.Equal(Sides, layout["sides"]);

		await cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(4));
		IElement more = cut.Find(".moka-chart-legend__more-button");
		Assert.Equal("+4", more.TextContent.Trim());
		Assert.Equal("Show 4 more series", more.GetAttribute("aria-label"));
		Assert.Equal("false", more.GetAttribute("aria-expanded"));

		await more.ClickAsync(new MouseEventArgs());
		Assert.Equal("true", cut.Find(".moka-chart-legend").GetAttribute("data-moka-expanded"));
		Assert.Equal("Show fewer series", cut.Find(".moka-chart-legend__more-button").GetAttribute("aria-label"));
	}

	[Fact]
	public void AShortLegend_OrFoldAboveZero_OrAStaticRender_NeverFolds()
	{
		BunitJSModuleInterop fit = JSInterop.SetupModule("./_content/Moka.Red.Core/moka-fit.js");
		fit.Mode = JSRuntimeMode.Loose;
		IReadOnlyList<MokaChartLegendItem> nine = [.. Enumerable.Range(1, 9).Select(i => new MokaChartLegendItem($"k{i}", $"Series {i}"))];

		Render<MokaChartLegend>(p => p.Add(x => x.Items, Three));
		SetRendererInfo(new RendererInfo("Server", true));
		Render<MokaChartLegend>(p => p.Add(x => x.Items, nine).Add(x => x.FoldAbove, 0));
		Render<MokaChartLegend>(p => p.Add(x => x.Items, nine).Add(x => x.Orientation, MokaDirection.Column));
		SetRendererInfo(new RendererInfo("Static", false));
		Render<MokaChartLegend>(p => p.Add(x => x.Items, nine));

		Assert.Empty(fit.Invocations);
	}

	[Fact]
	public void TheWords_ComeFromText()
	{
		MokaChartText words = MokaChartText.Default with { SeriesHidden = (name, shown, total) => $"{name} weg, {shown}/{total}" };
		IRenderedComponent<MokaChartLegend> cut = Toggles(MokaChartLegendActions.Hide, p => p.Add(x => x.Text, words));

		Entry(cut, "lan").Click(new MouseEventArgs { Detail = 1 });

		Assert.Equal("LAN weg, 2/3", Said(cut));
	}

	private IRenderedComponent<MokaChartLegend> Toggles(MokaChartLegendActions actions,
		Action<ComponentParameterCollectionBuilder<MokaChartLegend>>? more = null) =>
		Render<MokaChartLegend>(p =>
		{
			p.Add(x => x.Items, Three).Add(x => x.Actions, actions);
			more?.Invoke(p);
		});

	private static IElement Entry(IRenderedComponent<MokaChartLegend> cut, string key) => cut.Find($"[data-moka-series='{key}']");

	private static string Said(IRenderedComponent<MokaChartLegend> cut) => cut.Find(".moka-chart-legend > [role='status']").TextContent;
}
