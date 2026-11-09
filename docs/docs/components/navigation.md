---
title: Navigation
description: Menu, Breadcrumb, and Stepper components for application navigation.
order: 29
---

# Navigation

Moka.Red provides three navigation components: `MokaMenu` for sidebar/vertical navigation, `MokaBreadcrumb` for location trails, and `MokaStepper` for multi-step workflows.

---

## MokaMenu

A vertical navigation menu with support for nested items, icons, badges, and collapsible groups.

### MokaMenu Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Menu items (`MokaMenuItem`, `MokaMenuDivider`) |
| `Collapsed` | `bool` | `false` | Shows only icons (sidebar collapsed mode) |
| `Dense` | `bool` | `true` | Compact spacing |
| `Bordered` | `bool` | `false` | Border around the menu |
| `Adaptive` | `bool` | `false` | Draws every item for rows and the icon rail alike and leaves the choice to CSS: the rail inside an element with a `data-moka-rail` attribute, and taller rows on a phone. [`MokaAppShell`](app-shell.md) uses it, since its menu changes before .NET can say so. Ignored while `Collapsed` |
| `RailLabels` | `bool` | `false` | With `Adaptive`, the rail shows each item's name under its icon, cut to one line, and an item's tag shows only for a pill or a cut name. `MokaAppShell` marks the cut names with `data-moka-cut` on the item's tooltip |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Space around the menu. It still fills its container, with the margin inside |
| `Class` | `string?` | -- | Additional CSS classes |

The menu fills the width of its container unless you give it a width, as the examples below do with `style`. Up to 0.1.12 it was `width: 100%`, so a margin made it wider than the container.

### MokaMenuItem Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Nested child menu items (sub-menu). Makes the item a button that expands and collapses them |
| `Text` | `string?` | -- | Display text |
| `Icon` | `MokaIconDefinition?` | -- | Icon before the text |
| `ImageSource` | `string?` | -- | A picture in place of `Icon`, such as a plugin's own icon: a URL or a `data:image/...` URI, drawn as an `img` in the icon's box with the item's text naming it. Only relative addresses, `http`, `https`, `blob` and image `data:` URIs are used. Anything else, or a picture that fails to load, shows `Icon` instead. The current item gets a ring round the picture, since a picture can't take the icon's color |
| `Href` | `string?` | -- | Navigation link (renders as anchor). Ignored on an item with children. A `javascript:`, `vbscript:` or `data:` URL is not rendered: the item is not a link |
| `Active` | `bool` | `false` | Highlighted as active route. A link reports it with `aria-current="page"` |
| `Match` | `NavLinkMatch?` | -- | Works out `Active` from the address, as `NavLink` does, and follows it as the person moves around. `All` is the page itself, `Prefix` the page and anything under it, where a prefix ends at a `/`, so `theme` doesn't light for `theme-editor`. The query and fragment never count, as with `NavLink` on .NET 10. Left out, `Active` alone decides, and the item doesn't listen for page changes. With it, `Active` can still light the item. Links only |
| `Expanded` | `bool` | `false` | Sub-menu expanded state (two-way bindable). A one-way value applies only when it changes |
| `ExpandedChanged` | `EventCallback<bool>` | -- | Callback when expanded changes |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Click handler. Enter and Space raise it too |
| `OnContextMenu` | `EventCallback<MouseEventArgs>` | -- | Right-click handler. The context-menu key reaches it too |
| `Badge` | `string?` | -- | Badge text to the right (e.g., "3", "New") |
| `BadgeColor` | `MokaColor?` | `Primary` | Badge color |
| `Pill` | `MokaNavPill?` | -- | A count with a meaning: a `MokaBadge` in its tone beside the text, and a corner badge on the icon when the menu shows only icons. Screen readers and the tooltip beside an icon-only menu get its `Why` |
| `Indent` | `int` | `0` | Nesting depth (auto-incremented for nested items) |

### Basic Menu

```blazor-preview
<MokaMenu Bordered style="width:240px">
    <MokaMenuItem Text="Home" Icon="MokaIcons.Navigation.Home" Active />
    <MokaMenuItem Text="Dashboard" Icon="MokaIcons.Action.Settings" />
    <MokaMenuDivider />
    <MokaMenuItem Text="Messages" Icon="MokaIcons.Action.Edit" Badge="5" />
    <MokaMenuItem Text="Settings" Icon="MokaIcons.Action.Settings" />
</MokaMenu>
```

### Nested Menu

```blazor-preview
<MokaMenu Bordered style="width:240px">
    <MokaMenuItem Text="Products" Icon="MokaIcons.Navigation.Menu">
        <MokaMenuItem Text="All Products" />
        <MokaMenuItem Text="Categories" />
        <MokaMenuItem Text="Inventory" />
    </MokaMenuItem>
    <MokaMenuItem Text="Orders" Icon="MokaIcons.Action.Edit" Badge="12" />
    <MokaMenuItem Text="Analytics" Icon="MokaIcons.Status.Info" />
</MokaMenu>
```

### Lighting the Current Page

Give each link a `Match` and it lights itself from the address, the way `NavLink` does. The home page takes `All`, or every address would count as under it.

```razor
@using Microsoft.AspNetCore.Components.Routing

<MokaMenu aria-label="Main">
    <MokaMenuItem Text="Home" Href="./" Match="NavLinkMatch.All" />
    <MokaMenuItem Text="Devices" Href="devices" Match="NavLinkMatch.Prefix" />
    <MokaMenuItem Text="Theme" Href="theme" Match="NavLinkMatch.Prefix" />
    <MokaMenuItem Text="Theme editor" Href="theme-editor" Match="NavLinkMatch.Prefix" />
</MokaMenu>
```

`Devices` stays lit on `devices/tv`, and `Theme` doesn't light on `theme-editor`. A link to a part of a page, like `layout#Card`, matches the whole page, since the fragment never counts. To light one section among several on a page, set `Active` yourself.

### Keyboard and Screen Readers

`MokaMenu` is site navigation, so it is a `<nav>` of links and buttons rather than an ARIA `menu`. Tab moves through the items, Enter follows a link, and Enter or Space press a button. Screen readers read links as links, which the `menuitem` role would hide.

| Item | Renders as |
|------|------------|
| `Href`, no children | A link. With `Active`, or a `Match` that fits the address, it has `aria-current="page"` |
| Children | A disclosure button with `aria-expanded` and `aria-controls` naming its group. A collapsed group is `inert`, so Tab skips its links |
| `OnClick` or `OnContextMenu` | A button |
| None of these | Plain text that takes no focus |

In a `Collapsed` menu the text is hidden, so each item carries its `Text` as `aria-label` and shows it in a `MokaTooltip`
beside the menu, which is a column of icons there; on a menu at the window's right edge the tooltip opens to the left.
An item with nothing to click keeps its text for screen readers as visually hidden text. Up to 0.1.19 the name was a
`title`, the browser's own hint. Give the menu an `aria-label` when a page has more than one navigation landmark:

```razor
<MokaMenu aria-label="Admin">
    <MokaMenuItem Text="Users" Href="/users" Active />
    <MokaMenuItem Text="Reports">
        <MokaMenuItem Text="Monthly" Href="/reports/monthly" />
    </MokaMenuItem>
    <MokaMenuItem Text="Sign out" OnClick="SignOut" />
</MokaMenu>
```

A one-way `Expanded` sets the starting state and applies again only when its value changes, so a group the user opened or closed stays that way when the parent re-renders. Use `@bind-Expanded` to track it.

Up to 0.1.12 an item without `Href` was a `div` with `role="menuitem"` that the keyboard could not reach, a collapsed group kept its links in the tab order, and a parent re-render with a one-way `Expanded` undid the user's toggle.

---

## MokaBreadcrumb

A trail of where the person is, with links back up. The last level is the current page: text with
`aria-current="page"`, never a link.

### MokaBreadcrumb Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | `MokaBreadcrumbItem` elements |
| `Separator` | `string?` | `null` | Text between levels. Null is the look's own: a slash on the original design (`MokaTheme.Dark` and `Light`), a chevron on the new one. A path always takes a slash |
| `SeparatorContent` | `RenderFragment?` | -- | Custom separator (overrides text) |
| `MaxItems` | `int?` | -- | When the trail has more levels than this, the middle ones fold into a menu behind a "···" button. The first level and the last `MaxItems - 1` stay; at 1 only the current page stays. Counts the current page even when `ShowCurrent` is off. Values under 1 count as 1. A path ignores it |
| `ShowCurrent` | `bool` | `true` | Whether the current page shows. Turn it off in a page header, where the title says it |
| `MoreLabel` | `string` | `"Show {n} more levels"` | The "···" button's name and tooltip. `{n}` is the number of folded levels. The default says "level" for one |
| `Fold` | `MokaBreadcrumbFold` | `Never` | Folds the middle on its own in a narrow box: `Narrow` under 400 px (25rem), `Medium` under 560 px (35rem). The first level and the last two stay |
| `ItemMaxWidth` | `string` | `"12rem"` | How wide one name may get on the new design before it's cut, with the whole name in a tooltip. Set it to cut names on the original design too |
| `Look` | `MokaBreadcrumbLook` | `Trail` | `Trail`, or `Path`: a mono path in a well, for technical apps |
| `Root` | `RenderFragment?` | -- | Content before the first level, such as a host or an environment chip |
| `Copyable` | `bool` | `false` | A copy button at the end, in `MokaCopyButton`'s quiet look |
| `CopyText` | `string?` | -- | What the copy button copies. Null joins each level's `Segment`, or its text, with "/" |
| `CopyLabel` | `string` | `"Copy path"` | The copy button's name and tooltip |
| `Class` | `string?` | -- | Additional CSS classes |

### MokaBreadcrumbItem Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Custom content |
| `Text` | `string?` | -- | Display text |
| `Href` | `string?` | -- | Navigation link. A `javascript:`, `vbscript:` or `data:` URL is not rendered: the item shows as text |
| `Icon` | `MokaIconDefinition?` | -- | Icon before text |
| `IconOnly` | `bool` | `false` | Shows only the icon, named by `Text` through `aria-label` and a tooltip. Made for Home. Needs both an icon and text |
| `Segment` | `string?` | -- | What the level adds to the copied path when it differs from the text, such as a slug or an id. An empty string leaves the level out |

### Basic Breadcrumb

```blazor-preview
<MokaBreadcrumb>
    <MokaBreadcrumbItem Text="Home" Href="/" Icon="MokaIcons.Navigation.Home" />
    <MokaBreadcrumbItem Text="Products" Href="/products" />
    <MokaBreadcrumbItem Text="Electronics" Href="/products/electronics" />
    <MokaBreadcrumbItem Text="Laptops" />
</MokaBreadcrumb>
```

On the new design the links are quiet rather than in the accent, 28 px tall and separated by chevrons. Hover raises a
link with a background instead of changing its color, keyboard focus is a ring, and the current page is a neutral
pill. `MokaTheme.Dark` and `Light` keep accent links and slashes.

### Home as an Icon

```razor
<MokaBreadcrumb>
    <MokaBreadcrumbItem Text="Home" Href="/" Icon="MokaIcons.Navigation.Home" IconOnly />
    <MokaBreadcrumbItem Text="Network" Href="/network" />
    <MokaBreadcrumbItem Text="Office desktop" />
</MokaBreadcrumb>
```

### Over a Page Header

The title already says where the person is, so leave the current page out:

```razor
<MokaBreadcrumb ShowCurrent="false">
    <MokaBreadcrumbItem Text="Home" Href="/" Icon="MokaIcons.Navigation.Home" IconOnly />
    <MokaBreadcrumbItem Text="Network" Href="/network" />
    <MokaBreadcrumbItem Text="Devices" Href="/network/devices" />
    <MokaBreadcrumbItem Text="Office desktop" />
</MokaBreadcrumb>
<MokaPageHeader Title="Office desktop" Said="Online since 08:12" />
```

`MokaPageHeader`'s `Eyebrow` is text, so the breadcrumb goes just above the header rather than in it.

### Folding the Middle

When there are more levels than `MaxItems`, the middle ones fold into a menu behind a "···" button. The menu is a
`MokaDropdown`: the arrows, Home and End move through it, Escape closes it and focus goes back to the button. Each
folded level sits a step further in than its parent.

```razor
<MokaBreadcrumb MaxItems="3">
    <MokaBreadcrumbItem Text="Home" Href="/" />
    <MokaBreadcrumbItem Text="Network" Href="/network" />
    <MokaBreadcrumbItem Text="Home network" Href="/network/home" />
    <MokaBreadcrumbItem Text="Devices" Href="/network/home/devices" />
    <MokaBreadcrumbItem Text="Office desktop" />
</MokaBreadcrumb>
```

`Fold` folds the same way on its own, but only while the breadcrumb's box is narrow. It's a container query, with no
script:

```razor
<MokaBreadcrumb Fold="MokaBreadcrumbFold.Narrow">
    ...
</MokaBreadcrumb>
```

A container query needs a box whose width doesn't come from what's in it, so with `Fold` set, and always on the new
design, the breadcrumb takes the width it's given. In a row sized by its content, such as a grid `auto` column, give it
a width. In a `MokaCommandBar`'s left zone it stays as wide as its trail and doesn't fold.

Up to 0.2.2 `MaxItems` folded the middle into a "..." that did nothing, so the folded levels couldn't be reached, and
`MaxItems="1"` showed the first level and a dangling separator instead of the current page.

### Long Names and Narrow Boxes

On the new design a name wider than `ItemMaxWidth` is cut, with the whole name in a tooltip, and in a box under 240 px,
a phone's column, the trail becomes a link back to the current page's parent. Neither needs a parameter.

### A Path You Can Copy

```razor
<MokaBreadcrumb Look="MokaBreadcrumbLook.Path" Copyable>
    <Root>
        <MokaIcon Icon="MokaIcons.File.Database" SizeValue="14px" />
    </Root>
    <ChildContent>
        <MokaBreadcrumbItem Text="network" Href="/network" />
        <MokaBreadcrumbItem Text="devices" Href="/network/devices" />
        <MokaBreadcrumbItem Text="office-desktop" />
    </ChildContent>
</MokaBreadcrumb>
```

The button copies `network/devices/office-desktop`. A level whose text isn't what the path should hold takes a
`Segment`, and `CopyText` replaces the whole thing. A path keeps every level, whatever `MaxItems` says: one too long for
its well keeps its end in view and scrolls back to its start, and in a browser with scroll timelines the hidden side
fades.

### Levels That Change

The breadcrumb decides where every level goes, so levels can come and go at runtime, under `@key` too, and the current
page, the separators and the folded menu follow:

```razor
<MokaBreadcrumb>
    @foreach (Folder folder in _path)
    {
        <MokaBreadcrumbItem @key="folder.Id" Text="@folder.Name" Href="@folder.Href" />
    }
</MokaBreadcrumb>
```

Up to 0.2.2 each level decided for itself whether it was last, and a level with only text never rendered again: a level
added later left the old last one bold and current beside it, and a removed one left a link with a separator after it.

### Custom Separator

```blazor-preview
<MokaBreadcrumb Separator=">">
    <MokaBreadcrumbItem Text="Home" Href="/" />
    <MokaBreadcrumbItem Text="Docs" Href="/docs" />
    <MokaBreadcrumbItem Text="Components" />
</MokaBreadcrumb>
```

---

## MokaStepper

A step-by-step navigation component that guides users through a multi-step process.

### MokaStepper Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | `MokaStep` elements |
| `ActiveStep` | `int` | `0` | Currently active step index (two-way bindable). A one-way value applies only when it changes |
| `ActiveStepChanged` | `EventCallback<int>` | -- | Callback when the user activates a step |
| `Orientation` | `MokaStepperOrientation` | `Horizontal` | `Horizontal` or `Vertical` |
| `Linear` | `bool` | `false` | Steps must be completed in order: only earlier steps and the next one can be activated |
| `ShowStepNumbers` | `bool` | `true` | Show step numbers in the indicator |

### MokaStep Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Content displayed when the step is active |
| `Title` | `string?` | -- | Step title |
| `Subtitle` | `string?` | -- | Step subtitle |
| `Icon` | `MokaIconDefinition?` | -- | Custom icon (overrides step number) |
| `Completed` | `bool` | `false` | Shows a checkmark |
| `HasError` | `bool` | `false` | Shows error state |
| `Optional` | `bool` | `false` | Shows "Optional" label |
| `Disabled` | `bool` | `false` | Step cannot be clicked or activated, and Tab skips it |

### Horizontal Stepper

```blazor-preview
@code {
    int _step = 1;
}
<MokaStepper @bind-ActiveStep="_step">
    <MokaStep Title="Account" Subtitle="Create your account" Completed />
    <MokaStep Title="Profile" Subtitle="Set up your profile" />
    <MokaStep Title="Review" Subtitle="Confirm details" />
</MokaStepper>
```

### Vertical Stepper

```blazor-preview
@code {
    int _vstep = 0;
}
<MokaStepper @bind-ActiveStep="_vstep" Orientation="MokaStepperOrientation.Vertical">
    <MokaStep Title="Select Plan">
        <p>Choose a subscription plan that fits your needs.</p>
    </MokaStep>
    <MokaStep Title="Payment">
        <p>Enter your payment details.</p>
    </MokaStep>
    <MokaStep Title="Confirmation" Optional>
        <p>Review and confirm your order.</p>
    </MokaStep>
</MokaStepper>
```

### With Error State

```blazor-preview
<MokaStepper ActiveStep="1">
    <MokaStep Title="Upload" Completed />
    <MokaStep Title="Validate" HasError />
    <MokaStep Title="Process" />
</MokaStepper>
```

### Linear Stepper

When `Linear` is set, users cannot skip ahead to incomplete steps.

```blazor-preview
@code {
    int _linear = 0;
}
<MokaStepper @bind-ActiveStep="_linear" Linear>
    <MokaStep Title="Step 1" />
    <MokaStep Title="Step 2" />
    <MokaStep Title="Step 3" />
</MokaStepper>
```

### Keyboard and Screen Readers

The stepper is a list of steps, and each step's header is a button. Tab moves between the steps and Enter or Space activates one. A step that cannot be activated, because it is `Disabled` or lies beyond the next step of a `Linear` stepper, is a disabled button that Tab skips.

- The active step's button has `aria-current="step"`.
- The check and error icons are decorative, so a completed step is described as "Completed" and a step with `HasError` as "Error".
- A mouse click anywhere on a step activates it, the connector line included.

A one-way `ActiveStep` sets the starting step and applies again only when its value changes, so a step the user picked stays active when the parent re-renders. Use `@bind-ActiveStep` to track it.

Up to 0.1.12 the steps took no focus, a step whose own parameters had not changed kept showing the old active step, and a parent re-render with a one-way `ActiveStep` undid the user's choice.
