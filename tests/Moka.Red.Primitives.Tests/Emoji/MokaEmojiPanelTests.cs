using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Primitives.Emoji;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Emoji;

public sealed class MokaEmojiPanelTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Primitives/Emoji/MokaEmojiPanel.razor.js";
	private const string Thumbs = "\U0001F44D";
	private const string Tada = "\U0001F389";

	private static readonly DateTimeOffset Noon = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

	private readonly BunitJSModuleInterop _module;
	private readonly BunitJSModuleInterop _popup;
	private readonly FixedClock _clock = new(Noon);

	public MokaEmojiPanelTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		_popup = TopLayerPopup.Setup(JSInterop);
		Services.AddSingleton<TimeProvider>(_clock);
	}

	[Fact]
	public void ByDefault_ItIsAGroupWithSearchToneTabsAGridAndAPreview()
	{
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>();
		IElement root = cut.Find(".moka-emoji-panel");
		IElement grid = cut.Find(".moka-emoji-panel__scroll");

		Assert.Equal("group", root.GetAttribute("role"));
		Assert.Equal("Choose an emoji", root.GetAttribute("aria-label"));
		Assert.Null(root.GetAttribute("popover"));
		Assert.Contains("moka-emoji-panel--popover", root.ClassList);
		Assert.Equal("grid", grid.GetAttribute("role"));
		Assert.Equal("0", grid.GetAttribute("tabindex"));
		Assert.Empty(grid.Children);

		IElement query = cut.Find(".moka-emoji-panel__query");
		Assert.Equal("combobox", query.GetAttribute("role"));
		Assert.Equal(grid.Id, query.GetAttribute("aria-controls"));
		Assert.Null(query.GetAttribute("value"));

		Assert.Equal("Skin tone, Default", cut.Find(".moka-emoji-panel__tone").GetAttribute("aria-label"));
		Assert.Single(cut.FindAll(".moka-emoji-panel__preview[aria-hidden=true]"));
		Assert.Empty(cut.FindAll(".moka-emoji-panel__hints"));
		Assert.Single(cut.FindAll(".moka-emoji-panel__status[role=status]"));
	}

	[Fact]
	public void TheTabs_AreNineNamedIconButtons_InTooltips()
	{
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>();
		IReadOnlyList<IElement> tabs = cut.FindAll(".moka-emoji-panel__tab");

		Assert.Equal(
			["smileys", "people", "nature", "food", "travel", "activities", "objects", "symbols", "flags"],
			tabs.Select(tab => tab.GetAttribute("data-go")));
		Assert.Equal("Smileys and emotion", tabs[0].GetAttribute("aria-label"));
		Assert.Equal("0", tabs[0].GetAttribute("tabindex"));
		Assert.All(tabs.Skip(1), tab => Assert.Equal("-1", tab.GetAttribute("tabindex")));
		Assert.All(tabs, tab => Assert.NotNull(tab.QuerySelector("svg")));
		Assert.All(tabs, tab =>
		{
			Assert.Contains("moka-emoji-panel__tab-tip", tab.ParentElement!.ClassList);
			Assert.Equal(tab.GetAttribute("aria-label"), tab.ParentElement.QuerySelector("[role=tooltip]")?.TextContent.Trim());
		});
		Assert.Equal("horizontal", cut.Find(".moka-emoji-panel__tabs").GetAttribute("aria-orientation"));
	}

	[Theory]
	[InlineData(MokaEmojiNavigation.Tabs, true, false)]
	[InlineData(MokaEmojiNavigation.Rail, true, true)]
	[InlineData(MokaEmojiNavigation.Headings, false, false)]
	[InlineData(MokaEmojiNavigation.SearchOnly, false, false)]
	public void Navigation_PutsTheTabsWhereItSays(MokaEmojiNavigation navigation, bool tabs, bool rail)
	{
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.Options, new MokaEmojiOptions { Navigation = navigation }));

		Assert.Equal(tabs, cut.FindAll(".moka-emoji-panel__tabs").Count == 1);
		Assert.Equal(rail, cut.FindAll(".moka-emoji-panel__body--rail").Count == 1);
		if (rail)
		{
			Assert.Equal("vertical", cut.Find(".moka-emoji-panel__tabs").GetAttribute("aria-orientation"));
			Assert.NotNull(cut.Find(".moka-emoji-panel__main").QuerySelector(".moka-emoji-panel__tabs"));
		}

		Assert.Contains($"moka-emoji-panel--nav-{Moka.Red.Core.Utilities.MokaEnumHelpers.ToCssClass(navigation)}",
			cut.Find(".moka-emoji-panel").ClassList);
	}

	[Fact]
	public void TheComposerShape_PutsTheTabsAlongTheBottom()
	{
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.Options, new MokaEmojiOptions { Shape = MokaEmojiShape.Composer }));
		IElement body = cut.Find(".moka-emoji-panel__body");

		Assert.Contains("moka-emoji-panel__body--tabs-below", body.ClassList);
		Assert.Equal("moka-emoji-panel__tabs", body.Children.SkipWhile(c => !c.ClassList.Contains("moka-emoji-panel__main")).Skip(1).First().ClassName);
	}

	[Fact]
	public void TheListShape_IsAListbox()
	{
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.Options, new MokaEmojiOptions { Shape = MokaEmojiShape.List }));

		Assert.Equal("listbox", cut.Find(".moka-emoji-panel__scroll").GetAttribute("role"));
		Assert.Contains("moka-emoji-panel--list", cut.Find(".moka-emoji-panel").ClassList);
	}

	[Fact]
	public async Task TheStrip_ShowsQuickPicks_AndMoreOpensTheGridInPlace()
	{
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.Options, new MokaEmojiOptions { Shape = MokaEmojiShape.Strip })
			.Add(x => x.Selected, "\u2615"));

		IReadOnlyList<IElement> quick = cut.FindAll(".moka-emoji-panel__quick");
		Assert.Equal(8, quick.Count);
		Assert.Equal("\u2615", quick.Single(q => q.GetAttribute("aria-pressed") == "true").GetAttribute("data-emoji"));
		Assert.Empty(cut.FindAll(".moka-emoji-panel__body"));

		IElement more = cut.Find(".moka-emoji-panel__more");
		Assert.Equal("false", more.GetAttribute("aria-expanded"));
		await more.ClickAsync(new MouseEventArgs());

		Assert.Equal("true", cut.Find(".moka-emoji-panel__more").GetAttribute("aria-expanded"));
		Assert.Equal(cut.Find(".moka-emoji-panel__body").Id, cut.Find(".moka-emoji-panel__more").GetAttribute("aria-controls"));
		Assert.Single(cut.FindAll(".moka-emoji-panel__scroll"));
		_module.VerifyInvoke("focusStart");

		await cut.Find(".moka-emoji-panel").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		Assert.Empty(cut.FindAll(".moka-emoji-panel__body"));
	}

	[Fact]
	public void TheQuickPicks_CanBeTheAppsOwn()
	{
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.Options, new MokaEmojiOptions { Shape = MokaEmojiShape.Strip, Quick = [Thumbs, Tada] }));

		Assert.Equal([Thumbs, Tada], cut.FindAll(".moka-emoji-panel__quick").Select(q => q.GetAttribute("data-emoji")));
	}

	[Fact]
	public void EachSwitch_TakesItsPartAway()
	{
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.Options, new MokaEmojiOptions { ShowSearch = false, SkinTones = false, ShowPreview = false, ShowKeyHints = true }));

		Assert.Empty(cut.FindAll(".moka-emoji-panel__query"));
		Assert.Empty(cut.FindAll(".moka-emoji-panel__tone"));
		Assert.Empty(cut.FindAll(".moka-emoji-panel__top"));
		Assert.Empty(cut.FindAll(".moka-emoji-panel__preview"));
		IElement hints = cut.Find(".moka-emoji-panel__hints");
		Assert.Equal("true", hints.GetAttribute("aria-hidden"));
		Assert.Contains("Enter", hints.TextContent, StringComparison.Ordinal);
		Assert.Contains("PgUp", hints.TextContent, StringComparison.Ordinal);
		Assert.Contains("Home", hints.TextContent, StringComparison.Ordinal);
		Assert.NotEmpty(hints.QuerySelectorAll("kbd, .moka-kbd"));
	}

	[Fact]
	public void TheModule_GetsTheOptions_AndOnlyWhenTheyChange()
	{
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.Options, new MokaEmojiOptions { Navigation = MokaEmojiNavigation.SearchOnly, SearchKeywords = false, MaxVersion = 15.1, Columns = 7, DataSource = "emoji.sv.json", SkinTone = MokaSkinTone.Dark })
			.Add(x => x.Selected, Tada));

		IReadOnlyDictionary<string, object?> options = Options(_module.VerifyInvoke("update"));
		Assert.Equal("popover", options["shape"]);
		Assert.Equal("search-only", options["navigation"]);
		Assert.Equal(false, options["keywords"]);
		Assert.Equal(5, options["tone"]);
		Assert.Equal(15.1, options["maxVersion"]);
		Assert.Equal(7, options["columns"]);
		Assert.Equal("emoji.sv.json", options["dataSource"]);
		Assert.Equal(Tada, options["selected"]);
		Assert.Empty(Assert.IsType<string[]>(options["frequent"]));
		IReadOnlyDictionary<string, object?> words = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(options["words"]);
		Assert.Equal("Frequently used", words["frequent"]);

		cut.Render();
		_module.VerifyInvoke("update", 1);
	}

	[Fact]
	public void FrequentlyUsed_IsRankedByCountThenRecency_AndGetsATab()
	{
		IReadOnlyList<MokaEmojiUse> recent =
		[
			new(Tada, 2, Noon.AddDays(-2)),
			new(Thumbs, 5, Noon.AddDays(-3)),
			new("\U0001F680", 2, Noon.AddDays(-1))
		];

		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.Options, new MokaEmojiOptions { Recent = recent, FrequentCount = 2 }));

		Assert.Equal([Thumbs, "\U0001F680"], Assert.IsType<string[]>(Options(_module.VerifyInvoke("update"))["frequent"]));
		IElement first = cut.FindAll(".moka-emoji-panel__tab")[0];
		Assert.Equal("frequent", first.GetAttribute("data-go"));
		Assert.Equal("Frequently used", first.GetAttribute("aria-label"));
	}

	[Fact]
	public void FrequentlyUsed_Off_SendsNoneAndHasNoTab()
	{
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.Options, new MokaEmojiOptions { ShowFrequent = false, Recent = [new(Tada, 3, Noon)] }));

		Assert.Empty(Assert.IsType<string[]>(Options(_module.VerifyInvoke("update"))["frequent"]));
		Assert.Equal("smileys", cut.FindAll(".moka-emoji-panel__tab")[0].GetAttribute("data-go"));
	}

	[Fact]
	public async Task APick_RaisesOnPick_AndHandsBackTheCountedList()
	{
		MokaEmojiPick? picked = null;
		MokaEmojiOptions? changed = null;
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.OnPick, pick => picked = pick)
			.Add(x => x.OptionsChanged, options => changed = options));

		await cut.InvokeAsync(() => cut.Instance.OnEmojiPicked(Thumbs + "\U0001F3FD", Thumbs, "thumbs up", "+1"));
		cut.WaitForAssertion(() => Assert.NotNull(changed));

		Assert.Equal(new MokaEmojiPick(Thumbs + "\U0001F3FD", "thumbs up", "+1", Thumbs), picked);
		MokaEmojiUse use = Assert.Single(changed!.Recent!);
		Assert.Equal(new MokaEmojiUse(Thumbs, 1, Noon), use);

		_clock.Now = Noon.AddMinutes(5);
		changed = null;
		await cut.InvokeAsync(() => cut.Instance.OnEmojiPicked(Thumbs, Thumbs, "thumbs up", "+1"));
		cut.WaitForAssertion(() => Assert.NotNull(changed));
		Assert.Equal(new MokaEmojiUse(Thumbs, 2, Noon.AddMinutes(5)), Assert.Single(changed!.Recent!));
		Assert.Equal("frequent", cut.FindAll(".moka-emoji-panel__tab")[0].GetAttribute("data-go"));
	}

	[Fact]
	public async Task AnUnboundPanel_KeepsItsOwnList_WhenTheParentRendersAgain()
	{
		MokaEmojiOptions options = new();
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p.Add(x => x.Options, options));

		await cut.InvokeAsync(() => cut.Instance.OnEmojiPicked(Tada, Tada, "party popper", "tada"));
		cut.WaitForAssertion(() => Assert.Equal("frequent", cut.FindAll(".moka-emoji-panel__tab")[0].GetAttribute("data-go")));

		cut.Render(p => p.Add(x => x.Options, options));
		Assert.Equal("frequent", cut.FindAll(".moka-emoji-panel__tab")[0].GetAttribute("data-go"));
	}

	[Fact]
	public async Task TheToneButton_OpensSixTones_InTheTopLayer_AndAChoiceIsHandedBack()
	{
		MokaEmojiOptions? changed = null;
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.OptionsChanged, options => changed = options));

		IElement button = cut.Find(".moka-emoji-panel__tone");
		string? anchorId = button.GetAttribute("blazor:elementReference");
		Assert.Equal("false", button.GetAttribute("aria-expanded"));
		await button.ClickAsync(new MouseEventArgs());

		IElement tones = cut.Find(".moka-emoji-panel__tones");
		Assert.Equal("manual", tones.GetAttribute("popover"));
		Assert.Equal(tones.Id, cut.Find(".moka-emoji-panel__tone").GetAttribute("aria-controls"));
		Assert.Equal(6, tones.QuerySelectorAll("button").Length);
		Assert.Equal("Medium", tones.QuerySelectorAll("button")[3].GetAttribute("aria-label"));
		Assert.Equal("true", tones.QuerySelectorAll("button")[0].GetAttribute("aria-pressed"));
		TopLayerPopup.AssertOpened(_popup, tones, anchorId, "bottom-end");
		_module.VerifyInvoke("focusTone");

		await cut.FindAll(".moka-emoji-panel__tone-pick")[3].ClickAsync(new MouseEventArgs());

		Assert.Equal(MokaSkinTone.Medium, changed?.SkinTone);
		Assert.Empty(cut.FindAll(".moka-emoji-panel__tones"));
		Assert.Equal("Skin tone, Medium", cut.Find(".moka-emoji-panel__tone").GetAttribute("aria-label"));
		Assert.Equal("\u270B\U0001F3FD", cut.Find(".moka-emoji-panel__tone").TextContent);
		Assert.Equal(3, Options(_module.Invocations["update"][^1])["tone"]);
	}

	[Fact]
	public async Task EscapeInTheToneList_ClosesOnlyTheToneList()
	{
		bool closed = false;
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.OpenChanged, open => closed = !open));
		await cut.Find(".moka-emoji-panel__tone").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-emoji-panel__tones").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(cut.FindAll(".moka-emoji-panel__tones"));
		Assert.False(closed);
		Assert.Single(cut.FindAll(".moka-emoji-panel"));
	}

	[Fact]
	public void WithAnAnchor_ItIsADialogInTheTopLayer_WithABackdrop()
	{
		ElementReference anchor = new("emoji-anchor");
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.Anchor, anchor)
			.Add(x => x.Placement, Moka.Red.Feedback.Tooltip.MokaTooltipPosition.TopEnd));
		IElement root = cut.Find(".moka-emoji-panel");

		Assert.Equal("dialog", root.GetAttribute("role"));
		Assert.Contains("moka-emoji-panel--popup", root.ClassList);
		TopLayerPopup.AssertOpened(_popup, root, "emoji-anchor", "top-end");
		Assert.Equal("manual", cut.Find(".moka-emoji-panel__backdrop").GetAttribute("popover"));
		_module.VerifyInvoke("focusStart");
	}

	[Fact]
	public async Task APopup_ClosesOnEscape_OnABackdropClick_AndAfterAPick()
	{
		ElementReference anchor = new("emoji-anchor");
		bool open = true;
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.Anchor, anchor)
			.Add(x => x.Open, true)
			.Add(x => x.OpenChanged, value => open = value));

		await cut.Find(".moka-emoji-panel").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		Assert.False(open);
		Assert.Empty(cut.FindAll(".moka-emoji-panel"));
		TopLayerPopup.AssertClosed(_popup);

		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true));
		open = true;
		await cut.Find(".moka-emoji-panel__backdrop").ClickAsync(new MouseEventArgs());
		Assert.False(open);

		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true));
		open = true;
		await cut.InvokeAsync(() => cut.Instance.OnEmojiPicked(Tada, Tada, "party popper", "tada"));
		cut.WaitForAssertion(() => Assert.False(open));
	}

	[Fact]
	public async Task CloseOnPickOff_KeepsAPopupOpen()
	{
		bool open = true;
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.Anchor, new ElementReference("emoji-anchor"))
			.Add(x => x.CloseOnPick, false)
			.Add(x => x.OpenChanged, value => open = value));

		await cut.InvokeAsync(() => cut.Instance.OnEmojiPicked(Tada, Tada, "party popper", "tada"));

		Assert.True(open);
		Assert.Single(cut.FindAll(".moka-emoji-panel"));
	}

	[Fact]
	public async Task AComposerPanel_StaysOpenForSeveralPicks_AndEscapeClosesIt()
	{
		bool open = true;
		int picks = 0;
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.Options, new MokaEmojiOptions { Shape = MokaEmojiShape.Composer })
			.Add(x => x.OnPick, _ => picks++)
			.Add(x => x.OpenChanged, value => open = value));

		await cut.InvokeAsync(() => cut.Instance.OnEmojiPicked(Tada, Tada, "party popper", "tada"));
		await cut.InvokeAsync(() => cut.Instance.OnEmojiPicked(Thumbs, Thumbs, "thumbs up", "+1"));
		cut.WaitForAssertion(() => Assert.Equal(2, picks));
		Assert.True(open);

		await cut.Find(".moka-emoji-panel").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		Assert.False(open);
	}

	[Fact]
	public void Closed_RendersNothing_AndCallsNoScript()
	{
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p.Add(x => x.Open, false));

		Assert.Empty(cut.Markup.Trim());
		Assert.Empty(_module.Invocations["update"]);
	}

	[Fact]
	public async Task ADisabledPanel_IgnoresPicks()
	{
		int picks = 0;
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.Disabled, true)
			.Add(x => x.OnPick, _ => picks++));

		await cut.InvokeAsync(() => cut.Instance.OnEmojiPicked(Tada, Tada, "party popper", "tada"));

		Assert.Equal(0, picks);
		Assert.Contains("moka-emoji-panel--disabled", cut.Find(".moka-emoji-panel").ClassList);
	}

	[Fact]
	public void TheWords_CanBeReplaced()
	{
		string[] groups = ["Smileys", "Folk", "Natur", "Mat", "Resor", "Aktiviteter", "Saker", "Symboler", "Flaggor"];
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.Options, new MokaEmojiOptions
			{
				Text = MokaEmojiText.Default with { SearchPlaceholder = "S\u00F6k", SkinTone = "Hudton", Groups = groups, ToneNames = ["Ingen", "Ljus", "Mellanljus", "Mellan", "Mellanm\u00F6rk", "M\u00F6rk"] }
			}));

		Assert.Equal("S\u00F6k", cut.Find(".moka-emoji-panel__query").GetAttribute("placeholder"));
		Assert.Equal("Hudton, Ingen", cut.Find(".moka-emoji-panel__tone").GetAttribute("aria-label"));
		Assert.Equal("Flaggor", cut.FindAll(".moka-emoji-panel__tab")[8].GetAttribute("aria-label"));
		Assert.Equal(groups, Assert.IsType<string[]>(Options(_module.VerifyInvoke("update"))["groups"]));
	}

	[Fact]
	public void MarginAndPadding_LandOnTheRoot_AndTheRadiusOnTheBox()
	{
		IRenderedComponent<MokaEmojiPanel> cut = Render<MokaEmojiPanel>(p => p
			.Add(x => x.MarginValue, "7px")
			.Add(x => x.PaddingValue, "5px")
			.Add(x => x.RoundedValue, "3px")
			.Add(x => x.Height, "18rem"));

		string root = cut.Find(".moka-emoji-panel").GetAttribute("style") ?? "";
		Assert.Contains("margin: 7px", root, StringComparison.Ordinal);
		Assert.Contains("padding: 5px", root, StringComparison.Ordinal);
		Assert.Contains("--_body-height: 18rem", root, StringComparison.Ordinal);
		Assert.DoesNotContain("border-radius", root, StringComparison.Ordinal);
		Assert.Equal("border-radius: 3px", cut.Find(".moka-emoji-panel__body").GetAttribute("style"));
	}

	private static IReadOnlyDictionary<string, object?> Options(JSRuntimeInvocation update) =>
		Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(update.Arguments[2]);

	private sealed class FixedClock(DateTimeOffset now) : TimeProvider
	{
		public DateTimeOffset Now { get; set; } = now;

		public override DateTimeOffset GetUtcNow() => Now;
	}
}
