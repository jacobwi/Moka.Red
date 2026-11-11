---
title: Tag
description: Non-interactive label tag for categorization and status display.
order: 33
---

# Tag

`MokaTag` renders a small, non-interactive label for categorizing content or showing status. Unlike `MokaChip`, tags are purely visual and do not support click or delete interactions.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Text` | `string` | -- | Tag label text |
| `Color` | `MokaColor` | `Primary` | Color theme |
| `Variant` | `MokaVariant` | `Soft` | Visual style: `Filled`, `Outlined`, `Text`, `Soft` |
| `Size` | `MokaSize` | `Sm` | Tag size: `Xs`, `Sm`, `Md`, `Lg` |
| `Icon` | `MokaIconDefinition?` | -- | Optional icon displayed before the text |
| `Pill` | `bool` | `false` | Uses fully rounded (pill) shape |
| `New` | `bool` | `false` | The small tag that flags something as new. `Variant`, `Color` and `Size` are ignored |
| `Mono` | `bool` | `false` | Draws the text in the mono font |
| `Label` | `MokaLabelColor?` | -- | A color someone gave the tag: its tint behind readable ink, and a dot before the text when there's no icon. Replaces `Color`; `Variant` still shapes it. Ignored with `New` |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic

```blazor-preview
<div style="display:flex;gap:8px">
    <MokaTag Text="New" />
    <MokaTag Text="In Progress" />
    <MokaTag Text="Resolved" />
</div>
```

## Colors

```blazor-preview
<div style="display:flex;gap:8px;flex-wrap:wrap">
    <MokaTag Text="Primary" Color="MokaColor.Primary" />
    <MokaTag Text="Secondary" Color="MokaColor.Secondary" />
    <MokaTag Text="Success" Color="MokaColor.Success" />
    <MokaTag Text="Warning" Color="MokaColor.Warning" />
    <MokaTag Text="Error" Color="MokaColor.Error" />
    <MokaTag Text="Info" Color="MokaColor.Info" />
</div>
```

## Variants

```blazor-preview
<div style="display:flex;gap:8px">
    <MokaTag Text="Soft" Variant="MokaVariant.Soft" />
    <MokaTag Text="Filled" Variant="MokaVariant.Filled" />
    <MokaTag Text="Outlined" Variant="MokaVariant.Outlined" />
    <MokaTag Text="Text" Variant="MokaVariant.Text" />
</div>
```

## With Icon

```blazor-preview
<div style="display:flex;gap:8px">
    <MokaTag Text="Settings" Icon="MokaIcons.Action.Settings" />
    <MokaTag Text="Locked" Icon="MokaIcons.Toggle.Lock" Color="MokaColor.Warning" />
    <MokaTag Text="Done" Icon="MokaIcons.Status.CheckCircle" Color="MokaColor.Success" />
</div>
```

## Pill Shape

```blazor-preview
<div style="display:flex;gap:8px">
    <MokaTag Text="v1.0" Pill />
    <MokaTag Text="Beta" Pill Color="MokaColor.Warning" />
    <MokaTag Text="Stable" Pill Color="MokaColor.Success" />
</div>
```

## New

`New` draws the tag that sits beside something just added: accent tint, tight corners, bold tracked text.

```razor
<MokaTag Text="New" New />
```

## Mono

`Mono` draws the text in the mono font, for addresses, ports, versions and hashes. It works with every variant, color and size, and with `New`.

```razor
<MokaTag Text="10.0.0.12" Mono />
<MokaTag Text="v2.4.1" Mono Pill Color="MokaColor.Info" />
```

## Label colors

`Label` takes a [label color](/getting-started/label-colors), for tags people color themselves. It replaces `Color`. `Soft` (the default) and `Filled` draw the label's tint behind its ink, `Outlined` a border in the label's color, and `Text` the ink alone. A tag without an icon gets a dot in the label's color before its text. The shades follow light and dark and `MokaTheme.WithLabelTone`.

```razor
<MokaTag Text="Design" Label="MokaLabelColor.Teal"/>
<MokaTag Text="In review" Label="MokaLabelColor.Amber"/>
<MokaTag Text="Blocked" Label="MokaLabelColor.Red" Variant="MokaVariant.Outlined"/>
<MokaTag Text="Archive" Label="MokaLabelColor.Slate" Variant="MokaVariant.Text"/>
```

## Sizes

```blazor-preview
<div style="display:flex;gap:8px;align-items:center">
    <MokaTag Text="Xs" Size="MokaSize.Xs" />
    <MokaTag Text="Sm" Size="MokaSize.Sm" />
    <MokaTag Text="Md" Size="MokaSize.Md" />
    <MokaTag Text="Lg" Size="MokaSize.Lg" />
</div>
```
