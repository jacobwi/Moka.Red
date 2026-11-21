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
| `SizeValue` | `string?` | -- | Any size, such as `3.5rem` or `var(--app-avatar)`. The initials and the icon keep the share of the box they have at `Size` |
| `Rounded` | `MokaRounding?` | `Full` | `Full` (circle), `None` (square), or any rounding value |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Click handler. Makes the avatar a button: tab stop, focus ring, Enter and Space |
| `Tile` | `bool` | `false` | A soft 44 px square for an icon rather than a person: a device, a plugin or a section of an app. `SizeValue` and `Rounded` still apply, and the icon grows and shrinks with the tile |
| `Color` | `MokaColor?` | -- | Fills an avatar that shows no picture with a theme color, black or white ink on it by its lightness. `Surface` is the neutral fill. Unset, initials get their own label color |
| `ColorValue` | `string?` | -- | Any CSS color or token, such as `#2563eb` or `var(--moka-label-teal)`, filled the same way. Wins over `Color`; anything that isn't a color is ignored |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaAvatarGroup

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | `MokaAvatar` elements |
| `Max` | `int` | `5` | How many avatars show, in markup order. The rest are left out and a "+N" avatar counts them. 0 or less shows only the count |
| `Spacing` | `string` | `"-8px"` | Overlap amount (negative margin) |
| `Size` | `MokaSize` | `Md` | When set, the size of every avatar that sets no size of its own, and of the "+N" |
| `SizeValue` | `string?` | -- | When set, the own size of every avatar that sets no size of its own, and of the "+N", as on [`MokaAvatar`](#own-size) |

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

### Own size

`SizeValue` takes a length, `calc()`, `var()` or `clamp()`. The initials and the icon keep the share of the box they have at `Size`'s own size, so `SizeValue="56px"` with the default `Md` draws a larger `Md` avatar: 19.6 px initials and a 22.4 px icon, where `Lg` has 20 px of each. The text share follows the theme's font scale; the shares themselves are those of the default text size. A tile's icon keeps its share too: 20 px at the tile's own 2.75rem, 32 px at 4.4rem. A percentage, or `em`, `ex` or `ch`, sizes only the box and the content keeps `Size`'s sizes, since the box takes a percentage of its container and those units of its own text.

```razor
<MokaAvatar Initials="JD" SizeValue="3.5rem" />
<MokaAvatar Icon="MokaIcons.Navigation.Home" SizeValue="var(--profile-avatar)" />
<MokaAvatar Icon="MokaIcons.Content.Server" Tile SizeValue="2rem" />
```

Up to 0.10.2, `SizeValue` sized only the box: a large avatar kept the initials and icon of its step, and a tile's icon was 20 px at any size.

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

The group shows its first `Max` avatars in the order they stand and leaves the rest out, and a "+N" avatar at the end counts them. The "+N" is a `MokaAvatar` on the neutral fill, so it takes the group's overlap and ring, and its size. Avatars that come and go, such as those in a `@foreach`, move the count.

### Group size

A `Size` or `SizeValue` set on the group sizes every avatar in it that sets neither of its own, and the "+N" with them. An avatar with its own `Size` or `SizeValue` keeps it and takes nothing from the group. With a length `SizeValue` the initials and icons keep their share of the box, as on an avatar alone, from the group's `Size`.

```razor
<MokaAvatarGroup Size="MokaSize.Sm" Max="4">
    @foreach (var member in _members)
    {
        <MokaAvatar Initials="@member.Initials" />
    }
</MokaAvatarGroup>

<MokaAvatarGroup Size="MokaSize.Lg" SizeValue="3.5rem">
    <MokaAvatar Initials="AB" />
    <MokaAvatar Initials="CD" Size="MokaSize.Sm" />
</MokaAvatarGroup>
```

A group without `Size` or `SizeValue` leaves its avatars as they would be alone.

Up to 0.11.3 the group's `Size` and `SizeValue` never reached its avatars, and `Max` left nothing out: every avatar showed and the "+N" never appeared.
