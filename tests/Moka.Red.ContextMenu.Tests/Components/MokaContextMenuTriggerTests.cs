using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.ContextMenu.Extensions;
using static Moka.Red.ContextMenu.Tests.MenuDom;

namespace Moka.Red.ContextMenu.Tests.Components;

public class MokaContextMenuTriggerTests : BunitContext
{
	private const string TriggerModule = "./_content/Moka.Red.ContextMenu/MokaContextMenuTrigger.razor.js";
	private const string MenuModule = "./_content/Moka.Red.ContextMenu/MokaContextMenu.razor.js";

	private static readonly MokaContextMenuItem[] Items = [new() { Text = "New file" }, new() { Text = "New folder" }];

	// JS interop reads results with the web defaults, so this is how a box from the module arrives.
	private static readonly JsonSerializerOptions Interop = new(JsonSerializerDefaults.Web);

	private readonly BunitJSModuleInterop _module;

	public MokaContextMenuTriggerTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(TriggerModule);
	}

	[Fact]
	public async Task RightClick_OpensTheMenuAtThePointer_InsteadOfTheBrowsersMenu()
	{
		IRenderedComponent<MokaContextMenuTrigger> cut = RenderTrigger(MokaContextMenuTriggerType.RightClick);
		IElement wrapper = cut.Find(".moka-ctx-trigger");
		Assert.True(wrapper.HasAttribute("blazor:oncontextmenu:preventDefault"));

		await wrapper.ContextMenuAsync(new MouseEventArgs { ClientX = 120, ClientY = 80, Button = 2 });

		IElement menu = cut.Find("[role=menu]");
		AssertAt(menu, 120, 80);
		Assert.Equal(["New file", "New folder"], Rows(menu).Select(Text));
	}

	[Fact]
	public async Task ARightClickTrigger_IgnoresLeftClicks()
	{
		IRenderedComponent<MokaContextMenuTrigger> cut = RenderTrigger(MokaContextMenuTriggerType.RightClick);

		await cut.Find(".moka-ctx-trigger").ClickAsync(new MouseEventArgs { Detail = 1, ClientX = 30, ClientY = 45 });

		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public async Task ALeftClickTrigger_IgnoresRightClicks_AndLeavesTheBrowsersMenuAlone()
	{
		IRenderedComponent<MokaContextMenuTrigger> cut = RenderTrigger(MokaContextMenuTriggerType.LeftClick);
		IElement wrapper = cut.Find(".moka-ctx-trigger");
		Assert.False(wrapper.HasAttribute("blazor:oncontextmenu:preventDefault"));

		await wrapper.ContextMenuAsync(new MouseEventArgs { ClientX = 120, ClientY = 80, Button = 2 });

		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	[Theory]
	[InlineData(MokaContextMenuTriggerType.LeftClick)]
	[InlineData(MokaContextMenuTriggerType.Both)]
	public async Task LeftClick_OpensTheMenuAtThePointer(MokaContextMenuTriggerType trigger)
	{
		IRenderedComponent<MokaContextMenuTrigger> cut = RenderTrigger(trigger);

		await cut.Find(".moka-ctx-trigger").ClickAsync(new MouseEventArgs { Detail = 1, ClientX = 30, ClientY = 45 });

		AssertAt(cut.Find("[role=menu]"), 30, 45);
		_module.VerifyNotInvoke("measureContent");
	}

	// Enter or Space on a button fires a click with Detail 0 and a 0,0 position, so the menu used
	// to open in the top-left corner of the viewport.
	[Theory]
	[InlineData(MokaContextMenuTriggerType.LeftClick)]
	[InlineData(MokaContextMenuTriggerType.Both)]
	public async Task AKeyboardClick_OpensTheMenuUnderTheContent(MokaContextMenuTriggerType trigger)
	{
		SetContentBox(40, 30, 60);
		IRenderedComponent<MokaContextMenuTrigger> cut = RenderTrigger(trigger);
		// bUnit writes an element's reference id only on the render that creates it.
		string? wrapperId = cut.Find(".moka-ctx-trigger").GetAttribute("blazor:elementReference");

		await cut.Find(".moka-ctx-trigger").ClickAsync(new MouseEventArgs { Detail = 0 });

		AssertAt(cut.Find("[role=menu]"), 40, 64);
		ElementReference measured = Assert.IsType<ElementReference>(_module.VerifyInvoke("measureContent").Arguments[0]);
		Assert.Equal(wrapperId, measured.Id);
	}

	[Fact]
	public async Task AKeyboardClick_WithNothingToMeasure_UsesTheEventsPosition()
	{
		IRenderedComponent<MokaContextMenuTrigger> cut = RenderTrigger(MokaContextMenuTriggerType.LeftClick);

		await cut.Find(".moka-ctx-trigger").ClickAsync(new MouseEventArgs { Detail = 0 });

		AssertAt(cut.Find("[role=menu]"), 0, 0);
		_module.VerifyInvoke("measureContent");
	}

	[Fact]
	public async Task WithAHost_TheTriggerOpensTheSharedMenu()
	{
		Services.AddMokaContextMenu();
		IRenderedComponent<MokaContextMenuHost> host = Render<MokaContextMenuHost>();
		IRenderedComponent<MokaContextMenuTrigger> cut = RenderTrigger(MokaContextMenuTriggerType.RightClick);

		await cut.Find(".moka-ctx-trigger").ContextMenuAsync(new MouseEventArgs { ClientX = 120, ClientY = 80 });

		IMokaContextMenuService service = Services.GetRequiredService<IMokaContextMenuService>();
		Assert.True(service.Visible);
		Assert.Same(Items, service.Items);
		Assert.Empty(cut.FindAll("[role=menu]"));
		host.WaitForAssertion(() => AssertAt(host.Find("[role=menu]"), 120, 80));
	}

	[Fact]
	public async Task WithAHost_AKeyboardClickOpensTheSharedMenuUnderTheContent()
	{
		Services.AddMokaContextMenu();
		SetContentBox(40, 30, 60);
		Render<MokaContextMenuHost>();
		IRenderedComponent<MokaContextMenuTrigger> cut = RenderTrigger(MokaContextMenuTriggerType.LeftClick);

		await cut.Find(".moka-ctx-trigger").ClickAsync(new MouseEventArgs { Detail = 0 });

		IMokaContextMenuService service = Services.GetRequiredService<IMokaContextMenuService>();
		Assert.True(service.Visible);
		Assert.Equal(40, service.X);
		Assert.Equal(64, service.Y);
	}

	// Enter used to do nothing after a keyboard open, because nothing was highlighted.
	[Fact]
	public async Task AKeyboardClick_OpensOnTheFirstEnabledItem_SoEnterChoosesIt()
	{
		string? chosen = null;
		IRenderedComponent<MokaContextMenuTrigger> cut = Render<MokaContextMenuTrigger>(p => p
			.Add(x => x.Items,
			[
				new MokaContextMenuItem { Text = "Undo", Disabled = true },
				new MokaContextMenuItem { Text = "New file", OnClickSync = () => chosen = "New file" }
			])
			.Add(x => x.Trigger, MokaContextMenuTriggerType.LeftClick)
			.AddChildContent("<button type=\"button\">Actions</button>"));

		await cut.Find(".moka-ctx-trigger").ClickAsync(new MouseEventArgs { Detail = 0 });

		Assert.Equal("New file", Highlighted(cut.Find("[role=menu]")));

		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal("New file", chosen);
		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	// The context-menu key or Shift+F10 on something focused inside the trigger: no button.
	[Fact]
	public async Task TheContextMenuKey_OpensOnTheFirstItem()
	{
		IRenderedComponent<MokaContextMenuTrigger> cut = RenderTrigger(MokaContextMenuTriggerType.RightClick);

		await cut.Find(".moka-ctx-trigger").ContextMenuAsync(new MouseEventArgs { Type = "contextmenu", ClientX = 60, ClientY = 20 });

		Assert.Equal("New file", Highlighted(cut.Find("[role=menu]")));
	}

	[Fact]
	public async Task PointerOpens_HighlightNothing()
	{
		IRenderedComponent<MokaContextMenuTrigger> cut = RenderTrigger(MokaContextMenuTriggerType.Both);

		// A right click as Chromium reports it: no click count, the secondary button.
		await cut.Find(".moka-ctx-trigger").ContextMenuAsync(new MouseEventArgs { Type = "contextmenu", Detail = 0, Button = 2 });
		Assert.Null(Highlighted(cut.Find("[role=menu]")));
		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		await cut.Find(".moka-ctx-trigger").ClickAsync(new MouseEventArgs { Type = "click", Detail = 1 });
		Assert.Null(Highlighted(cut.Find("[role=menu]")));
	}

	[Fact]
	public async Task EachKeyboardOpen_StartsOnTheFirstItemAgain()
	{
		IRenderedComponent<MokaContextMenuTrigger> cut = RenderTrigger(MokaContextMenuTriggerType.LeftClick);
		await cut.Find(".moka-ctx-trigger").ClickAsync(new MouseEventArgs { Detail = 0 });
		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		Assert.Equal("New folder", Highlighted(cut.Find("[role=menu]")));
		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		await cut.Find(".moka-ctx-trigger").ClickAsync(new MouseEventArgs { Detail = 0 });

		Assert.Equal("New file", Highlighted(cut.Find("[role=menu]")));
	}

	// A menu under a button near the bottom of the viewport used to be pushed up over the button.
	[Fact]
	public async Task AKeyboardClickNearTheBottom_OpensTheMenuAboveTheContent()
	{
		SetContentBox(40, 500, 530);
		SetMenuSize(150, viewportHeight: 600);
		IRenderedComponent<MokaContextMenuTrigger> cut = RenderTrigger(MokaContextMenuTriggerType.LeftClick);

		await cut.Find(".moka-ctx-trigger").ClickAsync(new MouseEventArgs { Detail = 0 });

		// Below would be 534 to 684. Above, the menu keeps the same 4px gap to the content.
		AssertAt(cut.Find("[role=menu]"), 40, 346);
	}

	[Fact]
	public async Task WithAHost_AKeyboardClickNearTheBottom_OpensTheSharedMenuAboveTheContent()
	{
		Services.AddMokaContextMenu();
		SetContentBox(40, 500, 530);
		SetMenuSize(150, viewportHeight: 600);
		IRenderedComponent<MokaContextMenuHost> host = Render<MokaContextMenuHost>();
		IRenderedComponent<MokaContextMenuTrigger> cut = RenderTrigger(MokaContextMenuTriggerType.LeftClick);

		await cut.Find(".moka-ctx-trigger").ClickAsync(new MouseEventArgs { Detail = 0 });

		host.WaitForAssertion(() =>
		{
			IElement menu = host.Find("[role=menu]");
			AssertAt(menu, 40, 346);
			Assert.Equal("New file", Highlighted(menu));
		});
	}

	[Fact]
	public async Task Disabled_OpensNothing_AndLeavesTheBrowsersMenuAlone()
	{
		IRenderedComponent<MokaContextMenuTrigger> cut = RenderTrigger(MokaContextMenuTriggerType.Both, disabled: true);
		IElement wrapper = cut.Find(".moka-ctx-trigger");
		Assert.False(wrapper.HasAttribute("blazor:oncontextmenu:preventDefault"));

		await wrapper.ContextMenuAsync(new MouseEventArgs { ClientX = 120, ClientY = 80 });
		await cut.Find(".moka-ctx-trigger").ClickAsync(new MouseEventArgs { Detail = 1 });
		await cut.Find(".moka-ctx-trigger").ClickAsync(new MouseEventArgs { Detail = 0 });

		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public async Task ChoosingAnItem_ClosesTheTriggersOwnMenu()
	{
		bool renamed = false;
		IRenderedComponent<MokaContextMenuTrigger> cut = Render<MokaContextMenuTrigger>(p => p
			.Add(x => x.Items, [new MokaContextMenuItem { Text = "Rename", OnClickSync = () => renamed = true }])
			.AddChildContent("<span>report.pdf</span>"));

		await cut.Find(".moka-ctx-trigger").ContextMenuAsync(new MouseEventArgs { ClientX = 10, ClientY = 10 });
		await Row(cut.Find("[role=menu]"), "Rename").ClickAsync(new MouseEventArgs());

		Assert.True(renamed);
		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	private IRenderedComponent<MokaContextMenuTrigger> RenderTrigger(MokaContextMenuTriggerType trigger, bool disabled = false) =>
		Render<MokaContextMenuTrigger>(p => p
			.Add(x => x.Items, Items)
			.Add(x => x.Trigger, trigger)
			.Add(x => x.Disabled, disabled)
			.AddChildContent("<button type=\"button\">Actions</button>"));

	private void SetContentBox(double left, double top, double bottom) =>
		_module.Setup<MokaContextMenuTrigger.ContentBox?>("measureContent", _ => true)
			.SetResult(JsonSerializer.Deserialize<MokaContextMenuTrigger.ContentBox>(
				JsonSerializer.Serialize(new { left, top, bottom }), Interop));

	/// <summary>Makes every menu measure at this height, 200px wide, in a 1000px wide viewport.</summary>
	private void SetMenuSize(double height, double viewportHeight) =>
		JSInterop.SetupModule(MenuModule)
			.Setup<MokaContextMenu.MenuMetrics?>("measureMenu", _ => true)
			.SetResult(new MokaContextMenu.MenuMetrics
			{
				Width = 200,
				Height = height,
				ViewportWidth = 1000,
				ViewportHeight = viewportHeight
			});
}
