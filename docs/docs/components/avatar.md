---
title: Avatar
description: User avatar displaying an image, initials, or icon fallback with optional grouping.
order: 25
---

# Avatar

`MokaAvatar` displays a user representation as an image, initials, icon, or auto-generated identicon. Initials sit on a calm label color picked from the initials themselves, the same one every time, or on a color you name with `Color` or `ColorValue`.

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
| `Tile` | `bool` | `false` | A soft 44 px square for an icon rather than a person: a device, a plugin or a section of an app. `SizeValue` and `Rounded` still apply |
| `Color` | `MokaColor?` | -- | Fills an avatar that shows no picture with a theme color, black or white ink on it by its lightness. `Surface` is the neutral fill. Unset, initials get their own label color |
| `ColorValue` | `string?` | -- | Any CSS color or token, such as `#2563eb` or `var(--moka-label-teal)`, filled the same way. Wins over `Color`; anything that isn't a color is ignored |
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

Initials sit on one of six [label colors](/getting-started/label-colors): teal, cyan, indigo, violet, purple and pink, each tint with its own ink, which reads at 4.5:1 or better on every theme, light and dark. The colors that mean something are left out (red, rose, orange, amber, yellow, green, emerald, sky and blue are the error, warning, success and info hues) and so is slate, which reads as switched off, so a person never looks like a warning. The color comes from the initials alone, by an FNV-1a hash of their upper-cased letters: "JD" and "jd" match, and the answer is the same in every process, on every platform, on a WebAssembly load and on both sides of a prerender. Two people with the same initials share a color.

Up to 0.4.2 the color came from `string.GetHashCode`, which .NET seeds per process, so it changed at every restart and could change between the prerender and the interactive render; the palette included the error, warning, success and info colors; and the initials were always `on-primary`, white on the light green and orange of the dark themes.

```blazor-preview
<div style="display:flex;gap:12px;align-items:center">
    <MokaAvatar Initials="JD" />
    <MokaAvatar Initials="AB" />
    <MokaAvatar Initials="MK" />
    <MokaAvatar Initials="ZW" />
</div>
```

## Colors

`Color` fills the avatar with a theme color and `ColorValue` with any color or token, for a team, a role or a color someone picked. The ink on either is black or white, whichever reads better: where the browser can read a color's lightness (`lab(from ...)`, every current browser) it is worked out from the fill itself, so it works on a `var()` too; elsewhere white is blended against the fill. `MokaColor.Surface` keeps the neutral fill and its on-surface ink. Both apply to initials and icons, never under a picture.

```razor
<MokaAvatar Initials="OP" Color="MokaColor.Info" />
<MokaAvatar Initials="DS" ColorValue="#2563eb" />
<MokaAvatar Initials="QA" ColorValue="var(--moka-label-teal)" />
<MokaAvatar Icon="MokaIcons.Navigation.Home" Color="MokaColor.Secondary" />
<MokaAvatar Initials="ME" Color="MokaColor.Surface" />
```

The avatar's own `color` is the ink, and the initials and icon take it, so a `Style` that sets `color` reaches them.

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
