---
title: Scroll Area
description: Scrollable container with a thin or hidden scrollbar and a maximum height or width.
order: 113
---

# Scroll Area

`MokaScrollArea` caps the height or width of its content and scrolls the rest. By default it scrolls vertically, clips horizontal overflow, and shows a thin scrollbar in the accent border color.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Content to scroll |
| `Height` | `string?` | -- | Maximum height, applied as CSS `max-height`. Shorter content leaves the area shorter |
| `Width` | `string?` | -- | Maximum width, applied as CSS `max-width` |
| `ScrollY` | `bool` | `true` | Scrolls vertically. When off, vertical overflow is clipped |
| `ScrollX` | `bool` | `false` | Scrolls horizontally. When off, horizontal overflow is clipped |
| `ThinScrollbar` | `bool` | `true` | Thin scrollbar in the accent border color |
| `HideScrollbar` | `bool` | `false` | Hides the scrollbar while the content still scrolls. Wins over `ThinScrollbar` |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

`ThinScrollbar` and `HideScrollbar` add the global `moka-thin-scrollbar` and `moka-hide-scrollbar` classes from `moka.css`. Those classes work on your own elements too.

## Vertical

```blazor-preview
<div style="width:100%;max-width:360px">
    <MokaScrollArea Height="180px" Style="border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
        @for (var i = 1; i <= 20; i++)
        {
            <div style="padding:6px var(--moka-spacing-md);border-bottom:1px solid var(--moka-color-outline-variant);font-size:var(--moka-font-size-sm)">
                Deploy step @i finished
            </div>
        }
    </MokaScrollArea>
</div>
```

## Horizontal

Turn on `ScrollX` and turn off `ScrollY` for a row that scrolls sideways.

```blazor-preview
<div style="width:100%;max-width:360px">
    <MokaScrollArea ScrollX ScrollY="false">
        <div style="display:flex;gap:var(--moka-spacing-sm);width:max-content;padding-bottom:var(--moka-spacing-xs)">
            @foreach (var tag in new[] { "blazor", "dotnet", "css", "wasm", "razor", "signalr", "maui", "aspire", "efcore", "xunit" })
            {
                <MokaTag Text="@tag" />
            }
        </div>
    </MokaScrollArea>
</div>
```

## Both Directions

Long log lines scroll sideways, and the whole log scrolls down.

```blazor-preview
<div style="width:100%;max-width:420px">
    <MokaScrollArea Height="140px" ScrollX Style="background:var(--moka-color-surface);border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
        <div style="padding:var(--moka-spacing-sm) var(--moka-spacing-md);font-family:var(--moka-font-family-mono);font-size:var(--moka-font-size-xs)">
            @for (var i = 1; i <= 16; i++)
            {
                <div style="white-space:pre">10:@(i.ToString("00")):00  INFO  worker[@(i % 3)]  processed batch @(i * 128) of 2048 in @(i * 7) ms</div>
            }
        </div>
    </MokaScrollArea>
</div>
```

## Hidden Scrollbar

The content scrolls with the wheel or a swipe, but no scrollbar shows that it can. Use it where the cut-off content makes that clear, like the half-visible last row here.

```blazor-preview
<div style="width:100%;max-width:360px">
    <MokaScrollArea Height="135px" HideScrollbar Style="border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
        @for (var i = 1; i <= 12; i++)
        {
            <div style="padding:6px var(--moka-spacing-md);font-size:var(--moka-font-size-sm)">Message @i</div>
        }
    </MokaScrollArea>
</div>
```

## Accessibility

The area adds no tab stop of its own, and not every browser lets the keyboard focus a scrolling element that holds nothing focusable. For plain content, pass `tabindex="0"`, `role="region"` and an `aria-label`. Unmatched attributes go on the scrolling element.

```razor
<MokaScrollArea Height="240px" tabindex="0" role="region" aria-label="Release notes">
    ...
</MokaScrollArea>
```
