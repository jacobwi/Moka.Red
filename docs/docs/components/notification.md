---
title: Notification Center
description: Persistent notification panel with bell icon, unread badge, and notification management service.
order: 28
---

# Notification Center

`MokaNotificationCenter` provides a persistent notification panel with a bell icon button, unread count badge, and a dropdown listing all notifications. Notifications are managed through `IMokaNotificationService`.

## Setup

Register the feedback services in your `Program.cs`:

```csharp
builder.Services.AddMokaFeedback();
```

Place the component in your layout:

```razor
<MokaNotificationCenter />
```

## IMokaNotificationService

Inject `IMokaNotificationService` into any component or service to manage notifications.

### Methods

| Method | Description |
|--------|-------------|
| `Push(title, message, severity)` | Pushes a new notification |
| `MarkAsRead(id)` | Marks a specific notification as read |
| `MarkAllAsRead()` | Marks all notifications as read |
| `Remove(id)` | Removes a specific notification |
| `Clear()` | Removes all notifications |

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Notifications` | `IReadOnlyList<MokaNotification>` | All current notifications, newest first |
| `UnreadCount` | `int` | Count of unread notifications |

### Events

| Event | Description |
|-------|-------------|
| `OnChanged` | Raised when the notification list changes |

## MokaNotification Model

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Id` | `Guid` | Auto-generated | Unique identifier |
| `Title` | `string` | Required | Notification title |
| `Message` | `string` | Required | Message body |
| `Severity` | `MokaToastSeverity` | `Info` | Controls color and default icon |
| `Timestamp` | `DateTime` | `DateTime.UtcNow` | Creation timestamp |
| `Read` | `bool` | `false` | Whether the notification has been read |
| `Icon` | `MokaIconDefinition?` | -- | Custom icon override |
| `OnClick` | `Action?` | -- | Optional click action |

## Pushing Notifications

```razor
@inject IMokaNotificationService NotificationService

<MokaButton OnClick="SendNotification">Notify</MokaButton>

@code {
    void SendNotification()
    {
        NotificationService.Push(
            "Build Complete",
            "Your project compiled successfully.",
            MokaToastSeverity.Success);
    }
}
```

## Severity Levels

```razor
@inject IMokaNotificationService NotificationService

@code {
    void SendExamples()
    {
        NotificationService.Push("Info", "Informational message.");
        NotificationService.Push("Success", "Operation succeeded.", MokaToastSeverity.Success);
        NotificationService.Push("Warning", "Check your input.", MokaToastSeverity.Warning);
        NotificationService.Push("Error", "Something failed.", MokaToastSeverity.Error);
    }
}
```

## Managing Notifications

```razor
@inject IMokaNotificationService NotificationService

<MokaButton OnClick="@(() => NotificationService.MarkAllAsRead())">
    Mark All Read
</MokaButton>
<MokaButton OnClick="@(() => NotificationService.Clear())" Color="MokaColor.Error">
    Clear All
</MokaButton>
```
