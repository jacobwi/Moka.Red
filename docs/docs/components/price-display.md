---
title: Price Display
description: Price with an optional struck-out original price, currency code and discount badge.
order: 66
---

# Price Display

`MokaPriceDisplay` shows a price with a currency symbol and a fixed number of decimals. Set `OriginalPrice` to show a sale: the original is struck out and a badge shows the discount. It only displays amounts; to edit one, use `MokaCurrencyInput`.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Price` | `decimal` | **required** | Current or sale price |
| `OriginalPrice` | `decimal?` | -- | Price before the sale, shown struck out before `Price` |
| `CurrencySymbol` | `string` | `"$"` | Symbol written before each amount |
| `CurrencyCode` | `string?` | -- | Code shown after the price, such as `"USD"` |
| `DecimalPlaces` | `int` | `2` | Decimal places for both amounts, from 0 to 28. Values outside that range are clamped |
| `ShowDiscount` | `bool` | `true` | Shows the discount badge when `OriginalPrice` is higher than `Price` |
| `Highlight` | `bool` | `false` | Shows the current price at a larger size |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic

```blazor-preview
<MokaPriceDisplay Price="49m" />
```

## On Sale

```blazor-preview
<div style="display:flex;flex-direction:column;align-items:flex-start;gap:8px">
    <MokaPriceDisplay Price="39.99m" OriginalPrice="49.99m" />
    <MokaPriceDisplay Price="12m" OriginalPrice="20m" />
</div>
```

## Currencies

```blazor-preview
<div style="display:flex;flex-direction:column;align-items:flex-start;gap:8px">
    <MokaPriceDisplay Price="24.90m" CurrencySymbol="€" CurrencyCode="EUR" />
    <MokaPriceDisplay Price="18.50m" CurrencySymbol="£" CurrencyCode="GBP" />
    <MokaPriceDisplay Price="980m" CurrencySymbol="¥" CurrencyCode="JPY" DecimalPlaces="0" />
</div>
```

## Highlighted in a Card

```blazor-preview
<MokaCard Outlined Title="Team" Subtitle="For growing teams" Style="width:260px">
    <div style="display:flex;align-items:baseline;gap:6px">
        <MokaPriceDisplay Price="29m" OriginalPrice="39m" Highlight />
        <span style="font-size:var(--moka-font-size-sm);color:var(--moka-color-on-surface-variant)">per month</span>
    </div>
</MokaCard>
```

## Without the Badge

```blazor-preview
<MokaPriceDisplay Price="79m" OriginalPrice="99m" ShowDiscount="false" />
```

## Behaviour

- Amounts use the invariant culture: a `.` decimal point, no thousands separators, and the symbol before the number. `1299.5m` shows as `$1299.50`. For other formats, such as `1.299,50 €`, format the text yourself.
- The badge shows how much lower `Price` is than `OriginalPrice`, as a whole percent. Half percents round up, so 12.5% shows as 13%.
- The badge appears only when `Price` is zero or more and below `OriginalPrice`, and the percent comes to at least 1. A `Price` above `OriginalPrice` still shows the struck-out original, without a badge.
- With `OriginalPrice` set, the root gets the `moka-price--sale` class for your own styling.
- Pass decimal literals with the `m` suffix (`39.99m`). A bare `39.99` is a `double` and does not convert to `decimal`.

## Accessibility

The original price is marked only by its line-through style, so a screen reader reads both amounts and the badge in a row ("$49.99 $39.99 -20%") without saying which is which. Where it matters, add that context in text.
