---
title: Resizable
description: Wrapper the user can resize by dragging its right edge, bottom edge or corner.
order: 112
---

# Resizable

`MokaResizable` wraps content in a box the user resizes with the pointer. `Direction` picks the handles: the right edge for the width, the bottom edge for the height, or both edges plus a corner. The drag sizes the element itself, also inside a CSS grid, and reports the new size through `Width` and `Height`, which are two-way bindable.

For two panels that share one divider, use [Split Pane](split-pane). For panels docked to the edges of a layout, use [Dock Layout](dock-layout).

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Content to make resizable |
| `Direction` | `MokaResizeDirection` | `Horizontal` | `Horizontal` adds a handle on the right edge, `Vertical` one on the bottom edge, `Both` adds both and a corner handle |
| `Width` | `string?` | -- | Width as a CSS value. Two-way bindable. A drag keeps its size when the parent re-renders, and a new value from the parent replaces it |
| `WidthChanged` | `EventCallback<string>` | -- | Fires when a drag on the right edge or the corner ends, with the width in px, such as `"312px"` |
| `Height` | `string?` | -- | Height as a CSS value. Two-way bindable, and kept the same way as `Width` |
| `HeightChanged` | `EventCallback<string>` | -- | Fires when a drag on the bottom edge or the corner ends, with the height in px |
| `MinWidth` | `string?` | -- | Smallest width. Applied as CSS `min-width` and as a drag limit |
| `MaxWidth` | `string?` | -- | Largest width. Applied as CSS `max-width` and as a drag limit |
| `MinHeight` | `string?` | -- | Smallest height. Applied as CSS `min-height` and as a drag limit |
| `MaxHeight` | `string?` | -- | Largest height. Applied as CSS `max-height` and as a drag limit |
| `ShowHandle` | `bool` | `true` | Shows a grip line on each handle. Hidden handles still drag |
| `OnResized` | `EventCallback<MokaResizeResult>` | -- | Fires when a drag ends, with `Width` and `Height` in px |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

The limits accept px, rem, em, %, vw and vh. A percentage is a share of the parent's width or height.

## Width

Drag the right edge. The label shows the bound value.

```blazor-preview
<div style="width:100%;height:150px">
    <MokaResizable @bind-Width="_width" MinWidth="180px" MaxWidth="460px">
        <div style="height:130px;box-sizing:border-box;padding:var(--moka-spacing-md);background:var(--moka-color-surface);border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
            <strong>Notes</strong>
            <p style="margin:var(--moka-spacing-xs) 0 0;font-family:var(--moka-font-family-mono);font-size:var(--moka-font-size-sm)">width: @_width</p>
        </div>
    </MokaResizable>
</div>

@code {
    private string _width = "280px";
}
```

## Both Directions

`Both` adds the bottom edge and a corner handle that changes the width and height in one drag.

```blazor-preview
<div style="width:100%;height:260px">
    <MokaResizable Direction="MokaResizeDirection.Both"
                   @bind-Width="_width" @bind-Height="_height"
                   MinWidth="160px" MaxWidth="480px"
                   MinHeight="90px" MaxHeight="240px">
        <div style="width:100%;height:100%;box-sizing:border-box;display:flex;align-items:center;justify-content:center;background:var(--moka-color-surface-variant);border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md);font-family:var(--moka-font-family-mono);font-size:var(--moka-font-size-sm)">
            @_width by @_height
        </div>
    </MokaResizable>
</div>

@code {
    private string _width = "260px";
    private string _height = "140px";
}
```

## Height and OnResized

A console panel whose height the user drags. `OnResized` reports the final size in px when the drag ends.

```blazor-preview
<div style="width:100%;height:250px">
    <MokaResizable Direction="MokaResizeDirection.Vertical" @bind-Height="_height"
                   MinHeight="80px" MaxHeight="200px" Style="width:100%"
                   OnResized="@(r => _lastHeight = r.Height)">
        <div style="height:100%;box-sizing:border-box;padding:var(--moka-spacing-sm) var(--moka-spacing-md);background:var(--moka-color-surface);border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md);font-family:var(--moka-font-family-mono);font-size:var(--moka-font-size-sm)">
            $ dotnet test<br />
            Passed: 507, failed: 0
        </div>
    </MokaResizable>
    <p style="margin:var(--moka-spacing-sm) 0 0;font-size:var(--moka-font-size-sm)">Last resize: @(_lastHeight is null ? "none yet" : $"{_lastHeight} px")</p>
</div>

@code {
    private string _height = "120px";
    private double? _lastHeight;
}
```

## Behaviour

- The wrapper is an inline block. Without `Width` it is as wide as its content, and without `Height` as tall.
- The handles are 6px strips on the right and bottom edges and a 12px square at the corner. The grip line turns to the primary color on hover. With `ShowHandle="false"` the line is hidden and the edges still drag.
- `OnResized` always reports both axes. An axis that was never dragged and was not set in px reports 0.

Up to 0.1.12 a drag on a `MokaResizable` inside a CSS grid could resize the grid's track instead of the element, and the drag limits read px values only.

## Accessibility

The right and bottom handles are focusable separators, named "Resize width" and "Resize height", that report the size in px through `aria-valuenow`, `aria-valuemin` and `aria-valuemax`. The arrow keys resize by 10px (50px with Shift), and Home and End jump to the limits. A key goes through the same path as a drag, and `OnResized` fires when the key is released. The corner handle is for the pointer only, since the two edges cover both axes from the keyboard.

Limits accept px, rem, em, %, vw and vh. A `%` limit on an element inside a CSS grid is taken of its grid area. The handles set `touch-action: none`, so a touch drag resizes instead of scrolling the page.
