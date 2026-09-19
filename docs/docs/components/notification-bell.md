---
title: Notification Bell
description: Bell icon trigger with unread badge and dropdown notification list.
order: 77
---

# Notification Bell

`MokaNotificationBell` renders a bell icon with an unread count badge. Clicking it opens a dropdown panel listing recent notifications with mark-all-read and clear actions.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Notifications` | `IReadOnlyList<MokaNotificationBellItem>` | `[]` | List of notifications to display |
| `UnreadCount` | `int` | `0` | Number of unread notifications (shown as badge) |
| `OnNotificationClick` | `EventCallback<MokaNotificationBellItem>` | -- | Callback when a notification is clicked, or activated with Enter or Space. Without it the notifications are plain text that takes no focus |
| `OnMarkAllRead` | `EventCallback` | -- | Callback when "Mark all read" is clicked |
| `OnClear` | `EventCallback` | -- | Callback when "Clear all" is clicked |
| `MaxVisible` | `int` | `5` | Maximum notifications visible before scrolling |
| `Position` | `MokaPopoverPosition` | `BottomEnd` | Dropdown position relative to the bell |
| `Size` | `MokaSize` | `Md` | Bell icon size |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaNotificationBellItem

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier. Items that share an `Id` render unkeyed |
| `Title` | `string` | Notification title |
| `Message` | `string` | Notification body text |
| `Timestamp` | `DateTime` | When the notification was created |
| `Read` | `bool` | Whether the notification has been read |
| `Icon` | `MokaIconDefinition?` | Optional icon |

## Basic with Notifications

```blazor-preview
<MokaNotificationBell Notifications="@_notifications" UnreadCount="2" />

@code {
    private IReadOnlyList<MokaNotificationBellItem> _notifications = new[]
    {
        new MokaNotificationBellItem(Guid.NewGuid(), "Build succeeded", "Pipeline #42 completed.", DateTime.Now.AddMinutes(-5)),
        new MokaNotificationBellItem(Guid.NewGuid(), "New comment", "Alice replied to your review.", DateTime.Now.AddMinutes(-15), Read: true),
        new MokaNotificationBellItem(Guid.NewGuid(), "Deploy started", "Production deploy in progress.", DateTime.Now.AddHours(-1))
    };
}
```

## Empty State

```blazor-preview
<MokaNotificationBell Notifications="@Array.Empty<MokaNotificationBellItem>()" UnreadCount="0" />
```

## Keyboard and Screen Readers

The bell is a disclosure button: it reports `aria-expanded` as `"true"` or `"false"`, points `aria-controls` at the open panel, and its name includes `UnreadCount` ("Notifications, 2 unread"). The panel comes right after it, so Tab moves from the bell to Mark all read, Clear, and then the notifications.

- With `OnNotificationClick` each notification is a button, and Enter or Space raise the callback as a click does. An unread notification is described as "Unread".
- Escape closes the panel and returns focus to the bell. Inside a `MokaDialog` it closes only the panel.
- Clear closes the panel and returns focus to the bell.
- The panel closes when focus leaves it: Tab past the last notification, or a click anywhere else on the page.

```razor
<MokaNotificationBell Notifications="_items" UnreadCount="_unread"
                      OnNotificationClick="Open" OnMarkAllRead="MarkAllRead" />
```

Items are keyed by `Id`. Two items with the same `Id` render unkeyed rather than making Blazor throw on the next render, so they are matched by position.

Up to 0.1.12 a notification could only be clicked with the mouse, Escape did nothing, the panel stayed open until the bell was clicked again, and a repeated `Id` threw on the render after it appeared.
