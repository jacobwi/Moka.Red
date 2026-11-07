---
title: Reconnect Modal
description: Blazor Server's reconnect screen in your theme, in place of the plain dialog Blazor draws when the connection drops.
order: 101
---

# Reconnect Modal

`MokaReconnectModal` replaces the plain dialog Blazor draws when a Blazor Server app loses its connection. It says what's happening in each state, and offers the way forward: try again, resume or reload. It follows the theme with no setup. WebAssembly has no connection to lose, so it doesn't need one.

## Usage

Put one in `App.razor`, next to `blazor.web.js`, with no render mode of its own:

```razor
<body>
    <Routes @rendermode="InteractiveServer" />
    <MokaReconnectModal />
    <script src="@Assets["_framework/blazor.web.js"]"></script>
</body>
```

`dotnet new mokared-server` already does this, for the Server and Auto modes.

It works from an interactive layout too, but in `App.razor` it's on the page from the first response, and it isn't removed along with the layout. In `App.razor` it links its script with a `<script>` element, which the browser parses. Rendered interactively it writes no `<script>` and imports the same module instead, since Blazor would create the element and set its `src`, which a Trusted Types policy blocks.

It reads the tokens `MokaThemeProvider` writes to `:root`, so it needs a provider somewhere on the page, which every Moka.Red app has. It doesn't need to be inside it.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Text` | `MokaReconnectText` | see below | The words in each state. Set only the ones you want to change |
| `ChildContent` | `RenderFragment?` | refresh icon | Drawn in the middle of the rings, such as your logo |
| `Class`, `Style` | `string?` | | On the dialog, along with `Margin`, `Padding` and `Rounded`, and any other attributes |

`Id` is ignored. Blazor finds the dialog by the id `components-reconnect-modal`, so it can't change.

## States

| State | Heading | Offers |
|-------|---------|--------|
| First attempt | Reconnecting | A progress sweep |
| Later attempts | Still reconnecting, with the seconds to the next try | A progress sweep |
| Blazor has stopped trying | Can't reach the server | Try again, Reload |
| Paused by the server | Paused while you were away | Resume, Reload |
| A paused page can't be resumed | Couldn't pick up where you left off | Reload |

When the server answers but no longer has the page's state, as after a restart, the page reloads by itself. After a failed attempt it also tries again when the person comes back to the tab.

On .NET 9, Blazor reports only the first attempt, the point it stops trying and a rejected connection. The screen keeps saying "Reconnecting" until it stops, and the paused states don't occur.

## Changing the words

```razor
<MokaReconnectModal Text="_reconnectText" />

@code {
    private static readonly MokaReconnectText _reconnectText = new()
    {
        FailedTitle = "Can't reach your router",
        FailedMessage = "It may be restarting, or this device may have left the home network."
    };
}
```

| Property | Default |
|----------|---------|
| `ReconnectingTitle` | Reconnecting |
| `ReconnectingMessage` | The connection to the server dropped. Trying to get it back. |
| `RetryingTitle` | Still reconnecting |
| `RetryingMessage` | Trying again in {0}s. |
| `FailedTitle` | Can't reach the server |
| `FailedMessage` | Check your connection, then try again. |
| `PausedTitle` | Paused while you were away |
| `PausedMessage` | The server set this page aside to save memory. Resume to carry on where you left off. |
| `ResumeFailedTitle` | Couldn't pick up where you left off |
| `ResumeFailedMessage` | The server no longer has this page's place. Reload to start it again. |
| `TryAgainLabel` | Try again |
| `ResumeLabel` | Resume |
| `ReloadLabel` | Reload |

In `RetryingMessage`, `{0}` is where Blazor writes the seconds. Leave it out and no number is shown.

## Accessibility

- It's a modal `<dialog>`, so the page behind it can't be reached and focus stays inside.
- It's named by its heading and described by its message. A change of heading is read out. The message isn't, because the seconds in it change every second.
- When a state offers a choice, focus moves to its first button.
- Escape doesn't close it, since that would leave a dead page with nothing saying why.
- `prefers-reduced-motion` stops the rings and the sweep.

## Troubleshooting

**Nothing shows when the connection drops.** The script or the stylesheet isn't being served. Check the app calls `MapStaticAssets()` or `UseStaticFiles()`, and that it links `{YourApp}.styles.css`. A class library's static files are only served to `dotnet run` in Development, or from a published build, so running with another environment can cause this.

**Blazor's plain dialog still shows.** The component isn't on the page, or it's only on pages that didn't render before the connection dropped. Move it to `App.razor`.
