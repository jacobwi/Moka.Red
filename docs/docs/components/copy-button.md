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
| `TextSource` | `Func<string>?` | -- | Reads the text when the button is clicked, instead of `Text`. For text too long to build on every render. Needs an interactive render |
| `ChildContent` | `RenderFragment?` | -- | Custom button content. Replaces the copy and check icons |
| `TooltipText` | `string` | `"Copy"` | Tooltip before a copy. With the default icon content it also names the button |
| `CopiedText` | `string` | `"Copied!"` | Tooltip for two seconds after a copy |
| `FailedText` | `string` | `"Couldn't copy"` | Tooltip for two seconds when the browser refused the copy |
| `Quiet` | `bool` | `false` | A small borderless button for beside a value, which says it in a tag above itself. [`MokaCopyable`](/components/copyable) draws this |
| `TooltipPosition` | `MokaTooltipPosition` | `Bottom` | Where the tooltip opens: `Bottom` in a row, `Top` in a row at the bottom of a view, `Left` or `Right` in a vertical rail |
| `Class` | `string?` | -- | Additional CSS classes on the button |
| `Style` | `string?` | -- | Additional inline styles on the button |

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

## Long Text

`Text` is built on every render of the parent and written into the page. For a log that grows while it is on screen, that means copying the whole log for each line that arrives, so pass `TextSource`, which runs only on a click. `Text` is still required, and it can be empty: with a `TextSource` the button never copies it. `MokaTerminal` copies this way.

```razor
<MokaCopyButton Text="" TextSource="() => string.Join('\n', _log)" />
```

The browser asks .NET for the text inside the click and starts the copy straight away, as a clipboard item whose text is still on its way, which Chromium 98+, Firefox 127+ and Safari 13.1+ accept. On WebAssembly the answer usually arrives before the click is over and the copy is the plain kind. A static render has no .NET to answer, so there the button copies nothing.

## Behaviour

- The copy happens in the browser, inside the click (or the click Enter and Space raise), with no round trip to .NET first. Safari, iOS and Firefox only let a page write to the clipboard while the click is still live, and up to 0.4.2 a Blazor Server app's copy came a round trip later, so those browsers refused it.
- The button carries its text in `data-moka-copy`, and one click listener in Core's `moka-copy.js`, installed by the package's JavaScript initializer, does the copy. Nothing loads or binds per button.
- In a secure context (HTTPS or `localhost`) it uses the Clipboard API. Elsewhere, or when the browser refuses, it falls back to the copy command, with the text selected out of sight and focus left on the button.
- A refused copy shows `FailedText` for two seconds and turns the button red. A Blazor Server connection lost mid-copy stays silent, since nobody is there to read it.
- The tooltip is a `MokaTooltip`, shown on hover and on keyboard focus. A successful copy turns the button to the success color and swaps the tooltip to `CopiedText`, so a pointer still on the button sees it change. Another copy within the two seconds starts them again. Only the button redraws.
- The button sits inside the tooltip's element. `Class`, `Style` and unmatched attributes still land on the button.

## Copy from Your Own Markup

Any element can copy the same way. Give it the text in `data-moka-copy`, or a selector in `data-moka-copy-from` (the nearest ancestor holding a match supplies a field's value, or the text of every match, one per line), and listen to `@onmokacopied` (namespace `Moka.Red.Core.Clipboard`), whose `MokaCopyEventArgs.Copied` says how it went.

```razor
@using Moka.Red.Core.Clipboard

<button type="button" data-moka-copy="@_inviteLink" @onmokacopied="Copied">Copy invite link</button>
<span role="status">@_status</span>

@code {
    private readonly string _inviteLink = "https://example.com/invite/7f3a9c";
    private string? _status;

    private void Copied(MokaCopyEventArgs e) => _status = e.Copied ? "Copied" : "Press Ctrl+C to copy it";
}
```

## Accessibility

The button is a native `<button type="button">`, so Enter and Space copy it like a click. With the default icon content it is named by `aria-label`, the tooltip's text, because the icon is `aria-hidden`; with `ChildContent` the content names it. Up to 0.1.19 the name was a `title`. The switch to `CopiedText` is not announced to screen readers, except by the `Quiet` look, which has a status region.
