---
title: Scroll to Top
description: Floating button that appears once the page, or a scrolling panel, scrolls down and takes it back to the top.
order: 68
---

# Scroll to Top

`MokaScrollToTop` shows a small round button in the bottom-right corner once the page has scrolled past `ShowAfter` pixels, and scrolls back to the top when clicked. It watches the window by default; in a layout where the content scrolls inside a panel, point `ScrollContainerSelector` at that panel. Place one in your layout. For a fixed button that runs your own action, use `MokaFloatingActionButton`.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ShowAfter` | `int` | `200` | Scroll offset in pixels past which the button appears |
| `Smooth` | `bool` | `true` | Scrolls with a smooth animation. `false` jumps straight to the top |
| `ScrollContainerSelector` | `string?` | -- | CSS selector of the element that scrolls, such as `".app-main"`. The button then watches and scrolls that element instead of the window |
| `Label` | `string` | `"Scroll to top"` | Tooltip and accessible name of the button |
| `Disabled` | `bool` | `false` | Disables the button. Clicks do nothing |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles, for example to move the button |

## Basic

Docs previews run in a frame that grows to fit its content, so the frame never scrolls. These examples set `ShowAfter="-1"` to show the button at once. In an app, keep the default or pick your own offset.

```blazor-preview
<MokaScrollToTop ShowAfter="-1" />
```

## Position

The button sits 60px from the bottom and 16px from the right. Move it with `Style`, for example to clear a chat widget or a floating action button.

```blazor-preview
<MokaScrollToTop ShowAfter="-1" Style="bottom:16px;right:auto;left:16px" />
```

## In a Layout

Render it once, at the end of the layout, inside the theme provider.

```razor
@inherits LayoutComponentBase

<MokaThemeProvider>
    <main>@Body</main>
    <MokaScrollToTop ShowAfter="400" />
</MokaThemeProvider>
```

## In a Scrolling Panel

App shells and dock layouts often fix the page to the viewport and scroll a main panel instead, so the window never scrolls. Give the panel a class and pass a selector for it.

```razor
@inherits LayoutComponentBase

<MokaThemeProvider>
    <div style="display:flex;height:100vh">
        <nav style="width:220px">...</nav>
        <main class="app-main" style="flex:1;overflow:auto">@Body</main>
    </div>
    <MokaScrollToTop ScrollContainerSelector=".app-main" ShowAfter="400" />
</MokaThemeProvider>
```

A selector also reaches panels that a library component renders, such as a `MokaDockContent` with `Class="app-main"`, which you cannot capture with `@ref`.

## Behaviour

- The button is rendered only while the scroll offset is greater than `ShowAfter`. Below that point it is not in the DOM.
- By default the component watches `window.scrollY`. With `ScrollContainerSelector` it watches the `scrollTop` of the element that matches the selector, and a click scrolls that element. Use a selector that matches one element.
- The panel is matched on every scroll event, so the button keeps working when a re-render replaces the element. While no element matches, the button stays hidden. An invalid selector logs a warning to the browser console and hides the button.
- The browser tells the component only when the button should appear or disappear, not on every scroll event.
- A click scrolls to the top, smooth or instant depending on `Smooth`.
- Changing `ShowAfter` or `ScrollContainerSelector` moves the listener to the new settings.
- Each instance registers its own listener and removes it when the component is disposed.
- The button is 36px, round, and sits at `z-index` 900, fixed to the corner of the viewport even when it watches a panel.
- A disabled button is dimmed and ignores clicks.

## Accessibility

The button is a native `<button type="button">` named by its `title`, `Label`, because the arrow icon is `aria-hidden`. Set `Label` to translate it. The button only exists after the page scrolls, so render the component at the end of the layout, where it comes last in the tab order. `Disabled` sets the native `disabled` attribute, which takes the button out of the tab order.
