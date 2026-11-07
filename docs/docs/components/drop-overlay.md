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

Read the files before `OnFiles` returns. The overlay then swaps its file input for a fresh one, so dropping the same file twice still counts, and a file can only be read through the input that took it.

## How it works

Its module listens on the window for `dragenter`, `dragleave`, `dragover` and `drop`, only while the overlay is on the page and not disabled, and shows the layer itself, so a drag costs no round trips to .NET. Only a drag that carries files shows it: dragging text or a link around the page does nothing. The layer holds a file input over the whole window, so the drop lands on it and reaches the app the way a pick from the file dialog does. A drop that misses it is cancelled, so the browser never opens the file in the tab.

A drop skips the file dialog's type filter, so `Accept`, `MaxFileSize`, `Multiple` and `MaxFiles` are all checked in code, as `MokaFileUpload` does. If the drag ends outside the page without a `dragleave` (Escape, in some browsers), the layer hides after a quiet second.

## Screen readers

Dragging files is a pointer gesture, so the panel is hidden from screen readers and the file input is out of the tab order. Offer the same import through a button with a file picker, such as `MokaFileUpload`.
