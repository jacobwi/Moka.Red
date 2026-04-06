---
title: Callout
description: Informational callout/admonition box with GitHub-style types (Note, Tip, Important, Warning, Caution).
order: 27
---

# Callout

`MokaCallout` is an informational callout/admonition box modeled after GitHub-style alerts. It features a left border accent, colored background tint, auto-mapped icon, and optional close button.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Callout body content |
| `Title` | `string?` | -- | Title text (defaults to the type name when null) |
| `Type` | `MokaCalloutType` | `Note` | `Note`, `Tip`, `Important`, `Warning`, `Caution` |
| `Icon` | `MokaIconDefinition?` | -- | Custom icon override (auto-mapped from type by default) |
| `Closable` | `bool` | `false` | Shows a close button |
| `OnClose` | `EventCallback` | -- | Callback when closed |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Types

Each type has a distinct color accent and default icon.

```blazor-preview
<div style="display:flex;flex-direction:column;gap:12px">
    <MokaCallout Type="MokaCalloutType.Note">
        This is a note with informational context.
    </MokaCallout>
    <MokaCallout Type="MokaCalloutType.Tip">
        Helpful tip to improve your workflow.
    </MokaCallout>
    <MokaCallout Type="MokaCalloutType.Important">
        Important information you should not miss.
    </MokaCallout>
    <MokaCallout Type="MokaCalloutType.Warning">
        Proceed with caution -- this may have side effects.
    </MokaCallout>
    <MokaCallout Type="MokaCalloutType.Caution">
        Dangerous action that cannot be undone.
    </MokaCallout>
</div>
```

## Custom Title

```blazor-preview
<MokaCallout Type="MokaCalloutType.Tip" Title="Pro Tip">
    You can use keyboard shortcuts to speed up navigation.
</MokaCallout>
```

## Closable

```blazor-preview
<MokaCallout Type="MokaCalloutType.Note" Closable>
    This callout can be dismissed by the user.
</MokaCallout>
```

## Custom Icon

```blazor-preview
<MokaCallout Type="MokaCalloutType.Important" Icon="MokaIcons.Action.Settings">
    Custom icon overrides the default type icon.
</MokaCallout>
```
