using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Feedback.NotificationBell;

namespace Moka.Red.Feedback.Tests.Components;

// Closing the panel when focus leaves the component lives in MokaNotificationBell.razor.js. These
// tests cover the markup, Escape, where focus goes on close, and the item keys.
public class MokaNotificationBellTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Feedback/NotificationBell/MokaNotificationBell.razor.js";

	private static readonly MokaNotificationBellItem Build =
		new(Guid.NewGuid(), "Build succeeded", "Pipeline #42 completed.", DateTime.UtcNow);

	private static readonly MokaNotificationBellItem Comment =
		new(Guid.NewGuid(), "New comment", "Alice replied.", DateTime.UtcNow, Read: true);

	private readonly BunitJSModuleInterop _module;

	public MokaNotificationBellTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
	}

	[Fact]
	public async Task TheBell_IsADisclosureButton()
	{
		IRenderedComponent<MokaNotificationBell> cut = RenderBell();

		IElement bell = cut.Find(".moka-notification-bell__trigger");
		Assert.Equal("button", bell.GetAttribute("type"));
		Assert.Equal("false", bell.GetAttribute("aria-expanded"));
		Assert.False(bell.HasAttribute("aria-controls"));
		Assert.Equal("Notifications, 1 unread", bell.GetAttribute("aria-label"));

		await bell.ClickAsync(new MouseEventArgs());

		bell = cut.Find(".moka-notification-bell__trigger");
		IElement panel = cut.Find(".moka-notification-bell__dropdown");
		Assert.Equal("true", bell.GetAttribute("aria-expanded"));
		Assert.Equal(panel.Id, bell.GetAttribute("aria-controls"));
		Assert.Equal("Notifications", cut.Find($"[id='{panel.GetAttribute("aria-labelledby")}']").TextContent);
	}

	[Fact]
	public async Task NotificationsWithAHandler_AreButtons()
	{
		MokaNotificationBellItem? clicked = null;
		IRenderedComponent<MokaNotificationBell> cut = RenderBell(p => p
			.Add(x => x.OnNotificationClick, item => clicked = item));
		await cut.Find(".moka-notification-bell__trigger").ClickAsync(new MouseEventArgs());

		IReadOnlyList<IElement> buttons = cut.FindAll("[role=listitem] > button.moka-notification-bell__item-main");
		Assert.Equal(2, buttons.Count);
		Assert.Equal("Unread", Description(cut, buttons[0]));
		Assert.False(buttons[1].HasAttribute("aria-describedby"));

		await buttons[0].ClickAsync(new MouseEventArgs());

		Assert.Equal(Build, clicked);
	}

	[Fact]
	public async Task NotificationsWithoutAHandler_TakeNoFocus()
	{
		IRenderedComponent<MokaNotificationBell> cut = RenderBell();
		await cut.Find(".moka-notification-bell__trigger").ClickAsync(new MouseEventArgs());

		Assert.Equal(2, cut.FindAll("[role=listitem]").Count);
		Assert.Empty(cut.FindAll(".moka-notification-bell__list button"));
	}

	[Fact]
	public async Task Escape_ClosesThePanel_AndReturnsFocusToTheBell()
	{
		IRenderedComponent<MokaNotificationBell> cut = RenderBell();
		await cut.Find(".moka-notification-bell__trigger").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-notification-bell__action").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(cut.FindAll(".moka-notification-bell__dropdown"));
		AssertFocusWentToTheBell(cut);
	}

	// The dialog's Blazor handler also got the Escape that closed the panel, so both closed.
	[Fact]
	public async Task Escape_InsideADialog_ClosesOnlyThePanel()
	{
		bool? dialogChanged = null;
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.OpenChanged, open => dialogChanged = open)
			.AddChildContent<MokaNotificationBell>(bell => bell
				.Add(x => x.Notifications, [Build, Comment])));
		await cut.Find(".moka-notification-bell__trigger").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-notification-bell__action").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(cut.FindAll(".moka-notification-bell__dropdown"));
		Assert.Null(dialogChanged);
	}

	// The Clear button had focus and went with the panel, so focus fell to the page.
	[Fact]
	public async Task Clear_ReturnsFocusToTheBell()
	{
		bool cleared = false;
		IRenderedComponent<MokaNotificationBell> cut = RenderBell(p => p.Add(x => x.OnClear, () => cleared = true));
		await cut.Find(".moka-notification-bell__trigger").ClickAsync(new MouseEventArgs());

		await cut.FindAll(".moka-notification-bell__action")[1].ClickAsync(new MouseEventArgs());

		Assert.True(cleared);
		Assert.Empty(cut.FindAll(".moka-notification-bell__dropdown"));
		AssertFocusWentToTheBell(cut);
	}

	[Fact]
	public void TheBell_BindsThePopupScriptOnce()
	{
		IRenderedComponent<MokaNotificationBell> cut = RenderBell();

		JSRuntimeInvocation bind = _module.VerifyInvoke("bindPopup");
		ElementReference root = Assert.IsType<ElementReference>(bind.Arguments[0]);
		ElementReference trigger = Assert.IsType<ElementReference>(bind.Arguments[1]);
		Assert.Equal(root.Id, cut.Find(".moka-notification-bell").GetAttribute("blazor:elementReference"));
		Assert.Equal(trigger.Id, cut.Find(".moka-notification-bell__trigger").GetAttribute("blazor:elementReference"));
	}

	// Ids come from the consumer, and Blazor throws on the render after two siblings share a key.
	[Fact]
	public async Task RepeatedIds_SurviveARerender()
	{
		MokaNotificationBellItem twin = Build with { Title = "Build succeeded again" };
		IRenderedComponent<MokaNotificationBell> cut = RenderBell(items: [Build, twin, Comment]);
		await cut.Find(".moka-notification-bell__trigger").ClickAsync(new MouseEventArgs());

		cut.Render(p => p.Add(x => x.Notifications, [Build, twin, Comment]));

		Assert.Equal(["Build succeeded", "Build succeeded again", "New comment"],
			cut.FindAll(".moka-notification-bell__item-title").Select(e => e.TextContent));
	}

	private void AssertFocusWentToTheBell(IRenderedComponent<MokaNotificationBell> cut)
	{
		ElementReference focused = Assert.IsType<ElementReference>(_module.VerifyInvoke("focusElement").Arguments[0]);
		ElementReference bind = Assert.IsType<ElementReference>(_module.VerifyInvoke("bindPopup").Arguments[1]);
		Assert.Equal(bind.Id, focused.Id);
		Assert.NotNull(cut.Find(".moka-notification-bell__trigger"));
	}

	private static string? Description(IRenderedComponent<MokaNotificationBell> cut, IElement element)
	{
		string? id = element.GetAttribute("aria-describedby");
		return id is null ? null : cut.Find($"[id='{id}']").TextContent;
	}

	private IRenderedComponent<MokaNotificationBell> RenderBell(
		Action<ComponentParameterCollectionBuilder<MokaNotificationBell>>? configure = null,
		IReadOnlyList<MokaNotificationBellItem>? items = null) =>
		Render<MokaNotificationBell>(p =>
		{
			p.Add(x => x.Notifications, items ?? [Build, Comment]).Add(x => x.UnreadCount, 1);
			configure?.Invoke(p);
		});
}
