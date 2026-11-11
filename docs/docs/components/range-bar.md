---
title: Range Bar
description: A range dragged inside one track, with colored parts, ticks and pins showing what's already on the scale.
order: 146
---

# Range Bar

`MokaRangeBar` picks a range on a scale that already has things on it: the addresses a router hands out, between the ones kept for fixed devices and the guest pool, with a tick for each address in use. The range sits on one track with colored, labelled parts, ticks and pins along it, and a flag over each handle says where it is.

## Range Bar or Range Slider

| | `MokaRangeSlider` | `MokaRangeBar` |
|---|---|---|
| What it is | Two native range inputs on one line | One track with a range and everything else on the scale drawn along it |
| What's on the track | The filled range, nothing else | Parts, ticks, pins, and the range over them |
| Values shown | Both values under the track | A flag over each handle, kept inside the bar, and the two ends of the scale |
| Formatting | Numbers in the current culture | Any text, through `Formatter` (an IP address from a number, a time from minutes) |
| Read-only picture | No | `ReadOnly`, with a key under it |
| `EditForm` | No | `@bind-Value` with a `MokaRange` |

Use `MokaRangeSlider` for a price band or an age range. Use `MokaRangeBar` when the range only makes sense next to what's around it.

## Setup

```razor
@using Moka.Red.Forms.RangeBar
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Start` | `double` | `Min` | Where the range starts. Two-way bindable |
| `StartChanged` | `EventCallback<double>` | - | Raised when a key or a drag moves the start handle |
| `End` | `double` | `Max` | Where the range ends. Two-way bindable |
| `EndChanged` | `EventCallback<double>` | - | Raised when a key or a drag moves the end handle |
| `Value` | `MokaRange` | - | The range as one value, for `@bind-Value` inside an `EditForm`. `Start` and `End` win when they're given too |
| `ValueChanged` | `EventCallback<MokaRange>` | - | Raised with the whole range on every change |
| `Min` | `double` | `0` | The low end of the scale, and the lowest the range can start |
| `Max` | `double` | `100` | The high end of the scale, and the highest the range can end |
| `Step` | `double` | `1` | The values a handle stops at, counted from `Min`. Zero lets a drag stop anywhere, and the keys then move a hundredth of the scale |
| `Parts` | `IReadOnlyList<MokaRangePart>` | empty | Colored stretches of the track, under the range |
| `Ticks` | `IReadOnlyList<MokaRangeMark>` | empty | Thin lines under the track |
| `Pins` | `IReadOnlyList<MokaRangeMark>` | empty | Dots under the track |
| `Formatter` | `Func<double, string>?` | - | Turns a value into the text of a flag, a scale end, a mark's tooltip and what screen readers hear. Default: the number in the current culture |
| `Label` | `string?` | - | Text above the bar. Names the group, and each handle is named after it ("Pool start", "Pool end") |
| `StartLabel` | `string?` | `Label` + " start" | The start handle's name for screen readers |
| `EndLabel` | `string?` | `Label` + " end" | The end handle's name for screen readers |
| `RangeLabel` | `string?` | - | What the range is. Drawn inside it while `Labels` is on, and in the key when it's off |
| `Labels` | `bool` | `true` | Labels inside the parts and the range, on tinted tones. Off, the parts are solid and a key under the bar names them |
| `ReadOnly` | `bool` | `false` | No handles or flags: one track with the range drawn as a part in `Color` |
| `ShowScale` | `bool` | `true` | Writes the two ends of the scale under the bar |
| `TrackContent` | `RenderFragment?` | - | Drawn inside the track, behind the parts and the range, filling it: an overview of what the range picks from, such as a sparkline of the whole series. Hidden from screen readers with the rest of the picture |
| `TrackHeight` | `string?` | - | The track's height as a CSS length, such as `"2.5rem"` for an overview. The handles grow with it |
| `LargeStep` | `double?` | ten steps | How far Shift with an arrow, Page Up and Page Down move a handle, such as a day when `Step` is an hour |
| `MoveRange` | `bool` | `false` | Dragging the range between its handles moves it whole, keeping its width. The keys still move one handle at a time |
| `Color` | `MokaColor?` | `Primary` | The range's tone: the tint over the track, the handles, and the range's part when read-only |
| `HelperText` | `string?` | - | Text under the bar |
| `ErrorText` | `string?` | - | Error text under the bar, which also shows the error state. A validation message from an `EditForm` shows the same way |
| `Disabled` | `bool` | `false` | Takes the handles out of the tab order and stops keys and drags |
| `Id` | `string?` | generated | Goes on the start handle, or on the group when read-only. The label's id is built from it |
| `Class` | `string?` | - | Additional CSS classes on the group |
| `Style` | `string?` | - | Additional inline styles on the group |

Other attributes, such as `aria-label` for a bar without a label, land on the group. `Margin` goes on the field around the bar, `Padding` on the group, and `Rounded` shapes the track.

### MokaRange

`new MokaRange(Start, End)`, a value type. The bar always reports `Start` at or below `End`.

### MokaRangePart

`new MokaRangePart(Start, End, Label, Color)`.

| Member | Type | Default | Description |
|--------|------|---------|-------------|
| `Start` | `double` | | Where the part starts on the scale |
| `End` | `double` | | Where it ends. A part with no width still shows as a thin line |
| `Label` | `string?` | - | What the part is. Inside it while `Labels` is on, in the key when off |
| `Color` | `MokaColor` | `Secondary` | The part's tone: a tinted fill with matching text, or the solid color without labels |

A part that runs past the ends of the scale is cut at them, and one wholly outside it is left out.

### MokaRangeMark

`new MokaRangeMark(Value, Label, Color)`, for both ticks and pins.

| Member | Type | Default | Description |
|--------|------|---------|-------------|
| `Value` | `double` | | Where the mark sits |
| `Label` | `string?` | - | What it is. A named mark shows "Label: value" in a tooltip on hover |
| `Color` | `MokaColor?` | - | A tick is muted text color without one, a pin the accent |

## Basic

Bind `Start` and `End`. The parts show what's already taken, the ticks what's in use, the pins what's kept.

```razor
<MokaRangeBar Label="Handed out automatically"
              Min="1" Max="254"
              @bind-Start="_start" @bind-End="_end"
              Parts="_parts" Ticks="_inUse" Pins="_kept" />

<p>From @_start to @_end</p>

@code {
    private double _start = 64;
    private double _end = 143;

    private readonly MokaRangePart[] _parts =
    [
        new(20, 54, "Fixed", MokaColor.Info),
        new(158, 188, "Guests", MokaColor.Warning)
    ];

    private readonly MokaRangeMark[] _inUse = [new(70, "laptop"), new(81, "phone"), new(112, "tv")];

    private readonly MokaRangeMark[] _kept =
    [
        new(86, "printer", MokaColor.Success),
        new(120, "nas", MokaColor.Warning)
    ];
}
```

## Formatting values

The bar works in plain numbers. `Formatter` decides how a value reads, so what the numbers mean stays in the app. Here they're the last part of an IPv4 address:

```razor
<MokaRangeBar Label="Handed out automatically"
              Min="1" Max="254"
              @bind-Start="_start" @bind-End="_end"
              Formatter="Address" />

@code {
    private double _start = 64;
    private double _end = 143;

    private static string Address(double last) =>
        "192.168.1." + last.ToString(CultureInfo.InvariantCulture);
}
```

The same text is in the flags, the scale ends, the marks' tooltips and each handle's `aria-valuetext`. A drag asks the formatter for each value it passes over, once, and the flag moves on with its old text until the answer arrives. Keep the formatter quick and free of side effects.

## Read-only, with a key

`ReadOnly` draws the bar as a picture: one track, the range as one more part in `Color`, no handles or flags. With `Labels` off the parts are solid and a key under the bar names them.

```razor
<MokaRangeBar ReadOnly Labels="false" ShowScale="false"
              Min="1" Max="254" Start="64" End="143"
              RangeLabel="Handed out" Color="MokaColor.Success"
              Parts="_parts" />

@code {
    private readonly MokaRangePart[] _parts =
    [
        new(20, 54, "Fixed addresses", MokaColor.Info),
        new(158, 188, "Guests", MokaColor.Warning)
    ];
}
```

The key lists each label once, in the order the parts sit on the scale, so a part drawn in two places is named once.

## In an EditForm

`MokaRangeBar` is an input: bind `Value` to a `MokaRange` property and validation works like any other field. A change marks the field modified and validates it, and the message shows under the bar.

```razor
<EditForm Model="_lease" OnValidSubmit="Save">
    <DataAnnotationsValidator />
    <MokaRangeBar Label="Pool" Min="1" Max="254" @bind-Value="_lease.Pool" />
    <MokaButton Type="submit">Save</MokaButton>
</EditForm>

@code {
    private readonly Lease _lease = new() { Pool = new MokaRange(64, 143) };

    private sealed class Lease
    {
        [WideEnough]
        public MokaRange Pool { get; set; }
    }

    private sealed class WideEnoughAttribute() : ValidationAttribute("Keep at least 20 addresses in the pool.")
    {
        public override bool IsValid(object? value) => value is MokaRange range && range.End - range.Start >= 20;
    }

    private void Save() { }
}
```

## Keyboard and pointer

Each handle is a slider with one tab stop.

| Key | Does |
|-----|------|
| Right, Up | One `Step` up |
| Left, Down | One `Step` down |
| Shift with an arrow, Page Up, Page Down | `LargeStep`, else ten steps |
| Home | The start handle to `Min`, the end handle to the start |
| End | The start handle to the end, the end handle to `Max` |

Keys with Ctrl, Alt or Meta stay with the browser. A drag runs in the browser: the handle, its flag and the range follow the pointer, and the bar reports once, when the handle is let go, so nothing waits on the server while dragging. Two handles on the same value split on the first move: dragging left takes the start, right the end. The start never passes the end, and neither leaves the scale. With `MoveRange`, a drag that starts on the range moves both ends together and stops at the ends of the scale.

## An overview behind the range

A chart's range strip is this bar with the whole series drawn in the track. `TrackContent` fills the track behind the range, `TrackHeight` makes room for it, `LargeStep` makes Shift move a day while the arrows move an hour, and `MoveRange` lets the window be dragged whole.

```blazor-preview
<MokaRangeBar Label="Window" Min="0" Max="168" Step="1" LargeStep="24" MoveRange="true" TrackHeight="2.5rem"
              @bind-Start="_from" @bind-End="_to" ShowScale="false" Formatter="Hour">
    <TrackContent>
        <MokaSparkline Values="_week" ShowLastPoint="false" Style="width: 100%; height: 100%;" />
    </TrackContent>
</MokaRangeBar>

@code {
    private double _from = 96;
    private double _to = 120;

    private readonly double[] _week = [.. Enumerable.Range(0, 169).Select(h => 400 + 200 * Math.Sin(h / 6.0))];

    private static string Hour(double hour) => $"Day {(int)hour / 24 + 1}, {(int)hour % 24:00}:00";
}
```

## Accessibility

The bar is a `group` named by `Label` (or your `aria-label`). Each handle is a `slider` with `aria-valuemin`, `aria-valuemax`, `aria-valuenow` and the formatted `aria-valuetext`; the start's maximum is the end, and the end's minimum is the start. The picture is hidden from screen readers, and a list after it says what's where: each labelled part with its start and end ("Fixed: 20 to 54"), the range if it has a `RangeLabel`, and each named mark. With `Labels` off, that list is the key you see.

## Layout

The bar takes the width of its container and measures itself in container units, which is how the flags stay inside it: the start flag ends at its handle and the end flag starts at its own, so two close handles never put their flags on top of each other. Inside a row that sizes to its content, give it a width. The editable bar leaves half a handle's width at each side, so a handle at either end stays inside the field.
