using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.Notification;

namespace Moka.Red.Feedback.Tests.Components;

// Closing the panel when focus leaves the component lives in the shared notification panel script.
// These tests cover the markup, Escape, where focus goes, and the item keys.
public class MokaNotificationCenterTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Feedback/Notification/MokaNotificationCenter.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaNotificationCenterTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		Services.AddMokaFeedback();
	}

	private IMokaNotificationService Notifications => Services.GetRequiredService<IMokaNotificationService>();

	[Fact]
	public async Task TheBell_IsADisclosureButton()
	{
		Notifications.Push("Build succeeded", "Pipeline #42 completed.");
		IRenderedComponent<MokaNotificationCenter> cut = Render<MokaNotificationCenter>();

		IElement bell = cut.Find("button.moka-notification-bell");
		Assert.Equal("button", bell.GetAttribute("type"));
		Assert.Equal("false", bell.GetAttribute("aria-expanded"));
		Assert.Equal("Notifications, 1 unread", bell.GetAttribute("aria-label"));

		await bell.ClickAsync(new MouseEventArgs());

		bell = cut.Find("button.moka-notification-bell");
		IElement panel = cut.Find(".moka-notification-panel");
		Assert.Equal("true", bell.GetAttribute("aria-expanded"));
		Assert.Equal(panel.Id, bell.GetAttribute("aria-controls"));
	}

	[Fact]
	public async Task EachNotification_IsAButton_BesideItsDismissButton()
	{
		bool clicked = false;
		Notifications.Push(new MokaNotification
		{
			Title = "Deploy started",
			Message = "Production deploy in progress.",
			OnClick = () => clicked = true
		});
		IRenderedComponent<MokaNotificationCenter> cut = await RenderOpenAsync();

		IElement row = cut.Find("[role=listitem]");
		IElement main = row.QuerySelector("button.moka-notification-item__main")!;
		Assert.Equal("Dismiss Deploy started", row.QuerySelector(".moka-notification-item__dismiss")!.GetAttribute("aria-label"));
		Assert.Equal("Unread", cut.Find($"[id='{main.GetAttribute("aria-describedby")}']").TextContent);

		await main.ClickAsync(new MouseEventArgs());

		Assert.True(clicked);
		Assert.Equal(0, Notifications.UnreadCount);
		Assert.False(cut.Find("button.moka-notification-item__main").HasAttribute("aria-describedby"));
	}

	[Fact]
	public async Task Escape_ClosesThePanel_AndReturnsFocusToTheBell()
	{
		Notifications.Push("Build succeeded", "Pipeline #42 completed.");
		IRenderedComponent<MokaNotificationCenter> cut = await RenderOpenAsync();

		await cut.Find("button.moka-notification-item__main").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(cut.FindAll(".moka-notification-panel"));
		ElementReference focused = Assert.IsType<ElementReference>(_module.VerifyInvoke("focusElement").Arguments[0]);
		Assert.Equal(BoundTrigger().Id, focused.Id);
	}

	// The dismiss button had focus and went with its notification, so focus fell to the page.
	[Fact]
	public async Task Dismissing_MovesFocusToTheNotificationThatTakesItsPlace()
	{
		Notifications.Push("First", "One.");
		Notifications.Push("Second", "Two.");
		IRenderedComponent<MokaNotificationCenter> cut = await RenderOpenAsync();

		await cut.FindAll(".moka-notification-item__dismiss")[0].ClickAsync(new MouseEventArgs());

		JSRuntimeInvocation focus = _module.VerifyInvoke("focusNth");
		Assert.Equal(".moka-notification-item__main", focus.Arguments[1]);
		Assert.Equal(0, focus.Arguments[2]);
		Assert.Equal(BoundTrigger().Id, Assert.IsType<ElementReference>(focus.Arguments[3]).Id);
		Assert.Single(cut.FindAll("[role=listitem]"));
	}

	// Ids can come from the consumer through Push(MokaNotification), and Blazor throws on the render
	// after two siblings share a key. The service now keeps one entry per id; the center still drops
	// repeated keys for services that do not.
	[Fact]
	public async Task APushedIdAgain_ReplacesItsEntry_AndSurvivesARerender()
	{
		var id = Guid.NewGuid();
		Notifications.Push(new MokaNotification { Id = id, Title = "One", Message = "First." });
		Notifications.Push(new MokaNotification { Id = id, Title = "Two", Message = "Second." });
		IRenderedComponent<MokaNotificationCenter> cut = await RenderOpenAsync();

		await cut.InvokeAsync(() => Notifications.Push("Three", "Third."));

		IReadOnlyList<IElement> items = cut.FindAll("[role=listitem]");
		Assert.Equal(2, items.Count);
		Assert.Contains(items, item => item.TextContent.Contains("Second.", StringComparison.Ordinal));
		Assert.DoesNotContain(items, item => item.TextContent.Contains("First.", StringComparison.Ordinal));
	}

	private ElementReference BoundTrigger() =>
		Assert.IsType<ElementReference>(_module.VerifyInvoke("bindPopup").Arguments[1]);

	private async Task<IRenderedComponent<MokaNotificationCenter>> RenderOpenAsync()
	{
		IRenderedComponent<MokaNotificationCenter> cut = Render<MokaNotificationCenter>();
		await cut.Find("button.moka-notification-bell").ClickAsync(new MouseEventArgs());
		return cut;
	}
}
