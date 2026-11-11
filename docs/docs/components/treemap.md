---
title: Treemap
description: A hierarchy of parts as nested tiles sized by value, such as disk use by folder.
order: 60
---

# Treemap

`MokaTreemap` lays a hierarchy out as tiles whose areas are their values, for a whole with many parts, like disk use
by folder. Tiles are tinted rather than solid, colored by their top-level part, so a page of them stays calm, and each
tile shows its name only where it fits. The full name is in the tooltip and the table.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Nodes` | `IReadOnlyList<MokaTreemapNode>?` | -- | The top level of the hierarchy |
| `Depth` | `int` | `2` | How many levels are drawn as tiles. Deeper parts add up into their parent's tile |
| `MaxTiles` | `int` | `150` | The most tiles drawn. Past it the smallest parts of a parent fold into one "Other" tile |
| `OnNodeClick` | `EventCallback<MokaTreemapNode>` | -- | Raised with the part a tile stands for. With a listener every tile is a button |
| `ShareText` | `Func<string, string, string>?` | -- | Writes a share for the tooltip from the percent and the whole's name. Defaults to "18% of Photos" |
| `AllText` | `string` | `"all"` | What the whole is called in the tooltip's last share |
| `Formatter` | `Func<double, string>?` | -- | Writes a value, such as "12.4 GB" |
| `Height` | `string?` | -- | A fixed height, a CSS length or a `calc()`, `var()`, `min()`, `max()` or `clamp()` of lengths. Without it the tiles keep a 3 by 2 box, no taller than `Size` allows |
| `Size` | `MokaSize` | `Md` | The tallest the box gets |

It also takes the parameters every chart shares (`Title`, `Subtitle`, `Label`, `Look`, `Colors`, `Highlight`,
`HiddenSeries`, `Tooltip` and the rest). The legend lists the top level; hiding a top-level part takes its tiles away.

### MokaTreemapNode

| Name | Type | Description |
|------|------|-------------|
| `Name` | `string` | What it is called |
| `Value` | `double` | How big it is. Ignored when it has children, whose values add up to it |
| `Children` | `IReadOnlyList<MokaTreemapNode>?` | The parts inside it |
| `Color` | `MokaColor?` | The tint of its tiles. Top level only; the rest take their top part's |
| `ColorValue` | `string?` | A CSS color or token for the tint. Top level only |
| `Key` | `string?` | Its identity for hiding and highlighting. Null uses `Name` |
| `Detail` | `string?` | A line for the tooltip, such as "3,412 files" |

## Disk use by folder

```blazor-preview
@code {
    MokaTreemapNode[] _disk =
    [
        new("Media", 0) { Children = [new("Photos", 412), new("Videos", 388), new("Music", 64)] },
        new("Backups", 0) { Children = [new("Laptop", 236), new("Server", 142), new("Phone", 98)] },
        new("Work", 0) { Children = [new("Projects", 121), new("Mail", 38), new("Documents", 26)] },
        new("System", 95)
    ];

    static string Gb(double value) => $"{value:0} GB";
}

<MokaTreemap Nodes="_disk" Formatter="Gb" Title="Disk use by folder" />
```

The tooltip of a tile reads "Media / Photos: 412 GB, 48% of Media, 25% of all".

## Drilling in

`OnNodeClick` makes every tile a button and raises the part it stands for, so the app can show that part's children
next. The chart has no state of its own for it.

```razor
<MokaTreemap Nodes="@(_inside?.Children ?? _disk)" OnNodeClick="node => _inside = node.Children is { Count: > 0 } ? node : _inside" />
```

## Many parts

The layout is squarified (Bruls, Huizing and van Wijk) in C# into percentages, so it costs one element per tile and
no script. Past `MaxTiles` the smallest tiles fold into an "Other (n)" tile in their own parent, smallest first, so a
folder with ten thousand files still draws at most 150 tiles. Each tile is a size container: a name shows only when
the tile is wide enough for it, and the value only when the tile is tall enough for two lines.
