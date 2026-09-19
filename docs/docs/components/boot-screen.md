---
title: Boot Screen
description: Terminal-style splash screen with a glowing brand mark, a status line, spec chips and a loading bar, plus a static twin for the splash before Blazor starts.
order: 100
---

# Boot Screen

`MokaBootScreen` is a splash screen in the Moka terminal style: a brand mark with a glow, a status line with a pulsing dot, a row of spec chips, an indeterminate bar, a faint grid and a scanline. It follows the active theme, so it fits loading states inside a running app, such as fetching startup data, switching workspaces or reconnecting.

No component can render before Blazor starts. For that first splash, Moka.Red.Core ships a static twin, `moka-boot.css`. See [Pre-boot splash](#pre-boot-splash).

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Visible` | `bool` | `true` | Renders the screen. `false` removes it |
| `Brand` | `string` | `"MOKA"` | Brand mark text |
| `BrandSecondary` | `string?` | -- | Second brand segment, shown after a dot: `MOKA·RED` |
| `StatusText` | `string?` | `"initializing"` | Status line under the brand, with a pulsing dot. Null or empty hides the line. It also labels the screen for screen readers ("Loading" when null) |
| `ChipsContent` | `RenderFragment?` | -- | Row of spec chips. See [Chips](#chips) |
| `ShowGrid` | `bool` | `true` | Faint accent grid behind the brand, fading toward the edges |
| `ShowScanline` | `bool` | `true` | Accent line that sweeps down the screen, or down the container when embedded |
| `ShowBar` | `bool` | `true` | Indeterminate progress bar |
| `FullScreen` | `bool` | `true` | Covers the viewport with `position: fixed`. `false` fills the nearest positioned ancestor instead |
| `ChildContent` | `RenderFragment?` | -- | Extra content below the bar |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Embedded

With `FullScreen="false"` the screen is `position: absolute` and fills its parent. Give the parent `position: relative` and a height.

```blazor-preview
<div style="position:relative;width:100%;height:320px;overflow:hidden;border-radius:8px">
    <MokaBootScreen FullScreen="false" BrandSecondary="RED" StatusText="loading workspace">
        <ChipsContent>
            <span>engine<span class="moka-boot-screen-v">WASM</span></span>
            <span>ui<span class="moka-boot-screen-v">MOKA.RED</span></span>
        </ChipsContent>
    </MokaBootScreen>
</div>
```

## While Something Loads

Bind `Visible` to your loading flag. The screen covers the content until the work is done.

```blazor-preview
@code {
    bool _switching;
    string _workspace = "Personal";

    async Task SwitchAsync()
    {
        _switching = true;
        await Task.Delay(2000);
        _workspace = _workspace == "Personal" ? "Team" : "Personal";
        _switching = false;
    }
}

<div style="position:relative;width:100%;height:260px;overflow:hidden;border-radius:8px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:12px">
    <MokaText>Workspace: @_workspace</MokaText>
    <MokaButton OnClick="SwitchAsync">Switch workspace</MokaButton>

    <MokaBootScreen Visible="_switching" FullScreen="false" Brand="SWITCHING" StatusText="syncing workspace" ShowScanline="false" />
</div>
```

## Chips

Each chip is a `span` holding a label, followed by a nested `span` with the class `moka-boot-screen-v` for the value. The screen colors the value with the accent.

```razor
<MokaBootScreen BrandSecondary="RED">
    <ChipsContent>
        <span>engine<span class="moka-boot-screen-v">WASM</span></span>
        <span>build<span class="moka-boot-screen-v">0.1.12</span></span>
    </ChipsContent>
</MokaBootScreen>
```

## Minimal

Turn off the grid, scanline and bar for a quieter screen.

```blazor-preview
<div style="position:relative;width:100%;height:220px;overflow:hidden;border-radius:8px">
    <MokaBootScreen FullScreen="false" Brand="MOKA" BrandSecondary="NOTE" StatusText="opening notebook"
                    ShowGrid="false" ShowScanline="false" ShowBar="false" />
</div>
```

## Covering the App

At the default `FullScreen="true"` the screen covers the whole viewport, above page content. Put it in the layout and show it while the app loads its startup data:

```razor
@* MainLayout.razor *@
@inherits LayoutComponentBase
@inject WorkspaceService Workspaces

<MokaBootScreen Visible="_loading" BrandSecondary="RED" StatusText="@_status" />
@Body

@code {
    bool _loading = true;
    string _status = "loading workspace";

    protected override async Task OnInitializedAsync()
    {
        await Workspaces.LoadAsync();
        _loading = false;
    }
}
```

`WorkspaceService` stands for your own startup work.

## Pre-boot Splash

Blazor WebAssembly and MAUI Blazor Hybrid show `index.html` until the runtime starts. Link `moka-boot.css` from Moka.Red.Core in the head, and put the splash markup inside the element the root component mounts on. Blazor replaces that element's content on its first render, so the splash goes away without any code.

```html
<head>
    <link rel="stylesheet" href="_content/Moka.Red.Core/moka-boot.css" />
</head>
<body>
    <div id="app">
        <div class="moka-boot">
            <div class="moka-boot-brand">
                <div class="moka-boot-mark">MOKA<span class="moka-boot-dot">·</span>RED</div>
                <div class="moka-boot-subtitle">
                    <span class="moka-boot-pulse"></span>
                    <span>initializing runtime</span>
                </div>
                <div class="moka-boot-chips">
                    <span>engine<span class="moka-boot-v">WASM</span></span>
                    <span>ui<span class="moka-boot-v">MOKA.RED</span></span>
                </div>
                <div class="moka-boot-bar"><span></span></div>
            </div>
        </div>
    </div>
    <script src="_framework/blazor.webassembly.js"></script>
</body>
```

The `mokared-wasm` and `mokared-maui` templates ship this markup. Leave out any element you don't need: the subtitle, the chips or the bar.

The static file uses its own class names (`moka-boot-*`, and `moka-boot-v` for chip values). The component uses `moka-boot-screen-*`. To hand over without a visible jump, render `MokaBootScreen` with the same brand while the app finishes loading.

### Colors

`moka-boot.css` runs before any theme exists, so it can't read the `--moka-*` tokens. It defines its own custom properties on `.moka-boot`:

| Property | Default | Used for |
|----------|---------|----------|
| `--moka-boot-bg` | `#060608` | Background |
| `--moka-boot-accent` | `#ef5350` | Brand mark, scanline and bar |
| `--moka-boot-accent-glow` | `rgba(239, 83, 80, 0.12)` | Glow behind the brand |
| `--moka-boot-accent-glow-strong` | `rgba(239, 83, 80, 0.5)` | Glow around the brand text |
| `--moka-boot-accent-faint` | `rgba(239, 83, 80, 0.03)` | Grid lines |
| `--moka-boot-bar-track` | `rgba(239, 83, 80, 0.08)` | Bar track |
| `--moka-boot-text` | `#e8e8ec` | Dot between the brand segments |
| `--moka-boot-text-2` | `#a0a0aa` | Status line |
| `--moka-boot-text-3` | `#6a6a74` | Chip labels |
| `--moka-boot-pulse-color` | `#00e676` | Status dot |
| `--moka-boot-grid-size` | `48px` | Grid cell size |

To re-tint the splash, set them in a stylesheet that loads after `moka-boot.css`:

```html
<style>
    .moka-boot {
        --moka-boot-accent: #42a5f5;
        --moka-boot-accent-glow: rgba(66, 165, 245, 0.12);
        --moka-boot-accent-glow-strong: rgba(66, 165, 245, 0.5);
        --moka-boot-accent-faint: rgba(66, 165, 245, 0.03);
        --moka-boot-bar-track: rgba(66, 165, 245, 0.08);
    }
</style>
```

## Behaviour

- The screen is a `role="status"` region with `aria-live="polite"`, labelled by `StatusText`. A new `StatusText` is read out.
- Full screen, it sits at `--moka-z-modal`, above page content. Embedded, it has no z-index of its own.
- The scanline sweeps from the top edge to the bottom edge of whatever the screen covers, the viewport or the embedding container.
- With `prefers-reduced-motion`, the animations stop through `moka.css`. The static `moka-boot.css` stops them too, with a rule of its own, since `moka.css` is not on the page before Blazor starts.

Up to 0.1.12 the embedded scanline moved only its own 2px height instead of sweeping the container, and `moka-boot.css` kept animating with reduced motion on.
