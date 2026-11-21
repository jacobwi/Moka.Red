using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Icons;
using Moka.Red.Navigation.AppShell;

namespace Moka.Red.Navigation.Tests.Components;

// A row could only open its page in this tab, so a help page or a status page on another site took the app away.
public class MokaAccountCardTargetTests : BunitContext
{
	private const string LinksModule = "./_content/Moka.Red.Core/moka-links.js";

	private static readonly MokaAccountItem Help =
		new("Help", MokaIcons.Status.Info, "https://example.com/help") { Target = "_blank" };

	private static readonly MokaAccountItem Settings = new("Your settings", MokaIcons.Action.Settings, "/settings");

	private readonly BunitJSModuleInterop _links;

	public MokaAccountCardTargetTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule("./_content/Moka.Red.Core/moka-keys.js").Mode = JSRuntimeMode.Loose;
		_links = JSInterop.SetupModule(LinksModule);
		_links.Mode = JSRuntimeMode.Loose;
	}

	private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	public void ARowForANewTab_IsALinkThatSaysSo()
	{
		IRenderedComponent<MokaAccountCard> cut = Card(Help);
		Open(cut);

		IElement row = cut.Find("a[role=menuitem]");
		Assert.Equal("https://example.com/help", row.GetAttribute("href"));
		Assert.Equal("_blank", row.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", row.GetAttribute("rel"));
		Assert.Equal("-1", row.GetAttribute("tabindex"));
		Assert.Equal(", opens in a new tab", row.QuerySelector(".moka-visually-hidden")!.TextContent);
		IElement mark = row.QuerySelector(".moka-account-card__external")!;
		Assert.Equal("true", mark.GetAttribute("aria-hidden"));
		Assert.NotNull(mark.QuerySelector("svg"));
	}

	[Fact]
	public void NewTabText_ReplacesTheWords()
	{
		IRenderedComponent<MokaAccountCard> cut = Render<MokaAccountCard>(p => p
			.Add(x => x.Name, "BC")
			.Add(x => x.Items, [Help])
			.Add(x => x.NewTabText, "öppnas i en ny flik"));
		Open(cut);

		Assert.Equal(", öppnas i en ny flik", cut.Find("a[role=menuitem] .moka-visually-hidden").TextContent);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("_self")]
	public void ARowForThisTab_AddsNothing(string? target)
	{
		IRenderedComponent<MokaAccountCard> cut = Card(Settings with { Target = target });
		Open(cut);

		IElement row = cut.Find("a[role=menuitem]");
		Assert.Equal(target, row.GetAttribute("target"));
		Assert.False(row.HasAttribute("rel"));
		Assert.Empty(row.QuerySelectorAll(".moka-visually-hidden, .moka-account-card__external"));
	}

	[Fact]
	public void TargetDoesNotChangeTheConstructor()
	{
		MokaAccountItem item = new("Help", null, "https://example.com/help", false, false, false, null) { Target = "_blank" };

		Assert.Equal("_blank", item.Target);
		Assert.Equal(item, new MokaAccountItem("Help", Href: "https://example.com/help") { Target = "_blank" });
	}

	[Fact]
	public void ADisabledRow_IsNoLink()
	{
		IRenderedComponent<MokaAccountCard> cut = Card(Help with { Disabled = true });
		Open(cut);

		Assert.Empty(cut.FindAll("a"));
		IElement row = cut.Find("button[role=menuitem]");
		Assert.True(row.HasAttribute("disabled"));
		Assert.False(row.HasAttribute("target"));
		Assert.False(row.HasAttribute("rel"));
		Assert.Empty(row.QuerySelectorAll(".moka-visually-hidden, .moka-account-card__external"));
	}

	[Fact]
	public async Task AClick_IsTheLinksOwn_SoTheRowIsPickedAndNothingElseOpens()
	{
		MokaAccountItem? picked = null;
		IRenderedComponent<MokaAccountCard> cut = Card(Help, item => picked = item);
		Open(cut);

		await cut.Find("a[role=menuitem]").ClickAsync(new MouseEventArgs());

		Assert.Same(Help, picked);
		Assert.Empty(cut.FindAll("[role=menu]"));
		Assert.Empty(_links.Invocations["openTab"]);
		Assert.Empty(Navigation.History);
	}

	[Fact]
	public async Task Space_OpensTheTab_BeforeTheRowIsPicked()
	{
		List<string> order = [];
		IRenderedComponent<MokaAccountCard> cut = Card(Help,
			_ => order.Add(_links.Invocations["openTab"].Count == 1 ? "after the tab" : "first"));
		Open(cut);

		await cut.Find(".moka-account-card").KeyDownAsync(new KeyboardEventArgs { Key = " " });

		JSRuntimeInvocation open = Assert.Single(_links.Invocations["openTab"]);
		Assert.Equal(["https://example.com/help", "_blank"], open.Arguments);
		Assert.Equal(["after the tab"], order);
		Assert.Empty(cut.FindAll("[role=menu]"));
		Assert.Empty(Navigation.History);
	}

	[Fact]
	public async Task Space_OnALinkForThisTab_StillGoesThere()
	{
		IRenderedComponent<MokaAccountCard> cut = Card(Settings);
		Open(cut);

		await cut.Find(".moka-account-card").KeyDownAsync(new KeyboardEventArgs { Key = " " });

		Assert.EndsWith("/settings", Navigation.Uri, StringComparison.Ordinal);
		Assert.Empty(_links.Invocations["openTab"]);
	}

	// This page stays where it was, so focus goes back to the card rather than falling to the page with the menu.
	[Fact]
	public async Task PickingARowForANewTab_SendsFocusBackToTheCard()
	{
		IRenderedComponent<MokaAccountCard> cut = Card(Help);
		string? card = cut.Find(".moka-account-card__card").GetAttribute("blazor:elementReference");
		Open(cut);

		await cut.Find("a[role=menuitem]").ClickAsync(new MouseEventArgs());

		Assert.Contains(JSInterop.Invocations, call =>
			call.Identifier == "Blazor._internal.domWrapper.focus" &&
			call.Arguments[0] is ElementReference reference && reference.Id == card);
	}

	private IRenderedComponent<MokaAccountCard> Card(MokaAccountItem item, Action<MokaAccountItem>? picked = null) =>
		Render<MokaAccountCard>(p => p
			.Add(x => x.Name, "BC")
			.Add(x => x.Items, [item])
			.Add(x => x.OnItemClick, EventCallback.Factory.Create<MokaAccountItem>(this, value => picked?.Invoke(value))));

	private static void Open(IRenderedComponent<MokaAccountCard> cut) => cut.Find(".moka-account-card__card").Click();
}
