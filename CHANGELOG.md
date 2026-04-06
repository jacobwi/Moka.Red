# Changelog

All notable changes to Moka.Red will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

