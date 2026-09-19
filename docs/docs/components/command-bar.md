---
title: Command Bar
description: Persistent top bar with search, left/right content zones, and keyboard shortcut support.
order: 63
---

# Command Bar

`MokaCommandBar` is a persistent horizontal bar with three zones: left (breadcrumb/navigation), center (search), and right (actions). Inspired by the VS Code top bar layout. Unlike `MokaCommandPalette`, this is a visible, always-present bar and not a popup overlay.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `LeftContent` | `RenderFragment?` | -- | Content rendered in the left zone (breadcrumb, back button, etc.) |
| `CenterContent` | `RenderFragment?` | -- | Content rendered in the center zone; replaces the built-in search input when provided |
| `RightContent` | `RenderFragment?` | -- | Content rendered in the right zone (action buttons, avatar, etc.) |
| `SearchPlaceholder` | `string` | `"Search..."` | Placeholder text for the built-in search input |
| `SearchValue` | `string?` | -- | The current search value; supports two-way binding via `SearchValueChanged` |
| `SearchValueChanged` | `EventCallback<string?>` | -- | Callback invoked when `SearchValue` changes |
| `OnSearch` | `EventCallback<string>` | -- | Callback invoked when the user presses Enter in the search input |
| `ShowSearch` | `bool` | `true` | Whether to show the built-in search input in the center zone |
| `Dense` | `bool` | `false` | Whether to use dense (compact) height |
| `Bordered` | `bool` | `true` | Whether to show a bottom border |
| `Elevated` | `bool` | `false` | Whether to show an elevation shadow |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic with Search

A command bar with the built-in search input and action buttons on the right.

```blazor-preview
<MokaCommandBar SearchPlaceholder="Search files, commands...">
    <LeftContent>
        <MokaText Size="MokaSize.Sm" Weight="MokaFontWeight.Semibold">My Workspace</MokaText>
    </LeftContent>
    <RightContent>
        <MokaButton Variant="MokaVariant.Text" Size="MokaSize.Sm" StartIcon="MokaIcons.Action.Settings">Settings</MokaButton>
    </RightContent>
</MokaCommandBar>
```

## With Breadcrumb and Actions

Use the left zone for breadcrumb navigation and the right zone for action buttons.

```blazor-preview
<MokaCommandBar>
    <LeftContent>
        <MokaBreadcrumb>
            <MokaBreadcrumbItem Text="Home" Href="#" Icon="MokaIcons.Navigation.Home" />
            <MokaBreadcrumbItem Text="Projects" Href="#" />
            <MokaBreadcrumbItem Text="Moka.Red" />
        </MokaBreadcrumb>
    </LeftContent>
    <RightContent>
        <MokaButton Variant="MokaVariant.Outlined" Size="MokaSize.Sm">Share</MokaButton>
        <MokaButton Size="MokaSize.Sm" Color="MokaColor.Primary">Save</MokaButton>
    </RightContent>
</MokaCommandBar>
```

## Narrow Widths

When space runs short the start zone gives way first: it shrinks and clips, and a breadcrumb in it stays on one line. The search box keeps at least 8rem and shrinks before the actions do. Below 640px the bar grows a second row and the search takes it, full width, under the start zone and the actions.

## Dense

The dense variant uses compact height for tighter layouts.

```blazor-preview
<MokaCommandBar Dense SearchPlaceholder="Quick search...">
    <LeftContent>
        <MokaText Size="MokaSize.Xs">Editor</MokaText>
    </LeftContent>
    <RightContent>
        <MokaButton Variant="MokaVariant.Text" Size="MokaSize.Sm" StartIcon="MokaIcons.Action.Settings" />
    </RightContent>
</MokaCommandBar>
```

## Elevated

Add a shadow for visual separation from content below.

```blazor-preview
<MokaCommandBar Elevated Bordered="false" SearchPlaceholder="Type a command...">
    <LeftContent>
        <MokaButton Variant="MokaVariant.Text" Size="MokaSize.Sm" StartIcon="MokaIcons.Navigation.ArrowLeft">Back</MokaButton>
    </LeftContent>
</MokaCommandBar>
```
