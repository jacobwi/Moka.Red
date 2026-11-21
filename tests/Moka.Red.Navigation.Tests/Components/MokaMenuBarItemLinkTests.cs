using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Navigation.MenuBar;

namespace Moka.Red.Navigation.Tests.Components;

// An item that went somewhere was a button that navigated from OnClick, so a middle click and "copy link" didn't work,
// and a page on another site could only open from a round trip after the click.
public class MokaMenuBarItemLinkTests : BunitContext
{
	private const string LinksModule = "./_content/Moka.Red.Core/moka-links.js";
	private const string Address = "https://example.com/docs";

	private readonly BunitJSModuleInterop _links;

	public MokaMenuBarItemLinkTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_links = JSInterop.SetupModule(LinksModule);
		_links.Mode = JSRuntimeMode.Loose;
	}

	private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	public void AnItemForANewTab_IsALinkMenuItem_ThatSaysSo()
	{
		IElement item = Item(p => p.Add(x => x.Href, Address).Add(x => x.Target, "_blank")).Find(".moka-menubar-item");

		Assert.Equal("A", item.TagName);
		Assert.Equal("menuitem", item.GetAttribute("role"));
		Assert.Equal("-1", item.GetAttribute("tabindex"));
		Assert.Equal(Address, item.GetAttribute("href"));
		Assert.Equal("_blank", item.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", item.GetAttribute("rel"));
		Assert.False(item.HasAttribute("type"));
		Assert.Equal(", opens in a new tab", item.QuerySelector(".moka-visually-hidden")!.TextContent);
		IElement mark = item.QuerySelector(".moka-menubar-item__external")!;
		Assert.Equal("true", mark.GetAttribute("aria-hidden"));
		Assert.NotNull(mark.QuerySelector("svg"));
	}

	[Fact]
	public void TheMark_SitsAfterTheText_AndBeforeTheShortcut()
	{
		IElement item = Item(p => p
			.Add(x => x.Href, Address)
			.Add(x => x.Target, "_blank")
			.Add(x => x.Shortcut, "F1")).Find(".moka-menubar-item");

		Assert.Equal(
			["moka-menubar-item__icon-gap", "moka-menubar-item__text", "moka-visually-hidden", "moka-menubar-item__external", "moka-menubar-item__shortcut"],
			item.Children.Select(child => child.ClassList[0]));
	}

	[Fact]
	public void NewTabText_ReplacesTheWords() =>
		Assert.Equal(", öppnas i en ny flik", Item(p => p
			.Add(x => x.Href, Address)
			.Add(x => x.Target, "_blank")
			.Add(x => x.NewTabText, "öppnas i en ny flik")).Find(".moka-visually-hidden").TextContent);

	[Theory]
	[InlineData(null)]
	[InlineData("_self")]
	public void AnItemForThisTab_IsALink_WithNothingAdded(string? target)
	{
		IElement item = Item(p => p.Add(x => x.Href, "/docs").Add(x => x.Target, target)).Find(".moka-menubar-item");

		Assert.Equal("A", item.TagName);
		Assert.Equal("/docs", item.GetAttribute("href"));
		Assert.Equal(target, item.GetAttribute("target"));
		Assert.False(item.HasAttribute("rel"));
		Assert.Empty(item.QuerySelectorAll(".moka-visually-hidden, .moka-menubar-item__external"));
	}

	[Theory]
	[InlineData("javascript:alert(1)")]
	[InlineData("data:text/html,<script>alert(1)</script>")]
	[InlineData("")]
	public void AnUnsafeOrEmptyAddress_LeavesTheButton(string href)
	{
		IElement item = Item(p => p.Add(x => x.Href, href).Add(x => x.Target, "_blank")).Find(".moka-menubar-item");

		Assert.Equal("BUTTON", item.TagName);
		Assert.False(item.HasAttribute("href"));
		Assert.False(item.HasAttribute("target"));
	}

	[Fact]
	public void AnItemWithoutAnAddress_KeepsItsMarkup()
	{
		IElement item = Item(p => p.Add(x => x.Target, "_blank")).Find(".moka-menubar-item");

		Assert.Equal("BUTTON", item.TagName);
		Assert.Equal(["class", "type", "role", "tabindex"], item.Attributes.Select(a => a.Name)
			.Where(name => !name.StartsWith("b-", StringComparison.Ordinal) && !name.StartsWith("blazor:", StringComparison.Ordinal)));
		Assert.Equal(["moka-menubar-item__icon-gap", "moka-menubar-item__text"], item.Children.Select(child => child.ClassList[0]));
	}

	// A disabled item has nowhere to go, so it stays the disabled button, which the arrow keys pass over as before.
	[Fact]
	public void ADisabledItemWithAnAddress_StaysTheDisabledButton()
	{
		IElement item = Item(p => p
			.Add(x => x.Href, Address)
			.Add(x => x.Target, "_blank")
			.Add(x => x.Disabled, true)).Find(".moka-menubar-item");

		Assert.Equal("BUTTON", item.TagName);
		Assert.Equal("menuitem", item.GetAttribute("role"));
		Assert.True(item.HasAttribute("disabled"));
		Assert.False(item.HasAttribute("href"));
		Assert.False(item.HasAttribute("target"));
		Assert.False(item.HasAttribute("rel"));
		Assert.Empty(item.QuerySelectorAll(".moka-visually-hidden, .moka-menubar-item__external"));
	}

	[Fact]
	public async Task AClick_IsTheLinksOwn_SoTheMenuClosesAndTheItemRuns()
	{
		List<string> log = [];
		IRenderedComponent<MokaMenuBar> bar = Bar(Address, "_blank", log);
		await Key(bar, "ArrowDown");

		await bar.Find("a.moka-menubar-item").ClickAsync(new MouseEventArgs());

		Assert.Equal(["ran"], log);
		Assert.Empty(bar.FindAll("[role=menu]"));
		Assert.Empty(_links.Invocations["openTab"]);
		Assert.Empty(Navigation.History);
	}

	[Theory]
	[InlineData("Enter")]
	[InlineData(" ")]
	public async Task TheKeyboard_OpensTheTab_BeforeTheMenuClosesAndTheItemRuns(string key)
	{
		List<string> log = [];
		IRenderedComponent<MokaMenuBar> bar = Bar(Address, "_blank", log);
		await Key(bar, "ArrowDown");

		await Key(bar, key);

		JSRuntimeInvocation open = Assert.Single(_links.Invocations["openTab"]);
		Assert.Equal([Address, "_blank"], open.Arguments);
		Assert.Equal(["ran after the tab"], log);
		Assert.Empty(bar.FindAll("[role=menu]"));
		Assert.Empty(Navigation.History);
	}

	[Fact]
	public async Task TheKeyboard_FollowsALinkForThisTab_AfterTheItemRuns()
	{
		List<string> log = [];
		Navigation.LocationChanged += (_, _) => log.Add("navigated");
		IRenderedComponent<MokaMenuBar> bar = Bar("docs/start", null, log);
		await Key(bar, "ArrowDown");

		await Key(bar, "Enter");

		Assert.Equal(["ran", "navigated"], log);
		Assert.Equal("http://localhost/docs/start", Navigation.Uri);
		Assert.Empty(_links.Invocations["openTab"]);
	}

	private IRenderedComponent<MokaMenuBarItem> Item(Action<ComponentParameterCollectionBuilder<MokaMenuBarItem>> parameters) =>
		Render<MokaMenuBarItem>(p =>
		{
			p.Add(x => x.Text, "Docs");
			parameters(p);
		});

	// The item's action notes whether the menu had opened the tab by the time it ran.
	private IRenderedComponent<MokaMenuBar> Bar(string href, string? target, List<string> log) =>
		Render<MokaMenuBar>(p => p.AddChildContent<MokaMenuBarMenu>(menu => menu
			.Add(x => x.Label, "Help")
			.Add(x => x.ChildContent, (RenderFragment)(builder =>
			{
				builder.OpenComponent<MokaMenuBarItem>(0);
				builder.AddComponentParameter(1, nameof(MokaMenuBarItem.Text), "Docs");
				builder.AddComponentParameter(2, nameof(MokaMenuBarItem.Href), href);
				builder.AddComponentParameter(3, nameof(MokaMenuBarItem.Target), target);
				builder.AddComponentParameter(4, nameof(MokaMenuBarItem.OnClick), EventCallback.Factory.Create(this, () =>
					log.Add(_links.Invocations["openTab"].Count == 1 ? "ran after the tab" : "ran")));
				builder.CloseComponent();
			}))));

	private static Task Key(IRenderedComponent<MokaMenuBar> bar, string key) =>
		bar.Find(".moka-menubar").KeyDownAsync(new KeyboardEventArgs { Key = key });
}
