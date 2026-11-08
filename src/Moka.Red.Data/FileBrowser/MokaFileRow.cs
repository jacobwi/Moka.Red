namespace Moka.Red.Data.FileBrowser;

// One item with its description, read once per load so Describe doesn't run on every render.
internal sealed class MokaFileRow<TItem>(TItem item, MokaFileInfo info)
{
	public TItem Item { get; } = item;

	public MokaFileInfo Info { get; } = info;

	public string Key => Info.Key;

	// Where the row sits among those on screen, kept here because Virtualize doesn't pass it to its template.
	public int Index { get; set; }
}
