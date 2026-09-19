---
title: Diagnostics
description: Dev-time overlay with a log console, a theme token inspector, render and JS interop tracking, memory stats and an event log.
order: 105
---

# Diagnostics

`Moka.Red.Diagnostics` is a tool for development builds. `MokaDiagnosticsOverlay` puts a small "M" badge in a corner of the page. A click on it, or Ctrl+Shift+D, opens a panel with ten tabs: the app's log, the current theme's tokens, render counts, tracked components, JS interop calls, memory, component lifecycle, service status, an event log and settings. The panel is dark whatever your theme is. The same ten tabs also open as a full page at `/moka-diagnostics`.

## Install

The package is not part of the `Moka.Red` meta-package. Add it on its own:

```bash
dotnet add package Moka.Red.Diagnostics
```

It depends only on Moka.Red.Core. The setup below keeps it switched off outside debug builds.

## Register

```csharp
// Program.cs
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Diagnostics.Services;

builder.Services.AddMokaRed();

#if DEBUG
builder.Services.AddMokaDiagnostics(options =>
{
    options.Position = OverlayPosition.BottomLeft;
    options.PanelWidth = 480;
});
#endif
```

`AddMokaDiagnostics` registers:

| Service | Lifetime | Holds |
|---------|----------|-------|
| `DiagnosticsOptions` | Singleton | The options below |
| `IMokaDiagnosticsService` | Scoped | Render, JS interop and event data, per circuit (Blazor Server) or per app instance (WebAssembly) |
| `MokaDiagnosticsConsoleBuffer` | Singleton | The last 500 log messages |
| `ILoggerProvider` | Singleton | A logger that copies the app's log messages into the buffer |
| `IMokaJsInteropObserver` | Scoped | Receives the JS interop calls that components built on `MokaComponentBase` and form inputs built on `MokaInputBase` make, and records them for the Network tab. The interface is in `Moka.Red.Core.Base` |

It also registers an internal singleton that lets the full page in a popup find the data of the window that opened it (see [Full Page](#full-page)).

### DiagnosticsOptions

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `KeyboardShortcut` | `string` | `"Ctrl+Shift+D"` | Opens and closes the panel. `Ctrl`, `Shift`, `Alt` or `Meta` joined with `+`, then one key. The modifiers must match exactly. Read once, when the overlay first renders |
| `Position` | `OverlayPosition` | `BottomRight` | Corner for the badge and the panel: `BottomRight`, `BottomLeft`, `TopRight`, `TopLeft` |
| `StartExpanded` | `bool` | `false` | Opens the panel on load instead of showing the badge |
| `PanelWidth` | `int` | `420` | Panel width in pixels. The Settings tab offers 300 to 600 |
| `MinConsoleLogLevel` | `LogLevel` | `Debug` | The lowest level the Console tab keeps, within what the app's logging configuration lets through. A change, from code or the Settings tab, applies from the next message on |
| `MaxEventLogEntries` | `int` | `500` | How many events the event log keeps. The oldest go first. A change, from code or the Settings tab, applies at once |
| `RenderTrackingEnabled` | `bool` | `true` | Records renders and skipped renders from `DiagnosticComponentBase` |
| `JsInteropTrackingEnabled` | `bool` | `true` | Records JS interop calls (see the Network tab below) |

Up to 0.1.12 `MinConsoleLogLevel` and `MaxEventLogEntries` did nothing: the logger kept Debug and above and the log kept 500 events, whatever the options or the Settings tab said.

## Place the Overlay

Put `MokaDiagnosticsOverlay` once in the main layout, inside `MokaThemeProvider`, and give it the provider's theme:

```razor
@* MainLayout.razor *@
@inherits LayoutComponentBase
@using Moka.Red.Diagnostics.Components

<MokaThemeProvider Theme="_theme">
    @Body

    <MokaDiagnosticsOverlay Theme="_theme" />
</MokaThemeProvider>

@code {
    MokaTheme _theme = MokaTheme.Dark;
}
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Theme` | `MokaTheme?` | -- | Theme shown in the Theme tab |

- Without `AddMokaDiagnostics()` the overlay renders nothing, so the same layout works in release builds. Up to 0.1.12 it threw instead, because Blazor treats every `[Inject]` service as required, nullable or not, and the layout had to check for the service first.
- The Theme tab shows the theme passed in `Theme`. It doesn't read the cascaded theme, and without `Theme` the tab stays empty.
- The overlay handles clicks and keys, so the layout needs an interactive render mode.

### Using the Panel

- Drag the panel by its header. It stays inside the window.
- The header buttons pause and resume tracking, export the data, open the full page in a new window, and close the panel.
- Every button in the overlay, its tabs and the full page is `type="button"`, so an overlay placed inside a form doesn't submit it. Up to 0.1.12 they had no type, which makes a button submit the form around it.
- Export downloads `moka-diagnostics-yyyyMMdd-HHmmss.json` with the render entries, the JS interop entries, the active and disposed component counts, and every event the log keeps.

## Tabs

| Tab | Shows |
|-----|-------|
| Console | The app's `ILogger` messages: time, level, short category and message, with exception details behind a toggle. Filter by level and by category. Lists the latest 200 of the 500 kept |
| Theme | Every token of the theme passed to the overlay, grouped as Palette, Typography, Spacing and Border Radius, with a color swatch or a size bar. Filter by name. A click on a row, or Enter or Space on it, copies `var(--moka-...)` |
| Render | One row per tracked component instance: renders, skipped renders, time since the last render and how long it took. Sortable by any column. Rows past 10 renders are highlighted. Needs `DiagnosticComponentBase` |
| Tree | Tracked components grouped by type, each instance with its render count, marked by count (up to 5, 6 to 20, over 20), disposed ones flagged |
| Network | JS interop calls by function: call count, total, average and longest time. Sortable by any column. Calls over 50ms are highlighted |
| Memory | The bytes allocated now (`GC.GetTotalMemory`, garbage not yet collected included) and the heap size the last collection left (`GCMemoryInfo.HeapSizeBytes`), GC collections per generation, GC latency mode, server GC and the finalization queue, refreshed every 2 seconds. Force GC runs a full collection |
| Perf | Active and disposed component counts, with a trend that turns to "Growing" and then "Growing (possible leak)" while the active count keeps rising, and the JS interop calls |
| Services | Managed memory, GC counts, component counts with the average render time, and whether the diagnostics, toast and dialog services are registered |
| Log | Diagnostics events (renders, skipped renders, disposals and JS interop calls), newest first, filterable by type. Lists every event the log keeps (`MaxEventLogEntries`) |
| Settings | Position, panel width, auto-expand, console level, event log size and the two tracking switches, each labelled by its name. The shortcut is shown but can't be changed here |

- Components built on `MokaComponentBase` report the JS calls they make through its helpers: `GetJsModuleAsync` (as `import`), `SafeJsInvokeAsync`, `SafeModuleInvokeAsync` and their void forms, each by function name with how long it took. Form inputs built on `MokaInputBase` report theirs the same way, through its `GetJsModuleAsync` and `SafeModuleInvokeVoidAsync`. A call a component makes directly on `IJSRuntime`, or on the module `GetJsModuleAsync` returned, is not seen. `IMokaDiagnosticsService.RecordJsInteropCall(identifier, duration)` records any other call. Up to 0.1.12 nothing called it, so the Network tab stayed empty.
- The Settings tab's text labels its controls, so a screen reader names each one. Up to 0.1.12 the controls had no names.
- The Render and Network lists are tables to a screen reader. Each column header is a button: Tab reaches it, Enter or Space sorts by that column, and a second press reverses the order. The sorted column's header says which way it is sorted (`aria-sort`). Up to 0.1.12 only a mouse could sort.
- Each Theme row is a button, and the "Copied" message is announced. The Console and Theme filter boxes are named ("Filter by category", "Filter tokens"); up to 0.1.12 only their placeholder said what they were for.
- A token value draws a swatch only when it is a colour, and a size bar only when it cannot add CSS of its own (no `;`, braces, `<`, `>` or `\`). Anything else shows as text without a preview. Up to 0.1.12 the value went into the preview's `style` attribute as it was, so a theme value could add declarations or load a `url()`.
- The Memory tab's two figures read the same value up to 0.1.12.
- Settings changes go to the `DiagnosticsOptions` singleton and last until the app restarts. On Blazor Server they apply to every connected user.
- On Blazor Server the log buffer and Force GC belong to the server process, so they cover every circuit.

## Full Page

`MokaDiagnosticsPage` is a routable page at `/moka-diagnostics` with an empty layout. It shows the overlay's ten tabs at full width, and the overlay's new-window button opens it in a popup. Up to 0.1.12 it had seven: Tree, Network and Memory were missing.

The page lives in the Diagnostics assembly. Two steps are required to reach it:

1. Tell the router about the assembly:

   ```razor
   @* Routes.razor *@
   @using Moka.Red.Diagnostics.Pages

   <Router AppAssembly="typeof(Program).Assembly"
           AdditionalAssemblies="new[] { typeof(MokaDiagnosticsPage).Assembly }">
       <Found Context="routeData">
           <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
       </Found>
   </Router>
   ```

2. In a Blazor Web App, give the server the assembly too:

   ```csharp
   // Program.cs
   using Moka.Red.Diagnostics.Pages;

   app.MapRazorComponents<App>()
       .AddInteractiveServerRenderMode()
       .AddAdditionalAssemblies(typeof(MokaDiagnosticsPage).Assembly);
   ```

   Without it the server has no endpoint for `/moka-diagnostics`. Loading the URL, which is what the popup does, answers 404 with the app's not-found page, and the diagnostics page only appears later if the routes are interactive, once the circuit starts. A standalone WebAssembly app needs step 1 only.

- The page's Theme tab shows the cascaded `MokaTheme` from `AddMokaRed()`, not the theme your layout gives its provider.
- The page sets no render mode. It responds to clicks only when the app's routes are interactive: a global render mode on `<Routes>`, or a standalone WebAssembly app.
- On Blazor Server the popup runs a circuit of its own. The new-window button adds a random key to its URL (`?session=...`), and the page uses it to show the data of the window that opened it, live, in every tab. The key works while that window's overlay exists and only on the server that holds it.
- Opened any other way, and always in a WebAssembly app (where the popup is a separate app instance), the page shows its own window's data. A line under the title says which data it shows. The Console tab reads the shared log buffer either way.
- The popup's URL is resolved against `<base href>`, so an app hosted under a sub-path opens its own page.
- Up to 0.1.12 the popup always showed its own, empty data and opened `/moka-diagnostics` from the site root.

## Render Tracking

`DiagnosticComponentBase`, in `Moka.Red.Diagnostics.Base`, is a base class that reports to the overlay:

- every render, with its duration,
- every time `ShouldRender` returned `false` (a skipped render),
- disposal.

It derives from `MokaComponentBase`, so a component that switches to it needs a `RootClass`, and it takes on that base's render rule: it renders again after a parameter change or a `ForceRender()` call. See [Base Classes](../api/base-classes).

Switch to it in debug builds only, through a base class of your own:

```csharp
// TrackedComponentBase.cs
#if DEBUG
public abstract class TrackedComponentBase : Moka.Red.Diagnostics.Base.DiagnosticComponentBase
{
}
#else
public abstract class TrackedComponentBase : Moka.Red.Core.Base.MokaComponentBase
{
}
#endif
```

```razor
@* OrderSummary.razor *@
@inherits TrackedComponentBase

<div class="@CssClass" style="@CssStyle">
    <span>@Count orders</span>
</div>

@code {
    [Parameter] public int Count { get; set; }

    protected override string RootClass => "order-summary";
}
```

- Without `AddMokaDiagnostics()` the tracking base records nothing and behaves as `MokaComponentBase`. Up to 0.1.12 a missing registration stopped Blazor from creating the component.
- A component that overrides `SetParametersAsync` has to call the base, which looks the diagnostics service up.
- The duration runs from `ShouldRender` to `OnAfterRender`, so it includes applying the render to the page, and on Blazor Server the round trip to the browser.
- A component that overrides `OnAfterRender` has to call the base, or its renders are not recorded. One that overrides `ShouldRender` without calling the base still has its renders counted, but with a 0ms duration and no skipped renders.
- The pause button in the panel header stops the recording of renders, skipped renders, disposals and JS interop calls. The Console tab keeps logging. `RenderTrackingEnabled` stops render recording only.
