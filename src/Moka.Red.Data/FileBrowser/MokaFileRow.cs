namespace Moka.Red.Data.FileBrowser;

// One item with its description, read once per load so Describe doesn't run on every render.
internal sealed class MokaFileRow<TItem>(TItem item, MokaFileInfo info)
{
	public TItem Item { get; } = item;

	public MokaFileInfo Info { get; } = info;

	public string Key => Info.Key;

	// Where the row sits among those on screen, kept here because Virtualize doesn't pass it to its template.
	public int Index { get; set; }

	// The line of the view the row is drawn on, and its place along that line, for the arrow keys.
	public int Line { get; set; }

	public int Column { get; set; }

	// The heading the row sits under while the browser groups, as that heading's place in the order.
	public int Group { get; set; }

	// What the app's item filter last said, kept so a parent render that changes nothing doesn't re-sort the folder.
	public bool FilteredOut { get; set; }

	// Worked out once per load, since the app's mapping may be asked for every row of a large folder.
	public MokaFileType? Type { get; set; }
}
