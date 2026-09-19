# Changelog

All notable changes to Moka.Red will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.12] - 2026-09-18

### Added
- **Dialogs stack.** Each `IMokaDialogService` call opens its dialog straight away, on top of any open one, and returns its own result, so a dialog's action can await another dialog. New members: `OpenDialogs`, `Close(request, result)`, `CloseWithResult(request, result)` and `CloseAll()`, which cancels every open dialog. Escape and the backdrop close only the top dialog.
- **MokaTabStrip**: `OnTabContextMenu` (`MokaItemContextMenuArgs<TabInfo<TValue>>`) for an app's own tab menu, which keeps the built-in one closed; `CloseOnMiddleClick` (default on); keyboard support following the tabs pattern (arrow keys, Home, End, Enter or Space to activate, Delete to close). `MokaTabContainer` passes both parameters through.
- **MokaCommandPalette.Shortcut**, default `"Mod+K"` (Cmd on macOS, Ctrl elsewhere). Modifiers must match exactly, and null or empty turns it off. A shortcut that types a character, such as `/`, is ignored while the user types in a field, and an unknown modifier turns the shortcut off with a console warning.
- **MokaCheckbox.DisplayOnly** draws the box and label with no input, for a row or menu item that owns the checked state.
- **MokaThemeProvider.Nonce** for a strict content security policy: every style element the provider writes carries it, and the provider's tokens move out of the inline `style` attribute.
- **MokaDockPanel.Scrollable** and **MokaDockContent.Scrollable**. With `false` the region clips instead of scrolling and a single child fills it, for content such as a terminal or an editor that scrolls itself.
- **MokaTree** keyboard and screen reader support: one item in the tab order, the arrow keys, Home and End to move, Right and Left to expand, collapse and step between parent and child, Enter or Space to act, the context-menu key for an item's `OnContextMenu`, and `aria-expanded`, `aria-selected` and `aria-multiselectable`.
- `SafeModuleInvokeAsync<T>` on `MokaComponentBase`, the value-returning twin of `SafeModuleInvokeVoidAsync`.

### Changed
- **MokaThemeProvider renders `moka.css` and the `:root` tokens itself** instead of through `<HeadContent>`. The head shows one `HeadContent` at a time, so any other one on the page, `MokaTabContainer`'s included, removed them, and a host without a `HeadOutlet` (MAUI Blazor Hybrid) never had them. A provider nested in another one now themes only its subtree and leaves the page-wide styles to the outermost.
- **Tab styles ship in the scoped CSS bundle.** A `MokaTabStrip` on its own, in MAUI or without prerendering is styled from the first paint. The strip, its context menu and the container share one set of rules driven by the `--moka-tab-*` custom properties, and the strip applies `TabTheme` itself.
- The tab strip's pin and close buttons are out of the tab order, and the close button now shows on the active tab as well as on hover.
- **Context menus close before the chosen item's action runs**, in `MokaContextMenu` and in the tab strip's built-in menu, so an action that awaits a dialog no longer leaves the menu open over it.
- **MokaCommandBar** adapts to narrow widths: the start zone shrinks and clips, the search box keeps at least 8rem, and below 640px the search moves to its own row.
- `MokaTable` with `ShowPagination="false"` shows every row. `ServerData` is then asked for page 1 with a page size that covers the whole result set.
- Items under a disabled `MokaTreeItem` are disabled too. They already ignored the mouse; they now also report `aria-disabled` and ignore keys.

### Removed
- `_content/Moka.Red.Navigation/moka-tabs.css`. Its styles are in the scoped bundle; remove any `<link>` to it.

### Fixed
- **MokaRadioGroup**: picking an option did not redraw the items (the group cascaded itself with `IsFixed`), the radios could not be reached or changed with the keyboard, the checked state was not exposed properly, the hidden inputs shared no `name`, and a click on a label selected twice. Items are now native radio inputs inside their labels.
- **MokaDialog**: the focus trap ignored `autofocus` and `data-autofocus` and moved focus that content had already placed inside the dialog. A dialog created open could take its scroll lock twice. After several dialogs closed at once, focus fell to the page instead of going back to where it started. A service prompt now starts in its text field, which has an accessible name.
- **MokaTable**: `ShowPagination="false"` hid the pager but still showed one page. Row keys had no effect since 0.1.9, so a sorted or filtered row was rebuilt rather than moved and lost its state. Rows that share an `ItemKey` now render unkeyed instead of making Blazor throw.
- **MokaSortable** threw on its next render when two items shared a key, which includes equal items without an `ItemKey` (a list of strings with a repeat).
- **MokaTagInput** edited the list passed to `Values` in place. Changes now arrive as a new list through `ValuesChanged`.
- **MokaSelect** could not show a `null` item as the selection; it showed the placeholder instead.
- **MokaCommandPalette** opened on any Ctrl or Cmd+K, with Shift or Alt too, and always cancelled the key's default action. With `ShowGroups` on, Enter ran whichever command sat at the highlighted position in registration order rather than the highlighted one.
- **Dock layout**: a change to a panel's parameters reached the grid one render late; collapsing a panel unmounted its content and lost its state; `makeResizable` split grid templates on whitespace, which broke tracks such as `minmax()`, `calc()`, `repeat()` and named lines.
- **MokaTabStrip**: used without `MokaTabContainer` it was partly unstyled and its context menu not styled at all. `TabTheme` colours and a tab's `ActiveColor` were mostly overridden by the strip's own stylesheet. `aria-selected` rendered with an empty value, and `draggable` did too, which browsers read as "auto", so drag reorder never started. Plugin context-menu items (`IMokaTabPlugin.GetContextMenuItems`) were never shown. The built-in menu had no menu roles and could not be used or closed from the keyboard.
- **MokaThemeProvider**: `AutoDetectColorScheme` ran through `eval`, which a policy without `'unsafe-eval'` blocks, never noticed the OS setting change, and wrote the detected theme into `Theme`, where the next parent render undid it.
- `MokaOnboarding` and `MokaTerminal` no longer use `eval`. Onboarding built its script around the step's selector, which a quote or backslash could break out of.
- **MokaBreadcrumb** separators were near invisible on dark surfaces.
- **MokaCommandBar**: on a narrow bar the search box spread over the breadcrumb.
- Eleven components used `--moka-line-height-normal`, which is not a token, so their line height fell back to the inherited one. `MokaSidebar Elevated` and the table's filter popup used a shadow token that does not exist, and the popover's click-outside backdrop had no z-index.

## [0.1.11] - 2026-09-18

### Added
- `moka-keys.js` in Core, the shared answer to two things Blazor cannot do in .NET: cancel the browser's default action for a single key, and tell a key pressed on an element from one that bubbled out of a control inside it. `bindActivation` makes Enter and Space click a focused element the way they click a button; `preventKeys` cancels the default action of chosen keys and still lets the component's handlers see them. `moka-drag.js` re-exports both.
- `SafeModuleInvokeVoidAsync` on `MokaComponentBase` and `MokaInputBase`: calls a function on the component's JS module and swallows the exceptions a lost circuit or prerendering throws.

### Fixed
- **MokaDialog** was not named by its title for screen readers. The title now labels the dialog through `aria-labelledby`. Attributes passed to `MokaDialog`, such as an `aria-label` for a dialog without a title, now land on the `role="dialog"` element instead of being dropped, and so does `Id`.
- **MokaDialog took focus late on its first open.** It imported its JS module only once it opened, and set up scroll lock and dragging before the focus trap, so keys went to the page behind it for a moment. The module now loads while the dialog is still closed, and the trap goes first.
- **MokaSelect had no accessible name.** `<label for>` cannot name its `role="combobox"` div, so the trigger now points `aria-labelledby` at the label, falling back to an `aria-label` you pass and then to the placeholder. The option list is named the same way and linked with `aria-controls`. `aria-expanded` now always reads `true` or `false`; Blazor dropped the attribute entirely while the list was closed.
- **MokaSelect keyboard use.** Space and the arrow keys also scrolled the page while the trigger had focus, and Enter in the search box submitted a surrounding form. A searchable select never moved focus into its search box, and Tab from the trigger closed the list, so the search could not be reached from the keyboard; focus now moves in when the list opens and returns to the field when Enter or Escape closes it. With `GroupBy`, the arrow keys followed the order of `Items` rather than the order on screen. Enter or Space on the clear or chip-remove button also opened the list.
- **MokaSelect screen reader support.** The highlighted option was never announced (no `aria-activedescendant`), selected options rendered `aria-selected` with an empty value, the search box sat inside the listbox, and groups had no names. A disabled select stayed in the tab order, and its clear and chip-remove buttons still worked.
- **MokaAutoComplete**: Enter to pick a suggestion also submitted a surrounding form. `aria-expanded` rendered as a bare attribute, the list had no id or label, and the highlighted suggestion was not announced. With nothing highlighted, Enter still submits the form.
- **MokaChat stopped accepting typing after the first character.** Its input cancelled every keydown once it held any text, where only Enter was meant to be cancelled. Now plain Enter sends, Shift+Enter adds a line, and Enter that confirms an IME composition no longer sends.
- **MokaListItem could not be reached with the keyboard.** Rows with `OnClick` or `OnContextMenu` are now tab stops with a focus ring and a `moka-list-item--interactive` class, and `MokaList` activates them on Enter and Space. Keys pressed in a button or input inside a row do not activate it, and Space does not scroll the page. Link rows put `role="listitem"` on the `<a>`, which hid the link role; it now sits on a wrapper.
- **MokaBentoItem** and **MokaGlassCard**: a clickable card's key handler also fired for Enter or Space pressed in a button or input inside the card, and Space scrolled the page. Static cards carried `tabindex="-1"`, so a click inside one focused the card. Clickable cards now show the focus ring.
- **MokaContextMenu**: the arrow keys, Home, End and Space also scrolled the page behind the menu, and screen readers were not told which item was highlighted.
- **MokaToastHost** and **MokaDialogHost** handled service events in `async void` methods and changed their state on the calling thread. A toast or dialog raised from a background thread could throw mid-render and take the app down. Both now apply each change on the renderer's thread and pass any failure to Blazor's error handling.

## [0.1.10] - 2026-09-13

### Changed
- **MokaChip** uses the same variants as `MokaButton`. `Variant` defaults to `Soft`, a tinted background; `Filled` is a tinted border that fills on hover, `Outlined` a full-color border, `Text` neither. A selected chip gets a colored fill, border and inner glow on any variant, and an uncolored chip takes the primary color when selected. Chips used to ignore `Variant` and render as a plain outline, so existing chips change appearance.
- MokaChip sizes and margins use spacing tokens, and its disabled state uses `--moka-opacity-disabled`.

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

