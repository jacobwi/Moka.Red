---
title: Segmented Control
description: Row of connected buttons for choosing one option, such as a view mode or a time range.
order: 72
---

# Segmented Control

`MokaSegmentedControl` renders a row of connected segments for picking one option out of a few, such as a view mode or a time range. Each option is a `MokaSegment` with a string `Value`, and the control binds the selected one with `@bind-Value`. Underneath it is a radio group, so the keyboard works like any set of radio buttons. For multiple selection, use `MokaToggleGroup`.

## MokaSegmentedControl Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `string?` | -- | Value of the selected segment. Two-way bindable with `@bind-Value` |
| `ValueChanged` | `EventCallback<string>` | -- | Raised with the new value when a segment is selected |
| `AriaLabel` | `string?` | -- | Accessible name of the group, such as `"Time range"`. To use visible text instead, pass `aria-labelledby` with the id of that text |
| `FullWidth` | `bool` | `false` | Stretches the control across its container, with any margin inside it. Segments share the width equally |
| `Size` | `MokaSize` | `Md` | Segment padding and font size: `Xs`, `Sm`, `Md`, `Lg` |
| `Disabled` | `bool` | `false` | Disables every segment |
| `ChildContent` | `RenderFragment?` | -- | `MokaSegment` elements |
| `Rounded` / `RoundedValue` | `MokaRounding?` / `string?` | -- | Corner radius of the track. The segments take the same corners, so `Full` gives a pill track with pill segments |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Margin around the control |
| `Padding` / `PaddingValue` | `MokaSpacingScale?` / `string?` | -- | Space between the track's edge and the segments. Replaces the default 3px |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

```razor
<MokaSegmentedControl @bind-Value="_range" AriaLabel="Time range" Rounded="MokaRounding.Full">
    <MokaSegment Value="day" Text="Day" />
    <MokaSegment Value="week" Text="Week" />
</MokaSegmentedControl>
```

Without `Rounded` the segments keep their own, slightly smaller corners. Up to 0.1.12 the control ignored `Rounded`, and a `FullWidth` control was `width: 100%`, so a margin made it wider than its container.

## MokaSegment Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `string` | `""` | Value reported when this segment is selected. Give every segment its own value |
| `Text` | `string?` | -- | Label |
| `Icon` | `MokaIconDefinition?` | -- | Icon before the label, at 14px |
| `Disabled` | `bool` | `false` | Disables this segment. The arrow keys skip it |
| `Class` | `string?` | -- | Additional CSS classes, on the segment |
| `Style` | `string?` | -- | Additional inline styles, on the segment |

Each segment is a `<label>` around a radio input. `Class` and `Style` go on the label, which draws the segment. `Id` and any other attribute you pass, such as `aria-label` or `title`, go on the radio.

## Basic

```blazor-preview
<div style="display:flex;flex-direction:column;align-items:center;gap:8px">
    <MokaSegmentedControl @bind-Value="_range" AriaLabel="Time range">
        <MokaSegment Value="day" Text="Day" />
        <MokaSegment Value="week" Text="Week" />
        <MokaSegment Value="month" Text="Month" />
        <MokaSegment Value="year" Text="Year" />
    </MokaSegmentedControl>
    <span style="font-size:var(--moka-font-size-xs);color:var(--moka-color-on-surface-variant)">Showing: @_range</span>
</div>

@code {
    private string _range = "week";
}
```

## With Icons

```blazor-preview
<MokaSegmentedControl @bind-Value="_view">
    <MokaSegment Value="list" Text="List" Icon="MokaIcons.Content.RowsDense" />
    <MokaSegment Value="board" Text="Board" Icon="MokaIcons.Content.Columns" />
    <MokaSegment Value="files" Text="Files" Icon="MokaIcons.File.Folder" />
</MokaSegmentedControl>

@code {
    private string _view = "list";
}
```

## Icon Only

A segment without text has no accessible name, because its icon is hidden from assistive technology. Give each one an `aria-label` and a `title`. Both land on the segment's radio, which covers the whole segment, so the title also shows as a tooltip.

```blazor-preview
<MokaSegmentedControl @bind-Value="_density" Size="MokaSize.Sm" AriaLabel="Row density">
    <MokaSegment Value="comfortable" Icon="MokaIcons.Content.RowsComfortable" aria-label="Comfortable rows" title="Comfortable rows" />
    <MokaSegment Value="dense" Icon="MokaIcons.Content.RowsDense" aria-label="Dense rows" title="Dense rows" />
</MokaSegmentedControl>

@code {
    private string _density = "comfortable";
}
```

## Sizes

```blazor-preview
<div style="display:flex;flex-direction:column;align-items:center;gap:12px">
    @foreach (var size in new[] { MokaSize.Xs, MokaSize.Sm, MokaSize.Md, MokaSize.Lg })
    {
        <MokaSegmentedControl Size="size" @bind-Value="_value">
            <MokaSegment Value="a" Text="@size.ToString()" />
            <MokaSegment Value="b" Text="Option" />
            <MokaSegment Value="c" Text="Option" />
        </MokaSegmentedControl>
    }
</div>

@code {
    private string _value = "a";
}
```

## Full Width and Disabled Segments

```blazor-preview
<div style="width:100%;max-width:420px">
    <MokaSegmentedControl @bind-Value="_billing" FullWidth AriaLabel="Billing period">
        <MokaSegment Value="monthly" Text="Monthly" />
        <MokaSegment Value="yearly" Text="Yearly" />
        <MokaSegment Value="lifetime" Text="Lifetime" Disabled />
    </MokaSegmentedControl>
</div>

@code {
    private string _billing = "monthly";
}
```

## Disabled Control

```blazor-preview
<MokaSegmentedControl Value="week" Disabled AriaLabel="Time range">
    <MokaSegment Value="day" Text="Day" />
    <MokaSegment Value="week" Text="Week" />
    <MokaSegment Value="month" Text="Month" />
</MokaSegmentedControl>
```

## Behaviour

- One segment is selected at a time. Clicking the selected segment again does nothing, so the selection cannot be cleared from the UI.
- Values match with an ordinal, case-sensitive comparison. When `Value` matches no segment, none is selected.
- The selected segment is raised onto the background color with a small shadow.
- `Disabled` on the control disables every segment, and `Disabled` on a segment disables that one. Disabled segments are dimmed and ignore clicks and keys.

## Accessibility

The control is a radio group (`role="radiogroup"`). Each segment is a native radio input inside a `<label>`: the radio is invisible but covers the whole segment, so screen readers find a real radio button with its checked state, and the browser handles the keyboard. Tab moves to the selected segment, or to the first one when none is selected. The arrow keys move to the next or previous segment and select it, skipping disabled ones. Keyboard focus shows as a ring around the segment.

Name the group with `AriaLabel`, or with `aria-labelledby` pointing at a visible heading, so screen readers can say what the choice is about. A disabled segment uses the native `disabled` attribute and leaves the tab order.
