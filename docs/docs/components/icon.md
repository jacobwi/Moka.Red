---
title: Icon
description: Inline SVG icon component backed by the MokaIcons library (Lucide-style, 24x24 viewBox).
order: 2
---

# Icon

`MokaIcon` renders an inline SVG from a `MokaIconDefinition`. Icons use a 24x24 viewBox with stroke-based paths (Lucide/Feather style). The built-in library is organized into five categories: `Action`, `Navigation`, `Status`, `Content`, and `Toggle`.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Icon` | `MokaIconDefinition` | **required** | The icon definition to render |
| `Size` | `MokaSize` | `Md` | `Xs`, `Sm`, `Md`, `Lg` |
| `Color` | `MokaColor?` | — | Sets the icon color via CSS custom property |
| `Class` | `string?` | — | Additional CSS classes |
| `Style` | `string?` | — | Additional inline styles |

## Basic Usage

```blazor-preview
<MokaIcon Icon="MokaIcons.Action.Save" />
<MokaIcon Icon="MokaIcons.Status.CheckCircle" Color="MokaColor.Success" />
<MokaIcon Icon="MokaIcons.Status.Error" Color="MokaColor.Error" />
```

## Sizes

```blazor-preview
<div style="display:flex;gap:12px;align-items:center">
    <MokaIcon Icon="MokaIcons.Navigation.Home" Size="MokaSize.Xs" />
    <MokaIcon Icon="MokaIcons.Navigation.Home" Size="MokaSize.Sm" />
    <MokaIcon Icon="MokaIcons.Navigation.Home" Size="MokaSize.Md" />
    <MokaIcon Icon="MokaIcons.Navigation.Home" Size="MokaSize.Lg" />
</div>
```

## Colors

```blazor-preview
<div style="display:flex;gap:12px;align-items:center">
    <MokaIcon Icon="MokaIcons.Status.Info" Color="MokaColor.Info" />
    <MokaIcon Icon="MokaIcons.Status.Warning" Color="MokaColor.Warning" />
    <MokaIcon Icon="MokaIcons.Status.Error" Color="MokaColor.Error" />
    <MokaIcon Icon="MokaIcons.Status.CheckCircle" Color="MokaColor.Success" />
    <MokaIcon Icon="MokaIcons.Toggle.Star" Color="MokaColor.Primary" />
</div>
```

## Icon Library

### Action

| Icon | Usage |
|------|-------|
| `Save` | `MokaIcons.Action.Save` |
| `Delete` | `MokaIcons.Action.Delete` |
| `Edit` | `MokaIcons.Action.Edit` |
| `Add` | `MokaIcons.Action.Add` |
| `Remove` | `MokaIcons.Action.Remove` |
| `Search` | `MokaIcons.Action.Search` |
| `Settings` | `MokaIcons.Action.Settings` |
| `Refresh` | `MokaIcons.Action.Refresh` |
| `Download` | `MokaIcons.Action.Download` |
| `Upload` | `MokaIcons.Action.Upload` |
| `Sun` | `MokaIcons.Action.Sun` |
| `Moon` | `MokaIcons.Action.Moon` |

```blazor-preview
<div style="display:flex;gap:12px;flex-wrap:wrap">
    <MokaIcon Icon="MokaIcons.Action.Save" />
    <MokaIcon Icon="MokaIcons.Action.Delete" />
    <MokaIcon Icon="MokaIcons.Action.Edit" />
    <MokaIcon Icon="MokaIcons.Action.Add" />
    <MokaIcon Icon="MokaIcons.Action.Search" />
    <MokaIcon Icon="MokaIcons.Action.Settings" />
    <MokaIcon Icon="MokaIcons.Action.Refresh" />
    <MokaIcon Icon="MokaIcons.Action.Download" />
    <MokaIcon Icon="MokaIcons.Action.Upload" />
</div>
```

### Navigation

| Icon | Usage |
|------|-------|
| `ArrowLeft/Right/Up/Down` | `MokaIcons.Navigation.ArrowLeft` |
| `ChevronLeft/Right/Up/Down` | `MokaIcons.Navigation.ChevronLeft` |
| `Menu` | `MokaIcons.Navigation.Menu` |
| `Close` | `MokaIcons.Navigation.Close` |
| `Home` | `MokaIcons.Navigation.Home` |
| `MoreHorizontal` | `MokaIcons.Navigation.MoreHorizontal` |
| `MoreVertical` | `MokaIcons.Navigation.MoreVertical` |

```blazor-preview
<div style="display:flex;gap:12px;flex-wrap:wrap">
    <MokaIcon Icon="MokaIcons.Navigation.ArrowLeft" />
    <MokaIcon Icon="MokaIcons.Navigation.ArrowRight" />
    <MokaIcon Icon="MokaIcons.Navigation.ChevronDown" />
    <MokaIcon Icon="MokaIcons.Navigation.Menu" />
    <MokaIcon Icon="MokaIcons.Navigation.Close" />
    <MokaIcon Icon="MokaIcons.Navigation.Home" />
    <MokaIcon Icon="MokaIcons.Navigation.MoreVertical" />
</div>
```

### Status

`Check`, `CheckCircle`, `Warning`, `Error`, `Info`, `HelpCircle`, `Clock`, `Loading`, `Bell`

### Content

`Copy`, `Paste`, `Link`, `Unlink`, `Image`, `Attachment`, `Filter`, `Sort`

### Toggle

`Eye`, `EyeOff`, `Lock`, `Unlock`, `Star`, `Heart`, `ThumbsUp`, `ThumbsDown`

## Custom SVG

Pass a `MokaIconDefinition` constructed with a name and SVG path data to render any custom icon.

```blazor-preview
@code {
    static readonly MokaIconDefinition CustomIcon = new("custom",
        "M12 2L2 7l10 5 10-5-10-5z M2 17l10 5 10-5 M2 12l10 5 10-5");
}
<MokaIcon Icon="CustomIcon" Size="MokaSize.Lg" Color="MokaColor.Primary" />
```
