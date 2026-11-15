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
| `Push(title, message, severity)` | Pushes a new notification, stamped by a registered `TimeProvider`, else the system clock |
| `Push(notification)` | Pushes a built `MokaNotification`. One with the same `Id` replaces it in place; one with the same `Key` replaces the other |
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
| `OnClick` | `Action?` | -- | Optional click action. Enter and Space on the notification run it too |
| `Key` | `string?` | -- | Names what it is about, so a later one with the same key replaces it (unread, at the top) |
| `Href` | `string?` | -- | Makes the notification a link there. A `javascript:`, `vbscript:` or `data:` address is dropped |
| `Actions` | `IReadOnlyList<MokaNotificationAction>` | empty | Buttons under the notification |

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

## Actions, Links and Keys

A notification can carry buttons, lead somewhere, and take the place of an earlier one about the same thing. A
`MokaNotificationAction` takes its words and an `Action` or a `Func<Task>`; pressing it marks the notification read
(`MarksRead`, on by default) and, with `Removes`, takes it out of the center once it has run.

```razor
@inject IMokaNotificationService NotificationService

@code {
    void BuildStarted() => NotificationService.Push(new MokaNotification
    {
        Key = "build-42",
        Title = "Build #42",
        Message = "Running."
    });

    void BuildFailed() => NotificationService.Push(new MokaNotification
    {
        Key = "build-42",
        Title = "Build #42",
        Message = "Two tests failed.",
        Severity = MokaToastSeverity.Error,
        Href = "/builds/42",
        Actions = [new MokaNotificationAction("Run again", () => RunAgainAsync()) { Removes = true }]
    });

    Task RunAgainAsync() => Task.CompletedTask;
}
```

A toast shown with `AlsoInCenter` lands here too and stays after the toast has gone, under one key, so a tracked task's
loading entry turns into its result. Its action button comes along as an action.

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

## Keeping the List Short

The service keeps every notification pushed unless the app sets a limit. With `MokaFeedbackOptions.NotificationLimit`,
a push that takes the list past it removes the oldest read notifications first, then the oldest unread ones, and never
the one just pushed. A push that replaces an entry by its `Id` or `Key` removes nothing.

```csharp
builder.Services.AddMokaRed(o => o.Feedback = f => f.NotificationLimit = 100);
// or: builder.Services.AddMokaFeedback(f => f.NotificationLimit = 100);
```

## Looks

On the new design the list can take one of seven looks (`MokaNotificationLook`). Set one for the whole app on the theme, or give one center its own with `Look`, which wins over the theme. `MokaTheme.Dark` and `MokaTheme.Light` keep their one look whatever either says.

| Look | What it draws |
|------|---------------|
| `Standard` | Unread rows tinted in the accent; title, message and time on single lines |
| `Inbox` | A dot for unread, the icon in a round tile tinted by its severity, the time beside the title, two lines of message, and "2 new" in the header |
| `Cards` | Each notification a small card on a grey panel. Unread cards get an accent ring and a dot after the title |
| `Compact` | One line each: icon, title and time. The message is left to screen readers |
| `Grouped` | "Today" and "Earlier" headings, square icon tiles, a dot for unread and "2 new" in the header |
| `Timeline` | A line down the side with each icon in a ring on it, and the time above the title |
| `Terminal` | Monospace on the page color, a tag such as `[WARN]` in place of the icon, a glow on unread rows and "2 NEW" in the header |

The theme way:

```razor
<MokaThemeProvider Theme="@(MokaTheme.Default.WithNotifications(MokaNotificationLook.Inbox))">
    <MokaNotificationCenter />
</MokaThemeProvider>
```

The parameter way:

```razor
<MokaNotificationCenter Look="MokaNotificationLook.Grouped"
                        Text="@(new MokaNotificationCenterText { Today = "Aujourd'hui", Earlier = "Plus tôt", NewCount = n => $"{n} nouvelles" })" />
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Look` | `MokaNotificationLook?` | `null` | This center's look. Null takes the theme's |
| `Text` | `MokaNotificationCenterText` | English | Every word the center writes. See [Words](#words) |

## Words

| `MokaNotificationCenterText` | Default |
|------------------------------|---------|
| `Title` | "Notifications", the panel's heading |
| `MarkAllRead`, `ClearAll` | "Mark all read", "Clear all" |
| `Empty` | "No notifications" |
| `Unread` | "Unread", what a screen reader hears after an unread notification |
| `Today`, `Earlier` | The headings under `Grouped` |
| `NewCount` | "2 new", the header's count in `Inbox`, `Grouped` and `Terminal`. Hidden while nothing is unread |
| `Bell` | "Notifications, 3 unread", the bell's name, from the unread count |
| `Dismiss` | "Dismiss *title*" |
| `Time` | "5m ago", from the UTC timestamp and the clock's now |

`Grouped` counts a notification as today's when its `Timestamp` falls on today's local date. The date comes from a registered `TimeProvider` if there is one, else the system clock, so a test can pin it. A timestamp with no stated kind counts as UTC, as the relative time beside it does. Each heading names the list under it (`aria-labelledby`) rather than sitting in it, so a list still holds only its notifications, Tab still walks them in order, and dismissing one moves focus by the order on screen.

The `Terminal` tags are `[INFO]`, `[ OK ]`, `[WARN]` and `[FAIL]`, from the severity, and hidden from screen readers. Under every look an unread notification is still described as "Unread".

The panel floats, so under every look but `Cards` and `Terminal` it follows the theme's popup look (`MokaTheme.WithPopups`): its background, border, corners, shadow, blur, font and text colors. `Cards` and `Terminal` draw a box of their own.

## Keyboard and Screen Readers

The bell shows only an icon and a count, so it is named "Notifications" ("Notifications, 3 unread") by `aria-label` and
by a `MokaTooltip` below it, which hides while the panel is open, since the panel opens in the same place. Each
notification's dismiss button is named "Dismiss *title*" the same way; its tooltip opens to the left, over its own
notification, never over the button below.

The bell is a disclosure button: it reports `aria-expanded` as `"true"` or `"false"`, points `aria-controls` at the open panel, and its name includes the unread count ("Notifications, 3 unread"). The panel comes right after it, so Tab moves from the bell to Mark all read, Clear all, and then each notification and its dismiss button.

- Each notification is a button. Enter or Space marks it read and runs its `OnClick`, as a click does. An unread notification is described as "Unread".
- The dismiss button is named after its notification ("Dismiss Build Complete") and shows while its row has focus. Dismissing moves focus to the notification that took its place, or to the bell when none is left.
- Escape closes the panel and returns focus to the bell. Inside a `MokaDialog` it closes only the panel.
- Clear all closes the panel and returns focus to the bell.
- The panel closes when focus leaves it: Tab past the last notification, or a click anywhere else on the page.

Notifications are keyed by `Id`. Two notifications pushed with the same `Id` render unkeyed rather than making Blazor throw on the next render.

Up to 0.1.12 a notification could only be opened with the mouse, the dismiss button stayed invisible while it had focus, Escape did nothing, and the panel stayed open until the bell was clicked again.

## Above the page

The open notification center's panel sits in the browser's top layer, so a card that hides its overflow no longer cuts it off and nothing later on the page paints over it, whatever its `z-index`. It stays against the bell as the page scrolls and flips to the other side when the window has no room there. While it's open the library sets its position, so a stylesheet can restyle it but not move it.
