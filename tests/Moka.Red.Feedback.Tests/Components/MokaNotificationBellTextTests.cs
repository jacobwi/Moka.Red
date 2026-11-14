using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.NotificationBell;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

// Every word the bell writes comes from its Text, and Inline draws the list in place with no bell to open it.
public sealed class MokaNotificationBellTextTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Feedback/NotificationBell/MokaNotificationBell.razor.js";

	private readonly FakeClock _clock = new();
	private readonly BunitJSModuleInterop _module;

	public MokaNotificationBellTextTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		Services.AddSingleton<TimeProvider>(_clock);
	}

	private DateTime Now => _clock.GetUtcNow().UtcDateTime;

	private static MokaNotificationBellText Words => new()
	{
		Title = "Avisos",
		MarkAllRead = "Marcar leídos",
		Clear = "Borrar",
		Empty = "Sin avisos",
		Unread = "Sin leer",
		Today = "Hoy",
		Earlier = "Antes",
		NewCount = count => $"{count} nuevos",
		Bell = unread => $"Avisos ({unread})",
		More = count => $"y {count} más",
		Time = (_, _) => "hace poco",
		UnreadTag = "[NUE]",
		ReadTag = "[ - ]"
	};

	[Fact]
	public async Task EveryWord_ComesFromTheText()
	{
		IRenderedComponent<MokaNotificationBell> cut = Render<MokaNotificationBell>(p => p
			.Add(x => x.Notifications, [Item("Build", minutes: 5), Item("Upload", minutes: 30, read: true)])
			.Add(x => x.UnreadCount, 1)
			.Add(x => x.MaxVisible, 1)
			.Add(x => x.Text, Words));

		Assert.Equal("Avisos (1)", cut.Find(".moka-notification-bell__trigger").GetAttribute("aria-label"));

		await cut.Find(".moka-notification-bell__trigger").ClickAsync(new MouseEventArgs());

		Assert.Equal("Avisos", cut.Find(".moka-notification-bell__title").TextContent);
		Assert.Equal(["Marcar leídos", "Borrar"], cut.FindAll(".moka-notification-bell__action").Select(button => button.TextContent));
		Assert.Equal("Sin leer", cut.Find(".moka-notification-bell__list > span[hidden]").TextContent);
		Assert.Equal("hace poco", cut.Find(".moka-notification-bell__item-time").TextContent);
		Assert.Equal("y 1 más", cut.Find(".moka-notification-bell__more").TextContent);
	}

	[Fact]
	public async Task AnEmptyList_SaysTheTextsWord()
	{
		IRenderedComponent<MokaNotificationBell> cut = Render<MokaNotificationBell>(p => p
			.Add(x => x.Notifications, [])
			.Add(x => x.Text, Words));

		await cut.Find(".moka-notification-bell__trigger").ClickAsync(new MouseEventArgs());

		Assert.Equal("Sin avisos", cut.Find(".moka-notification-bell__empty").TextContent.Trim());
	}

	[Fact]
	public void TheGroupedHeadings_AndTheCount_ComeFromTheText()
	{
		IRenderedComponent<MokaNotificationBell> cut = Render<MokaNotificationBell>(p => p
			.Add(x => x.Inline, true)
			.Add(x => x.Look, MokaNotificationLook.Grouped)
			.Add(x => x.Notifications, [Item("Build", minutes: 5), Item("Upload", minutes: 60 * 72, read: true)])
			.Add(x => x.UnreadCount, 1)
			.Add(x => x.Text, Words));

		Assert.Equal(["Hoy", "Antes"], cut.FindAll(".moka-notification-bell__group").Select(group => group.TextContent));
		Assert.Equal("1 nuevos", cut.Find(".moka-notification-bell__count").TextContent);
	}

	[Fact]
	public void TheTerminalTags_ComeFromTheText()
	{
		IRenderedComponent<MokaNotificationBell> cut = Render<MokaNotificationBell>(p => p
			.Add(x => x.Inline, true)
			.Add(x => x.Look, MokaNotificationLook.Terminal)
			.Add(x => x.Notifications, [Item("Build", minutes: 5), Item("Upload", minutes: 30, read: true)])
			.Add(x => x.Text, Words));

		Assert.Equal(["[NUE]", "[ - ]"], cut.FindAll(".moka-notification-bell__item-tag").Select(tag => tag.TextContent));
	}

	// The age is measured from the registered clock, as the notification center's is.
	[Fact]
	public void TheAge_IsMeasuredFromTheRegisteredClock()
	{
		IRenderedComponent<MokaNotificationBell> cut = Render<MokaNotificationBell>(p => p
			.Add(x => x.Inline, true)
			.Add(x => x.Notifications, [Item("Build", minutes: 5)]));

		Assert.Equal("5m ago", cut.Find(".moka-notification-bell__item-time").TextContent);
	}

	[Fact]
	public void Inline_DrawsTheListInPlace_WithNoBell()
	{
		IRenderedComponent<MokaNotificationBell> cut = Render<MokaNotificationBell>(p => p
			.Add(x => x.Inline, true)
			.Add(x => x.Notifications, [Item("Build", minutes: 5)])
			.Add(x => x.UnreadCount, 1));

		IElement panel = cut.Find(".moka-notification-bell__dropdown");
		Assert.Contains("moka-notification-bell--inline", cut.Find(".moka-notification-bell").ClassName, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll(".moka-notification-bell__trigger"));
		Assert.False(panel.HasAttribute("popover"));
		Assert.False(panel.HasAttribute("tabindex"));
		Assert.Equal("group", panel.GetAttribute("role"));
		Assert.Equal("Notifications", cut.Find($"[id='{panel.GetAttribute("aria-labelledby")}']").TextContent);
		Assert.DoesNotContain(panel.ClassList, name => name.StartsWith("moka-notification-bell__dropdown--", StringComparison.Ordinal));
		Assert.DoesNotContain(_module.Invocations, call => call.Identifier == "bindPopup");
	}

	[Fact]
	public async Task Inline_StaysOpen_AfterClear()
	{
		bool cleared = false;
		IRenderedComponent<MokaNotificationBell> cut = Render<MokaNotificationBell>(p => p
			.Add(x => x.Inline, true)
			.Add(x => x.Notifications, [Item("Build", minutes: 5)])
			.Add(x => x.OnClear, () => cleared = true));

		await cut.FindAll(".moka-notification-bell__action").Single(button => button.TextContent == "Clear").ClickAsync(new MouseEventArgs());

		Assert.True(cleared);
		Assert.NotNull(cut.Find(".moka-notification-bell__dropdown"));
		Assert.DoesNotContain(_module.Invocations, call => call.Identifier == "focusElement");
	}

	// Leaving the inline form brings the bell back closed, and binds it to the root it now has.
	[Fact]
	public void LeavingInline_BringsTheBellBack_Closed_AndBound()
	{
		IRenderedComponent<MokaNotificationBell> cut = Render<MokaNotificationBell>(p => p
			.Add(x => x.Inline, true)
			.Add(x => x.Notifications, [Item("Build", minutes: 5)]));

		cut.Render(p => p.Add(x => x.Inline, false));

		Assert.Equal("false", cut.Find(".moka-notification-bell__trigger").GetAttribute("aria-expanded"));
		Assert.Empty(cut.FindAll(".moka-notification-bell__dropdown"));
		Assert.Single(_module.Invocations, call => call.Identifier == "bindPopup");
	}

	private MokaNotificationBellItem Item(string title, int minutes, bool read = false) =>
		new(Guid.NewGuid(), title, $"{title} message", Now.AddMinutes(-minutes), read);
}
