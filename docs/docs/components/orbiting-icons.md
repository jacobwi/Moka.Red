---
title: Orbiting Icons
description: Icons that orbit around a center point with configurable radius, speed, and direction.
order: 84
---

# Orbiting Icons

`MokaOrbitingIcons` displays a set of icons rotating around a circular orbit path. Place content in the center using `ChildContent`. Control the orbit radius, speed, direction, and visual styling of both the icons and the orbit ring.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | — | Content rendered at the center of the orbit |
| `Icons` | `IReadOnlyList<MokaIconDefinition>` | — | Icons to display on the orbit |
| `Radius` | `int` | `120` | Orbit radius in pixels |
| `Duration` | `double` | `20` | Revolution duration in seconds |
| `Reverse` | `bool` | `false` | Rotate counter-clockwise |
| `IconSize` | `int` | `20` | Icon size in pixels |
| `IconColor` | `string?` | — | Icon color override |
| `ShowPath` | `bool` | `true` | Show the orbit ring |
| `PathColor` | `string?` | — | Orbit ring color |
| `Paused` | `bool` | `false` | Pause the animation |
| `Size` | `int?` | — | Container size override in pixels |

## Default Orbit

```blazor-preview
<div style="display:flex;justify-content:center;padding:40px">
    <MokaOrbitingIcons Icons="@_icons" Radius="100">
        <MokaIcon Icon="MokaIcons.Status.Info" SizeValue="32" />
    </MokaOrbitingIcons>
</div>
@code {
    IReadOnlyList<MokaIconDefinition> _icons = new[] { MokaIcons.Action.Edit, MokaIcons.Action.Delete, MokaIcons.Action.Add, MokaIcons.Action.Search };
}
```

## Reverse Direction

```blazor-preview
<div style="display:flex;justify-content:center;padding:40px">
    <MokaOrbitingIcons Icons="@_icons" Radius="90" Reverse Duration="15" IconColor="#d32f2f">
        <MokaText Weight="MokaFontWeight.Bold">Center</MokaText>
    </MokaOrbitingIcons>
</div>
@code {
    IReadOnlyList<MokaIconDefinition> _icons = new[] { MokaIcons.Navigation.Home, MokaIcons.Navigation.Menu, MokaIcons.Action.Settings };
}
```

## Hidden Orbit Path

```blazor-preview
<div style="display:flex;justify-content:center;padding:40px">
    <MokaOrbitingIcons Icons="@_icons" ShowPath="false" Radius="80" Duration="12">
        <MokaAvatar Initials="MR" Size="MokaSize.Lg" />
    </MokaOrbitingIcons>
</div>
@code {
    IReadOnlyList<MokaIconDefinition> _icons = new[] { MokaIcons.Action.Star, MokaIcons.Status.Check, MokaIcons.Action.Favorite };
}
```
