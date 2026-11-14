---
title: Toast
description: Short messages that come and go, with handles to update them, tracked tasks, progress, repeats, and a countdown that waits while nobody can read it.
order: 10
---

# Toast

`IMokaToastService` shows short messages that come and go, and `MokaToastHost` draws them. Every call returns a handle,
so a toast can report progress, turn into a result, or go away when the work it describes is gone. A toast counts down
in the browser, as the animation of the bar along its edge, and the countdown waits while the pointer is over the toasts,
focus is in them, or the tab is hidden, so nothing goes before someone could read it.

## Setup

Place `MokaToastHost` once in the application layout:

```razor
@* MainLayout.razor *@
<MokaToastHost />
```

`AddMokaRed()` registers the service, as `AddMokaFeedback()` does on its own. The service is safe to call from any
thread, for example after `Task.Run` or from a background job; the host reads the toasts on the renderer's thread.

## MokaToastHost Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Position` | `MokaToastPosition` | `TopRight` | Screen position of the toast container |
| `MaxVisible` | `int` | `5` | How many toasts show at once, the newest. Older ones wait their turn without counting down |
| `Look` | `MokaToastLook?` | `null` | How the toasts are drawn. Null takes the theme's choice. See [Looks](#looks) |
| `Stacking` | `MokaToastStacking?` | `null` | `List` or `Pile`. Null takes the theme's choice. See [Looks](#looks) |
| `ProgressLook` | `MokaToastProgressLook?` | `null` | How progress shows, unless a toast sets its own. See [Progress](#progress) |
| `Text` | `MokaToastText` | English | Every word the host writes, such as "Close". See [Words](#words) |
| `OffsetTop` | `string?` | `null` | A CSS length kept clear along the top of the window, for the top positions. See [Keeping toasts off part of the window](#keeping-toasts-off-part-of-the-window) |
| `OffsetRight` | `string?` | `null` | A CSS length kept clear along the right edge, for the right and centred positions |
| `OffsetBottom` | `string?` | `null` | A CSS length kept clear along the bottom, for the bottom positions |
| `OffsetLeft` | `string?` | `null` | A CSS length kept clear along the left edge, for the left and centred positions |
| `Class` | `string?` | `null` | Additional CSS classes on the host, after its own |
| `Style` | `string?` | `null` | Inline styles on the host, after the offsets |

### MokaToastPosition Values

`TopRight`, `TopLeft`, `TopCenter`, `BottomRight`, `BottomLeft`, `BottomCenter`

## IMokaToastService

| Member | Description |
|--------|-------------|
| `Show(message, severity, configure?)` | Shows a toast and returns a `MokaToastHandle` |
| `ShowSuccess`, `ShowError`, `ShowWarning`, `ShowInfo` | The same with the severity named |
| `Update(id, change)` | Changes a toast in place; `false` when it has gone |
| `Dismiss(id, reason?)` | Takes a toast away |
| `Clear()` | Takes every toast away |
| `Track(task, loading, success, failure, cancelled?, configure?)` | One toast from loading to result. See [Tracking a task](#tracking-a-task) |
| `Toasts` | The toasts showing or waiting, oldest first, as an immutable snapshot |
| `Changed` | Raised after every change, on the thread that made it |
| `Expire(id, run)` | For hosts: a countdown ended |

`MokaToastHandle` has `Id`, `IsShowing`, `Update(change)` and `Dismiss()`.

## MokaToastOptions

`configure` gets a fresh `MokaToastOptions`, and `Update` one holding the toast as it is.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Message` | `string` | from `Show` | The body text |
| `Severity` | `MokaToastSeverity` | from `Show` | Color and icon |
| `Title` | `string?` | - | Bold title above the message |
| `Duration` | `TimeSpan?` | 5 s | How long it shows, counted only while it can be read. Null keeps it until closed |
| `ShowCloseButton` | `bool` | `true` | Renders the close button |
| `ShowIcon` | `bool` | `true` | Shows the severity icon |
| `Icon` | `MokaIconDefinition?` | - | An icon in place of the severity's |
| `OnClick`, `OnClickAsync` | `Action?`, `Func<Task>?` | - | Runs on a click on the toast. Without one the toast listens to no clicks |
| `ActionText` | `string?` | - | The action button's words |
| `OnAction`, `OnActionAsync` | `Action?`, `Func<Task>?` | - | Runs when the action button is pressed |
| `CloseOnAction` | `bool` | `true` | The toast goes once its action has run |
| `Key` | `string?` | - | Keeps one toast per key. See [Repeats](#repeats) |
| `Repeats` | `MokaToastRepeats` | `Stack` | What a repeat does. See [Repeats](#repeats) |
| `Progress` | `MokaToastProgress?` | - | A share from 0 to 1, or `MokaToastProgress.Indeterminate`. See [Progress](#progress) |
| `ProgressLook` | `MokaToastProgressLook?` | - | This toast's progress look |
| `AlsoInCenter` | `bool` | `false` | Also puts the toast in the [notification center](/components/notification) |
| `Content` | `RenderFragment?` | - | Anything more under the message |
| `OnDismissed` | `Action<MokaToastDismissReason>?` | - | Runs once when the toast goes, with why: `Expired`, `Closed`, `Swiped`, `Action` or `Dismissed` |

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

## With a Title, and One That Stays

```blazor-preview
@inject IMokaToastService Toast

<MokaButton OnClick="ShowWithTitle">With a title</MokaButton>
<MokaButton OnClick="ShowPersistent">Stays until closed</MokaButton>

@code {
    void ShowWithTitle() => Toast.ShowSuccess(
        "Your changes have been saved to the server.",
        o => o.Title = "Saved");

    void ShowPersistent() => Toast.ShowError(
        "Database connection lost.",
        o =>
        {
            o.Title = "Connection error";
            o.Duration = null;
        });
}
```

## Action Button

The action runs, then the toast goes, unless `CloseOnAction` is off. The button is a `type="button"`, so a toast inside a
form submits nothing.

```blazor-preview
@inject IMokaToastService Toast

<MokaButton OnClick="Delete">Delete</MokaButton>

@code {
    void Delete() => Toast.ShowInfo("Email moved to trash.", o =>
    {
        o.ActionText = "Undo";
        o.OnAction = () => Toast.ShowSuccess("Restored.");
    });
}
```

## Updating a Toast

`Show` returns a handle. `Update` hands the change a `MokaToastOptions` holding the toast as it is. New words or a new
severity start the countdown again and are read to screen readers; progress alone changes neither.

```blazor-preview
@inject IMokaToastService Toast

<MokaButton OnClick="SaveAsync">Save</MokaButton>

@code {
    async Task SaveAsync()
    {
        MokaToastHandle toast = Toast.ShowInfo("Saving", o => o.Progress = MokaToastProgress.Indeterminate);
        await Task.Delay(1200);
        toast.Update(o =>
        {
            o.Message = "Saved";
            o.Severity = MokaToastSeverity.Success;
            o.Progress = null;
        });
    }
}
```

## Tracking a Task

`Track` shows one toast while a task runs and turns it into its result. The task it returns finishes as yours does, so a
failure still reaches your code: the toast reports it, it never swallows it. A cancelled task takes the toast away, since
whoever cancelled knows; pass `cancelled` for words instead.

```blazor-preview
@inject IMokaToastService Toast

<MokaButton OnClick="ExportAsync">Export</MokaButton>

@code {
    async Task ExportAsync()
    {
        int rows = await Toast.Track(
            CountRowsAsync(),
            loading: "Exporting",
            success: n => $"Exported {n} rows",
            failure: ex => $"Export failed: {ex.Message}",
            configure: o => o.Title = "Report");
    }

    static async Task<int> CountRowsAsync()
    {
        await Task.Delay(1500);
        return 1284;
    }
}
```

## Progress

A toast with `Progress` doesn't count down; it stays until an update takes the progress away or something dismisses it.
Updates can come as fast as the work reports: the host draws at most one every 100 ms and always the latest, and only the
toast that changed redraws. `MokaToastProgress` holds a share from 0 to 1, or `Indeterminate`, and optionally the time
left; without it the toast estimates the time left from how fast the share has moved, once at least three seconds and five
percent have passed.

| `MokaToastProgressLook` | What it draws |
|-------------------------|---------------|
| `Spinner` | A spinner in the icon's place and a bar along the bottom edge. The default |
| `Ring` | A ring in the icon's place with the percent inside |
| `Detailed` | A bar under the message with the percent and the time left |
| `Pill` | The percent on the message's line. The default under the `Pill` and `Snackbar` looks |
| `BarOnly` | Only the bar along the bottom edge; the severity icon stays |

The looks draw on the new design; `MokaTheme.Dark` and `MokaTheme.Light` draw `Spinner`. Each look has one element with
`role="progressbar"`, named by the toast's message, with the percent in `aria-valuenow`.

```blazor-preview
@inject IMokaToastService Toast

<MokaButton OnClick="UploadAsync">Upload</MokaButton>

@code {
    async Task UploadAsync()
    {
        MokaToastHandle toast = Toast.ShowInfo("Uploading report.pdf", o =>
        {
            o.Progress = 0;
            o.ProgressLook = MokaToastProgressLook.Detailed;
        });

        for (int i = 1; i <= 20; i++)
        {
            await Task.Delay(150);
            double share = i / 20d;
            toast.Update(o => o.Progress = share);
        }

        toast.Update(o =>
        {
            o.Message = "Uploaded report.pdf";
            o.Severity = MokaToastSeverity.Success;
            o.Progress = null;
        });
    }
}
```

## Repeats

A toast repeats one already showing when both have the same `Key`, or, when neither has a key, the same severity, title
and message. `Repeats` says what happens:

| `MokaToastRepeats` | What it does |
|--------------------|--------------|
| `Stack` | Each call shows a toast of its own, as before. With a `Key`, it replaces instead, since a key keeps one toast per key |
| `Count` | The toast stays, shows a count ("×3") and starts its countdown again. Screen readers aren't told again |
| `Replace` | The newest words take the toast's place, without a count, and its countdown starts again |

```blazor-preview
@inject IMokaToastService Toast

<MokaButton OnClick="Ping">Lose the connection</MokaButton>

@code {
    void Ping() => Toast.ShowError("Connection lost", o => o.Repeats = MokaToastRepeats.Count);
}
```

## A Toast in Markup

`MokaToast` ties a toast to page state. It renders nothing where it stands: it hands the toast to the service, so the
host draws it in the same stack, with the same countdown and announcements. Changing its parameters updates the toast in
place, and when the toast goes on its own `OpenChanged` raises false. Its `ChildContent` renders under the message, and
its event handlers run on your component.

```blazor-preview
<MokaButton OnClick="() => _saved = true">Save</MokaButton>

<MokaToast @bind-Open="_saved" Message="Saved" Severity="MokaToastSeverity.Success"
           ActionText="Undo" OnAction="() => _undone++" />

<p>Undone @_undone times.</p>

@code {
    bool _saved;
    int _undone;
}
```

Only a change of `Open` acts, as with every parameter the library copies in, so bind it two-way, or pass false and then
true, to show it again after it went on its own.

## Timing and Reading

- The countdown is a CSS animation on the toast's timer bar. When it ends, the host's script tells .NET and the toast
  goes. No timer runs in .NET.
- It pauses while the pointer is over any toast, while focus is in one (WCAG 2.2.1), while a finger is swiping one, and
  while the browser tab is hidden. A hidden tab draws no frames, so without the pause every toast would end the moment
  the tab came back, unseen.
- Under reduced motion the countdown keeps its length and the bar hides; nothing moves.
- Toasts past `MaxVisible` wait without counting down, and count down once their turn comes.
- Screen readers hear each toast once, its title and message, through the [announcer](/components/announcer): errors at
  once, the rest politely. A repeat count isn't read again; new words and a tracked task's result are. The host carries
  the announcer's live regions, so nothing else is needed in the layout.
- On a touch screen a sideways swipe takes a toast away. The script listens to the move only while a finger is down, and
  .NET hears only the dismissal.

## Words

Every word the host writes comes from `Text`, a `MokaToastText`:

| Property | Default |
|----------|---------|
| `Close` | "Close" |
| `Working` | "Working", for progress nobody can measure |
| `Percent` | "40%" in the current culture |
| `TimeLeft` | "About 2 min left" |
| `CountBadge` | "×3" |
| `Repeated` | "shown 3 times", for screen readers |
| `TerminalTag` | "[ OK ]", "[WARN]", "[FAIL]", "[INFO]" |
| `Announcement` | the title and message |

```razor
<MokaToastHost Text="@(new MokaToastText { Close = "Fermer", Working = "En cours" })" />
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

## Looks

On the new design (every preset except `MokaTheme.Dark` and `MokaTheme.Light`) the toasts can take one of seven looks,
and several toasts can pile up instead of standing in a list. Both are separate choices, so any look can pile.

| `MokaToastLook` | What it draws |
|-----------------|---------------|
| `Standard` | A raised card. The severity shows in the icon and the bar counting down |
| `ToneEdge` | A thick edge in the severity's color down the start side |
| `Tinted` | The whole toast in the severity's soft color, with the title in its ink |
| `IconTile` | The icon in a tinted tile, and the action drawn as a small outlined button |
| `Snackbar` | One dark line on a light page, a lighter one on a dark page, with the action at its end |
| `Pill` | A small capsule centred on the host's top or bottom edge, whatever side `Position` names |
| `Terminal` | Monospace capitals on the page color, a tag such as `[FAIL]` in place of the icon, and a glow in the severity's color |

`Snackbar` and `Pill` show one line: the title, or the message when a toast has no title. A message under a title is
hidden from the eye only, so a screen reader still reads it, and the action and close button stay. Neither shows the bar
counting down, though it still runs and the toast closes on time. The `Terminal` tag is hidden from screen readers.

`MokaToastStacking.Pile` puts the newest toast in front, against the window's edge, with the two before it peeking out
behind it. Hovering the pile, or moving focus into it with Tab, fans it out into the full list, so every toast and its
buttons stay reachable from the keyboard. `MaxVisible`, the countdown, the close button and the actions work as they do
in a list.

Set them for the whole app on the theme:

```razor
<MokaThemeProvider Theme="_theme">
    <MokaToastHost />
</MokaThemeProvider>

@code {
    private readonly MokaTheme _theme = MokaTheme.Default
        .WithToasts(MokaToastLook.ToneEdge)
        .WithToastStacking(MokaToastStacking.Pile);
}
```

Or on the host, which wins over the theme:

```razor
<MokaToastHost Look="MokaToastLook.Snackbar" Stacking="MokaToastStacking.List" />
```

`MokaTheme.WithComponentStyle` sets the toast look together with the other feedback looks: `Soft` gives `IconTile` and
`Terminal` gives `Terminal`. On `MokaTheme.Dark` and `MokaTheme.Light` nothing changes, even with `Look` or `Stacking`
set on the host.

## Clearing All Toasts

```blazor-preview
@inject IMokaToastService Toast

<MokaButton OnClick='() => Toast.ShowInfo("Toast 1")'>Add</MokaButton>
<MokaButton Color="MokaColor.Error" Variant="MokaVariant.Outlined" OnClick='() => Toast.Clear()'>
    Clear All
</MokaButton>
```
