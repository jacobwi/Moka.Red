using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Icons;
using Moka.Red.Navigation.AppShell;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

public class MokaAccountCardTests : BunitContext
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	private static readonly MokaAccountItem Settings = new("Your settings", MokaIcons.Action.Settings, "/settings");
	private static readonly MokaAccountItem Power = new("Power off", Danger: true);
	private static readonly MokaAccountItem Locked = new("Not available", Disabled: true);
	private static readonly MokaAccountItem SignOut = new("Sign out");

	private readonly BunitJSModuleInterop _keys;

	public MokaAccountCardTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_keys = JSInterop.SetupModule(KeysModule);
		_keys.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void TheCard_ShowsWhoIsSignedIn()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();

		Assert.Equal("BC", cut.Find(".moka-account-card__name").TextContent);
		Assert.Equal("Owner", cut.Find(".moka-account-card__detail").TextContent);
		Assert.Equal("BC", cut.Find(".moka-avatar").TextContent.Trim());
		Assert.Equal("true", cut.Find(".moka-avatar").GetAttribute("aria-hidden"));
	}

	[Fact]
	public void TheCard_LeavesOutADetailItDoesNotHave()
	{
		IRenderedComponent<MokaAccountCard> cut = Render<MokaAccountCard>(p => p.Add(x => x.Name, "BC"));

		Assert.Empty(cut.FindAll(".moka-account-card__detail"));
		Assert.Equal("BC, Account menu", cut.Find("button").GetAttribute("aria-label"));
	}

	// The rail shows only the avatar, so the name can't come from the text on screen.
	[Fact]
	public void TheCard_IsNamedByItsLabel()
	{
		IRenderedComponent<MokaAccountCard> cut = Render<MokaAccountCard>(p => p
			.Add(x => x.Name, "BC")
			.Add(x => x.Detail, "Owner")
			.Add(x => x.MenuLabel, "Your account"));

		Assert.Equal("BC, Owner, Your account", cut.Find("button").GetAttribute("aria-label"));
	}

	[Theory]
	[InlineData("BC", "BC")]
	[InlineData("jacob", "JA")]
	[InlineData("Jacob Wi", "JW")]
	[InlineData("Ada Byron Lovelace", "AB")]
	public void TheAvatar_TakesItsLettersFromTheName(string name, string expected)
	{
		IRenderedComponent<MokaAccountCard> cut = Render<MokaAccountCard>(p => p.Add(x => x.Name, name));

		Assert.Equal(expected, cut.Find(".moka-avatar").TextContent.Trim());
	}

	[Fact]
	public void TheAvatar_TakesInitialsAndAPicture()
	{
		IRenderedComponent<MokaAccountCard> withLetters = Render<MokaAccountCard>(p => p.Add(x => x.Name, "BC").Add(x => x.Initials, "XY"));
		IRenderedComponent<MokaAccountCard> withPicture = Render<MokaAccountCard>(p => p.Add(x => x.Name, "BC").Add(x => x.AvatarSrc, "me.png"));

		Assert.Equal("XY", withLetters.Find(".moka-avatar").TextContent.Trim());
		Assert.Equal("me.png", withPicture.Find(".moka-avatar img").GetAttribute("src"));
	}

	[Fact]
	public void AClosedCard_HasNoMenu_AndListensForNoKeys()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();

		IElement card = cut.Find(".moka-account-card__card");
		Assert.Equal("menu", card.GetAttribute("aria-haspopup"));
		Assert.Equal("false", card.GetAttribute("aria-expanded"));
		Assert.False(card.HasAttribute("aria-controls"));
		Assert.Empty(cut.FindAll("[role=menu]"));
		Assert.Throws<MissingEventHandlerException>(() => cut.Find(".moka-account-card").KeyDown(new KeyboardEventArgs { Key = "Escape" }));
	}

	// A card nobody opens loads no script.
	[Fact]
	public void AClosedCard_BindsNoKeys()
	{
		Card();

		_keys.VerifyNotInvoke("preventKeys");
	}

	[Fact]
	public void Opening_ShowsTheMenu_AndFocusesTheFirstRow()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();

		Open(cut);

		IElement card = cut.Find(".moka-account-card__card");
		IElement menu = cut.Find("[role=menu]");
		Assert.Equal("true", card.GetAttribute("aria-expanded"));
		Assert.Equal(menu.Id, card.GetAttribute("aria-controls"));
		Assert.Equal("BC", menu.GetAttribute("aria-label"));
		Assert.Contains("moka-account-card__card--open", card.ClassList);
		Assert.Equal(ReferenceId(cut, 0), FocusCalls(1)[0].Id);
	}

	[Fact]
	public void Opening_CancelsTheBrowsersPartOfTheKeys()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();

		Open(cut);

		JSRuntimeInvocation call = Assert.Single(_keys.Invocations, i => i.Identifier == "preventKeys");
		using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(call.Arguments[1]));
		JsonElement[] rules = [.. document.RootElement.EnumerateArray()];
		Assert.Equal("[role^=\"menuitem\"]", rules[0].GetProperty("selector").GetString());
		Assert.Equal(["ArrowDown", "ArrowUp", "Home", "End"], rules[0].GetProperty("keys").EnumerateArray().Select(k => k.GetString()));
		Assert.Equal("a[role=\"menuitem\"]", rules[1].GetProperty("selector").GetString());
		Assert.Equal([" "], rules[1].GetProperty("keys").EnumerateArray().Select(k => k.GetString()));
	}

	[Fact]
	public void Reopening_BindsTheKeysOnce()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();

		Open(cut);
		cut.Find(".moka-account-card__card").Click();
		Open(cut);

		Assert.Single(_keys.Invocations, i => i.Identifier == "preventKeys");
	}

	[Fact]
	public void TheRows_AreLinksAndButtons_InARoleOfMenuItem()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();

		Open(cut);

		IReadOnlyList<IElement> rows = cut.FindAll("[role=menuitem]");
		Assert.Equal(["Your settings", "Power off", "Not available", "Sign out"], rows.Select(row => row.TextContent.Trim()));
		Assert.Equal("A", rows[0].TagName);
		Assert.Equal("/settings", rows[0].GetAttribute("href"));
		Assert.Equal("BUTTON", rows[1].TagName);
		Assert.All(rows, row => Assert.Equal("-1", row.GetAttribute("tabindex")));
	}

	[Fact]
	public void TheHeadingAndDividers_AreNotRows()
	{
		IRenderedComponent<MokaAccountCard> cut = Render<MokaAccountCard>(p => p
			.Add(x => x.Name, "BC")
			.Add(x => x.Heading, "BC, owner")
			.Add(x => x.Items, [Settings, MokaAccountItem.Divider, SignOut]));

		Open(cut);

		Assert.Equal("BC, owner", cut.Find(".moka-account-card__heading").TextContent);
		Assert.Single(cut.FindAll("[role=separator]"));
		Assert.Equal(2, cut.FindAll("[role=menuitem]").Count);
	}

	[Fact]
	public void ARowsLooks_ComeFromItsFlags()
	{
		IRenderedComponent<MokaAccountCard> cut = Render<MokaAccountCard>(p => p
			.Add(x => x.Name, "BC")
			.Add(x => x.Items, [Settings with { Active = true }, Power]));

		Open(cut);

		IReadOnlyList<IElement> rows = cut.FindAll("[role=menuitem]");
		Assert.Contains("moka-account-card__item--active", rows[0].ClassList);
		Assert.Equal("page", rows[0].GetAttribute("aria-current"));
		Assert.Contains("moka-account-card__item--danger", rows[1].ClassList);
		Assert.False(rows[1].HasAttribute("aria-current"));
	}

	[Fact]
	public void ADisabledRow_IsDisabled_AndNotALink()
	{
		IRenderedComponent<MokaAccountCard> cut = Render<MokaAccountCard>(p => p
			.Add(x => x.Name, "BC")
			.Add(x => x.Items, [Settings with { Disabled = true }]));

		Open(cut);

		IElement row = cut.Find("[role=menuitem]");
		Assert.Equal("BUTTON", row.TagName);
		Assert.True(row.HasAttribute("disabled"));
	}

	[Fact]
	public void AnUnsafeAddress_IsNotALink()
	{
		IRenderedComponent<MokaAccountCard> cut = Render<MokaAccountCard>(p => p
			.Add(x => x.Name, "BC")
			.Add(x => x.Items, [new MokaAccountItem("Bad", Href: "javascript:alert(1)")]));

		Open(cut);

		Assert.Equal("BUTTON", cut.Find("[role=menuitem]").TagName);
	}

	[Fact]
	public void ACheckedRow_IsACheckbox()
	{
		IRenderedComponent<MokaAccountCard> cut = Render<MokaAccountCard>(p => p
			.Add(x => x.Name, "BC")
			.Add(x => x.Items, [new MokaAccountItem("On", Checked: true), new MokaAccountItem("Off", Checked: false)]));

		Open(cut);

		IReadOnlyList<IElement> rows = cut.FindAll("[role=menuitemcheckbox]");
		Assert.Equal("true", rows[0].GetAttribute("aria-checked"));
		Assert.Equal("false", rows[1].GetAttribute("aria-checked"));
		Assert.Single(rows[0].QuerySelectorAll("svg"));
		Assert.Empty(rows[1].QuerySelectorAll("svg"));
	}

	[Theory]
	[InlineData("ArrowDown", 1)]
	[InlineData("End", 2)]
	[InlineData("ArrowUp", 2)]
	public void TheKeys_MoveThroughTheRows_PassingOverADisabledOne(string key, int expected)
	{
		IRenderedComponent<MokaAccountCard> cut = Card();
		Open(cut);
		string?[] ids = RowIds(cut);

		cut.Find(".moka-account-card").KeyDown(new KeyboardEventArgs { Key = key });

		// Settings, Power and Sign out: Not available sits between the last two and isn't a stop.
		string?[] stops = [ids[0], ids[1], ids[3]];
		Assert.Equal(stops[expected], FocusCalls(2)[1].Id);
	}

	[Fact]
	public void TheArrows_WrapAround_AndHomeGoesBack()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();
		Open(cut);
		string?[] ids = RowIds(cut);
		IElement root = cut.Find(".moka-account-card");

		root.KeyDown(new KeyboardEventArgs { Key = "End" });
		root = cut.Find(".moka-account-card");
		root.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
		root = cut.Find(".moka-account-card");
		root.KeyDown(new KeyboardEventArgs { Key = "End" });
		root = cut.Find(".moka-account-card");
		root.KeyDown(new KeyboardEventArgs { Key = "Home" });

		ElementReference[] calls = FocusCalls(5);
		Assert.Equal(ids[3], calls[1].Id);
		Assert.Equal(ids[0], calls[2].Id);
		Assert.Equal(ids[3], calls[3].Id);
		Assert.Equal(ids[0], calls[4].Id);
	}

	[Fact]
	public void AKeyWithAModifier_IsLeftAlone()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();
		Open(cut);

		cut.Find(".moka-account-card").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });

		FocusCalls(1);
	}

	[Fact]
	public void Escape_ClosesTheMenu_AndGoesBackToTheCard()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();
		string? card = cut.Find(".moka-account-card__card").GetAttribute("blazor:elementReference");
		Open(cut);

		cut.Find(".moka-account-card").KeyDown(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(cut.FindAll("[role=menu]"));
		Assert.Equal(card, FocusCalls(2)[1].Id);
	}

	// The drawer closes on Escape too, on an element around this one.
	[Fact]
	public void Escape_StopsAtTheOpenMenu()
	{
		bool heard = false;
		IRenderedComponent<ContainerFragment> cut = Render(b =>
		{
			b.OpenElement(0, "div");
			b.AddAttribute(1, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, () => heard = true));
			b.OpenComponent<MokaAccountCard>(2);
			b.AddComponentParameter(3, nameof(MokaAccountCard.Name), "BC");
			b.AddComponentParameter(4, nameof(MokaAccountCard.Items), (IReadOnlyList<MokaAccountItem>)[Settings]);
			b.CloseComponent();
			b.CloseElement();
		});
		cut.Find(".moka-account-card__card").Click();

		cut.Find(".moka-account-card").KeyDown(new KeyboardEventArgs { Key = "Escape" });

		Assert.False(heard);
		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public void Tab_ClosesTheMenu_AndLeavesFocusToTheBrowser()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();
		Open(cut);

		cut.Find(".moka-account-card").KeyDown(new KeyboardEventArgs { Key = "Tab" });

		Assert.Empty(cut.FindAll("[role=menu]"));
		FocusCalls(1);
	}

	[Fact]
	public void TheScrim_ClosesTheMenu()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();
		Open(cut);

		cut.Find(".moka-account-card__scrim").Click();

		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public void TheCard_ClosesAnOpenMenu()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();
		Open(cut);

		cut.Find(".moka-account-card__card").Click();

		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public void APick_ClosesTheMenu_AndSaysWhich()
	{
		MokaAccountItem? picked = null;
		IRenderedComponent<MokaAccountCard> cut = Render<MokaAccountCard>(p => p
			.Add(x => x.Name, "BC")
			.Add(x => x.Items, [Settings, SignOut])
			.Add(x => x.OnItemClick, EventCallback.Factory.Create<MokaAccountItem>(this, item => picked = item)));
		string? card = cut.Find(".moka-account-card__card").GetAttribute("blazor:elementReference");
		Open(cut);

		cut.FindAll("[role=menuitem]")[1].Click();

		Assert.Same(SignOut, picked);
		Assert.Empty(cut.FindAll("[role=menu]"));
		Assert.Equal(card, FocusCalls(2)[1].Id);
	}

	// The page changes, and focus is the router's to place.
	[Fact]
	public void PickingALink_LeavesFocusAlone()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();
		Open(cut);

		cut.Find("a[role=menuitem]").Click();

		Assert.Empty(cut.FindAll("[role=menu]"));
		FocusCalls(1);
	}

	[Fact]
	public void ACheckedRow_LeavesTheMenuOpen()
	{
		MokaAccountItem item = new("Descriptions", Checked: true);
		MokaAccountItem? picked = null;
		IRenderedComponent<MokaAccountCard> cut = Render<MokaAccountCard>(p => p
			.Add(x => x.Name, "BC")
			.Add(x => x.Items, [item])
			.Add(x => x.OnItemClick, EventCallback.Factory.Create<MokaAccountItem>(this, value => picked = value)));
		Open(cut);

		cut.Find("[role=menuitemcheckbox]").Click();

		Assert.Same(item, picked);
		Assert.Single(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public void Space_OnALink_GoesThere()
	{
		BunitNavigationManager navigation = Services.GetRequiredService<BunitNavigationManager>();
		MokaAccountItem? picked = null;
		IRenderedComponent<MokaAccountCard> cut = Render<MokaAccountCard>(p => p
			.Add(x => x.Name, "BC")
			.Add(x => x.Items, [Settings])
			.Add(x => x.OnItemClick, EventCallback.Factory.Create<MokaAccountItem>(this, item => picked = item)));
		Open(cut);

		cut.Find(".moka-account-card").KeyDown(new KeyboardEventArgs { Key = " " });

		Assert.EndsWith("/settings", navigation.Uri, StringComparison.Ordinal);
		Assert.Same(Settings, picked);
	}

	// A button clicks itself on Space, so doing it here as well would pick it twice.
	[Fact]
	public void Space_OnAButton_IsLeftToTheBrowser()
	{
		MokaAccountItem? picked = null;
		IRenderedComponent<MokaAccountCard> cut = Render<MokaAccountCard>(p => p
			.Add(x => x.Name, "BC")
			.Add(x => x.Items, [SignOut])
			.Add(x => x.OnItemClick, EventCallback.Factory.Create<MokaAccountItem>(this, item => picked = item)));
		Open(cut);

		cut.Find(".moka-account-card").KeyDown(new KeyboardEventArgs { Key = " " });

		Assert.Null(picked);
		Assert.Single(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public void GoingToAnotherPage_ClosesTheMenu()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();
		Open(cut);

		Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/elsewhere");

		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role=menu]")));
	}

	[Fact]
	public void ADisabledCard_CannotBeOpened() =>
		Assert.True(Render<MokaAccountCard>(p => p.Add(x => x.Name, "BC").Add(x => x.Disabled, true)).Find(".moka-account-card__card").HasAttribute("disabled"));

	// The gap is a margin on the card's side, which is where moka-popup.js takes it from in the top layer.
	[Fact]
	public void TheMenu_OpensAboveTheCard_AndBesideTheRail()
	{
		IReadOnlyDictionary<string, string> above = ScopedCss.Declarations("MokaAccountCard.razor.css", ".moka-account-card__menu");
		IReadOnlyDictionary<string, string> beside = ScopedCss.Declarations("MokaAccountCard.razor.css", "[data-moka-rail] .moka-account-card__menu");

		Assert.Equal("100%", above["bottom"]);
		Assert.Equal("0.5rem", above["margin-bottom"]);
		Assert.Equal("0", beside["bottom"]);
		Assert.Equal("100%", beside["left"]);
		Assert.Equal("0", beside["margin-bottom"]);
		Assert.Equal("1.625rem", beside["margin-left"]);
		Assert.Equal("12.5rem", beside["width"]);
		Assert.Equal("var(--moka-color-surface-variant)", above["background-color"]);
		Assert.Equal("var(--moka-shadow-popup)", above["box-shadow"]);
		Assert.Equal("2.125rem", ScopedCss.Declarations("MokaAccountCard.razor.css", ".moka-account-card__item")["min-height"]);
	}

	[Fact]
	public void TheRail_ShowsOnlyTheAvatar()
	{
		Assert.Equal("none", ScopedCss.Declarations("MokaAccountCard.razor.css", "[data-moka-rail] .moka-account-card__text")["display"]);
		Assert.Equal("none", ScopedCss.Declarations("MokaAccountCard.razor.css", "[data-moka-rail] .moka-account-card__more")["display"]);
	}

	[Fact]
	public void TheCard_IsNotTheCurrentPlace_WhileNoneOfItsPagesShows()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();

		Assert.DoesNotContain("moka-account-card__card--current", cut.Find(".moka-account-card__card").ClassList);
	}

	// On one of its pages the card is where you are, and takes the menu's current tile.
	[Fact]
	public void TheCard_IsTheCurrentPlace_WhileOneOfItsPagesShows()
	{
		IRenderedComponent<MokaAccountCard> cut = Render<MokaAccountCard>(p => p
			.Add(x => x.Name, "BC")
			.Add(x => x.Items, [Settings with { Active = true }, SignOut]));

		Assert.Contains("moka-account-card__card--current", cut.Find(".moka-account-card__card").ClassList);
		Assert.Equal("var(--moka-current-bg)",
			ScopedCss.Declarations("MokaAccountCard.razor.css", ".moka-account-card__card--current")["background-color"]);
	}

	private IRenderedComponent<MokaAccountCard> Card() => Render<MokaAccountCard>(p => p
		.Add(x => x.Name, "BC")
		.Add(x => x.Detail, "Owner")
		.Add(x => x.Items, [Settings, Power, Locked, SignOut]));

	private static void Open(IRenderedComponent<MokaAccountCard> cut) => cut.Find(".moka-account-card__card").Click();

	private static string? ReferenceId(IRenderedComponent<MokaAccountCard> cut, int row) => RowIds(cut)[row];

	// bUnit writes an element's reference id only on the render that creates it (gotcha 15), so read these right after opening.
	private static string?[] RowIds(IRenderedComponent<MokaAccountCard> cut) =>
		[.. cut.FindAll("[role=menuitem]").Select(row => row.GetAttribute("blazor:elementReference"))];

	private ElementReference[] FocusCalls(int count) =>
		[.. JSInterop.VerifyFocusAsyncInvoke(count).Select(call => Assert.IsType<ElementReference>(call.Arguments[0]))];
}
