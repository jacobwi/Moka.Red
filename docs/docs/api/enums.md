---
title: Enums
description: Reference for all Moka.Red enumerations and their CSS mappings.
---

# Enums

All Moka.Red enums live in `Moka.Red.Core.Enums`. `MokaEnumHelpers` converts them to CSS
values or kebab-case class segments via static `ToCssValue` / `ToCssClass` methods.

---

## MokaSize

Controls the size of components (icon dimensions, input height, font size, etc.).
Maps to pixels via `MokaEnumHelpers.ToPixels` and to font-size tokens via
`MokaEnumHelpers.ToFontSize`.

| Value | Pixel mapping | Font-size token |
|-------|--------------|----------------|
| `Xs` | 14 px | `--moka-font-size-xs` |
| `Sm` | 16 px | `--moka-font-size-sm` |
| `Md` | 20 px | `--moka-font-size-base` |
| `Lg` | 24 px | `--moka-font-size-lg` |

Default for `MokaVisualComponentBase.Size` is `Md`.

```razor
<MokaButton Size="MokaSize.Sm">Small</MokaButton>
<MokaIcon Size="MokaSize.Lg" />
```

---

## MokaColor

Semantic colors sourced from the active `MokaTheme`. Resolves to `--moka-color-*` CSS
custom properties defined by `MokaThemeProvider`.

| Value | CSS class segment | Meaning |
|-------|------------------|---------|
| `Primary` | `primary` | Brand color (default `#d32f2f` light / `#ef5350` dark). |
| `Secondary` | `secondary` | Secondary accent. |
| `Error` | `error` | Destructive actions, validation errors. |
| `Warning` | `warning` | Caution / recoverable states. |
| `Success` | `success` | Positive / completion states. |
| `Info` | `info` | Informational messaging. |
| `Surface` | `surface` | Neutral surface / subdued. |

```razor
<MokaAlert Color="MokaColor.Warning">Check your input.</MokaAlert>
<MokaBadge Color="MokaColor.Success">Active</MokaBadge>
```

---

## MokaVariant

Controls the visual rendering style of a component.

| Value | CSS class segment | Appearance |
|-------|------------------|-----------|
| `Filled` | `filled` | Solid background with contrasting text (default). |
| `Outlined` | `outlined` | Transparent background with a visible border. |
| `Text` | `text` | No background or border — text only. |
| `Soft` | `soft` | Subtle filled background at low opacity. |

```razor
<MokaButton Variant="MokaVariant.Outlined" Color="MokaColor.Primary">Cancel</MokaButton>
<MokaButton Variant="MokaVariant.Filled"   Color="MokaColor.Primary">Save</MokaButton>
```

---

## MokaSpacingScale

Spacing values for margin, padding, and gap. Each value resolves to a
`--moka-spacing-*` CSS custom property (or `0` for `None`).

| Value | CSS value |
|-------|-----------|
| `None` | `0` |
| `Xxs` | `var(--moka-spacing-xxs)` |
| `Xs` | `var(--moka-spacing-xs)` |
| `Sm` | `var(--moka-spacing-sm)` |
| `Md` | `var(--moka-spacing-md)` |
| `Lg` | `var(--moka-spacing-lg)` |
| `Xl` | `var(--moka-spacing-xl)` |
| `Xxl` | `var(--moka-spacing-xxl)` |

```razor
<MokaGrid Gap="MokaSpacingScale.Md" Columns="3">...</MokaGrid>
<MokaCard Padding="MokaSpacingScale.Lg">...</MokaCard>
```

---

## MokaDirection

Flex direction for `MokaFlexbox` and other stack-like components.

| Value | CSS `flex-direction` |
|-------|---------------------|
| `Column` | `column` (default) |
| `Row` | `row` |
| `ColumnReverse` | `column-reverse` |
| `RowReverse` | `row-reverse` |

---

## MokaAlign

`align-items` values for flex and grid layouts.

| Value | CSS value |
|-------|-----------|
| `Start` | `flex-start` |
| `Center` | `center` |
| `End` | `flex-end` |
| `Stretch` | `stretch` |
| `Baseline` | `baseline` |

Used by `MokaFlexbox.Align`, `MokaGrid.AlignItems`, and `MokaBreakpoint.Align`.

---

## MokaJustify

`justify-content` / `justify-items` values for flex and grid layouts.

| Value | CSS value |
|-------|-----------|
| `Start` | `flex-start` |
| `Center` | `center` |
| `End` | `flex-end` |
| `SpaceBetween` | `space-between` |
| `SpaceAround` | `space-around` |
| `SpaceEvenly` | `space-evenly` |

---

## MokaRounding

Border radius scale. Resolves to `--moka-radius-*` CSS custom properties.

| Value | CSS value | Approx. radius |
|-------|-----------|---------------|
| `None` | `0` | Sharp corners |
| `Sm` | `var(--moka-radius-sm)` | 0.125 rem |
| `Md` | `var(--moka-radius-md)` | 0.25 rem (default) |
| `Lg` | `var(--moka-radius-lg)` | 0.375 rem |
| `Xl` | `var(--moka-radius-xl)` | 0.5 rem |
| `Full` | `var(--moka-radius-full)` | 9999 px (pill) |

```razor
<MokaChip Rounded="MokaRounding.Full">Label</MokaChip>
<MokaCard Rounded="MokaRounding.Lg">...</MokaCard>
```

---

## MokaFontWeight

Font weight options backed by `--moka-font-weight-*` CSS tokens.

| Value | CSS value |
|-------|-----------|
| `Light` | `var(--moka-font-weight-light)` |
| `Normal` | `var(--moka-font-weight-normal)` |
| `Medium` | `var(--moka-font-weight-medium)` |
| `Semibold` | `var(--moka-font-weight-semibold)` |
| `Bold` | `var(--moka-font-weight-bold)` |

---

## MokaTextAlign

| Value | CSS `text-align` |
|-------|-----------------|
| `Left` | `left` |
| `Center` | `center` |
| `Right` | `right` |
| `Justify` | `justify` |

---

## MokaBreakpoint

`MokaBreakpoint` is a record (not an enum) used as the element type for `MokaGrid.Breakpoints`
and `MokaFlexbox.Breakpoints`. Each instance applies overrides above a given `MinWidth`.

```csharp
public sealed record MokaBreakpoint
{
    public required string MinWidth { get; init; }  // e.g. "768px"

    // Grid overrides
    public int?            Columns      { get; init; }
    public string?         ColumnsValue { get; init; }

    // Flexbox overrides
    public MokaDirection?  Direction    { get; init; }
    public bool?           Wrap         { get; init; }

    // Shared layout overrides
    public MokaJustify?    Justify      { get; init; }
    public MokaAlign?      Align        { get; init; }
    public MokaSpacingScale? Gap        { get; init; }
    public string?         GapValue     { get; init; }

    // Visibility
    public bool?           Hidden       { get; init; }
}
```

Only set the properties you want to change — `null` values inherit from the base component.
