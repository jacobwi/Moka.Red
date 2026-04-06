---
title: Dialog
description: Modal dialog with backdrop, focus trap, draggable/resizable support, and a programmatic service API.
order: 6
---

# Dialog

Moka.Red provides two dialog authoring styles:

- **Declarative** — `MokaDialog` component with two-way `@bind-Open` in the template.
- **Programmatic** — `IMokaDialogService` injected into code-behind, returns `Task<T>` results.

Both styles require `MokaDialogHost` placed once in the application shell (typically `MainLayout`).

## Setup

```razor
@* MainLayout.razor *@
<MokaDialogHost />
```

Register the service in `Program.cs`:

```csharp
builder.Services.AddMokaFeedback(); // registers IMokaDialogService
```

## MokaDialog Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Open` | `bool` | `false` | Visibility — two-way bindable |
| `OpenChanged` | `EventCallback<bool>` | — | Notified when open state changes |
| `Title` | `string?` | — | Dialog header title |
| `ChildContent` | `RenderFragment?` | — | Body content |
| `Actions` | `RenderFragment?` | — | Footer actions slot |
| `ShowCloseButton` | `bool` | `true` | Renders the X button in the header |
| `CloseOnBackdropClick` | `bool` | `true` | Clicking the backdrop closes the dialog |
| `CloseOnEscape` | `bool` | `true` | Escape key closes the dialog |
| `DialogSize` | `MokaDialogSize` | `Medium` | `Small`, `Medium`, `Large`, `FullScreen` |
| `PreventScroll` | `bool` | `true` | Locks body scroll while open |
| `Draggable` | `bool` | `false` | Dialog can be dragged by its header |
| `Resizable` | `bool` | `false` | Dialog can be resized |
| `MinWidth` | `string` | `"200px"` | Minimum width when resizable |
| `MinHeight` | `string` | `"100px"` | Minimum height when resizable |
| `OnClose` | `EventCallback` | — | Fires when the dialog closes |

## Basic Declarative Dialog

```blazor-preview
@code {
    bool _open;
}

<MokaButton OnClick="@(() => _open = true)">Open Dialog</MokaButton>

<MokaDialog @bind-Open="_open" Title="Confirm Action">
    <ChildContent>
        Are you sure you want to proceed?
    </ChildContent>
    <Actions>
        <MokaButton Variant="MokaVariant.Text" OnClick="@(() => _open = false)">Cancel</MokaButton>
        <MokaButton Color="MokaColor.Primary" OnClick="@(() => _open = false)">Confirm</MokaButton>
    </Actions>
</MokaDialog>
```

## Size Variants

```blazor-preview
@code {
    MokaDialogSize _size = MokaDialogSize.Medium;
    bool _open;
    void Open(MokaDialogSize size) { _size = size; _open = true; }
}

<div style="display:flex;gap:8px">
    <MokaButton Size="MokaSize.Sm" OnClick="@(() => Open(MokaDialogSize.Small))">Small</MokaButton>
    <MokaButton Size="MokaSize.Sm" OnClick="@(() => Open(MokaDialogSize.Medium))">Medium</MokaButton>
    <MokaButton Size="MokaSize.Sm" OnClick="@(() => Open(MokaDialogSize.Large))">Large</MokaButton>
    <MokaButton Size="MokaSize.Sm" OnClick="@(() => Open(MokaDialogSize.FullScreen))">FullScreen</MokaButton>
</div>

<MokaDialog @bind-Open="_open" Title="@_size.ToString() Dialog" DialogSize="_size">
    <ChildContent>Dialog content for @_size size.</ChildContent>
    <Actions>
        <MokaButton OnClick="@(() => _open = false)">Close</MokaButton>
    </Actions>
</MokaDialog>
```

## Draggable Dialog

```blazor-preview
@code { bool _open; }

<MokaButton OnClick="@(() => _open = true)">Open Draggable</MokaButton>

<MokaDialog @bind-Open="_open" Title="Drag Me" Draggable>
    <ChildContent>Drag this dialog by the title bar.</ChildContent>
    <Actions>
        <MokaButton OnClick="@(() => _open = false)">Close</MokaButton>
    </Actions>
</MokaDialog>
```

## Resizable Dialog

```blazor-preview
@code { bool _open; }

<MokaButton OnClick="@(() => _open = true)">Open Resizable</MokaButton>

<MokaDialog @bind-Open="_open" Title="Resize Me" Draggable Resizable MinWidth="300px" MinHeight="200px">
    <ChildContent>Drag the edges to resize this dialog.</ChildContent>
    <Actions>
        <MokaButton OnClick="@(() => _open = false)">Close</MokaButton>
    </Actions>
</MokaDialog>
```

## IMokaDialogService

### ConfirmAsync

Returns `true` when the user clicks the confirm button.

```csharp
@inject IMokaDialogService Dialog

async Task DeleteAsync()
{
    bool confirmed = await Dialog.ConfirmAsync(
        "This will permanently delete the record.",
        title: "Delete Item?");

    if (confirmed)
        await Repository.DeleteAsync(ItemId);
}
```

### PromptAsync

Returns the entered string, or `null` if cancelled.

```csharp
string? name = await Dialog.PromptAsync(
    "Enter a name for the new folder:",
    title: "New Folder",
    defaultValue: "Untitled");

if (name is not null)
    await CreateFolderAsync(name);
```

### ShowAsync

Displays arbitrary `RenderFragment` content and waits until closed.

```csharp
await Dialog.ShowAsync("Help", @<p>Read the docs at <a href="/docs">docs</a>.</p>);
```

### ShowComponentAsync

Renders any `IComponent` inside the dialog. Parameters are passed as a dictionary and the component can return a typed result by calling `DialogService.CloseWithResult(result)`.

```csharp
object? result = await Dialog.ShowComponentAsync<UserPickerDialog>(
    "Select User",
    p => p["TeamId"] = teamId,
    o => o.DialogSize = MokaDialogSize.Large);

if (result is User selected)
    AssignUser(selected);
```
