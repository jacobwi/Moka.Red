---
title: Split Pane
description: Resizable two-panel layout for side-by-side content arrangement.
order: 23
---

# Split Pane

`MokaSplitPane` divides its container into two resizable panels separated by a draggable divider. It supports horizontal (left/right) and vertical (top/bottom) orientations, configurable initial and minimum sizes, and an optional collapse toggle.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `FirstContent` | `RenderFragment?` | -- | Content rendered in the first (left or top) panel |
| `SecondContent` | `RenderFragment?` | -- | Content rendered in the second (right or bottom) panel |
| `Orientation` | `MokaDirection` | `Row` | `Row` for horizontal split, `Column` for vertical split |
| `InitialSize` | `string` | `"50%"` | Initial size of the first panel (CSS length) |
| `MinSize` | `string` | `"100px"` | Minimum size for each panel |
| `Collapsed` | `bool` | `false` | Whether the second panel is collapsed |
| `ShowHandle` | `bool` | `true` | Shows a visible drag handle on the divider |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Horizontal Split

The default orientation places two panels side by side.

```blazor-preview
<div style="height: 200px;">
    <MokaSplitPane InitialSize="40%">
        <FirstContent>
            <div style="padding: var(--moka-spacing-md); background: var(--moka-color-surface-variant); height: 100%;">
                <MokaHeading Level="4">Left Panel</MokaHeading>
                <MokaParagraph>Navigation or file tree goes here.</MokaParagraph>
            </div>
        </FirstContent>
        <SecondContent>
            <div style="padding: var(--moka-spacing-md); height: 100%;">
                <MokaHeading Level="4">Right Panel</MokaHeading>
                <MokaParagraph>Main content or editor area.</MokaParagraph>
            </div>
        </SecondContent>
    </MokaSplitPane>
</div>
```

## Vertical Split

Set `Orientation="MokaDirection.Column"` for a top/bottom arrangement.

```blazor-preview
<div style="height: 300px;">
    <MokaSplitPane Orientation="MokaDirection.Column" InitialSize="40%">
        <FirstContent>
            <div style="padding: var(--moka-spacing-md); background: var(--moka-color-surface-variant); height: 100%;">
                <MokaHeading Level="4">Top Panel</MokaHeading>
                <MokaParagraph>Source code or input area.</MokaParagraph>
            </div>
        </FirstContent>
        <SecondContent>
            <div style="padding: var(--moka-spacing-md); height: 100%;">
                <MokaHeading Level="4">Bottom Panel</MokaHeading>
                <MokaParagraph>Output console or preview area.</MokaParagraph>
            </div>
        </SecondContent>
    </MokaSplitPane>
</div>
```

## Collapsed Second Pane

Set `Collapsed="true"` to hide the second panel. The first panel fills all available space.

```blazor-preview
<div style="height: 200px;">
    <MokaSplitPane Collapsed="true">
        <FirstContent>
            <div style="padding: var(--moka-spacing-md); height: 100%;">
                <MokaHeading Level="4">Full Width</MokaHeading>
                <MokaParagraph>The second panel is collapsed. Toggle it back to restore the split.</MokaParagraph>
            </div>
        </FirstContent>
        <SecondContent>
            <div style="padding: var(--moka-spacing-md); height: 100%;">
                <MokaParagraph>Hidden content.</MokaParagraph>
            </div>
        </SecondContent>
    </MokaSplitPane>
</div>
```
