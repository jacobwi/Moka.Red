---
title: System Notifications
description: Notifications outside the page through the browser's Notification API, opt-in, asking for permission only when the app does.
order: 165
---

# System Notifications

`IMokaSystemNotifier` shows notifications outside the page through the browser's Notification API, for news worth
hearing while the app isn't in sight: a long export finished, a message arrived. It is opt-in and never asks for
permission on its own. A host without the API, such as a MAUI web view, is reported as unsupported rather than failing.

## Setup

```csharp
builder.Services.AddMokaRed();
builder.Services.AddMokaSystemNotifier();
```

Call it only from an interactive render, since it talks to the browser.

## Asking and Notifying

Ask from something the person pressed: browsers refuse to ask otherwise, and Safari only asks while the click is recent,
which a Blazor Server round trip on a slow connection can outlast.

```razor
@inject IMokaSystemNotifier Notifier

<MokaButton OnClick="AllowAsync">Allow notifications</MokaButton>
<MokaButton OnClick="NotifyAsync">Notify when hidden</MokaButton>
<p>@_state</p>

@code {
    string _state = "";

    async Task AllowAsync() => _state = (await Notifier.RequestPermissionAsync()).ToString();

    async Task NotifyAsync()
    {
        MokaSystemNotificationResult result = await Notifier.NotifyAsync(new MokaSystemNotification
        {
            Title = "Export finished",
            Body = "report.pdf is ready.",
            Tag = "export",
            OnlyWhenHidden = true
        });
        _state = result.ToString();
    }
}
```

| Member | Description |
|--------|-------------|
| `GetPermissionAsync()` | `Unsupported`, `NotAsked`, `Granted` or `Denied`, without asking |
| `RequestPermissionAsync()` | Asks, unless the person already answered |
| `NotifyAsync(notification)` | `Shown`, `PageVisible` (with `OnlyWhenHidden` while the page is in sight), `NotAllowed` or `Unsupported` |

## MokaSystemNotification

| Property | Type | Description |
|----------|------|-------------|
| `Title` | `string` | The first line |
| `Body` | `string?` | The text under it |
| `Icon` | `string?` | The address of a picture beside the text |
| `Tag` | `string?` | A later notification with the same tag replaces this one |
| `OnlyWhenHidden` | `bool` | Shows it only while the page is out of sight |
| `OnClick` | `Func<Task>?` | Runs when the person clicks it, after the browser brings the window forward. A failure is reported in the browser's console |

Chrome on Android shows notifications only through a service worker, so there the result is `Unsupported`.
