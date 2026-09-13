namespace Moka.Red.Data.Table;

/// <summary>
///     Context passed to the <see cref="MokaTable{TItem}.OnExport" /> callback.
///     Contains the item list to export and the visible column definitions.
/// </summary>
/// <typeparam name="TItem">The row data type.</typeparam>
public sealed record MokaTableExportContext<TItem>
{
	/// <summary>
	///     The rows to export: every row matching the current search and column filters, in the
	///     current sort order, not just the current page. Under server-side data these come from a
	///     dedicated ServerData request sized to cover the whole result set - check
	///     <see cref="IsCompleteSet" /> to see whether the server honoured it.
	/// </summary>
	public required IReadOnlyList<TItem> Items { get; init; }

	/// <summary>Visible column definitions.</summary>
	public required IReadOnlyList<MokaColumn<TItem>> Columns { get; init; }

	/// <summary>
	///     True when <see cref="Items" /> covers the whole result set. False when a server-side data
	///     source returned fewer rows than it reported as the total (for example a server that caps
	///     page size), in which case <see cref="Items" /> is a partial export.
	/// </summary>
	public bool IsCompleteSet { get; init; } = true;
}
