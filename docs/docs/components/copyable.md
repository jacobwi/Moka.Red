---
title: Copyable
description: A value with its copy button, for addresses, keys and IDs.
order: 123
---

# Copyable

`MokaCopyable` draws a value with a small copy button beside it. After a copy a tag above the button says "Copied", and if the browser refused, "Couldn't copy". For a copy button on its own, use [`MokaCopyButton`](/components/copy-button).

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Text` | `string` | **required** | What gets copied, and what shows unless `ChildContent` is given |
| `ChildContent` | `RenderFragment?` | - | What shows instead of `Text`, for a value shown shorter than what's copied |
| `Label` | `string?` | - | What it copies in words, for the button's name and tooltip: "VPN key". Defaults to `Text` |
| `Mono` | `bool` | `true` | Draws the value in the mono font |
| `Wrap` | `bool` | `false` | A long value breaks onto the next line instead of being cut short |
| `CopyLabel` | `string?` | - | The button's name and tooltip. Defaults to "Copy " and `Label` |
| `CopiedText` | `string` | `"Copied"` | Shown above the button after a copy |
| `FailedText` | `string` | `"Couldn't copy"` | Shown above the button when the browser refused the copy |
| `TooltipPosition` | `MokaTooltipPosition` | `Bottom` | Where the button's tooltip opens |
| `Class` | `string?` | - | Additional CSS classes on the root |
| `Style` | `string?` | - | Additional inline styles on the root |

Other attributes land on the root. `Margin`, `Padding` and `Rounded` land on the root too.

## Basic

```razor
<MokaCopyable Text="192.168.1.24" Label="IPv4" />
```

## A value shown shorter than what's copied

```razor
<MokaCopyable Text="@_key" Label="VPN key">sk-live-...cdef</MokaCopyable>
```

## Long values

A value is cut short with an ellipsis to fit its container, and the whole of it is still copied. `Wrap` shows all of it instead.

```razor
<MokaCopyable Text="@_ipv6" Label="IPv6" Style="max-width: 180px" />
<MokaCopyable Text="@_ipv6" Label="IPv6" Wrap />
```

## In a list

In a table row, a `role="row"` element or any element with the `moka-quiet-copies` class, the button stays faint until the row is hovered, so a list of addresses stays calm. Keyboard focus and the copied state bring it out too. On a touch screen it's larger and always visible.

```razor
<div class="moka-quiet-copies">
    Office desktop <MokaCopyable Text="192.168.1.24" Label="office desktop's address" />
</div>
```
