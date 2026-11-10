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
| `Filter` / `@bind-Filter` | `string?` | The text in the filter box, to fill or clear it from outside or to replace it |
| `ItemFilter` | `Func<TItem, bool>?` | Hides items without reading the folder again. Asked once per item on every render of the parent, so whatever it reads can change freely |
| `Controls` | `MokaFileControls` | Which built-in toolbar controls show. Default `All` |
| `IconOnlyActions` | `bool` | Draws New folder and Upload as icons with tooltips, for a narrow panel |
| `Keys` | `MokaFileKeys` | Which keys it takes. Default `All` |
| `KeyScope` | `MokaFileKeyScope` | Where focus has to be for it to take them, `List` by default or `Browser` |
| `ResizableColumns` | `bool` | Lets the columns after the name be resized. Default off |
| `ColumnWidths` / `@bind-ColumnWidths` | `IReadOnlyDictionary<string, double>?` | The widths given so far, in CSS pixels by column `Id`, to store and hand back |
| `MinColumnWidth` | `double` | The narrowest a resized column can be, and the narrowest resizing leaves the name. Default 50 |
| `KeepListOnLoadFailure` | `bool` | Keeps the list on screen when a folder can't be read, and shows an alert. Off, the problem replaces the list |
| `ReselectOnReturn` | `bool` | Going Back, Forward or Up selects the folder just left |
| `FitWidths` | `MokaFileFitWidths?` | Where `Medium` and `Wide` columns start, in rem. Null is 36 and 52 |
| `ByteUnits` | `MokaByteUnits` | How sizes are counted in the column, the tiles and the status bar. In `Moka.Red.Feedback.JobProgress` |
| `ByteFormatter` | `Func<double, string>?` | Writes a size, unit included. Wins over `ByteUnits` |
| `DragText` | `Func<TItem, string>?` | The text a dragged item carries to other apps and other parts of the page. Null is the row's text |
| `BoxSelect` | `bool` | Lets a mouse press on the list's empty space draw a box that selects every item it touches, rows that aren't drawn included. Default off |

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
| `FilePicker` | `IMokaFilePicker?` | Picks the files Upload sends in place of the browser's dialog. Wins over a registered `IMokaFilePicker`. See [Your own file picker](#your-own-file-picker) |
| `Download` | `Func<IReadOnlyList<TItem>, Task>?` | The menu. Doesn't reload |
| `OnOpenFile` | `EventCallback<TItem>` | Double click or Enter on a file. Folders open in the browser |
| `OnOpenFiles` | `EventCallback<IReadOnlyList<TItem>>` | Enter when several items are selected and all are files, in place of opening the one under the cursor |
| `ConfirmDelete` | `Func<IReadOnlyList<TItem>, Task<bool>>?` | Asks before deleting, in your words. Null shows the browser's own dialog |
| `ConfirmDrop` | `Func<IReadOnlyList<TItem>, string, Task<bool>>?` | Asks before a drag moves items, given the target's key. Cut and paste doesn't ask |
| `ValidateName` | `Func<string, string?>?` | Checks a new name, returning an error message or null |
| `ItemMenu` | `Func<MokaFileMenuContext<TItem>, IReadOnlyList<MokaContextMenuItem>>?` | Builds the menu of the items right-clicked |
| `BackgroundMenu` | `Func<MokaFileMenuContext<TItem>, IReadOnlyList<MokaContextMenuItem>>?` | Builds the menu of the empty space |

### Slots

| Name | Type | Description |
|------|------|-------------|
| `Side` | `RenderFragment?` | A pane beside the list. In a narrow browser a button opens it in a drawer |
| `Details` | `RenderFragment<IReadOnlyList<TItem>>?` | A pane about the selection. It receives none when nothing is selected |
| `ToolbarStart` | `RenderFragment?` | More controls at the start of the first toolbar row |
| `ToolbarEnd` | `RenderFragment?` | More controls at the end of the second toolbar row |
| `ToolbarControl` | `RenderFragment<MokaFileControlContext>?` | Draws each built-in control in its place, to replace it or add beside it |
| `NameCell` | `RenderFragment<MokaFileNameContext<TItem>>?` | Draws what follows the icon in a list row |
| `Empty` | `RenderFragment<MokaFileEmptyContext>?` | Shows in place of the list when it has nothing to show |
| `FailureActions` | `RenderFragment<MokaFileFailure>?` | Buttons at the end of the failure alert |
| `FailureTitle` | `Func<MokaFileFailure, string?>?` | Words the alert's title. Null, or null returned, keeps it |
| `StatusEnd` | `RenderFragment?` | More `MokaStatusBarItem`s at the end of the status bar |
| `Problem` | `RenderFragment<MokaFileProblem>?` | Shows in place of the list when a folder can't be read |

`Class`, `Style`, `Id`, other attributes, `Margin`, `Padding` and `Rounded` land on the root.

### Events

| Name | Type | Raised |
|------|------|--------|
| `OnBusyChanged` | `EventCallback<bool>` | When `IsBusy` changes |
| `OnHeldChanged` | `EventCallback<MokaFileHeld<TItem>?>` | When what is held for the next paste changes |
| `OnCopyUnavailable` | `EventCallback` | By Ctrl+C on some items when there is no `Copy`, to tell the person why nothing happened |
| `OnFailureChanged` | `EventCallback<MokaFileFailure?>` | When the failure alert appears, changes or closes. Null once it has closed |

### Through a reference

| Member | Description |
|--------|-------------|
| `ReloadAsync()` | Reads the folder again, for changes made elsewhere |
| `NavigateAsync(folder, select)` | Goes to a folder, and selects the item with the key `select` once it arrives |
| `RevealAsync(key)` | Selects a row and scrolls to it, clearing a filter that hides it. While a folder is loading it waits for it |
| `Held` | What is held, as `MokaFileHeld<TItem>` (`Items`, `IsCut`). Null when nothing is |
| `PasteAsync(folder)` | Pastes what is held. Null is the folder on screen |
| `HoldAsync(items, cut)` | Holds items for the next paste, as cutting or copying them would |
| `ClearHeldAsync()` | Lets go of what is held |
| `ResetHistoryAsync()` | Starts Back and Forward again from the folder on screen |
| `ReportFailureAsync(error)` | Says something failed, for an app that dealt with it without throwing |
| `IsBusy` | Whether a folder is loading or an action is running |

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
| `Mono` | Monospace with tabular digits. Lines the column up on the end too, unless `Align` says otherwise |
| `Align` | `MokaTextAlign?`. Null is the end for the built-in columns and `Mono` ones, and the start for the rest |
| `Fit` | `Always`, `Medium` (from 36rem of the browser's own width) or `Wide` (from 52rem). Default `Wide` for your own columns |

`FitWidths` moves the two widths. Container queries can't read a variable, so widths you choose are applied by the script.

```razor
<MokaFileBrowser TItem="Entry" ... FitWidths="new MokaFileFitWidths(Medium: 24, Wide: 40)" />
```

## Resizing columns

`ResizableColumns` puts a handle on the edge before each column. Drag it, or Tab to it and use the arrows (Shift for a bigger step, Home for the narrowest, End for the widest). A double click gives the column its own width back. The name takes what is left, and resizing stops at `MinColumnWidth`, for the name as well as the column.

The browser stores nothing. `@bind-ColumnWidths` gives you every width as a dictionary to keep, with the column's `Id` as the key.

```razor
<MokaFileBrowser TItem="Entry" ... ResizableColumns @bind-ColumnWidths="_widths" MinColumnWidth="64" />

@code {
	private IReadOnlyDictionary<string, double> _widths = Settings.Load();
}
```

The heading row leaves room for the list's scrollbar through `--moka-file-browser-gutter`, which the browser sets on the row from what the scrollbar measures. An app that replaces that row's padding can add it back.

## More in the name cell

`NameCell` draws what follows the icon in a list row. `Name` is the name as the browser draws it. Tiles and a row being renamed don't use it.

```razor
<MokaFileBrowser TItem="Entry" ...>
	<NameCell Context="cell">
		@cell.Name
		@if (cell.Item.LinkTarget is { } target)
		{
			<span class="link-target">&rarr; @target</span>
		}
	</NameCell>
</MokaFileBrowser>
```

## Keeping the view

`MokaFileView` is a plain record, so it can go straight into settings. Its `FoldersFirst` is on unless you turn it off.

```razor
<MokaFileBrowser TItem="Entry" ... View="_view" ViewChanged="SaveViewAsync" />

@code {
	private MokaFileView _view = new(FoldersFirst: false);
}
```

## Choosing the toolbar

`Controls` says which built-in controls show. Without New folder, Upload, the sort and the view buttons the second row goes, which helps in a panel a few hundred pixels wide.

```razor
<MokaFileBrowser TItem="Entry" ... IconOnlyActions
                 Controls="MokaFileControls.All & ~(MokaFileControls.Sort | MokaFileControls.ViewMode)" />
```

`ToolbarControl` is asked for each control that shows, in its place. `Which` says which one, `Enabled` and `Run` are what it would do, and `Default` is the built-in control, to keep it. `Run` is null for the path, filter, sort and view controls: drive those with `NavigateAsync`, `@bind-Filter` and `@bind-View`.

```razor
<MokaFileBrowser TItem="Entry" ... @ref="_browser" @bind-Filter="_filter">
	<ToolbarStart>
		<MokaButton Quiet StartIcon="MokaIcons.Navigation.Home" aria-label="Home"
		            OnClick="() => _browser!.NavigateAsync(HomeKey)" />
	</ToolbarStart>
	<ToolbarControl Context="control">
		@if (control.Which == MokaFileControls.Path)
		{
			<PathEditor Path="@_folder" OnGo="path => _browser!.NavigateAsync(path)" />
		}
		else if (control.Which == MokaFileControls.Filter)
		{
			<MokaTextField @bind-Value="_filter" Placeholder="Filter" data-moka-file-filter="" />
		}
		else
		{
			@control.Default
		}
	</ToolbarControl>
</MokaFileBrowser>
```

Give your own filter field `data-moka-file-filter` and `/` finds it. Your own path keeps taking drops if its parts carry `data-moka-file-target`.

## Hiding items

`ItemFilter` hides items without another `Load`, such as dot files. The item counts leave out what it hides.

```razor
<MokaFileBrowser TItem="Entry" ... ItemFilter="entry => _showHidden || !entry.Name.StartsWith('.')" />
```

## An empty list

`Empty` replaces the empty state. `Filter` is the text in the box, `Hidden` counts what `ItemFilter` hides, `ClearFilter` empties the box and `Default` is what the browser would show.

```razor
<MokaFileBrowser TItem="Entry" ...>
	<Empty Context="empty">
		@if (empty.Hidden > 0 && empty.Filter.Length == 0)
		{
			<MokaEmptyState Title="Only hidden items">
				<MokaButton OnClick="() => _showHidden = true">Show them</MokaButton>
			</MokaEmptyState>
		}
		else
		{
			@empty.Default
		}
	</Empty>
</MokaFileBrowser>
```

## Selecting from outside

`RevealAsync` selects a row and scrolls to it. Called while a folder is loading, it waits for the folder to arrive, and `NavigateAsync` takes the key to select with the folder to go to.

```razor
<MokaFileBrowser TItem="Entry" ... @ref="_browser" ReselectOnReturn />

@code {
	private MokaFileBrowser<Entry>? _browser;

	private async Task SaveAsAsync(Entry entry)
	{
		await Files.SaveAsync(entry);
		await _browser!.NavigateAsync(entry.Folder, select: entry.Path);
	}
}
```

## Cut and copy from outside

`Held` says what is waiting for a paste and whether it was cut, and `OnHeldChanged` says when that changes. `PasteAsync` takes a folder to paste into, `ClearHeldAsync` lets go. Pressing Ctrl+C with no `Copy` raises `OnCopyUnavailable`, so you can say why nothing happened.

```razor
<MokaFileBrowser TItem="Entry" ... @ref="_browser" Move="MoveAsync"
                 OnCopyUnavailable="@(() => Toast.Show("This connection can only move files", MokaToastSeverity.Warning))" />

<MokaButton Disabled="_browser?.Held is null" OnClick="() => _browser!.PasteAsync(_archive)">Paste into Archive</MokaButton>
```

`HoldAsync` holds items from code, including items from another folder, so an app can keep its own cut and copy commands and use the browser's paste. It does nothing for a cut without a `Move` or a copy without a `Copy`.

```razor
<MokaButton OnClick="() => _browser!.HoldAsync(_picked, cut: true)">Cut</MokaButton>
```

## Asking before a drag

`Move` runs for both a drag and a paste. A drag is the one that can happen by accident, so `ConfirmDrop` asks before that one alone. Return false to leave the items where they are.

```razor
<MokaFileBrowser TItem="Entry" ... Move="MoveAsync" ConfirmDrop="AskAsync" />

@code {
	private Task<bool> AskAsync(IReadOnlyList<Entry> entries, string folder) =>
		Dialogs.ConfirmAsync($"Move {entries.Count} items?");
}
```

## Starting history over

The browser remembers the folder it was mounted on. An app that mounts it on a placeholder while it finds the real start calls `ResetHistoryAsync` once the real folder has reached the browser, and Back has nothing to go back to. It works while a kept list is still waiting for that folder too.

```razor
@code {
	private async Task ShowStartAsync(string start)
	{
		_folder = start;
		StateHasChanged();
		await _browser!.ResetHistoryAsync();
	}
}
```

## When an action fails

A throw shows in the alert above the list and keeps what was cut. `FailureActions` adds buttons to that alert. `Error` is what went wrong, and `Retry` runs what failed again.

```razor
<MokaFileBrowser TItem="Entry" ...>
	<FailureActions Context="failure">
		@if (failure.Error is UnauthorizedAccessException)
		{
			<MokaButton Size="MokaSize.Xs" OnClick="() => ElevateThenAsync(failure.Retry)">Try as admin</MokaButton>
		}
	</FailureActions>
</MokaFileBrowser>
```

An action that deals with a failure itself instead of throwing calls `ReportFailureAsync()`, so what was cut stays held. Pass the exception to show it in the browser's alert. The folder still reloads, since part of the action may have worked.

```razor
@code {
	private async Task MoveAsync(IReadOnlyList<Entry> entries, string folder)
	{
		if (!await Files.TryMoveAsync(entries, folder))
		{
			Notices.Show("Some files couldn't be moved");
			await _browser!.ReportFailureAsync();
		}
	}
}
```

## When a folder can't be read

By default the problem replaces the list. With `KeepListOnLoadFailure` the list stays, the folder doesn't change, and an alert says what went wrong with `FailureActions` beside it. The folder on screen changes only once the new one has arrived, and a `Folder` set from outside that can't be read is reported back through `FolderChanged`.

The alert's failure carries `Folder`, the key of the folder that didn't open (null when an action failed), and `Title`, the title it would show. `FailureTitle` words it, and `OnFailureChanged` says when the alert appears and closes.

```razor
<MokaFileBrowser TItem="Entry" ... KeepListOnLoadFailure
                 FailureTitle="@(failure => failure.Folder is { } key ? $"{NameOf(key)} didn't open" : null)"
                 OnFailureChanged="failure => _showHelp = failure is not null" />
```

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

Focus stays on the list, with one tab stop, and the row under the cursor is announced through `aria-activedescendant`. A shortcut matches only its exact modifiers, so Ctrl+Shift+X is not Ctrl+X. With text selected on the page, Ctrl+C copies the text.

After a delete the cursor moves to the row that took the deleted one's place, and focus comes back to the list if the action took it away.

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

`Keys` says which of these it takes. A key left out reaches the page as if the browser weren't there, and the menu stops showing it as a hint.

```razor
<MokaFileBrowser TItem="Entry" ... Keys="MokaFileKeys.All & ~(MokaFileKeys.Duplicate | MokaFileKeys.EscapeDropsHeld)" />
```

| Flag | Keys |
|------|------|
| `Move` | Arrows, Home, End, Page Up, Page Down, with Shift or Ctrl where the table above has them |
| `Toggle` | Space |
| `SelectAll` | Ctrl+A |
| `Open` | Enter |
| `Up` | Backspace |
| `History` | Alt+Left, Alt+Right |
| `Rename` | F2 |
| `Delete` | Del |
| `Copy`, `Cut`, `Paste`, `Duplicate` | Ctrl+C, Ctrl+X, Ctrl+V, Ctrl+D |
| `Escape` | Esc |
| `EscapeDropsHeld` | Whether Esc also lets go of what is cut or copied |
| `Refresh` | F5 |
| `Filter` | `/` |
| `TypeAhead` | Letters |

`KeyScope` says where focus has to be. `List` is the default. With `Browser` the keys work from anywhere inside it, the toolbars and panes included, except in a field or a control with keys of its own, such as a menu. Enter and Space stay with a focused button or link.

```razor
<MokaFileBrowser TItem="Entry" ... KeyScope="MokaFileKeyScope.Browser" />
```

On a touch screen a tap opens, a long press selects and shows the menu, and once something is selected a tap adds to it.

## Box selection

`BoxSelect` lets people select with a box, as in a file manager: a mouse press on the list's empty space and a drag draw a rectangle, and every row or tile it touches is selected while it moves. Shift adds to the selection, Control or Command flips what the box touches, and Esc puts the selection back as it was. Near the top or bottom of the list it scrolls, and the box keeps its start, so it reaches rows that `Virtualize` hasn't drawn: the browser knows its own rows and tile columns and selects them by position.

```razor
<MokaFileBrowser TItem="Entry" ... BoxSelect @bind-Selected="_selected" />
```

A press on a row or tile keeps its click, drag and menu, and touch keeps scrolling. In tiles the gaps between tiles are empty space. In the list a row takes the whole width, as in Windows Explorer's details view, so a box starts from the space under the last row or, in a long folder, from the tiles view. `SelectedChanged` is raised once, when the box is released, not on every move. The click that ends a drag doesn't clear the selection. The same box is available for any content as [Selection Area](/components/selection-area).

## Narrow

The browser reacts to its own width rather than the window's, so it fits a panel. `Modified` and your own columns fold away first, then the side and details panes move into drawers opened from the toolbar.

## Files dropped from outside

Files dropped anywhere on the browser go to `Upload` for the folder on screen. For a drop anywhere in the window, put a `MokaDropOverlay` on the page and call `ReloadAsync()` after your upload.

## Your own file picker

The Upload button and menu item click a hidden file input, which opens the browser's file dialog. With an `IMokaFilePicker` registered, or one passed to `FilePicker`, they ask it instead, for several files of any type, and the files go to `Upload` with the folder that was on screen when Upload was pressed, then the folder reloads. A Blazor Hybrid app on Windows wants this, since WebView2's file dialog can end the app. [File Picker](/components/file-picker) has a complete MAUI picker.

```razor
<MokaFileBrowser TItem="Entry" ... Upload="UploadAsync" FilePicker="_picker" />
```

An empty list, or an `OperationCanceledException`, is a cancel. Any other exception shows in the failure alert, titled `Text.ActionFailed`, and its retry asks the picker again. While the picker is open, another press of Upload does nothing. Dropped files still arrive through the browser, and with a picker the drop layer cancels a click, so the moment it stays up after a drag that ended off the page can't open the browser's dialog either.

## Dragging out

A dragged row carries its text, which other apps and your own drop targets can read. `DragText` sets that text, such as a full path. It also makes rows draggable when there is no `Move`.

```razor
<MokaFileBrowser TItem="Entry" ... DragText="entry => entry.Path" />
```
