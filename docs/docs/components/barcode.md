---
title: Barcode
description: One-dimensional barcode drawn as inline SVG, in Code 128, Code 39, EAN-13, EAN-8 or UPC-A.
order: 60
---

# Barcode

`MokaBarcode` encodes a value as a one-dimensional barcode and draws it as inline SVG. The encoder is written in C#, so there is no JavaScript and no image request. It supports Code 128 (subset B), Code 39, EAN-13, EAN-8 and UPC-A. For two-dimensional codes, use `MokaQRCode`.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `string` | **required** | Data to encode. An empty value renders an empty wrapper |
| `BarcodeFormat` | `MokaBarcodeFormat` | `Code128` | Symbology: `Code128`, `Code39`, `EAN13`, `EAN8`, `UPC` |
| `BarcodeWidth` | `int` | `200` | Width of the SVG in pixels, quiet zones included |
| `BarcodeHeight` | `int` | `80` | Height of the SVG in pixels, printed text included |
| `ForegroundColor` | `string` | `"#000000"` | Color of the bars and the printed text. A value that is not a CSS color draws the default |
| `BackgroundColor` | `string` | `"#ffffff"` | Background of the whole symbol. A value that is not a CSS color draws the default |
| `ShowText` | `bool` | `true` | Prints the value under the bars |
| `TextSize` | `string` | `"12px"` | Font size of the printed value: a number with an optional CSS unit, such as `"14px"` or `"0.8rem"`. Anything else uses the default |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### Accepted Input

| Format | Accepts | Check digit |
|--------|---------|-------------|
| `Code128` | Printable ASCII, space through `~` | Added automatically |
| `Code39` | Digits, `A` to `Z`, space and `- . $ / + %`. Lowercase is uppercased | None |
| `EAN13` | 12 digits, or 13 with the check digit | Computed when missing, verified when supplied |
| `EAN8` | 7 digits, or 8 with the check digit | Computed when missing, verified when supplied |
| `UPC` | UPC-A: 11 digits, or 12 with the check digit | Computed when missing, verified when supplied |

EAN and UPC values may carry surrounding spaces, which are trimmed. Their printed text is the full code, check digit included.

## Basic

```blazor-preview
<MokaBarcode Value="INV-0142" />
```

## Formats

```blazor-preview
<div style="display:flex;gap:16px;flex-wrap:wrap;align-items:flex-end">
    <MokaBarcode Value="SKU-10442-B" />
    <MokaBarcode Value="MOKA-RED-01" BarcodeFormat="MokaBarcodeFormat.Code39" BarcodeWidth="260" />
    <MokaBarcode Value="400638133393" BarcodeFormat="MokaBarcodeFormat.EAN13" />
    <MokaBarcode Value="9638507" BarcodeFormat="MokaBarcodeFormat.EAN8" BarcodeWidth="150" />
    <MokaBarcode Value="03600029145" BarcodeFormat="MokaBarcodeFormat.UPC" />
</div>
```

## Check Digits

The first code leaves out its check digit, so the component computes it and prints `4006381333931`. The second supplies a wrong one, so the SVG shows the error in place of the bars.

```blazor-preview
<div style="display:flex;gap:16px;flex-wrap:wrap;align-items:flex-end">
    <MokaBarcode Value="400638133393" BarcodeFormat="MokaBarcodeFormat.EAN13" />
    <MokaBarcode Value="4006381333932" BarcodeFormat="MokaBarcodeFormat.EAN13" BarcodeWidth="300" />
</div>
```

## Size and Colors

```blazor-preview
<MokaBarcode Value="PKG-88213"
             BarcodeWidth="320"
             BarcodeHeight="110"
             TextSize="14px"
             ForegroundColor="#1a1a22"
             BackgroundColor="#f5f5f7" />
```

## Without Text

```blazor-preview
<MokaBarcode Value="PKG-88213" ShowText="false" BarcodeHeight="48" />
```

## Behaviour

- Invalid data does not throw. The SVG shows the error message in place of the bars, for example `EAN-13 check digit is 2 but should be 1.` SVG text does not wrap, so a narrow barcode can cut the message off.
- The quiet zone, the blank margin scanners need on each side, is part of `BarcodeWidth`: 10 modules per side for Code 128 and Code 39, 11 left and 7 right for EAN-13, 7 for EAN-8 and 9 for UPC-A. The bars stretch over the rest, so a long value at a small width gives very thin bars. Give long values more width.
- With `ShowText`, the bottom 18 pixels of `BarcodeHeight` hold the text and the bars get the rest.
- Scanners expect dark bars on a light background with strong contrast.
- The colors must be CSS colors: hex, a keyword such as `black`, or a color function such as `rgb()`, `hsl()` or `var()`. `TextSize` must be a number with an optional unit. Anything else falls back to the default, and every value is escaped before it goes into the SVG, so a stray quote cannot add markup.
- The SVG is rebuilt only when a parameter changes.

## Accessibility

The SVG has no role or text alternative. Add `role="img"` and an `aria-label` (unmatched attributes go on the wrapper `div`), or show the value as text next to the code.
