---
title: Toast
description: Non-blocking notification toasts with auto-dismiss, progress bar, severity icons, and action buttons.
order: 10
---

# Toast

`MokaToastHost` renders the active toast stack. `IMokaToastService` is injected into components or services to trigger notifications. Toasts auto-dismiss after `DurationMs` milliseconds and animate in/out.

## Setup

Place `MokaToastHost` once in the application layout:

```razor
@* MainLayout.razor *@
<MokaToastHost />
```

Register services in `Program.cs`:

```csharp
builder.Services.AddMokaFeedback(); // registers IMokaToastService
```

## MokaToastHost Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Position` | `MokaToastPosition` | `TopRight` | Screen position of the toast container |
| `MaxVisible` | `int` | `5` | Maximum toasts shown simultaneously |
| `OffsetTop` | `string?` | `null` | A CSS length kept clear along the top of the window, for the top positions. See [Keeping toasts off part of the window](#keeping-toasts-off-part-of-the-window) |
| `OffsetRight` | `string?` | `null` | A CSS length kept clear along the right edge, for the right and centred positions |
| `OffsetBottom` | `string?` | `null` | A CSS length kept clear along the bottom, for the bottom positions |
| `OffsetLeft` | `string?` | `null` | A CSS length kept clear along the left edge, for the left and centred positions |
| `Class` | `string?` | `null` | Additional CSS classes on the host, after its own |
| `Style` | `string?` | `null` | Inline styles on the host, after the offsets |

### MokaToastPosition Values

`TopRight`, `TopLeft`, `TopCenter`, `BottomRight`, `BottomLeft`, `BottomCenter`

## IMokaToastService Methods

| Method | Description |
|--------|-------------|
| `Show(message, severity, configure?)` | Show with explicit severity |
| `ShowSuccess(message, configure?)` | Green success toast |
| `ShowError(message, configure?)` | Red error toast |
| `ShowWarning(message, configure?)` | Amber warning toast |
| `ShowInfo(message, configure?)` | Blue info toast |
| `Remove(id)` | Dismiss a specific toast by `Guid` |
| `Clear()` | Dismiss all active toasts |

You can call the service from any thread, for example from a timer callback or after `Task.Run`. `MokaToastHost` applies each change on the renderer's thread.

## MokaToastOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Title` | `string?` | - | Bold title above the message |
| `DurationMs` | `int` | `5000` | Auto-dismiss after N ms. `0` = persistent |
| `ShowCloseButton` | `bool` | `true` | Renders the X button |
| `ShowIcon` | `bool` | `true` | Shows severity icon |
| `CustomIcon` | `MokaIconDefinition?` | - | Overrides the default severity icon |
| `OnClick` | `Action?` | - | Called when toast body is clicked |
| `ActionText` | `string?` | - | Optional action button label |
| `OnAction` | `Action?` | - | Called when action button is clicked |

## Basic Usage

```blazor-preview
@inject IMokaToastService Toast

<div style="display:flex;gap:8px;flex-wrap:wrap">
    <MokaButton Color="MokaColor.Success" OnClick='() => Toast.ShowSuccess("Record saved!")'>
        Success
    </MokaButton>
    <MokaButton Color="MokaColor.Error" OnClick='() => Toast.ShowError("Failed to connect.")'>
        Error
    </MokaButton>
    <MokaButton Color="MokaColor.Warning" OnClick='() => Toast.ShowWarning("Disk space is low.")'>
        Warning
    </MokaButton>
    <MokaButton Color="MokaColor.Info" OnClick='() => Toast.ShowInfo("Update available.")'>
        Info
    </MokaButton>
</div>
```

## With Title

```blazor-preview
@inject IMokaToastService Toast

<MokaButton OnClick="ShowWithTitle">Show With Title</MokaButton>

@code {
    void ShowWithTitle() => Toast.ShowSuccess(
        "Your changes have been saved to the server.",
        o => o.Title = "Saved Successfully");
}
```

## Persistent Toast

Set `DurationMs = 0` to prevent auto-dismiss. The user must click the X button.

```blazor-preview
@inject IMokaToastService Toast

<MokaButton OnClick="ShowPersistent">Persistent Toast</MokaButton>

@code {
    void ShowPersistent() => Toast.ShowError(
        "Database connection lost. Retrying…",
        o => {
            o.Title = "Connection Error";
            o.DurationMs = 0;
        });
}
```

## Toast with Action Button

```blazor-preview
@inject IMokaToastService Toast

<MokaButton OnClick="ShowWithAction">Show With Action</MokaButton>

@code {
    void ShowWithAction() => Toast.Show(
        "New version deployed.",
        MokaToastSeverity.Info,
        o => {
            o.Title = "Deployment Complete";
            o.ActionText = "View Changes";
            o.OnAction = () => { /* navigate to /changelog */ };
        });
}
```

## Custom Icon

```blazor-preview
@inject IMokaToastService Toast

<MokaButton OnClick="ShowCustomIcon">Custom Icon</MokaButton>

@code {
    void ShowCustomIcon() => Toast.Show(
        "Your export is ready to download.",
        MokaToastSeverity.Info,
        o => {
            o.CustomIcon = MokaIcons.Action.Download;
            o.ActionText = "Download";
        });
}
```

## Positioning

```blazor-preview
@* Override host position - place a second host only in demos *@
<MokaToastHost Position="MokaToastPosition.BottomCenter" />
```

> Only one `MokaToastHost` should exist per application. The `Position` parameter on `MokaToastHost` controls where all toasts appear.

## Keeping toasts off part of the window

The host sits against the window's edges with a small padding. When an app draws something of its own along an edge, such
as a 48 px title bar with the window's minimise, maximise and close buttons, or a status bar along the bottom, an offset
keeps that strip clear. Each offset moves only the edge it names, and only for the positions that sit against that edge,
on top of the padding: `OffsetTop` moves the top positions down, `OffsetBottom` the bottom positions up, and `OffsetLeft`
and `OffsetRight` the side positions in. The centred positions stay centred between the left and right offsets.

```razor
@* MainLayout.razor: toasts start below the app's 48px title bar and stay above its 24px status bar. *@
<MokaToastHost Position="MokaToastPosition.TopRight" OffsetTop="48px" OffsetBottom="24px" />
```

Each offset is a CSS custom property on the host, `--moka-toast-offset-top`, `-right`, `-bottom` and `-left`, read with a
fallback of 0. The component writes a variable only when its parameter is set, so an app can also set them from its own
CSS, on the host or on any element around it, for example a value that differs between a desktop and a web host:

```css
/* The desktop host draws its own title bar; the browser does not. */
.desktop-shell {
    --moka-toast-offset-top: 48px;
}
```

A close button shows its name in a tooltip, below it, or above it for the bottom positions. The centred positions are
centred with auto margins rather than a transform, so that tooltip, placed against the window, is never held inside the
toast stack.

## Clearing All Toasts

```blazor-preview
@inject IMokaToastService Toast

<MokaButton OnClick='() => Toast.ShowInfo("Toast 1")'>Add</MokaButton>
<MokaButton Color="MokaColor.Error" Variant="MokaVariant.Outlined" OnClick='() => Toast.Clear()'>
    Clear All
</MokaButton>
```
