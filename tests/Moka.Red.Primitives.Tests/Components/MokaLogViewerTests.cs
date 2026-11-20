using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Moka.Red.Primitives.LogViewer;
using Moka.Red.Primitives.Tooltip;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public sealed class MokaLogViewerTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Primitives/LogViewer/MokaLogViewer.razor.js";
	private const int RemoteCount = 5000;

	private static readonly DateTime At = new(2026, 11, 19, 9, 30, 0, DateTimeKind.Utc);

	private static readonly MokaLogEntry Failure = new(At, MokaLogLevel.Error, "Payment failed", "Payments")
	{
		Exception = "System.TimeoutException: The gateway did not answer.\n   at Payments.Charge()",
		Properties = [new("OrderId", "4711"), new("Attempt", "3")]
	};

	private static readonly IReadOnlyList<MokaLogEntry> Entries =
	[
		new(At, MokaLogLevel.Info, "Application started.", "Host"),
		new(At.AddSeconds(1), MokaLogLevel.Warning, "Cache miss for key 'user:42'", "Cache"),
		Failure,
		new(At.AddSeconds(3), MokaLogLevel.Fatal, "Out of memory", "Runtime")
	];

	private readonly List<(string Search, IReadOnlySet<MokaLogLevel>? Levels)> _asked = [];
	private readonly BunitJSModuleInterop _module;
	private IReadOnlySet<MokaLogLevel>? _levels;
	private string _search = string.Empty;

	public MokaLogViewerTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
	}

	private static IReadOnlyList<MokaLogEntry> Many(int count) =>
	[
		.. Enumerable.Range(0, count).Select(i => new MokaLogEntry(At.AddMilliseconds(i),
			i % 10 == 0 ? MokaLogLevel.Error : MokaLogLevel.Info, $"Line {i}", "Worker"))
	];

	private ValueTask<ItemsProviderResult<MokaLogEntry>> Provide(ItemsProviderRequest request)
	{
		_asked.Add((_search, _levels));
		MokaLogEntry[] rows =
		[
			.. Enumerable.Range(request.StartIndex, Math.Min(request.Count, RemoteCount - request.StartIndex))
				.Select(i => new MokaLogEntry(At.AddSeconds(i), MokaLogLevel.Info, $"Remote line {i}"))
		];
		return ValueTask.FromResult(new ItemsProviderResult<MokaLogEntry>(rows, RemoteCount));
	}

	private IRenderedComponent<MokaLogViewer> RenderRemote() => Render<MokaLogViewer>(p => p
		.Add(x => x.ItemsProvider, Provide)
		.Add(x => x.SearchText, _search)
		.Add(x => x.SearchTextChanged, (string text) => _search = text)
		.Add(x => x.LevelsChanged, (IReadOnlySet<MokaLogLevel> levels) => _levels = levels));

	private static ICollection<MokaLogEntry> VirtualItems(IRenderedComponent<MokaLogViewer> cut) =>
		cut.FindComponent<Virtualize<MokaLogEntry>>().Instance.Items!;

	[Fact]
	public void Virtualize_DrawsTheRowsInViewThroughVirtualize()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Many(10_000))
			.Add(x => x.Virtualize, true));

		Assert.Contains("moka-log-viewer--virtual", cut.Find(".moka-log-viewer").ClassList);
		Virtualize<MokaLogEntry> virtualize = cut.FindComponent<Virtualize<MokaLogEntry>>().Instance;
		Assert.Equal(24f, virtualize.ItemSize);
		Assert.Equal(10_000, virtualize.Items!.Count);
		Assert.InRange(cut.FindAll(".moka-log-viewer__row").Count, 1, 200);
	}

	[Fact]
	public void Virtualize_TheListIsATabStopTheKeyboardCanScroll()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.Virtualize, true));

		IElement body = cut.Find(".moka-log-viewer__body");
		Assert.Equal("0", body.GetAttribute("tabindex"));
		Assert.Equal("region", body.GetAttribute("role"));
		Assert.Equal("Log entries", body.GetAttribute("aria-label"));
	}

	[Fact]
	public void Virtualize_EachRowIsOneLineWithItsWholeMessageInATitle()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.Virtualize, true));

		Assert.Equal("Application started.", cut.Find(".moka-log-viewer__message").GetAttribute("title"));
		IReadOnlyDictionary<string, string> row =
			ScopedCss.Declarations("MokaLogViewer.razor.css", ".moka-log-viewer--virtual .moka-log-viewer__row");
		Assert.Equal("var(--_log-row-height)", row["height"]);
		Assert.Equal("border-box", row["box-sizing"]);
		Assert.Equal("nowrap", row["white-space"]);
	}

	// The row height Virtualize places rows by and the one the row is drawn at are one number, written in the invariant
	// culture: a comma-decimal culture turned it into "22,5px", which CSS drops.
	[Fact]
	public void Virtualize_DrawsRowsAtTheHeightVirtualizePlacesThemBy_InAnyCulture()
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("de-DE");
		try
		{
			IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
				.Add(x => x.Entries, Entries)
				.Add(x => x.Virtualize, true)
				.Add(x => x.VirtualRowHeight, 22.5f));

			Assert.Equal("max-height: 400px; --_log-row-height: 22.5px",
				cut.Find(".moka-log-viewer__body").GetAttribute("style"));
			Assert.Equal(22.5f, cut.FindComponent<Virtualize<MokaLogEntry>>().Instance.ItemSize);
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
		}
	}

	[Fact]
	public void Virtualize_ShowsEveryEntry_UnlessMaxEntriesIsSet()
	{
		IRenderedComponent<MokaLogViewer> all = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Many(1200))
			.Add(x => x.Virtualize, true));
		IRenderedComponent<MokaLogViewer> capped = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Many(1200))
			.Add(x => x.Virtualize, true)
			.Add(x => x.MaxEntries, 100));
		IRenderedComponent<MokaLogViewer> plain = Render<MokaLogViewer>(p => p.Add(x => x.Entries, Many(1200)));

		Assert.Equal(1200, VirtualItems(all).Count);
		Assert.Equal(100, VirtualItems(capped).Count);
		Assert.Equal("Line 1100", VirtualItems(capped).First().Message);
		Assert.Equal(500, plain.FindAll(".moka-log-viewer__row").Count);
	}

	[Fact]
	public void Virtualize_SearchesAndFiltersTheEntries()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Many(1000))
			.Add(x => x.Virtualize, true));

		cut.Find(".moka-log-viewer__filter-btn--info").Click();
		Assert.Equal(100, VirtualItems(cut).Count);

		cut.Find(".moka-log-viewer__search-input").Input("Line 99");
		Assert.Equal(["Line 990"], VirtualItems(cut).Select(entry => entry.Message));
	}

	[Fact]
	public void ItemsProvider_DrawsWhatItReturns_ThroughVirtualizeWhateverVirtualizeSays()
	{
		IRenderedComponent<MokaLogViewer> cut = RenderRemote();

		Assert.NotEmpty(_asked);
		Assert.Contains("moka-log-viewer--virtual", cut.Find(".moka-log-viewer").ClassList);
		Assert.Equal("Remote line 0", cut.Find(".moka-log-viewer__message").TextContent);
	}

	[Fact]
	public void ItemsProviderAndEntries_Throw()
	{
		InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.ItemsProvider, Provide)));

		Assert.Equal("MokaLogViewer takes Entries or ItemsProvider, not both.", error.Message);
	}

	[Fact]
	public void ItemsProvider_IsAskedAgainWithTheSearchTyped()
	{
		IRenderedComponent<MokaLogViewer> cut = RenderRemote();
		int before = _asked.Count;

		cut.Find(".moka-log-viewer__search-input").Input("timeout");

		Assert.True(_asked.Count > before);
		Assert.Equal("timeout", _asked[^1].Search);
	}

	[Fact]
	public void ItemsProvider_IsAskedAgainWithTheLevelsPicked()
	{
		IRenderedComponent<MokaLogViewer> cut = RenderRemote();
		int before = _asked.Count;

		cut.Find(".moka-log-viewer__filter-btn--trace").Click();

		Assert.True(_asked.Count > before);
		IReadOnlySet<MokaLogLevel>? levels = _asked[^1].Levels;
		Assert.NotNull(levels);
		Assert.DoesNotContain(MokaLogLevel.Trace, levels);
		Assert.Equal(5, levels.Count);
	}

	[Fact]
	public void ItemsProvider_IsAskedAgainWhenTheParentChangesTheSearch_ButNotForTheEchoOfATypedOne()
	{
		IRenderedComponent<MokaLogViewer> cut = RenderRemote();
		cut.Find(".moka-log-viewer__search-input").Input("timeout");
		int typed = _asked.Count;

		cut.Render(p => p.Add(x => x.SearchText, "timeout"));
		Assert.Equal(typed, _asked.Count);

		_search = "refused";
		cut.Render(p => p.Add(x => x.SearchText, "refused"));
		Assert.Equal(typed + 1, _asked.Count);
		Assert.Equal("refused", cut.Find(".moka-log-viewer__search-input").GetAttribute("value"));
	}

	[Fact]
	public async Task RefreshDataAsync_AsksTheProviderAgain()
	{
		IRenderedComponent<MokaLogViewer> cut = RenderRemote();
		int before = _asked.Count;

		await cut.InvokeAsync(cut.Instance.RefreshDataAsync);

		Assert.Equal(before + 1, _asked.Count);
	}

	[Fact]
	public void SearchText_FromTheParent_FiltersTheEntries()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.SearchText, "cache"));

		Assert.Equal("cache", cut.Find(".moka-log-viewer__search-input").GetAttribute("value"));
		Assert.Equal(["Cache miss for key 'user:42'"], cut.FindAll(".moka-log-viewer__message").Select(m => m.TextContent));
	}

	[Fact]
	public void TypingASearch_RaisesSearchTextChanged()
	{
		string? reported = null;
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.SearchTextChanged, (string text) => reported = text));

		cut.Find(".moka-log-viewer__search-input").Input("memory");

		Assert.Equal("memory", reported);
		Assert.Equal(["Out of memory"], cut.FindAll(".moka-log-viewer__message").Select(m => m.TextContent));
	}

	[Fact]
	public void Levels_FromTheParent_FilterTheEntriesAndLightTheirButtons()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.Levels, new HashSet<MokaLogLevel> { MokaLogLevel.Error, MokaLogLevel.Fatal }));

		Assert.Equal(["Payment failed", "Out of memory"], cut.FindAll(".moka-log-viewer__message").Select(m => m.TextContent));
		Assert.Equal(["moka-log-viewer__filter-btn--error", "moka-log-viewer__filter-btn--fatal"],
			cut.FindAll(".moka-log-viewer__filter-btn--active")
				.Select(button => button.ClassList.Single(c => c.StartsWith("moka-log-viewer__filter-btn--", StringComparison.Ordinal)
				                                               && c != "moka-log-viewer__filter-btn--active")));
	}

	[Fact]
	public void TheLevelButtons_SayWhetherTheyArePressed()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p.Add(x => x.Entries, Entries));

		Assert.All(cut.FindAll(".moka-log-viewer__filter-btn"), button => Assert.Equal("true", button.GetAttribute("aria-pressed")));

		cut.Find(".moka-log-viewer__filter-btn--debug").Click();

		Assert.Equal("false", cut.Find(".moka-log-viewer__filter-btn--debug").GetAttribute("aria-pressed"));
		Assert.Equal("true", cut.Find(".moka-log-viewer__filter-btn--info").GetAttribute("aria-pressed"));
	}

	[Fact]
	public void TheSearchBox_IsNamed()
	{
		IRenderedComponent<MokaLogViewer> named = Render<MokaLogViewer>();
		IRenderedComponent<MokaLogViewer> renamed = Render<MokaLogViewer>(p => p
			.Add(x => x.Text, new MokaLogViewerText { SearchLabel = "Protokoll durchsuchen" }));

		Assert.Equal("Search logs", named.Find(".moka-log-viewer__search-input").GetAttribute("aria-label"));
		Assert.Equal("Protokoll durchsuchen", renamed.Find(".moka-log-viewer__search-input").GetAttribute("aria-label"));
	}

	[Fact]
	public void PickingALevel_RaisesLevelsChangedWithANewSet()
	{
		IReadOnlySet<MokaLogLevel>? reported = null;
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.LevelsChanged, (IReadOnlySet<MokaLogLevel> levels) => reported = levels));

		cut.Find(".moka-log-viewer__filter-btn--warning").Click();

		Assert.NotNull(reported);
		Assert.Equal([MokaLogLevel.Trace, MokaLogLevel.Debug, MokaLogLevel.Info, MokaLogLevel.Error, MokaLogLevel.Fatal],
			reported.Order());
	}

	// A parent that builds its set again on every render, without listening, mustn't undo what the reader picked.
	[Fact]
	public void ALevelSetRebuiltEveryRender_KeepsTheReadersPick()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.Levels, new HashSet<MokaLogLevel>(Enum.GetValues<MokaLogLevel>())));

		cut.Find(".moka-log-viewer__filter-btn--info").Click();
		cut.Render(p => p.Add(x => x.Levels, new HashSet<MokaLogLevel>(Enum.GetValues<MokaLogLevel>())));

		Assert.DoesNotContain("Application started.", cut.FindAll(".moka-log-viewer__message").Select(m => m.TextContent));
	}

	[Fact]
	public void HasDetail_IsAnExceptionOrAProperty()
	{
		MokaLogEntry plain = new(At, MokaLogLevel.Info, "Hello");

		Assert.False(plain.HasDetail);
		Assert.False((plain with { Exception = "", Properties = [] }).HasDetail);
		Assert.True((plain with { Exception = "boom" }).HasDetail);
		Assert.True((plain with { Properties = [new("Key", "Value")] }).HasDetail);
	}

	[Fact]
	public void OnlyARowWithDetail_HasAToggle_NamedAndInATooltip()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p.Add(x => x.Entries, Entries));

		IElement toggle = cut.Find(".moka-log-viewer__toggle");
		Assert.Single(cut.FindAll(".moka-log-viewer__toggle"));
		Assert.Equal("button", toggle.GetAttribute("type"));
		Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
		Assert.False(toggle.HasAttribute("aria-controls"));
		Assert.Contains("moka-log-viewer__row--error", toggle.Closest(".moka-log-viewer__row")!.ClassList);
		IconTooltip.AssertNamed(toggle, "Show details", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public void TheToggle_OpensTheDetailUnderItsRow_AndClosesIt()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p.Add(x => x.Entries, Entries));

		cut.Find(".moka-log-viewer__toggle").Click();

		IElement toggle = cut.Find(".moka-log-viewer__toggle");
		IElement row = toggle.Closest(".moka-log-viewer__row")!;
		IElement detail = row.NextElementSibling!;
		Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
		Assert.Contains("moka-log-viewer__row--open", row.ClassList);
		Assert.Contains("moka-log-viewer__detail", detail.ClassList);
		Assert.Equal(detail.Id, toggle.GetAttribute("aria-controls"));
		IconTooltip.AssertNamed(toggle, "Hide details", MokaTooltipPosition.BottomEnd);
		Assert.Equal(Failure.Exception, detail.QuerySelector("pre.moka-log-viewer__exception")!.TextContent);
		Assert.Equal(["OrderId", "4711", "Attempt", "3"],
			detail.QuerySelectorAll(".moka-log-viewer__properties > *").Select(e => e.TextContent));
		Assert.Empty(cut.FindAll(".moka-log-viewer__detail-message"));
		Assert.Empty(cut.FindAll(".moka-log-viewer__pane"));

		cut.Find(".moka-log-viewer__toggle").Click();

		Assert.Empty(cut.FindAll(".moka-log-viewer__detail"));
		Assert.Equal("false", cut.Find(".moka-log-viewer__toggle").GetAttribute("aria-expanded"));
	}

	[Fact]
	public void DetailTemplate_DrawsTheDetail()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.DetailTemplate, entry => builder =>
			{
				builder.OpenElement(0, "span");
				builder.AddAttribute(1, "class", "mine");
				builder.AddContent(2, entry.Properties!.Count);
				builder.CloseElement();
			}));

		cut.Find(".moka-log-viewer__toggle").Click();

		Assert.Equal("2", cut.Find(".moka-log-viewer__detail .mine").TextContent);
		Assert.Empty(cut.FindAll(".moka-log-viewer__exception"));
	}

	[Fact]
	public void UnderVirtualize_TheToggleOpensThePaneUnderTheList()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.Virtualize, true));

		cut.Find(".moka-log-viewer__toggle").Click();

		IElement toggle = cut.Find(".moka-log-viewer__toggle");
		IElement pane = cut.Find(".moka-log-viewer__pane");
		Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
		Assert.Equal(pane.Id, toggle.GetAttribute("aria-controls"));
		Assert.Contains("moka-log-viewer__row--open", toggle.Closest(".moka-log-viewer__row")!.ClassList);
		Assert.Contains("moka-log-viewer", pane.ParentElement!.ClassList);
		Assert.Contains("moka-log-viewer__body", pane.PreviousElementSibling!.ClassList);
		Assert.Equal("Payment failed", pane.QuerySelector(".moka-log-viewer__detail-message")!.TextContent);
		Assert.Equal(Failure.Exception, pane.QuerySelector(".moka-log-viewer__exception")!.TextContent);
		Assert.Equal("09:30:00.000 ERR Payments", pane.GetAttribute("aria-label"));
		Assert.Equal(["09:30:00.000", "ERR", "Payments"],
			pane.QuerySelector(".moka-log-viewer__pane-title")!.Children.Select(c => c.TextContent));
		Assert.Empty(cut.FindAll(".moka-log-viewer__detail"));
		IconTooltip.AssertNamed(cut.Find(".moka-log-viewer__close"), "Close details", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public async Task UnderVirtualize_ClosingThePane_PutsFocusOnTheList()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.Virtualize, true));
		string body = BodyReference(cut);
		await cut.Find(".moka-log-viewer__toggle").ClickAsync(new());

		await cut.Find(".moka-log-viewer__close").ClickAsync(new());

		Assert.Empty(cut.FindAll(".moka-log-viewer__pane"));
		Assert.Equal("false", cut.Find(".moka-log-viewer__toggle").GetAttribute("aria-expanded"));
		JSRuntimeInvocation focus = Assert.Single(JSInterop.Invocations, i => i.Identifier == "Blazor._internal.domWrapper.focus");
		Assert.Equal(body, Assert.IsType<ElementReference>(focus.Arguments[0]).Id);
	}

	[Fact]
	public void AutoScroll_FollowsTheListInTheBrowser_Once()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p.Add(x => x.Entries, Entries));
		string body = BodyReference(cut);
		cut.Render(p => p.Add(x => x.Entries, Many(3)));

		JSRuntimeInvocation watch = _module.VerifyInvoke("watch");
		Assert.Equal(body, Assert.IsType<ElementReference>(watch.Arguments[0]).Id);
		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(watch.Arguments[1]);
		Assert.Equal(true, options["follow"]);
		Assert.Equal(false, options["newestFirst"]);
		Assert.Equal(0f, options["rowHeight"]);
	}

	// bUnit writes the reference's id into the markup of the render that captured it, the first one.
	private static string BodyReference(IRenderedComponent<MokaLogViewer> cut)
	{
		string? id = cut.Find(".moka-log-viewer__body").GetAttribute("blazor:elementReference");
		Assert.False(string.IsNullOrEmpty(id));
		return id;
	}

	[Fact]
	public void WithoutAutoScroll_NoScriptLoads()
	{
		Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.AutoScroll, false));

		Assert.Empty(_module.Invocations);
	}

	[Fact]
	public void TurningAutoScrollOff_StopsFollowing()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p.Add(x => x.Entries, Entries));

		cut.Render(p => p.Add(x => x.AutoScroll, false));

		_module.VerifyInvoke("unwatch");
	}

	[Fact]
	public async Task Disposing_StopsFollowing()
	{
		Render<MokaLogViewer>(p => p.Add(x => x.Entries, Entries));

		await DisposeComponentsAsync();

		_module.VerifyInvoke("unwatch");
	}

	[Fact]
	public void Text_ReplacesTheWords()
	{
		MokaLogViewerText words = new() { SearchPlaceholder = "Protokoll durchsuchen", NoEntries = "Keine Einträge" };

		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p.Add(x => x.Text, words));

		Assert.Equal("Protokoll durchsuchen", cut.Find(".moka-log-viewer__search-input").GetAttribute("placeholder"));
		Assert.Equal("Keine Einträge", cut.Find(".moka-log-viewer__empty").TextContent);
	}
}
