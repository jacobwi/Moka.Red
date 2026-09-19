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
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Space around the menu. It still fills its container, with the margin inside |
| `Class` | `string?` | -- | Additional CSS classes |

The menu fills the width of its container unless you give it a width, as the examples below do with `style`. Up to 0.1.12 it was `width: 100%`, so a margin made it wider than the container.

### MokaMenuItem Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Nested child menu items (sub-menu). Makes the item a button that expands and collapses them |
| `Text` | `string?` | -- | Display text |
| `Icon` | `MokaIconDefinition?` | -- | Icon before the text |
| `Href` | `string?` | -- | Navigation link (renders as anchor). Ignored on an item with children. A `javascript:`, `vbscript:` or `data:` URL is not rendered: the item is not a link |
| `Active` | `bool` | `false` | Highlighted as active route. A link reports it with `aria-current="page"` |
| `Expanded` | `bool` | `false` | Sub-menu expanded state (two-way bindable). A one-way value applies only when it changes |
| `ExpandedChanged` | `EventCallback<bool>` | -- | Callback when expanded changes |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Click handler. Enter and Space raise it too |
| `OnContextMenu` | `EventCallback<MouseEventArgs>` | -- | Right-click handler. The context-menu key reaches it too |
| `Badge` | `string?` | -- | Badge text to the right (e.g., "3", "New") |
| `BadgeColor` | `MokaColor?` | `Primary` | Badge color |
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

### Keyboard and Screen Readers

`MokaMenu` is site navigation, so it is a `<nav>` of links and buttons rather than an ARIA `menu`. Tab moves through the items, Enter follows a link, and Enter or Space press a button. Screen readers read links as links, which the `menuitem` role would hide.

| Item | Renders as |
|------|------------|
| `Href`, no children | A link. With `Active` it has `aria-current="page"` |
| Children | A disclosure button with `aria-expanded` and `aria-controls` naming its group. A collapsed group is `inert`, so Tab skips its links |
| `OnClick` or `OnContextMenu` | A button |
| None of these | Plain text that takes no focus |

In a `Collapsed` menu the text is hidden, so each item carries its `Text` as `aria-label` and `title`. Give the menu an `aria-label` when a page has more than one navigation landmark:

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

A breadcrumb navigation trail. The last item is rendered as plain text (current page).

### MokaBreadcrumb Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | `MokaBreadcrumbItem` elements |
| `Separator` | `string` | `"/"` | Text separator between items |
| `SeparatorContent` | `RenderFragment?` | -- | Custom separator (overrides text) |
| `MaxItems` | `int?` | -- | Collapses middle items with ellipsis when exceeded |
| `Class` | `string?` | -- | Additional CSS classes |

### MokaBreadcrumbItem Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Custom content |
| `Text` | `string?` | -- | Display text |
| `Href` | `string?` | -- | Navigation link. A `javascript:`, `vbscript:` or `data:` URL is not rendered: the item shows as text |
| `Icon` | `MokaIconDefinition?` | -- | Icon before text |

### Basic Breadcrumb

```blazor-preview
<MokaBreadcrumb>
    <MokaBreadcrumbItem Text="Home" Href="/" Icon="MokaIcons.Navigation.Home" />
    <MokaBreadcrumbItem Text="Products" Href="/products" />
    <MokaBreadcrumbItem Text="Electronics" Href="/products/electronics" />
    <MokaBreadcrumbItem Text="Laptops" />
</MokaBreadcrumb>
```

### Max Items with Ellipsis

When the item count exceeds `MaxItems`, middle items are collapsed with an ellipsis.

```blazor-preview
<MokaBreadcrumb MaxItems="3">
    <MokaBreadcrumbItem Text="Home" Href="/" />
    <MokaBreadcrumbItem Text="Category" Href="/cat" />
    <MokaBreadcrumbItem Text="Subcategory" Href="/cat/sub" />
    <MokaBreadcrumbItem Text="Product" Href="/cat/sub/product" />
    <MokaBreadcrumbItem Text="Details" />
</MokaBreadcrumb>
```

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
