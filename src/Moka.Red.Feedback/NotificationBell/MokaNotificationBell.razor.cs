using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Base;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Internal;
using Moka.Red.Feedback.Notification;
using Moka.Red.Feedback.Popover;
using Moka.Red.Feedback.Tooltip;

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
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	private readonly string _generatedId = $"moka-notification-bell-{Guid.NewGuid():N}";
	private bool _focusTriggerPending;
	private bool _isOpen;
	private ElementReference _panel;
	private ElementReference _root;
	private string? _shownPopup;
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

	/// <summary>
	///     How the list is drawn. Null takes the theme's choice (<see cref="MokaTheme.WithNotifications" />), which is
	///     <see cref="MokaNotificationLook.Standard" /> unless the app set one. Only the new design draws the looks: on
	///     <see cref="MokaTheme.Dark" /> and <see cref="MokaTheme.Light" /> nothing changes, even with this set. The
	///     items carry no severity, so under <see cref="MokaNotificationLook.Terminal" /> an unread one is tagged [NEW]
	///     and a read one [---].
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
	///     The header's count, from <see cref="UnreadCount" />, in the looks that show one
	///     (<see cref="MokaNotificationLook.Inbox" />, <see cref="MokaNotificationLook.Grouped" /> and
	///     <see cref="MokaNotificationLook.Terminal" />). Default "2 new".
	/// </summary>
	[Parameter]
	public Func<int, string>? NewCountText { get; set; }

	[Inject] private IServiceProvider Services { get; set; } = default!;

	[CascadingParameter] private MokaTheme? CascadedTheme { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-notification-bell";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-notification-bell--open", _isOpen)
		.AddClass($"moka-notification-bell--{MokaEnumHelpers.ToCssClass(Size)}")
		.AddClass(MokaNotificationLooks.ModifierClass(RootClass, ShownLook))
		.AddClass(Class)
		.Build();

	private MokaNotificationLook ShownLook => MokaNotificationLooks.Resolve(Look, CascadedTheme);

	private TimeProvider Clock => Services.GetService<TimeProvider>() ?? TimeProvider.System;

	private string NewCount => NewCountText?.Invoke(UnreadCount) ?? MokaNotificationLooks.DefaultNewCount(UnreadCount);

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

	// The panel opens on the side the bell has room on, and so does the bell's name.
	private MokaTooltipPosition TriggerTooltipPosition => Position switch
	{
		MokaPopoverPosition.Top => MokaTooltipPosition.Top,
		MokaPopoverPosition.TopStart => MokaTooltipPosition.TopStart,
		MokaPopoverPosition.TopEnd => MokaTooltipPosition.TopEnd,
		MokaPopoverPosition.Bottom => MokaTooltipPosition.Bottom,
		MokaPopoverPosition.BottomStart => MokaTooltipPosition.BottomStart,
		MokaPopoverPosition.Left => MokaTooltipPosition.Left,
		MokaPopoverPosition.Right => MokaTooltipPosition.Right,
		_ => MokaTooltipPosition.BottomEnd
	};

	private bool Clickable => OnNotificationClick.HasDelegate;
	private string PanelId => $"{Id ?? _generatedId}-panel";
	private string TitleId => $"{PanelId}-title";
	private string UnreadId => $"{PanelId}-unread";

	// What moka-popup.js was last told: a render that changes none of it sends nothing.
	private string? PopupState => _isOpen ? Position.ToString() : null;

	/// <summary>Has internal open/close state.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "bindPopup", _root, _trigger);
		}

		// The panel goes to the top layer, out of reach of an ancestor's overflow and of later stacking contexts.
		string? popup = PopupState;
		if (popup != _shownPopup)
		{
			_shownPopup = popup;
			if (popup is null)
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", _generatedId);
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "open", _generatedId, _panel, _root, null,
					new Dictionary<string, object?> { ["placement"] = MokaEnumHelpers.ToCssClass(Position) });
			}
		}

		if (_focusTriggerPending)
		{
			_focusTriggerPending = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "focusElement", _trigger);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_shownPopup is not null)
		{
			_shownPopup = null;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _generatedId);
		}

		await base.DisposeAsyncCore();
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

	private IReadOnlyList<MokaNotificationGroup<MokaNotificationBellItem>> Groups(
		IReadOnlyList<MokaNotificationBellItem> visible, MokaNotificationLook look) =>
		MokaNotificationLooks.Group(visible, MokaUniqueKeys.For(visible, item => item.Id), look,
			item => item.Timestamp, Clock, TodayText, EarlierText);

	private string? GroupHeadingId(MokaNotificationGroup<MokaNotificationBellItem> group) =>
		group.Heading is null ? null : $"{PanelId}-{group.Key}";

	// Compact keeps the message for screen readers, which hear it with the rest of the notification.
	private static string MessageClass(MokaNotificationLook look) => new CssBuilder("moka-notification-bell__item-message")
		.AddClass("moka-visually-hidden", look == MokaNotificationLook.Compact)
		.Build();

	// The same width either way, so the titles line up.
	private static string Tag(MokaNotificationBellItem item) => item.Read ? "[---]" : "[NEW]";
}
