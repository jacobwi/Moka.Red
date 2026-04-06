---
title: Alert
description: Inline alert banner for displaying contextual messages with severity-based styling.
order: 20
---

# Alert

`MokaAlert` displays contextual feedback messages with severity-based colors and icons. It uses `role="alert"` for screen reader accessibility. Supports closable, outlined, and dense variants.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Alert body content |
| `Severity` | `MokaToastSeverity` | `Info` | `Info`, `Success`, `Warning`, `Error` |
| `Title` | `string?` | -- | Optional bold title above the content |
| `Closable` | `bool` | `false` | Shows a close button |
| `OnClose` | `EventCallback` | -- | Callback when the alert is closed |
| `Outlined` | `bool` | `false` | Uses outlined style instead of filled |
| `Dense` | `bool` | `false` | Compact layout with reduced padding |
| `Icon` | `MokaIconDefinition?` | -- | Custom icon override (auto-mapped from severity by default) |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Severities

Each severity maps to a default icon and color scheme.

```blazor-preview
<div style="display:flex;flex-direction:column;gap:8px">
    <MokaAlert Severity="MokaToastSeverity.Info">This is an informational message.</MokaAlert>
    <MokaAlert Severity="MokaToastSeverity.Success">Operation completed successfully.</MokaAlert>
    <MokaAlert Severity="MokaToastSeverity.Warning">Please review before continuing.</MokaAlert>
    <MokaAlert Severity="MokaToastSeverity.Error">Something went wrong.</MokaAlert>
</div>
```

## With Title

```blazor-preview
<MokaAlert Severity="MokaToastSeverity.Warning" Title="Update Available">
    A new version is available. Please update at your earliest convenience.
</MokaAlert>
```

## Outlined

```blazor-preview
<div style="display:flex;flex-direction:column;gap:8px">
    <MokaAlert Severity="MokaToastSeverity.Info" Outlined>Outlined info alert.</MokaAlert>
    <MokaAlert Severity="MokaToastSeverity.Error" Outlined>Outlined error alert.</MokaAlert>
</div>
```

## Dense

```blazor-preview
<MokaAlert Severity="MokaToastSeverity.Success" Dense>Compact success message.</MokaAlert>
```

## Closable

```blazor-preview
<MokaAlert Severity="MokaToastSeverity.Info" Closable Title="Dismissible">
    Click the close button to dismiss this alert.
</MokaAlert>
```

## Custom Icon

```blazor-preview
<MokaAlert Severity="MokaToastSeverity.Info" Icon="MokaIcons.Action.Settings">
    Custom icon overrides the default severity icon.
</MokaAlert>
```
