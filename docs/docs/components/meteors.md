---
title: Meteors
description: Animated meteor streaks falling across a container for decorative visual effects.
order: 83
---

# Meteors

`MokaMeteors` renders animated streaks that fall diagonally across the container, creating a meteor-shower visual effect. Wrap any content to add this decorative animation as a background layer.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | — | Content rendered on top of the meteors |
| `Count` | `int` | `12` | Number of meteor streaks |
| `Color` | `string?` | — | Streak color |
| `MinDuration` | `double` | `3` | Minimum animation duration in seconds |
| `MaxDuration` | `double` | `8` | Maximum animation duration in seconds |
| `Angle` | `int` | `35` | Streak angle in degrees |
| `MinLength` | `int` | `30` | Minimum streak length in pixels |
| `MaxLength` | `int` | `80` | Maximum streak length in pixels |
| `TravelDistance` | `int` | `800` | Travel distance in pixels |
| `MinHeight` | `string?` | — | Minimum height of the container |

## Default Meteors

```blazor-preview
<MokaMeteors MinHeight="300px">
    <div style="display:flex;align-items:center;justify-content:center;height:100%">
        <MokaHeading Level="3">Meteor Shower</MokaHeading>
    </div>
</MokaMeteors>
```

## Custom Color and Count

```blazor-preview
<MokaMeteors Count="20" Color="#d32f2f" MinDuration="2" MaxDuration="5" MinHeight="250px">
    <div style="display:flex;align-items:center;justify-content:center;height:100%">
        <MokaText>Red streaks</MokaText>
    </div>
</MokaMeteors>
```

## Steep Angle with Long Streaks

```blazor-preview
<MokaMeteors Angle="60" MinLength="50" MaxLength="120" Count="8" MinHeight="250px">
    <div style="display:flex;align-items:center;justify-content:center;height:100%">
        <MokaText>Steep, long streaks</MokaText>
    </div>
</MokaMeteors>
```
