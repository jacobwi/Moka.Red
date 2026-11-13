---
title: Dialog
description: Modal dialog with placement modes, backdrop effects, exit animations, focus trap, draggable/resizable support, and a programmatic service API.
order: 6
---

# Dialog

Moka.Red provides two dialog authoring styles:

- **Declarative** - `MokaDialog` component with two-way `@bind-Open` in the template.
- **Programmatic** - `IMokaDialogService` injected into code-behind, returns `Task<T>` results.

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
| `Open` | `bool` | `false` | Visibility. Two-way bindable. See [Closing without binding](#closing-without-binding) |
| `OpenChanged` | `EventCallback<bool>` | - | Notified when open state changes |
| `Title` | `string?` | - | Dialog header title |
| `Icon` | `MokaIconDefinition?` | - | Icon before the title |
| `IconColor` | `MokaColor?` | - | The tint of the icon's tile in the `IconLead` and `Centered` looks: `Error`, `Warning`, `Success` or `Info`, else the accent. See [Looks](#looks) |
| `Look` | `MokaDialogLook?` | - | How the dialog is drawn. Null takes the theme's choice. See [Looks](#looks) |
| `TitleContent` | `RenderFragment?` | - | Title markup in place of `Title`. Renders inside the heading the dialog is named by |
| `HeaderActions` | `RenderFragment?` | - | Controls in the header, between the title and the close button |
| `ChildContent` | `RenderFragment?` | - | Body content |
| `Actions` | `RenderFragment?` | - | Footer actions slot |
| `ShowCloseButton` | `bool` | `true` | Renders the X button in the header |
| `CloseOnBackdropClick` | `bool` | `true` | A click on the backdrop closes the dialog. See [Backdrop clicks](#backdrop-clicks) |
| `CloseOnEscape` | `bool` | `true` | Escape key closes the dialog |
| `DialogSize` | `MokaDialogSize` | `Medium` | `Small` (360px), `Medium` (520px), `Large` (720px), `ExtraLarge` (1200x900, each kept inside the window), `FullScreen` |
| `Position` | `MokaDialogPosition` | `Center` | Where the dialog appears. See [Positions](#positions) |
| `Backdrop` | `MokaDialogBackdrop` | `Default` | The effect painted over the page. See [Backdrop effects](#backdrop-effects) |
| `Width` | `string?` | - | Width, overriding `DialogSize`. Any CSS length |
| `MaxWidth` | `string?` | - | Maximum width. Any CSS length |
| `Height` | `string?` | - | Height. Any CSS length; the dialog never grows past the window either way |
| `BodyPadding` | `bool` | `true` | Whether the body pads its content. Off for content that owns its edges |
| `ScrollBody` | `bool` | `true` | Whether the body scrolls. Off for a view that scrolls itself. See [A body that scrolls itself](#a-body-that-scrolls-itself) |
| `PreventScroll` | `bool` | `true` | Locks body scroll while open |
| `OnKeyDown` | `EventCallback<KeyboardEventArgs>` | - | Every key pressed in the dialog, Escape included, before the dialog acts on it. See [Keys](#keys) |
| `Busy` | `bool` | `false` | While the dialog's action runs: Escape, the backdrop and the close button can't close it. See [While the action runs](#while-the-action-runs) |
| `InitialFocus` | `MokaDialogInitialFocus` | `Content` | Where focus goes when the dialog opens. See [Where focus starts](#where-focus-starts) |
| `Draggable` | `bool` | `false` | Dialog can be dragged by its header |
| `Resizable` | `bool` | `false` | Dialog can be resized |
| `MinWidth` | `string` | `"200px"` | Minimum width when resizable |
| `MinHeight` | `string` | `"100px"` | Minimum height when resizable |
| `OnClose` | `EventCallback` | - | Fires when the dialog closes, as its exit animation starts |
| `OnExited` | `EventCallback` | - | Fires when the exit animation has finished and the dialog is gone from the page |

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

## Closing without binding

The dialog can close itself, on Escape or its close button for example, and reports it through
`OpenChanged` and `OnClose`. It stays closed until the parent passes a different `Open`, so a
one-way `Open="_open"` no longer opens it again on the parent's next render, as it did up to 0.1.12.

The parent's field still says `true` after such a close, and setting `true` again changes
nothing. Use `@bind-Open`, or set the field back in `OnClose`:

```razor
<MokaDialog Open="_open" OnClose="() => _open = false" Title="Details">
    <ChildContent>Escape and the X both close this dialog.</ChildContent>
</MokaDialog>
```

A dialog the parent closes starts centered the next time it opens, and a draggable one can be
dragged again. Up to 0.1.12 it kept the position it was dragged to, and its header lost the drag.

## Backdrop clicks

A click on the dimmed area around the dialog closes it when `CloseOnBackdropClick` is `true`.
The press and the release both have to land there: selecting text in the dialog and letting go
outside it leaves the dialog open. A press on the backdrop does not move focus, so with
`CloseOnBackdropClick="false"` focus stays in the dialog and Escape still works.

With stacked service dialogs, each one has its own backdrop and the top one covers the dialogs
below it, so a backdrop click closes only the top dialog.

Up to 0.1.12 the backdrop sat under the element that centers the dialog, which took every click,
so a backdrop click never closed a dialog.

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

## Positions

`Position` anchors the dialog somewhere other than the center: `Left` and `Right` are full-height
panels that slide in from their edge, `Top` and `Bottom` are full-width panels. This is the
side-sheet a settings pane or a detail drawer wants, with everything a dialog already does - the
focus trap, the scroll lock, stacking, the service.

```blazor-preview
@code {
    MokaDialogPosition _position = MokaDialogPosition.Right;
    bool _open;
    void Open(MokaDialogPosition position) { _position = position; _open = true; }
}

<div style="display:flex;gap:8px;flex-wrap:wrap">
    <MokaButton Size="MokaSize.Sm" OnClick="@(() => Open(MokaDialogPosition.Left))">Left</MokaButton>
    <MokaButton Size="MokaSize.Sm" OnClick="@(() => Open(MokaDialogPosition.Right))">Right</MokaButton>
    <MokaButton Size="MokaSize.Sm" OnClick="@(() => Open(MokaDialogPosition.Top))">Top</MokaButton>
    <MokaButton Size="MokaSize.Sm" OnClick="@(() => Open(MokaDialogPosition.Bottom))">Bottom</MokaButton>
</div>

<MokaDialog @bind-Open="_open" Title="Panel" Position="_position" Width="420px">
    <ChildContent>
        A panel pinned to @_position. It fills its edge and slides in and out from it;
        the window-facing corners are square, the inner ones stay round.
    </ChildContent>
    <Actions>
        <MokaButton OnClick="@(() => _open = false)">Close</MokaButton>
    </Actions>
</MokaDialog>
```

A side panel takes its width from the size presets or `Width`, and a top or bottom panel takes
half the viewport in height unless `Height` says otherwise. `FullScreen` wins over `Position` at
any edge. `Draggable` and `Resizable` are centering-only: an edge-anchored panel has nowhere to be
dragged to, so the dialog leaves both off it.

`Position` is on `MokaDialogOptions` too, so a service dialog can be a panel:

```csharp
await Dialog.ShowAsync("Filters", filtersFragment,
    o => { o.Position = MokaDialogPosition.Right; o.Width = "380px"; });
```

For a panel that is not modal at all - no backdrop, the page still usable behind it - use
`MokaDrawer` with `Overlay="false"` instead.

## Backdrop effects

`Backdrop` picks what sits between the dialog and the page:

| Value | Look |
|---|---|
| `Default` | The dimmed page with the standard blur (8px) |
| `Dim` | The dimmed page, no blur - the lightest overlay |
| `Frosted` | A lighter dim with a strong blur (16px), the frosted-glass look |
| `Dark` | A heavier dim, no blur, for content that must stand out sharply |
| `None` | Nothing at all. The page shows through untouched, but the backdrop still catches clicks, so the dialog stays modal |

```blazor-preview
@code {
    MokaDialogBackdrop _backdrop = MokaDialogBackdrop.Frosted;
    bool _open;
}

<div style="display:flex;gap:8px;flex-wrap:wrap">
    <MokaButton Size="MokaSize.Sm" OnClick="@(() => { _backdrop = MokaDialogBackdrop.Dim; _open = true; })">Dim</MokaButton>
    <MokaButton Size="MokaSize.Sm" OnClick="@(() => { _backdrop = MokaDialogBackdrop.Frosted; _open = true; })">Frosted</MokaButton>
    <MokaButton Size="MokaSize.Sm" OnClick="@(() => { _backdrop = MokaDialogBackdrop.Dark; _open = true; })">Dark</MokaButton>
    <MokaButton Size="MokaSize.Sm" OnClick="@(() => { _backdrop = MokaDialogBackdrop.None; _open = true; })">None</MokaButton>
</div>

<MokaDialog @bind-Open="_open" Title="@_backdrop.ToString() backdrop" Backdrop="_backdrop">
    <ChildContent>The page behind this one is @_backdrop.ToString().ToLower().</ChildContent>
    <Actions>
        <MokaButton OnClick="@(() => _open = false)">Close</MokaButton>
    </Actions>
</MokaDialog>
```

## The exit animation

Closing is decided the moment it starts - the focus trap, the scroll lock and the callbacks have
all gone by then - but the dialog plays a short exit first: the centered box fades down, an edge
panel slides back to its edge, and the backdrop fades with it. `OnClose` fires as the exit starts
and `OnExited` fires once the dialog is really gone, which is the moment to, say, clear the form
behind it. A dialog reopened mid-exit cancels the removal and plays its entrance again.

Under `prefers-reduced-motion` the exit is instant; the markup still leaves after the same short
delay.

## Responsive behavior

The size presets are wider than a phone screen once the wrapper's padding is taken out, so under
640px they give way to the whole viewport: `Small`, `Medium` and `Large` dialogs go full width,
side panels take the full width, and top or bottom panels stay short enough (90vh) to leave the
page readable behind them. Heights are measured in `dvh` where the browser knows it, so a dialog
fits whatever browser chrome a phone is showing.

## A body that scrolls itself

The body pads its content and scrolls it. A view with a layout of its own, such as a list beside a
detail panel, wants neither: it owns its edges and shows its own scrollbar.

```razor
<MokaDialog @bind-Open="_open" Title="Logs" DialogSize="MokaDialogSize.ExtraLarge"
            BodyPadding="false" ScrollBody="false">
    <LogView />
</MokaDialog>
```

With `ScrollBody="false"` the body clips and a single child fills it, so that child gets a definite
size to scroll inside, the same way `MokaDockContent` works with `Scrollable="false"`. Several
children stack at their own size; give the one that should fill `flex: 1`. (Up to 0.4.4 the rule that
makes the one child fill was scoped to the dialog and never reached the app's markup, so the child kept
its own height.)

`ExtraLarge` is 1200 by 900, each side kept inside the window. For anything else, `Width`,
`MaxWidth` and `Height` take a CSS length and override the size preset.

## A header of your own

`Icon` puts a glyph before the title, `TitleContent` replaces the title text with markup (it still
renders inside the heading the dialog is named by, so put only the title in it), and `HeaderActions`
holds controls between the title and the close button.

```razor
<MokaDialog @bind-Open="_open" Icon="MokaIcons.File.FileText" Title="Build log">
    <HeaderActions>
        <MokaButton Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Copy">Copy</MokaButton>
    </HeaderActions>
    <ChildContent>...</ChildContent>
</MokaDialog>
```

All of these are on `MokaDialogOptions` too, so a service dialog gets the same header and body.

## Keys

`OnKeyDown` hears every key pressed while focus is in the dialog, Escape included, before the dialog acts on
it. A picture viewer steps with the arrows:

```razor
<MokaDialog Open="@(_index is not null)" OpenChanged="OnOpenChanged" Title="@Shown.Name"
            DialogSize="MokaDialogSize.FullScreen" BodyPadding="false" ScrollBody="false"
            OnKeyDown="StepAsync">
    <div class="viewer-stage" tabindex="-1" data-autofocus>
        <img src="@Shown.Url" alt="@Shown.Name" />
    </div>
</MokaDialog>

@code {
    private async Task StepAsync(KeyboardEventArgs key)
    {
        // With Alt, Ctrl or Cmd these are the browser's own shortcuts, such as Alt+Left for back.
        if (key.AltKey || key.CtrlKey || key.MetaKey)
            return;

        switch (key.Key)
        {
            case "ArrowLeft": await MoveToAsync(_index - 1); break;
            case "ArrowRight": await MoveToAsync(_index + 1); break;
            case "Home": await MoveToAsync(0); break;
            case "End": await MoveToAsync(_pictures.Count - 1); break;
        }
    }
}
```

- Escape still closes the dialog after `OnKeyDown` has heard it, unless `CloseOnEscape` is `false`.
- A key an open popup in the dialog keeps to itself, such as an arrow in an open select, doesn't reach it.
- `KeyboardEventArgs` carries no target, so a key typed in a field in the dialog arrives too. A dialog that
  holds fields and also steps with the arrows can listen with `@onmokakeydown` on its own element instead and
  skip presses whose `Owner` is `Field` or `Control` (see [Hotkeys](/components/hotkeys#page-shortcuts-with-onmokakeydown)).
- **Arrows and scrolling.** With `ScrollBody` on, an arrow also scrolls the body. A dialog that steps with the
  arrows sets `ScrollBody="false"`: the body clips, so the arrows have nothing to scroll, and the page behind is
  locked while the dialog is open. Don't cancel the keys with `@onkeydown:preventDefault`, which cancels Tab too.

## While the action runs

`Busy` keeps the dialog open while its action runs. Escape and a backdrop click do nothing, the close button
is disabled, and the box carries `aria-busy`. Setting `Open` to `false` still closes it. An app already owns
its buttons, so it shows the loading state and the failure itself:

```razor
<MokaDialog @bind-Open="_open" Title="Share" Busy="_saving">
    <ChildContent>
        ...
        @if (_error is not null)
        {
            <MokaAlert Severity="MokaToastSeverity.Error" Dense>@_error</MokaAlert>
        }
    </ChildContent>
    <Actions>
        <MokaButton Variant="MokaVariant.Text" Disabled="_saving" OnClick="@(() => _open = false)">Cancel</MokaButton>
        <MokaButton Loading="_saving" OnClick="ShareAsync">Share</MokaButton>
    </Actions>
</MokaDialog>
```

A service confirm does all of it for you through `OnConfirmAsync`; see [A confirm that does the work](#a-confirm-that-does-the-work).

## Where focus starts

When a dialog opens, focus goes to an element marked `autofocus` or `data-autofocus` if the content has one.
Otherwise `InitialFocus` decides:

| Value | Where focus goes |
|---|---|
| `Content` | The first control in the body, else the first button in `Actions`, else a control in `HeaderActions`; the dialog itself when it holds nothing but its close button. The default |
| `FirstControl` | The first control in tab order, which is usually the close button. What every dialog did up to 0.4.4 |
| `Dialog` | The dialog itself, so a screen reader reads it from its title and a key reaches it before any control |

This follows the WAI-ARIA Authoring Practices dialog pattern: focus starts on the first focusable element,
and on the least destructive action when the step can't be undone, which is why Cancel goes first among the
actions. The close button never takes the first focus by default: its tooltip shows on keyboard focus and
covered the dialog's first line the moment it opened. Tab still reaches it. A service dialog takes
`InitialFocus` from its options, and a confirm starts on Cancel.

## Looks

The theme picks how every dialog is drawn, and a dialog's own `Look` wins over it. Only the new design
draws the looks: under `MokaTheme.Dark` and `MokaTheme.Light` a dialog stays as it is, whatever either says.

| Look | What it draws |
|------|---------------|
| `Standard` | The title in the display font, a close button, and the buttons at the bottom end. The default |
| `Bands` | The header and the buttons in tinted bands with a line between each and the body. Clearest for long forms |
| `IconLead` | The `Icon` in a tinted tile beside the title, and the body lined up under the title |
| `Centered` | Icon, title and text centered over buttons stacked across the whole width, the main one on top. For confirmations and phones |
| `Terminal` | Monospace capitals behind a `>` prompt, a border in the accent with a glow, and outlined monospace buttons |

Set it for the whole app on the theme:

```razor
<MokaThemeProvider Theme="MokaTheme.Default.WithDialogs(MokaDialogLook.IconLead)">
    @Body
</MokaThemeProvider>
```

Or on one dialog, which wins over the theme:

```razor
<MokaDialog @bind-Open="_open" Look="MokaDialogLook.Centered"
            Icon="MokaIcons.Action.Delete" IconColor="MokaColor.Error" Title="Delete 3 files?">
    <ChildContent>report.pdf, notes.md and photo.jpg go to the trash.</ChildContent>
    <Actions>
        <MokaButton Variant="MokaVariant.Text" OnClick="@(() => _open = false)">Cancel</MokaButton>
        <MokaButton Variant="MokaVariant.Filled" Color="MokaColor.Error" OnClick="DeleteAsync">Delete</MokaButton>
    </Actions>
</MokaDialog>
```

A service dialog takes `Look` and `IconColor` from its options. Without an `IconColor` the tile follows
`ConfirmColor`, so a confirm with a red button shows its icon in the error tint:

```csharp
bool delete = await DialogService.ConfirmAsync("They go to the trash.", "Delete 3 files?", o =>
{
    o.Look = MokaDialogLook.IconLead;
    o.Icon = MokaIcons.Action.Delete;
    o.ConfirmText = "Delete";
    o.ConfirmColor = MokaColor.Error;
});
```

- `IconLead` and `Centered` draw a tile only when the dialog has an `Icon`. Without one they lay out like `Standard`.
- `Centered` draws the main action on top, but the markup keeps the order you wrote, Cancel first. A dialog
  without a close button then still opens with focus on the safe choice, and a screen reader hears the buttons
  in the same order as in every other look. Tab moves up the stack. Header actions sit under the title, and the
  close button moves to the top corner.
- `Terminal`'s `>` is decoration: it is left out of the name the title gives the dialog. It styles only the
  `MokaButton`s in `Actions`.
- Every look keeps `Position`, `FullScreen`, `Draggable`, `Resizable` and `BodyPadding="false"` working. The
  buttons row wraps when it runs out of room.

## Draggable Dialog

The header drags the dialog with a mouse, a pen or a finger, and the dialog stays under the
pointer from the first move. It stays on screen: its top edge cannot leave the top of the viewport,
and at least 40px of it stay visible on the other sides. Up to 0.1.12 the first drag made the
dialog jump by half its size, and a draggable dialog opened with its corner at the middle of the
screen for the length of the open animation.

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
- Focus starts on an element marked `autofocus` or `data-autofocus` when the content has one, and otherwise on the first control in the body, then the first action, never the close button (see [Where focus starts](#where-focus-starts)). Content that already moved focus inside the dialog keeps it. A service prompt starts in its text field, and a confirm on Cancel.
- `Busy` marks the box `aria-busy` and keeps it open while its action runs. A service confirm's failure is a `role="alert"`, read out as it appears.
- A control that turns disabled while it has focus, such as a button showing its loading state, used to drop focus to the page behind the dialog, where Tab then walked the page. Focus now goes to the dialog itself and back to the control once it is enabled again, so after a failed attempt the person is on the button they pressed.
- A dialog rendered with `Open="true"` from the start traps focus from its first render.
- A press on text in the dialog focuses the dialog box, and a press on the backdrop leaves focus where it is, so focus stays inside the dialog.
- Escape closes the dialog unless `CloseOnEscape` is `false`.
- The dialog is drawn inside the page's markup, so its keys reach the page. It stops Blazor's `@onkeydown` at its edge, and hotkeys from `IMokaHotkeyService` don't fire while it is open, but a custom key event or a page script still sees them. A page that handles keys itself listens with `@onmokakeydown` and skips `Owned` presses (see [Hotkeys](/components/hotkeys#page-shortcuts-with-onmokakeydown)).
- The close button shows only a cross, so it is named "Close" by `aria-label` and by a `MokaTooltip` below it, lined up with the dialog's edge, the same box as the tooltips an app draws around its own `HeaderActions`.

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

### A confirm that does the work

Hand the confirm its work through `OnConfirmAsync`, and the dialog stays open while it runs:

```csharp
bool deleted = await Dialog.ConfirmAsync("They go to the trash for everyone.", "Delete 9 items?", o =>
{
    o.ConfirmText = "Delete";
    o.ConfirmColor = MokaColor.Error;
    o.OnConfirmAsync = token => Library.DeleteAsync(selection, token);
});
```

- The confirm button shows its loading state, and Escape, the backdrop and the close button can't close the dialog.
- If the work throws, the dialog stays open and shows the exception's message under its text, so the person can
  press the confirm button again. `FailedText` says something else instead.
- Success closes the dialog and the call returns `true`.
- Cancel cancels the token, and the dialog closes once the work has stopped: the call returns `false`, or `true` if
  the work finished anyway. The token is cancelled too when the dialog is closed through the service (`Close`,
  `CloseAll`) or its host goes away.
- Only `ConfirmAsync` runs it; a prompt, `ShowAsync` and component dialogs ignore it. Without it a confirm closes as
  soon as it is confirmed, as before.

| Option | Type | Default | Description |
|---|---|---|---|
| `OnConfirmAsync` | `Func<CancellationToken, Task>?` | - | The work the confirm button runs before the dialog closes |
| `FailedText` | `string?` | - | What the dialog says when the work fails. Null shows the exception's message, or "Something went wrong." when it has none |
| `InitialFocus` | `MokaDialogInitialFocus` | `Content` | Where focus starts; a confirm starts on Cancel |

### PromptAsync

Returns the entered string, or `null` if cancelled. Enter in the field confirms, as the confirm button does, and Escape cancels.

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
