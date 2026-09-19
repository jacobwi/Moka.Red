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
| `Alt` | `string?` | -- | Alt text for the image. A clickable avatar also uses it as its accessible name |
| `Initials` | `string?` | -- | Fallback initials (e.g., "JD") |
| `Icon` | `MokaIconDefinition?` | -- | Fallback icon when no image or initials |
| `IdenticonValue` | `string?` | -- | String to generate a deterministic identicon from |
| `ShowIdenticon` | `bool` | `true` | Whether to show identicon as final fallback |
| `Bordered` | `bool` | `false` | White border for overlapping avatars |
| `Size` | `MokaSize` | `Md` | `Xs` (24px), `Sm` (32px), `Md` (40px), `Lg` (56px) |
| `Rounded` | `MokaRounding?` | `Full` | `Full` (circle), `None` (square), or any rounding value |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Click handler. Makes the avatar a button: tab stop, focus ring, Enter and Space |
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
<MokaAvatar Icon="MokaIcons.Action.Search" Size="MokaSize.Lg" />
```

## Clickable Avatar

```blazor-preview
@code { string _msg = ""; }
<div style="display:flex;gap:12px;align-items:center">
    <MokaAvatar Initials="JD" Alt="Jane Doe" OnClick="@(() => _msg = "Opened Jane's profile")" />
    <span>@_msg</span>
</div>
```

An avatar with `OnClick` is a button to the keyboard and to screen readers. It joins the tab order with the focus ring, and Enter or Space click it, the same as the mouse. Space doesn't scroll the page. An avatar without `OnClick` takes no focus.

Its accessible name is `Alt`, whatever the avatar shows, so give a clickable avatar an `Alt`. Without one, screen readers only get the initials, and an image, icon or identicon avatar has no name at all. An `aria-label` you pass wins over `Alt`:

```razor
<MokaAvatar Src="@user.PhotoUrl" Alt="@user.Name" aria-label="Account menu" OnClick="OpenMenu" />
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
