---
title: Identicon
description: Symmetric pattern generated from a string, for default avatars and visual fingerprints.
order: 62
---

# Identicon

`MokaIdenticon` hashes a string, such as a user name, an email address or an ID, into a small symmetric pattern and draws it as inline SVG. The same input always gives the same image, so it works as a default avatar or as a visual fingerprint for keys and records. `MokaAvatar` uses it as its last fallback when you set `IdenticonValue`.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `string` | `""` | Input to hash. An empty value draws nothing |
| `IdenticonSize` | `int` | `5` | Cells per side. Values outside 3 to 8 are clamped |
| `Palette` | `IReadOnlyList<string>?` | -- | CSS colors to pick from. `null` or empty uses the built-in palette of 12 colors. Where the hash picks an entry that is not a color, the built-in palette's color is drawn |
| `Background` | `string?` | `"transparent"` | Background color. `null`, or a value that is not a CSS color, draws no background |
| `Size` | `MokaSize` | `Md` | Width and height from the size scale: `Xs` 14px, `Sm` 16px, `Md` 20px, `Lg` 24px |
| `SizeValue` | `string?` | -- | Any CSS length for the width and height. Overrides `Size` |
| `Rounded` | `MokaRounding?` | -- | Rounds the corners and clips the image to them. `Full` gives a circle |
| `RoundedValue` | `string?` | -- | Any CSS border radius. Overrides `Rounded` and clips the same way |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic

```blazor-preview
<div style="display:flex;gap:12px;flex-wrap:wrap">
    <MokaIdenticon Value="mara.okafor" SizeValue="48px" />
    <MokaIdenticon Value="j.lindqvist" SizeValue="48px" />
    <MokaIdenticon Value="ci-runner-07" SizeValue="48px" />
    <MokaIdenticon Value="billing-service" SizeValue="48px" />
</div>
```

## Grid Size

The same value at three grid sizes. The color stays the same and the pattern changes.

```blazor-preview
<div style="display:flex;gap:16px;flex-wrap:wrap;align-items:flex-end">
    @foreach (var cells in new[] { 3, 5, 8 })
    {
        <div style="display:flex;flex-direction:column;align-items:center;gap:4px">
            <MokaIdenticon Value="mara.okafor" IdenticonSize="cells" SizeValue="64px" />
            <span style="font-size:var(--moka-font-size-xs);color:var(--moka-color-on-surface-variant)">@cells x @cells</span>
        </div>
    }
</div>
```

## Rounded with Background

Any rounding clips the image, so the background follows the corners.

```blazor-preview
<div style="display:flex;gap:12px;flex-wrap:wrap">
    <MokaIdenticon Value="mara.okafor" SizeValue="48px" Rounded="MokaRounding.Md" Background="#eceff1" />
    <MokaIdenticon Value="j.lindqvist" SizeValue="48px" Rounded="MokaRounding.Full" Background="#eceff1" />
    <MokaIdenticon Value="ci-runner-07" SizeValue="48px" Rounded="MokaRounding.Full" Background="#eceff1" />
</div>
```

## Custom Palette

```blazor-preview
<div style="display:flex;gap:12px;flex-wrap:wrap">
    @foreach (var key in new[] { "key-7f3a", "key-91c2", "key-0d44", "key-e5b8" })
    {
        <MokaIdenticon Value="@key" IdenticonSize="6" SizeValue="40px" Palette="_palette" />
    }
</div>

@code {
    private static readonly string[] _palette = ["#ef5350", "#c62828", "#ff6b68", "#a0a0aa"];
}
```

## In a User List

The name is already next to the image, so the identicon is hidden from assistive technology.

```blazor-preview
<div style="display:flex;flex-direction:column;gap:10px;width:100%;max-width:320px">
    @foreach (var user in _users)
    {
        <div style="display:flex;align-items:center;gap:10px">
            <MokaIdenticon Value="@user.Email"
                           SizeValue="32px"
                           Rounded="MokaRounding.Full"
                           Background="#eceff1"
                           aria-hidden="true" />
            <div style="display:flex;flex-direction:column">
                <span>@user.Name</span>
                <span style="font-size:var(--moka-font-size-xs);color:var(--moka-color-on-surface-variant)">@user.Email</span>
            </div>
        </div>
    }
</div>

@code {
    private readonly (string Name, string Email)[] _users =
    [
        ("Mara Okafor", "mara@example.com"),
        ("Jonas Lindqvist", "jonas@example.com"),
        ("Priya Raman", "priya@example.com")
    ];
}
```

## Behaviour

- The hash is FNV-1a over the string, so a value draws the same image on every machine and in every render mode, including prerendering.
- The pattern is mirrored left to right. The hash also picks the color from the palette.
- The SVG is rebuilt when `Value`, `IdenticonSize`, `Palette` or `Background` changes. The palette is compared by content, so a list changed in place and passed again redraws too.
- Palette entries and `Background` must be CSS colors: hex, a keyword such as `red` or `transparent`, or a color function such as `rgb()`, `hsl()`, `oklch()` or `var()`. Anything else is dropped as described above, and every value is escaped before it goes into the SVG.

## Accessibility

The identicon has no role or label. Next to a visible name, hide it with `aria-hidden="true"` as in the list above. Where it stands alone, give it `role="img"` and an `aria-label`. Unmatched attributes go on the wrapper `div`.
