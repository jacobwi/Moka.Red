using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.ContextMenu.Extensions;
using static Moka.Red.ContextMenu.Tests.MenuDom;

namespace Moka.Red.ContextMenu.Tests.Components;

// An item had no address, so an app that opened a page from a right-click menu called window.open from OnClick: the
// tab opened from a round trip rather than the click, with no middle click, no "copy link" and no rel.
public class MokaContextMenuLinkTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.ContextMenu/MokaContextMenu.razor.js";
	private const string Address = "http://192.168.1.2:3000/";

	private readonly BunitJSModuleInterop _module;

	public MokaContextMenuLinkTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
	}

	private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	public void AnItemForANewTab_IsALinkMenuItem_ThatSaysSo()
	{
		IElement row = Row(Menu(new MokaContextMenuItem { Text = "Open", Href = Address, Target = "_blank" }), "Open");

		Assert.Equal("A", row.TagName);
		Assert.Equal("menuitem", row.GetAttribute("role"));
		Assert.Equal(Address, row.GetAttribute("href"));
		Assert.Equal("_blank", row.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", row.GetAttribute("rel"));
		Assert.Equal("-1", row.GetAttribute("tabindex"));
		Assert.Equal("true", row.QuerySelector(".moka-ctx-external")!.GetAttribute("aria-hidden"));
		Assert.Equal(", opens in a new tab", row.QuerySelector(".moka-visually-hidden")!.TextContent);
	}

	[Fact]
	public void AnItemForThisTab_IsALink_WithNothingAdded()
	{
		IElement row = Row(Menu(new MokaContextMenuItem { Text = "Details", Href = "apps/notes" }), "Details");

		Assert.Equal("A", row.TagName);
		Assert.Equal("apps/notes", row.GetAttribute("href"));
		Assert.False(row.HasAttribute("target"));
		Assert.False(row.HasAttribute("rel"));
		Assert.Empty(row.QuerySelectorAll(".moka-ctx-external, .moka-visually-hidden"));
	}

	[Fact]
	public void AnItemWithoutAnAddress_IsTheRowItWas()
	{
		IElement row = Row(Menu(new MokaContextMenuItem { Text = "Rename" }), "Rename");

		Assert.Equal("DIV", row.TagName);
		Assert.False(row.HasAttribute("href"));
		Assert.False(row.HasAttribute("tabindex"));
	}

	[Theory]
	[InlineData("javascript:alert(document.domain)")]
	[InlineData("data:text/html,<script>alert(1)</script>")]
	public void AScriptAddress_IsNeverALink(string href) =>
		Assert.Equal("DIV", Row(Menu(new MokaContextMenuItem { Text = "Open", Href = href, Target = "_blank" }), "Open").TagName);

	[Fact]
	public void AnItemWithASubmenu_IgnoresItsAddress()
	{
		IElement row = Row(Menu(new MokaContextMenuItem
		{
			Text = "Open in",
			Href = Address,
			Target = "_blank",
			Children = [new MokaContextMenuItem { Text = "This tab" }]
		}), "Open in");

		Assert.Equal("DIV", row.TagName);
		Assert.Equal("true", row.GetAttribute("aria-haspopup"));
	}

	[Fact]
	public void ADisabledLink_GoesNowhere()
	{
		IElement row = Row(Menu(new MokaContextMenuItem { Text = "Open", Href = Address, Target = "_blank", Disabled = true }), "Open");

		Assert.Equal("A", row.TagName);
		Assert.False(row.HasAttribute("href"));
		Assert.False(row.HasAttribute("target"));
		Assert.False(row.HasAttribute("rel"));
		Assert.Equal("true", row.GetAttribute("aria-disabled"));
	}

	[Theory]
	[InlineData("Enter")]
	[InlineData(" ")]
	public async Task TheKeyboard_OpensTheTab_BeforeTheItemRuns_AndClosesTheMenu(string key)
	{
		List<string> order = [];
		int closed = 0;
		IRenderedComponent<MokaContextMenu> cut = Render<MokaContextMenu>(p => p
			.Add(x => x.Items, [new MokaContextMenuItem
			{
				Text = "Open",
				Href = Address,
				Target = "_blank",
				OnClickSync = () => order.Add(_module.Invocations["openTab"].Count == 1 ? "after the tab" : "first")
			}])
			.Add(x => x.Visible, true)
			.Add(x => x.OnClose, () => closed++));

		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = key });

		JSRuntimeInvocation open = Assert.Single(_module.Invocations["openTab"]);
		Assert.Equal([Address, "_blank"], open.Arguments);
		Assert.Equal(["after the tab"], order);
		Assert.Equal(1, closed);
		Assert.Empty(Navigation.History);
	}

	[Fact]
	public async Task TheKeyboard_FollowsALinkForThisTab_AfterTheItemRuns()
	{
		List<string> order = [];
		Navigation.LocationChanged += (_, _) => order.Add("navigated");
		IRenderedComponent<MokaContextMenu> cut = Render<MokaContextMenu>(p => p
			.Add(x => x.Items, [new MokaContextMenuItem { Text = "Details", Href = "apps/notes", OnClickSync = () => order.Add("ran") }])
			.Add(x => x.Visible, true));

		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal(["ran", "navigated"], order);
		Assert.Equal("http://localhost/apps/notes", Navigation.Uri);
		Assert.Empty(_module.Invocations["openTab"]);
	}

	[Fact]
	public async Task AClick_IsTheLinksOwn_SoTheItemRunsAndNothingElseOpens()
	{
		bool ran = false;
		int closed = 0;
		IRenderedComponent<MokaContextMenu> cut = Render<MokaContextMenu>(p => p
			.Add(x => x.Items, [new MokaContextMenuItem { Text = "Open", Href = Address, Target = "_blank", OnClickSync = () => ran = true }])
			.Add(x => x.Visible, true)
			.Add(x => x.OnClose, () => closed++));

		await Row(cut.Find("[role=menu]"), "Open").ClickAsync(new MouseEventArgs());

		Assert.True(ran);
		Assert.Equal(1, closed);
		Assert.Empty(_module.Invocations["openTab"]);
		Assert.Empty(Navigation.History);
	}

	[Fact]
	public async Task TheHost_PassesItsWords()
	{
		Services.AddMokaContextMenu();
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>(p => p.Add(x => x.NewTabText, "ny flik"));
		IMokaContextMenuService service = Services.GetRequiredService<IMokaContextMenuService>();

		await cut.InvokeAsync(() => service.Show(10, 10, [new MokaContextMenuItem { Text = "Open", Href = Address, Target = "_blank" }]));

		Assert.Equal(", ny flik", cut.Find("a.moka-ctx-item .moka-visually-hidden").TextContent);
	}

	private IElement Menu(params MokaContextMenuItem[] items) =>
		Render<MokaContextMenu>(p => p
			.Add(x => x.Items, items)
			.Add(x => x.Visible, true)).Find("[role=menu]");
}
