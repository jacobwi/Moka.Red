---
title: Relative Time
description: A moment as words, such as 3 minutes ago or in 2 hours, that keeps itself current.
order: 155
---

# Relative Time

`MokaRelativeTime` shows a moment as "3 minutes ago" or "in 2 hours" and keeps the text current. It renders a `<time>` element with a machine-readable `datetime`, and the full date and time show on hover. It takes the size and color of the text around it.

The words are English. To use another language, set `Formatter`.

## Setup

```razor
@using Moka.Red.Primitives.RelativeTime
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `DateTimeOffset?` | - | The moment to describe |
| `Live` | `bool` | `true` | Keeps the text current as time passes. Off, it's only worked out when the parent renders |
| `AbsoluteAfter` | `TimeSpan?` | - | Once the value is this far away, in either direction, a date takes the place of the words |
| `Format` | `string` | `"d"` | The format of that date. `"d"` is the culture's short date |
| `Formatter` | `Func<TimeSpan, string>?` | - | Writes the words in place of the English ones. Gets how long ago the value was, negative for one still to come |
| `EmptyText` | `string` | `"-"` | Shown when `Value` is null |
| `ShowTooltip` | `bool` | `true` | Shows the full date and time on hover |
| `TooltipFormat` | `string` | `"G"` | The format of the tooltip. `"G"` is the culture's date and time with seconds |
| `TooltipPosition` | `MokaTooltipPosition` | `Top` | Where the tooltip opens |
| `Size` | `MokaSize` | `Md` | `Md` follows the text around it, and the others use the type scale |
| `SizeValue` | `string?` | - | A font size of its own, such as `"18px"`. Wins over `Size` |
| `Color` | `MokaColor?` | - | The text color. Null follows the text around it |

`Class`, `Style`, `Id`, other attributes, `Margin`, `Padding` and `Rounded` land on the `<time>` element.

## Basic

```razor
<MokaRelativeTime Value="@comment.CreatedAt" />
<MokaRelativeTime Value="@job.StartsAt" />
```

| How far away | Reads |
|--------------|-------|
| Under a minute, either way | just now |
| Under an hour | 3 minutes ago, in 3 minutes |
| Under a day | 2 hours ago, in 2 hours |
| Under a week | 4 days ago, in 4 days |
| Under 30 days | 2 weeks ago, in 2 weeks |
| Under 365 days | 5 months ago, in 5 months |
| More | 2 years ago, in 2 years |

Counts are whole units, rounded down. A month is 30 days and a year 365, so the words are near enough and the tooltip has the exact date.

## A date for older values

Past `AbsoluteAfter` a date shows instead of the words, whichever side of now the value is on. `Format` takes any .NET date format, and `TooltipFormat` sets the fuller date in the tooltip.

```razor
<MokaRelativeTime Value="@post.PublishedAt"
                  AbsoluteAfter="TimeSpan.FromDays(7)"
                  Format="MMM d, yyyy" />
```

## Time zones

Dates show in the offset the `DateTimeOffset` carries. On Blazor Server the server's local time is rarely the reader's, so convert the value first if it matters which clock the date is read on.

```razor
<MokaRelativeTime Value="@TimeZoneInfo.ConvertTime(entry.At, _readerZone)" />
```

## Other languages or wording

`Formatter` replaces all the words, "just now" included. It gets how long ago the value was: positive for the past, negative for a value still to come. A date past `AbsoluteAfter` is written by `Format`, not by this.

```razor
<MokaRelativeTime Value="@item.At" Formatter="@Swedish" />

@code {
	private static string Swedish(TimeSpan age)
	{
		int minutes = (int)Math.Abs(age.TotalMinutes);
		return age < TimeSpan.Zero ? $"om {minutes} min" : $"för {minutes} min sedan";
	}
}
```

## Nothing to show

A null `Value` shows a hyphen, or whatever `EmptyText` says, in a plain `<span>`, since a `<time>` needs a time.

```razor
<MokaRelativeTime Value="@device.LastSeen" EmptyText="Never" />
```

## How it stays current

Each instance sets one timer for the moment its own text next changes: the next minute, the next hour, and so on. "Just now" doesn't tick, and a value from last year wakes rarely, so a table can hold thousands of them. `Formatter` is asked again at those same moments.

There's no timer on a static or prerendered render, and the text is whatever it was when the page was drawn. `Live="false"` turns it off too. The text isn't a live region, so a screen reader doesn't interrupt anyone when it changes.

It reads the clock from a `TimeProvider` registered in the app's services, or the system clock when there isn't one, so a test can move time itself.

A long table that doesn't need the date on hover can set `ShowTooltip="false"`, which saves a tooltip per row.
