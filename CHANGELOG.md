# Changelog

All notable changes to Moka.Red will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

