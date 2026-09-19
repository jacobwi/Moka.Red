---
title: Notice
description: Full-width banner for announcements, warnings, and status messages.
order: 56
---

# Notice

`MokaNotice` renders a full-width banner across the top or bottom of a page. Use it for site-wide announcements, maintenance warnings, or promotional messages.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment` | **required** | The notice message content |
| `Color` | `MokaColor` | `Info` | Color theme for the banner |
| `Position` | `MokaNoticePosition` | `Top` | Position: `Top` or `Bottom` |
| `Closable` | `bool` | `true` | Shows a close button |
| `OnClose` | `EventCallback` | -- | Callback when the notice is dismissed |
| `Icon` | `MokaIconDefinition?` | `null` | Optional leading icon |
| `Sticky` | `bool` | `true` | Sticks to the viewport edge when scrolling |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Space around the banner. It still fills its container, with the margin inside |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

The banner fills the width of its container. Up to 0.1.12 it was `width: 100%`, so a margin made it wider than the container.

## Info Notice

```blazor-preview
<MokaNotice Color="MokaColor.Info">
    New version available! Check the release notes for details.
</MokaNotice>
```

## Warning Notice

```blazor-preview
<MokaNotice Color="MokaColor.Warning" Closable="true">
    Scheduled maintenance this Saturday from 2:00 AM to 6:00 AM UTC.
</MokaNotice>
```

## Bottom Position

```blazor-preview
<div style="position: relative; min-height: 120px; border: 1px dashed var(--moka-color-outline-variant); border-radius: var(--moka-radius-md);">
    <MokaNotice Color="MokaColor.Success" Position="MokaNoticePosition.Bottom">
        Your changes have been saved.
    </MokaNotice>
</div>
```

## With Icon

```blazor-preview
<MokaNotice Color="MokaColor.Error" Icon="MokaIcons.Status.Error" Closable="true">
    Service disruption detected. Some features may be unavailable.
</MokaNotice>
```
