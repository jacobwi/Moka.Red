---
title: Badge
description: Notification badge that overlays a count or dot indicator on any child content.
order: 4
---

# Badge

`MokaBadge` wraps any child content and positions a small indicator at one of four corners. The indicator can show a count, a truncated count with a `MaxCount` cap, or a simple dot. The badge is independently colored and its visibility is togglable.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | — | The element to badge (icon, avatar, button, etc.) |
| `Content` | `string?` | — | Badge text (e.g., `"3"`, `"99+"`) |
| `Dot` | `bool` | `false` | Shows only a dot with no text |
| `Visible` | `bool` | `true` | Controls badge visibility |
| `MaxCount` | `int` | `99` | Numeric values above this show as `N+` |
| `Overlap` | `bool` | `true` | Badge overlaps the child content |
| `Position` | `MokaBadgePosition` | `TopRight` | `TopRight`, `TopLeft`, `BottomRight`, `BottomLeft` |
| `Color` | `MokaColor?` | `Error` | Badge indicator color |
| `Class` | `string?` | — | Additional CSS classes |
| `Style` | `string?` | — | Additional inline styles |

## Basic Count Badge

```blazor-preview
<MokaBadge Content="3">
    <MokaIcon Icon="MokaIcons.Navigation.Menu" Size="MokaSize.Lg" />
</MokaBadge>
```

## Dot Variant

Use `Dot` when you only need to indicate the presence of notifications, not the count.

```blazor-preview
<div style="display:flex;gap:24px">
    <MokaBadge Dot>
        <MokaIcon Icon="MokaIcons.Status.Info" Size="MokaSize.Lg" />
    </MokaBadge>

    <MokaBadge Dot Color="MokaColor.Success">
        <MokaIcon Icon="MokaIcons.Navigation.Home" Size="MokaSize.Lg" />
    </MokaBadge>
</div>
```

## MaxCount

When the numeric value exceeds `MaxCount` the badge displays `MaxCount+`.

```blazor-preview
<div style="display:flex;gap:24px">
    <MokaBadge Content="5" MaxCount="99">
        <MokaButton Variant="MokaVariant.Outlined" StartIcon="MokaIcons.Action.Download">
            Notifications
        </MokaButton>
    </MokaBadge>

    <MokaBadge Content="142" MaxCount="99">
        <MokaButton Variant="MokaVariant.Outlined">Messages</MokaButton>
    </MokaBadge>
</div>
```

## Colors

```blazor-preview
<div style="display:flex;gap:24px">
    <MokaBadge Content="1" Color="MokaColor.Error">
        <MokaIcon Icon="MokaIcons.Status.Warning" Size="MokaSize.Lg" />
    </MokaBadge>
    <MokaBadge Content="2" Color="MokaColor.Success">
        <MokaIcon Icon="MokaIcons.Status.CheckCircle" Size="MokaSize.Lg" />
    </MokaBadge>
    <MokaBadge Content="3" Color="MokaColor.Info">
        <MokaIcon Icon="MokaIcons.Status.Info" Size="MokaSize.Lg" />
    </MokaBadge>
    <MokaBadge Content="4" Color="MokaColor.Warning">
        <MokaIcon Icon="MokaIcons.Status.Clock" Size="MokaSize.Lg" />
    </MokaBadge>
</div>
```

## Positions

```blazor-preview
<div style="display:flex;gap:32px">
    <MokaBadge Content="1" Position="MokaBadgePosition.TopRight">
        <MokaIcon Icon="MokaIcons.Navigation.Home" Size="MokaSize.Lg" />
    </MokaBadge>
    <MokaBadge Content="2" Position="MokaBadgePosition.TopLeft">
        <MokaIcon Icon="MokaIcons.Navigation.Home" Size="MokaSize.Lg" />
    </MokaBadge>
    <MokaBadge Content="3" Position="MokaBadgePosition.BottomRight">
        <MokaIcon Icon="MokaIcons.Navigation.Home" Size="MokaSize.Lg" />
    </MokaBadge>
    <MokaBadge Content="4" Position="MokaBadgePosition.BottomLeft">
        <MokaIcon Icon="MokaIcons.Navigation.Home" Size="MokaSize.Lg" />
    </MokaBadge>
</div>
```

## Toggling Visibility

```blazor-preview
@code {
    bool _show = true;
    int _count = 5;
}
<MokaBadge Content="@_count.ToString()" Visible="_show" Color="MokaColor.Primary">
    <MokaButton Variant="MokaVariant.Outlined">Inbox</MokaButton>
</MokaBadge>
<MokaButton Variant="MokaVariant.Text" OnClick="@(() => _show = !_show)">
    @(_show ? "Hide" : "Show") Badge
</MokaButton>
```

## Badge on Avatar

```blazor-preview
<MokaBadge Dot Color="MokaColor.Success" Position="MokaBadgePosition.BottomRight">
    <MokaAvatar>JD</MokaAvatar>
</MokaBadge>
```
