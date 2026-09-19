---
title: Popover
description: Positioned popup anchored to a trigger element with click, hover, or manual control.
order: 22
---

# Popover

`MokaPopover` is a positioned popup that anchors to a trigger element. Unlike `MokaTooltip`, popovers stay open for interaction and support any content. Supports click, hover, and manual trigger modes with configurable positioning.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | The trigger element |
| `PopoverContent` | `RenderFragment?` | -- | Content displayed inside the popover |
| `Open` | `bool` | `false` | Whether the popover is visible (two-way bindable). The popover still opens and closes itself; a value from the parent takes effect when it differs from the last one passed |
| `OpenChanged` | `EventCallback<bool>` | -- | Callback when open state changes |
| `Trigger` | `MokaPopoverTrigger` | `Click` | `Click`, `Hover`, `Manual` |
| `Position` | `MokaPopoverPosition` | `Bottom` | `Top`, `Bottom`, `Left`, `Right`, `TopStart`, `TopEnd`, `BottomStart`, `BottomEnd` |
| `CloseOnClickOutside` | `bool` | `true` | Clicking outside closes the popover |
| `CloseOnEscape` | `bool` | `true` | Pressing Escape closes the popover. The key stops there, so a dialog around the popover stays open |
| `OffsetX` | `int` | `0` | Horizontal offset in pixels |
| `OffsetY` | `int` | `4` | Vertical offset (gap from anchor) in pixels |
| `Arrow` | `bool` | `false` | Shows an arrow/caret pointing to the anchor |
| `MatchWidth` | `bool` | `false` | Popover matches the trigger element width |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Margin around the popover's wrapper, the box that holds the trigger in the page |
| `Padding` / `PaddingValue` | `MokaSpacingScale?` / `string?` | -- | Padding inside the popup panel |
| `Rounded` / `RoundedValue` | `MokaRounding?` / `string?` | -- | Corner radius of the popup panel |
| `Id` | `string?` | -- | `id` of the wrapper |
| `Class` | `string?` | -- | Additional CSS classes on the wrapper |
| `Style` | `string?` | -- | Additional inline styles on the wrapper |

The wrapper carries the `moka-popover` class, and `Id`, `Class`, `Style` and any other attributes go on it. It is always in the page, around the trigger. The popup exists only while the popover is open and keeps an id of its own, which the trigger's `aria-controls` points at. Up to 0.1.12 `Id` was ignored, and the wrapper's class was `moka-popover-wrapper`: rename that in your own CSS.

## Spacing

The popover has two boxes: the wrapper around the trigger, which sits in your layout, and the popup panel. The margin and `Style` go on the wrapper, and the padding and radius on the panel, so the content needs no padded `div` of its own:

```razor
<MokaPopover PaddingValue="12px" Rounded="MokaRounding.Lg">
    <ChildContent>
        <MokaButton Variant="MokaVariant.Outlined">Details</MokaButton>
    </ChildContent>
    <PopoverContent>Popover content goes here.</PopoverContent>
</MokaPopover>
```

Up to 0.1.12 the popover ignored `Margin`, `Padding`, `Rounded` and `Style`.

## Click Trigger (Default)

```blazor-preview
<MokaPopover>
    <ChildContent>
        <MokaButton Variant="MokaVariant.Outlined">Click me</MokaButton>
    </ChildContent>
    <PopoverContent>
        <div style="padding:12px">
            <p>Popover content goes here.</p>
        </div>
    </PopoverContent>
</MokaPopover>
```

## Hover Trigger

```blazor-preview
<MokaPopover Trigger="MokaPopoverTrigger.Hover" Position="MokaPopoverPosition.Right">
    <ChildContent>
        <MokaButton Variant="MokaVariant.Soft">Hover me</MokaButton>
    </ChildContent>
    <PopoverContent>
        <div style="padding:12px">
            This appears on hover and stays while you interact.
        </div>
    </PopoverContent>
</MokaPopover>
```

## With Arrow

```blazor-preview
<MokaPopover Arrow Position="MokaPopoverPosition.Top">
    <ChildContent>
        <MokaButton>With Arrow</MokaButton>
    </ChildContent>
    <PopoverContent>
        <div style="padding:12px">Arrow points to the trigger.</div>
    </PopoverContent>
</MokaPopover>
```

## Positions

```blazor-preview
<div style="display:flex;gap:12px;flex-wrap:wrap;padding:60px 0">
    <MokaPopover Position="MokaPopoverPosition.Top">
        <ChildContent><MokaButton Variant="MokaVariant.Outlined" Size="MokaSize.Sm">Top</MokaButton></ChildContent>
        <PopoverContent><div style="padding:8px">Top</div></PopoverContent>
    </MokaPopover>
    <MokaPopover Position="MokaPopoverPosition.Bottom">
        <ChildContent><MokaButton Variant="MokaVariant.Outlined" Size="MokaSize.Sm">Bottom</MokaButton></ChildContent>
        <PopoverContent><div style="padding:8px">Bottom</div></PopoverContent>
    </MokaPopover>
    <MokaPopover Position="MokaPopoverPosition.BottomStart">
        <ChildContent><MokaButton Variant="MokaVariant.Outlined" Size="MokaSize.Sm">Bottom Start</MokaButton></ChildContent>
        <PopoverContent><div style="padding:8px">Bottom Start</div></PopoverContent>
    </MokaPopover>
    <MokaPopover Position="MokaPopoverPosition.BottomEnd">
        <ChildContent><MokaButton Variant="MokaVariant.Outlined" Size="MokaSize.Sm">Bottom End</MokaButton></ChildContent>
        <PopoverContent><div style="padding:8px">Bottom End</div></PopoverContent>
    </MokaPopover>
</div>
```

## Manual Control

Use `Trigger="MokaPopoverTrigger.Manual"` with two-way binding on `Open` for full programmatic control.

```blazor-preview
@code {
    bool _open;
}
<MokaButton OnClick="@(() => _open = !_open)" Variant="MokaVariant.Outlined">
    @(_open ? "Close" : "Open") Popover
</MokaButton>

<MokaPopover Trigger="MokaPopoverTrigger.Manual" @bind-Open="_open">
    <ChildContent><span></span></ChildContent>
    <PopoverContent>
        <div style="padding:12px">Manually controlled popover.</div>
    </PopoverContent>
</MokaPopover>
```

## Match Width

Set `MatchWidth` to make the popover the same width as the trigger -- useful for dropdown-style menus.

```blazor-preview
<MokaPopover MatchWidth>
    <ChildContent>
        <MokaButton FullWidth Variant="MokaVariant.Outlined">Full-width trigger</MokaButton>
    </ChildContent>
    <PopoverContent>
        <div style="padding:12px">This popover matches the trigger width.</div>
    </PopoverContent>
</MokaPopover>
```
