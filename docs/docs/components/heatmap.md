---
title: Heatmap
description: A calendar heatmap, the contribution-graph view: one square per day, colored by how much happened.
order: 59
---

# Heatmap

`MokaHeatmap` lays a calendar out weeks across and days of the week down, coloring each day by
how much happened then - the contribution-graph view. The scale is the theme's primary mixed
toward transparent, so the busiest day is always the deepest.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Data` | `IReadOnlyDictionary<DateTime, int>?` | -- | How many days each date had. Matched by day; time is ignored |
| `Weeks` | `int` | `53` | Number of week columns to show |
| `EndDate` | `DateTime?` | -- | The last day shown. Defaults to today. The grid still ends on that week's Sunday |
| `MaxValue` | `int?` | -- | The count a day must reach for the deepest color. Null = the largest count in `Data` |
| `ShowMonthLabels` | `bool` | `true` | Whether month names run along the top |
| `ShowWeekdayLabels` | `bool` | `true` | Whether weekday names run down the side |
| `Label` | `string?` | -- | What the heatmap shows, for assistive technology |

## Basic

```blazor-preview
@code {
    Dictionary<DateTime, int> BuildData()
    {
        var random = new Random(7);
        var data = new Dictionary<DateTime, int>();
        for (DateTime day = DateTime.Today.AddDays(-364); day <= DateTime.Today; day = day.AddDays(1))
        {
            if (random.Next(100) < 65)
                data[day] = random.Next(1, 14);
        }
        return data;
    }
}

<MokaHeatmap Data="BuildData()" Weeks="26" />
```

## A fixed scale

Left alone, the busiest day in the data is the deepest. `MaxValue` pins the scale instead, so two
heatmaps beside each other read on the same one:

```razor
<MokaHeatmap Data="_lastWeek" Weeks="8" MaxValue="20" />
```

The grid runs Monday to Sunday and always includes the whole week the end day sits in. Days past
the end are drawn faint and carry no count, and every cell carries a native tooltip with its
count and date. Month and weekday names follow the current culture.
