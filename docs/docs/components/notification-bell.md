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
| `Notifications` | `IReadOnlyList<MokaBellNotification>` | `[]` | List of notifications to display |
| `UnreadCount` | `int` | `0` | Number of unread notifications (shown as badge) |
| `OnNotificationClick` | `EventCallback<MokaBellNotification>` | -- | Callback when a notification is clicked |
| `OnMarkAllRead` | `EventCallback` | -- | Callback when "Mark all read" is clicked |
| `OnClear` | `EventCallback` | -- | Callback when "Clear all" is clicked |
| `MaxVisible` | `int` | `5` | Maximum notifications visible before scrolling |
| `Position` | `MokaPopoverPosition` | `BottomEnd` | Dropdown position relative to the bell |
| `Size` | `MokaSize` | `Md` | Bell icon size |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaBellNotification

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier |
| `Title` | `string` | Notification title |
| `Message` | `string?` | Notification body text |
| `Timestamp` | `DateTime` | When the notification was created |
| `IsRead` | `bool` | Whether the notification has been read |
| `Icon` | `MokaIconDefinition?` | Optional icon |

## Basic with Notifications

```blazor-preview
<MokaNotificationBell Notifications="@_notifications" UnreadCount="2" />

@code {
    private IReadOnlyList<MokaBellNotification> _notifications = new[]
    {
        new MokaBellNotification { Id = "1", Title = "Build succeeded", Message = "Pipeline #42 completed.", Timestamp = DateTime.Now.AddMinutes(-5) },
        new MokaBellNotification { Id = "2", Title = "New comment", Message = "Alice replied to your review.", Timestamp = DateTime.Now.AddMinutes(-15), IsRead = true },
        new MokaBellNotification { Id = "3", Title = "Deploy started", Message = "Production deploy in progress.", Timestamp = DateTime.Now.AddHours(-1) }
    };
}
```

## Empty State

```blazor-preview
<MokaNotificationBell Notifications="@Array.Empty<MokaBellNotification>()" UnreadCount="0" />
```
