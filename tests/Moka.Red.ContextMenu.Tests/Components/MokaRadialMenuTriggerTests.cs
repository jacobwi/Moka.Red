using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.ContextMenu.Extensions;
using static Moka.Red.ContextMenu.Tests.RadialDom;

namespace Moka.Red.ContextMenu.Tests.Components;

public class MokaRadialMenuTriggerTests : BunitContext
{
	private const string TriggerModule = "./_content/Moka.Red.ContextMenu/MokaRadialMenuTrigger.razor.js";

	private readonly BunitJSModuleInterop _ring;
	private readonly BunitJSModuleInterop _trigger;

	public MokaRadialMenuTriggerTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_trigger = JSInterop.SetupModule(TriggerModule);
		_ring = JSInterop.SetupModule(Module);
	}

	[Fact]
	public void TheWrapper_TakesNoRoom_AndListensForLongPresses()
	{
		IRenderedComponent<MokaRadialMenuTrigger> cut = RenderTrigger();

		IElement wrapper = cut.Find(".moka-radial-menu-trigger");
		Assert.Equal("Desktop", wrapper.TextContent.Trim());
		JSRuntimeInvocation bind = _trigger.VerifyInvoke("bindLongPress");
		Assert.IsType<ElementReference>(bind.Arguments[0]);
	}

	[Fact]
	public async Task ARightClick_OpensTheRingAtThePointer_InsteadOfTheBrowsersMenu()
	{
		IRenderedComponent<MokaRadialMenuTrigger> cut = RenderTrigger();
		IElement wrapper = cut.Find(".moka-radial-menu-trigger");
		Assert.True(wrapper.HasAttribute("blazor:oncontextmenu:preventDefault"));

		await wrapper.ContextMenuAsync(new MouseEventArgs { ClientX = 120, ClientY = 80, Button = 2, Detail = 1 });

		Assert.Equal("--moka-radial-x: 120px; --moka-radial-y: 80px", Ring(cut).GetAttribute("style"));
		Assert.Null(Lit(cut));
		Assert.False(Assert.IsType<bool>(_ring.VerifyInvoke("bind").Arguments[2]));
	}

	[Fact]
	public async Task APressStillHeld_OpensTheRingForAFlick()
	{
		IRenderedComponent<MokaRadialMenuTrigger> cut = RenderTrigger();

		await cut.Find(".moka-radial-menu-trigger")
			.ContextMenuAsync(new MouseEventArgs { ClientX = 50, ClientY = 60, Button = 2, Buttons = 2, Detail = 1 });

		Assert.True(Assert.IsType<bool>(_ring.VerifyInvoke("bind").Arguments[2]));
	}

	[Fact]
	public async Task TheContextMenuKey_CentresTheRingOnTheFocusedElement_OnItsFirstButton()
	{
		_ring.Setup<MokaRadialMenu.FocusCenter?>("measureFocusCenter")
			.SetResult(new MokaRadialMenu.FocusCenter { X = 400, Y = 200 });
		IRenderedComponent<MokaRadialMenuTrigger> cut = RenderTrigger();

		await cut.Find(".moka-radial-menu-trigger").ContextMenuAsync(new MouseEventArgs());

		cut.WaitForAssertion(() =>
			Assert.Equal("--moka-radial-x: 400px; --moka-radial-y: 200px", Ring(cut).GetAttribute("style")),
			TimeSpan.FromSeconds(5));
		Assert.Equal("Open", Lit(cut));
	}

	[Fact]
	public async Task Disabled_LeavesTheBrowsersMenu()
	{
		IRenderedComponent<MokaRadialMenuTrigger> cut = RenderTrigger(disabled: true);
		IElement wrapper = cut.Find(".moka-radial-menu-trigger");
		Assert.False(wrapper.HasAttribute("blazor:oncontextmenu:preventDefault"));

		await wrapper.ContextMenuAsync(new MouseEventArgs { Button = 2, Detail = 1 });

		Assert.Empty(cut.FindAll(".moka-radial-menu"));
	}

	[Fact]
	public void WithoutASpec_LeavesTheBrowsersMenu()
	{
		IRenderedComponent<MokaRadialMenuTrigger> cut = Render<MokaRadialMenuTrigger>(p => p
			.Add(x => x.ChildContent, (RenderFragment)(b => b.AddContent(0, "Desktop"))));

		Assert.False(cut.Find(".moka-radial-menu-trigger").HasAttribute("blazor:oncontextmenu:preventDefault"));
	}

	[Fact]
	public async Task ItsOwnRing_ClosesAfterAPick_AndRunsTheSpec()
	{
		List<string> picked = [];
		IRenderedComponent<MokaRadialMenuTrigger> cut = RenderTrigger(spec: Device(pick =>
		{
			picked.Add(pick.Answer);
			return Task.CompletedTask;
		}));
		await cut.Find(".moka-radial-menu-trigger").ContextMenuAsync(new MouseEventArgs { Button = 2, Detail = 1 });

		await Button(cut, "Rename").ClickAsync(new MouseEventArgs());

		Assert.Equal(["rename"], picked);
		Assert.Empty(cut.FindAll(".moka-radial-menu"));
	}

	[Fact]
	public async Task WithAHost_OpensTheSharedRing_AndDrawsNoneOfItsOwn()
	{
		Services.AddMokaContextMenu();
		IMokaContextMenuService service = Services.GetRequiredService<IMokaContextMenuService>();
		service.RegisterHost();
		MokaRadialMenuSpec spec = Device();
		IRenderedComponent<MokaRadialMenuTrigger> cut = RenderTrigger(spec: spec);

		await cut.Find(".moka-radial-menu-trigger")
			.ContextMenuAsync(new MouseEventArgs { ClientX = 70, ClientY = 90, Button = 2, Detail = 1 });

		Assert.Empty(cut.FindAll(".moka-radial-menu"));
		MokaRadialMenuRequest request = Assert.IsType<MokaRadialMenuRequest>(service.Radial);
		Assert.Same(spec, request.Spec);
		Assert.Equal(70, request.X);
		Assert.Equal(90, request.Y);
		Assert.False(request.OpenedFromKeyboard);
	}

	private IRenderedComponent<MokaRadialMenuTrigger> RenderTrigger(MokaRadialMenuSpec? spec = null, bool disabled = false) =>
		Render<MokaRadialMenuTrigger>(p => p
			.Add(x => x.Spec, spec ?? Device())
			.Add(x => x.Disabled, disabled)
			.Add(x => x.ChildContent, (RenderFragment)(b => b.AddContent(0, "Desktop"))));
}
