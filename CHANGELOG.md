# Changelog

All notable changes to Moka.Red will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.14] - 2026-11-08

### Added
- `MokaRadioGroup.Cards` draws the options as picture cards, for a choice people judge by looking. `MinColumn` sets the
  narrowest a card gets before they wrap, and `MokaRadioItem.Preview` and `Hint` fill each card. It's the same native
  radio group, so the keyboard, `EditForm` and form posts work as before.
- `MokaIcons.Content.Scale`, a balance scale.
- `MokaGrid.MinColumn` takes a `var()` reference as well as a length, with or without a fallback, for a size that lives in
  a token. `CssValues.IsLengthOrVariable` is the check.

## [0.3.13] - 2026-11-08

### Added
- `MokaDiffViewer.OldStartLine` and `NewStartLine` number each side from a given line, for a viewer showing one hunk.
- `MokaTreeItem.EndContent` holds tags or controls at the end of the row. `IconColor` colors its icon, and `Draggable`,
  `OnDragStart`, `OnDragEnd` and `OnDrop` let rows be dragged and take drops, each row for itself.
- `MokaListItem.IconColor`.
- `MokaColorSwatch.AllowClear`: a click on the chosen swatch clears the selection.

### Changed
- `MokaPageHeader` stacks by its own width, below 30rem, rather than below a 720 px window. A header in a narrow panel
  now adapts, and one in a small desktop window keeps its row. `TitleInTopBar` still follows the window, as the app
  shell's top bar does.

### Fixed
- A `MokaTable` with `Bleed` in an `Inner` card overhung the card and lost the bottom of its last row, since it reached
  by the padding of a full card. It now takes the padding of the card it's in.
- A `MokaMeter` with `Segments` painted its filled part in the surface color. The filled part now shows the zones at
  full strength, and the rest of the track shows them faintly.
- A `MokaSelectionBar` on a phone ran off both sides of the screen, cutting off its count and squeezing its buttons. It
  now wraps and stays on screen.
- A wrapped `MokaCodeBlock` split ordinary words at the end of a line. It now breaks at spaces first, and inside only
  what can't fit.

## [0.3.12] - 2026-11-08

### Added
- `MokaSettingsLayout.Landing` shows a home page while no section is active. On a narrow layout it takes the place of
  the section list, so it's the first thing a phone sees.
- The class `moka-settings-full` makes any content take a whole row of a `MokaSettingsForm` drawn as cards, as
  `MokaSettingsGroup.Span="Full"` does for a group.

### Fixed
- A nested `MokaThemeProvider` took the window's height, so a theme preview in a small card overflowed it. Only the
  outermost provider fills the window now.
- `MokaLineChart` and `MokaBarChart` chose the x labels to keep by their index alone, and blank labels counted, so
  marks an app placed on purpose could need an enormously wide axis or be dropped. Blank labels now take no place on the
  axis, and each label shows once it fits beside the labels that outrank it, judged by the real length of each. Axes
  with every label filled in thin the same way, and one long label now costs only its neighbours.
- A tooltip on a point or bar whose label is blank no longer starts with a separator.

## [0.3.11] - 2026-11-08

### Added
- `MokaCard.Fill` fills its container, such as a grid cell, and scrolls its body inside it.
- `MokaStat.Visual` puts a sparkline or meter beside the value, and `MokaStat.Mono` draws the value in mono.
- `MokaLineChart` thins its x labels to the chart's width, always keeping the last, and `TooltipLabel` sets a tooltip's
  start apart from the axis label.
- `MokaBarChart.Height`, and the bar chart now fills its width as the line chart does.
- `MokaDonutChart.Formatter` for its tooltips and legend, and `Total`, which draws each segment as a share of a quota
  with the rest as track.
- `MokaJobProgress.ByteUnits` (`MokaByteUnits.Binary` counts in 1024 with KiB and MiB) and `ByteFormatter`.
- `MokaHeatmap.Unknown` marks days with no data apart from zero, with `UnknownText`.
- `MokaHeading.Micro`, a small uppercase heading for a group title that keeps its heading element.
- `MokaIdenticon.CurrentColor` draws the cells in the text color, so CSS sets them.
- `MokaSegment.Count` shows a number after the text, as in "On 3".
- `MokaTag.Mono`.
- `MokaLink.OnClick`: without `Href` it renders a button that reads as the link, for an inline text action.
- `MokaDrawer.AriaLabel` and `CloseLabel`.
- `MokaTable.TitleContent`, markup in the toolbar in place of the `Title` string.

### Changed
- A clickable `MokaCard`, or one with `Href`, is a plain element with a real button or link as its main action, which
  is the title or a hidden action named by the new `ActionLabel`. A click anywhere on the card still runs it, while
  switches, links and buttons inside the card work on their own. A button or link no longer wraps them, which was
  invalid. Styles or tests that selected `a.moka-card` or `.moka-card[role=button]` need `.moka-card-action`, and
  `OnClick` is no longer raised by clicks on controls inside the card.
- `MokaBarChart` is drawn as a stretched plot with HTML tick labels, so `.moka-barchart-svg` and its svg text ticks are
  gone.
- `MokaSparkline` draws its last point as a round dot that stays round when the chart is stretched, and its strokes no
  longer scale, so `StrokeWidth` is in screen pixels.
- On the new design a `MokaMeter`'s track is a visible grey, so it shows inside a stat tile.
- `MokaDrawer`'s animations follow the transition tokens, its close button is named "Close" and drawn in a muted color
  rather than at 60% opacity, and a drawer with a `Title` is named by it.

### Fixed
- `MokaHeading.Truncate` did nothing, because no stylesheet defined its class. It now cuts the heading off with an
  ellipsis.

## [0.3.10] - 2026-11-08

### Added
- `MokaSettingsSection.Icon` puts an icon before a row's label in `MokaSettingsLayout`'s list (the phone's list page
  too) and beside the section's name in search results. It's muted, takes the accent on the current row and the accent's
  ink on a solid one. A row without one leaves no gap.

## [0.3.9] - 2026-11-08

### Fixed
- `MokaDropOverlay` blurred the whole window behind it while files were dragged over, which without GPU compositing
  (some WebView2 setups, virtual machines, remote desktops) halved the frame rate whenever anything beneath redrew. The
  layer is now a stronger solid backdrop with no blur.

## [0.3.8] - 2026-11-08

### Fixed
- `MokaTabStrip` used without a `MokaTabContainer` drew empty close, pin and group buttons, because only the container
  passed it an icon provider. Without `IconProvider` the strip now uses the `MokaTabIconProvider` that `AddMokaTabs`
  registers, and the built-in icons when there is none.

### Added
- `MokaColorSwatch.ColorLabel` names a color, such as "Blue" for `#42a5f5`, for the swatch's accessible name, its
  tooltip and the text beside the chosen color. A color with no name is named by its value, as before. Each swatch
  reports whether it is the chosen one through `aria-pressed`.
- `MokaTheme.WithCurrentPlace(MokaCurrentPlace)`, a style choice for where you are in the app: the current page in
  `MokaAppShell`'s rail and side menu, the account card when one of its pages shows, and the current section of a
  `MokaSettingsLayout` list. `Ring`, the default, draws it as a selected row, as before. `Solid` fills it with the accent
  and a soft glow, in the accent's dark ink on the dark scheme. Rows picked in tables and lists keep `WithSelection`.
- `MokaAppShell.RailLabels` shows each screen's name under its icon on an 80 px rail. A name that doesn't fit is cut
  with an ellipsis, and the tag beside an item then shows only for a pill's reason or a cut name. `MokaMenu.RailLabels`
  is the same for any adaptive menu.
- `MokaNavEntry.Bottom` lists a screen, such as Settings, at the bottom of the shell's menu, under the list that scrolls.
- `MokaAccountCard` is drawn as the current place while one of its items is `Active`.
- `MokaSettingsForm.Appearance`: `Cards` draws the header and each group as a card of its own, the groups two to a row
  once the form is 46rem wide (a container query). `Icon` puts an accent tile before the title, `HeaderActions` puts
  controls at the end of the header, and `MokaSettingsGroup.Span="Full"` takes a whole row.
- `MokaTreeItem.HasChildren`. `true` draws the expand arrow before any children are loaded, so a tree can load them in
  `ExpandedChanged`; `false` draws neither the arrow nor the children. Unset, the arrow follows the child content as it
  did.

## [0.3.7] - 2026-11-07

### Fixed
- `MokaLineChart` letterboxed in a box wider than its 600 by 260 proportion allowed: a md chart wider than about 554 px
  drew at the height cap, centered between empty bands. The plot now stretches to fill the width. Tick labels are text
  beside and under it, in a column as wide as the longest label, lines and grid keep their width, and points and the
  latest-value dots are drawn so they stay round. Without a height the box is the size it always was.
- Enter in an `IMokaDialogService.PromptAsync` field confirms the prompt, as its confirm button does. The field is in a
  form of its own, so the browser submits it, and Enter while an IME is composing still goes to the IME.
- `MokaDropOverlay` lost the dropped files when it was disabled while `OnFiles` ran, such as during their upload: the
  render took its file input away, and the next file couldn't be read. The input now stays until `OnFiles` returns.
- `MokaSettingsLayout` and `MokaSettingsGroup` chose their columns by the window's width, so beside an app shell's rail
  a settings section was cramped and two-column groups overlapped. The layout becomes a list page below 45rem of its own
  width (a container query), and a group's column is never narrower than 16rem of the group's width.

### Added
- `MokaLineChart.Height`: a fixed height such as `"230px"`, whatever the width, for a dashboard card.
- `MokaStatusBar.Fixed`, default true. False lays the bar out where it is, as a row across its container, for a
  status row under a panel; fixed, it covered the app shell's account card.
- `moka-fill-main`, for the root of a page that fills `MokaAppShell`'s main area: the main column becomes as tall as the
  window under any fixed top bar, and the room kept under the page drops to the top padding plus a showing
  `MokaActionBar`. The theme also writes `--moka-main-padding-top` and `--moka-main-padding-bottom`, read from the
  `--moka-main-padding` shorthand, for a page that works out its own height.
- `MokaSearchInput.HideLabel`: keeps the `Label` as the input's name and hides it from sight, for a toolbar.

## [0.3.6] - 2026-11-07

### Added
- `MokaFlag` (Primitives): a country's flag from its two-letter code, drawn in the Twemoji Country Flags font so it
  looks the same on Windows as elsewhere, with a globe for a missing or unknown code. `GB-ENG`, `GB-SCT` and `GB-WLS`
  draw England, Scotland and Wales. `ShowTooltip` shows the name; `MokaFlag.ToEmoji` gives the emoji alone.
- The Twemoji Country Flags font ships in Core (`fonts/twemoji-country-flags.woff2`, 78 KB) and is declared in
  `moka-fonts.css` with the flag `unicode-range`, so a browser fetches it only once a flag is on screen. The artwork is
  Twemoji's, CC BY 4.0; the font is TalkJS's subset from country-flag-emoji-polyfill (MIT). The license and an
  attribution notice sit next to it.
- `MokaQRScanner` (Primitives): reads a QR code from the camera or from a picked image. Decoding happens in the
  browser, with its own `BarcodeDetector` where it reads QR codes and otherwise the bundled jsQR 1.4.0 (Apache-2.0,
  license included) in a worker, loaded the first time something is scanned. Frames never reach .NET. `Active` turns
  the camera on and off and follows a scan through `ActiveChanged`, `Accept` turns codes away without ending the scan,
  `Continuous` keeps going, and a denied, missing or busy camera says so and offers the image picker.
- `MokaDropOverlay` (Forms): a layer over the whole window that appears while files are dragged over it and takes
  the drop. `Accept`, `Multiple`, `MaxFiles` and `MaxFileSize` are checked in code, files that fail go to
  `OnRejected` with the reason, and its window listeners exist only while it's on the page and enabled.
- `MokaCodeEditor` (Forms): a text area for code or settings with line numbers, coloring from a `Highlighter` the app
  passes (`MokaCodeSpan`, `MokaCodeTokenKind`) and problems from `Diagnose` or `Diagnostics` (`MokaCodeDiagnostic`),
  underlined in place and listed below with a jump to each. On a static page it shows plain text and posts under the
  field's name.
- `MokaLineChart` draws a live window of samples: `Formatter` writes ticks, tooltips and the legend ("1.2 MiB/s"),
  `ShowCurrent` and `ShowPeak` put the latest values and the peak in the legend and its accessible name, `AutoScale`
  with `ScaleUnits` keeps ticks round in the unit shown, `SuggestedMax` keeps an idle chart flat, `Smooth` draws
  monotone curves, `ShowLastPoint` marks the latest values, `Live` slides the lines one step instead of jumping, and
  `EmptyText` fills an empty plot.
- `MokaIcons.Content.Globe`, `Camera` and `QrCode`.

### Changed
- `MokaLineChart` draws again only when a parameter differs from the last set, the series compared by value, so a
  parent rendering every second with the same window costs no render. A list changed in place still redraws.
- A long tick label moves `MokaLineChart`'s plot right to make room, where it used to run off the left edge.

## [0.3.5] - 2026-11-07

### Fixed
- `MokaAppShell` stopped the whole UI rendering under Trusted Types (`require-trusted-types-for 'script'`) in
  WebAssembly, and logged a CSP violation without them. It wrote its boot script into every render, and Blazor creates a
  script in markup by setting its text, a Trusted Types sink. Now only a static render (prerendering) writes it, with the
  `Nonce`; an interactive render writes no script. The rail attribute, `data-moka-rail`, is set on `<html>` rather than
  the shell's root, so the rail a prerendered page settled holds when Blazor replaces the shell going interactive. A
  style of your own that read `.moka-app-shell[data-moka-rail]` reads `[data-moka-rail] .moka-app-shell` now.
- `MokaReconnectModal` writes its `<script>` only in a static render, such as `App.razor`. In an interactive layout it
  imports the same module, rather than have Blazor set a script's `src`, which Trusted Types also block.
- `MokaSidebar`'s drawer on a phone faded its visibility in, so it was still hidden when it moved focus to its close
  button: focus stayed behind, and Escape and the Tab trap did nothing. It's visible the moment it opens, and still
  hides only once it has slid out.

### Changed
- A bUnit test that renders `MokaAppShell` or `MokaReconnectModal` has to call `SetRendererInfo`, since both now ask
  whether they render interactively.

## [0.3.2] - 2026-11-07

What MokaGate needs to replace its own table with `MokaTable`. Every addition is opt-in: a table that sets none of
them renders as it did in 0.3.1.

### Added
- `MokaTable.ExpandTogglePosition`: `End` puts the detail toggle in a narrow last column, after the row actions, with no
  tooltip. Its chevron turns when the row is open, it carries `aria-expanded`, and it is named "Show more about" and the
  row's `ItemName`. On the phone list it sits at the far right. `SingleExpand` closes the open row when another opens.
- `MokaTable.ItemsName`, the rows in the plural ("devices"), for "50 of 312 devices", "Show all 312", "Select all
  devices shown", the bulk bar's name and the loading status.
- `MokaTable.ShowAtLeastAsync(rows)`: under `ShowMore`, draws at least that many rows, reading a list changed in place.
- `MokaColumn.SortDescendingFirst`, for rates, sizes and times, and `MokaTable.AllowUnsort`: off, a sorted column only
  flips. A `SortComparer` is used as given in both directions.
- `MokaTable.Label`, the `aria-label` of the `table` element.
- `MokaColumn.Class`, on every cell of the column, header, filter, skeleton and aggregate cells included, so a page can
  hide a column at some widths.
- `MokaTable.ToolbarInFrame`: with `Framed`, the toolbar, chips and chooser are the card's first rows.
- `MokaTable.FootContent`, a row of any markup under the rows, inside the frame. `FooterContent` stays `tfoot` rows.
- `MokaTable.MinWidth`: the table scrolls sideways inside itself below it. With `Responsive="false"` and no `MinWidth`
  or `Height`, the header (and the bulk bar) sticks to the page under a fixed app bar.
- `MokaTable.BulkClearIcon`: the bulk bar's Clear as a cross named "Clear the selection".

## [0.3.1] - 2026-11-07

### Fixed
- On the new design, a `MokaTag` in a flex row that shrinks its items, such as a status bar, broke into one letter per
  line. A long word in a narrow card still wraps.

## [0.3.0] - 2026-11-07

### Changed
- `Moka.Red.Navigation` now depends on `Moka.Red.Feedback`, since the breadcrumb's fold menu is a `MokaDropdown`. An
  app on the `Moka.Red` package sees no difference; one that references Navigation alone now gets Feedback with it.
- `MokaBreadcrumb.Separator` is `string?` and null by default: a slash on Dark and Light, a chevron on the new design.
  Setting it works as before.
- `MokaCopyButton` says "Couldn't copy" when the browser refuses, where it used to say nothing, so a person didn't
  know they'd paste whatever they copied last. `FailedText` changes the words.
- A `MokaBadge` with no child content renders a `span` rather than a `div`, so a count can sit inside a button.
- **A new default look.** `MokaThemeProvider` without a `Theme`, `AddMokaRed()` and the `dotnet new` templates now
  start on `MokaTheme.Default`: dark graphite, a red accent, soft corners, roomy controls, sentence case and
  Space Grotesk titles. An app that names `MokaTheme.Dark` or `MokaTheme.Light` keeps exactly the look it has.
  An app that names no theme gets the new one on upgrade, and its light partner is `MokaTheme.DefaultLight`.
- `MokaThemeProvider`'s `DarkTheme` and `LightTheme` default to `MokaTheme.Default` and `DefaultLight`.
- Every component has a look for the new design, drawn only under `.moka-gate`, so the original design's rules
  didn't change. `MokaTable`'s `Dense` does nothing on the new design: the theme's density decides row height.
- On the new design `MokaToolbar` and `MokaAppBar` wrap instead of cutting off what doesn't fit, a toolbar group's
  `Label` sits beside its items, and `MokaCommandBar`'s search box fits the bar at either height.
- **Breaking: `Theme` on `MokaComponentBase` and `MokaInputBase<TValue>` is now read-only and doesn't redraw on a
  change.** As a cascading parameter it made any layout render redraw every Moka component on the page. A
  subclass that computes from the theme in C# declares its own `[CascadingParameter] MokaTheme?`, and one under a
  bare `CascadingValue<MokaTheme>` rather than a `MokaThemeProvider` reads that too. CSS variables carry a theme
  to everything else.
- **Breaking: don't dispose what `GetJsModuleAsync` returns.** It imports once per path per circuit or page and
  components share it, where it used to be one import per component. A component can also use more than one path
  now: it used to get its first module back for any path.
- **Hovering sends nothing to .NET** in `MokaSelect`, `MokaCommandPalette` and `MokaCascader`, where one row was a
  render of the whole list (412 edits and 18 KB in a select of 200 options). An arrow key still carries on from the
  hovered row, and Enter still picks it. `MokaRating`, `MokaDateRangePicker`, `MokaSchedulePicker` and a
  `MokaContextMenu` with no submenus listen only while hover does something.
- `MokaInfiniteScroll` watches with an `IntersectionObserver`, so scrolling sends nothing to .NET, and `Threshold`
  takes any CSS length. `MokaKnob` and `MokaSwipeActions` listen for pointer moves only during a drag.
- `MokaIcon` no longer renders again with its parent when nothing about it changed.
- `MokaThemeProvider` links `moka.css` and `moka-fonts.css` through `@Assets`, so they're fingerprinted and can be
  cached for good where the app uses `MapStaticAssets`.
- **`MokaTable` sends far less on every render.** Cells have no focus or key handlers of their own, and one
  listener in the module replaces them. At 25 rows by 5 columns a render where nothing changed went from 317 edits
  and 12.4 KB on Blazor Server to 15 edits and 584 bytes, and focusing a cell or typing in the search or filter
  boxes no longer renders at all. Arrow keys, Home and End no longer call .NET to move.
- A key in a control inside a `MokaTable` `CellTemplate` now reaches handlers above the table, so Escape in a text
  box in a cell closes the dialog around the table. Keys used to stop at the template.
- Enter with Shift, Ctrl, Alt or Meta held no longer starts editing a `MokaTable` cell.
- `MokaDialogHost` and `MokaTabContainer` open a component type directly rather than through `DynamicComponent`,
  which the trimmer cannot follow. `MokaFormBuilderExport` uses source-generated JSON and writes the same text.
- **Apps on `MokaTheme.Dark` or `Light` now get Inter and JetBrains Mono.** Their font stacks always named
  them, but no files shipped, so a machine without them fell back to Segoe UI or Cascadia Code. Pass
  `LoadFonts="false"` to keep that.
- The built-in themes, palettes, typography and spacing are shared instances, so
  `Theme="MokaTheme.Dark"` in markup no longer regenerates every token on every parameter set.

### Added
- **`MokaReconnectModal`**, Blazor Server's reconnect screen in the theme's look, in place of the plain dialog Blazor
  draws. Each state in words, with Try again, Resume and Reload, on .NET 9 and 10. `MokaReconnectText` changes the
  words. The `dotnet new mokared-server` template includes it for Server and Auto.
- **`MokaVirtualList.ItemsProvider`** loads the items in view on demand, for a list too long to hold in memory, with
  `Placeholder` and `RefreshDataAsync()`. `Items` is no longer marked required, since one or the other is. The docs
  cover the browser's limit on how tall a list can be.
- `MokaSelect.ValueTemplate` draws the chosen item in the closed box of a single select, the way `ItemTemplate` draws
  it in the list, so an icon stays once it's picked.
- **Boot screen variants.** `MokaBootScreen.BootVariant` picks Mark (a plain mark, a thin line and one sentence, the
  default on the new design), Log (startup lines ticking in), Steps (a checklist beside the brand), Classic (the
  original look, still the default on Dark and Light), and three more in Classic's family: Terminal, Pulse and
  Horizon. `Steps` (`MokaBootStep`), `Progress`, and a failed state with `Error`, `OnRetry` and `ErrorContent` work in
  every variant, and a small embedded box draws any of them as a compact row. The pre-boot splash in `moka-boot.css`
  matches Mark now, in Red's colors and light from the OS, with the original kept as `moka-boot--matrix`.
- **A new breadcrumb on the new design**: chevrons, quiet links on bigger targets, and the current page as a pill
  (`ShowCurrent` leaves it out under a page title). `MaxItems` folds the middle into a "···" menu of the hidden
  levels (`MoreLabel`), `Fold` folds it on its own in a narrow box, `ItemMaxWidth` cuts long names, and under 240 px
  it becomes a back link to the parent. `Look="Path"` draws a mono path you can copy (`Root`, `Copyable`, `CopyText`,
  `MokaBreadcrumbItem.Segment`). `MokaBreadcrumbItem.IconOnly` shows just the icon, named by a tooltip. Dark and Light
  keep their breadcrumb.
- **A new slash menu on the new design.** Items group under their `Category` as headings, icons sit in tiles, and
  `MokaSlashMenuItem.Shortcut` shows what you could type instead as key caps. The lit row keeps its text color with
  only its icon in the accent, the key hints are a footer with words, and an empty state says what was typed
  (`EmptyText`, `EmptyHint`). Dark and Light keep the slash menu they had.
- **`MokaRadialMenu`**, round buttons around what was right-clicked with its name in the hub, opened by right click,
  Shift+F10 or a long press and answered by a click, a flick, the arrow keys or a digit. Buttons can be dangerous or
  good, carry a count, or open a short list of choices. `IMokaContextMenuService.ShowRadial` opens it, and the one
  `MokaContextMenuHost` draws it, so a layout needs nothing new. `MokaRadialMenuTrigger` wraps content like
  `MokaContextMenuTrigger` does.
- A `MokaAppBar` with `Fixed`, and `MokaAppShell`'s phone top bar, report their height as `--moka-appbar-space`, and
  `moka.css` makes it the page's `scroll-padding-top` (and a `MokaActionBar`'s `--moka-action-bar-space` its
  `scroll-padding-bottom`). A link to part of a page, and a page opened at one, lands below the bar. Your own sticky
  parts can use `top: var(--moka-appbar-space, 0px)`.
- **Board customizing on `MokaBentoGrid`.** Give it a `Board` of `MokaBoardCard`s and people can reorder cards by
  dragging a grip or with the arrow keys, size them Small, Wide or Full, hide them into a tray, and go back to the
  default. Every change raises `BoardChanged` with the new board, and the app stores it. `MokaBentoItem` gets `Card`,
  `Icon`, `Source` and an Open link (`Href`) for a card's head. A grid without `Board` works as before.
- **`MokaRangeBar`**, one track with a range you drag inside it, and colored labelled parts, ticks and pins along it.
  Its value is a `MokaRange`, so `@bind-Value` works in an `EditForm`, and `Start` and `End` bind on their own too.
  `Formatter` writes the flags, so an app can show addresses or times. `MokaRangeSlider` stays the plain two-thumb
  slider.
- **`MokaChainCheck`**, a row of steps along a chain (`MokaChainStep`, tones `Ok`, `Warn`, `Fail`, `Wait`, `Off`), with
  a line between them that's dashed where a step can't be judged yet, and below them what's wrong with a `Fix`. It shows
  where a chain breaks, where `MokaSteps` shows progress.
- **`MokaJobProgress`**, a long job in words: what it's doing, a percent, a bar in the accent (sliding when there's no
  number), and facts such as bytes of total, speed and time so far.
- **`MokaAppShell`**, the frame around every page: a side menu built from `MokaNavEntry` records in named groups,
  the main area, and below 1280 px an icon rail (`Rail`, `MokaRailChoice`, `[` to flip it, kept for the tab). Below
  720 px the menu is a drawer behind a 52 px top bar (`TopBarEnd`, `TopBarTitle`). `Brand`, `Status` and `Account`
  slots, with `MokaAccountCard` and its menu of `MokaAccountItem`s for the account.
- **`MokaSettingsForm`**, a section of settings with a real save flow: what's set now, an Unsaved tag, a sticky
  footer with Save and Put back, a question before leaving unsaved edits, a refusal under the fields, and deep links
  that scroll to a field, mark it and put the cursor in it, turning on "Show advanced" first when needed.
  `MokaSettingsGroup` groups fields (`Advanced` hides them behind the toggle), and `MokaSettingsLayout` is the page
  around them: a searchable list of sections (`MokaSettingsSection`, `MokaSettingsEntry`) that is its own page on a
  phone.
- **`MokaActionBar`**, a floating bar for something waiting on the person, with a countdown ring, a body for what
  it changes, and room for the buttons. It reports its height as `--moka-action-bar-space`, which a settings form's
  footer and the page's scroll padding keep clear of.
- **`MokaErrorBoundary`**, which shows a failure in the app's look, with Try again, Reload and the app's own
  `Links`, `Compact` for a part of a page, and `RecoverOnNavigation`.
- **`MokaPageTitle`, `MokaPageTitleHost` and `MokaTabPresence`.** Pages name themselves and the layout adds a prefix
  such as `(2)` to the tab's title. `MokaTabPresence` says when the tab has been out of sight for a while, so
  refreshes can rest. `AddMokaRed()` registers `IMokaPageTitleService` through `AddMokaPageTitles()`.
- **`MokaTable` additions.** `BulkActions`, a bar in the selected tint while rows are picked, naming them through
  `ItemName`. Shift-click picks a run. `ShowMore` in place of numbered pages. `Framed` and `Bleed`. `Loading` draws
  skeleton rows in the real columns, and `EmptyIcon`, `EmptyTitle`, `EmptyText` or `Empty` an empty state. A
  column chooser (`ShowColumnChooser`) with `Layout` and `LayoutChanged` (`MokaTableLayout`), saved views as chips
  (`Views`, `ActiveView`, `ViewPicked`, `ViewSaved`, `ViewForgotten`, `MokaTableView`), and `RefreshAsync()`, which
  keeps the rows on screen while new ones load. Below 720 px the rows become a list, placed by `MokaColumn.Phone`
  (`MokaColumnPhone`). `MokaColumn.Fixed` and `Mono`, and `ItemKey` for a row's identity.
- `MokaGauge.Word`, `WordColor` and `AutoScale`, which raises the scale to the next round number and draws a half
  dial with both ends of it.
- `MokaWizard.Rail`, the steps in a rail down the side and the buttons in a bar along the bottom, with `ShowSkip`,
  `OnSkip` and a progress count. `MokaWizardStep.Decided` says what a step settled, and `HasProblem` marks one.
- `MokaTerminal.Commands` draws commands as blocks with their state and Copy, Edit (`OnEdit`) and Run again
  (`OnRunAgain`). `Ansi` decodes colors and styles in the text. Output renders in chunks of 200 lines, so adding a
  line to a long log redraws only the last chunk.
- `MokaMenuItem.Match`, which lights a link from the address and follows the person as they move, as `NavLink`
  does: `All` for the page itself, `Prefix` for it and below, the query and fragment ignored.
- `MokaMenuItem.Pill` (`MokaNavPill`), a count with a meaning beside the text and on the icon in a rail.
  `MokaMenu.Adaptive` draws items for rows and rail alike. `MokaSidebar.Drawer` makes it a drawer at 720 px and under.
- `MokaCopyButton.TextSource`, which reads the text when clicked, for text too long to build on every render.
- `MokaIcons.Navigation.ChevronsUpDown`.
- **`MokaPageHeader`**, what a page starts with: `Title`, `Eyebrow` (`EyebrowHref` makes it a way back), `Said` for
  what is so right now, `Lead` for what the page is for, `Actions` and `Media`. It stacks below 720 px.
- **`MokaTabs`**, the plain tabs on a page, with a line under the one showing, or `Segmented`, or `Stretch`. Items
  are `MokaTabItem`, with a count, an icon and an address of their own. It follows the WAI-ARIA tabs pattern.
- **`MokaCopyable`**, a value with its copy button, for addresses and keys. The button stays faint in a table row
  or `.moka-quiet-copies` until the row is hovered, and is larger on a touch screen.
- **`MokaNativeSelect<TValue>`**, a real `<select>`, for a phone's own picker and for long lists. Bound to a field of
  a type that reads back from text (strings, numbers, enums, dates, `Guid`), it works on a page with no interactivity.
- `MokaBadge.Tone`: `Count`, `Limited`, `NeedsYou` and `Ready`, a count with a fixed meaning on every theme.
- `MokaTheme.WithDescriptions(bool)` shows or hides text saying what a thing is for, such as `MokaPageHeader.Lead`
  and anything with the `moka-about` class.
- `MokaCard.Inner` with `Live` and `Dashed`, `MokaPaper.Panel`, `MokaGrid.MinColumn`, `MokaAvatar.Tile`,
  `MokaTag.New`, `MokaLabel.Micro` and `MokaCaption.Micro`, `MokaButton.Quiet`, `MokaStat.Tile`.
- `MokaAlert.FixAction` and `MokaCallout.FixAction` for the button that fixes the problem, and `MokaAlert.Accent`.
- `MokaEmptyState.Compact`, `Framed` and `Live`.
- `MokaProgress.Thin` and `Segments`, several colored parts side by side (`MokaProgressSegment`).
- `Optional` on `MokaFieldWrapper` and the fields that use it. `Mono` and `Suffix` on `MokaTextField` and
  `MokaNumericField`, `Mono` on `MokaTextArea`, `StartIcon` on `MokaNumericField`.
- `MokaTimePicker.Typed`, which reads 21:30, 9:30 pm, 9.30p, 21h30, 2130, 930pm, 9 pm and 21.
- `MokaSparkline.Height`, `ColorValue`, `Min` and `Max`. A `double.NaN` is a gap in `MokaSparkline` and `MokaLineChart`.
- `--moka-heading-tracking` and `--moka-about-display`. `MokaThemeSerializer` carries `descriptions`.
- A new look for `MokaSkeleton` and `MokaEmptyState` on the new design: a 1.4 s shimmer, and an icon in a tile.
- **Colors and style are separate choices.** Colors: `MokaTheme.Red`, `Gate`, `Crisp` and `Matrix`, each with a
  `*Light` partner, or `WithSurfaces(MokaSurfaces)` and `WithAccent`. Style, under any of them:
  `WithDensity(MokaDensity)`, `WithTextSize(MokaTextSize)`, `WithCorners(MokaCorners)`,
  `WithMainButton(MokaMainButton)`, `WithLetterCase(MokaLetterCase)`, `WithTitleFont(MokaTitleFont)` and
  `WithSelection(MokaSelection)`. None of them do anything on `MokaTheme.Dark` or `Light`.
- `MokaAccent.Red`. `MokaPalette.Graphite`, `GraphiteLight`, `NearBlack` and `NearBlackLight`, held to the same
  contrast targets as the others.
- 22 tokens: `--moka-button-height`, `-sm`, `-lg`, `--moka-button-padding`, `-sm`, `--moka-input-padding`,
  `--moka-segment-height`, `--moka-cell-padding`, `--moka-pill-padding`, `--moka-selected-bar`,
  `--moka-focus-ring-error`, `--moka-button-primary-bg`, `-color`, `-border`, `-hover-bg`, `-hover-border`,
  `--moka-font-size-heading`, `-card-title`, `-dialog`, `-menu`, `-mono` and `--moka-font-weight-heading`.
  `--moka-height-toolbar`, `-toolbar-dense` and `-statusbar` now follow density, through `MokaSpacing.ToolbarHeight`,
  `ToolbarHeightDense` and `StatusBarHeight`.
- `CssValues.WithPixels`.
- Red, Crisp, Matrix and Original in `MokaThemePresets`, whose old Moka Light and Moka Dark are now Original Light
  and Original Dark. `MokaThemeSerializer` carries `surfaces`, `selection` and `mainButton`.
- **The Gate look's tokens.** `MokaTheme.Gate` and `MokaTheme.GateLight` bring MokaGate's blue-grey surfaces,
  solid borders and blue accent.
- `MokaLook` and `MokaTheme.Look`. `WithDark` stays in the theme's look and colors.
- 55 tokens: five shades for each of success, warning, error and info (`--moka-color-success-bg`,
  `-ink`, `-text`, `-line` and so on), more surfaces, borders and text steps, the accent's selected shades,
  `--moka-font-family-display`, `--moka-font-size-xxs`, `-display` and `-root`,
  `--moka-control-case`, `-tracking`, `--moka-title-case`, `-tracking`, `--moka-radius-card`, `-popup`, and
  layout tokens such as `--moka-card-padding` and `--moka-control-height`.
- `MokaAccent`: six presets, and `FromHue` and `FromColor`, which find each shade by luminance so any hue
  meets the same contrast. `WithAccent(MokaAccent)`, and on Gate `WithAccent(string)` works this way too.
- `MokaTheme.WithHighContrast()` for the Gate look. `WithDensity` on Gate also swaps the layout tokens.
- **Fonts.** Inter, Space Grotesk and JetBrains Mono ship in `Moka.Red.Core` (225 KB of woff2, SIL OFL), and
  `MokaThemeProvider` links them through `moka-fonts.css`. `LoadFonts="false"` turns it off.
- Gate Light and Gate Dark in `MokaThemePresets`. `MokaThemeSerializer` carries `look` and `highContrast`.
- `RunDetached` on `MokaComponentBase` and `MokaInputBase<TValue>`, for work nothing can await.
- **Every package but Diagnostics is trimmable.** A Blazor WebAssembly app now trims what it does not use,
  which took the WASM sample's Moka.Red download from 611 KB to 95 KB on its home page and from 618 KB to 271 KB
  on a page using the table, dialogs and tabs (Brotli). A CI job drives a published, trimmed build in a browser.
- `MokaTableState.CancellationToken`, cancelled once the table has asked for something newer or gone away. Pass it
  on to whatever fetches the rows.

### Fixed
- **Forms posted nothing from a page with no interactivity.** No input had a `name`, so a sign-in form failed its own
  validation with every field typed in. `MokaTextField`, `MokaPasswordField`, `MokaTextArea`, `MokaNumericField`,
  `MokaPhoneInput`, `MokaCurrencyInput`, `MokaColorInput`, `MokaColorPicker`, `MokaCheckbox`, `MokaSwitch` and
  `MokaRadioGroup` now post under the field they're bound to, and `MokaSelect`, `MokaDatePicker`, `MokaTimePicker` and
  `MokaCascader` post their value in a hidden input, so a post doesn't blank it. Inputs on interactive server pages
  carry the `name` as well, as `InputText` does. New for custom inputs: `MokaInputBase.FieldName`,
  `MokaVisualInputBase.PostedValue` and `HiddenField`. The forms page lists what works on a static page.
- A retry right after a failed JS module import could get the same failed import back, under load, instead of trying
  again.
- The `dotnet new` layouts keep their sidebar and app bar in view while the page scrolls, and on the new design a
  `MokaSidebar` whose content overflows shows a thin scrollbar.
- `MokaBootScreen`: a WASM app on the new look jumped from the old splash to the new screen when Blazor took over;
  most screen readers never read the first status; full screen, Tab still reached the page underneath; and the bar
  stopped dead mid-track and left the track empty for part of each loop.
- **`MokaBreadcrumb` lost track of the current page.** A level added after the first render left the old last level
  bold and marked current, beside the new one, a level removed left a link with a "/" after it, and `MaxItems="1"`
  hid the current page. The "..." `MaxItems` left was dead, so the folded levels couldn't be reached; it is a "···"
  menu of them now, on every theme.
- **Floating panels showed behind other components or got cut off.** Popovers, dropdowns, hover cards, the
  notification panels, split button and theme switcher menus, menu bar menus, the table's column menu, and the select,
  suggestion and picker lists in Forms were positioned inside their trigger's wrapper, so a card or table that hides
  its overflow clipped them, and any later element with its own stacking context painted over them. They open in the
  browser's top layer now (the Popover API), placed against their trigger with CSS anchor positioning, or from the
  trigger's box where that isn't supported. While a panel is open the library sets its position, so app CSS can
  restyle it but not move it. `MokaSlashMenu` gets `Anchor` (usually its textarea) and `Placement` to do the same,
  taking the side of the editor with more room and no more than that room;
  without `Anchor` the app still places it. `MokaContextMenu` and its submenus, `MokaRadialMenu`, the tab strip's menu and
  `MokaAccountCard`'s menu open in the top layer too, so a transformed, filtered or contained ancestor no longer moves
  them and nothing paints over them.
- `MokaFadeIn`, `MokaSlideIn`, `MokaScaleIn` and `MokaStagger` let go of their animation when it ends, and
  `MokaReveal` drops its permanent `will-change`. A dialog, drawer or tooltip opened inside one was trapped in it,
  because a filling animation keeps acting as `will-change: transform`, which captures fixed descendants.
- The slash menu's lit row could sit out of sight below the list, and every row it was moved over made a round trip
  to .NET. The lit row scrolls into view now, hovering stays in the browser, and one row is lit at a time. On the new
  design its rows had 4 px of padding above and 10 px below, and so did `MokaDropdownItem`'s.
- On the new design, `MokaPanel`'s header had more padding below than above, and `MokaFormBuilder`'s palette and
  properties columns lost their side padding, because a two-value padding token was used where one value fits. A
  convention test now catches that in every stylesheet.
- `MokaContextMenu` could light two rows at once, one under the mouse and one from the keyboard. One row is lit now,
  a key carries on from the row under the mouse, and on the new design that row follows the accent.
- Scrolling past the end of a `MokaSidebar`'s body no longer goes on to scroll the page behind it.
- `MokaThemeEditor`'s sticky preview, `MokaSettingsLayout`'s side list and `MokaWizard`'s rail and progress bar stay
  below a sticky app bar instead of sliding under it.
- **`MokaSignaturePad` was blurry.** Its canvas kept a 600 px buffer that the browser stretched to the pad's width and
  the screen's density, and every live stroke was drawn over itself until its edges went jagged. The buffer now
  follows the displayed size in device pixels, refits on resize, zoom and a move to another monitor without losing
  the drawing, and strokes are smoothed and drawn once. `StrokeWidth` is really CSS pixels now. `Value` is a PNG at
  2 pixels per CSS pixel of the pad on every device, so an `<img>` showing it needs a width, or it shows at twice
  the pad's size.
- On the new design, the page heading drew a focus ring after each page change once a key had been pressed, because
  Blazor's `FocusOnNavigate` focuses it. A heading with `tabindex="-1"` gets no ring now.
- **`Breakpoints` on `MokaGrid` and `MokaFlexbox` did nothing** for any property the component also writes inline,
  which is nearly all of them: a grid asking for 5 columns at 1200 px stayed at 1, a flexbox stayed a column, and
  `Hidden` never hid. A breakpoint's values are now `!important`, so at its width one also beats `Style`. A page that
  has these set will change on upgrade, to what it asked for.
- **`MokaMenuBar` threw on every key.** It gave `moka-keys.js` key names where it reads rule objects, so no key was
  cancelled: the arrows also scrolled the page, and Enter and Space also clicked what they had just opened. A key
  held with Alt, Ctrl or Meta is left to the browser now, and doesn't move the bar or count as type-ahead. Choosing
  an item puts focus back on its menu, where it used to fall to the page.
- **A slow old `ServerData` answer could overwrite a newer one**, and the same in `MokaAutoComplete`'s `SearchFunc`.
  A replaced request's answer or failure is now ignored, and the loading state lasts until the latest one answers.
  A pick or a clear in `MokaAutoComplete` also stops a search waiting on its debounce.
- **A `MokaSignaturePad` drawing over 32 KB closed the Blazor Server connection.** It now arrives as a stream.
- **`MokaInfiniteScroll` never loaded more when an element around it did the scrolling**, which is how its own
  docs showed it. It now works whichever element scrolls, the page included. A first page too short to scroll also
  loaded nothing more, and now fills the space.
- `MokaDatePicker`, `MokaTimePicker`, `MokaColorPicker` and `MokaSelect` raised `ValueChanged` twice per change.
- `DebounceDelay` did nothing on `MokaTextArea`, `MokaNumericField`, `MokaPasswordField` and `MokaColorInput`.
  `MokaCurrencyInput` and `MokaPhoneInput` report on blur, so they have nothing to delay. With a delay set,
  `MokaTextField`'s clear button went stale until the parent rendered.
- `MokaKnob` stopped following a drag at its own edge, so one drag couldn't reach its full range, and a release
  outside it left the drag running. `MokaSwipeActions` left a drag running the same way.
- An arrow key scrolling a `MokaSelect`, `MokaCommandPalette` or `MokaCascader` list under a still mouse moved the
  highlight back to the mouse, and a stationary mouse showed a second highlight.
- `<ResourcePreloader />` in `mokared-server --Framework net9.0` built with a warning and rendered an unknown
  element.
- **`mokared-server --interactivity WebAssembly` and `Auto` couldn't boot WebAssembly**, because there was no client
  project. They now add `MyApp.Client/` inside the app's folder, with the routes, layout and pages, or only the
  counter with `--allInteractive false`. `cd MyApp && dotnet run` still works, and Server and None are unchanged.
- `launchSettings.json` from `mokared-server` was invalid JSON, so `dotnet run` could lose the Development
  environment and its static assets.
- `mokared-server` and `mokared-wasm` linked a `favicon.png` they never shipped, which logged a 404 on every load.
  They ship a `favicon.svg`.
- **A `MokaTable` whose `ServerData` threw on a search or filter could end the whole server process.** The
  debounce timer's callback was `async void`, so the exception was rethrown on the thread pool. It now reaches
  the error boundary.
- A consumer's handler that failed from a timer or a synchronous path now reaches the error boundary rather than
  vanishing: `MokaTextField.OnInput`, `MokaSearchInput.OnSearch`, `MokaAutoComplete.SearchFunc`, the carousels'
  `ActiveIndexChanged`, `MokaTypewriter.OnComplete`, and a `MokaCommandPalette` command run with Enter.
- `MokaColorPicker` raised `ValueChanged` twice per step of a drag.
- `MokaCountdown` no longer swallows an `ObjectDisposedException` thrown by its `OnComplete` handler.
- The WASM sample did not boot: `index.html`'s placeholders were served as written, so its script returned 404.
- `dotnet new mokared-wasm --Framework net9.0`, and `mokared-server --Framework net9.0` with WebAssembly
  interactivity, failed restore on a 10.0 package. The net10.0 templates' 10.0.12 packages, which carried a
  high-severity advisory, are 10.0.14. The WASM template fills in its preload link and import map on net10.0.

## [0.2.2] - 2026-11-04

### Changed
- Dependencies: ASP.NET Core Components 9.0.22 / 10.0.14 (the version the packages require, up from
  9.0.20 / 10.0.12), xunit.v3 4.0.2, xunit.runner.visualstudio 4.0.1, Microsoft.NET.Test.Sdk 18.11.0,
  bunit 2.13.3, Microsoft.Testing.Extensions.TrxReport 2.5.2, and the WebAssembly sample's packages 10.0.14.

## [0.2.1] - 2026-11-04

### Fixed
- **Closing a tab no longer logs errors from Moka.Red components.** When a Blazor Server circuit
  ends, Blazor disposes its components, and a JS call still on its way fails with
  `TaskCanceledException`. The components only expected `JSDisconnectedException`, so every closed
  or reloaded page with a dialog in it logged a pair of "Unhandled exception in circuit" errors from
  `MokaDialog`'s disposal (its module import, then its module disposal). Every component's teardown
  and its other JS calls now treat a disconnected circuit, a cancelled call and a disposed JS
  runtime alike. The ones that let the cancelled call through before: `MokaDialog`,
  `MokaBottomSheet`, `MokaCommandPalette`, the context menu and its trigger, the theme provider,
  the tab strip, the tab context menu, the browser tab storage, `MokaResizable`,
  `MokaImageCompare`, `MokaSortable`, `MokaReveal`, `MokaChat`, the color, date, date range, time
  and tree pickers, the segmented inputs, the theme import/export and the diagnostics overlay.
- **`MokaComponentBase` and `MokaInputBase` keep a lost circuit out of the logs.** A
  `DisposeAsyncCore`, a consumer's included, that fails only because the circuit has gone no longer
  surfaces as an unhandled error, and the cached JS module is disposed even when it throws. Any
  other exception still propagates.

### Added
- **`MokaJsInterop`** (`Moka.Red.Core.Base`): `IsCircuitGone(exception)` for a catch filter, and
  `SafeInvokeVoidAsync` and `SafeDisposeAsync` extensions on `IJSObjectReference?`, for components
  that manage their own JS modules.

## [0.2.0] - 2026-11-01

### Fixed
- **`MokaQRCode` drew codes that did not scan.** Two faults, either one fatal: the 15 format
  information bits were written in reverse order on both copies, so a reader could not learn the
  error correction level or the mask pattern, and the error correction block counts were wrong
  from version 5 up, which garbles the codeword interleave for everything but the smallest codes.
  The format bits are laid out least-significant first now, the block table matches the standard's
  for every version and level, and the encoder is proven by decoding: the tests round-trip every
  level across versions 1 to 40 through ZXing (a test-only dependency), payloads and all.
- **`MokaQRCode` grows to version 40.** It stopped at version 10 - 271 bytes at `Low`, too small
  for payloads like a Wi-Fi code with a long password or a vCard. Versions 11 to 40 lift the cap
  to 2953 bytes at `Low` (1273 at `High`), with the alignment patterns and codeword counts
  computed from the standard's formulas instead of transcribed tables. Give large payloads a
  proportionally larger `QRSize`, since the modules of a 177×177 code shrink as the data grows.

### Added
- **Dialog positions.** `MokaDialog` grows a `Position`: `Left` and `Right` are full-height panels
  that slide in from their edge, `Top` and `Bottom` full-width ones - the side sheet a settings
  pane or a filters panel wants, with everything a dialog already does (the focus trap, the scroll
  lock, stacking, the service). A side panel takes its width from the size presets or `Width`, a
  top or bottom one half the viewport unless `Height` says otherwise; the window-facing corners
  are square, the inner ones stay round; `FullScreen` wins over any edge; `Draggable` and
  `Resizable` stay centering-only, since an anchored panel has nowhere to be dragged to. The
  position is on `MokaDialogOptions` too, so a service confirm can be a panel. For a panel that is
  not modal at all, `MokaDrawer` with `Overlay="false"` remains the tool.
- **Dialog backdrop effects.** `Backdrop` picks what sits between the dialog and the page:
  `Default` (the dimmed page with the standard blur, as before), `Dim` (no blur), `Frosted` (a
  lighter dim with a strong blur - the frosted-glass look), `Dark` (a heavier dim), and `None`
  (nothing painted at all; the backdrop still catches clicks, so the dialog stays modal). Three
  new tokens back them - `--moka-color-backdrop-strong`, `--moka-color-backdrop-frosted` and
  `--moka-backdrop-blur-strong` - in `moka.css` and `MokaTheme.ToCssVariables()` both, so the
  effects follow the theme like the base backdrop does.
- **The exit animation.** Closing a dialog plays a short exit now instead of blinking off: the
  centered box fades down, an edge panel slides back to its edge, the backdrop fades with it. The
  close itself is decided the moment it starts - the focus trap, the scroll lock, `OpenChanged`
  and `OnClose` have all gone by then - and a new `OnExited` fires once the dialog is really gone.
  `MokaDialogHost` keeps a service dialog mounted while its exit plays, so confirm and prompt
  dialogs animate out too, and a reopen mid-exit cancels the removal and plays the entrance again.
- **A responsive dialog.** Under 640px the size presets give way to the viewport (`Small`,
  `Medium` and `Large` go full width, side panels full width, top and bottom panels capped at
  90vh), the wrapper's padding shrinks, and heights are measured in `dvh` where the browser knows
  it, so a dialog fits whatever chrome a phone is showing.
- **The chart family** (`Moka.Red.Data.Chart`): `MokaSparkline`, `MokaLineChart`, `MokaBarChart`
  and `MokaDonutChart`. Pure SVG, token-themed, no charting dependency: grid lines, tick labels,
  native tooltips, legends, grouped or stacked bars, an area under the line or a filled donut
  ring, and a palette that rotates the theme's semantic colors when a series does not name one.
- **`MokaHeatmap`** (`Moka.Red.Data.Heatmap`): the calendar heatmap, one square per day colored
  by how much happened - weeks across, Monday to Sunday, month and weekday names from the
  culture, the scale pinned or read from the data, native tooltips on every cell.
- **`MokaCascader`** (`Moka.Red.Forms.Cascader`): a drill-down selector over a tree of options -
  province/city/district, category trees, folder choosers. Each choice opens the column of its
  children, the value is the whole path (two-way bindable), the trigger shows it as a breadcrumb,
  and the keyboard walks the columns (arrows within, right into children, left back out). A value
  set from code opens drilled down to it; `ChangeOnSelect` chooses a parent without waiting for a
  leaf.
- **`MokaImageCompare`** (`Moka.Red.Primitives.ImageCompare`): two images over each other with a
  draggable divider between them, before and after. The drag runs in the browser (no re-render
  per move, the position reported once at the end), the divider is a slider for the keyboard, and
  the split can run top-to-bottom or left-to-right.

## [0.1.22] - 2026-11-01

### Fixed
- **A JS interop report could end the render it reported for.** `MokaComponentBase`'s optional observer was looked up
  in a `try` that caught only `ObjectDisposedException`, so a component whose JS call finished while its scope was
  being torn down — a circuit ending under the render the call belongs to — threw past it, taking an `OnAfterRender`
  chain and the app with it; an Mokaterm 0.2.6 start-up crash traced to exactly this. The lookup and the observer's
  callback are both guarded now: a report never breaks the component it reports for.
- **Every MokaCountdown test failed on the day the clocks changed** (1 November in their time zone): targets were
  built with wall-clock arithmetic, so "three hours from now" was four hours of remaining time across a fall-back.
  The targets are built on the instant and handed over in local time, DST-proof all year.

### Changed
- **`MokaSelect`'s dropdown reads like the menus beside it.** The chosen option carries a check mark of its own,
  single select included (a tint alone read as nothing in particular, and borrowed the accent's meaning), with the
  mark's gutter reserved so choosing never shifts a row's width. The panel is a raised surface like the tooltips and
  the context menu - border, soft shadow, accent ring - instead of a flat sheet over the content; it sizes to its
  content rather than the trigger, so a narrow trigger no longer squeezes long options; its scrollbar is the thin
  styled one instead of the browser's; rows breathe a little more and twelve fit before it scrolls; the trigger's
  chevron stands slightly prouder.

### Added
- **`MokaStatusBarSeparator`**, the thin vertical rule between a status bar's items, as a component: what apps had
  been drawing by styling the items' neighbours themselves (adjacent-sibling background hacks over the bar's own
  classes). It is a `role="separator"` with no tab stop, sized and coloured from the theme, and takes `Class` and
  `Style` like every component.

## [0.1.21] - 2026-10-18

### Added
- **`MokaMenuBar`**, the File/Edit/View row along the top of an app, with `MokaMenuBarMenu`, `MokaMenuBarItem` and `MokaMenuBarDivider`. It is one tab stop for the whole bar: Left and Right move between the menus and carry an open one with them, Down opens on the first item and Up on the last, the arrows and typing move inside a menu, Home and End jump, Escape closes and hands focus back to the label. A `role="menubar"` of `role="menuitem"` labels with `aria-haspopup`, `aria-expanded` and `aria-controls`, each popup a `role="menu"` named by its label. An item's `Shortcut` is drawn as key caps; choosing an item closes the menu before running the action, so an action that opens a dialog does not fight it for focus.
- **The hotkey registry**: `IMokaHotkeyService`, `MokaHotkey`, `MokaHotkeyHost` and `AddMokaHotkeys()` (which `AddMokaRed()` now calls). Registering a shortcut both binds it and lists it, so a shortcut cannot work without being documented or be documented without working. Registrations are `IDisposable`, re-registering an id replaces it in place, and `InvokeAsync(id)` runs one as a press would, so a menu item and its shortcut share an action. Matching happens in the browser, so a key that is not a shortcut never reaches .NET.
- **`MokaCheatsheet` builds itself from the registry.** With no `Groups` it lists the registered hotkeys, one group per `Group` in the order the groups first appear, with the keys split into caps. `MokaCheatsheet.FromRegistry` does the conversion on its own. Setting `Groups` still wins, so an app with its own list, or no registry at all, is unaffected.
- **Grouped rows in `MokaTable`**: `GroupBy` gathers rows under collapsible headings that span the table, with `GroupHeaderTemplate`, `GroupsCollapsible`, `InitiallyCollapsedGroups` and `OnGroupToggled`. Grouping gathers rather than reorders: a group appears where its first row landed and keeps the order the sort and filters gave it. Grouping wins over `Virtualize`, which has no room for headings between its rows.
- **`MokaValidationSummary`** lists every message the form's `EditContext` holds, above the fields. It renders nothing while the form is clean and is a `role="alert"`, so it is read out when it fills.
- **`MokaStatusBarItem.TooltipClass` and `TooltipStyle`** reach the tooltip element that wraps an item with a `Tooltip`. That element, not the item, is what the status bar's flex layout sees, so `min-width`, `flex-shrink` and `order` belong there; `Class` keeps styling the item itself.
- `MokaKeyValue.CopyLabel`, `CopiedLabel` and `CopyTooltipPosition`.

### Fixed
- **A list of copyable key-values gave a row of buttons all called "Copy".** A `MokaKeyValue` copy button is now named "Copy " and the key's `Label`, in its tooltip and for screen readers. The label's case is kept: lower-casing would mangle the acronyms these labels are full of (IP, API, SHA-256), and a name that matches the visible text is what WCAG's Label in Name asks for.
- `MokaInfiniteScroll` has tests for the first time, so every component in the library now has some.

### Changed
- The command palette's shortcut parser and matcher moved to Core's new `moka-hotkeys.js`, which the palette imports. The two had the same forty lines each, and a fix to one would have missed the other.
- `MokaComponentBase` gained a `SafeFocusAsync`, so components stop repeating the four catch blocks that moving focus needs.

## [0.1.20] - 2026-10-13

### Changed
- **Every icon-only control the library draws names itself with `MokaTooltip` instead of the browser's `title`.** A `title` showed the browser's own pale hint a second late, beside the Moka tooltips an app draws around its own buttons in the same row. Each control is named by `aria-label` and shows the same name in a `MokaTooltip` on hover and on keyboard focus, also while it is disabled. Converted from `title`: `MokaDockPanel`'s collapse, expand, undock and dock buttons; `MokaTabStrip`'s close and pin buttons and `TabInfo.Tooltip`; `MokaStatusBarItem.Tooltip`; a collapsed `MokaMenu`'s items; `MokaPagination`'s arrows; `MokaTable`'s column, export, refresh and density buttons and the reason a reorder handle is off; `MokaSelect`'s clear button; `MokaColorPicker`'s copy and eyedropper buttons and swatches; `MokaColorSwatch`'s swatches and custom colour button; `MokaFormBuilder`'s move and remove buttons; `MokaThemeToggle`, `MokaCopyButton`, `MokaScrollToTop`; `MokaSlashMenu`'s key hints. Named for screen readers before but with no hover name, and given a tooltip now: the close buttons of `MokaDialog`, `MokaDrawer`, `MokaAlert`, `MokaCallout`, `MokaNotice`, `MokaCheatsheet`, toasts and `MokaOnboarding`'s skip; the remove buttons of `MokaChip`, `MokaAttribute`, `MokaTagInput`, `MokaSelect` chips and `MokaFileUpload`; the clear buttons of `MokaTextField`, `MokaSearchInput`, `MokaAutoComplete`, `MokaTagInput` and `MokaTable`'s search; `MokaPasswordField`'s toggle; the date, time and colour pickers' toggles; `MokaCalendar`'s month arrows; the arrows and dots of `MokaCarousel` and `MokaInfiniteCarousel`; `MokaMediaGallery`'s lightbox buttons; `MokaChat`'s send; the expand toggles of `MokaTreeItem`, `MokaOrganizationChart`, `MokaPanel` and `MokaTable` detail rows; `MokaSplitButton`'s arrow; the notification bells and dismiss buttons; `MokaFloatingActionButton` when it shows only its icon. Given a name for the first time: `MokaDatePicker`'s month arrows (bare `<` and `>`), `MokaDateRangePicker`'s month arrows, `MokaTreeSelect`'s expand toggles (with `aria-expanded`), `MokaTransferList`'s arrows and `MokaColorSwatch`'s custom colour buttons.
- Tooltips follow the controls' places: below a row of buttons, lined up with the edge at a row's end; above a row at the bottom of a view (a status bar, a pager, a chat's input, toasts at the bottom); beside a column (a collapsed menu or dock strip, tree and table toggles, a list's remove buttons). A button that opens a popup in the same place (pickers, the column menu, the split button, the notification bells) shows no tooltip while it is open.
- **`MokaTooltip` ships in `Moka.Red.Primitives`**, under its old namespace `Moka.Red.Feedback.Tooltip`, so every package can draw it. `Moka.Red.Feedback` forwards `MokaTooltip` and `MokaTooltipPosition` there, and references Primitives already, so nothing that compiles against Feedback changes.
- **A tooltip shows only its own text.** The shown state matched every tooltip inside the hovered trigger, so a tooltip around a closable chip also showed the chip's "Remove". It matches the trigger's own popup now.
- `MokaStatusBarItem.Tooltip` and `TabInfo.Tooltip` wrap at 20rem and keep line breaks; on an item or tab that shows text they are its accessible description, as the `title` was. Each tab now sits inside its tooltip's element (`.moka-tab-tooltip`), with or without a `Tooltip`, and the tab's tooltip gives way while the pointer is on a button inside the tab. A status bar item with a `Tooltip` sits inside its tooltip's element (`.moka-statusbar-item-tooltip`): app CSS that relied on items being adjacent siblings (`.moka-statusbar-item + .moka-statusbar-item`) or that sets an item's flex behaviour in the bar (`min-width`, `flex-shrink`) moves to that element, for example `.moka-statusbar-item-tooltip:has(> .my-item)`. The same goes for every wrapped control: the tooltip's element is what its parent lays out.
- **`MokaScrollToTop` and `MokaFloatingActionButton` have a root element that holds their place.** The fixed button inside a tooltip would have left the tooltip behind in the page, so the root takes the fixed position, `Class`, `Style` and the margin (a style still moves the button), and the button keeps `Id`, attributes and, for the FAB, the padding and rounding. `MokaThemeToggle` and `MokaCopyButton` keep `Class` and `Style` on the button.
- Buttons placed inside an input, at the edge of a carousel or lightbox, under an org chart card, and the centred toast hosts are positioned with auto margins instead of a `transform`, which would have made them the box their tooltip's fixed popup is placed in.
- `MokaCodeBlock`'s copy button lost its `title`, which only repeated its visible "Copy" label. Library buttons that had no `type` and sit where a form could submit (`MokaCallout`, `MokaAlert`, `MokaDialog` and toast close buttons, `MokaFormBuilder`'s move and remove buttons) are `type="button"`.

### Added
- **`MokaToastHost` offsets**: `OffsetTop`, `OffsetRight`, `OffsetBottom` and `OffsetLeft` keep a strip of the window clear, such as an app's 48px title bar or a 24px status bar. Each moves only the edge it names, for the positions against that edge, on top of the existing padding; the centred positions stay centred between the side offsets. They are written as `--moka-toast-offset-top` and the rest, only when set, so an app can also set them from its own CSS on the host or around it. `MokaToastHost` also takes `Class` and `Style`.
- **`MokaTooltip.MaxWidth`**: text longer than the given width wraps, and a line break in `Text` starts a new line. Without it a tooltip stays on one line, as before.
- `MokaThemeToggle.TooltipPosition`, `DarkModeLabel` and `LightModeLabel`, and `MokaCopyButton.TooltipPosition`.

## [0.1.19] - 2026-10-12

### Changed
- **A tooltip is a raised surface instead of an inverted box.** It was `on-surface` on `surface`, which in the dark theme is a near-white block that glared in a dark title bar. It now uses the surface, outline and popup shadow the context menu uses (a red-tinted ring and glow in the dark theme), and `--moka-tooltip-bg`, `--moka-tooltip-color` and `--moka-tooltip-border` let an app retune it.

## [0.1.18] - 2026-10-12

### Fixed
- **A tooltip stayed up after its button was clicked.** The shown state matched `:focus-within`, and a button keeps focus after a mouse click, so the tooltip stayed once the mouse had gone and came back whenever a menu or dialog closed and handed focus back to the button that opened it. Keyboard focus (`:focus-visible`) shows it now; a click does not.
- **Leaving a tooltip's trigger waited as long as hovering it did.** The delay was a `transition-delay` on the popup itself, so it applied in both directions and a tooltip trailed the mouse onto the next button. `Delay` now sets `--moka-tooltip-delay`, which only the shown state reads.
- **A tooltip inside a container that clips its overflow was cut off, and one at the window's edge ran past it.** Where the browser supports CSS anchor positioning the popup is placed against the window next to its trigger (`position-anchor`, `position-area`), and `position-try-fallbacks` moves it to the side with room: a `Top` tooltip at the top of the window shows below, a `Right` one at the right edge shows on the left. Each tooltip names its own anchor. Browsers without anchor positioning keep the old placement.

## [0.1.17] - 2026-10-11

Everything Mokaterm reported while building on 0.1.16.

### Fixed
- **A split pane nested in another took both axes' styles.** The direction rules reached their parts with descendant selectors, and a nested split is the same component with the same CSS-isolation scope, so a vertical split inside a horizontal one matched both: in a pane grid the column handles came out a few pixels square. Every rule that styles a split's own handle, grip and halves is a child selector now.
- **A tooltip near the window's edge could scroll the page sideways.** A hidden tooltip was `visibility: hidden`, which still takes part in layout, so a centred one on the right-most button of a toolbar reached past the window and made the page scrollable; a focus or `scrollIntoView` then shifted the whole layout. A hidden tooltip generates no box at all now, and the fade is kept with `transition-behavior: allow-discrete` and `@starting-style`.
- **A context menu opened with Enter or Space landed in the top-left corner.** `IMokaContextMenuService.Show(MouseEventArgs, items)` saw that the event came from the keyboard but still used its position, which a keyboard click reports as 0,0. The menu now opens under whatever has focus, measured while it positions itself and before it takes focus. The context-menu key and Shift+F10 are unchanged: the browser positions those on the focused element already.
- **A segmented control hid the options that did not fit.** The track clipped its overflow and never wrapped, so in a narrow row the last options were gone with nothing on screen saying so. It wraps instead.
- **A split pane measured its first pane once**, on its first render, so one laid out hidden or at another size kept a stale pixel size that every later drag started from. It measures again whenever its own size changes, and `MeasureAsync()` forces it.
- **`MokaSearchInput` asked a flex parent for the browser's default input width** (about 200px) and would not shrink below it. It follows its container.

### Changed
- **A split pane's handle drag runs in the browser** and reports back once, when the drag ends. It used to run a .NET handler per `pointermove`, re-rendering both panes and everything in them on every move; the listener was also attached while nothing was being dragged, so merely moving the pointer across a split cost a round trip. Without the module the old path still works.
- The command palette footer, the slash menu hints and the rest of 0.1.16's key caps are unchanged; this release adds no new caps.

### Added
- **`MokaDialog` header slots**: `Icon`, `TitleContent` (markup in place of the title text, inside the heading the dialog is named by) and `HeaderActions` (controls between the title and the close button). All three are on `MokaDialogOptions` too, so a service dialog gets the same header.
- **`MokaDialog` body and size control**: `BodyPadding` and `ScrollBody` turn off the padding and the body's own scrollbar, so a view that scrolls itself shows the only scrollbar, the way `MokaDockContent` already worked. `MokaDialogSize.ExtraLarge` is 1200 by 900 with each side kept inside the window, and `Width`, `MaxWidth` and `Height` take any CSS length. All on `MokaDialogOptions` as well.
- **`MokaTooltipPosition.TopStart`, `TopEnd`, `BottomStart` and `BottomEnd`** line a tooltip up with one edge of its trigger instead of centring it, for a trigger at the edge of the window.
- **`MokaRadioGroup.AriaLabel`** names a group that already sits under a heading of its own, without drawing a second one. Without it the group is still named by `Label`.
- **`MokaFieldWrapper.LabelIdFor`** is public, so a control named by `aria-labelledby` rather than `<label for>` can work out the field label's id.
- **`MokaCalloutType.Info`**, an alias for `Note` under the name callers coming from other libraries look for first.
- **`MokaSplitPane.MeasureAsync()`** measures the first pane again, for a split shown after something the automatic watch cannot see.

## [0.1.16] - 2026-10-06

### Changed
- **Every shortcut hint in the library is drawn as key caps.** The context menu already did; now `MokaCommandPalette` splits a command's `Shortcut` the same way and draws a `MokaKbd` per key, its footer hints (arrows, Enter, Esc) are caps, `MokaSlashMenu`'s header hints are caps, and `MokaSearchInput`'s `/` hint is one. The palette's shortcut chip and the slash menu's hand-rolled key style are gone, so the same `Ctrl+S` looks the same wherever it appears.
- The command palette footer is dimmed with a text colour rather than `opacity`, which washed the caps out along with the labels.

### Added
- **`ShortcutKeys.Split`** in `Moka.Red.Core.Utilities` reads a hint such as `"Ctrl+Shift+P"` as the keys it names, so a consumer can draw caps the way the library does. Spaces around the separator are dropped and an empty piece between separators is the plus key itself, so `"Ctrl++"` is Ctrl and plus. The context menu's private copy of this is gone.

### Fixed
- **A dock panel's resize splitter had almost no hit area**, so hovering a panel edge rarely showed the resize cursor. The grab zone was a pseudo-element centred on the edge, reaching outside the panel, and `MokaDockPanel` clips its overflow, so the outer half was cut away and about a pixel was left to aim at. The grab zone is the splitter's own width now (6px, inside the panel) and nothing changes on screen, since the panel's border still draws the line.

## [0.1.15] - 2026-09-28

Re-release of 0.1.14. Only the version strings changed, so there is nothing here to upgrade for if
you are already on 0.1.14.

## [0.1.14] - 2026-09-28

### Changed
- **A context menu draws a shortcut hint as one key cap per key** instead of a run of monospace text, so it reads the way the cheatsheet and the rest of the library already do. `MokaContextMenuItem.Shortcut` is unchanged, still a `string?`: the split happens at render time, and an empty piece between separators is the plus key itself, so `"Ctrl++"` is Ctrl and plus rather than Ctrl and two separators.

## [0.1.13] - 2026-09-19

### Added
- **MokaTabContainer.StorageKey** saves the open tabs after every change and restores them the first time a container renders in a new session, so a reload brings them back. It goes through `ITabStorageProvider`, which shipped with a browser implementation that nothing used: `AddMokaTabs` now registers it against `sessionStorage` unless you registered a provider of your own. A container mounted again in the same circuit keeps the live tabs, and a session that cannot be read is never overwritten.
- **OTP, PIN, IP and MAC inputs take part in `EditForm` validation.** They accept `ValueExpression` (which `@bind-Value` supplies) and `Required`, show the field's validation message, carry the `modified`, `valid` and `invalid` classes, and report `aria-invalid`, `aria-required` and `aria-describedby` on every box. The group is named by its label, and OTP and IP boxes are named one by one. `MokaPinInput` now renders `Label`, `HelperText` and `ErrorText`, which it used to ignore.
- **MokaSelect.IsOpen** is a two-way bindable parameter. `IsOpenChanged` existed, but the list could not be opened from markup.
- The date, time, colour, date range and tree pickers open with ArrowDown or Space on their field, report `aria-haspopup` and `aria-expanded`, and give focus back to the field when Escape closes them.
- **Context menu submenus work from the keyboard.** Right, Enter or Space moves focus into a submenu and highlights its first enabled item, Left or Escape closes only that submenu and returns to its item, and nested submenus work the same way. Items with children report `aria-expanded`, and each submenu is named by its item.
- A keyboard click on a `MokaContextMenuTrigger` opens the menu under the trigger's content instead of in the top-left corner.
- **Keyboard and screen reader support** for components that only worked with a mouse:
  - `MokaRating` is one slider: a single tab stop, the arrow keys, Home and End, and `aria-valuenow` and `aria-valuetext` ("3 of 5"). A read-only rating keeps its tab stop, a disabled one leaves the tab order.
  - `MokaAvatar` and `MokaStatusBarItem` with `OnClick`, and a `Clickable` `MokaCard`, are buttons: a tab stop, a focus ring, and Enter or Space to activate. A collapsible card's title area is its toggle, with `aria-expanded`.
  - `MokaMediaGallery` thumbnails are buttons named by their `Alt` or `Caption`. The lightbox is a modal dialog: focus moves in and stays there, the arrow keys and Escape work straight away, and closing returns focus to the thumbnail.
  - `MokaThemeSwitcher` is a menu button with a checked item per theme: the arrow keys, Home, End, Enter or Space to pick, and Escape or Tab to close.
- **MokaCheatsheet and MokaDrawer take focus when they open**, keep Tab inside and give focus back when they close, so Escape works without a click first. A drawer without `Overlay` is no longer announced as modal.
- **moka-drag.js `makeResizable` takes a `target`**: `'track'` (the default) resizes the grid track, as the dock needs, and `'element'` resizes the element itself. Its new `resolveLength` understands px, rem, em, %, vw and vh.
- `preventKeys` in `moka-keys.js` takes `unlessModified`, which leaves a key alone while Ctrl, Alt or Meta is held.
- **More keyboard and screen reader support**:
  - `MokaMenuItem` renders links and buttons that Tab reaches, a parent is a disclosure button with `aria-expanded`, the active link has `aria-current="page"`, and a collapsed group is `inert`. `Disabled` now works: a disabled link loses its `href`.
  - Each `MokaStep` header is a button with `aria-current="step"`, disabled when the step cannot be reached.
  - `MokaDropdown` follows the menu button pattern: Enter, Space or Down open it on the first item, Up on the last, the arrows, Home and End move, Escape and Tab close, and choosing an item closes the menu before its action runs.
  - `MokaSplitButton`'s arrow is a named menu button (`ToggleLabel`, default "More options") with the same keys, and choosing an item closes the menu.
  - `MokaNotificationCenter` and `MokaNotificationBell` open as disclosures, their notifications are buttons, and focus moves sensibly on Escape, Clear and dismiss.
  - `MokaVirtualList` is a listbox with one tab stop: the arrows, Page Up/Down, Home and End move the active item and Enter or Space raise `OnItemClick`.
  - Enter on a `MokaTable` cell raises `OnRowClick` (editable cells keep Enter for editing).
  - `MokaTable`'s sortable headers hold a real button: Tab reaches it, Enter or Space sorts (with Shift, adds a column in a multi-sort), and the header of the first sorted column has `aria-sort`. The mouse still sorts from anywhere in the header, and a column drag still starts there, in Firefox too.
  - Required form fields report `aria-required`.
  - `MokaKnob` is a working slider: the arrows move one `Step`, Page Up/Down ten, Home and End go to the ends, and it is named by `Label` or an `aria-label`.
  - `MokaRangeSlider` thumbs have a focus ring and names ("Price start", "Price end"), and `MokaSlider`'s ring now shows in Firefox too.
  - `MokaCreditCardInput`'s fields each have a name, and the group is labelled.
  - `MokaPagination`: every button is named (the compact pager's arrows had none), the current page has `aria-current`, the page size select is labelled, and the pager is a navigation region.
- **MokaOnboarding** scrolls an off-screen target into view and keeps the spotlight on it while the page scrolls or resizes. The step card is a modal dialog that takes focus, Escape skips the tour (when `ShowSkipButton` allows it), and a step whose target is missing shows its card centred instead of leaving an overlay with nothing to click.
- **MokaSignaturePad** draws a `Value` passed in by the parent, and clearing it from the parent clears the drawing.
- **MokaCountdown**'s `Flip` style animates each digit that changes, as its documentation always said.
- **MokaResizable** handles work from the keyboard: the right and bottom edges are focusable separators, the arrow keys resize by 10px (50px with Shift), and Home and End go to the limits.
- **MokaBottomSheet** takes focus when it opens, keeps Tab inside and gives focus back on close. Dragging its handle down past about a third of its height closes it. It is named by `Header` or an `aria-label`, and `Id` and extra attributes land on the dialog element.
- **Context menus**: Tab closes the menu and puts focus back; a menu opened from the keyboard highlights its first item; a menu without room below opens above the pointer or trigger. `IMokaContextMenuService` has a `Show(x, y, items, openedFromKeyboard, anchorTop)` overload and `OpenedFromKeyboard` and `AnchorTop` members, with default implementations.
- **MokaStepper shows the active step's content**: below the steps when horizontal, under the step when vertical. `ChildContent` was documented and never rendered.
- **MokaPopover.AriaLabel** names the popup; without it the popup is named by the trigger's text. The trigger reports `aria-haspopup`, `aria-expanded` and `aria-controls`.
- **Shared page scroll lock** in Core (`moka-scroll-lock.js`), which `moka-dialog.js` re-exports, so dialogs, bottom sheets and the `MokaMediaGallery` lightbox share one count.
- **An overlay MokaSidebar works like a modal dialog.** Opening it moves focus inside, Tab and Shift+Tab stay inside, Escape closes it (raising `OpenChanged`) and focus goes back to the opener. Turning `Overlay` off while it is open only lets go of Tab.
- **MokaSlashMenu.ListboxId and ActiveOptionId** give the host textarea its `aria-controls` and `aria-activedescendant`, so screen readers hear the highlighted row while focus stays in the editor.
- **MokaScrollToTop.ScrollContainerSelector** watches and scrolls an inner panel instead of the window, for dock layouts and app shells where the window never scrolls. `Label` names the button (default "Scroll to top").
- `MokaStatusDot.Live` makes a dot a polite live region, `MokaSegmentedControl.AriaLabel` names the group, and `MokaDiffViewer.AddedLabel` and `RemovedLabel` set the word screen readers hear before a changed line.
- `.moka-visually-hidden` in `moka.css` hides text from the eye but not from screen readers.
- `.moka-fill-width` and `.moka-fill-height` in `moka.css` fill the container with the element's margin inside it (`stretch`, with the vendor keywords and 100% as fallbacks).
- `MokaFieldWrapper.Style`, `Class` and `CustomControl`: the field-wrapped inputs use `Style` for their margin, `MokaFileUpload` (which has no element of its own) uses `Class`, and `CustomControl` keeps the label's id but drops its `for`, for a control that `aria-labelledby` names instead, and `MokaSegmentedInputBase.SegmentStyle`, the radius each box of a segmented input takes.
- **Backdrop tokens**: `--moka-color-backdrop` (the theme background at 85%) and `--moka-backdrop-blur` (8px). Every modal backdrop uses them: dialog, bottom sheet, drawer, command palette, cheatsheet and overlay sidebar.
- **MokaThemeSerializer.TryFromJson** returns the theme or a readable reason (a wrong type, a null, a syntax error with its line and position) instead of throwing. The import box shows the reason.
- **IMokaJsInteropObserver** in Core hears about every JS interop call `MokaComponentBase`'s helpers make, with its duration, when one is registered. Moka.Red.Diagnostics registers one, which fills its Network tab.
- **CssValues** in Core (`Moka.Red.Core.Utilities`): the checks the library uses before a string reaches SVG markup, a `url()`, a data URI or a style block. `IsColor`, `IsLength`, `IsLengthList`, `IsSafe`, `IsSelfContained`, `ColorOrDefault`, `LengthOrDefault`, `TryParsePixels`, `Url`, `SvgDataUrl` and `EscapeXml`.
- **UrlValues** in Core: `HasBlockedScheme` and `SafeHref`, which drop a `javascript:`, `vbscript:` or `data:` link the way a browser reads the scheme (tabs and newlines inside it, leading spaces and control characters ignored).
- The diagnostics popup on Blazor Server shows the live data of the window that opened it. It runs its own circuit, so it used to show its own, empty data.

### Changed
- **An open popup keeps Escape to itself.** `MokaSelect`, `MokaAutoComplete`, `MokaTagInput` suggestions, `MokaPopover`, the pickers above and `MokaContextMenu` stop the key, so a surrounding `MokaDialog` no longer closes along with them.
- **MokaFormBuilder** sends every change through `FieldsChanged` as a new list instead of editing the list passed to `Fields`.
- `MokaDateRangePicker` shows a range as `start - end`.
- `MokaCodeBlock`, `MokaTerminal`, `MokaInfiniteScroll` and the diagnostics overlay use the shared `.moka-thin-scrollbar` and `.moka-hide-scrollbar` utilities instead of their own scrollbar rules.
- `MokaSelectBase`: the protected open state is now `DropdownOpen`, since `IsOpen` is the parameter.
- **Components keep the user's change when the parent renders again**, and only a new value from the parent replaces it: `Open` on `MokaDialog`, `MokaBottomSheet`, `MokaDrawer` and `MokaCheatsheet`, `Visible` on `MokaCookieConsent` (an answered banner came back); the value or state parameters of `MokaRating`, `MokaThemeSwitcher`, `MokaCard`, `MokaPanel`, `MokaDockPanel` (`Collapsed`, `Floating` and its position), `MokaResizable`, `MokaPagination`, `MokaOnboarding`, `MokaSlashMenu`, `MokaWizard`, `MokaColorWheel`, `MokaCreditCardInput`, `MokaKnob`, `MokaSearchInput`, `MokaSignaturePad`, `MokaSlider`, `MokaRangeSlider`, `MokaToggleGroup`, `MokaCommandBar`, `MokaSidebar`, `MokaAttribute`, `MokaCarousel`, `MokaChip`, `MokaColorSwatch`, `MokaConfetti`, `MokaInfiniteCarousel`, `MokaSegmentedControl`, `MokaTreeItem`, `MokaThemeToggle`, `MokaThemeEditor`, and `Value` on the OTP, PIN, IP and MAC inputs. Each used to write the user's change into its own parameter, and the next parent render put the old value back. With one-way binding, a parent that wants to reopen or reset one of them has to pass a different value, or bind it with `@bind-`.
- `MokaRating.ValueChanged` and `MokaThemeSwitcher.SelectedThemeChanged` no longer fire when the pick equals the current value.
- A dock panel collapsed to zero size is `inert`, so its header, actions and content leave the tab order and the accessibility tree. An empty `CollapsedSize` counts as zero instead of dropping the grid track.
- `IMokaNotificationService.Push` with an `Id` that is already listed replaces that notification in its place, so `MarkAsRead` and `Remove` always mean one entry.
- `MokaStepper` puts its steps in an inner `.moka-stepper__steps` list, so the active step's content can sit below them.
- `MokaIcon` with `Color="MokaColor.Surface"` draws in the text colour on a surface instead of the surface colour itself, which made it invisible.
- The dialog and bottom sheet backdrop now sits beside the box instead of under a full-screen wrapper, so a stacked service dialog dims the one below it.
- A collapsed dock panel strip shows only its title and the expand button; the undock button and `Actions` are hidden (still mounted) until it expands.
- `--moka-z-appbar` is 1035, below every overlay. At 1100 the app bar and `MokaStatusBar` drew over open dialogs, and a dialog dragged to the top hid its header under the bar.
- **MokaWatermark** sizes its tiles from the text and `Gap` (a fixed 300x200 tile cut long text off and ignored `Gap`), uses the theme's font instead of the browser's serif, and paints in `var(--moka-color-on-surface)` by default instead of black, which barely showed on the dark theme. `Color` now accepts tokens and `var()`.
- `makeDraggable` in `moka-drag.js` reacts to the primary button only, ends a drag on `pointercancel`, and does not report a press that never moved.
- `MokaSignaturePad` applies `Height` (default 200px), which it never did: pads were a third of their width tall. The drawing buffer keeps a width of 600, so saved signatures keep their width, and takes its height from the displayed shape.
- Focus handling in `moka-dialog.js`: Tab and Shift+Tab always move through the dialog's tabbable elements from wherever focus is, and closing an overlay returns focus to where it was even when that was inside another overlay.
- **MokaSegmentedControl is a radio group.** Each segment is a native radio, so Tab reaches the selected segment and the arrow keys move the selection. It was a tablist without the tab keys or panels. `Id` and unmatched attributes on a `MokaSegment`, such as `aria-label` and `title`, now go on its radio; `Class` and `Style` stay on the segment.
- **MokaDiffViewer** computes the diff with Myers' algorithm instead of an LCS table. A 50,000-line file with a few hundred edits takes milliseconds, and the 2,000-line cap is gone; a step budget stops a huge, very different pair from freezing a render. A block of added or removed lines that could sit in several places goes as far down as it can, like git: an appended function no longer shows as starting with the previous function's closing brace.
- `MokaStatusDot` is no longer a live region by default, since a table of dots all announcing at once talked over each other. Set `Live` for a single status that changes. A dot without `Label` but with an `aria-label` is `role="img"`.
- `MokaSelectionBar` announces the count ("3 files selected") from a hidden live region that is always on the page, instead of `aria-live` on the toolbar, which a bar that appeared already filled never announced.
- `MokaLoadingOverlay` makes its content `inert` and `aria-busy` while loading, and the alert layer no longer carries `aria-busy`, which could stop screen readers from announcing it.
- `MokaSidebar` sits at `--moka-z-modal` over a `--moka-z-modal-backdrop` backdrop tinted with `--moka-color-background`, and animates over `--moka-transition-slow`. An inline sidebar no longer sets `z-index: 100`, which covered popups from the page next to it; `Elevated` lifts it one layer so its shadow shows.
- `Margin`, `Padding` and `Rounded` on inputs: the margin goes on the field's outer wrapper, the padding and radius on the element that draws the border (the input, the select trigger, the tag container). On a checkbox or switch the radius shapes the box or track. On `MokaMeter` and linear `MokaProgress` the radius shapes the track, and the fill follows it.
- **Theme export and import cover every property.** JSON and C# export write every theme, palette, typography and spacing value; they are written from one table per record, so a property added later cannot be left out. Import matches names in any case, takes a missing value from the built-in light or dark theme (whichever `isDark` names), and skips keys it does not know, so a theme exported by a later version still imports.
- The extra ThemeGen presets work out the colours they do not set (surface and text steps, dim fills, glow and border tokens) from their own palette instead of taking the default theme's.
- **Moka.Red.Diagnostics is optional where it is used.** Without `AddMokaDiagnostics()` the overlay renders nothing and the diagnostics page says the services are not registered; both used to throw. The Memory tab's cards are "Allocated" and "Heap (last GC)", with what each measures.
- The z-index of every popup is on the token scale: the pickers, select, autocomplete and tag input suggestions sit at `--moka-z-dropdown` (they were at 100, under sticky headers and app bars), the notification panel at `--moka-z-dropdown`, context menus at `--moka-z-popover`, the media gallery lightbox at `--moka-z-modal` and `MokaScrollToTop` at `--moka-z-fixed`. Context menus and the lightbox were at 999999, over toasts and tooltips.
- The drawer, bottom sheet and command palette backdrop was a flat 50% black, and the dialog's a near-black that was just as dark on the light theme. All now use `--moka-color-backdrop`, which dims a dark theme and frosts a light one, with the same blur.
- `MokaToggleGroupItem` is a plain toggle button with `aria-pressed`; it also had `role="option"`, which belongs in a listbox.
- `MokaTimeline`'s `Alternate` layout applies from 641px up; below that the items stay on one side of the line.
- **MokaTable columns are identified by the column itself**, not by `Title`: the column toggle, column moves and resized widths work for columns without a title, and two columns with the same title no longer share them. Sorting and filters still name a column by `Title`. A column's `Visible` is the state it starts in: the toggle can show a column that starts hidden, and a new `Visible` from the parent replaces the user's choice.
- **MokaTable sorting**: a new `SortColumn` or `SortDirection` from the parent replaces the current sort, a multi-column one included, and goes back to page 1; a value that `@bind-` hands back is not new, so it no longer loads the rows a second time. `SortComparer` works in multi-column sorts and needs no `Field`, and a header that has nothing to sort by shows no sort icon.
- `MokaTable` row reordering: the handles are off while a sort, search or filter is active, and `OnRowReordered` reports positions in `Items`, so `RemoveAt(OldIndex)` then `Insert(NewIndex, Item)` applies the move.
- `MokaTable.SelectionActions` receives a copy of the selection, and `ServerData` copies of the sort descriptors, so neither can change the table's state behind its back.
- `MokaTable` marks drop targets for column and row drags in `moka-table.js` instead of calling .NET on every `dragover`, which re-rendered the table many times a second. Drag handlers are only attached while dragging is on.
- **Id, Class and extra attributes on the Forms inputs land on the control.** A consumer's `Id` becomes the id of the focusable control on every field-wrapped input, and the label's `for` and the ids built from it (`{Id}-label`, `{Id}-listbox`) follow it; `MokaCheckbox` and `MokaSwitch` put it on their checkbox, `MokaRadioGroup` on its radio group. Unmatched attributes such as `name`, `autocomplete` or `aria-describedby` go on the control in `MokaDatePicker`, `MokaTimePicker`, `MokaColorPicker`, `MokaSelect` and `MokaRating`, on the group in `MokaRadioGroup` and on the file input in `MokaFileUpload`. `MokaDateRangePicker` and `MokaTreeSelect` move `Id` and extra attributes from their outer element to their text input and trigger button.
- `MokaTheme.ToCssVariables()` leaves out a token whose value holds `;`, `{`, `}`, `<`, `>` or `\`, so the `moka.css` default applies. A font name written with CSS escapes is dropped too: write the characters instead.
- `MokaVideoEmbed` puts a YouTube or Vimeo address in an iframe only when it is an http, https or protocol-relative URL; anything else goes to a `video` element.
- A closed `MokaSidebar` drops its margin and padding, so a closed sidebar leaves no strip or gap; the overlay sidebar is sized by `top` and `bottom`. `MokaCheatsheet` is centred by a flex wrapper, and a full-screen `MokaBottomSheet` stretches in CSS, so a margin no longer pushes either off the screen.
- `MokaTable` attaches its row click, row context menu and cell double-click handlers only when `OnRowClick`, `OnRowContextMenu` or an editable column needs them. Every click on a row used to go to .NET and render the whole table again.
- `MokaTable` column drags and row drags draw the drop marker on the side the item will land on.
- `MokaColorInput`'s swatch shows only hex colours (`#rgb`, `#rgba`, `#rrggbb`, `#rrggbbaa`), the values the input is for. It used to show CSS colour names too.
- `MokaTreeSelect`'s button is named by its label and its current choice ("Department Engineering"), `MokaRating` and `MokaSelect` by their label through `aria-labelledby`: a label's `for` cannot name a `div`.
- **MokaPopover**'s root class is `moka-popover` (it was `moka-popover-wrapper`), built from its root class like every component, and `Id` goes on it. Rename any CSS aimed at `.moka-popover-wrapper`. `MokaDropdown` passes its `Id` there too: its own root takes no part in the layout.
- A fixed `MokaSelectionBar` sits in a fixed strip along the bottom of the screen, centred by flexbox, and may use the whole width less a small gap (it was capped at half). Clicks beside the bar reach the page.
- **Diagnostics**: the Network and Render tabs' sortable headers are buttons with `aria-sort`, the Theme tab's token rows are buttons with a "Copied" announcement, the Console and Theme filters are labelled, and a component that has not rendered shows "never".
- **StyleBuilder** drops a value that could end its own declaration or swallow the next one: a `;` outside quotes and brackets, a brace, an unclosed string, bracket, comment or `url(`, or a trailing `\`. A `;` inside quotes, brackets or `url(...)`, as in a data URI, is fine. `AddStyle(style)`, which takes a consumer's own `Style` text, is unchanged. The built string no longer ends with `;`.
- **Link parameters drop `javascript:`, `vbscript:` and `data:` URLs**: `MokaLink`, `MokaButton`, `MokaCard`, `MokaListItem`, `MokaBreadcrumbItem`, `MokaMenuItem`, `MokaBlockquote.CitationHref` and `MokaCommand.Href`. A blocked button, card or row renders as its non-link form. Relative URLs, fragments, `http`, `https`, `mailto`, `tel` and other schemes are untouched.
- Theme import rejects a value the theme cannot use, with the reason: `'palette.primary' must be a CSS color, like #ef5350 or rgb(239 83 80).`, and the same for lengths and fonts.

### Removed
- `MokaToggleBase.IsChecked` (protected), which only repeated `CurrentValue`.
- `MokaSidebar.CollapsedChanged`, which was never raised: nothing inside the sidebar collapses it. `@bind-Collapsed` no longer compiles; pass `Collapsed` one way.
- `MokaLoadingOverlay.LoadingChanged` and `MessageChanged`, which were never raised. Pass `Loading` and `Message` one way.

### Fixed
- **Security: markup injection through SVG parameters.** `MokaBarcode` (`ForegroundColor`, `BackgroundColor`, `TextSize`), `MokaQRCode` (both colours) and `MokaIdenticon` (`Palette`, `Background`) wrote parameter strings into SVG they render as markup without escaping, so a quote ended the attribute and what followed became markup. `MokaWatermark` wrote `Text`, `Color`, `FontSize` and `ImageSrc` into its SVG unescaped. Colours are now checked (hex, keywords and colour functions such as `rgb()`, `oklch()` or `var()`; `url()` is refused), lengths are checked, and every value is escaped. An invalid value falls back to the default. Do not pass untrusted input to these parameters on older versions.
- **Security: CSV injection in the MokaTable export.** A cell starting with `=`, `+`, `-`, `@`, a tab or a carriage return went into the CSV as it was, so a spreadsheet ran it as a formula when the file was opened (`=HYPERLINK(...)` and worse). Such cells now start with `'`, apart from plain numbers such as `-5`, and a cell holding a carriage return is quoted.
- **Security: script injection through `MokaVideoEmbed.Src`.** Any address containing "youtube.com" went into an iframe, so `javascript:...//youtube.com` ran script in the page as soon as the component rendered.
- **Security: CSS injection** through `MokaGridBackground` (`PatternColor`, `DashArray`, `HighlightColor`, `BackgroundColor`), `MokaParallax.BackgroundImage`, theme values, which `MokaThemeProvider` writes into a `<style>` element (a `}` in a colour could restyle the whole page), and the colour inputs (`MokaColorInput`, `MokaColorPicker`, `MokaColorWheel`), which wrote typed text straight into a `style` attribute. Each value is checked and escaped, and an invalid one falls back to its default.
- **Security: `javascript:` links.** The `Href` of `MokaLink`, `MokaButton`, `MokaCard`, `MokaListItem`, `MokaBreadcrumbItem` and `MokaMenuItem`, `MokaBlockquote.CitationHref` and `MokaCommand.Href` took `javascript:`, `vbscript:` and `data:` URLs, so a link built from user input ran script when clicked (the command palette navigated to it).
- **Security: CSS injection through style parameters.** A string parameter that reached a `style` attribute, such as a colour or a width, could add declarations of its own with a `;` (a full-page overlay, a tracking `url()`). `StyleBuilder` now drops such values, the style strings `MokaColorSwatch`, `MokaKanbanBoard`, `MokaLogViewer`, `MokaMeter`, `MokaTerminal`, `MokaTabStrip`, `MokaThemeSwitcher`, `MokaSpinner`, `MokaProgress` and the ThemeGen editors built by hand go through it, and colour parameters are checked.
- **MokaTabStrip**: a middle click closed the tab on release but left the press alone, so Windows started auto-scroll once the tabs overflowed. Every middle press in the strip is now cancelled.
- **A parent re-render undid the user's change** in `MokaPopover` (`Open`), `MokaDateRangePicker` (`StartDate`, `EndDate`), `MokaTreeSelect` (`Value`, `SelectedValues`) and `MokaAutoComplete` (`Value`). Each wrote the user's change into its own parameter, and the next render passed the old value back in.
- **MokaFormBuilder**: fields that shared an `Id` made Blazor throw on the next render, and selecting one highlighted both.
- **Inputs leaked their `EditContext` subscription.** Blazor calls only `DisposeAsync` on a component that has one, so `InputBase`'s own cleanup never ran and a form kept every input removed from it alive.
- **OTP, PIN, IP and MAC inputs** left a rejected character on screen, such as the "." typed after "10" in an IP box, where it also counted against the box's length. On Blazor Server, typing fast dropped characters: moving to the next box waited for a server round trip, so the next key hit the full box. The browser now moves focus itself (`moka-segments.js`, new in Forms), and the separator key moves on without being typed.
- **Responsive breakpoints**: `MokaFlexbox` ignored `RowGap` and `RowGapValue` at a breakpoint. A breakpoint value containing `;`, braces, `<`, `>` or a backslash could break out of the generated style element; such values are now dropped.
- **Context menus**: End with every item disabled pointed `aria-activedescendant` at a row that does not exist, and closing a submenu from the mouse dropped keyboard focus on the page.
- **MokaPanel and MokaAccordionItem** cut off content taller than 1000px and 500px: the body opened to a fixed `max-height`. It now opens to the content's own height. A collapsed body stayed in the tab order; it is now `inert`. The panel's toggle had no name and neither toggle reported its state; both have `aria-expanded` and `aria-controls` now. An accordion item's `Id`, `Style` and extra attributes were dropped.
- **MokaCard**: a `HeaderActions` button also toggled a collapsible card and clicked a clickable one, toggling a clickable card also clicked it, and a card with `Href` never raised `OnClick`.
- **MokaAvatarGroup** never overlapped its avatars or drew their ring: its rules could not reach the child components.
- **MokaIpAddressInput** ignored `Size`.
- **MokaThemeSwitcher**'s button had no `type`, so it submitted a surrounding form.
- **MokaResizable** inside a CSS grid resized the grid track instead of itself, and snapped back to its starting width when the parent passed `Width` one way. `MinWidth` and `MaxWidth` with units other than px were misread.
- **Dock layout**: `MinSize` and `MaxSize` applied to the panel element rather than its grid track, so a panel could sit smaller than its track; the dock imported its JS module twice; a dock panel's collapse button did not report its state.
- **MokaDialog** closed by the parent kept its drag state, so it could not be dragged after reopening and came back where it was left. **MokaBottomSheet** created open imported its module twice and locked page scroll twice, so the page stayed locked after it closed.
- **Styles that never applied.** Rules for a component's own root were written behind `::deep`, or targeted a child component without it, so browsers never matched them: `MokaSidebar` (its layout, `Bordered`, `Elevated`, `Overlay`, the closed state and the backdrop), `MokaMenu` (`Bordered`, `Dense`, `Collapsed`), `MokaStepper` (horizontal steps were stacked), `MokaBreadcrumb`, and the error border of `MokaTextField`, `MokaTextArea`, `MokaPasswordField`, `MokaNumericField`, `MokaSelect`, `MokaAutoComplete` and `MokaTagInput`. `MokaButtonGroup`'s rules were malformed since 0.1.4, so grouped buttons never joined up.
- **MokaTable**: keys pressed in a control inside a `CellTemplate` also reached the cell, so arrows typed in an input moved to another cell, Enter on a button started an edit and Space selected the row.
- The tree's context-menu key opened a context menu as if right-clicked, with nothing highlighted.
- `MokaNotificationCenter` dropped exceptions from its render after a service change.
- `MokaDrawer`'s close button and every `MokaPagination` button submitted a surrounding form.
- **MokaOnboarding** started again on its last step after finishing, and a tour on from the first render stayed dark until something else rendered it.
- **MokaCurrencyInput** reported the unclamped amount and then clamped silently, so the parent kept a value outside `Min`/`Max`.
- **MokaKnob** threw on a key or the wheel when `Max` was below `Min`, left the keys dead with a `Step` of 0, and gave values like 0.7000000000000001.
- **MokaRangeSlider**: a thumb dragged past the other stayed there; where the thumbs met, the end one always caught the mouse, so they could not be pulled apart downwards; `Id` and extra attributes were dropped.
- **MokaCreditCardInput**: typed letters stayed on screen, a CVV stayed at 4 digits after leaving Amex, and a Visa pasted over an Amex was cut to 15 digits.
- A `MokaSignaturePad` that started read-only or disabled could never be drawn on.
- **MokaCountdown**: with `ShowDays="false"`, 2 days and 3 hours showed as 03 hours; a new `TargetDate` after completion was ignored and one set while running showed only at the next tick; a UTC target was off by the local offset; `OnComplete` lost its exceptions and fired during initialization when the target had passed.
- **MokaIdenticon** redrew only when `Value` changed (size, palette and background changes did nothing), and `Rounded` only worked as `Full`.
- **MokaPriceDisplay** rendered "$F-1" for a negative `DecimalPlaces` and rounded the discount half to even.
- **MokaCopyButton** let a lost circuit's exception escape the click handler and changed its state off the renderer's thread.
- **`CloseOnBackdropClick` never worked** in a browser for `MokaDialog` and `MokaBottomSheet` (since 0.1.4): a full-screen wrapper sat over the backdrop and took the click. A click closes the overlay only when the press also started on the backdrop.
- **A draggable MokaDialog jumped** by half its size on the first drag, opened with its corner at the middle of the screen for the length of its open animation, and wrote its position with the current culture, which broke it under a decimal comma.
- **MokaDialog** could set up a second focus trap during a re-render and never release the first, and a click beside the box focused a wrapper outside the trap.
- **MokaBottomSheet**'s Escape did nothing until the user clicked into it.
- **Dock layout**: a panel collapsed to a strip clipped its expand button out of view but kept it in the tab order.
- **MokaResizable**: changing `Direction` left the new handle dead, limits changed after the first render never reached the drag, a touch drag scrolled the page, and a `%` limit on a grid item was taken of the whole grid.
- Escape in a `MokaMediaGallery` lightbox inside a dialog closed the dialog too, and a gallery removed with its lightbox open left the page scroll locked.
- **MokaConfetti** with a one-way `Active="true"` fired again on every parent render. **MokaThemeToggle** and **MokaPanel** without a binding did not redraw after a click. In **MokaSegmentedControl** and **MokaToggleGroup** the previous choice stayed highlighted. **MokaAttribute** kept its pill shape after `Pill` went false. **MokaCreditCardInput** showed no card brand for a number passed in by the parent.
- **`Margin`, `Padding` and `Rounded` work on every visual component.** About ninety ignored some or all of them: the typography components, `MokaStat`, `MokaNotice`, `MokaCallout`, `MokaBadge`, `MokaChip`, `MokaListItem`, `MokaMeter`, `MokaRibbon`, most Layout containers, every Forms input (the sliders, `MokaKnob`, `MokaSignaturePad`, `MokaCreditCardInput` and the OTP, PIN, IP and MAC inputs included), the overlays (`MokaBottomSheet`, `MokaCheatsheet`, `MokaPopover`, `MokaDropdown`, `MokaLoadingOverlay`, `MokaSidebar`), `MokaEmptyState`, `MokaSpinner`, `MokaSkeleton`, `MokaProgress`, `MokaNotificationBell`, `MokaMenuItem`, `MokaSegmentedControl`, `MokaStatusDot`, `MokaSplitButton`, `MokaThemeSwitcher`, `MokaVirtualList` and `MokaTable`. Where a component's root is not the box it draws, the radius goes on the box: the track of a slider or meter, each box of a segmented input, the dot of a status dot, the outer corners of the end buttons in `MokaButtonGroup` and `MokaSplitButton`, the panels of `MokaTransferList`, the units of `MokaCountdown` and the thumbnails of `MokaMediaGallery`.
- `MokaDatePicker`, `MokaTimePicker` and `MokaColorPicker` ignored `Style`, `MokaPopover` did too, and `MokaDropdown`'s `display: contents` root dropped the margin and `Style` it was given.
- **Escape inside a MokaDrawer or MokaCheatsheet** also closed a dialog or drawer around it.
- **The page behind an open MokaCheatsheet or modal MokaDrawer still scrolled.** Both take the shared scroll lock and give it back on every way out.
- A closed `MokaSidebar`'s links stayed in the tab order.
- `MokaLoadingOverlay`: Tab reached the controls under the overlay, and the text, rectangle and card skeletons came out zero wide.
- `MokaBootScreen`'s embedded scanline moved only its own 2px height instead of sweeping the container, and `moka-boot.css` kept animating with reduced motion on.
- `MokaSegmentedControl`, `MokaThemeToggle` and `MokaScrollToTop` ignored `Disabled`.
- `MokaTimeline`'s `Alternate` moved the line to the middle, under the items, instead of putting the items either side of it. A dot's icon was always drawn in the on-primary colour, and a `Surface` dot vanished into the page.
- `MokaDiffViewer` marked changed lines only with colour and a hidden marker, so screen readers read them as unchanged.
- `MokaScrollToTop` called .NET on every scroll event, a message per frame on Blazor Server.
- **Escape in a MokaDialog inside a drawer or dialog** closed the outer one too.
- The page behind an open overlay `MokaSidebar` still scrolled.
- **ThemeGen**: a hex field threw on text shorter than seven characters (only `#rgb` was handled), which ended the circuit; a palette value the native colour input could not read did the same. The field now takes `#rgb`, `#rgba`, `#rrggbb` and `#rrggbbaa` and puts the previous colour back on anything else.
- **ThemeGen**: export left out properties, so import brought back a different theme; the C# export did not escape strings and broke on `NaN` or infinity; buttons had no `type`, so they submitted a surrounding form; many inputs had no accessible name.
- **Diagnostics**: the Settings tab's log size and console level only applied after a restart; the Network tab never received a call; the popup ignored `<base href>`, so it failed under a sub-path; the Memory tab's two cards showed the same figure under two names.
- The DevApp answered 404 on a direct load of `/moka-diagnostics`: the page's assembly was missing from `AddAdditionalAssemblies`, which the docs now list as a step.
- **MokaTable**: dropping a column header moved the wrong column once the order had changed, because screen positions were taken as registration positions. A sort the parent set was ignored after a header click, the header icon did not follow it, and in client mode a sort set from the start never sorted.
- **MokaTable**: saving a `Format`ted cell unchanged reported an edit; the grid's tab stop could stay on a cell that a page change, filter or hidden column had removed; `@bind-PageSize` hid the pager after the user's pick, and a page size pick loaded the rows three times.
- **MokaTable**: hiding a filtered column kept its filter with no control left to clear it; `Sticky` and `HideOnMobile` missed the filter, skeleton and aggregate cells; toolbar buttons submitted a surrounding form; a cancelled drag left its highlight behind.
- **MokaTable** selection: with `SingleSelect`, unticking the selected row did not clear it, and a parent that handled only `SelectedItemsChanged` lost every tick on its next render. `Rounded` was ignored.
- **Forms inputs ignored `Id`**, nineteen of them, rendering a generated id instead, so a label or script could not find the control by it. `MokaDatePicker`, `MokaTimePicker`, `MokaColorPicker`, `MokaPhoneInput`, `MokaCurrencyInput` and `MokaFileUpload` ignored `Class`, and seven inputs dropped extra attributes. `MokaSlider` and `MokaTagInput` labels named no control.
- `MokaCodeBlock`'s copy button chained `ContinueWith`, let a lost circuit's exception escape and changed its state off the renderer's thread, like `MokaCopyButton` did.
- The diagnostics Settings controls had no names and its buttons no `type`, the popup page lacked the Tree, Network and Memory tabs, and form inputs' JS calls never reached the Network tab.
- **MokaTable**: Enter in a table nested in a detail row also reached the outer table, which raised its `OnRowClick`; a column parameter the parent changed (`Title`, `Width`, `Align` and the rest) showed one render late; with `Virtualize`, the tab stop could sit on a row that was not rendered, leaving the grid unreachable by Tab; turning `ShowFilters` off kept the filters on with no control to clear them; a column whose `Title` went away kept sorting the rows.
- **Forms validation classes**: `MokaDatePicker`, `MokaTimePicker`, `MokaColorPicker`, `MokaPhoneInput`, `MokaCurrencyInput`, `MokaCheckbox`, `MokaSwitch` and `MokaRadioGroup` never carried `modified`, `valid` and `invalid` in an `EditForm`. `MokaCurrencyInput` had no `aria-required`.
- **MokaFileUpload drag and drop never worked**: the drop zone cancelled every drop and nothing read the files. The invisible file input now covers the whole zone, border included, and takes the drop, so a dropped file arrives like a picked one. `Accept` is checked for both (it only filtered the dialog), a drop of several files without `Multiple` keeps the first, `MaxFiles` caps the whole list instead of losing an oversized pick, and the drag highlight no longer switches off early or stays on after a drop. A disabled upload could still open the dialog from the keyboard, remove files, or let a drop fall through and open the file in the tab. Its remove buttons are named after their files.
- `MokaCalendar` wrote `aria-selected` as a bare attribute, so screen readers heard no value.
- **Comma-decimal and other locales**: `MokaColorPicker`'s thumbs and alpha gradient broke; `MokaNumericField` showed 1.5 as "1,5" on a German page and read the next digit back as 152; `MokaFormBuilder`'s `Min` and `Max` printed a Unicode minus in Swedish, which the number input showed as empty, and lost negative values in Arabic. `MokaProgress` wrote "45,5%"; `MokaNumberTicker`'s digits stayed at 0 where the minus sign is U+2212; a `MokaSwipeActions` drag broke on a decimal comma; negative angles in `MokaMeteors` and `MokaRetroGrid`, negative `MokaPopover` offsets and `MokaGridBackground`'s numbers were dropped.
- The labels of `MokaRating`, `MokaSelect` and `MokaTreeSelect` named no control.
- A margin made a box sized at 100% overflow its container: `MokaNotice`, `MokaMenu`, an inline `MokaSidebar` (in height), `MokaCarousel`, `MokaInfiniteCarousel`, `MokaMediaGallery`, `MokaVideoEmbed`, `MokaSteps`, a linear `MokaProgress`, a `FullWidth` `MokaSegmentedControl` or `MokaButton`, `MokaMeter`, and a `MokaMenuItem` inside a menu. A margin also pushed a fixed `MokaSelectionBar` off centre.
- The diagnostics Theme tab wrote token values straight into its swatches' style attributes.
- `MokaGridBackground` drew nothing for a NaN or infinite number or a `CellSize` of zero or less; those fall back to the defaults.
- `MokaCopyButton` and `MokaCodeBlock`: after quick repeated copies, an earlier copy's timer reset the "Copied!" state early.

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
- **MokaKanbanBoard** - `ItemTemplate` is now optional (nullable). When omitted, items render as their `ToString()` value. Previously crashed with NullReferenceException.
- **MokaKanbanBoard** - `ColumnWidth` is now nullable; avoids invalid `width: ; min-width: ;` inline styles when unset.
- **Docs preview host** - registered `AddMokaRed()` services in `Program.cs` to fix `IMokaToastService` injection error.
- **Docs preview host** - removed 404 references to non-existent `moka-reset.css` and `moka-tokens.css` (merged into `moka.css`).
- **Docs** - fixed 19 compilation errors in preview code blocks: record constructor syntax, wrong type names, wrong enum types, wrong icon category paths.

## [0.1.7] - 2026-04-10

### Added
- **Matrix dark theme** - complete overhaul of the dark palette: `#060608` background, `#0c0c10` surface, red-tinted borders (`rgba(239,83,80,0.06-0.24)`), glow rings instead of drop shadows, Inter + JetBrains Mono font stacks, 4/8/10/12px radius scale, 120/150/200ms transitions.
- **New palette tokens** - `SurfaceHover`, `Surface2`, `Surface3`, `PrimaryGlow`, `PrimaryGlowMd`, `PrimaryGlowStrong`, `PrimaryBorder`, `PrimaryBorderDim`, `OnSurfaceTertiary`, `OnSurfaceQuaternary`.
- **New CSS tokens** - `--moka-focus-ring`, `--moka-selected-glow`, `--moka-color-surface-hover`, `--moka-color-surface-2`, `--moka-color-surface-3`, `--moka-color-primary-glow*`, `--moka-color-primary-border*`, `--moka-color-on-surface-tertiary`, `--moka-color-on-surface-quaternary`.
- **MokaGridBackground** - decorative container with 6 grid patterns (Lines, Dots, Dashed, Cross, DiagonalLines, Honeycomb), fade edges, center glow highlight, customizable cell size/color/opacity.
- **MokaRetroGrid** - perspective vanishing-point grid with animated scroll, glowing horizon line, configurable angle/perspective/cell size.
- **MokaMeteors** - animated shooting star streaks with bright head dots, fading tails, thickness variation, configurable count/angle/color.
- **MokaOrbitingIcons** - icons revolving around center content with counter-rotation, orbit path ring, pause/reverse support.
- **MokaGlassCard** - glassmorphism card with backdrop-filter blur, translucent tint, optional glow border, keyboard accessible.
- **MokaBentoGrid** + **MokaBentoItem** - asymmetric bento-box grid layout with column/row spanning, clickable items, keyboard accessible.
- **MokaGridPattern** enum - Lines, Dots, Dashed, Cross, DiagonalLines, Honeycomb.
- Documentation pages for all 6 new components.

### Changed
- **Button CSS** - uppercase labels with `letter-spacing: 0.08em`, transparent backgrounds with colored border accents, glow hover/active states. All variants (filled, outlined, text, soft) updated.
- **Card CSS** - red-tinted borders, glow on hover instead of translateY, uppercase micro-label titles.
- **TextField CSS** - red-glow focus ring (`--moka-focus-ring`), monospace placeholders.
- **Dialog CSS** - backdrop blur, red-tinted border, uppercase header titles.
- **Toast CSS** - severity-colored glow shadows per toast type.
- **List CSS** - inset glow on selected items, surface-hover on hover.
- **Chip CSS** - uppercase labels, square radii (not pills), glow focus ring.
- **Accordion, Popover, Sidebar, Alert** - updated to matrix aesthetic.
- **Scrollbar styling** - uses red-tinted `--moka-color-primary-border` thumb color.

### Fixed
- **OrbitingIcons** - removed unnecessary `::deep` CSS selectors; uses scoped `__icon-wrap` div instead.
- **GlassCard/BentoItem** - added `role="button"`, `tabindex="0"`, and Enter/Space keyboard handlers for accessibility.
- **Hardcoded `10px` font sizes** - replaced with `var(--moka-font-size-xs)` in Card, GlassCard, BentoItem.
- **Meteors CSS** - replaced hardcoded `#ef5350` fallbacks with `var(--moka-color-primary)`.
- **Docs: progress.md** - fixed `Rounded` → `RoundedEnds` parameter name.
- **Docs: forms.md** - fixed `<MokaRadio>` → `<MokaRadioItem>` component name.
- **Docs: theme-switcher.md** - fixed outdated `#121212` → `#060608` dark palette color.

## [0.1.6] - 2026-04-08

### 🐛 Fixed
- **Actually** strips the implicit `Microsoft.AspNetCore.App` `FrameworkReference` from
  packed library nuspecs via a build-time `<Target BeforeTargets="ProcessFrameworkReferences">`
  in `Directory.Build.targets`. The 0.1.5 release intended this fix but only added the
  `Microsoft.AspNetCore.Components.Web` `PackageReference` substitution - the underlying
  `FrameworkReference` was still being implicitly added by the SDK and propagated into
  the published nuspecs, so consumers PackageReferencing Moka.Red from a Blazor WebAssembly
  app still hit `NETSDK1082` ("no runtime pack for browser-wasm"). Verified by inspecting
  `Moka.Red.0.1.6.nupkg`'s nuspec - no `<frameworkReferences>` group present.

### 🔧 Changed
- `docs/mokadocs.yaml` - drops the explicit `previewHost`, `references`, and
  `stylesheets` plugin options in favor of the new `library: Moka.Red@0.1.6` shape.
  The mokadocs-blazor-preview plugin (v3.x) auto-discovers / scaffolds / publishes
  the docs preview-host project transparently. Removes the cross-repo path that was
  pointing at the sibling `Moka.BlazorRepl` repo.
- Deletes `nuget.config` - restores default behavior of using only the global
  `nuget.org` source.

## [0.1.5] - 2026-04-05

### 🐛 Fixed
- Replaced `FrameworkReference` to `Microsoft.AspNetCore.App` with `PackageReference` to `Microsoft.AspNetCore.Components.Web` across all 12 library projects - fixes NETSDK1082 errors when consumers reference Moka.Red from Blazor WebAssembly projects

### 🔧 Changed
- Removed WASM workaround from `Directory.Build.targets` and WasmApp sample - no longer needed

## [0.1.4] - 2026-04-05

### ✨ New Components (55+)
- **Motion**: MokaFadeIn, MokaSlideIn, MokaScaleIn, MokaStagger, MokaTypewriter - CSS animation wrappers
- **Forms**: MokaCalendar, MokaColorWheel, MokaColorInput, MokaDateRangePicker, MokaInputGroup, MokaKnob, MokaIpAddressInput (IPv4+IPv6), MokaMacAddressInput, MokaPinInput, MokaPasswordStrength, MokaSchedulePicker, MokaToggleGroup, MokaTreeSelect, MokaFormBuilder (visual form designer with JSON/Razor export)
- **Primitives**: MokaTag, MokaSteps, MokaCodeBlock, MokaConfetti, MokaDataList, MokaFloatingActionButton, MokaGauge, MokaInfiniteCarousel, MokaKanbanBoard, MokaKeyValue, MokaLogViewer, MokaMarquee, MokaMeter, MokaNotice, MokaNumberTicker, MokaOrganizationChart, MokaParallax, MokaReveal, MokaSplitButton, MokaSwipeActions, MokaTerminal, MokaThemeSwitcher, MokaTransferList
- **Feedback**: MokaCookieConsent, MokaDrawer, MokaHoverCard, MokaOnboarding, MokaWizard, MokaNotificationBell
- **Layout**: MokaSplitPane
- **Navigation**: MokaCommandBar
- **Data**: MokaInfiniteScroll
- **Diagnostics**: NetworkPanel, ComponentTreePanel, MemoryPanel (3 new overlay panels, 10 total)

### 🏗️ Architecture
- `MokaSegmentedInputBase` - shared base class for OTP, PIN, IPv4, IPv6, MAC inputs
- `MokaVisualComponentBase` properties (Size, Variant, Color, Disabled, SizeValue) now `virtual` - subclasses use `override` for different defaults
- CssBuilder/StyleBuilder use inline `string[8]` array instead of `List<string>` - zero container allocation
- `MokaEnumHelpers.ToCssClass<TEnum>` cached via `ConcurrentDictionary` - zero allocation after warmup
- `GetJsModuleAsync` thread-safe via `SemaphoreSlim` double-checked locking
- `_parametersChanged` marked `volatile` for thread safety
- Shadow strings extracted as `const` fields - compile-time interned

### 🎨 Theming
- `ToCssVariables()` now generates ALL 90+ tokens (was 66) - added transitions, heights, z-index, semantic state tokens
- Theme CSS injected at `:root` via `<style>` tag - external libraries can now consume `--moka-*` tokens
- `.moka-dark` CSS overrides in `moka.css` for pure-CSS dark mode targeting
- Fluent `With*` API: `MokaTheme.Light.WithPrimary("#1976d2")`, `.WithFontFamily()`, `.WithDark()`
- `AutoDetectColorScheme` parameter on `MokaThemeProvider` - auto-detects OS `prefers-color-scheme`
- ThemeGen live preview fixed - now uses inline style instead of nested `MokaThemeProvider`

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
- `MokaThemeProvider` auto-injects `moka.css` via `<HeadContent>` - zero manual CSS links
- Merged `moka-reset.css` + `moka-tokens.css` + `moka-text.css` into single `moka.css`
- Blazor WebAssembly sample app proving WASM compatibility

### 🐛 Fixed
- `MokaSegmentedControl` both tabs showing active - removed `IsFixed="true"` from `CascadingValue`
- `MokaTag` runtime `InvalidOperationException` - removed `new` on `[Parameter]`, use `override`
- `MokaThemeProvider` `IsFixed="true"` prevented theme changes from cascading
- `MokaTooltip` missing `CssClass` override - `Class` parameter wasn't applied
- `MokaDialog`, `MokaAccordionItem`, `MokaRadioItem` using private CSS class instead of `CssClass` override
- `MokaProgress` changed to `MokaVisualComponentBase`, renamed `Rounded` to `RoundedEnds`
- Missing `ShouldRender()` on MokaKanbanBoard, MokaAlert, MokaTreeSelect
- FAB demo floating over viewport - use `position:absolute` in container
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

