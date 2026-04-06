---
title: Panel & Paper
description: Content surface components with elevation, borders, and collapsible sections.
order: 21
---

# Panel & Paper

`MokaPaper` is the simplest surface component -- a styled div with elevation and background. `MokaPanel` adds a titled header with optional toolbar actions and collapsible body.

---

## MokaPaper

A surface container with configurable elevation (box shadow depth) or outlined border.

### Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Content inside the paper |
| `Elevation` | `int` | `0` | Box shadow depth, 0--4 |
| `Outlined` | `bool` | `false` | Border instead of box shadow |
| `Rounded` | `MokaRounding?` | `Md` | Border radius (`None`, `Sm`, `Md`, `Lg`, `Xl`, `Full`) |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### Elevation Levels

```blazor-preview
<div style="display:flex;gap:16px;flex-wrap:wrap">
    <MokaPaper Elevation="0" Style="padding:16px;width:100px;text-align:center">Flat</MokaPaper>
    <MokaPaper Elevation="1" Style="padding:16px;width:100px;text-align:center">1</MokaPaper>
    <MokaPaper Elevation="2" Style="padding:16px;width:100px;text-align:center">2</MokaPaper>
    <MokaPaper Elevation="3" Style="padding:16px;width:100px;text-align:center">3</MokaPaper>
    <MokaPaper Elevation="4" Style="padding:16px;width:100px;text-align:center">4</MokaPaper>
</div>
```

### Outlined

```blazor-preview
<MokaPaper Outlined Style="padding:16px">
    Outlined paper with border instead of shadow.
</MokaPaper>
```

### Square

```blazor-preview
<MokaPaper Elevation="2" Rounded="MokaRounding.None" Style="padding:16px">
    Square corners (no border radius).
</MokaPaper>
```

---

## MokaPanel

A titled content panel with optional toolbar actions and collapsible body.

### Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Panel body content |
| `Title` | `string?` | -- | Header title text |
| `TitleContent` | `RenderFragment?` | -- | Custom header content (overrides `Title`) |
| `Actions` | `RenderFragment?` | -- | Toolbar actions in the header (right-aligned) |
| `Collapsible` | `bool` | `false` | Body can be collapsed |
| `Collapsed` | `bool` | `false` | Whether the body is collapsed (two-way bindable) |
| `CollapsedChanged` | `EventCallback<bool>` | -- | Callback when collapsed state changes |
| `Bordered` | `bool` | `true` | Border around the panel |
| `Elevated` | `bool` | `false` | Elevation shadow instead of border |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### Basic Panel

```blazor-preview
<MokaPanel Title="Details">
    <p>Panel body content goes here.</p>
</MokaPanel>
```

### With Actions

```blazor-preview
<MokaPanel Title="Users">
    <Actions>
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Outlined" StartIcon="MokaIcons.Action.Add">
            Add User
        </MokaButton>
    </Actions>
    <ChildContent>
        <p>User list content here.</p>
    </ChildContent>
</MokaPanel>
```

### Collapsible

```blazor-preview
<MokaPanel Title="Collapsible Section" Collapsible>
    <p>This content can be collapsed by clicking the toggle in the header.</p>
</MokaPanel>
```

### Elevated

```blazor-preview
<MokaPanel Title="Elevated Panel" Elevated>
    <p>Uses elevation shadow instead of border.</p>
</MokaPanel>
```
