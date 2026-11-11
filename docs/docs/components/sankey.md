---
title: Sankey
description: Flows from sources to destinations, each as wide as its volume.
order: 61
---

# Sankey

`MokaSankey` draws flows from sources to destinations, each as wide as its volume, such as traffic from the WAN to
the networks behind it. It reads well at card size with three or four nodes a side, down to a phone. Past that the
ribbons tangle and the names collide, so for anything bigger a table or grouped bars say it better; the chart folds
the smallest nodes into "Other" rather than tangle.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Links` | `IReadOnlyList<MokaSankeyLink>?` | -- | The flows: `From` and `To` node keys and a `Value` |
| `Nodes` | `IReadOnlyList<MokaSankeyNode>?` | -- | Names, colors and columns for the keys the links use. A key without one shows as itself |
| `MaxNodesPerSide` | `int` | `6` | The most nodes a column shows. Past it the smallest fold into "Other (n)". Four reads best |
| `ShareText` | `Func<string, string, string>?` | -- | Writes a share for the tooltip. Defaults to "62% of WAN" |
| `ToText` | `string` | `"to"` | The word between a flow's ends, "WAN to LAN" |
| `Formatter` | `Func<double, string>?` | -- | Writes a value, such as "12.4 GB" |
| `Height` | `string?` | -- | A fixed height. Without it the height follows the busiest column |

It also takes the parameters every chart shares (`Title`, `Subtitle`, `Label`, `Look`, `Colors`, `Highlight`,
`HiddenSeries`, `Tooltip` and the rest). The legend lists the sources; hiding one takes its flows away.

### MokaSankeyLink

| Name | Type | Description |
|------|------|-------------|
| `From`, `To` | `string` | The keys of the nodes it leaves and reaches |
| `Value` | `double` | How much flowed. Zero, less, or not a number draws nothing |
| `Color` | `MokaColor?` | The color of this flow, such as `MokaColor.Error` for one that needs a look. Null takes the color of the node it leaves |
| `ColorValue` | `string?` | A CSS color or token for this flow. Wins over `Color`; anything that isn't a color is ignored |

### MokaSankeyNode

| Name | Type | Description |
|------|------|-------------|
| `Key` | `string` | The key its links use |
| `Name` | `string` | What it is called |
| `Color` | `MokaColor?` | The color of the node and the flows leaving it |
| `ColorValue` | `string?` | A CSS color or token for it |
| `Column` | `int?` | The column it sits in, from 0. Null places it by the longest path to it |

## Where requests went

```blazor-preview
@code {
    MokaSankeyLink[] _links =
    [
        new("web", "cache", 3.4), new("web", "db", 1.9), new("web", "storage", 0.5),
        new("api", "cache", 1.1), new("api", "db", 1.5), new("api", "storage", 0.5),
        new("jobs", "cache", 0.1), new("jobs", "db", 0.4), new("jobs", "storage", 0.6)
    ];

    MokaSankeyNode[] _nodes =
    [
        new("web", "Web"), new("api", "API"), new("jobs", "Jobs"),
        new("cache", "Cache"), new("db", "Database"), new("storage", "Storage")
    ];
}

<MokaSankey Links="_links" Nodes="_nodes" Title="Where requests went" />
```

## How it is laid out

- Nodes sit in columns by the longest path to them; a node nothing leaves moves to the last column. A flow that would
  close a cycle is dropped, and flows between the same two nodes are added together.
- In each column the largest node is at the top. Flows leave a node in the order of where they arrive, so they cross
  as little as they can.
- Sources take the palette in the legend's order. A node further on takes the color of the source behind its largest
  inflow, and the last column stays gray, so the eye follows each source to where its flows end. A link's own `Color`
  or `ColorValue` paints just that flow; a highlight that mutes the node it leaves mutes it too. Flows between the same
  two nodes, and flows folded into "Other", keep a color only when every one of them gave the same.
- The flows are filled paths in a drawing that stretches to the box, the nodes and their names are HTML, and nothing
  needs script. Each flow's tooltip reads "Web to Cache: 3.4K, 59% of Web".
- The outer columns' names sit beside the drawing, in room kept for the longest of them up to a third of the chart.
  Under 26rem of chart width they move inside their columns, over the flows, on a wash of the surface.

## Flows in their own colors, and what a click says

Here the camera's flow to the cloud is amber and the cloud's flow to one country red, while the rest follow their
sources. `OnPointClick` and `ActiveChanged` carry a `MokaChartPoint`: for a flow, `SeriesKey` is the node it leaves and
`ToKey` (with `ToName`) the node it reaches; for a node, `SeriesKey` is the node and `ToKey` is null, so the two can be
told apart. `Category` still holds the heading the tooltip writes, "camera to cloud".

```razor
<MokaSankey Links="_traffic" Formatter="Gb" OnPointClick="Picked" />

@code {
    MokaSankeyLink[] _traffic =
    [
        new("laptop", "web", 14), new("tv", "video", 22), new("phone", "web", 5),
        new("camera", "cloud", 3) { Color = MokaColor.Warning },
        new("web", "US", 12), new("video", "US", 28), new("cloud", "CN", 4) { ColorValue = "var(--moka-color-error)" }
    ];

    void Picked(MokaChartPoint point)
    {
        if (point.ToKey is { } to)
        {
            // A flow from point.SeriesKey to to.
        }
        else
        {
            // The node point.SeriesKey.
        }
    }
}
```
