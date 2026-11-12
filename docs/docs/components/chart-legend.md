---
title: Chart Legend
description: The names and colors of a chart's series, as names or as toggles that hide and isolate, in a chart or as one row for a whole dashboard.
order: 159
---

# Chart Legend

Every chart draws its legend with `MokaChartLegend`. On its own, it is one row of toggles for a whole dashboard: turn a series off and it leaves every chart that shares the set, and the rest keep their colors, because a color follows the series, not its position.

## Setup

```razor
@using Moka.Red.Data.Chart
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Items` | `IReadOnlyList<MokaChartLegendItem>?` | - | The entries, in order. An entry whose key repeats is left out |
| `Hidden` | `IReadOnlyCollection<string>?` | - | The keys of the hidden series. Two-way bindable; pass a new collection to change it from outside |
| `HiddenChanged` | `EventCallback<IReadOnlyCollection<string>>` | - | Raised with the new set whenever an entry hides or shows a series |
| `Actions` | `MokaChartLegendActions` | `None` | What the entries do: `Highlight`, `Hide`, `Isolate`, or `All`. Isolating turns hiding on too |
| `Key` | `MokaChartLegendKey` | `Dot` | A dot, the series' own mark (`Mark`: a short line, a box or a dot), or the whole entry as a `Pill` |
| `Orientation` | `MokaDirection` | `Row` | A row that wraps, or a column |
| `Title` | `string?` | - | A small label before the entries, such as "Services". It names the group when `AriaLabel` isn't set |
| `AriaLabel` | `string?` | `MokaChartText.Series` ("Series") | The group's name for screen readers |
| `FoldAbove` | `int` | `6` | With more entries than this, a row keeps to one line and folds what doesn't fit behind a "+N" button. Zero lets the entries wrap |
| `MoreLabel` | `Func<int, string>?` | `MokaChartText.MoreSeries` ("Show 3 more series") | Names the "+N" button from how many entries it holds |
| `LessLabel` | `string?` | `MokaChartText.FewerSeries` ("Show fewer series") | Names the button that folds the legend back |
| `Dense` | `bool` | `false` | Monospace names in small capitals with their values beside them, for a title row |
| `Texture` | `bool` | `false` | Hatches box keys, as a chart with texture hatches its bars |
| `SyncGroup` | `string?` | - | With `Highlight`, pointing at an entry dims the other series in the charts of this group |
| `Text` | `MokaChartText?` | English | The words: Show all and the sentences a screen reader hears |
| `ItemClass` | `string?` | - | Classes added to every entry |
| `Class`, `Style`, `Id` | | | On the legend's root. `Margin`, `Padding` and `Rounded` land there too |

### MokaChartLegendItem

`new MokaChartLegendItem(Key, Name) { ... }`

| Member | Type | Default | Description |
|--------|------|---------|-------------|
| `Key` | `string` | | What hiding and highlighting go by. Use the chart series' key (its name, unless it sets one) |
| `Name` | `string` | | What the entry reads |
| `Slot` | `int?` | its position | The palette slot, counting from 1. Give a series the same slot everywhere so it keeps its color |
| `Color` | `MokaColor?` | - | A theme color that wins over the slot |
| `ColorValue` | `string?` | - | A CSS color or token that wins over `Color`. Anything that isn't a color is ignored |
| `Value` | `string?` | - | The latest value, in mono beside the name |
| `Mark` | `MokaChartLegendMark` | `Dot` | The key's shape when `Key` is `Mark`: `Dot`, `Line` or `Box`. `Dash`, a short dashed line for a line drawn dashed such as a comparison, shows whatever the `Key` |

## Names

Without actions the entries are names, not controls.

```blazor-preview
<MokaChartLegend Items="_items" />

@code {
    private readonly MokaChartLegendItem[] _items =
    [
        new("lan", "LAN") { Value = "412 Mbps" },
        new("wifi", "Wi-Fi") { Value = "236 Mbps" },
        new("guest", "Guest") { Value = "38 Mbps" }
    ];
}
```

## Toggles for a dashboard

With `Hide`, each entry is a toggle button, pressed while its series shows. A click hides or shows it. With `Isolate`, a double click, an Alt click or Alt Enter shows that series alone, and doing it again brings the rest back. A Show all button appears while anything is hidden. A hidden entry keeps its color as an outline, so it can be found again.

Bind `Hidden` and pass the same set to each chart's `HiddenSeries`:

```razor
<MokaChartLegend Items="_zones" @bind-Hidden="_hidden" Actions="MokaChartLegendActions.All"
                 Key="MokaChartLegendKey.Mark" Title="Zones" SyncGroup="network" />

<MokaLineChart Series="_traffic" HiddenSeries="_hidden" ShowLegend="false" SyncGroup="network" />
<MokaBarChart Series="_daily" HiddenSeries="_hidden" ShowLegend="false" />

@code {
    private IReadOnlyCollection<string>? _hidden;

    private readonly MokaChartLegendItem[] _zones =
    [
        new("LAN", "LAN") { Slot = 1, Mark = MokaChartLegendMark.Box },
        new("Wi-Fi", "Wi-Fi") { Slot = 2, Mark = MokaChartLegendMark.Box },
        new("Guest", "Guest") { Slot = 3, Mark = MokaChartLegendMark.Box }
    ];
}
```

## Many series

A legend with more entries than `FoldAbove` keeps to one row. What doesn't fit drops out whole, and a "+N" button opens the legend onto more rows with the same toggles in their places; it folds back once everything fits again. Folding was chosen over a menu so that every entry stays the one toggle button it is, with its color, its pressed state and its keys, and over scrolling so that the chart keeps its height until someone asks for more. Without an interactive render the entries simply wrap. A legend beside the plot (`Orientation` `Column`) never folds; it scrolls inside the plot's height instead.

## Keyboard and screen readers

Each toggle is a button in the tab order. Enter and Space hide or show, Alt Enter isolates. The legend is a `group` named by `AriaLabel`, and every change is said in a polite status region:

| Action | Said |
|--------|------|
| Hide | "LAN hidden. 2 of 3 series shown." |
| Show | "LAN shown. 3 of 3 series shown." |
| Isolate | "Showing LAN only. Do it again, or press Show all, to bring the rest back." |
| Show all, or isolate again | "All 3 series shown." |

After Show all, focus moves to the first entry. Every sentence comes from `MokaChartText`, so it can be translated.

## In a chart

A chart places its legend with `Legend` (`Bottom`, `Top`, `Right`, `TitleRow`, `None`), picks its key with `LegendKey` and its actions with `LegendActions`; each defaults to what the chart's look says. `MokaChartControls.Legend` puts it in the toolbar row instead. See [Chart Frame](chart-frame).
