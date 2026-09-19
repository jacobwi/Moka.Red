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
| `Position` | `MokaDrawerPosition` | `Left` | Which edge the drawer slides in from: `Left`, `Right`, `Top`, `Bottom` |
| `Width` | `string` | `"320px"` | Width of the drawer when `Position` is `Left` or `Right` |
| `Height` | `string` | `"40vh"` | Height of the drawer when `Position` is `Top` or `Bottom` |
| `Title` | `string?` | -- | Optional title displayed in the drawer header |
| `ShowCloseButton` | `bool` | `true` | Shows a close button in the header |
| `CloseOnBackdropClick` | `bool` | `true` | Closes the drawer when the backdrop overlay is clicked |
| `CloseOnEscape` | `bool` | `true` | Closes the drawer when the Escape key is pressed |
| `Overlay` | `bool` | `true` | Shows a semi-transparent backdrop behind the drawer and makes it modal: it takes focus, holds Tab and stops the page behind it from scrolling |
| `ChildContent` | `RenderFragment?` | -- | Content rendered inside the drawer body |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Left Drawer

```blazor-preview
<MokaButton OnClick="() => leftOpen = !leftOpen">Open Left Drawer</MokaButton>

<MokaDrawer @bind-Open="leftOpen" Title="Navigation">
    <MokaList>
        <MokaListItem Text="Dashboard" Icon="MokaIcons.Navigation.Home" />
        <MokaListItem Text="Settings" Icon="MokaIcons.Action.Settings" />
        <MokaListItem Text="Profile" Icon="MokaIcons.Action.Search" />
    </MokaList>
</MokaDrawer>

@code {
    bool leftOpen;
}
```

## Right Drawer

```blazor-preview
<MokaButton OnClick="() => rightOpen = !rightOpen">Open Right Drawer</MokaButton>

<MokaDrawer @bind-Open="rightOpen" Position="MokaDrawerPosition.Right" Title="Details">
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

<MokaDrawer @bind-Open="bottomOpen" Position="MokaDrawerPosition.Bottom" Height="30vh" Title="Actions">
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

## Keyboard and Focus

With `Overlay` on (the default) the drawer is modal (`aria-modal="true"`):

- Opening moves focus into the drawer: to an element marked `autofocus` or `data-autofocus` if there is one, otherwise its first control, usually the close button. Content that already took focus inside the drawer keeps it.
- Tab and Shift+Tab stay inside the drawer and wrap at the ends, wherever focus is in it.
- Escape closes it, unless `CloseOnEscape` is `false`.
- Closing it, by Escape, the close button, the backdrop or `Open` set to `false`, returns focus to where it was before the drawer opened.
- A dialog or cheatsheet opened from inside the drawer hands focus back into the drawer when it closes.
- The page behind doesn't scroll while the drawer is open. The lock is the one dialogs and bottom sheets take, counted, so closing the drawer over an open dialog leaves the dialog's lock in place. Every way the drawer closes gives the lock back, and so does removing the drawer while it is open.

Without `Overlay` the drawer isn't modal. It leaves focus where it is, doesn't hold on to Tab, and the page behind stays usable and scrolls. Escape still closes it while focus is inside it.

While the drawer is open, keys pressed inside it don't reach Blazor `@onkeydown` handlers on elements around it, so Escape closes only the drawer and not a drawer or dialog it sits in. A cheatsheet opened inside the drawer stops its keys the same way, so its Escape leaves the drawer open. A listener added with JavaScript on the document still hears the keys.

The close button is `type="button"`, so it doesn't submit a form the drawer sits in. Up to 0.1.12 it did, and opening a drawer left focus on the page behind it, so Escape did nothing until the user clicked inside the drawer. Escape inside a drawer also closed a drawer or dialog around it, and the page behind a modal drawer still scrolled.
