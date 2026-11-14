namespace Moka.Red.Data.Table;

/// <summary>How a <see cref="MokaTable{TItem}" /> moves through its rows.</summary>
public enum MokaPagerLook
{
	/// <summary>Numbered pages with first, previous, next and last, a page size and "1-10 of 200". The default.</summary>
	Numbered,

	/// <summary>"Show 10 entries" above the rows, and "Showing 1 to 10 of 200 entries" with Previous and Next below.</summary>
	Entries,

	/// <summary>Only previous, "3 / 25" and next.</summary>
	Compact,

	/// <summary>Previous, "Page [3] of 25" with a box to type a page in, and next.</summary>
	Jump,

	/// <summary>The first page of rows, then "Show 10 more" and "Show all" under them.</summary>
	LoadMore,

	/// <summary>More rows load by themselves as the end of the rows scrolls into view, with a button for the keyboard.</summary>
	Endless,

	/// <summary>
	///     Newer and Older, for servers that page by cursor and don't count the rows. The table hands the server
	///     <see cref="MokaTableRequest.Cursor" /> and reads <see cref="MokaTableResult{TItem}.NextCursor" /> and
	///     <see cref="MokaTableResult{TItem}.PreviousCursor" />. With <c>Items</c> it steps through pages.
	/// </summary>
	Cursor
}
