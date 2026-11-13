---
title: Alert
description: Inline alert banner for displaying contextual messages with severity-based styling.
order: 20
---

# Alert

`MokaAlert` displays contextual feedback messages with severity-based colors and icons. It uses `role="alert"` for screen reader accessibility, or what `Live` asks for. Supports closable, outlined, and dense variants.

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
| `FixAction` | `RenderFragment?` | -- | What the reader can do about it, usually a button. At the end of the row before the close button, and under the text when the alert is narrow |
| `Accent` | `bool` | `false` | Draws the alert in the theme's accent, whatever `Severity` says. `Severity` still picks the icon |
| `Live` | `MokaAlertLive` | `Alert` | How screen readers hear it: `Alert` (`role="alert"`, read out at once), `Status` (`role="status"`, read out politely) or `None` (no role). See [Screen readers](#screen-readers) |
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

The close button shows only a cross, so it is named "Close" by `aria-label` and by a `MokaTooltip` below it, lined up
with the alert's edge.

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

## Fix Action

A problem usually comes with something to do about it. `FixAction` puts that at the end of the row, before the close button.

```razor
<MokaAlert Severity="MokaToastSeverity.Error" Title="Printer stopped answering." Closable>
    <FixAction>
        <MokaButton Size="MokaSize.Sm" Variant="MokaVariant.Outlined">Move it back</MokaButton>
    </FixAction>
    <ChildContent>Last seen on the guest network.</ChildContent>
</MokaAlert>
```

## Accent

`Accent` is for news that is neither good nor bad, such as what's new in a version. It draws the alert in the accent's tint whatever `Severity` says, and the alert keeps the role `Live` gives it, `role="alert"` by default.

```razor
<MokaAlert Accent Title="New in this version.">
    <FixAction>
        <MokaButton Size="MokaSize.Sm" Variant="MokaVariant.Outlined">Read more</MokaButton>
    </FixAction>
    <ChildContent>Tables can group their rows now.</ChildContent>
</MokaAlert>
```

## An edge instead of a fill

`MokaTheme.WithCallouts(MokaCalloutLook.Edge)` draws a filled alert, accent ones included, with a 3 px edge in its severity's color down the start side over a faint tint, the way it draws `MokaCallout`. An outlined alert keeps its outline. It's a choice on the new design that `MokaTheme.Ember` has on; `MokaTheme.Dark` and `Light` don't change.

```razor
<MokaThemeProvider Theme="_theme">
    <MokaAlert Severity="MokaToastSeverity.Error" Title="Sync failed">Could not reach server 10.0.0.4.</MokaAlert>
</MokaThemeProvider>

@code {
    private readonly MokaTheme _theme = MokaTheme.Red.WithCallouts(MokaCalloutLook.Edge);
}
```

## Screen readers

An alert is `role="alert"` by default: a screen reader reads it out at once, over whatever it was saying. That suits a save that failed, not a message that is part of the page. `Live` picks how it is heard, and changes nothing on screen:

| `Live` | Role | Heard |
|---|---|---|
| `Alert` (default) | `role="alert"` | At once, interrupting |
| `Status` | `role="status"` | Once the screen reader is idle, for news that can wait, such as "Saved" |
| `None` | none | Where it sits on the page, like a callout, for a message that is there from the start |

```razor
<MokaAlert Severity="MokaToastSeverity.Success" Live="MokaAlertLive.Status">Saved.</MokaAlert>
<MokaAlert Severity="MokaToastSeverity.Warning" Live="MokaAlertLive.None" Title="Read only">
    This device is managed by your organization.
</MokaAlert>
```

A live region speaks when its content changes, so add the alert to the page, or change what it says, when the news happens. A `role` attribute you pass yourself, such as `role="note"`, still wins. Up to 0.4.2 every alert was `role="alert"`.
