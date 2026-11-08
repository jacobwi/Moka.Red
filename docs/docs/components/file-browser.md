---
title: File Browser
description: A folder of items with navigation, a filter, sorting, list and tile views, selection, keyboard control and drag and drop.
order: 157
---

# File Browser

`MokaFileBrowser<TItem>` browses folders of anything your app can list: files on a disk, a remote server, a library in a database. You give it a way to load a folder and to describe an item. Each action you want to offer is a callback, and an action left null doesn't appear.

It owns history, selection, the keys, sorting, the filter and dragging. Permissions, tags, transfers and what gets stored stay in your app.

Its menus go through `MokaContextMenuHost` when the layout has one. Without one the browser draws its own.

## Setup

```razor
@using Moka.Red.Data.FileBrowser
```

## A minimal browser

```razor
<MokaFileBrowser TItem="Entry"
                 Load="LoadAsync"
                 Describe="Describe"
                 @bind-Folder="_folder"
                 Crumbs="_crumbs" />

@code {
	private string _folder = "/";
	private IReadOnlyList<MokaFileCrumb> _crumbs = [new("/", "Home")];

	private Task<IReadOnlyList<Entry>> LoadAsync(string folder, CancellationToken token) =>
		Files.ListAsync(folder, token);

	private static MokaFileInfo Describe(Entry entry) =>
		new(entry.Path, entry.Name, entry.IsFolder, entry.Length, entry.Changed);
}
```

`Crumbs` is the path to the folder on screen, the top first. Up goes to the second to last one, so keep it matching `Folder`.

## Parameters

### What it shows

| Name | Type | Description |
|------|------|-------------|
| `Load` | `Func<string, CancellationToken, Task<IReadOnlyList<TItem>>>` | Reads a folder. Cancelled when another load replaces it, and a late answer is ignored |
| `Describe` | `Func<TItem, MokaFileInfo>` | Tells the browser what an item is. Called once per item per load |
| `Folder` / `@bind-Folder` | `string` | The key of the folder on screen |
| `Crumbs` | `IReadOnlyList<MokaFileCrumb>` | The path, from the top. The last entry is the folder itself |
| `Selected` / `@bind-Selected` | `IReadOnlySet<string>?` | The keys of the selected items |
| `View` / `@bind-View` | `MokaFileView?` | The view, sort and direction, a record you can store |
| `Columns` | `IReadOnlyList<MokaFileColumn<TItem>>?` | The columns after the name. Null shows size and modified |
| `Text` | `MokaFileBrowserText` | Every word it writes |
| `Height` | `string?` | A CSS length. Default is the full height of its parent, at least 20rem |

### Actions

Each one left null hides what it offers. A throw shows as an error, and success reloads the folder.

| Name | Type | Offers |
|------|------|--------|
| `Rename` | `Func<TItem, string, Task>?` | F2 and the menu, in place. A throw shows under the field and keeps it open |
| `CreateFolder` | `Func<string, string, Task>?` | New folder, in place. Given the parent's key and the name |
| `Delete` | `Func<IReadOnlyList<TItem>, Task>?` | Del and the menu, after `ConfirmDelete` |
| `Move` | `Func<IReadOnlyList<TItem>, string, Task>?` | Dragging onto a folder, and cut and paste. Given the target's key |
| `Copy` | `Func<IReadOnlyList<TItem>, string, Task>?` | Copy, paste and duplicate. Given the target's key |
| `Upload` | `Func<IReadOnlyList<IBrowserFile>, string, Task>?` | The Upload button and dropping files on the browser. Read the files before the task ends |
| `Download` | `Func<IReadOnlyList<TItem>, Task>?` | The menu. Doesn't reload |
| `OnOpenFile` | `EventCallback<TItem>` | Double click or Enter on a file. Folders open in the browser |
| `ConfirmDelete` | `Func<IReadOnlyList<TItem>, Task<bool>>?` | Asks before deleting, in your words. Null shows the browser's own dialog |
| `ValidateName` | `Func<string, string?>?` | Checks a new name, returning an error message or null |
| `ItemMenu` | `Func<MokaFileMenuContext<TItem>, IReadOnlyList<MokaContextMenuItem>>?` | Builds the menu of the items right-clicked |
| `BackgroundMenu` | `Func<MokaFileMenuContext<TItem>, IReadOnlyList<MokaContextMenuItem>>?` | Builds the menu of the empty space |

### Slots

| Name | Type | Description |
|------|------|-------------|
| `Side` | `RenderFragment?` | A pane beside the list. In a narrow browser a button opens it in a drawer |
| `Details` | `RenderFragment<IReadOnlyList<TItem>>?` | A pane about the selection. It receives none when nothing is selected |
| `ToolbarEnd` | `RenderFragment?` | More controls at the end of the second toolbar row |
| `StatusEnd` | `RenderFragment?` | More `MokaStatusBarItem`s at the end of the status bar |
| `Problem` | `RenderFragment<MokaFileProblem>?` | Shows in place of the list when a folder can't be read |

`Class`, `Style`, `Id`, other attributes, `Margin`, `Padding` and `Rounded` land on the root.

`ReloadAsync()` reads the folder again, for changes made elsewhere.

## Actions

```razor
<MokaFileBrowser TItem="Entry" Load="LoadAsync" Describe="Describe"
                 @bind-Folder="_folder" Crumbs="_crumbs"
                 Rename="RenameAsync"
                 CreateFolder="CreateFolderAsync"
                 Delete="DeleteAsync"
                 Move="MoveAsync"
                 Upload="UploadAsync"
                 OnOpenFile="Open" />
```

## Asking before deleting

Without `ConfirmDelete` the browser asks with its own dialog, worded by `Text.DeleteTitle` and `Text.DeleteMessage`. To use your own wording or dialog, return whether to go ahead.

```razor
<MokaFileBrowser TItem="Entry" ... Delete="DeleteAsync" ConfirmDelete="AskAsync" />

@code {
	private Task<bool> AskAsync(IReadOnlyList<Entry> entries) =>
		Dialogs.ConfirmAsync($"Move {entries.Count} items to the trash?");
}
```

## Menus

`ItemMenu` and `BackgroundMenu` receive what the browser would show, in `Standard`, and return the menu. Add to it, reorder it, or leave things out.

```razor
<MokaFileBrowser TItem="Entry" ... ItemMenu="WithShare" />

@code {
	private IReadOnlyList<MokaContextMenuItem> WithShare(MokaFileMenuContext<Entry> context) =>
	[
		.. context.Standard,
		new MokaContextMenuItem { Text = "Share", OnClickSync = () => Share(context.Items) }
	];
}
```

## Columns

`Columns` replaces the columns after the name. Use `MokaFileSort.Size` and `MokaFileSort.Modified` for the built-in ones.

```razor
<MokaFileBrowser TItem="Entry" ... Columns="_columns" />

@code {
	private readonly IReadOnlyList<MokaFileColumn<Entry>> _columns =
	[
		new(MokaFileSort.Modified),
		new(MokaFileSort.Size),
		new("owner", "Owner")
		{
			Text = entry => entry.Owner,
			Compare = (a, b) => string.Compare(a.Owner, b.Owner, StringComparison.OrdinalIgnoreCase),
			Width = "7rem",
			Fit = MokaFileColumnFit.Medium
		}
	];
}
```

| Property | Description |
|----------|-------------|
| `Id` | Names the column in `MokaFileView.SortBy` |
| `Title` | The heading |
| `Text` | The cell as text |
| `Cell` | Draws the cell. Wins over `Text` |
| `Compare` | Makes the column sortable |
| `Width` | A CSS length. The name takes what is left |
| `Mono` | Monospace with tabular digits |
| `Fit` | `Always`, `Medium` (from 36rem of the browser's own width) or `Wide` (from 52rem). Default `Wide` for your own columns |

## Keeping the view

`MokaFileView` is a plain record, so it can go straight into settings.

```razor
<MokaFileBrowser TItem="Entry" ... View="_view" ViewChanged="SaveViewAsync" />
```

## When a folder can't be read

`Problem` replaces the error, for something like a locked library or a lost connection. It receives the exception and a way to try again.

```razor
<MokaFileBrowser TItem="Entry" ...>
	<Problem Context="problem">
		@if (problem.Error is LibraryLockedException)
		{
			<UnlockPrompt OnUnlocked="problem.Retry" />
		}
		else
		{
			<MokaCallout Type="MokaCalloutType.Caution" Title="Couldn't open this">@problem.Error.Message</MokaCallout>
		}
	</Problem>
</MokaFileBrowser>
```

## A side pane

`Side` holds whatever suits your app, such as a tree of folders. Any element inside the browser with `data-moka-file-target` set to a folder's key takes a dragged row, so your own places can receive drops.

```razor
<MokaFileBrowser TItem="Entry" ...>
	<Side>
		@foreach (Place place in _places)
		{
			<MokaButton Variant="MokaVariant.Text" data-moka-file-target="@place.Key"
			            OnClick="() => _folder = place.Key">@place.Name</MokaButton>
		}
	</Side>
</MokaFileBrowser>
```

## Words

Every word comes from `Text`. Change the ones you need.

```razor
<MokaFileBrowser TItem="Entry" ... Text="_text" />

@code {
	private readonly MokaFileBrowserText _text = new()
	{
		Label = "Dateien",
		NewFolder = "Neuer Ordner",
		ItemCount = count => count == 1 ? "1 Datei" : $"{count} Dateien"
	};
}
```

## Keyboard

Focus stays on the list, with one tab stop, and the row under the cursor is announced through `aria-activedescendant`.

| Key | Action |
|-----|--------|
| Arrows, Home, End, Page Up, Page Down | Move the cursor and select that row. In tiles, Left and Right move by one and Up and Down by a row |
| Shift and those | Extend the selection |
| Ctrl and those | Move the cursor only |
| Space | Toggles the row under the cursor |
| Ctrl+A | Selects everything showing |
| Enter | Opens |
| Backspace | Goes up a folder |
| Alt+Left, Alt+Right | Back and forward |
| F2 | Renames |
| Del | Deletes |
| Ctrl+C, Ctrl+X, Ctrl+V | Copy, cut and paste |
| Ctrl+D | Duplicates |
| Esc | Clears the selection and lets go of cut or copied items |
| F5 | Reloads |
| `/` | Goes to the filter, and Down comes back |
| Letters | Jump to the next name starting with them |

On a touch screen a tap opens, a long press selects and shows the menu, and once something is selected a tap adds to it.

## Narrow

The browser reacts to its own width rather than the window's, so it fits a panel. `Modified` and your own columns fold away first, then the side and details panes move into drawers opened from the toolbar.

## Files dropped from outside

Files dropped anywhere on the browser go to `Upload` for the folder on screen. For a drop anywhere in the window, put a `MokaDropOverlay` on the page and call `ReloadAsync()` after your upload.
