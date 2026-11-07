---
title: Flag
description: A country's flag from its two-letter code, the same on every system, with a globe when there is no code.
order: 150
---

# Flag

`MokaFlag` draws a country's flag from its ISO 3166-1 code, such as `"SE"` or `"us"`. It's the flag emoji, drawn in the Twemoji Country Flags font, so it looks the same on Windows (whose emoji font has no flags) as on macOS, iOS and Android. A missing code, or one with no flag, shows a globe.

## Setup

```razor
@using Moka.Red.Primitives.Flag
```

The font comes with `moka-fonts.css`, which `MokaThemeProvider` links unless `LoadFonts` is off. Its `unicode-range` covers only the flag characters, so the browser fetches it (78 KB) the first time a flag is on screen and never for ordinary text. With `LoadFonts="false"`, the system's emoji font draws the flags, and Windows shows two letters.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Code` | `string?` | -- | The two-letter country code, in any case. `"GB-ENG"`, `"GB-SCT"` and `"GB-WLS"` draw England, Scotland and Wales |
| `Name` | `string?` | -- | The country's name, read out by screen readers and shown in the tooltip. Null uses the code |
| `UnknownName` | `string` | `"Unknown country"` | What the globe is called when there is no code |
| `ShowTooltip` | `bool` | `false` | Whether a tooltip shows the name on hover |
| `TooltipPosition` | `MokaTooltipPosition` | `Top` | Where the tooltip opens |
| `Size` | `MokaSize` | `Md` | How big it is next to the text around it: `Xs` 0.9em, `Sm` 1em, `Md` 1.15em, `Lg` 1.5em |
| `SizeValue` | `string?` | -- | A size of its own, such as `"32px"`. Wins over `Size` |

## Inline with text

The flag follows the size of the text it sits in.

```razor
<span><MokaFlag Code="se" Name="Sweden" /> Stockholm, 12 ms</span>
<span><MokaFlag Code="jp" Name="Japan" /> Tokyo, 241 ms</span>
<span><MokaFlag /> Unknown</span>
```

## With a tooltip

```razor
<MokaFlag Code="DE" Name="Germany" ShowTooltip="true" Size="MokaSize.Lg" />
<MokaFlag Code="GB-SCT" Name="Scotland" ShowTooltip="true" Size="MokaSize.Lg" />
<MokaFlag Code="EU" Name="European Union" ShowTooltip="true" SizeValue="32px" />
```

## Which codes draw a flag

The 258 regions Unicode has a flag for: every ISO 3166-1 code, plus `EU`, `UN` and the few exceptional reservations emoji fonts carry (`AC`, `CP`, `DG`, `EA`, `IC`, `TA`, `XK`). Any other pair of letters would draw as two boxed letters, so it gets the globe. `MokaFlag.ToEmoji(code)` gives the emoji for a code, or null, for use outside the component.

## Screen readers

The flag is an image (`role="img"`) named by `Name`, else the code in capitals, else `UnknownName`. It has no `title`, so the browser's own hint never shows beside the tooltip.

## Credits

The flag artwork is from [Twemoji](https://github.com/twitter/twemoji), Copyright 2019 Twitter, Inc and other contributors, licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). The font is TalkJS's subset of Mozilla's Twemoji Mozilla, from [country-flag-emoji-polyfill](https://github.com/talkjs/country-flag-emoji-polyfill) (MIT), shipped unchanged. Its license and notice sit next to it in the package, under `_content/Moka.Red.Core/fonts/`.
