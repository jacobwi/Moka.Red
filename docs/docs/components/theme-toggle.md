---
title: Theme Toggle
description: Switches between light and dark mode, or light, dark and system, as a button, a switch, a segmented control or a chip.
order: 70
---

# Theme Toggle

`MokaThemeToggle` switches between light and dark mode. By default it's a round icon button that shows a moon while light mode is on and a sun while dark mode is on, and reports each click through `IsDarkChanged`. `Look` draws it another way and `ShowSystem` adds a third state that follows the device. It does not change the theme itself: bind `IsDark` or `Mode` and pass the matching theme to your `MokaThemeProvider`. To pick from more than two themes, use `MokaThemeSwitcher`.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `IsDark` | `bool` | `false` | Whether dark mode is on. Two-way bindable with `@bind-IsDark` |
| `IsDarkChanged` | `EventCallback<bool>` | -- | Raised with the new value on each click. Not raised when the new state is System |
| `Mode` | `MokaColorMode?` | `null` | The current state, for three states. Two-way bindable to a `MokaColorMode` with `@bind-Mode`. Null follows `IsDark` |
| `ModeChanged` | `EventCallback<MokaColorMode>` | -- | Raised with the new state on every change |
| `ShowSystem` | `bool` | `false` | Adds System as a third state |
| `Look` | `MokaThemeToggleLook` | `Icon` | How it's drawn. See [Looks](#looks) |
| `Size` | `MokaSize` | `Md` | The size of every look |
| `Disabled` | `bool` | `false` | Disables the button. Clicks do nothing and the mode stays as it is |
| `TooltipPosition` | `MokaTooltipPosition` | `Bottom` | Where the tooltip naming the button opens: `Bottom` in a row such as an app bar, `Top` in a row at the bottom of a view, `Left` or `Right` in a vertical rail |
| `DarkModeLabel` | `string` | `"Switch to dark mode"` | The button's name and tooltip while the light theme is on |
| `LightModeLabel` | `string` | `"Switch to light mode"` | The button's name and tooltip while the dark theme is on |
| `SystemModeLabel` | `string` | `"Follow this device"` | The name and tooltip when a click would move to System |
| `LightText`, `DarkText`, `SystemText` | `string` | `"Light"`, `"Dark"`, `"System"` | The word for each state, on segments, chips and tooltips |
| `GroupLabel` | `string` | `"Color mode"` | The accessible name of the radio group the segmented looks and a three-state switch are |
| `SwitchLabel` | `string` | `"Dark mode"` | The name and tooltip of the two-state switch, which is on while dark |
| `Class` | `string?` | -- | Additional CSS classes on the button |
| `Style` | `string?` | -- | Additional inline styles on the button |

## Basic

```blazor-preview
<div style="display:flex;align-items:center;gap:12px">
    <MokaThemeToggle @bind-IsDark="_dark" />
    <span style="font-size:var(--moka-font-size-sm);color:var(--moka-color-on-surface-variant)">@(_dark ? "Dark mode" : "Light mode")</span>
</div>

@code {
    private bool _dark;
}
```

## Looks

Every look works with two states and with three.

| Look | What it draws | What a screen reader meets |
|------|---------------|----------------------------|
| `Icon` | The round button. A click moves on to the next state | A button named by what a click does |
| `Switch` | A sliding track with a sun and a moon, and a monitor with three states | A switch with two states, a radio group of three with three |
| `Segmented` | A segmented control of icons | A radio group, each radio named by its word |
| `IconsAndWords` | A segmented control with an icon and a word on each segment | A radio group |
| `Chip` | A small pill with the current state's icon and word. A click moves on | A button named by its word and what a click does |

```razor
<MokaThemeToggle Look="MokaThemeToggleLook.Switch" @bind-IsDark="_dark" />
<MokaThemeToggle Look="MokaThemeToggleLook.IconsAndWords" Size="MokaSize.Sm" @bind-IsDark="_dark" />
```

The segmented looks are a `MokaSegmentedControl`, so they follow its sizes and its keyboard: Tab reaches the selected segment and the arrow keys move it.

## Three states

`ShowSystem` adds System. Bind `Mode` rather than `IsDark`, since System is neither.

```razor
<MokaThemeToggle Look="MokaThemeToggleLook.Segmented" ShowSystem @bind-Mode="_mode" />

@code {
    private MokaColorMode _mode = MokaColorMode.System;
}
```

`MokaColorMode` is the same type `MokaAppearance.Mode` holds, in the namespace `Moka.Red.ThemeGen.Appearance`. It ships in `Moka.Red.Core`, so you don't need ThemeGen to use it.

With three states the `Icon` look goes Light, Dark, System and shows the current state, because the page can't show that it's following the device. With two it keeps showing what a click gives.

## Switching the Theme

The bound value picks the theme for `MokaThemeProvider`, which restyles everything inside it.

```blazor-preview
<MokaThemeProvider Theme="_dark ? MokaTheme.Dark : MokaTheme.Light">
    <MokaCard Outlined Title="Appearance" Subtitle="Light or dark mode" Style="width:280px">
        <div style="display:flex;align-items:center;justify-content:space-between">
            <span>@(_dark ? "Dark" : "Light")</span>
            <MokaThemeToggle @bind-IsDark="_dark" />
        </div>
    </MokaCard>
</MokaThemeProvider>

@code {
    private bool _dark;
}
```

## In an App Bar

```blazor-preview
<MokaAppBar Title="Moka Admin" Bordered Style="width:100%">
    <EndContent>
        <MokaThemeToggle @bind-IsDark="_dark" />
    </EndContent>
</MokaAppBar>

@code {
    private bool _dark;
}
```

## Behaviour

- A `MokaTooltip` names the mode a click switches to: "Switch to dark mode" or "Switch to light mode", from `DarkModeLabel` and `LightModeLabel`. It shows on hover and on keyboard focus, also while the toggle is disabled. In dark mode the button also gets the `moka-theme-toggle--dark` class.
- The button sits inside the tooltip's element. `Class`, `Style` and the margin still land on the button, so a style that places the toggle in its row with auto margins or flex settings belongs on an element of your own around it.
- The `Icon` look is 36px and round at the default size, and `Size` makes it larger or smaller. On hover it tilts 15 degrees while the icon turns back to stay upright.
- A disabled toggle is dimmed, does not tilt, and ignores clicks.
- To follow the operating system setting instead of a manual switch, set `AutoDetectColorScheme` on `MokaThemeProvider`.

## Accessibility

Every look that shows only icons has a name and a `MokaTooltip` for each thing you can press. Below is the default `Icon` look.

The toggle is a native `<button type="button">` named by `aria-label`, the same text its tooltip shows, because the icon is `aria-hidden`. To translate it, set `DarkModeLabel` and `LightModeLabel`; up to 0.1.19 the name was a fixed English `title`. The button does not expose `aria-pressed`. `Disabled` sets the native `disabled` attribute, which takes the button out of the tab order.
