---
title: Bytes
description: A byte count as text, such as 1.5 GB, or a rate, such as 12 MB/s.
order: 154
---

# Bytes

`MokaBytes` writes a byte count in the largest unit that fits, "1.5 GB", or as a rate, "12 MB/s". Digits are tabular, so a value that keeps changing doesn't jitter. It takes the size and color of the text around it.

`MokaJobProgress` writes sizes through the same code, so the two always read alike.

## Setup

```razor
@using Moka.Red.Primitives.Bytes
```

`MokaByteUnits` needs `@using Moka.Red.Feedback.JobProgress`. The enum started out on `MokaJobProgress` and kept its namespace when it moved to `Moka.Red.Core`, so it's there with any Moka.Red package.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `long?` | - | The byte count. A negative count keeps its sign |
| `PerSecond` | `bool` | `false` | Reads the value as a rate, adding "/s" |
| `Units` | `MokaByteUnits` | `Decimal` | `Decimal` counts in powers of 1000 (KB, MB, GB, TB), `Binary` in powers of 1024 (KiB, MiB, GiB, TiB) |
| `Decimals` | `int?` | - | How many digits follow the point, always that many, from 0 to 15. Null shows one under 100 and none from there up, dropping a zero |
| `Formatter` | `Func<long, string>?` | - | Writes the count, unit included. Wins over `Units` and `Decimals` |
| `EmptyText` | `string` | `"-"` | Shown when `Value` is null |
| `ShowTooltip` | `bool` | `false` | Shows the exact count on hover, such as "1,536,000 bytes" |
| `ExactText` | `Func<long, string>?` | - | Writes the tooltip's text in place of "1,536,000 bytes" |
| `TooltipPosition` | `MokaTooltipPosition` | `Top` | Where the tooltip opens |
| `Size` | `MokaSize` | `Md` | `Md` follows the text around it, and the others use the type scale |
| `SizeValue` | `string?` | - | A font size of its own, such as `"18px"`. Wins over `Size` |
| `Color` | `MokaColor?` | - | The text color. Null follows the text around it |

`Class`, `Style`, `Id`, other attributes, `Margin`, `Padding` and `Rounded` land on the text.

## Basic

```razor
<MokaBytes Value="1500000000" />                @* 1.5 GB *@
<MokaBytes Value="12000000" PerSecond="true" /> @* 12 MB/s *@
<MokaBytes Value="@file.Length" />
```

Units run up to exabytes, and a value that rounds up moves to the next unit, so 999,500 bytes reads "1 MB". Numbers follow the current culture.

## Binary units

```razor
<MokaBytes Value="1073741824" Units="MokaByteUnits.Binary" /> @* 1 GiB *@
```

## A steady width

By default the digit after the point comes and goes, "1.5 GB" and then "2 GB". In a column or a live readout, `Decimals` keeps it. A count in plain bytes never gets decimals, so 512 bytes stays "512 B".

```razor
<MokaBytes Value="@_received" Decimals="1" PerSecond="true" />
```

## Your own wording

`Formatter` gets the count and returns it written out, unit included. `PerSecond` still adds "/s" after it. Leave `PerSecond` off to write the whole rate yourself.

```razor
<MokaBytes Value="@size" Formatter="@(bytes => $"{bytes / 1e6:0.0} megabytes")" />
```

## Nothing to show

A null `Value` shows a hyphen, or whatever `EmptyText` says.

```razor
<MokaBytes Value="@device.Quota" EmptyText="No limit" />
```

## The exact count on hover

The tooltip is off by default, because a long table doesn't need one per row. `ExactText` replaces its words, and screen readers get the same text as a description.

```razor
<MokaBytes Value="1536000" ShowTooltip="true" />
<MokaBytes Value="1536000" ShowTooltip="true" ExactText="@(bytes => $"{bytes:N0} octets")" />
```

## In a string

`MokaByteFormat` in `Moka.Red.Core.Utilities` is the same code as a static call.

```csharp
string size = MokaByteFormat.Format(1_500_000_000);                        // "1.5 GB"
string rate = MokaByteFormat.Format(12_000_000, perSecond: true);          // "12 MB/s"
string binary = MokaByteFormat.Format(1_048_576, MokaByteUnits.Binary, 2); // "1.00 MiB"

(string number, string unit) = MokaByteFormat.Parts(240_000_000);          // ("240", "MB")
```
