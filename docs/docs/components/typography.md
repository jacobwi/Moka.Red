---
title: Typography
description: Semantic text components for headings, body copy, code, links, captions, and more.
order: 3
---

# Typography

Moka.Red provides a full suite of semantic text components that map to HTML elements. All components inherit the theme font stack and spacing tokens, and accept `Color`, `Class`, and `Style` parameters.

## Components Overview

| Component | HTML Element | Purpose |
|-----------|-------------|---------|
| `MokaHeading` | `h1`-`h6` | Section headings |
| `MokaParagraph` | `p` | Body paragraphs |
| `MokaText` | `span` | Inline text with color/weight control |
| `MokaCaption` | `span` | Small secondary text |
| `MokaLabel` | `label` | Form labels |
| `MokaLink` | `a`, or `button` with `OnClick` and no `Href` | Styled anchor elements and inline text buttons |
| `MokaCode` | `code` / `pre` | Inline or block code |
| `MokaMark` | `mark` | Highlighted text |
| `MokaBlockquote` | `blockquote` | Pull quotes |
| `MokaKbd` | `kbd` | Keyboard shortcut keys |

## MokaHeading

```blazor-preview
<MokaHeading Level="1">Heading 1</MokaHeading>
<MokaHeading Level="2">Heading 2</MokaHeading>
<MokaHeading Level="3">Heading 3</MokaHeading>
<MokaHeading Level="4">Heading 4</MokaHeading>
<MokaHeading Level="5">Heading 5</MokaHeading>
<MokaHeading Level="6">Heading 6</MokaHeading>
```

### MokaHeading Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Level` | `int` | `2` | Heading level 1-6 |
| `Micro` | `bool` | `false` | The small uppercase look, on the element `Level` names. `SizeValue`, `Weight` and `Color` are ignored |
| `ChildContent` | `RenderFragment?` | - | Heading text |
| `Color` | `MokaColor?` | - | Text color |
| `Class` | `string?` | - | Additional CSS classes |

## MokaParagraph

```blazor-preview
<MokaParagraph>
    Moka.Red is a lightweight, performance-focused Blazor UI component library
    targeting .NET 9 and .NET 10.
</MokaParagraph>
```

## MokaText

Renders an inline `<span>`. Useful for applying color or weight to a fragment of text.

```blazor-preview
<p>
    Status: <MokaText Color="MokaColor.Success">Online</MokaText> -
    last seen <MokaText Color="MokaColor.Secondary">2 minutes ago</MokaText>
</p>
```

### MokaText Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | - | Inline text |
| `Color` | `MokaColor?` | - | Text color via CSS custom property |
| `Class` | `string?` | - | Additional CSS classes |

## MokaCaption

Secondary, reduced-size text for metadata, hints, or labels beneath other content.

```blazor-preview
<MokaCaption>Last updated 5 minutes ago</MokaCaption>
```

### MokaCaption Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Align` | `MokaTextAlign?` | - | Text alignment |
| `Micro` | `bool` | `false` | The small uppercase look. `SizeValue` and `Color` are ignored |
| `ChildContent` | `RenderFragment?` | - | Caption text |

## Micro labels

`Micro` on `MokaLabel`, `MokaCaption` or `MokaHeading` draws the small uppercase label that sits over a group of values: 11px, bold, tracked, in the faint text color. It sets the size, weight and color itself. Use `MokaLabel` when it names an input, `MokaHeading` when it titles a group, so screen readers can jump to it, and `MokaCaption` otherwise.

```razor
<MokaCaption Micro>Last check</MokaCaption>
<MokaLabel Micro For="zone-input">Zone</MokaLabel>
<MokaHeading Level="3" Micro>Connected devices</MokaHeading>
```

`MokaHeading` keeps the `h1` to `h6` element its `Level` names, so the outline of the page stays right. The look comes from the `moka-micro-label` class in `moka.css`, which `MokaCaption` uses too.

## MokaLabel

Renders a `<label>` with optional `For` binding to an input id.

```blazor-preview
<MokaLabel For="email-input">Email address</MokaLabel>
<input id="email-input" type="email" />
```

### MokaLabel Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `For` | `string?` | - | The `id` of the associated input |
| `Required` | `bool` | `false` | Appends a required indicator |
| `Micro` | `bool` | `false` | The small uppercase look. `Size`, `SizeValue`, `Weight` and `Color` are ignored |
| `ChildContent` | `RenderFragment?` | - | Label text |

## MokaLink

Renders an `<a>` with consistent styling and optional external-link icon.

```blazor-preview
<MokaLink Href="https://github.com" Target="_blank">View on GitHub</MokaLink>
```

### MokaLink Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Href` | `string?` | - | URL. Left unset, the link points to `#`, or is a button when it has `OnClick`. A `javascript:`, `vbscript:` or `data:` URL is not rendered, so the link has no `href` |
| `OnClick` | `EventCallback<MouseEventArgs>` | - | Raised on a click. Without `Href` the link renders as a button |
| `Disabled` | `bool` | `false` | Disables the button. A link with `Href` ignores it |
| `External` | `bool` | `false` | Opens the link in a new tab with `rel="noopener noreferrer"` and shows an icon |
| `Underline` | `MokaLinkUnderline` | `Hover` | `Always`, `Hover` or `None` |
| `Weight` | `MokaFontWeight?` | - | Font weight |
| `Color` | `MokaColor?` | `Primary` | Link color |
| `ChildContent` | `RenderFragment?` | - | Link text |

### Text button

With `OnClick` and no `Href`, `MokaLink` renders a `<button type="button">` that reads as the text around it: inline, no box or padding, the surrounding font, the link color, `Underline` and `Weight`, and a focus ring for the keyboard. Use it for an action inside a sentence. `Disabled` dims it and stops the click.

```razor
<p>
    3 devices are hidden.
    <MokaLink OnClick="ShowAll">Show them</MokaLink>
</p>
```

With both `Href` and `OnClick` it stays a link and raises `OnClick` as well.

## MokaCode

Inline `<code>` or block `<pre><code>` for code snippets.

```blazor-preview
<p>Call <MokaCode>ToastService.ShowSuccess("Done!")</MokaCode> after saving.</p>

<MokaCode Block>
    var result = await dialog.ConfirmAsync("Delete this item?");
    if (result) await DeleteAsync();
</MokaCode>
```

### MokaCode Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | - | Code content |
| `Block` | `bool` | `false` | Renders as `<pre><code>` block instead of inline |
| `Language` | `string?` | - | Language hint for syntax highlighting integrations |

## MokaMark

Highlights text with a background color, like a marker.

```blazor-preview
<p>The <MokaMark>important term</MokaMark> is highlighted here.</p>
```

## MokaBlockquote

```blazor-preview
<MokaBlockquote>
    "Performance is a feature, not an afterthought."
</MokaBlockquote>
```

`Citation` adds a footer naming the source, and `CitationHref` links it. A `javascript:`, `vbscript:` or `data:` URL is not rendered, so the citation shows as plain text.

## MokaKbd

Renders a single key as a raised cap: monospace text, a heavier bottom border and an inset shadow.
One cap per key, so a combination is several of them.

```blazor-preview
<p>Press <MokaKbd>Ctrl</MokaKbd> + <MokaKbd>K</MokaKbd> to open search.</p>
```

Every shortcut hint in the library is built from this component: `MokaContextMenu` and
`MokaCommandPalette` split a `Shortcut` string on `+` and draw a cap per key, `MokaCheatsheet` draws
one per entry in `Keys`, and `MokaSlashMenu` and `MokaSearchInput` use it for their key hints. To split
a hint the same way in your own markup, call `ShortcutKeys.Split`:

```razor
@foreach (string key in ShortcutKeys.Split("Ctrl+Shift+P"))
{
    <MokaKbd>@key</MokaKbd>
}
```
