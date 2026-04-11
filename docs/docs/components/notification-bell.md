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
| `OnNotificationClick` | `EventCallback<MokaNotificationBellItem>` | -- | Callback when a notification is clicked |
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
| `Id` | `Guid` | Unique identifier |
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
