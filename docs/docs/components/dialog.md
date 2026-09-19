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

## Accessibility

- A `Title` names the dialog (`aria-labelledby`), so screen readers read it out when the dialog opens. Without a title, pass `aria-label`. Extra attributes on `MokaDialog` land on the element with `role="dialog"`.
- Focus moves into the dialog as it opens, and Tab stays inside until it closes. Focus then goes back to where it was.
- Focus starts on an element marked `autofocus` or `data-autofocus` when the content has one, and on the first control otherwise. Content that already moved focus inside the dialog keeps it. A service prompt starts in its text field.
- A dialog rendered with `Open="true"` from the start traps focus from its first render.
- Escape closes the dialog unless `CloseOnEscape` is `false`.

## IMokaDialogService

You can call the service from any thread, for example from a timer callback or after `Task.Run`. `MokaDialogHost` applies each change on the renderer's thread.

### Stacked Dialogs

Each call opens its dialog straight away, on top of any dialog already open, and returns its own result. A dialog's action can await another dialog:

```csharp
async Task DeleteAllAsync()
{
    // The confirmation opens over whatever dialog called this.
    if (await Dialog.ConfirmAsync("Delete every item?", title: "Are you sure?"))
        await Repository.DeleteAllAsync();
}
```

Escape and a backdrop click close only the dialog on top. `Close(bool)` and `CloseWithResult(object)` also act on the top dialog. To close a particular one, use the overloads that take its request, from `OpenDialogs` (bottom first):

| Member | Description |
|---|---|
| `OpenDialogs` | The open requests, bottom first |
| `Close(request, result)` | Closes that dialog. `result` is the confirm/cancel flag |
| `CloseWithResult(request, result)` | Closes that dialog with a result value |
| `CloseAll()` | Cancels every open dialog, top first. Each caller gets its cancel value (`false` or `null`) |

Up to 0.1.11 the service queued requests and showed one dialog at a time, so a dialog that awaited another one waited forever.

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

Renders any `IComponent` inside the dialog. Parameters are passed as a dictionary. The component closes its own dialog through the cascaded `MokaDialogContext`, which still targets the right dialog when another one has opened on top of it:

```razor
@code {
    [CascadingParameter] public MokaDialogContext? Dialog { get; set; }

    void Pick(User user) => Dialog?.Close(user);

    void Cancel() => Dialog?.Cancel();
}
```

```csharp
object? result = await Dialog.ShowComponentAsync<UserPickerDialog>(
    "Select User",
    p => p["TeamId"] = teamId,
    o => o.DialogSize = MokaDialogSize.Large);

if (result is User selected)
    AssignUser(selected);
```
