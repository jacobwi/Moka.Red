---
title: Avatar
description: User avatar displaying an image, initials, or icon fallback with optional grouping.
order: 25
---

# Avatar

`MokaAvatar` displays a user representation as an image, initials, icon, or auto-generated identicon. Background color for initials is deterministically generated from the initials text.

`MokaAvatarGroup` stacks multiple avatars with overlap and shows a "+N" overflow indicator.

## Parameters

### MokaAvatar

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Src` | `string?` | -- | Image URL |
| `Alt` | `string?` | -- | Alt text for the image |
| `Initials` | `string?` | -- | Fallback initials (e.g., "JD") |
| `Icon` | `MokaIconDefinition?` | -- | Fallback icon when no image or initials |
| `IdenticonValue` | `string?` | -- | String to generate a deterministic identicon from |
| `ShowIdenticon` | `bool` | `true` | Whether to show identicon as final fallback |
| `Bordered` | `bool` | `false` | White border for overlapping avatars |
| `Size` | `MokaSize` | `Md` | `Xs` (24px), `Sm` (32px), `Md` (40px), `Lg` (56px) |
| `Rounded` | `MokaRounding?` | `Full` | `Full` (circle), `None` (square), or any rounding value |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Click handler |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaAvatarGroup

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | `MokaAvatar` elements |
| `Max` | `int` | `5` | Maximum avatars before showing "+N" |
| `Spacing` | `string` | `"-8px"` | Overlap amount (negative margin) |
| `Size` | `MokaSize` | `Md` | Size applied to all child avatars |

## Image Avatar

```blazor-preview
<div style="display:flex;gap:12px;align-items:center">
    <MokaAvatar Src="https://i.pravatar.cc/150?u=a" Alt="User A" />
    <MokaAvatar Src="https://i.pravatar.cc/150?u=b" Alt="User B" Size="MokaSize.Lg" />
</div>
```

## Initials

Background color is auto-generated from the initials hash.

```blazor-preview
<div style="display:flex;gap:12px;align-items:center">
    <MokaAvatar Initials="JD" />
    <MokaAvatar Initials="AB" />
    <MokaAvatar Initials="MK" />
    <MokaAvatar Initials="ZW" />
</div>
```

## Sizes

```blazor-preview
<div style="display:flex;gap:12px;align-items:center">
    <MokaAvatar Initials="XS" Size="MokaSize.Xs" />
    <MokaAvatar Initials="SM" Size="MokaSize.Sm" />
    <MokaAvatar Initials="MD" Size="MokaSize.Md" />
    <MokaAvatar Initials="LG" Size="MokaSize.Lg" />
</div>
```

## Square Avatars

```blazor-preview
<div style="display:flex;gap:12px">
    <MokaAvatar Initials="SQ" Rounded="MokaRounding.None" />
    <MokaAvatar Initials="RD" Rounded="MokaRounding.Md" />
</div>
```

## Icon Fallback

```blazor-preview
<MokaAvatar Icon="MokaIcons.Action.User" Size="MokaSize.Lg" />
```

## Avatar Group

```blazor-preview
<MokaAvatarGroup Max="3">
    <MokaAvatar Initials="AA" />
    <MokaAvatar Initials="BB" />
    <MokaAvatar Initials="CC" />
    <MokaAvatar Initials="DD" />
    <MokaAvatar Initials="EE" />
</MokaAvatarGroup>
```
