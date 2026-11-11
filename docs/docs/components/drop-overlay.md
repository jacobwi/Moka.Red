---
title: Drop Overlay
description: A layer over the whole window that appears while files are dragged over it and takes the drop.
order: 152
---

# Drop Overlay

`MokaDropOverlay` turns the whole window into a drop target. It stays hidden until files are dragged over the window, then covers it with a panel saying what to drop, and hands the dropped files to the app. Put one on a page, or in a layout for every page. For a drop zone inside a form, with a file list, use [`MokaFileUpload`](/components/forms).

## Setup

```razor
@using Moka.Red.Forms.DropOverlay
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Accept` | `string?` | -- | Accepted file types, such as `".conf,.zip,image/*"`. Every dropped file is checked against it |
| `Multiple` | `bool` | `true` | Whether a drop can bring several files. Off takes the first and turns the rest away |
| `MaxFiles` | `int` | `10` | The most files one drop hands over |
| `MaxFileSize` | `long` | 10 MB | The largest file accepted, in bytes |
| `OnFiles` | `EventCallback<IReadOnlyList<IBrowserFile>>` | -- | The files that passed every check. Not raised when none did |
| `OnRejected` | `EventCallback<IReadOnlyList<MokaRejectedFile>>` | -- | The files that failed a check, each with its `Reason`: `Type`, `Size` or `Count` |
| `Title` | `string` | `"Drop files here"` | The large line in the panel |
| `Hint` | `string?` | -- | A short line under it, such as the types it takes, in the monospace font |
| `Icon` | `MokaIconDefinition` | `MokaIcons.Action.Upload` | The icon above the title |
| `ChildContent` | `RenderFragment?` | -- | Replaces the icon, title and hint |
| `Disabled` | `bool` | `false` | Takes the listeners off, so a drop does what it does without the overlay |
| `Look` | `MokaDropOverlayLook` | `Standard` | How the layer covers the window: `Standard`, `Card`, `Frame`, `Corner`, `Wash` or `Banner` |
| `Destination` | `string?` | -- | Where the files go, such as a folder's name, in the looks other than `Standard` |
| `DestinationText` | `string` | `"to {0}"` | The words round `Destination`, which goes where `{0}` is |
| `CountTitle` | `string` | `"Drop {0} files"` | The title once the browser says how many files are dragged, in the looks other than `Standard` |
| `OneFileTitle` | `string` | `"Drop 1 file"` | The title when one file is dragged |
| `SizeLimitText` | `string` | `"{0} each"` | The size limit beside the accepted types on the `Card` look |

## Basic

```razor
<MokaDropOverlay Accept=".conf,.zip,image/*"
                 Title="Drop to import"
                 Hint=".conf · .zip · QR image"
                 OnFiles="ImportAsync"
                 OnRejected="SayWhy" />

@code {
    private async Task ImportAsync(IReadOnlyList<IBrowserFile> files)
    {
        foreach (IBrowserFile file in files)
        {
            await using Stream stream = file.OpenReadStream(maxAllowedSize: 1024 * 1024);
            // Read the file here.
        }
    }

    private void SayWhy(IReadOnlyList<MokaRejectedFile> files)
    {
        // A toast, say: "notes.txt isn't a type this app imports."
    }
}
```

Read the files before `OnFiles` returns. The overlay then swaps its file input for a fresh one, so dropping the same file twice still counts, and a file can only be read through the input that took it. `OnFiles` may read them for as long as it runs, even if the overlay is disabled meanwhile, such as while those files upload: the input stays until the callback returns.

## Looks

A drop anywhere on the window counts in every look; they differ in how much of the page they hide. `Standard`, the default, is the panel above, unchanged.

```razor
<MokaDropOverlay Look="MokaDropOverlayLook.Card"
                 Accept=".pdf,.png,.jpg"
                 Destination="@_folder.Name"
                 OnFiles="UploadAsync" />
```

- `Card` dims the page so it still shows, with a solid card saying how many files and where they go: "Drop 3 files to Q3 plans", then the accepted types from `Accept` and the size limit ("PDF", "PNG", "10 MB each").
- `Frame` leaves the page readable: an accent glow runs round the window's edge and a pill at the top says what a drop does.
- `Corner` changes nothing but a small target in the bottom corner, with `Destination`, else `Hint`, under its title. A drop anywhere still counts; the badge only says so.
- `Wash` turns the whole window the accent with large words. Impossible to miss, and it hides the page completely.
- `Banner` puts an accent outline round the page and a dark banner at the bottom naming the folder the files go to.

While files are dragged the browser says how many, but not their names or sizes, and the module puts the count in the title: `OneFileTitle` for one, `CountTitle` with the number in place of `{0}` for more. Until it does, and in a browser that doesn't say, `Title` shows. The count is written in the browser, so a drag still costs no round trips to .NET. `Destination` reads through `DestinationText` ("to {0}"). `ChildContent` replaces what any look draws inside its box, and `Padding` and `Rounded` go on that box.

```razor
<MokaDropOverlay Look="MokaDropOverlayLook.Banner"
                 Destination="Q3 plans"
                 Title="Drop files to upload them"
                 CountTitle="Drop {0} files to upload them"
                 OneFileTitle="Drop 1 file to upload it" />
```

## How it works

Its module listens on the window for `dragenter`, `dragleave`, `dragover` and `drop`, only while the overlay is on the page and not disabled, and shows the layer itself, so a drag costs no round trips to .NET. Only a drag that carries files shows it: dragging text or a link around the page does nothing. The layer holds a file input over the whole window, so the drop lands on it and reaches the app the way a pick from the file dialog does. A drop that misses it is cancelled, so the browser never opens the file in the tab.

A drop skips the file dialog's type filter, so `Accept`, `MaxFileSize`, `Multiple` and `MaxFiles` are all checked in code, as `MokaFileUpload` does. If the drag ends outside the page without a `dragleave` (Escape, in some browsers), the layer hides after a quiet second.

## Screen readers

Dragging files is a pointer gesture, so the panel is hidden from screen readers and the file input is out of the tab order. Offer the same import through a button with a file picker, such as `MokaFileUpload`.
