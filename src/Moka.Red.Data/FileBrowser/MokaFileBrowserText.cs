namespace Moka.Red.Data.FileBrowser;

/// <summary>Every word a <see cref="MokaFileBrowser{TItem}" /> writes. English until replaced.</summary>
public sealed record MokaFileBrowserText
{
	/// <summary>Names the whole browser.</summary>
	public string Label { get; init; } = "Files";

	/// <summary>Names the list of items.</summary>
	public string ItemsLabel { get; init; } = "Items in this folder";

	/// <summary>The Back button.</summary>
	public string Back { get; init; } = "Back";

	/// <summary>The Forward button.</summary>
	public string Forward { get; init; } = "Forward";

	/// <summary>The Up button.</summary>
	public string Up { get; init; } = "Up one folder";

	/// <summary>The Refresh button and menu item.</summary>
	public string Refresh { get; init; } = "Refresh";

	/// <summary>Names the filter box.</summary>
	public string FilterLabel { get; init; } = "Filter this folder";

	/// <summary>The filter box's placeholder.</summary>
	public string FilterPlaceholder { get; init; } = "Filter";

	/// <summary>The New folder button and menu item.</summary>
	public string NewFolder { get; init; } = "New folder";

	/// <summary>The name offered for a new folder. A number is added when it is taken.</summary>
	public string NewFolderName { get; init; } = "New folder";

	/// <summary>The Upload button and menu item.</summary>
	public string Upload { get; init; } = "Upload";

	/// <summary>Names the sort choice.</summary>
	public string SortLabel { get; init; } = "Sort by";

	/// <summary>The name column's heading and sort choice.</summary>
	public string SortName { get; init; } = "Name";

	/// <summary>The size column's heading and sort choice.</summary>
	public string SortSize { get; init; } = "Size";

	/// <summary>The modified column's heading.</summary>
	public string ColumnModified { get; init; } = "Modified";

	/// <summary>The modified sort choice.</summary>
	public string SortModified { get; init; } = "Date";

	/// <summary>The button that reverses the sort while it runs from the lowest up.</summary>
	public string SortAscending { get; init; } = "Sorted lowest first";

	/// <summary>The button that reverses the sort while it runs from the highest down.</summary>
	public string SortDescending { get; init; } = "Sorted highest first";

	/// <summary>The rows view button.</summary>
	public string ListView { get; init; } = "List";

	/// <summary>The tiles view button.</summary>
	public string TilesView { get; init; } = "Tiles";

	/// <summary>The button and panel of the side pane when it can't sit beside the list.</summary>
	public string Side { get; init; } = "Places";

	/// <summary>The button and panel of the details pane when it can't sit beside the list.</summary>
	public string Details { get; init; } = "Details";

	/// <summary>The Open menu item.</summary>
	public string Open { get; init; } = "Open";

	/// <summary>The Download menu item.</summary>
	public string Download { get; init; } = "Download";

	/// <summary>The Rename menu item.</summary>
	public string Rename { get; init; } = "Rename";

	/// <summary>The Cut menu item.</summary>
	public string Cut { get; init; } = "Cut";

	/// <summary>The Copy menu item.</summary>
	public string Copy { get; init; } = "Copy";

	/// <summary>The Paste menu item.</summary>
	public string Paste { get; init; } = "Paste";

	/// <summary>The Duplicate menu item.</summary>
	public string Duplicate { get; init; } = "Duplicate";

	/// <summary>The Delete menu item and the delete dialog's button.</summary>
	public string Delete { get; init; } = "Delete";

	/// <summary>The Select all menu item.</summary>
	public string SelectAll { get; init; } = "Select all";

	/// <summary>The delete dialog's cancel button.</summary>
	public string Cancel { get; init; } = "Cancel";

	/// <summary>The delete dialog's title, given how many items.</summary>
	public Func<int, string> DeleteTitle { get; init; } = count => count == 1 ? "Delete this item?" : $"Delete {count} items?";

	/// <summary>The delete dialog's sentence, given the items' names.</summary>
	public Func<IReadOnlyList<string>, string> DeleteMessage { get; init; } = names => names.Count == 1
		? $"\"{names[0]}\" will be deleted."
		: $"\"{names[0]}\" and {names.Count - 1} more will be deleted.";

	/// <summary>The item count in the status bar.</summary>
	public Func<int, string> ItemCount { get; init; } = count => count == 1 ? "1 item" : $"{count} items";

	/// <summary>The item count while a filter hides some, given those showing and the total.</summary>
	public Func<int, int, string> FilteredCount { get; init; } = (shown, total) => $"{shown} of {total} items";

	/// <summary>The selection count in the status bar.</summary>
	public Func<int, string> SelectedCount { get; init; } = count => $"{count} selected";

	/// <summary>What the status bar says about items waiting to be pasted, given how many and whether they were cut.</summary>
	public Func<int, bool, string> HeldCount { get; init; } = (count, cut) => cut ? $"{count} to move" : $"{count} to copy";

	/// <summary>The title of an empty folder.</summary>
	public string EmptyTitle { get; init; } = "This folder is empty";

	/// <summary>The sentence under the title of an empty folder that can take files.</summary>
	public string EmptyHint { get; init; } = "Drop files here to upload them.";

	/// <summary>The title when the filter hides everything.</summary>
	public string NoMatchesTitle { get; init; } = "No matches";

	/// <summary>The sentence when the filter hides everything, given the filter.</summary>
	public Func<string, string> NoMatches { get; init; } = filter => $"Nothing in this folder has \"{filter}\" in its name.";

	/// <summary>The button that clears the filter.</summary>
	public string ClearFilter { get; init; } = "Clear filter";

	/// <summary>Said to screen readers while a folder loads.</summary>
	public string Loading { get; init; } = "Opening the folder";

	/// <summary>The title when a folder didn't load.</summary>
	public string LoadFailed { get; init; } = "This folder didn't open";

	/// <summary>The button that loads a folder again.</summary>
	public string TryAgain { get; init; } = "Try again";

	/// <summary>The title when an action failed.</summary>
	public string ActionFailed { get; init; } = "That didn't work";

	/// <summary>The layer shown while files are dragged over the browser.</summary>
	public string DropTitle { get; init; } = "Drop to upload";

	/// <summary>Names the field while renaming.</summary>
	public string RenameLabel { get; init; } = "New name";

	/// <summary>Names the field while naming a new folder.</summary>
	public string NewFolderLabel { get; init; } = "Folder name";

	/// <summary>The error shown for a blank name.</summary>
	public string NameRequired { get; init; } = "Enter a name";
}
