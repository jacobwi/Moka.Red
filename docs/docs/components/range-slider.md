---
title: Range Slider
description: Two-thumb slider for picking a range between a lower and an upper value.
order: 118
---

# Range Slider

`MokaRangeSlider` picks a range with two thumbs on one track, such as a price band or an age range. The track between the thumbs is filled with the primary color. For a single value, use `MokaSlider`, covered on the [Forms](forms#slider) page.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ValueStart` | `double` | `0` | Lower end of the range. Two-way bindable |
| `ValueStartChanged` | `EventCallback<double>` | -- | Fires while the start thumb moves |
| `ValueEnd` | `double` | `100` | Upper end of the range. Two-way bindable |
| `ValueEndChanged` | `EventCallback<double>` | -- | Fires while the end thumb moves |
| `Min` | `double` | `0` | Lowest value on the track |
| `Max` | `double` | `100` | Highest value on the track |
| `Step` | `double` | `1` | Increment between values |
| `Label` | `string?` | -- | Text above the slider |
| `ShowValues` | `bool` | `true` | Shows both values under the track |
| `Disabled` | `bool` | `false` | Disables both thumbs and dims the slider |
| `Size` | `MokaSize` | `Md` | Size of the label text. The track and thumbs keep one size |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic

```blazor-preview
<div style="width:100%;max-width:360px;display:flex;flex-direction:column;gap:var(--moka-spacing-sm)">
    <MokaRangeSlider @bind-ValueStart="_from" @bind-ValueEnd="_to"
                     Label="Price" Min="0" Max="500" Step="10" />
    <span style="font-size:var(--moka-font-size-sm)">Showing items from @_from to @_to USD</span>
</div>

@code {
    private double _from = 100;
    private double _to = 350;
}
```

## Custom Range

Hide the built-in values with `ShowValues="false"` and print them your own way.

```blazor-preview
<div style="width:100%;max-width:360px;display:flex;flex-direction:column;gap:var(--moka-spacing-sm)">
    <MokaRangeSlider @bind-ValueStart="_minAge" @bind-ValueEnd="_maxAge"
                     Label="Age" Min="18" Max="65" ShowValues="false" />
    <span style="font-size:var(--moka-font-size-sm)">Ages @_minAge to @_maxAge</span>
</div>

@code {
    private double _minAge = 25;
    private double _maxAge = 40;
}
```

## Decimal Steps

```blazor-preview
<div style="width:100%;max-width:360px">
    <MokaRangeSlider @bind-ValueStart="_low" @bind-ValueEnd="_high"
                     Label="Rating" Min="0" Max="5" Step="0.5" />
</div>

@code {
    private double _low = 2.5;
    private double _high = 4.5;
}
```

## Disabled

```blazor-preview
<div style="width:100%;max-width:360px">
    <MokaRangeSlider ValueStart="20" ValueEnd="80" Label="Volume limits" Disabled />
</div>
```

## Behaviour

- The values change while a thumb moves, so the `Changed` callbacks fire many times during one drag. Debounce expensive work you start from them, such as a search request.
- The thumbs cannot pass each other: the start thumb stops at the end value, and the end thumb stops at the start value.
- Only the thumbs take the pointer. A click on the track does not move a thumb.
- A one-way value sets where a thumb starts. The slider keeps the user's changes when the parent re-renders, and moves a thumb only when the parent passes a different value for it.
- `Margin` goes on the field, around the label. `Padding` goes on the element that holds both thumbs, and `Rounded` shapes the track.
- `Id`, `Class`, `Style` and unmatched attributes go on the element that holds both thumbs.

Up to 0.1.12 a parent render moved the thumbs of an unbound slider back to where they started, and the slider ignored `Margin`, `Padding` and `Rounded`.

## Accessibility

- Each thumb is a native `<input type="range">` with a tab stop of its own. The arrow keys move a thumb by `Step`, Page Up and Page Down in larger steps, and Home and End toward `Min` and `Max`, stopping at the other thumb. The focused thumb shows the focus ring.
- The thumbs are named after `Label`: "Price start" and "Price end". The pair sits in a `role="group"` that `Label` names, and a click on the label focuses the start thumb.
- Without a `Label`, pass `aria-label`: it names the group, and the thumbs are named after it, such as "Budget start". Without either, they are "Range start" and "Range end".
