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
    <MokaTag Text="Locked" Icon="MokaIcons.Action.Lock" Color="MokaColor.Warning" />
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

## Sizes

```blazor-preview
<div style="display:flex;gap:8px;align-items:center">
    <MokaTag Text="Xs" Size="MokaSize.Xs" />
    <MokaTag Text="Sm" Size="MokaSize.Sm" />
    <MokaTag Text="Md" Size="MokaSize.Md" />
    <MokaTag Text="Lg" Size="MokaSize.Lg" />
</div>
```
