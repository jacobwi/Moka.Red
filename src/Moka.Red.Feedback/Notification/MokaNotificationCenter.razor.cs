using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Internal;

namespace Moka.Red.Feedback.Notification;

/// <summary>
///     Persistent notification panel with a bell icon button, unread count badge,
///     and a dropdown panel listing all notifications.
/// </summary>
/// <remarks>
///     The bell is a disclosure button with <c>aria-expanded</c>, and the panel follows it, so Tab
///     moves into the panel. Each notification is a button beside its dismiss button. Escape closes
///     the panel and returns focus to the bell, and the panel closes when focus leaves the component.
/// </remarks>
public partial class MokaNotificationCenter
{
	private const string ModulePath = "./_content/Moka.Red.Feedback/Notification/MokaNotificationCenter.razor.js";
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	private readonly string _generatedId = $"moka-notification-center-{Guid.NewGuid():N}";

	// The index of the notification whose dismiss button had focus, so focus can move to the one
	// that took its place.
	private int? _focusAfterRemove;
	private bool _focusTriggerPending;
	private bool _isOpen;
	private ElementReference _panel;
	private bool _panelShown;
	private ElementReference _root;
	private ElementReference _trigger;

	[Inject] private IMokaNotificationService NotificationService { get; set; } = default!;

	[Inject] private IServiceProvider Services { get; set; } = default!;

	[CascadingParameter] private MokaTheme? CascadedTheme { get; set; }

	/// <summary>
	///     How the list is drawn. Null takes the theme's choice (<see cref="MokaTheme.WithNotifications" />), which is
	///     <see cref="MokaNotificationLook.Standard" /> unless the app set one. Only the new design draws the looks: on
	///     <see cref="MokaTheme.Dark" /> and <see cref="MokaTheme.Light" /> nothing changes, even with this set.
	/// </summary>
	[Parameter]
	public MokaNotificationLook? Look { get; set; }

	/// <summary>
	///     The heading over the notifications from today, under <see cref="MokaNotificationLook.Grouped" />. Today is
	///     the local date of the registered <see cref="TimeProvider" />, else the system clock's.
	/// </summary>
	[Parameter]
	public string TodayText { get; set; } = "Today";

	/// <summary>The heading over the older notifications, under <see cref="MokaNotificationLook.Grouped" />.</summary>
	[Parameter]
	public string EarlierText { get; set; } = "Earlier";

	/// <summary>
	///     The header's count of unread notifications, from how many there are, in the looks that show one
	///     (<see cref="MokaNotificationLook.Inbox" />, <see cref="MokaNotificationLook.Grouped" /> and
	///     <see cref="MokaNotificationLook.Terminal" />). Default "2 new".
	/// </summary>
	[Parameter]
	public Func<int, string>? NewCountText { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-notification-center";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-notification-center--open", _isOpen)
		.AddClass(MokaNotificationLooks.ModifierClass(RootClass, ShownLook))
		.AddClass(Class)
		.Build();

	private MokaNotificationLook ShownLook => MokaNotificationLooks.Resolve(Look, CascadedTheme);

	private TimeProvider Clock => Services.GetService<TimeProvider>() ?? TimeProvider.System;

	private IReadOnlyList<MokaNotification> Notifications => NotificationService.Notifications;

	private int UnreadCount => NotificationService.UnreadCount;

	private string BadgeText => UnreadCount > 99 ? "99+" : UnreadCount.ToString(CultureInfo.InvariantCulture);

	// The badge sits inside the button, whose aria-label would otherwise hide the count.
	private string TriggerLabel => UnreadCount > 0 ? $"Notifications, {BadgeText} unread" : "Notifications";

	private string PanelId => $"{Id ?? _generatedId}-panel";
	private string TitleId => $"{PanelId}-title";
	private string UnreadId => $"{PanelId}-unread";

	private string NewCount => NewCountText?.Invoke(UnreadCount) ?? MokaNotificationLooks.DefaultNewCount(UnreadCount);

	/// <summary>Has internal open/close state.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized() => NotificationService.OnChanged += HandleChanged;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "bindPopup", _root, _trigger);
		}

		// The panel goes to the top layer, out of reach of an ancestor's overflow and of later stacking contexts.
		// Before any focus call: a panel not shown yet can't take focus.
		if (_panelShown != _isOpen)
		{
			_panelShown = _isOpen;
			if (_isOpen)
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "open", _generatedId, _panel, _root, null,
					new Dictionary<string, object?> { ["placement"] = "bottom-end" });
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", _generatedId);
			}
		}

		if (_focusTriggerPending)
		{
			_focusTriggerPending = false;
			_focusAfterRemove = null;
			await SafeModuleInvokeVoidAsync(ModulePath, "focusElement", _trigger);
		}
		else if (_focusAfterRemove is { } index)
		{
			_focusAfterRemove = null;
			await SafeModuleInvokeVoidAsync(ModulePath, "focusNth", _panel, ".moka-notification-item__main", index,
				_trigger);
		}
	}

	private void TogglePanel() => _isOpen = !_isOpen;

	private void HandleChanged() => _ = RefreshAsync();

	// The service raises OnChanged on the caller's thread. The render goes to the renderer's thread
	// and its task is observed, so a failure reaches the app's error handling instead of vanishing.
	private async Task RefreshAsync()
	{
		try
		{
			await InvokeAsync(StateHasChanged);
		}
		catch (ObjectDisposedException)
		{
			// The renderer went away between the event and the dispatch.
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			await DispatchExceptionAsync(ex);
		}
	}

	// A .NET handler on the root also gets keys from the buttons inside it, which is what Escape
	// wants: it closes the panel wherever focus is.
	private void HandleKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Escape" && _isOpen)
		{
			_isOpen = false;
			_focusTriggerPending = true;
		}
	}

	private void HandleMarkAllRead() => NotificationService.MarkAllAsRead();

	private void HandleClear()
	{
		NotificationService.Clear();
		_isOpen = false;

		// The Clear button had focus and is gone with the panel.
		_focusTriggerPending = true;
	}

	private void HandleRemove(Guid id, int index)
	{
		// The dismiss button goes with its notification, so focus moves to the one that takes its
		// place, or to the bell when none is left.
		_focusAfterRemove = index;
		NotificationService.Remove(id);
	}

	private void HandleItemClick(MokaNotification notification)
	{
		if (!notification.Read)
		{
			NotificationService.MarkAsRead(notification.Id);
		}

		notification.OnClick?.Invoke();
	}

	private static MokaIconDefinition GetIcon(MokaNotification notification)
		=> notification.Icon ?? MokaFeedbackFormat.SeverityIcon(notification.Severity);

	private IReadOnlyList<MokaNotificationGroup<MokaNotification>> Groups(
		IReadOnlyList<MokaNotification> notifications, MokaNotificationLook look) =>
		MokaNotificationLooks.Group(notifications, MokaUniqueKeys.For(notifications, n => n.Id), look,
			n => n.Timestamp, Clock, TodayText, EarlierText);

	private string? GroupHeadingId(MokaNotificationGroup<MokaNotification> group) =>
		group.Heading is null ? null : $"{PanelId}-{group.Key}";

	// Compact keeps the message for screen readers, which hear the whole notification as its button's name.
	private static string MessageClass(MokaNotificationLook look) => new CssBuilder("moka-notification-item__message")
		.AddClass("moka-visually-hidden", look == MokaNotificationLook.Compact)
		.Build();

	private static string FormatTime(DateTime timestamp) => MokaFeedbackFormat.RelativeTime(timestamp);

	private static string ItemCss(MokaNotification notification) => new CssBuilder("moka-notification-item")
		.AddClass("moka-notification-item--unread", !notification.Read)
		.AddClass($"moka-notification-item--{MokaEnumHelpers.ToCssClass(notification.Severity)}")
		.Build();

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		NotificationService.OnChanged -= HandleChanged;
		if (_panelShown)
		{
			_panelShown = false;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _generatedId);
		}

		await base.DisposeAsyncCore();
	}
}
