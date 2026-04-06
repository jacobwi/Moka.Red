---
title: Drawer
description: Slide-in overlay panel for secondary content, navigation, or detail views.
order: 31
---

# Drawer

`MokaDrawer` renders a slide-in panel that overlays the main content from any edge of the viewport. It supports backdrop click and escape-key dismissal, an optional title bar with close button, and two-way binding on its open state.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Open` | `bool` | `false` | Whether the drawer is visible. Supports two-way binding via `@bind-Open`. |
| `OpenChanged` | `EventCallback<bool>` | -- | Callback when the open state changes |
| `Position` | `MokaDockPosition` | `Left` | Which edge the drawer slides in from: `Left`, `Right`, `Top`, `Bottom` |
| `Width` | `string` | `"320px"` | Width of the drawer when `Position` is `Left` or `Right` |
| `Height` | `string` | `"40vh"` | Height of the drawer when `Position` is `Top` or `Bottom` |
| `Title` | `string?` | -- | Optional title displayed in the drawer header |
| `ShowCloseButton` | `bool` | `true` | Shows a close button in the header |
| `CloseOnBackdropClick` | `bool` | `true` | Closes the drawer when the backdrop overlay is clicked |
| `CloseOnEscape` | `bool` | `true` | Closes the drawer when the Escape key is pressed |
| `Overlay` | `bool` | `true` | Shows a semi-transparent backdrop behind the drawer |
| `ChildContent` | `RenderFragment?` | -- | Content rendered inside the drawer body |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Left Drawer

```blazor-preview
<MokaButton OnClick="() => leftOpen = !leftOpen">Open Left Drawer</MokaButton>

<MokaDrawer @bind-Open="leftOpen" Title="Navigation">
    <MokaList>
        <MokaListItem Text="Dashboard" Icon="MokaIcons.Action.Home" />
        <MokaListItem Text="Settings" Icon="MokaIcons.Action.Settings" />
        <MokaListItem Text="Profile" Icon="MokaIcons.Action.Person" />
    </MokaList>
</MokaDrawer>

@code {
    bool leftOpen;
}
```

## Right Drawer

```blazor-preview
<MokaButton OnClick="() => rightOpen = !rightOpen">Open Right Drawer</MokaButton>

<MokaDrawer @bind-Open="rightOpen" Position="MokaDockPosition.Right" Title="Details">
    <MokaParagraph>Detail panel content slides in from the right.</MokaParagraph>
</MokaDrawer>

@code {
    bool rightOpen;
}
```

## Bottom Sheet Style

Using `Position="Bottom"` creates a bottom sheet effect.

```blazor-preview
<MokaButton OnClick="() => bottomOpen = !bottomOpen">Open Bottom Sheet</MokaButton>

<MokaDrawer @bind-Open="bottomOpen" Position="MokaDockPosition.Bottom" Height="30vh" Title="Actions">
    <div style="display:flex;gap:8px;padding:16px">
        <MokaButton Variant="MokaVariant.Outlined">Share</MokaButton>
        <MokaButton Variant="MokaVariant.Outlined">Copy Link</MokaButton>
        <MokaButton Variant="MokaVariant.Outlined" Color="MokaColor.Error">Delete</MokaButton>
    </div>
</MokaDrawer>

@code {
    bool bottomOpen;
}
```

## Without Overlay

Set `Overlay="false"` to allow interaction with the main content while the drawer is open.

```blazor-preview
<MokaButton OnClick="() => noOverlay = !noOverlay">Toggle Drawer</MokaButton>

<MokaDrawer @bind-Open="noOverlay" Overlay="false" Title="Side Panel">
    <MokaParagraph>The main page remains interactive behind this drawer.</MokaParagraph>
</MokaDrawer>

@code {
    bool noOverlay;
}
```

## Programmatic Open/Close

```blazor-preview
<div style="display:flex;gap:8px">
    <MokaButton OnClick="() => progOpen = true">Open</MokaButton>
    <MokaButton OnClick="() => progOpen = false" Variant="MokaVariant.Outlined">Close</MokaButton>
</div>

<MokaDrawer @bind-Open="progOpen" Title="Controlled Drawer"
            CloseOnBackdropClick="false" ShowCloseButton="false">
    <MokaParagraph>This drawer can only be closed via the external Close button.</MokaParagraph>
</MokaDrawer>

@code {
    bool progOpen;
}
```
