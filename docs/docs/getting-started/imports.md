---
title: Namespace Imports
description: Recommended _Imports.razor configuration for Moka.Red consumers
order: 4
---

# Namespace Imports

Blazor uses `_Imports.razor` files to declare namespaces that are automatically available in all `.razor` files within the same directory (and subdirectories). Adding Moka.Red namespaces here means you won't need per-file `@using` statements.

Place or update the `_Imports.razor` file in your project's root component directory (e.g., `Components/_Imports.razor` in a Blazor Web App).

## Full import set (meta-package)

If you reference the **Moka.Red** meta-package, copy this block into your `_Imports.razor`:

```razor
@* ── Core: base classes, theming, enums, icons, utilities ── *@
@using Moka.Red.Core.Base
@using Moka.Red.Core.Enums
@using Moka.Red.Core.Extensions
@using Moka.Red.Core.Icons
@using Moka.Red.Core.Layout
@using Moka.Red.Core.Theming
@using Moka.Red.Core.Utilities
@using Moka.Red.Icons

@* ── Primitives: buttons, typography, avatars, badges, chips, etc. ── *@
@using Moka.Red.Primitives.Attribute
@using Moka.Red.Primitives.Avatar
@using Moka.Red.Primitives.Badge
@using Moka.Red.Primitives.Barcode
@using Moka.Red.Primitives.Button
@using Moka.Red.Primitives.Callout
@using Moka.Red.Primitives.Carousel
@using Moka.Red.Primitives.Chat
@using Moka.Red.Primitives.Chip
@using Moka.Red.Primitives.ColorSwatch
@using Moka.Red.Primitives.Countdown
@using Moka.Red.Primitives.Icon
@using Moka.Red.Primitives.Identicon
@using Moka.Red.Primitives.Image
@using Moka.Red.Primitives.Kanban
@using Moka.Red.Primitives.KeyValue
@using Moka.Red.Primitives.List
@using Moka.Red.Primitives.Media
@using Moka.Red.Primitives.Price
@using Moka.Red.Primitives.QRCode
@using Moka.Red.Primitives.Ribbon
@using Moka.Red.Primitives.SegmentedControl
@using Moka.Red.Primitives.Meter
@using Moka.Red.Primitives.Sortable
@using Moka.Red.Primitives.Stat
@using Moka.Red.Primitives.Steps
@using Moka.Red.Primitives.Timeline
@using Moka.Red.Primitives.Tag
@using Moka.Red.Primitives.Tree
@using Moka.Red.Primitives.Typography
@using Moka.Red.Primitives.TransferList
@using Moka.Red.Primitives.DataList
@using Moka.Red.Primitives.Fab
@using Moka.Red.Primitives.Marquee
@using Moka.Red.Primitives.NumberTicker
@using Moka.Red.Primitives.Gauge
@using Moka.Red.Primitives.CodeBlock
@using Moka.Red.Primitives.Confetti
@using Moka.Red.Primitives.SplitButton
@using Moka.Red.Primitives.SwipeActions
@using Moka.Red.Primitives.Notice
@using Moka.Red.Primitives.Terminal
@using Moka.Red.Primitives.Motion
@using Moka.Red.Primitives.OrgChart
@using Moka.Red.Primitives.InfiniteCarousel
@using Moka.Red.Primitives.Parallax
@using Moka.Red.Primitives.Reveal
@using Moka.Red.Primitives.ThemeSwitcher
@using Moka.Red.Primitives.LogViewer
@using Moka.Red.Primitives.Utility

@* ── Layout: grid, flexbox, card, panel, accordion, dock, etc. ── *@
@using Moka.Red.Layout.Accordion
@using Moka.Red.Layout.AppBar
@using Moka.Red.Layout.AspectRatio
@using Moka.Red.Layout.Card
@using Moka.Red.Layout.Container
@using Moka.Red.Layout.Divider
@using Moka.Red.Layout.DockLayout
@using Moka.Red.Layout.Flexbox
@using Moka.Red.Layout.Footer
@using Moka.Red.Layout.Grid
@using Moka.Red.Layout.Panel
@using Moka.Red.Layout.Paper
@using Moka.Red.Layout.Resizable
@using Moka.Red.Layout.ScrollArea
@using Moka.Red.Layout.Spacer
@using Moka.Red.Layout.StatusBar
@using Moka.Red.Layout.Sticky
@using Moka.Red.Layout.Toolbar
@using Moka.Red.Layout.SplitPane
@using Moka.Red.Layout.Watermark

@* ── Forms: text fields, selects, date/time pickers, sliders, etc. ── *@
@using Moka.Red.Forms.Base
@using Moka.Red.Forms.AutoComplete
@using Moka.Red.Forms.Calendar
@using Moka.Red.Forms.Checkbox
@using Moka.Red.Forms.ColorPicker
@using Moka.Red.Forms.CreditCard
@using Moka.Red.Forms.CurrencyInput
@using Moka.Red.Forms.DatePicker
@using Moka.Red.Forms.FileUpload
@using Moka.Red.Forms.InputGroup
@using Moka.Red.Forms.NumericField
@using Moka.Red.Forms.OtpInput
@using Moka.Red.Forms.PasswordField
@using Moka.Red.Forms.PhoneInput
@using Moka.Red.Forms.RadioGroup
@using Moka.Red.Forms.Rating
@using Moka.Red.Forms.SearchInput
@using Moka.Red.Forms.SelectField
@using Moka.Red.Forms.SignaturePad
@using Moka.Red.Forms.Slider
@using Moka.Red.Forms.Switch
@using Moka.Red.Forms.TagInput
@using Moka.Red.Forms.TextArea
@using Moka.Red.Forms.TextField
@using Moka.Red.Forms.TimePicker
@using Moka.Red.Forms.TreeSelect
@using Moka.Red.Forms.ColorInput
@using Moka.Red.Forms.ColorWheel
@using Moka.Red.Forms.DateRangePicker
@using Moka.Red.Forms.PasswordStrength
@using Moka.Red.Forms.PinInput
@using Moka.Red.Forms.ToggleGroup
@using Moka.Red.Forms.FormBuilder
@using Moka.Red.Forms.Knob
@using Moka.Red.Forms.IpAddressInput
@using Moka.Red.Forms.MacAddressInput
@using Moka.Red.Forms.SchedulePicker

@* ── Feedback: dialogs, toasts, tooltips, popovers, loading, etc. ── *@
@using Moka.Red.Feedback.Alert
@using Moka.Red.Feedback.BottomSheet
@using Moka.Red.Feedback.CommandPalette
@using Moka.Red.Feedback.Dialog
@using Moka.Red.Feedback.Drawer
@using Moka.Red.Feedback.Dropdown
@using Moka.Red.Feedback.EmptyState
@using Moka.Red.Feedback.Extensions
@using Moka.Red.Feedback.Loading
@using Moka.Red.Feedback.Notification
@using Moka.Red.Feedback.Popover
@using Moka.Red.Feedback.Progress
@using Moka.Red.Feedback.Toast
@using Moka.Red.Feedback.HoverCard
@using Moka.Red.Feedback.Tooltip
@using Moka.Red.Feedback.Wizard
@using Moka.Red.Feedback.CookieConsent
@using Moka.Red.Feedback.NotificationBell
@using Moka.Red.Feedback.Onboarding

@* ── Data: table, pagination, virtual list ── *@
@using Moka.Red.Data.Table
@using Moka.Red.Data.Pagination
@using Moka.Red.Data.InfiniteScroll
@using Moka.Red.Data.VirtualList

@* ── Navigation: menu, breadcrumb, sidebar, stepper, tabs ── *@
@using Moka.Red.Navigation.Breadcrumb
@using Moka.Red.Navigation.Menu
@using Moka.Red.Navigation.Sidebar
@using Moka.Red.Navigation.Stepper
@using Moka.Red.Navigation.CommandBar

@* ── Context Menu ── *@
@using Moka.Red.ContextMenu
```

## Minimal import set (individual packages)

If you only reference specific Moka.Red packages, include only the groups you need. At minimum, **Core** and **Icons** are always required:

```razor
@* ── Always required ── *@
@using Moka.Red.Core.Base
@using Moka.Red.Core.Enums
@using Moka.Red.Core.Theming
@using Moka.Red.Core.Utilities
@using Moka.Red.Icons
```

Then add the sections that correspond to the packages you installed. For example, if you use `Moka.Red.Primitives` and `Moka.Red.Layout`:

```razor
@* ── Primitives (pick the component namespaces you use) ── *@
@using Moka.Red.Core.Icons
@using Moka.Red.Primitives.Button
@using Moka.Red.Primitives.Icon
@using Moka.Red.Primitives.Typography

@* ── Layout ── *@
@using Moka.Red.Layout.Card
@using Moka.Red.Layout.Flexbox
@using Moka.Red.Layout.Grid
```

## What each namespace provides

| Namespace | Contents |
|-----------|----------|
| `Moka.Red.Core.Base` | `MokaComponentBase`, `MokaVisualComponentBase` -- base classes all components inherit from |
| `Moka.Red.Core.Enums` | `MokaSize`, `MokaColor`, `MokaVariant`, `MokaDirection`, `MokaRounding`, `MokaSpacingScale`, and other shared enums used as component parameters |
| `Moka.Red.Core.Extensions` | `AddMokaRed()` service registration extension method |
| `Moka.Red.Core.Icons` | `MokaIconDefinition` struct for defining custom icons |
| `Moka.Red.Core.Layout` | `MokaBreakpoint` record for responsive overrides |
| `Moka.Red.Core.Theming` | `MokaTheme`, `MokaThemeProvider`, `MokaPalette`, `MokaTypography`, `MokaSpacing` |
| `Moka.Red.Core.Utilities` | `CssBuilder`, `StyleBuilder` for composing CSS classes and inline styles |
| `Moka.Red.Icons` | `MokaIcons` static class with 59 built-in SVG icon definitions (Action, Navigation, Status, Content, Toggle, File) |
| `Moka.Red.Primitives.*` | Individual component namespaces: Button, Icon, Typography, Avatar, Badge, Chip, Callout, List, Image, Kanban, KeyValue, Meter, Steps, Tag, Terminal, TransferList, DataList, Fab, Marquee, NumberTicker, Gauge, Confetti, CodeBlock, SwipeActions, etc. |
| `Moka.Red.Layout.*` | Grid, Flexbox, Card, Paper, Panel, Accordion, AppBar, Toolbar, Container, Divider, DockLayout, SplitPane, etc. |
| `Moka.Red.Forms.Base` | `MokaToggleBase`, `MokaTextInputBase`, `MokaSelectBase` -- needed when referencing base class types (e.g., `MokaToggleBase.LabelPosition`) |
| `Moka.Red.Forms.*` | TextField, PasswordField, NumericField, Checkbox, Switch, SelectField, DatePicker, TimePicker, ColorPicker, ColorWheel, ColorInput, TreeSelect, PinInput, PasswordStrength, Slider, Rating, Calendar, InputGroup, FormBuilder, etc. |
| `Moka.Red.Feedback.*` | Alert, Dialog, Drawer, Toast, Tooltip, Popover, Dropdown, Progress, Loading (Spinner/Skeleton/Overlay), CommandPalette, Notification, EmptyState, BottomSheet, Wizard, CookieConsent, Onboarding |
| `Moka.Red.Feedback.Extensions` | `AddMokaFeedback()` service registration for dialog and toast services |
| `Moka.Red.Data.*` | `MokaTable<TItem>`, `MokaPagination`, `MokaVirtualList<TItem>`, `MokaInfiniteScroll` |
| `Moka.Red.Navigation.*` | Menu, Breadcrumb, Sidebar, Stepper, CommandBar |
| `Moka.Red.ContextMenu` | `MokaContextMenu`, `MokaContextMenuTrigger`, `MokaContextMenuItem` |

> **Tip:** The `Moka.Red.Forms.Base` namespace is only needed if your code references base-class types directly, such as `MokaToggleBase.LabelPosition` for checkbox/switch label placement. Most consumers can skip it.
