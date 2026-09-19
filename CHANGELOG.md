# Changelog

All notable changes to Moka.Red will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

