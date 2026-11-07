---
title: Boot Screen
description: A screen for an app starting, a reconnect or a panel loading, in seven variants, with steps, progress and a failed state, plus a static twin for the splash before Blazor starts.
order: 100
---

# Boot Screen

`MokaBootScreen` covers the app, or a part of it, while something starts: fetching startup data, switching workspaces, reconnecting, a plugin view loading. It says how far along it is, lists the steps if you give it some, and says what went wrong when startup fails, with a way out.

It draws one of seven variants. With none named it draws **Mark** on the new design (`MokaTheme.Default` and the other presets) and **Classic**, exactly as before, on `MokaTheme.Dark` and `Light`. In a small box any variant draws as a compact card row.

No component can render before Blazor starts. For that first splash, Moka.Red.Core ships a static twin, `moka-boot.css`. See [Pre-boot splash](#pre-boot-splash).

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `BootVariant` | `MokaBootVariant?` | -- | How it draws: `Mark`, `Log`, `Steps`, `Classic`, `Terminal`, `Pulse` or `Horizon`. Null is `Mark` on the new design and `Classic` on Dark and Light. (`Variant`, from the base class, is the button-style variant every visual component has, and the boot screen ignores it.) |
| `Visible` | `bool` | `true` | Renders the screen. `false` removes it |
| `Brand` | `string` | `"MOKA"` | The brand |
| `BrandSecondary` | `string?` | -- | Second part of the brand, after a dot: `Moka·Red`. Muted on the new design |
| `Logo` | `RenderFragment?` | -- | Something to draw beside the brand, such as an app icon. Without it the new design draws the brand's first letter on an accent tile |
| `StatusText` | `string?` | `"initializing"` | The status line. With `Steps`, the running step's label stands in for it while it is empty or left at its default. Null or empty hides the line |
| `ChipsContent` | `RenderFragment?` | -- | Facts such as the version and engine. See [Chips](#chips) |
| `Steps` | `IReadOnlyList<MokaBootStep>?` | -- | The steps of the startup. See [Giving it steps](#giving-it-steps) |
| `ShowTimes` | `bool` | `true` | A log's time column and the step list's durations |
| `MaxLines` | `int` | `8` | How many lines a log keeps. Older lines scroll off the top |
| `Title` | `string` | `"Starting up"` | The label over the step list |
| `Hint` | `string?` | -- | A line under the bar: "Usually about 5 seconds" |
| `Aside` | `RenderFragment?` | -- | Content under the brand on the left of `Steps` |
| `Progress` | `double?` | -- | How far along it is, 0 to 100. Null fills the bar from `Steps` when there are any, and otherwise slides a piece along it |
| `ShowBar` | `bool` | `true` | Draws the progress bar |
| `Error` | `string?` | -- | What went wrong. When set, the failed state shows. A failed step shows it too |
| `OnRetry` | `EventCallback` | -- | Called by Try again. Without it there is no button |
| `RetryText` | `string` | `"Try again"` | Text of the Try again button |
| `StepCountText` | `Func<int, int, string>?` | -- | Writes the card row's count from the current step's number and the number of steps. Null writes "Step 3 of 5" |
| `StepsDoneText` | `Func<int, int, string>?` | -- | Writes a log's footer count from the finished steps and the number of steps. Null writes "3 of 5 steps" |
| `StoppedText` | `Func<int, int, string>?` | -- | Writes the line under a step list that stopped, from the failed step's number and the number of steps. Null writes "Stopped at step 3 of 5". `Error` takes its place when set |
| `StepStateText` | `Func<MokaBootStepState, string>?` | -- | Names a step's state for screen readers, read before each step of the list. Null reads "Waiting", "Running", "Done", "Warning", "Failed" and "Skipped" |
| `LogWordText` | `Func<MokaBootStepState, string>?` | -- | The state word a log prints on each line, which Terminal draws in capitals inside its brackets. Null prints "ok", "warn", "fail", "skip" and "run", and Terminal's "OK", "WARN", "FAIL", "SKIP" and ".." |
| `DurationText` | `Func<TimeSpan, string>?` | -- | Writes how long a step or the whole startup took. Null writes "120 ms" under a second, else "1.39 s" |
| `ErrorContent` | `RenderFragment?` | -- | More for the failed state, beside Try again: details, a link or a second button |
| `Icon` | `MokaIconDefinition?` | -- | An icon the card row shows in place of its spinner |
| `ShowGrid` | `bool` | `true` | The faint accent grid behind `Classic` |
| `ShowScanline` | `bool` | `true` | The line sweeping down `Classic` |
| `FullScreen` | `bool` | `true` | Covers the window, and the page under it is inert until it goes. `false` fills the nearest positioned ancestor instead |
| `ChildContent` | `RenderFragment?` | -- | Extra content under the rest |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Mark

The brand in the title font, a thin line under it and one sentence. Calm enough for any app, which is why it is the default on the new design. The facts in `ChipsContent` become one quiet mono line at the bottom.

```razor
<div style="position:relative;height:320px;overflow:hidden;border-radius:12px">
    <MokaBootScreen FullScreen="false" BootVariant="MokaBootVariant.Mark"
                    Brand="Moka" BrandSecondary="Red" StatusText="Loading your workspace">
        <ChipsContent>
            <span>v0.2.2</span>
            <span>wasm</span>
        </ChipsContent>
    </MokaBootScreen>
</div>
```

With a `Progress` the line fills, with a 0.4 second width transition. Without one, a 30% piece slides end to end at a steady speed.

## Log

Each step prints one line with its time and a state word: `ok`, `warn`, `fail`, `skip`, and `run` with a blinking cursor on the step under way. Waiting steps don't print yet. For developer tools and apps whose people like to see the machinery. The time is when the step started, counted from the first, and the footer counts finished steps.

```razor
<MokaBootScreen BootVariant="MokaBootVariant.Log" Brand="Moka" BrandSecondary="Term"
                Steps="_steps" MaxLines="6">
    <ChipsContent><span>v1.4.0</span></ChipsContent>
</MokaBootScreen>
```

## Steps

The brand on the left, a checklist on the right: each step with its state, a detail line and how long it took. For apps with a known startup, such as signing in and opening a workspace. Below 512 px wide the two columns stack.

```razor
<MokaBootScreen BootVariant="MokaBootVariant.Steps" Brand="Moka" BrandSecondary="Gate"
                Steps="_steps" Title="Starting up" Hint="Usually about 5 seconds"
                Error="@_error" OnRetry="StartAsync">
    <Aside>Your home router</Aside>
    <ChipsContent><span>v0.9.5</span><span>router.local</span></ChipsContent>
</MokaBootScreen>
```

A failed step says why under its label and offers Try again on its row. The steps after it wait, and the bar gives way to "Stopped at step 3 of 5".

## Classic

The original boot screen: the brand in the accent with a glow, a pulsing status, chips, a faint grid, a scanline and a bar. It is what Dark and Light draw with no variant named, exactly as before, and on the new design you can name it to get it in the theme's colors.

```razor
<MokaBootScreen BootVariant="MokaBootVariant.Classic" BrandSecondary="RED" StatusText="loading workspace">
    <ChipsContent>
        <span>engine<span class="moka-boot-screen-v">WASM</span></span>
        <span>ui<span class="moka-boot-screen-v">MOKA.RED</span></span>
    </ChipsContent>
</MokaBootScreen>
```

Turn off the grid, scanline and bar for a quieter screen: `ShowGrid="false" ShowScanline="false" ShowBar="false"`.

## Terminal

Classic's family: near black, a glowing mono wordmark, the accent as light. Under the wordmark the steps print as a boot sequence, `[  OK  ]` and `[ WARN ]` with an uptime stamp, and the step under way ends in a blinking block cursor. With no steps it shows a prompt with the status. With a progress it draws a block bar, `[████░░░░] 46%`.

```razor
<MokaBootScreen BootVariant="MokaBootVariant.Terminal" BrandSecondary="TERM" Steps="_steps" />
```

## Pulse

Classic's family again: rings breathe out from a glowing core while two dots orbit it, over the glowing wordmark, the status and the bar. The core holds the brand's first letter, or `Logo`.

```razor
<MokaBootScreen BootVariant="MokaBootVariant.Pulse" BrandSecondary="RED" StatusText="reconnecting" />
```

## Horizon

The glowing wordmark over a lit horizon, with a grid floor running toward you under it.

```razor
<MokaBootScreen BootVariant="MokaBootVariant.Horizon" BrandSecondary="RED" StatusText="syncing workspace" />
```

## Giving It Steps

`Log`, `Steps` and `Terminal` list `Steps`, and the card row counts them. Every variant shows the failed state when a step fails, and the running step's label stands in for `StatusText` while that is empty or left at its default.

```csharp
public sealed record MokaBootStep(
    string Label,
    MokaBootStepState State = MokaBootStepState.Waiting,
    string? Detail = null,
    TimeSpan? Elapsed = null,
    string? Error = null);

public enum MokaBootStepState { Waiting, Running, Done, Warning, Failed, Skipped }
```

`Detail` is a short fact ("42 of 118"), `Elapsed` how long the step took or has taken so far, and `Error` the sentence under a failed step. Pass a new list on each change: the screen reads it on every render.

```razor
@code {
    IReadOnlyList<MokaBootStep> _steps =
    [
        new("Connect to the router", MokaBootStepState.Done, "router.local", TimeSpan.FromMilliseconds(120)),
        new("Check your sign-in", MokaBootStepState.Done, "BC, owner", TimeSpan.FromMilliseconds(40)),
        new("Load devices", MokaBootStepState.Running, "42 of 118"),
        new("Load firewall rules"),
        new("Start live traffic")
    ];
}
```

With no `Progress`, the bar fills by finished steps: done, warning and skipped ones count.

## When Startup Fails

Set `Error` and the line turns into what went wrong and a way out. `OnRetry` draws Try again; `ErrorContent` sits beside it for details, a link or a second button.

```razor
<MokaBootScreen Brand="Moka" BrandSecondary="Red"
                Error="Couldn't reach the server" OnRetry="StartAsync">
    <ErrorContent>
        <MokaButton Variant="MokaVariant.Outlined" Color="MokaColor.Secondary" OnClick="ShowDetails">Show details</MokaButton>
    </ErrorContent>
</MokaBootScreen>
```

A failed step does the same with its own label and `Error`, so a step list needs no separate `Error`. Every variant has this state: the classic ones turn their status red and stop their motion.

## In Your Own Words

Every word the screen writes itself can be replaced: `Title`, `RetryText` and `StatusText` take text, and the counts, state words and durations take a function. Leave one null for the English default.

```razor
<MokaBootScreen BootVariant="MokaBootVariant.Steps" Steps="_steps"
                Title="Startar" RetryText="Försök igen"
                StepCountText="@((step, total) => $"Steg {step} av {total}")"
                StoppedText="@((step, total) => $"Stannade vid steg {step} av {total}")"
                StepStateText="@(state => state == MokaBootStepState.Done ? "Klar" : "Pågår")" />
```

## In a Small Box

With `FullScreen="false"` the screen fills its nearest positioned ancestor. Give that a position and a height. When the box is under 360 by 240 px, any variant draws as a card row instead: an icon tile with a spinner (or `Icon`), the status, "Step 2 of 3" with the time so far, and a thin bar. Failed, it shows the problem and a small Try again. A container query decides, so nothing measures in script.

```razor
<div style="position:relative;width:320px;height:140px;border-radius:12px;overflow:hidden">
    <MokaBootScreen FullScreen="false" StatusText="Connecting to the collector"
                    Steps="_steps" Icon="MokaIcons.File.Database" />
</div>
```

On Dark and Light a screen with no variant named keeps its old look in a small box too.

## Covering the App

At the default `FullScreen="true"` the screen covers the whole window, above page content, and the page under it is inert until it goes: Tab and a screen reader's cursor stay on the screen, and the page doesn't scroll. Put it in the layout and show it while the app loads its startup data:

```razor
@* MainLayout.razor *@
@inherits LayoutComponentBase
@inject WorkspaceService Workspaces

<MokaBootScreen Visible="_loading" Brand="Moka" BrandSecondary="Red"
                StatusText="Loading your workspace" Error="@_error" OnRetry="LoadAsync" />
@Body

@code {
    bool _loading = true;
    string? _error;

    protected override Task OnInitializedAsync() => LoadAsync();

    async Task LoadAsync()
    {
        _error = null;
        try
        {
            await Workspaces.LoadAsync();
            _loading = false;
        }
        catch (HttpRequestException)
        {
            _error = "Couldn't reach the server";
        }
    }
}
```

`WorkspaceService` stands for your own startup work.

## Chips

Each chip is a `span` holding a label, followed by a nested `span` with the class `moka-boot-screen-v` for the value. The classic variants draw them as a row of chips with the value in the accent. Mark, Log and Steps draw them as one quiet mono line, separated by dots.

```razor
<ChipsContent>
    <span>engine<span class="moka-boot-screen-v">WASM</span></span>
    <span>build<span class="moka-boot-screen-v">0.2.3</span></span>
</ChipsContent>
```

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
                <div class="moka-boot-mark"><span class="moka-boot-logo">M</span>Moka<span class="moka-boot-dot">·</span><span class="moka-boot-second">Red</span></div>
                <div class="moka-boot-bar"><span></span></div>
                <div class="moka-boot-subtitle"><span>Loading your workspace</span></div>
            </div>
            <div class="moka-boot-chips"><span>v0.2.3</span><span>wasm</span></div>
        </div>
    </div>
    <script src="_framework/blazor.webassembly.js"></script>
</body>
```

It draws Mark in the colors of `MokaTheme.Red`, the default theme, and of `RedLight` when the OS asks for light. It imports the library's fonts, so the splash and the app draw the same letters.

The `mokared-wasm` and `mokared-maui` templates ship splash markup. Leave out any element you don't need: the logo, the subtitle, the chips or the bar.

### Handing Over Without a Jump

On the Red theme, render `MokaBootScreen` with the same brand and status while the app finishes loading. Mark sits where the splash sat, at the same sizes and in the same colors, so the handover doesn't show:

```razor
<MokaBootScreen Visible="_loading" Brand="Moka" BrandSecondary="Red" StatusText="Loading your workspace">
    <ChipsContent><span>v0.2.3</span><span>wasm</span></ChipsContent>
</MokaBootScreen>
```

On another preset the splash still shows Red's colors, so re-tint it to match (below).

### The Original Splash

Add `moka-boot--matrix` for the splash as it was: a glowing mono brand, a pulsing status, chips, a faint grid and a scanline.

```html
<div class="moka-boot moka-boot--matrix">
    <div class="moka-boot-brand">
        <div class="moka-boot-mark">MOKA<span class="moka-boot-dot">·</span>RED</div>
        <div class="moka-boot-subtitle"><span class="moka-boot-pulse"></span><span>initializing runtime</span></div>
        <div class="moka-boot-chips">
            <span>engine<span class="moka-boot-v">WASM</span></span>
            <span>ui<span class="moka-boot-v">MOKA.RED</span></span>
        </div>
        <div class="moka-boot-bar"><span></span></div>
    </div>
</div>
```

Markup written for the original splash works without the class too, drawn as Mark: the pulse hides and the chips move to the bottom line.

### Colors

`moka-boot.css` runs before any theme exists, so it can't read the `--moka-*` tokens. It defines its own custom properties on `.moka-boot`:

| Property | Mark, dark | Mark, light | Used for |
|----------|------------|-------------|----------|
| `--moka-boot-bg` | `#0C0C0E` | `#F5F5F6` | Background |
| `--moka-boot-text` | `#ECECEF` | `#16161A` | Brand |
| `--moka-boot-text-2` | `#A0A0AA` | `#5A5A64` | Second part of the brand, status |
| `--moka-boot-text-3` | `#82828C` | `#666670` | Dot, chips |
| `--moka-boot-track` | `#27272D` | `#DFDFE3` | Bar track |
| `--moka-boot-accent` | `#EF5350` | `#C62828` | Logo tile, bar |
| `--moka-boot-on-accent` | `#0A0E15` | `#FFFFFF` | Logo letter |

`moka-boot--matrix` keeps its own: `--moka-boot-bg` `#060608`, `--moka-boot-accent` `#ef5350`, `--moka-boot-accent-glow`, `--moka-boot-accent-glow-strong`, `--moka-boot-accent-faint`, `--moka-boot-bar-track`, `--moka-boot-pulse-color` `#00e676` and `--moka-boot-grid-size` `48px`.

To re-tint the splash, set them in a stylesheet that loads after `moka-boot.css`. A rule on `.moka-boot` wins in both color schemes:

```html
<style>
    .moka-boot {
        --moka-boot-accent: #6E92FF;
        --moka-boot-bg: #0A0E15;
    }
</style>
```

## Behaviour

- A hidden status region is on the page whether or not the screen shows, and gets its text only after it is there, so a screen reader reads the first status and each step after it. A failure is read with its detail. The screen itself is not a live region.
- Full screen, it sits at `--moka-z-modal`, the page under it is inert and doesn't scroll, and Blazor's reconnect and error UI stay usable. Embedded, it has no z-index of its own and leaves the page alone.
- Every animation runs at a steady speed: the bar's piece, the spinner, the cursor, the rings and orbit, the horizon's floor. With `prefers-reduced-motion` they stop and rest where they read: the piece still at the start of the bar, one ring half grown. The static `moka-boot.css` stops its own, since `moka.css` is not on the page before Blazor starts.

Up to 0.2.2 the status region was created together with its text, so most screen readers never read the first status; a full-screen screen left the page under it live, so Tab walked its controls; the bar's keyframes stopped it dead mid-track and left the track empty for about a quarter of each loop; and the splash drew the old look, so an app on Red jumped at handoff.
