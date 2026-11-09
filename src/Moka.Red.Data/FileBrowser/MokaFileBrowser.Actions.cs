using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.ContextMenu;
using Moka.Red.Icons;

namespace Moka.Red.Data.FileBrowser;

public partial class MokaFileBrowser<TItem>
{
	private const long SecondaryButton = 2;

	private bool _busy;
	private bool _creating;
	private MokaFileFailure? _failure;
	private Func<Task>? _runningRetry;
	private bool _reportedFailure;
	private HeldItems? _held;
	private int _inputKey;
	private bool _menuFromKeyboard;
	private IReadOnlyList<MokaContextMenuItem> _menuItems = [];
	private bool _menuOpen;
	private double _menuX;
	private double _menuY;
	private string _newFolderName = "";
	private List<MokaFileRow<TItem>>? _pendingDelete;
	private bool _reloadAfterEdit;
	private string? _renamingKey;

	private bool IsEditing(MokaFileRow<TItem> row) => string.Equals(_renamingKey, row.Key, StringComparison.Ordinal);

	private void DismissFailure() => _failure = null;

	private void ShowFailure(Exception error, string title, Func<Task>? retry, string? folder = null) =>
		_failure = new MokaFileFailure(error, retry is null ? null : Redrawing(async () =>
		{
			_failure = null;
			await retry();
		}))
		{
			Folder = folder,
			Title = title
		};

	// One change at a time, so a second key press can't act on rows the first is about to change.
	// Says whether it worked: an app that dealt with a failure itself didn't throw, so returning isn't enough to tell.
	private async Task<bool> RunAsync(Func<Task> action, bool reload = true, Func<Task>? retry = null)
	{
		if (_busy)
		{
			return false;
		}

		_busy = true;
		_failure = null;
		_reportedFailure = false;
		_runningRetry = retry ?? (() => RunAsync(action, reload));
		StateHasChanged();
		bool threw = false;
		try
		{
			await action();
		}
#pragma warning disable CA1031 // Whatever the app's action throws is shown to the person, and the list stays as it was.
		catch (Exception ex)
#pragma warning restore CA1031
		{
			threw = true;
			ShowFailure(ex, Text.ActionFailed, _runningRetry);
		}
		finally
		{
			_busy = false;
			_runningRetry = null;
		}

		bool succeeded = !threw && !_reportedFailure;
		if (!threw && reload)
		{
			await LoadAsync(fresh: false);
		}

		return succeeded;
	}

	private void BeginRename(MokaFileRow<TItem>? row)
	{
		if (Rename is not null && row is not null)
		{
			_renamingKey = row.Key;
		}
	}

	private async Task SaveRenameAsync(MokaFileRow<TItem> row, string name)
	{
		await Rename!(row.Item, name);
		_reloadAfterEdit = true;
		_selectNameAfterLoad = name;
	}

	private async Task RenameClosedAsync(bool open)
	{
		if (!open)
		{
			_renamingKey = null;
			await FinishEditAsync();
		}
	}

	private void BeginCreateFolder()
	{
		if (CreateFolder is null)
		{
			return;
		}

		_renamingKey = null;
		_newFolderName = MokaFileNames.Free(Text.NewFolderName, _rows.Select(row => row.Info.Name));
		_creating = true;
	}

	private Task BeginCreateFolderAsync()
	{
		BeginCreateFolder();
		return Task.CompletedTask;
	}

	private async Task SaveNewFolderAsync(string name)
	{
		await CreateFolder!(CurrentFolder, name);
		_reloadAfterEdit = true;
		_selectNameAfterLoad = name;
	}

	private async Task NewFolderClosedAsync(bool open)
	{
		if (!open)
		{
			_creating = false;
			await FinishEditAsync();
		}
	}

	private async Task FinishEditAsync()
	{
		FocusListWhenDrawn();
		if (_reloadAfterEdit)
		{
			_reloadAfterEdit = false;
			await LoadAsync(fresh: false);
		}
	}

	private async Task RequestDeleteAsync(List<MokaFileRow<TItem>> rows)
	{
		if (Delete is null || rows.Count == 0 || _busy)
		{
			return;
		}

		if (ConfirmDelete is null)
		{
			_pendingDelete = [.. rows];
			return;
		}

		List<TItem> items = rows.ConvertAll(row => row.Item);
		if (await TryConfirmAsync(() => ConfirmDelete!(items)))
		{
			await DeleteRowsAsync(rows, items);
		}
	}

	private async Task DeleteRowsAsync(List<MokaFileRow<TItem>> rows, List<TItem> items)
	{
		_cursorAfterLoad = rows.Min(row => row.Index);
		FocusListWhenDrawn();
		await RunAsync(() => Delete!(items));
		_cursorAfterLoad = null;
	}

	private async Task<bool> TryConfirmAsync(Func<Task<bool>> ask)
	{
		_failure = null;
		try
		{
			return await ask();
		}
#pragma warning disable CA1031 // The app's own question failed, so nothing happens and the person hears why.
		catch (Exception ex)
#pragma warning restore CA1031
		{
			ShowFailure(ex, Text.ActionFailed, retry: null);
			return false;
		}
	}

	private void HandleDeleteDialog(bool open)
	{
		if (!open)
		{
			_pendingDelete = null;
		}
	}

	private async Task ConfirmPendingDeleteAsync()
	{
		if (_pendingDelete is not { } rows || Delete is null)
		{
			return;
		}

		_pendingDelete = null;
		await DeleteRowsAsync(rows, rows.ConvertAll(row => row.Item));
	}

	private void HoldForCopy(List<MokaFileRow<TItem>> rows) => Hold(rows, cut: false);

	private void HoldForMove(List<MokaFileRow<TItem>> rows) => Hold(rows, cut: true);

	private void Hold(List<MokaFileRow<TItem>> rows, bool cut)
	{
		if ((cut ? Move is not null : Copy is not null) && rows.Count > 0)
		{
			_held = new HeldItems([.. rows], rows.Select(row => row.Key).ToHashSet(StringComparer.Ordinal), cut);
		}
	}

	private async Task PasteIntoAsync(string target)
	{
		if (_held is not { } held)
		{
			return;
		}

		List<TItem> items = [.. held.Rows.Where(row => !string.Equals(row.Key, target, StringComparison.Ordinal)).Select(row => row.Item)];
		if (items.Count == 0)
		{
			return;
		}

		Func<Task> again = () => PasteIntoAsync(target);
		if (held.Cut && Move is not null)
		{
			if (await RunAsync(() => Move(items, target), retry: again) && ReferenceEquals(_held, held))
			{
				_held = null;
			}
		}
		else if (!held.Cut && Copy is not null)
		{
			await RunAsync(() => Copy(items, target), retry: again);
		}
	}

	private async Task DuplicateAsync(List<MokaFileRow<TItem>> rows)
	{
		if (Copy is null || rows.Count == 0)
		{
			return;
		}

		string target = CurrentFolder;
		List<TItem> items = rows.ConvertAll(row => row.Item);
		await RunAsync(() => Copy(items, target));
	}

	private async Task DownloadAsync(List<MokaFileRow<TItem>> rows)
	{
		if (Download is null || rows.Count == 0)
		{
			return;
		}

		List<TItem> items = rows.ConvertAll(row => row.Item);
		await RunAsync(() => Download(items), reload: false);
	}

	private async Task PickFilesAsync() => await SafeModuleInvokeVoidAsync(ModulePath, "pickFiles", _root);

	private async Task HandleFilesAsync(InputFileChangeEventArgs e)
	{
		if (Upload is null)
		{
			return;
		}

		IReadOnlyList<IBrowserFile> files = e.GetMultipleFiles(e.FileCount);
		string target = CurrentFolder;
		try
		{
			await RunAsync(() => Upload(files, target));
		}
		finally
		{
			// Chrome sends no change for a drop of the files the input already holds, so a new input takes the next one.
			_inputKey++;
		}
	}

	private async Task HandleItemMenu(MokaFileRow<TItem> row, MouseEventArgs args)
	{
		if (IsEditing(row))
		{
			return;
		}

		if (!_selection.Contains(row.Key))
		{
			await SelectOnlyAsync(row);
		}

		ShowMenu(args, ItemMenuItems());
	}

	// The context-menu key lands on the list itself, so it opens the menu of the row under the cursor.
	private async Task HandleBackgroundMenu(MouseEventArgs args)
	{
		bool fromKeyboard = args.Detail == 0 && args.Button != SecondaryButton;
		if (fromKeyboard && CursorRow is { } row && _visible.Contains(row))
		{
			await HandleItemMenu(row, args);
			return;
		}

		ShowMenu(args, BackgroundMenuItems());
	}

	private void ShowMenu(MouseEventArgs args, List<MokaContextMenuItem> items)
	{
		if (items.Count == 0)
		{
			return;
		}

		if (UsesSharedMenu)
		{
			_menuService!.Show(args, items);
			return;
		}

		_menuItems = items;
		_menuX = args.ClientX;
		_menuY = args.ClientY;
		_menuFromKeyboard = args.Detail == 0 && args.Button != SecondaryButton;
		_menuOpen = true;
	}

	private void CloseMenu() => _menuOpen = false;

	private Func<Task> Act(Func<Task> action) => async () =>
	{
		await action();
		StateHasChanged();
	};

	private Action Act(Action action) => () =>
	{
		action();
		StateHasChanged();
	};

	// For what an app's own markup runs: its click belongs to the app, which may not render the browser afterwards.
	private Func<Task> Redrawing(Func<Task> action) => () => InvokeAsync(async () =>
	{
		await action();
		StateHasChanged();
	});

	private string? Hint(MokaFileKeys key, string shortcut) => Keys.HasFlag(key) ? shortcut : null;

	private List<MokaContextMenuItem> ItemMenuItems()
	{
		List<MokaFileRow<TItem>> rows = [.. TargetRows()];
		if (rows.Count == 0)
		{
			return [];
		}

		MokaFileRow<TItem>? only = rows.Count == 1 ? rows[0] : null;
		List<MokaContextMenuItem> standard = [];

		if (only is not null && (only.Info.IsFolder || OnOpenFile.HasDelegate))
		{
			standard.Add(new MokaContextMenuItem { Text = Text.Open, Icon = only.Info.IsFolder ? MokaIcons.File.FolderOpen : MokaIcons.File.Document, Shortcut = Hint(MokaFileKeys.Open, "Enter"), OnClick = Act(() => OpenAsync(only)) });
		}

		if (Download is not null)
		{
			standard.Add(new MokaContextMenuItem { Text = Text.Download, Icon = MokaIcons.Action.Download, OnClick = Act(() => DownloadAsync(rows)) });
		}

		bool divider = standard.Count > 0;
		bool TakeDivider()
		{
			bool taken = divider;
			divider = false;
			return taken;
		}

		if (Rename is not null)
		{
			standard.Add(new MokaContextMenuItem { Text = Text.Rename, Icon = MokaIcons.Action.Edit, Shortcut = Hint(MokaFileKeys.Rename, "F2"), Disabled = only is null, DividerBefore = TakeDivider(), OnClickSync = Act(() => BeginRename(only)) });
		}

		if (Move is not null)
		{
			standard.Add(new MokaContextMenuItem { Text = Text.Cut, Icon = MokaIcons.Content.Cut, Shortcut = Hint(MokaFileKeys.Cut, "Ctrl+X"), DividerBefore = TakeDivider(), OnClickSync = Act(() => HoldForMove(rows)) });
		}

		if (Copy is not null)
		{
			standard.Add(new MokaContextMenuItem { Text = Text.Copy, Icon = MokaIcons.Content.Copy, Shortcut = Hint(MokaFileKeys.Copy, "Ctrl+C"), DividerBefore = TakeDivider(), OnClickSync = Act(() => HoldForCopy(rows)) });
			standard.Add(new MokaContextMenuItem { Text = Text.Duplicate, Icon = MokaIcons.Content.Copy, Shortcut = Hint(MokaFileKeys.Duplicate, "Ctrl+D"), OnClick = Act(() => DuplicateAsync(rows)) });
		}

		if (_held is not null)
		{
			standard.Add(new MokaContextMenuItem { Text = Text.Paste, Icon = MokaIcons.Content.Paste, Shortcut = Hint(MokaFileKeys.Paste, "Ctrl+V"), DividerBefore = TakeDivider(), OnClick = Act(() => PasteIntoAsync(CurrentFolder)) });
		}

		if (Delete is not null)
		{
			standard.Add(new MokaContextMenuItem { Text = Text.Delete, Icon = MokaIcons.Action.Delete, Shortcut = Hint(MokaFileKeys.Delete, "Del"), DividerBefore = standard.Count > 0, OnClick = Act(() => RequestDeleteAsync(rows)) });
		}

		return ItemMenu is null
			? standard
			: [.. ItemMenu(new MokaFileMenuContext<TItem>(rows.ConvertAll(row => row.Item), standard))];
	}

	private List<MokaContextMenuItem> BackgroundMenuItems()
	{
		List<MokaContextMenuItem> standard = [];
		if (CreateFolder is not null)
		{
			standard.Add(new MokaContextMenuItem { Text = Text.NewFolder, Icon = MokaIcons.File.FolderPlus, OnClickSync = Act(BeginCreateFolder) });
		}

		if (Upload is not null)
		{
			standard.Add(new MokaContextMenuItem { Text = Text.Upload, Icon = MokaIcons.Action.Upload, OnClick = PickFilesAsync });
		}

		if (_held is not null)
		{
			standard.Add(new MokaContextMenuItem { Text = Text.Paste, Icon = MokaIcons.Content.Paste, Shortcut = Hint(MokaFileKeys.Paste, "Ctrl+V"), DividerBefore = standard.Count > 0, OnClick = Act(() => PasteIntoAsync(CurrentFolder)) });
		}

		standard.Add(new MokaContextMenuItem { Text = Text.SelectAll, Shortcut = Hint(MokaFileKeys.SelectAll, "Ctrl+A"), DividerBefore = standard.Count > 0, OnClick = Act(SelectAllAsync) });
		standard.Add(new MokaContextMenuItem { Text = Text.Refresh, Icon = MokaIcons.Action.Refresh, Shortcut = Hint(MokaFileKeys.Refresh, "F5"), OnClick = ReloadAsync });

		return BackgroundMenu is null
			? standard
			: [.. BackgroundMenu(new MokaFileMenuContext<TItem>([], standard))];
	}

	private sealed record HeldItems(List<MokaFileRow<TItem>> Rows, HashSet<string> Keys, bool Cut);
}
