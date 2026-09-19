---
title: QR Code
description: QR code generated in C# and drawn as inline SVG, with error correction levels and rounded modules.
order: 61
---

# QR Code

`MokaQRCode` encodes a string as a QR code and draws it as inline SVG. The encoder is written in C# with no dependencies and needs no JavaScript. It uses byte mode (UTF-8) and QR versions 1 to 10, which covers URLs, IDs and short payloads such as Wi-Fi credentials. For one-dimensional barcodes, use `MokaBarcode`.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `string` | **required** | Data to encode. An empty value renders an empty wrapper |
| `QRSize` | `int` | `256` | Width and height of the SVG in pixels, quiet zone included |
| `ForegroundColor` | `string` | `"#000000"` | Color of the dark modules. A value that is not a CSS color draws the default |
| `BackgroundColor` | `string` | `"#ffffff"` | Background of the whole code, quiet zone included. A value that is not a CSS color draws the default |
| `ErrorCorrection` | `MokaQRErrorCorrection` | `Medium` | Recovery level: `Low` (about 7%), `Medium` (15%), `Quartile` (25%), `High` (30%) |
| `QuietZone` | `int` | `4` | Blank border around the code, in modules |
| `RoundedModules` | `bool` | `false` | Draws each module with rounded corners |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### Capacity

The encoder picks the smallest version that fits the data. Version 10 is the largest it supports, which caps the data at these sizes. The limit is in UTF-8 bytes, so a character outside ASCII takes two to four.

| Error correction | Max bytes |
|------------------|-----------|
| `Low` | 271 |
| `Medium` | 213 |
| `Quartile` | 151 |
| `High` | 119 |

Longer data draws the text "Data too long" in place of the code.

## Basic

```blazor-preview
<MokaQRCode Value="https://example.com/orders/10442" QRSize="160" />
```

## Error Correction

Higher levels survive more damage but need more modules for the same data.

```blazor-preview
<div style="display:flex;gap:16px;flex-wrap:wrap">
    @foreach (var level in new[] { MokaQRErrorCorrection.Low, MokaQRErrorCorrection.Medium, MokaQRErrorCorrection.Quartile, MokaQRErrorCorrection.High })
    {
        <div style="display:flex;flex-direction:column;align-items:center;gap:4px">
            <MokaQRCode Value="https://example.com/orders/10442" QRSize="120" ErrorCorrection="level" />
            <span style="font-size:var(--moka-font-size-xs);color:var(--moka-color-on-surface-variant)">@level</span>
        </div>
    }
</div>
```

## Colors and Rounded Modules

```blazor-preview
<div style="display:flex;gap:16px;flex-wrap:wrap">
    <MokaQRCode Value="https://example.com/menu" QRSize="140" RoundedModules />
    <MokaQRCode Value="https://example.com/menu" QRSize="140" ForegroundColor="#c62828" RoundedModules />
    <MokaQRCode Value="https://example.com/menu" QRSize="140" ForegroundColor="#101015" BackgroundColor="#fdf6e3" />
</div>
```

## Wi-Fi Credentials

A QR code can carry any short text. Phone cameras offer to join a network from this format. The code also gets a text alternative, since a screen reader cannot read the image.

```blazor-preview
<div style="display:flex;align-items:center;gap:16px">
    <MokaQRCode Value="@_wifi"
                QRSize="140"
                ErrorCorrection="MokaQRErrorCorrection.Quartile"
                role="img"
                aria-label="QR code to join the Moka-Guest Wi-Fi network" />
    <div style="display:flex;flex-direction:column;gap:2px">
        <span style="font-weight:600">Moka-Guest</span>
        <span style="font-size:var(--moka-font-size-sm);color:var(--moka-color-on-surface-variant)">Scan to join the guest network</span>
    </div>
</div>

@code {
    private readonly string _wifi = "WIFI:T:WPA;S:Moka-Guest;P:espresso-2026;;";
}
```

## Behaviour

- `Low` gives the smallest code. `High` survives the most damage, which helps with printed codes or a logo placed over the center.
- `QRSize` includes the quiet zone. Each module is `QRSize` divided by the module count plus twice `QuietZone` (versions 1 to 10 have 21 to 57 modules per side). The QR standard asks for a quiet zone of 4 modules, and smaller ones can fail on some readers.
- Rounded modules are drawn as separate rounded squares, so neighbouring modules no longer join into solid blocks. Test the result with the readers you target.
- Scanners expect dark modules on a light background with strong contrast.
- The colors must be CSS colors: hex, a keyword such as `black`, or a color function such as `rgb()`, `hsl()` or `var()`. Anything else falls back to the default, and both are escaped before they go into the SVG, so a stray quote cannot add markup.
- The SVG is rebuilt only when a parameter changes.

## Accessibility

The SVG has no role or text alternative. Add `role="img"` and an `aria-label` as in the Wi-Fi example (unmatched attributes go on the wrapper `div`), and show the link or payload as text for people who cannot scan the code.
