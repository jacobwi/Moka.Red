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
| `Open` | `bool` | `false` | Whether the popover is visible (two-way bindable) |
| `OpenChanged` | `EventCallback<bool>` | -- | Callback when open state changes |
| `Trigger` | `MokaPopoverTrigger` | `Click` | `Click`, `Hover`, `Manual` |
| `Position` | `MokaPopoverPosition` | `Bottom` | `Top`, `Bottom`, `Left`, `Right`, `TopStart`, `TopEnd`, `BottomStart`, `BottomEnd` |
| `CloseOnClickOutside` | `bool` | `true` | Clicking outside closes the popover |
| `CloseOnEscape` | `bool` | `true` | Pressing Escape closes the popover |
| `OffsetX` | `int` | `0` | Horizontal offset in pixels |
| `OffsetY` | `int` | `4` | Vertical offset (gap from anchor) in pixels |
| `Arrow` | `bool` | `false` | Shows an arrow/caret pointing to the anchor |
| `MatchWidth` | `bool` | `false` | Popover matches the trigger element width |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

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
