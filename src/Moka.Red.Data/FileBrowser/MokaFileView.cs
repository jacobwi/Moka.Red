namespace Moka.Red.Data.FileBrowser;

/// <summary>How a <see cref="MokaFileBrowser{TItem}" /> shows its items.</summary>
public enum MokaFileViewMode
{
	/// <summary>Rows with columns.</summary>
	List,

	/// <summary>A grid of tiles.</summary>
	Tiles,

	/// <summary>One short line per item, icon and name, flowing into more columns when there's room.</summary>
	Compact,

	/// <summary>The folder above, this folder and a preview of the selection side by side. The folder above goes when narrow.</summary>
	Columns,

	/// <summary>Folders as a row of chips, then big thumbnails for pictures and type icons for other files.</summary>
	Gallery,

	/// <summary>A small card per item: name and date, then type and size. One card per line on a phone.</summary>
	Cards
}

/// <summary>The sort names a <see cref="MokaFileView" /> uses for the built-in columns.</summary>
public static class MokaFileSort
{
	/// <summary>By name, with runs of digits compared as numbers.</summary>
	public const string Name = "name";

	/// <summary>By size.</summary>
	public const string Size = "size";

	/// <summary>By when the item last changed.</summary>
	public const string Modified = "modified";
}

/// <summary>What an app stores to bring a <see cref="MokaFileBrowser{TItem}" /> back the way it was.</summary>
/// <param name="Mode">How the items are laid out.</param>
/// <param name="SortBy">A <see cref="MokaFileSort" /> name, or the <c>Id</c> of a column with a <c>Compare</c>.</param>
/// <param name="Descending">Whether the sort runs from the highest down.</param>
/// <param name="FoldersFirst">Whether folders stay ahead of files, whichever way the sort runs.</param>
public sealed record MokaFileView(
	MokaFileViewMode Mode = MokaFileViewMode.List,
	string SortBy = MokaFileSort.Name,
	bool Descending = false,
	bool FoldersFirst = true);
