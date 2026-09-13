# Changelog

All notable changes to Moka.Red will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.9] - 2026-09-12

### Added
- **Context menu service.** `IMokaContextMenuService` and `MokaContextMenuHost` show one shared menu, registered by `AddMokaRed()`. `MokaTable.OnRowContextMenu`, `MokaKanbanBoard.OnCardContextMenu` and `MokaSortable.OnItemContextMenu` pass `MokaItemContextMenuArgs<T>`; `MokaListItem`, `MokaTreeItem` and `MokaMenuItem` gained `OnContextMenu`.
- **New components:** `MokaStatusDot`, `MokaSelectionBar` and `MokaDiffViewer` in Primitives; `MokaBootScreen`, `MokaCheatsheet` and `MokaSlashMenu` in Feedback. `moka-boot.css` in Core gives host pages a matching splash before Blazor starts.
- **Runtime theming:** `MokaTheme.WithAccent`, `WithDensity` and `WithFontScale`, with the `--moka-density` and `--moka-font-scale` tokens, the semantic `-dim` fills and `--moka-color-primary-glow-faint`.
- **Sticky pagination:** `MokaPagination.Sticky` and `StickyPosition`, and `MokaTable.StickyPagination`.
- **Forms validation is wired to `EditContext`.** `MokaInputBase<TValue>` exposes `ValidationMessages`, `HasValidationError` and `ValidationErrorText`. Eleven input components combine `ErrorText` with the EditContext result and emit the framework's `modified`/`valid`/`invalid` classes on their root element. DataAnnotations messages inside an `EditForm` were previously invisible across the whole library.
- **MokaBarcode** now implements Code 39, EAN-13, EAN-8 and UPC-A as real encoders, with check-digit computation and validation, per-format quiet zones, and human-readable text that includes the computed check digit.
- **MokaTable**: the filter row, inline cell editing, keyboard cell navigation, row drag-reordering and column resizing are all wired to UI. New `ReloadAsync()`, `MokaTableState.ColumnFilters` and `MokaTableExportContext.IsCompleteSet`.
- **MokaCheckbox** and **MokaSwitch** gained `ErrorText`.
- **MokaDialog** traps focus while open and restores it on close. `PromptAsync` gained a `configure` overload.
- **IMokaToastService.Toasts** exposes the live list so a late-mounting host does not lose toasts.
- **IMokaNotificationService.Push(MokaNotification)** makes `Icon` and `OnClick` reachable.
- **MokaCommand.Href** now navigates.
- **IMokaTabSessionState** gained `RemoveTabAsync(tabId, force)`, `ValueSerializer`, `ValueDeserializer` and `LastRestoreWarnings`.
- **MokaIconDefinition.Filled**, honoured by `MokaIcon`, so solid glyphs are distinct from their outline twins. 12 new icons.
- `prefers-reduced-motion` support across every animation.
- `Moka.Red.Data.Tests` project. Test count went from 286 to 371.
- The four `dotnet new` template options that did nothing (`noHttps`, `allInteractive`, `pwa`, `supportBrowser`) now work.

### Fixed
- **MokaChip** ignored `Color` unless the chip was selected, so the documented color examples rendered as plain gray chips. An unselected chip now takes the color on its border and text; selecting it still adds the tinted fill.
- **QR mask selection** was missing spec penalty rule 3 (the 1:1:3:1:1 finder lookalike, +40 each).
- **MokaMarquee never scrolled.** Its `animation-duration` used `calc(100vw / var(--speed) * 1s)`; CSS `calc` cannot divide a length by a length, so the declaration was dropped.
- **MokaTable** rendered a row-reorder header cell with no matching body cell, so every data row was one cell short. Also: `<Virtualize>` inside `<tbody>` without `SpacerElement="tr"`, an unchecked `ICollection<T>` cast, boxing that made expand rows never open for struct items, skeleton rows that ignored two columns, and `SelectedItems` being aliased rather than copied.
- **Only one dialog could ever be shown.** A second request overwrote the pending `TaskCompletionSource` and the first `await` never completed. Requests now queue. `Close(false)` resolved to `null` instead of `false`.
- **MokaScrollToTop** and **MokaCommandPalette** leaked JS listeners across component instances; either one's `dispose()` tore down the other's.
- **`lockBodyScroll`** had no reference counting, so the first of a stack of dialogs to close unlocked scrolling for all.
- **MokaSplitPane** and **MokaInfiniteScroll** built JS source by interpolating a caller-supplied `Id` into `eval`, with fallbacks that could measure an unrelated element.
- **MokaResizable**'s corner handle was rendered but never wired, and `MokaResizeResult` always reported `0` for one axis.
- **MokaTransferList** and **MokaCalendar** mutated internal state without a render, so checkbox toggles, search input and month navigation did nothing.
- **MokaCallout**'s five per-type icon colours and **MokaStat**'s three size variants used duplicate bare selectors, so only the last rule in each applied.
- **`.moka-dark`** did not override seven tokens, giving white-on-light text in a CSS-only dark toggle.
- **`.moka-thin-scrollbar`** thumb faded out on hover instead of in.
- **`ToCssClass`** hyphenated before every capital, so `EAN13` became `e-a-n13` and `UPC` became `u-p-c`.
- **`MokaResponsiveStyleBuilder`** ordered breakpoints by string, so `1024px` sorted before `768px` and the wider breakpoint lost the cascade.
- **`MokaIconDefinition.GetHashCode()`** threw on `default`. `Star`/`StarOutline` and `Heart`/`HeartOutline` had identical path data and rendered identically.
- **`downloadCsv`** ran `atob` over a UTF-8 base64 string, corrupting every non-ASCII CSV export.
- **Context menu**: the focus highlight landed on the wrong row when a divider was present, keyboard navigation did not work until the menu was clicked, submenus used a hardcoded `X + 200` offset with no viewport clamping, and nested backdrops swallowed clicks.
- **Tabs**: `TogglePin`'s two branches were identical, the three bulk-close methods were never called, `TabAdded` was never invoked, and `RestoreStateAsync` silently dropped most of `TabInfo`.
- **`SafeJsInvokeAsync`** now catches `ObjectDisposedException` and `OperationCanceledException`, both common during circuit teardown.
- Culture-dependent CSS lengths in `MokaSplitPane`, `MokaResizable`, `MokaDockPanel`, `MokaGridBackground` and `MokaConfetti`.
- Every raw inline `<svg>` outside the seven data-driven or animated exceptions converted to `MokaIcon`, and 17 hardcoded colours replaced with tokens (11 of them in `MokaMediaGallery`, whose lightbox rendered identically in light and dark). Doing so exposed 11 `.razor.css` rules that had been dead since an earlier icon migration, because Blazor CSS isolation scopes a selector to the last compound and a `MokaIcon`-rendered svg carries MokaIcon's scope. `MokaRating` now delegates to `MokaIcon` instead of hand-rendering its `Icon`/`FilledIcon` paths.

### Changed
- `MokaTextArea` moved from `MokaVisualInputBase<string>` to `MokaTextInputBase<string>`.
- `MokaTextField` routes `@oninput` through the base's debounce, so `DebounceDelay` works.
- `MokaSelect` no longer mutates the caller's `SelectedValues` list in place.
- `MokaCommandPaletteService` is thread-safe and dictionary-backed; `IsOpen`'s setter raises `OnToggle`.
- `MokaFieldWrapper.Size` is applied instead of ignored.
- `MokaNumericField` and `MokaPasswordField` stopped borrowing `MokaTextField`'s class names, which were scoped behind `::deep` and never matched.
- Dependencies: xunit.v3 4.0.0, xunit.runner.visualstudio 4.0.0, Microsoft.NET.Test.Sdk 18.10.0, bunit 2.10.3, ASP.NET Core Components 9.0.20 / 10.0.12. Tests now run on Microsoft.Testing.Platform via `global.json`.

### Removed
- **`MokaSortable.Group`** and the `window._mokaSortableGroups` registry. Cross-list drag was registered but never implemented.
- **`MokaPopupBase`** and its `PopupPosition` enum, **`MokaDataComponentBase<TItem>`**, **`MokaContainerBase`**. All three were public API with zero inheritors.
- `moka-dialog.js` `dispose()`; `registerShortcut`/`dispose` in `moka-command-palette.js` now take a handle.
- Seven empty `.gitkeep` placeholder folders.
- Bootstrap from the WasmApp sample.

### Breaking
- The three deleted base classes are public API in 0.1.8.
- `MokaSortable.Group` removed.
- `MokaNotification.Read` is `init`-only.
- `TabGroupInfo.BorderPosition` is now `BorderPosition?` so "unset" is distinguishable from an explicit `Left`.
- `IMokaTabSessionState<TValue>`, `IMokaToastService`, `IMokaNotificationService` and `IMokaDialogService` gained members. External implementers break; callers do not.
- `MokaContextMenuTrigger` routes through `IMokaContextMenuService` when a `MokaContextMenuHost` is mounted, so only one menu is open at a time.
- `MokaTabContainer.TabRemoved` now fires for every removal path, not just the close button.
- JS module signatures: `trapFocus`/`releaseFocus`, `registerShortcut`/`dispose`, `initAllColumnResize`, `constrainContextMenu`.
- CSS variable `--moka-marquee-speed` replaced by `--moka-marquee-duration`.

## [0.1.8] - 2026-04-11

### Fixed
- **MokaKanbanBoard** — `ItemTemplate` is now optional (nullable). When omitted, items render as their `ToString()` value. Previously crashed with NullReferenceException.
- **MokaKanbanBoard** — `ColumnWidth` is now nullable; avoids invalid `width: ; min-width: ;` inline styles when unset.
- **Docs preview host** — registered `AddMokaRed()` services in `Program.cs` to fix `IMokaToastService` injection error.
- **Docs preview host** — removed 404 references to non-existent `moka-reset.css` and `moka-tokens.css` (merged into `moka.css`).
- **Docs** — fixed 19 compilation errors in preview code blocks: record constructor syntax, wrong type names, wrong enum types, wrong icon category paths.

## [0.1.7] - 2026-04-10

### Added
- **Matrix dark theme** — complete overhaul of the dark palette: `#060608` background, `#0c0c10` surface, red-tinted borders (`rgba(239,83,80,0.06–0.24)`), glow rings instead of drop shadows, Inter + JetBrains Mono font stacks, 4/8/10/12px radius scale, 120/150/200ms transitions.
- **New palette tokens** — `SurfaceHover`, `Surface2`, `Surface3`, `PrimaryGlow`, `PrimaryGlowMd`, `PrimaryGlowStrong`, `PrimaryBorder`, `PrimaryBorderDim`, `OnSurfaceTertiary`, `OnSurfaceQuaternary`.
- **New CSS tokens** — `--moka-focus-ring`, `--moka-selected-glow`, `--moka-color-surface-hover`, `--moka-color-surface-2`, `--moka-color-surface-3`, `--moka-color-primary-glow*`, `--moka-color-primary-border*`, `--moka-color-on-surface-tertiary`, `--moka-color-on-surface-quaternary`.
- **MokaGridBackground** — decorative container with 6 grid patterns (Lines, Dots, Dashed, Cross, DiagonalLines, Honeycomb), fade edges, center glow highlight, customizable cell size/color/opacity.
- **MokaRetroGrid** — perspective vanishing-point grid with animated scroll, glowing horizon line, configurable angle/perspective/cell size.
- **MokaMeteors** — animated shooting star streaks with bright head dots, fading tails, thickness variation, configurable count/angle/color.
- **MokaOrbitingIcons** — icons revolving around center content with counter-rotation, orbit path ring, pause/reverse support.
- **MokaGlassCard** — glassmorphism card with backdrop-filter blur, translucent tint, optional glow border, keyboard accessible.
- **MokaBentoGrid** + **MokaBentoItem** — asymmetric bento-box grid layout with column/row spanning, clickable items, keyboard accessible.
- **MokaGridPattern** enum — Lines, Dots, Dashed, Cross, DiagonalLines, Honeycomb.
- Documentation pages for all 6 new components.

### Changed
- **Button CSS** — uppercase labels with `letter-spacing: 0.08em`, transparent backgrounds with colored border accents, glow hover/active states. All variants (filled, outlined, text, soft) updated.
- **Card CSS** — red-tinted borders, glow on hover instead of translateY, uppercase micro-label titles.
- **TextField CSS** — red-glow focus ring (`--moka-focus-ring`), monospace placeholders.
- **Dialog CSS** — backdrop blur, red-tinted border, uppercase header titles.
- **Toast CSS** — severity-colored glow shadows per toast type.
- **List CSS** — inset glow on selected items, surface-hover on hover.
- **Chip CSS** — uppercase labels, square radii (not pills), glow focus ring.
- **Accordion, Popover, Sidebar, Alert** — updated to matrix aesthetic.
- **Scrollbar styling** — uses red-tinted `--moka-color-primary-border` thumb color.

### Fixed
- **OrbitingIcons** — removed unnecessary `::deep` CSS selectors; uses scoped `__icon-wrap` div instead.
- **GlassCard/BentoItem** — added `role="button"`, `tabindex="0"`, and Enter/Space keyboard handlers for accessibility.
- **Hardcoded `10px` font sizes** — replaced with `var(--moka-font-size-xs)` in Card, GlassCard, BentoItem.
- **Meteors CSS** — replaced hardcoded `#ef5350` fallbacks with `var(--moka-color-primary)`.
- **Docs: progress.md** — fixed `Rounded` → `RoundedEnds` parameter name.
- **Docs: forms.md** — fixed `<MokaRadio>` → `<MokaRadioItem>` component name.
- **Docs: theme-switcher.md** — fixed outdated `#121212` → `#060608` dark palette color.

## [0.1.6] - 2026-04-08

### 🐛 Fixed
- **Actually** strips the implicit `Microsoft.AspNetCore.App` `FrameworkReference` from
  packed library nuspecs via a build-time `<Target BeforeTargets="ProcessFrameworkReferences">`
  in `Directory.Build.targets`. The 0.1.5 release intended this fix but only added the
  `Microsoft.AspNetCore.Components.Web` `PackageReference` substitution — the underlying
  `FrameworkReference` was still being implicitly added by the SDK and propagated into
  the published nuspecs, so consumers PackageReferencing Moka.Red from a Blazor WebAssembly
  app still hit `NETSDK1082` ("no runtime pack for browser-wasm"). Verified by inspecting
  `Moka.Red.0.1.6.nupkg`'s nuspec — no `<frameworkReferences>` group present.

### 🔧 Changed
- `docs/mokadocs.yaml` — drops the explicit `previewHost`, `references`, and
  `stylesheets` plugin options in favor of the new `library: Moka.Red@0.1.6` shape.
  The mokadocs-blazor-preview plugin (v3.x) auto-discovers / scaffolds / publishes
  the docs preview-host project transparently. Removes the cross-repo path that was
  pointing at the sibling `Moka.BlazorRepl` repo.
- Deletes `nuget.config` — restores default behavior of using only the global
  `nuget.org` source.

## [0.1.5] - 2026-04-05

### 🐛 Fixed
- Replaced `FrameworkReference` to `Microsoft.AspNetCore.App` with `PackageReference` to `Microsoft.AspNetCore.Components.Web` across all 12 library projects — fixes NETSDK1082 errors when consumers reference Moka.Red from Blazor WebAssembly projects

### 🔧 Changed
- Removed WASM workaround from `Directory.Build.targets` and WasmApp sample — no longer needed

## [0.1.4] - 2026-04-05

### ✨ New Components (55+)
- **Motion**: MokaFadeIn, MokaSlideIn, MokaScaleIn, MokaStagger, MokaTypewriter — CSS animation wrappers
- **Forms**: MokaCalendar, MokaColorWheel, MokaColorInput, MokaDateRangePicker, MokaInputGroup, MokaKnob, MokaIpAddressInput (IPv4+IPv6), MokaMacAddressInput, MokaPinInput, MokaPasswordStrength, MokaSchedulePicker, MokaToggleGroup, MokaTreeSelect, MokaFormBuilder (visual form designer with JSON/Razor export)
- **Primitives**: MokaTag, MokaSteps, MokaCodeBlock, MokaConfetti, MokaDataList, MokaFloatingActionButton, MokaGauge, MokaInfiniteCarousel, MokaKanbanBoard, MokaKeyValue, MokaLogViewer, MokaMarquee, MokaMeter, MokaNotice, MokaNumberTicker, MokaOrganizationChart, MokaParallax, MokaReveal, MokaSplitButton, MokaSwipeActions, MokaTerminal, MokaThemeSwitcher, MokaTransferList
- **Feedback**: MokaCookieConsent, MokaDrawer, MokaHoverCard, MokaOnboarding, MokaWizard, MokaNotificationBell
- **Layout**: MokaSplitPane
- **Navigation**: MokaCommandBar
- **Data**: MokaInfiniteScroll
- **Diagnostics**: NetworkPanel, ComponentTreePanel, MemoryPanel (3 new overlay panels, 10 total)

### 🏗️ Architecture
- `MokaSegmentedInputBase` — shared base class for OTP, PIN, IPv4, IPv6, MAC inputs
- `MokaVisualComponentBase` properties (Size, Variant, Color, Disabled, SizeValue) now `virtual` — subclasses use `override` for different defaults
- CssBuilder/StyleBuilder use inline `string[8]` array instead of `List<string>` — zero container allocation
- `MokaEnumHelpers.ToCssClass<TEnum>` cached via `ConcurrentDictionary` — zero allocation after warmup
- `GetJsModuleAsync` thread-safe via `SemaphoreSlim` double-checked locking
- `_parametersChanged` marked `volatile` for thread safety
- Shadow strings extracted as `const` fields — compile-time interned

### 🎨 Theming
- `ToCssVariables()` now generates ALL 90+ tokens (was 66) — added transitions, heights, z-index, semantic state tokens
- Theme CSS injected at `:root` via `<style>` tag — external libraries can now consume `--moka-*` tokens
- `.moka-dark` CSS overrides in `moka.css` for pure-CSS dark mode targeting
- Fluent `With*` API: `MokaTheme.Light.WithPrimary("#1976d2")`, `.WithFontFamily()`, `.WithDark()`
- `AutoDetectColorScheme` parameter on `MokaThemeProvider` — auto-detects OS `prefers-color-scheme`
- ThemeGen live preview fixed — now uses inline style instead of nested `MokaThemeProvider`

### 🧹 CSS Audit
- 9 shadow tokens (`--moka-shadow-0` through `--moka-shadow-4`, popup, popup-lg, modal, subtle)
- 211+ redundant `var(--moka-token, #hex)` inline fallbacks removed across 39 files
- Shimmer `@keyframes` deduplicated to single definition in `moka.css`
- Inline SVGs in MokaNotificationCenter replaced with `MokaIcon`
- CSS chevron hacks in Panel/Card/DockPanel replaced with `MokaIcon`
- Hardcoded font-size, gap, padding, border-radius, transition values replaced with tokens across 50+ files
- Hardcoded disabled opacity replaced with `var(--moka-opacity-disabled)` in 7 files
- Shared `.moka-thin-scrollbar` and `.moka-hide-scrollbar` utility classes

### 📦 Consumer Onboarding
- Single `AddMokaRed()` call registers all services (theme + feedback)
- `MokaThemeProvider` auto-injects `moka.css` via `<HeadContent>` — zero manual CSS links
- Merged `moka-reset.css` + `moka-tokens.css` + `moka-text.css` into single `moka.css`
- Blazor WebAssembly sample app proving WASM compatibility

### 🐛 Fixed
- `MokaSegmentedControl` both tabs showing active — removed `IsFixed="true"` from `CascadingValue`
- `MokaTag` runtime `InvalidOperationException` — removed `new` on `[Parameter]`, use `override`
- `MokaThemeProvider` `IsFixed="true"` prevented theme changes from cascading
- `MokaTooltip` missing `CssClass` override — `Class` parameter wasn't applied
- `MokaDialog`, `MokaAccordionItem`, `MokaRadioItem` using private CSS class instead of `CssClass` override
- `MokaProgress` changed to `MokaVisualComponentBase`, renamed `Rounded` to `RoundedEnds`
- Missing `ShouldRender()` on MokaKanbanBoard, MokaAlert, MokaTreeSelect
- FAB demo floating over viewport — use `position:absolute` in container
- 25 `NoWarn` suppressions removed by fixing actual code issues

### 📖 Documentation
- 55+ new component doc pages (80+ total)
- Consumer `_Imports.razor` convenience guide
- `mokadocs.yaml` navigation updated for all new components
- DevApp sidebar: All/Categories segmented toggle with 130+ component links with anchor scrolling

### 🧪 Testing
- 222 tests across 7 projects (was 124 across 2)
- 16 benchmarks across 3 classes
- Blazor WASM sample app

