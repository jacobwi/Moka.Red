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
| `Class` | `string?` | -- | Additional CSS classes |

### MokaMenuItem Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Nested child menu items (sub-menu) |
| `Text` | `string?` | -- | Display text |
| `Icon` | `MokaIconDefinition?` | -- | Icon before the text |
| `Href` | `string?` | -- | Navigation link (renders as anchor) |
| `Active` | `bool` | `false` | Highlighted as active route |
| `Expanded` | `bool` | `false` | Sub-menu expanded state (two-way bindable) |
| `ExpandedChanged` | `EventCallback<bool>` | -- | Callback when expanded changes |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Click handler |
| `Badge` | `string?` | -- | Badge text to the right (e.g., "3", "New") |
| `BadgeColor` | `MokaColor?` | `Primary` | Badge color |
| `Indent` | `int` | `0` | Nesting depth (auto-incremented for nested items) |

### Basic Menu

```blazor-preview
<MokaMenu Bordered style="width:240px">
    <MokaMenuItem Text="Home" Icon="MokaIcons.Navigation.Home" Active />
    <MokaMenuItem Text="Dashboard" Icon="MokaIcons.Action.Settings" />
    <MokaMenuDivider />
    <MokaMenuItem Text="Messages" Icon="MokaIcons.Content.Edit" Badge="5" />
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
    <MokaMenuItem Text="Orders" Icon="MokaIcons.Content.Edit" Badge="12" />
    <MokaMenuItem Text="Analytics" Icon="MokaIcons.Status.Info" />
</MokaMenu>
```

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
| `Href` | `string?` | -- | Navigation link |
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
| `ActiveStep` | `int` | `0` | Currently active step index (two-way bindable) |
| `ActiveStepChanged` | `EventCallback<int>` | -- | Callback when active step changes |
| `Orientation` | `MokaStepperOrientation` | `Horizontal` | `Horizontal` or `Vertical` |
| `Linear` | `bool` | `false` | Steps must be completed in order |
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
| `Disabled` | `bool` | `false` | Step cannot be clicked |

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
