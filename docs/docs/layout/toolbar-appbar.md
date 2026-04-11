---
title: Toolbar, AppBar & StatusBar
description: Horizontal action bars for toolbars, app navigation headers, and status footers.
order: 22
---

# Toolbar, AppBar & StatusBar

Moka.Red provides three horizontal bar components: `MokaToolbar` for action/control bars, `MokaAppBar` for top navigation headers, and `MokaStatusBar` for bottom status footers.

---

## MokaToolbar

A horizontal bar of actions and controls suitable for rich text editors, action bars, or any row of grouped controls. Flows in-document (not fixed).

### MokaToolbar Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Toolbar content (buttons, selects, dividers) |
| `Dense` | `bool` | `true` | Compact height (32px) |
| `Bordered` | `bool` | `true` | Border around the toolbar |
| `Elevated` | `bool` | `false` | Elevation shadow |
| `Rounded` | `MokaRounding?` | `Md` | Border radius |
| `Wrap` | `bool` | `false` | Allow items to wrap to next line |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaToolbarDivider

A vertical separator between toolbar sections. No parameters.

### MokaToolbarGroup Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Group content |
| `Label` | `string?` | -- | Optional label above the group |

### Basic Toolbar

```blazor-preview
<MokaToolbar>
    <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Save" />
    <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Content.Copy" />
    <MokaToolbarDivider />
    <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Edit" />
    <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Delete" />
</MokaToolbar>
```

### Grouped Toolbar

```blazor-preview
<MokaToolbar>
    <MokaToolbarGroup Label="File">
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Save" />
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Download" />
    </MokaToolbarGroup>
    <MokaToolbarDivider />
    <MokaToolbarGroup Label="Edit">
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Edit" />
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Content.Copy" />
    </MokaToolbarGroup>
</MokaToolbar>
```

### Elevated Toolbar

```blazor-preview
<MokaToolbar Elevated Bordered="false">
    <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Soft">Action A</MokaButton>
    <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Soft">Action B</MokaButton>
</MokaToolbar>
```

---

## MokaAppBar

Top navigation bar with title, start/end content areas, and optional fixed positioning.

### Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Custom center content |
| `Title` | `string?` | -- | Title text (ignored if `TitleContent` is set) |
| `TitleContent` | `RenderFragment?` | -- | Rich title content |
| `StartContent` | `RenderFragment?` | -- | Left side content (menu button, logo) |
| `EndContent` | `RenderFragment?` | -- | Right side content (actions, avatar) |
| `Elevated` | `bool` | `true` | Elevation shadow |
| `Bordered` | `bool` | `false` | Bottom border |
| `Fixed` | `bool` | `false` | Sticky positioning at top |
| `Dense` | `bool` | `true` | Compact height |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### Basic AppBar

```blazor-preview
<MokaAppBar Title="My Application">
    <StartContent>
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Navigation.Menu" />
    </StartContent>
    <EndContent>
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Settings" />
        <MokaAvatar Initials="JD" Size="MokaSize.Sm" />
    </EndContent>
</MokaAppBar>
```

### Bordered (No Elevation)

```blazor-preview
<MokaAppBar Title="Flat AppBar" Elevated="false" Bordered>
    <EndContent>
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Outlined">Sign In</MokaButton>
    </EndContent>
</MokaAppBar>
```

---

## MokaStatusBar

Bottom status bar similar to VS Code's status bar. Fixed to the viewport bottom with compact height and small font. Supports start (left) and end (right) content areas.

### MokaStatusBar Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Generic status bar content |
| `StartContent` | `RenderFragment?` | -- | Left-aligned items |
| `EndContent` | `RenderFragment?` | -- | Right-aligned items |
| `Bordered` | `bool` | `true` | Top border |
| `Class` | `string?` | -- | Additional CSS classes |

### MokaStatusBarItem Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Custom content (overrides `Text`/`Icon`) |
| `Text` | `string?` | -- | Display text |
| `Icon` | `MokaIconDefinition?` | -- | Icon before the text |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Click handler (makes item interactive) |
| `Tooltip` | `string?` | -- | Tooltip on hover |
| `Class` | `string?` | -- | Additional CSS classes |

### Basic StatusBar

```blazor-preview
<MokaStatusBar>
    <StartContent>
        <MokaStatusBarItem Icon="MokaIcons.Status.CheckCircle" Text="Ready" />
        <MokaStatusBarItem Text="main" Icon="MokaIcons.File.Code" />
    </StartContent>
    <EndContent>
        <MokaStatusBarItem Text="UTF-8" />
        <MokaStatusBarItem Text="Ln 42, Col 8" />
    </EndContent>
</MokaStatusBar>
```

### Interactive Items

```blazor-preview
@code {
    string _encoding = "UTF-8";
}
<MokaStatusBar>
    <StartContent>
        <MokaStatusBarItem Text="Connected" Icon="MokaIcons.Status.CheckCircle" />
    </StartContent>
    <EndContent>
        <MokaStatusBarItem Text="@_encoding" OnClick="@(() => _encoding = _encoding == "UTF-8" ? "ASCII" : "UTF-8")" Tooltip="Click to toggle encoding" />
    </EndContent>
</MokaStatusBar>
```
