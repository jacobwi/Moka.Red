---
title: Copy Button
description: Small button that copies a string to the clipboard and confirms with a check mark.
order: 63
---

# Copy Button

`MokaCopyButton` copies a string to the clipboard when clicked and shows a check mark for two seconds. Put it next to commands, IDs, keys and links. `MokaCodeBlock` (`ShowCopyButton`) and `MokaKeyValue` (`Copyable`) have their own copy buttons; use this one everywhere else.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Text` | `string` | **required** | String copied to the clipboard |
| `ChildContent` | `RenderFragment?` | -- | Custom button content. Replaces the copy and check icons |
| `TooltipText` | `string` | `"Copy"` | Tooltip (`title`) before a copy |
| `CopiedText` | `string` | `"Copied!"` | Tooltip (`title`) for two seconds after a copy |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic

```blazor-preview
<div style="display:flex;align-items:center;gap:8px">
    <code style="font-family:var(--moka-font-family-mono);font-size:var(--moka-font-size-sm)">dotnet add package Moka.Red</code>
    <MokaCopyButton Text="dotnet add package Moka.Red" />
</div>
```

## Custom Tooltips

```blazor-preview
<MokaCopyButton Text="ord_8f41c2a9" TooltipText="Copy order ID" CopiedText="Order ID copied" />
```

## Custom Content

`ChildContent` replaces the icons, so the copied state shows only through the success color and the tooltip.

```blazor-preview
<MokaCopyButton Text="https://example.com/invite/7f3a9c">
    <MokaIcon Icon="MokaIcons.Content.Link" SizeValue="14px" />
    Copy invite link
</MokaCopyButton>
```

## Next to Values

Each button gets its own tooltip, which is also its accessible name, so screen reader users can tell the buttons apart.

```blazor-preview
<div style="display:grid;grid-template-columns:auto 1fr auto;gap:8px 12px;align-items:center;font-size:var(--moka-font-size-sm)">
    @foreach (var row in _rows)
    {
        <span style="color:var(--moka-color-on-surface-variant)">@row.Label</span>
        <span style="font-family:var(--moka-font-family-mono)">@row.Value</span>
        <MokaCopyButton Text="@row.Value" TooltipText="@row.Tooltip" />
    }
</div>

@code {
    private readonly (string Label, string Value, string Tooltip)[] _rows =
    [
        ("Tenant ID", "3f9c2e41-7b0a-4d6e-9a15-c2d8e0f4b716", "Copy tenant ID"),
        ("Region", "eu-west-1", "Copy region"),
        ("Build", "2026.09.18-4412", "Copy build number")
    ];
}
```

## Behaviour

- In a secure context (HTTPS or `localhost`) the button uses the Clipboard API. Elsewhere, or when the browser refuses, it falls back to a hidden text area and `document.execCommand("copy")`.
- If both fail, the button stays as it was. No error is shown or raised. The same goes for a Blazor Server connection lost mid-copy and a script that fails to load.
- A successful copy turns the button to the success color and swaps the tooltip to `CopiedText`. Another copy within the two seconds starts them again.
- The clipboard code lives in Core's `moka-drag.js`, which loads on the first click.

## Accessibility

The button is a native `<button type="button">`. With the default icon content its accessible name is the `title`, because the icon is `aria-hidden`. The switch to `CopiedText` is not announced to screen readers.
