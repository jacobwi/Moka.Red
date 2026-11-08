using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Forms.SettingsForm;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

public class MokaSettingsLayoutTests : BunitContext
{
	// Inside an app shell the rail and the padding take about 130 px, so a 794 px window left the section 260 px.
	// The layout's own width now decides when the list becomes a page of its own.
	[Fact]
	public void ItsOwnWidth_DecidesWhenTheListBecomesAPage()
	{
		string css = System.IO.File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Css", Stylesheet));

		Assert.Equal("moka-settings-layout / inline-size", ScopedCss.Declarations(Stylesheet, ".moka-settings-layout")["container"]);
		Assert.Contains("@container moka-settings-layout (max-width: 45rem)", css, StringComparison.Ordinal);
		Assert.DoesNotContain("@media", css, StringComparison.Ordinal);
		Assert.Equal("1 / -1", ScopedCss.Declarations(Stylesheet, ".moka-settings-layout > .moka-settings-layout__content")["grid-column"]);
	}

	private const string Module = "./_content/Moka.Red.Forms/SettingsForm/MokaSettingsLayout.razor.js";
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";
	private const string HoverModule = "./_content/Moka.Red.Core/moka-hover.js";
	private const string Stylesheet = "MokaSettingsLayout.razor.css";

	private static readonly TimeSpan Plenty = TimeSpan.FromSeconds(10);

	private static readonly IReadOnlyList<MokaSettingsSection> Sections =
	[
		new("general", "General", "System", "settings/general"),
		new("updates", "Updates", "System", "settings/updates"),
		new("names", "Names", "Network", "settings/names")
	];

	private static readonly IReadOnlyList<MokaSettingsEntry> Entries =
	[
		new("Time zone", "clock region", "time-zone") { Section = "General", Route = "settings/general" },
		new("Host name", "router title", "host-name") { Section = "General", Route = "settings/general" },
		new("Lookup servers", "dns time", "lookups", Advanced: true) { Section = "Names" }
	];

	private readonly BunitJSModuleInterop _keys;
	private readonly BunitJSModuleInterop _module;
	private readonly BunitJSModuleInterop _hover;

	public MokaSettingsLayoutTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
		_keys = JSInterop.SetupModule(KeysModule);
		_keys.Mode = JSRuntimeMode.Loose;
		_hover = JSInterop.SetupModule(HoverModule);
		_hover.Mode = JSRuntimeMode.Loose;
	}

	private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	private IRenderedComponent<MokaSettingsLayout> Layout(
		Action<ComponentParameterCollectionBuilder<MokaSettingsLayout>>? more = null,
		IReadOnlyList<MokaSettingsSection>? sections = null,
		string? active = "general") =>
		Render<MokaSettingsLayout>(p =>
		{
			p.Add(x => x.Sections, sections ?? Sections).Add(x => x.Active, active);
			more?.Invoke(p);
		});

	private IRenderedComponent<MokaSettingsLayout> WithSearch(Action<ComponentParameterCollectionBuilder<MokaSettingsLayout>>? more = null) =>
		Layout(p =>
		{
			p.Add(x => x.Entries, Entries);
			more?.Invoke(p);
		});

	private static void Type(IRenderedComponent<MokaSettingsLayout> cut, string text)
	{
		cut.Find("input").Focus();
		cut.Find("input").Input(text);
	}

	private static void Press(IRenderedComponent<MokaSettingsLayout> cut, string key) =>
		cut.Find("input").KeyDown(new KeyboardEventArgs { Key = key });

	private static string[] ResultLabels(IRenderedComponent<MokaSettingsLayout> cut) =>
		[.. cut.FindAll(".moka-settings-layout__result-label").Select(label => label.TextContent)];

	[Fact]
	public void TheSideList_IsNavigation_InGroups()
	{
		IRenderedComponent<MokaSettingsLayout> cut = Layout();

		Assert.Equal("Settings sections", cut.Find("nav").GetAttribute("aria-label"));
		Assert.Equal(["System", "Network"], cut.FindAll(".moka-settings-layout__group-name").Select(name => name.TextContent));
		Assert.Equal(["General", "Updates", "Names"], cut.FindAll(".moka-settings-layout__row-label").Select(row => row.TextContent));
	}

	[Fact]
	public void SectionsOfAGroup_StayTogether()
	{
		IRenderedComponent<MokaSettingsLayout> cut = Layout(sections:
		[
			new MokaSettingsSection("a", "A", "One"),
			new MokaSettingsSection("b", "B", "Two"),
			new MokaSettingsSection("c", "C", "One")
		]);

		Assert.Equal(["One", "Two"], cut.FindAll(".moka-settings-layout__group-name").Select(name => name.TextContent));
		Assert.Equal(["A", "C", "B"], cut.FindAll(".moka-settings-layout__row-label").Select(row => row.TextContent));
	}

	[Fact]
	public void ASectionWithoutAGroup_HasNoGroupName()
	{
		IRenderedComponent<MokaSettingsLayout> cut = Layout(sections: [new MokaSettingsSection("a", "A")]);

		Assert.Empty(cut.FindAll(".moka-settings-layout__group-name"));
	}

	[Fact]
	public void TheActiveRow_IsMarked()
	{
		IReadOnlyList<IElement> rows = Layout().FindAll(".moka-settings-layout__row");

		Assert.Equal(["page", null, null], rows.Select(row => row.GetAttribute("aria-current")));
		Assert.Contains("moka-settings-layout__row--active", rows[0].ClassName, StringComparison.Ordinal);
		Assert.DoesNotContain("moka-settings-layout__row--active", rows[1].ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void ARowWithAnAddress_IsALink()
	{
		IElement row = Layout().FindAll(".moka-settings-layout__row")[1];

		Assert.Equal("A", row.TagName);
		Assert.Equal("settings/updates", row.GetAttribute("href"));
	}

	[Fact]
	public void ARowWithoutAnAddress_IsAButton_ThatPicksIt()
	{
		string? picked = "none";
		IRenderedComponent<MokaSettingsLayout> cut = Layout(
			p => p.Add(x => x.ActiveChanged, EventCallback.Factory.Create<string?>(this, id => picked = id)),
			[new MokaSettingsSection("a", "A"), new MokaSettingsSection("b", "B")],
			"a");

		cut.FindAll("button.moka-settings-layout__row")[1].Click();

		Assert.Equal("b", picked);
		Assert.Contains("moka-settings-layout__row--active", cut.FindAll(".moka-settings-layout__row")[1].ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void AnUnsafeAddress_IsDropped()
	{
		IRenderedComponent<MokaSettingsLayout> cut =
			Layout(sections: [new MokaSettingsSection("a", "A", Href: "javascript:alert(1)")]);

		Assert.Equal("BUTTON", cut.Find(".moka-settings-layout__row").TagName);
	}

	[Fact]
	public void WhileASectionShows_ThePhoneShowsIt_NotTheList() =>
		Assert.DoesNotContain("moka-settings-layout--list", Layout().Find(".moka-settings-layout").ClassName, StringComparison.Ordinal);

	[Fact]
	public void WithNothingActive_ThePhoneShowsTheList() =>
		Assert.Contains("moka-settings-layout--list",
			Layout(active: null).Find(".moka-settings-layout").ClassName, StringComparison.Ordinal);

	[Fact]
	public void OnAPhone_OnlyOneOfTheTwoShows()
	{
		Assert.Equal("none", ScopedCss.Declarations(Stylesheet, ".moka-settings-layout--list > .moka-settings-layout__content")["display"]);
		Assert.Equal("none", ScopedCss.Declarations(Stylesheet, ".moka-settings-layout:not(.moka-settings-layout--list) > .moka-settings-layout__side")["display"]);
	}

	// It stuck at 16 px from the window's top, under a sticky app bar. On a phone it doesn't stick at all.
	[Fact]
	public void TheSideList_SticksBelowAStickyAppBar()
	{
		Assert.Equal(["sticky", "static"], ScopedCss.Values(Stylesheet, ".moka-settings-layout__side", "position"));
		Assert.Equal(["calc(var(--moka-appbar-space, 0px) + var(--moka-spacing-xl))"],
			ScopedCss.Values(Stylesheet, ".moka-settings-layout__side", "top"));
	}

	[Fact]
	public void BackHref_MakesTheWayBackALink()
	{
		IElement back = Layout(p => p.Add(x => x.BackHref, "settings")).Find(".moka-settings-layout__back");

		Assert.Equal("A", back.TagName);
		Assert.Equal("settings", back.GetAttribute("href"));
		Assert.Equal("All settings", back.TextContent.Trim());
	}

	[Fact]
	public void WithoutBackHref_TheWayBackClearsActive()
	{
		string? picked = "none";
		IRenderedComponent<MokaSettingsLayout> cut = Layout(p => p
			.Add(x => x.ActiveChanged, EventCallback.Factory.Create<string?>(this, id => picked = id)));

		cut.Find("button.moka-settings-layout__back").Click();

		Assert.Null(picked);
		Assert.Contains("moka-settings-layout--list", cut.Find(".moka-settings-layout").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void TheFormInside_MarksTheActiveRowWhileEdited()
	{
		bool dirty = true;
		RenderFragment form = builder =>
		{
			builder.OpenComponent<MokaSettingsForm>(0);
			builder.AddComponentParameter(1, nameof(MokaSettingsForm.Title), "General");
			builder.AddComponentParameter(2, nameof(MokaSettingsForm.Dirty), dirty);
			builder.CloseComponent();
		};

		IRenderedComponent<MokaSettingsLayout> cut = Layout(p => p.Add(x => x.ChildContent, form));

		cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".moka-settings-layout__dirty")), Plenty);
		Assert.Equal("Unsaved edits", cut.Find(".moka-settings-layout__row--active .moka-visually-hidden").TextContent);

		dirty = false;
		cut.Render(p => p.Add(x => x.ChildContent, form));

		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".moka-settings-layout__dirty")), Plenty);
	}

	[Fact]
	public void TheFormInside_TakesTheLayoutsEntries_ForALinkToAnAdvancedSetting()
	{
		Navigation.NavigateTo("/settings#lookups");
		RenderFragment form = builder =>
		{
			builder.OpenComponent<MokaSettingsForm>(0);
			builder.AddComponentParameter(1, nameof(MokaSettingsForm.Title), "General");
			builder.AddComponentParameter(2, nameof(MokaSettingsForm.HasAdvanced), true);
			builder.CloseComponent();
		};

		IRenderedComponent<MokaSettingsLayout> cut = WithSearch(p => p.Add(x => x.ChildContent, form));

		cut.WaitForAssertion(() =>
			Assert.Equal("true", cut.Find(".moka-settings-form__advanced").GetAttribute("aria-expanded")), Plenty);
	}

	[Fact]
	public void WithoutEntries_ThereIsNoSearchBox()
	{
		IRenderedComponent<MokaSettingsLayout> cut = Layout();

		Assert.Empty(cut.FindAll("input"));
		Assert.Empty(_module.Invocations["bindSlashKey"]);
	}

	[Fact]
	public void TheSearchBox_IsANamedCombobox_WithTheSlashHint()
	{
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch();

		IElement input = cut.Find("input");
		Assert.Equal("combobox", input.GetAttribute("role"));
		Assert.Equal("Search settings", input.GetAttribute("aria-label"));
		Assert.Equal("Search settings", input.GetAttribute("placeholder"));
		Assert.Equal("false", input.GetAttribute("aria-expanded"));
		Assert.Equal("/", cut.Find(".moka-kbd").TextContent.Trim());
		Assert.Single(cut.FindAll("[role=search]"));
	}

	[Fact]
	public void TheSlashKey_IsBoundOnce()
	{
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch();

		cut.Render(p => p.Add(x => x.BackText, "Back"));

		Assert.Single(_module.Invocations["bindSlashKey"]);
	}

	[Fact]
	public void TheKeysItUses_AreCancelledInTheBrowser_ByRule()
	{
		WithSearch();

		JSRuntimeInvocation call = Assert.Single(_keys.Invocations["preventKeys"]);
		Dictionary<string, object?> rule = Assert.Single((Dictionary<string, object?>[])call.Arguments[1]!);
		Assert.Equal("input", rule["selector"]);
		Assert.Equal(["ArrowDown", "ArrowUp", "Enter"], (string[])rule["keys"]!);
		Assert.Equal(true, rule["unlessModified"]);
	}

	[Fact]
	public void Typing_ShowsWhatMatches_InTheLabelKeywordsOrSection()
	{
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch();

		Type(cut, "clock");
		Assert.Equal(["Time zone"], ResultLabels(cut));

		Type(cut, "names");
		Assert.Equal(["Lookup servers"], ResultLabels(cut));
		Assert.Equal("Names", cut.Find(".moka-settings-layout__result-section").TextContent);
	}

	[Fact]
	public void ALabelThatStartsWithTheWord_ComesFirst()
	{
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch();

		Type(cut, "time");

		Assert.Equal(["Time zone", "Lookup servers"], ResultLabels(cut));
	}

	[Fact]
	public void EveryWord_HasToMatch()
	{
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch();

		Type(cut, "time dns");

		Assert.Equal(["Lookup servers"], ResultLabels(cut));
	}

	[Fact]
	public void MaxResults_LimitsTheList()
	{
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch(p => p.Add(x => x.MaxResults, 1));

		Type(cut, "time");

		Assert.Equal(["Time zone"], ResultLabels(cut));
	}

	[Fact]
	public void NoMatch_SaysSo()
	{
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch();

		Type(cut, "zebra");

		Assert.Equal("No setting matches that.", cut.Find(".moka-settings-layout__none").TextContent);
		Assert.Null(cut.Find("input").GetAttribute("aria-activedescendant"));
	}

	[Fact]
	public void TheResults_AreAListboxTheInputPointsAt()
	{
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch();

		Type(cut, "time");

		IElement input = cut.Find("input");
		IElement results = cut.Find("[role=listbox]");
		Assert.Equal("true", input.GetAttribute("aria-expanded"));
		Assert.Equal(results.Id, input.GetAttribute("aria-controls"));
		Assert.Equal(cut.FindAll("[role=option]")[0].Id, input.GetAttribute("aria-activedescendant"));
	}

	[Fact]
	public void TheResults_CloseWithFocus()
	{
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch();
		Type(cut, "time");

		cut.Find("input").Blur();

		Assert.Empty(cut.FindAll("[role=listbox]"));
	}

	[Fact]
	public void TheArrows_MoveTheHighlight_AndWrap()
	{
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch();
		Type(cut, "time");

		string[] Selected() => [.. cut.FindAll("[role=option]").Select(option => option.GetAttribute("aria-selected")!)];

		Assert.Equal(["true", "false"], Selected());
		Press(cut, "ArrowDown");
		Assert.Equal(["false", "true"], Selected());
		Assert.Equal(cut.FindAll("[role=option]")[1].Id, cut.Find("input").GetAttribute("aria-activedescendant"));
		Press(cut, "ArrowDown");
		Assert.Equal(["true", "false"], Selected());
		Press(cut, "ArrowUp");
		Assert.Equal(["false", "true"], Selected());
	}

	[Fact]
	public void Enter_GoesToTheHighlightedSetting()
	{
		MokaSettingsEntry? picked = null;
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch(p => p
			.Add(x => x.OnPick, EventCallback.Factory.Create<MokaSettingsEntry>(this, entry => picked = entry)));
		Type(cut, "time");

		Press(cut, "Enter");

		Assert.Equal("Time zone", picked?.Label);
		Assert.EndsWith("/settings/general#time-zone", Navigation.Uri, StringComparison.Ordinal);
		Assert.Equal(string.Empty, cut.Find("input").GetAttribute("value"));
		Assert.Empty(cut.FindAll("[role=listbox]"));
	}

	[Fact]
	public void AnEntryWithoutARoute_StaysOnThisPage()
	{
		Navigation.NavigateTo("/settings/names?tab=1");
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch();
		Type(cut, "lookup");

		Press(cut, "Enter");

		Assert.EndsWith("/settings/names?tab=1#lookups", Navigation.Uri, StringComparison.Ordinal);
	}

	[Fact]
	public void ClickingAResult_GoesToIt_WithoutTakingFocusFromTheBox()
	{
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch();
		Type(cut, "host");

		cut.Find("[role=option]").Click();

		Assert.EndsWith("/settings/general#host-name", Navigation.Uri, StringComparison.Ordinal);
	}

	[Fact]
	public void Escape_ClearsTheSearch()
	{
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch();
		Type(cut, "time");

		Press(cut, "Escape");

		Assert.Empty(cut.FindAll("[role=listbox]"));
		Assert.Equal(string.Empty, cut.Find("input").GetAttribute("value"));
	}

	[Fact]
	public void AKeyWithAModifier_IsLeftAlone()
	{
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch();
		Type(cut, "time");

		cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });

		Assert.Equal("true", cut.FindAll("[role=option]")[0].GetAttribute("aria-selected"));
	}

	[Fact]
	public void TheMouse_MovesTheHighlightThroughTheBrowser()
	{
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch();

		Type(cut, "time");

		Assert.Single(_hover.Invocations["trackHover"]);
		Assert.Equal(["0", "1"], cut.FindAll("[role=option]").Select(option => option.GetAttribute("data-moka-index")));
		Assert.DoesNotContain(cut.Find("[role=option]").Attributes, a => a.Name.Contains("mouseenter", StringComparison.Ordinal));
	}

	[Fact]
	public void AKeyAfterTheMouseMoved_CarriesOnFromThatRow()
	{
		IRenderedComponent<MokaSettingsLayout> cut = WithSearch();
		Type(cut, "time");

		cut.Instance.OnItemHovered(1);
		Press(cut, "ArrowUp");

		Assert.Equal("true", cut.FindAll("[role=option]")[0].GetAttribute("aria-selected"));
	}

	[Fact]
	public void OneRowLights_ForTheMouseOrTheKeyboard()
	{
		IReadOnlyDictionary<string, string> keyboard = ScopedCss.Declarations(Stylesheet,
			".moka-settings-layout__results:not([data-moka-pointer]) .moka-settings-layout__result--active");
		IReadOnlyDictionary<string, string> mouse = ScopedCss.Declarations(Stylesheet,
			".moka-settings-layout__results[data-moka-pointer] .moka-settings-layout__result:hover");

		Assert.Equal(keyboard["background"], mouse["background"]);
	}

	[Fact]
	public void TheLayout_TakesClassStyleIdAndAttributes()
	{
		IElement layout = Layout(p => p
			.Add(x => x.Class, "mine")
			.Add(x => x.Style, "max-width: 30rem")
			.Add(x => x.Id, "settings")
			.AddUnmatched("data-area", "main")).Find(".moka-settings-layout");

		Assert.EndsWith("mine", layout.ClassName, StringComparison.Ordinal);
		Assert.Equal("max-width: 30rem", layout.GetAttribute("style"));
		Assert.Equal("settings", layout.Id);
		Assert.Equal("main", layout.GetAttribute("data-area"));
	}

	[Fact]
	public void TheEntrysHref_IsItsRouteAndAnchor()
	{
		Assert.Equal("settings/general#time-zone", Entries[0].Href);
		Assert.Equal("#a%20b", new MokaSettingsEntry("A", "", "a b").Href);
	}

	// The current section reads the theme's current-place tokens, so a theme that asks for a solid tile fills it.
	[Fact]
	public void TheCurrentSection_ReadsTheCurrentPlaceTokens()
	{
		IReadOnlyDictionary<string, string> row = ScopedCss.Declarations("MokaSettingsLayout.razor.css", ".moka-settings-layout__row--active");

		Assert.Equal("var(--moka-current-bg)", row["background"]);
		Assert.Equal("var(--moka-current-color)", row["color"]);
		Assert.Equal("var(--moka-current-shadow)", row["box-shadow"]);
		Assert.Equal("inset 0 0 0 1px var(--moka-current-edge), var(--moka-focus-ring)",
			ScopedCss.Declarations("MokaSettingsLayout.razor.css", ".moka-settings-layout__row--active:focus-visible")["box-shadow"]);
	}
}
