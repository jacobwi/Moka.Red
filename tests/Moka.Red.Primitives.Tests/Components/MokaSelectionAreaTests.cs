using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Primitives.SelectionArea;
using Moka.Red.Tests.Shared;
using TestContext = Xunit.TestContext;

namespace Moka.Red.Primitives.Tests.Components;

public sealed partial class MokaSelectionAreaTests : BunitContext
{
	private const string ModulePath = "./_content/Moka.Red.Core/moka-box-select.js";

	private readonly BunitJSModuleInterop _module;

	public MokaSelectionAreaTests()
	{
		_module = JSInterop.SetupModule(ModulePath);
		_module.SetupVoid("bindBoxSelect", _ => true).SetVoidResult();
		_module.SetupVoid("unbindBoxSelect", _ => true).SetVoidResult();
	}

	private static RenderFragment Tiles => builder =>
	{
		foreach (string key in new[] { "a", "b", "c" })
		{
			builder.OpenElement(0, "div");
			builder.AddAttribute(1, "data-moka-select-key", key);
			builder.AddContent(2, key);
			builder.CloseElement();
		}
	};

	private static Dictionary<string, object?> Options(JSRuntimeInvocation bind) =>
		Assert.IsType<Dictionary<string, object?>>(bind.Arguments[2]);

	[Fact]
	public void Off_RendersOnlyTheWrapper_AndTouchesNoScript()
	{
		IRenderedComponent<MokaSelectionArea> cut = Render<MokaSelectionArea>(p => p.Add(x => x.ChildContent, Tiles));

		cut.MarkupMatches("""<div class="moka-selection-area"><div data-moka-select-key="a">a</div><div data-moka-select-key="b">b</div><div data-moka-select-key="c">c</div></div>""");
		Assert.Empty(JSInterop.Invocations);
	}

	[Fact]
	public void Disabled_IsOffToo()
	{
		IRenderedComponent<MokaSelectionArea> cut = Render<MokaSelectionArea>(p => p
			.Add(x => x.BoxSelect, true)
			.Add(x => x.Disabled, true));

		Assert.Empty(cut.FindAll("[data-moka-box]"));
		Assert.Empty(JSInterop.Invocations);
	}

	[Fact]
	public void On_DrawsAHiddenBox_AndBindsTheWrapper()
	{
		IRenderedComponent<MokaSelectionArea> cut = Render<MokaSelectionArea>(p => p
			.Add(x => x.BoxSelect, true)
			.Add(x => x.ChildContent, Tiles));

		AngleSharp.Dom.IElement box = cut.Find(".moka-selection-area > .moka-box-select");
		Assert.True(box.HasAttribute("hidden"));
		Assert.Equal("true", box.GetAttribute("aria-hidden"));
		Assert.True(box.HasAttribute("data-moka-box"));

		JSRuntimeInvocation bind = Assert.Single(_module.Invocations["bindBoxSelect"]);
		Assert.IsType<ElementReference>(bind.Arguments[0]);
		Assert.IsType<DotNetObjectReference<MokaSelectionArea>>(bind.Arguments[1]);
		Dictionary<string, object?> options = Options(bind);
		Assert.Equal(":scope > [data-moka-box]", options["box"]);
		Assert.Equal("data-moka-select-key", options["key"]);
		Assert.Null(options["items"]);
		Assert.Null(options["scroller"]);
		Assert.Null(options["geometry"]);
		Assert.Equal("HandleBoxSelect", options["method"]);
	}

	[Fact]
	public void TheAppsAttributeSelectorAndScroller_ReachTheScript()
	{
		Render<MokaSelectionArea>(p => p
			.Add(x => x.BoxSelect, true)
			.Add(x => x.KeyAttribute, "data-node")
			.Add(x => x.ItemSelector, ".ark-tile")
			.Add(x => x.ScrollContainerSelector, ".ark-explorer__view"));

		Dictionary<string, object?> options = Options(Assert.Single(_module.Invocations["bindBoxSelect"]));
		Assert.Equal("data-node", options["key"]);
		Assert.Equal(".ark-tile", options["items"]);
		Assert.Equal(".ark-explorer__view", options["scroller"]);
	}

	[Fact]
	public void Geometry_ReachesTheScript()
	{
		Render<MokaSelectionArea>(p => p
			.Add(x => x.BoxSelect, true)
			.Add(x => x.Geometry, new MokaBoxSelectGeometry(5000) { Columns = 3, LineHeight = 36, IndexAttribute = "aria-rowindex", IndexBase = 2 }));

		Dictionary<string, object?> geometry = Assert.IsType<Dictionary<string, object?>>(Options(Assert.Single(_module.Invocations["bindBoxSelect"]))["geometry"]);
		Assert.Equal(5000, geometry["count"]);
		Assert.Equal(3, geometry["columns"]);
		Assert.Equal(36d, geometry["lineHeight"]);
		Assert.Equal("aria-rowindex", geometry["index"]);
		Assert.Equal(2, geometry["base"]);
	}

	[Fact]
	public void ARenderWithTheSameOptions_DoesNotBindAgain_AndNewOnesUpdateTheBinding()
	{
		IRenderedComponent<MokaSelectionArea> cut = Render<MokaSelectionArea>(p => p
			.Add(x => x.BoxSelect, true)
			.Add(x => x.Geometry, new MokaBoxSelectGeometry(10)));

		cut.Render(p => p.Add(x => x.Geometry, new MokaBoxSelectGeometry(10)));
		Assert.Single(_module.Invocations["bindBoxSelect"]);

		cut.Render(p => p.Add(x => x.Geometry, new MokaBoxSelectGeometry(11)));
		Assert.Equal(2, _module.Invocations["bindBoxSelect"].Count);
		Assert.Empty(_module.Invocations["unbindBoxSelect"]);
	}

	[Fact]
	public void TurningItOff_UnbindsAndRemovesTheBox()
	{
		IRenderedComponent<MokaSelectionArea> cut = Render<MokaSelectionArea>(p => p.Add(x => x.BoxSelect, true));

		cut.Render(p => p.Add(x => x.BoxSelect, false));

		Assert.Empty(cut.FindAll("[data-moka-box]"));
		Assert.Single(_module.Invocations["unbindBoxSelect"]);
	}

	[Fact]
	public async Task Disposing_Unbinds()
	{
		IRenderedComponent<MokaSelectionArea> cut = Render<MokaSelectionArea>(p => p.Add(x => x.BoxSelect, true));

		await cut.Instance.DisposeAsync();

		Assert.Single(_module.Invocations["unbindBoxSelect"]);
	}

	[Fact]
	public async Task DisposingWhileOff_CallsNothing()
	{
		IRenderedComponent<MokaSelectionArea> cut = Render<MokaSelectionArea>();

		await cut.Instance.DisposeAsync();

		Assert.Empty(JSInterop.Invocations);
	}

	[Theory]
	[InlineData(0, 0, MokaBoxSelectPhase.Started, MokaBoxSelectMode.Replace)]
	[InlineData(1, 1, MokaBoxSelectPhase.Changed, MokaBoxSelectMode.Add)]
	[InlineData(2, 2, MokaBoxSelectPhase.Finished, MokaBoxSelectMode.Toggle)]
	[InlineData(3, 0, MokaBoxSelectPhase.Cancelled, MokaBoxSelectMode.Replace)]
	public async Task AReport_ReachesOnBoxSelect(int phase, int mode, MokaBoxSelectPhase expectedPhase, MokaBoxSelectMode expectedMode)
	{
		List<MokaBoxSelectArgs> reports = [];
		IRenderedComponent<MokaSelectionArea> cut = Render<MokaSelectionArea>(p => p
			.Add(x => x.BoxSelect, true)
			.Add(x => x.OnBoxSelect, reports.Add));

		await cut.InvokeAsync(() => cut.Instance.HandleBoxSelect(phase, mode, ["a", "b"], null));

		MokaBoxSelectArgs report = Assert.Single(reports);
		Assert.Equal(expectedPhase, report.Phase);
		Assert.Equal(expectedMode, report.Mode);
		Assert.Equal(["a", "b"], report.Keys);
		Assert.Null(report.Range);
	}

	[Fact]
	public async Task AReportWithARange_CarriesIt()
	{
		MokaBoxSelectArgs? report = null;
		IRenderedComponent<MokaSelectionArea> cut = Render<MokaSelectionArea>(p => p
			.Add(x => x.BoxSelect, true)
			.Add(x => x.OnBoxSelect, args => report = args));

		await cut.InvokeAsync(() => cut.Instance.HandleBoxSelect(1, 0, null, [10, 4000, 0, 0, 1, 5000]));

		Assert.NotNull(report);
		Assert.Empty(report.Keys);
		Assert.Equal(new MokaBoxSelectRange(10, 4000, 0, 0, 1, 5000), report.Range);
		Assert.Equal(10, report.Range!.Value.FirstIndex);
		Assert.Equal(4000, report.Range.Value.LastIndex);
	}

	[Theory]
	[InlineData(4, 0)]
	[InlineData(-1, 0)]
	[InlineData(0, 3)]
	public async Task AReportOutsideTheEnums_IsIgnored(int phase, int mode)
	{
		List<MokaBoxSelectArgs> reports = [];
		IRenderedComponent<MokaSelectionArea> cut = Render<MokaSelectionArea>(p => p
			.Add(x => x.BoxSelect, true)
			.Add(x => x.OnBoxSelect, reports.Add));

		await cut.InvokeAsync(() => cut.Instance.HandleBoxSelect(phase, mode, ["a"], null));

		Assert.Empty(reports);
	}

	[Fact]
	public async Task AHandlerThatFails_ReachesTheRenderer()
	{
		IRenderedComponent<MokaSelectionArea> cut = Render<MokaSelectionArea>(p => p
			.Add(x => x.BoxSelect, true)
			.Add(x => x.OnBoxSelect, EventCallback.Factory.Create<MokaBoxSelectArgs>(this, _ => throw new InvalidOperationException("Handler failed"))));

		await cut.InvokeAsync(() => cut.Instance.HandleBoxSelect(2, 0, ["a"], null));

		Exception error = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		Assert.Equal("Handler failed", error.Message);
	}

	[Fact]
	public void SpacingAndAttributes_LandOnTheWrapper()
	{
		IRenderedComponent<MokaSelectionArea> cut = Render<MokaSelectionArea>(p => p
			.Add(x => x.Class, "mine")
			.Add(x => x.Id, "area")
			.Add(x => x.MarginValue, "7px")
			.AddUnmatched("tabindex", "0"));

		AngleSharp.Dom.IElement root = cut.Find("div");
		Assert.Equal("moka-selection-area mine", root.ClassName);
		Assert.Equal("area", root.Id);
		Assert.Equal("0", root.GetAttribute("tabindex"));
		Assert.Equal("margin: 7px", root.GetAttribute("style"));
	}

	[Theory]
	[InlineData(MokaBoxSelectMode.Replace, new[] { "b", "c", "d" })]
	[InlineData(MokaBoxSelectMode.Add, new[] { "a", "b", "c", "d" })]
	[InlineData(MokaBoxSelectMode.Toggle, new[] { "a", "c", "d" })]
	public void Combine_FollowsTheMode(MokaBoxSelectMode mode, string[] expected)
	{
		MokaBoxSelectArgs args = new(MokaBoxSelectPhase.Changed, mode, []);

		HashSet<string> result = args.Combine(["a", "b"], ["b", "c", "d", "c"]);

		Assert.Equal(expected, result.Order(StringComparer.Ordinal));
	}

	[Fact]
	public void Combine_WhenCancelled_GivesBackWhatWasKept()
	{
		MokaBoxSelectArgs args = new(MokaBoxSelectPhase.Cancelled, MokaBoxSelectMode.Toggle, []);

		Assert.Equal(["a", "b"], args.Combine(["a", "b"], ["b", "c"]).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void Combine_UsesTheComparer()
	{
		MokaBoxSelectArgs args = new(MokaBoxSelectPhase.Finished, MokaBoxSelectMode.Toggle, []);

		Assert.Equal(["b"], args.Combine(["A", "b"], ["a"], StringComparer.OrdinalIgnoreCase));
	}

	[Fact]
	public void ARangeOverAGrid_SkipsThePlacesPastTheLastItem()
	{
		MokaBoxSelectRange range = new(1, 2, 2, 3, Columns: 4, Count: 10);

		Assert.Equal([6, 7], range.Indexes);
		Assert.Equal(6, range.FirstIndex);
		Assert.Equal(7, range.LastIndex);
		Assert.True(range.Contains(7));
		Assert.False(range.Contains(5));
		Assert.False(range.Contains(10));
		Assert.False(default(MokaBoxSelectRange).Contains(0));
	}

	[Fact]
	public void ARangeOverAList_IsEveryIndexBetween()
	{
		MokaBoxSelectRange range = new(3, 6, 0, 0, Columns: 1, Count: 100);

		Assert.Equal([3, 4, 5, 6], range.Indexes);
		Assert.Equal(3, range.FirstIndex);
		Assert.Equal(6, range.LastIndex);
	}

	[Fact]
	public void TheScriptsPhasesAndModes_MatchTheEnums()
	{
		string script = File.ReadAllText(ScriptPath());

		AssertTable(Table("PHASE").Match(script), Enum.GetValues<MokaBoxSelectPhase>().ToDictionary(value => value.ToString(), value => (int)value));
		AssertTable(Table("MODE").Match(script), Enum.GetValues<MokaBoxSelectMode>().ToDictionary(value => value.ToString(), value => (int)value));
	}

	[Fact]
	public void TheBox_DrawsFromTokens()
	{
		IReadOnlyDictionary<string, string> css = ScopedCss.Declarations("moka.css", ".moka-box-select");

		Assert.Equal("var(--moka-border-width) solid var(--moka-color-primary-border)", css["border"]);
		Assert.Equal("var(--moka-color-primary-glow)", css["background"]);
		Assert.Equal("none", css["pointer-events"]);
		Assert.Equal("absolute", css["position"]);
		Assert.False(css.ContainsKey("transition"));
		Assert.False(css.ContainsKey("animation"));
	}

	private static void AssertTable(Match table, Dictionary<string, int> expected)
	{
		Assert.True(table.Success);
		Dictionary<string, int> script = Entry().Matches(table.Groups[1].Value)
			.ToDictionary(entry => entry.Groups[1].Value, entry => int.Parse(entry.Groups[2].Value, CultureInfo.InvariantCulture));
		Assert.Equal(expected, script);
	}

	private static Regex Table(string name) => new($@"const {name} = \{{([^}}]*)\}};", RegexOptions.None, TimeSpan.FromSeconds(1));

	private static string ScriptPath()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "Moka.Red.slnx")))
			{
				return Path.Combine(folder.FullName, "src", "Moka.Red.Core", "wwwroot", "moka-box-select.js");
			}
		}

		throw new FileNotFoundException("Moka.Red.slnx");
	}

	[GeneratedRegex(@"(\w+):\s*(\d+)")]
	private static partial Regex Entry();
}
