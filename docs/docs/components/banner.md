---
title: Banner
description: App-wide notices along the top of the page, such as maintenance, being offline or a new version, dismissible and remembered.
order: 163
---

# Banner

`IMokaBannerService` shows app-wide notices along the top of the page, and `MokaBannerHost` draws them: planned
maintenance, being offline, a new version ready. A banner can be dismissed, and with `Remember` the dismissal is kept
across visits. Screen readers hear each banner once, politely. Colors follow [Notice](/components/notice).

## Setup

`AddMokaRed()` registers the service and the storage, as `AddMokaFeedback()` does on its own. Place the host at the top
of the layout:

```razor
@* MainLayout.razor *@
<MokaBannerHost />
@Body
```

## MokaBannerHost Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Sticky` | `bool` | `true` | The banners stay at the top of the window as the page scrolls |
| `Text` | `MokaBannerText` | English | `Dismiss` ("Dismiss"), `Region` ("Notices", the strip's name for screen readers), `NewTab` ("opens in a new tab", heard after a link whose `Target` opens another tab) and `Announcement` |
| `Class`, `Style`, `Id` | | | On the strip |

## IMokaBannerService

| Member | Description |
|--------|-------------|
| `Show(banner)` | Shows a banner, or replaces the one with its `Id` in place |
| `Dismiss(id)` | Takes a banner away. Nothing is remembered |
| `Clear()` | Takes every banner away |
| `Banners` | The banners shown, oldest first, as an immutable snapshot |
| `Changed` | Raised after every change |

## MokaBanner

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Id` | `string` | required | Names the banner. A dismissal is remembered by it, so a notice that should come back once it changes gets a new id |
| `Message` | `string` | required | The notice |
| `Title` | `string?` | - | Bold words before the message |
| `Severity` | `MokaToastSeverity` | `Info` | Color and icon |
| `Icon`, `ShowIcon` | | severity's, `true` | The icon |
| `Dismissible` | `bool` | `true` | Whether people can close it. Off for a notice that lasts as long as its cause |
| `Remember` | `bool` | `false` | Keeps a dismissal across visits |
| `LinkText`, `Href` | `string?` | - | A link after the message. A `javascript:`, `vbscript:` or `data:` address is dropped |
| `Target` | `string?` | - | Where the link opens, such as `_blank`. One that opens another tab gets `rel="noopener noreferrer"` and the host's `Text.NewTab` for screen readers. See [Links to other sites](#links-to-other-sites) |
| `ActionText`, `OnAction` | `string?`, `Func<Task>?` | - | An action button |

## Showing Banners

```blazor-preview
@inject IMokaBannerService Banners

<MokaButton OnClick="Maintenance">Maintenance</MokaButton>
<MokaButton OnClick="Offline">Offline</MokaButton>
<MokaButton OnClick="Online">Back online</MokaButton>

<MokaBannerHost Sticky="false" />

@code {
    void Maintenance() => Banners.Show(new MokaBanner
    {
        Id = "maintenance-2026-11-20",
        Title = "Maintenance",
        Message = "The service is down on Friday from 22:00 to 23:00.",
        Severity = MokaToastSeverity.Warning,
        LinkText = "Details",
        Href = "/status",
        Remember = true
    });

    void Offline() => Banners.Show(new MokaBanner
    {
        Id = "offline",
        Message = "You're offline. Changes are kept and sent once you're back.",
        Severity = MokaToastSeverity.Error,
        Dismissible = false
    });

    void Online() => Banners.Dismiss("offline");
}
```

## Links to other sites

`Target` sends a banner's link somewhere else, such as `_blank` for a status page outside the app. A target that opens
another tab gives the link `rel="noopener noreferrer"`, so the page it opens can't reach this one, and screen readers
hear ", opens in a new tab" after the link's words. The host's `Text.NewTab` changes the words. `_self`, `_parent` and
`_top` render as given and add nothing.

```csharp
Banners.Show(new MokaBanner
{
    Id = "incident-2026-11-21",
    Message = "Sign-in is slow for some people.",
    Severity = MokaToastSeverity.Warning,
    LinkText = "Status page",
    Href = "https://status.example.com",
    Target = "_blank"
});
```

## Remembering Dismissals

With `Remember` on, closing a banner writes its id to `IMokaBannerStorage`. The default, `MokaBrowserBannerStorage`,
uses the browser's local storage under keys starting `moka-banner:`; where the browser refuses storage, nothing is
remembered and the console says why. A remembered banner is drawn only once the storage has said it wasn't dismissed, so
it never flashes up and goes. `Remember` is off by default, so a notice that comes back on its own, such as Offline,
isn't silenced for good.

To keep dismissals with the person's account instead, register your own storage before or after `AddMokaRed()`:

```csharp
builder.Services.AddScoped<IMokaBannerStorage, AccountBannerStorage>();
```

`IMokaBannerStorage` has `IsDismissedAsync(id)`, `RememberDismissedAsync(id)` and `ForgetAsync(id)`.
