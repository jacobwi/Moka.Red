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
| `MokaHeading` | `h1`–`h6` | Section headings |
| `MokaParagraph` | `p` | Body paragraphs |
| `MokaText` | `span` | Inline text with color/weight control |
| `MokaCaption` | `span` | Small secondary text |
| `MokaLabel` | `label` | Form labels |
| `MokaLink` | `a` | Styled anchor elements |
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
| `Level` | `int` | `1` | Heading level 1–6 |
| `ChildContent` | `RenderFragment?` | — | Heading text |
| `Color` | `MokaColor?` | — | Text color |
| `Class` | `string?` | — | Additional CSS classes |

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
    Status: <MokaText Color="MokaColor.Success">Online</MokaText> —
    last seen <MokaText Color="MokaColor.Secondary">2 minutes ago</MokaText>
</p>
```

### MokaText Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | — | Inline text |
| `Color` | `MokaColor?` | — | Text color via CSS custom property |
| `Class` | `string?` | — | Additional CSS classes |

## MokaCaption

Secondary, reduced-size text for metadata, hints, or labels beneath other content.

```blazor-preview
<MokaCaption>Last updated 5 minutes ago</MokaCaption>
```

## MokaLabel

Renders a `<label>` with optional `For` binding to an input id.

```blazor-preview
<MokaLabel For="email-input">Email address</MokaLabel>
<input id="email-input" type="email" />
```

### MokaLabel Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `For` | `string?` | — | The `id` of the associated input |
| `Required` | `bool` | `false` | Appends a required indicator |
| `ChildContent` | `RenderFragment?` | — | Label text |

## MokaLink

Renders an `<a>` with consistent styling and optional external-link icon.

```blazor-preview
<MokaLink Href="https://github.com" Target="_blank">View on GitHub</MokaLink>
```

### MokaLink Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Href` | `string?` | — | URL |
| `Target` | `string?` | — | Link target (e.g., `_blank`) |
| `Color` | `MokaColor?` | `Primary` | Link color |
| `ChildContent` | `RenderFragment?` | — | Link text |

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
| `ChildContent` | `RenderFragment?` | — | Code content |
| `Block` | `bool` | `false` | Renders as `<pre><code>` block instead of inline |
| `Language` | `string?` | — | Language hint for syntax highlighting integrations |

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

## MokaKbd

Renders keyboard shortcuts in a monospace pill style.

```blazor-preview
<p>Press <MokaKbd>Ctrl</MokaKbd> + <MokaKbd>K</MokaKbd> to open search.</p>
```
