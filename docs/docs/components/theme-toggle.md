---
title: Theme Toggle
description: Round sun and moon button that switches between light and dark mode.
order: 70
---

# Theme Toggle

`MokaThemeToggle` is a round icon button for switching between light and dark mode. It shows a moon while light mode is on and a sun while dark mode is on, and reports each click through `IsDarkChanged`. It does not change the theme itself: bind `IsDark` and pass the matching theme to your `MokaThemeProvider`. To pick from more than two themes, use `MokaThemeSwitcher`.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `IsDark` | `bool` | `false` | Whether dark mode is on. Two-way bindable with `@bind-IsDark` |
| `IsDarkChanged` | `EventCallback<bool>` | -- | Raised with the new value on each click |
| `Disabled` | `bool` | `false` | Disables the button. Clicks do nothing and the mode stays as it is |
| `TooltipPosition` | `MokaTooltipPosition` | `Bottom` | Where the tooltip naming the button opens: `Bottom` in a row such as an app bar, `Top` in a row at the bottom of a view, `Left` or `Right` in a vertical rail |
| `DarkModeLabel` | `string` | `"Switch to dark mode"` | The button's name and tooltip while the light theme is on |
| `LightModeLabel` | `string` | `"Switch to light mode"` | The button's name and tooltip while the dark theme is on |
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
- The button is 36px and round. On hover it tilts 15 degrees while the icon turns back to stay upright.
- A disabled toggle is dimmed, does not tilt, and ignores clicks.
- To follow the operating system setting instead of a manual switch, set `AutoDetectColorScheme` on `MokaThemeProvider`.

## Accessibility

The toggle is a native `<button type="button">` named by `aria-label`, the same text its tooltip shows, because the icon is `aria-hidden`. To translate it, set `DarkModeLabel` and `LightModeLabel`; up to 0.1.19 the name was a fixed English `title`. The button does not expose `aria-pressed`. `Disabled` sets the native `disabled` attribute, which takes the button out of the tab order.
