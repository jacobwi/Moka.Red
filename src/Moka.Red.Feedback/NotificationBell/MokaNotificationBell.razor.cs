using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Internal;
using Moka.Red.Feedback.Popover;

namespace Moka.Red.Feedback.NotificationBell;

/// <summary>
///     Lightweight bell icon trigger with unread badge count and a dropdown notification list.
///     Unlike <see cref="Notification.MokaNotificationCenter" />, this is a standalone trigger
///     that works with a simple item list rather than a service.
/// </summary>
/// <remarks>
///     The bell is a disclosure button with <c>aria-expanded</c>, and the panel follows it, so Tab
///     moves into the panel. With <see cref="OnNotificationClick" /> each notification is a button.
///     Escape closes the panel and returns focus to the bell, and the panel closes when focus
///     leaves the component.
/// </remarks>
public partial class MokaNotificationBell : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Feedback/NotificationBell/MokaNotificationBell.razor.js";

	private readonly string _generatedId = $"moka-notification-bell-{Guid.NewGuid():N}";
	private bool _focusTriggerPending;
	private bool _isOpen;
	private ElementReference _root;
	private ElementReference _trigger;

	/// <summary>
	///     Notifications to display in the dropdown. Items are keyed by <see cref="MokaNotificationBellItem.Id" />;
	///     items that share an id render unkeyed.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaNotificationBellItem>? Notifications { get; set; }

	/// <summary>Number of unread notifications to show on the badge. 0 hides the badge.</summary>
	[Parameter]
	public int UnreadCount { get; set; }

	/// <summary>
	///     Raised when a notification item is clicked, or activated with Enter or Space. Without a
	///     handler the items are plain text and take no focus.
	/// </summary>
	[Parameter]
	public EventCallback<MokaNotificationBellItem> OnNotificationClick { get; set; }

	/// <summary>Raised when the "Mark all read" action is clicked.</summary>
	[Parameter]
	public EventCallback OnMarkAllRead { get; set; }

	/// <summary>Raised when the "Clear" action is clicked.</summary>
	[Parameter]
	public EventCallback OnClear { get; set; }

	/// <summary>Maximum number of notifications visible in the dropdown before scrolling.</summary>
	[Parameter]
	public int MaxVisible { get; set; } = 5;

	/// <summary>Position of the dropdown relative to the bell icon.</summary>
	[Parameter]
	public MokaPopoverPosition Position { get; set; } = MokaPopoverPosition.BottomEnd;

	/// <inheritdoc />
	protected override string RootClass => "moka-notification-bell";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-notification-bell--open", _isOpen)
		.AddClass($"moka-notification-bell--{MokaEnumHelpers.ToCssClass(Size)}")
		.AddClass(Class)
		.Build();

	private string DropdownClass => new CssBuilder("moka-notification-bell__dropdown")
		.AddClass($"moka-notification-bell__dropdown--{MokaEnumHelpers.ToCssClass(Position)}")
		.Build();

	// The root only wraps the bell button, so the padding and radius go to the panel it opens,
	// the box this component draws. The margin stays on the root.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle(Style)
		.Build();

	private string? DropdownStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private IReadOnlyList<MokaNotificationBellItem> VisibleNotifications =>
		Notifications is null
			? []
			: Notifications.Count <= MaxVisible
				? Notifications
				: Notifications.Take(MaxVisible).ToList();

	private int RemainingCount =>
		Notifications is null ? 0 : Math.Max(0, Notifications.Count - MaxVisible);

	private string BadgeText => UnreadCount > 99 ? "99+" : UnreadCount.ToString(CultureInfo.InvariantCulture);

	// The badge sits inside the button, whose aria-label would otherwise hide the count.
	private string TriggerLabel => UnreadCount > 0 ? $"Notifications, {BadgeText} unread" : "Notifications";

	private bool Clickable => OnNotificationClick.HasDelegate;
	private string PanelId => $"{Id ?? _generatedId}-panel";
	private string TitleId => $"{PanelId}-title";
	private string UnreadId => $"{PanelId}-unread";

	/// <summary>Has internal open/close state.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "bindPopup", _root, _trigger);
		}

		if (_focusTriggerPending)
		{
			_focusTriggerPending = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "focusElement", _trigger);
		}
	}

	private void ToggleDropdown() => _isOpen = !_isOpen;

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

	private async Task HandleItemClick(MokaNotificationBellItem item) => await OnNotificationClick.InvokeAsync(item);

	private async Task HandleMarkAllRead() => await OnMarkAllRead.InvokeAsync();

	private async Task HandleClear()
	{
		await OnClear.InvokeAsync();
		_isOpen = false;

		// The Clear button had focus and is gone with the panel.
		_focusTriggerPending = true;
	}

	private static string FormatTime(DateTime timestamp) => MokaFeedbackFormat.RelativeTime(timestamp);

	private static string ItemCss(MokaNotificationBellItem item) => new CssBuilder("moka-notification-bell__item")
		.AddClass("moka-notification-bell__item--unread", !item.Read)
		.Build();
}
