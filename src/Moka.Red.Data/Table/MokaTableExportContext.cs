namespace Moka.Red.Data.Table;

/// <summary>
///     Context passed to the <see cref="MokaTable{TItem}.OnExport" /> callback.
///     Contains the full item list and visible column definitions.
/// </summary>
/// <typeparam name="TItem">The row data type.</typeparam>
public sealed record MokaTableExportContext<TItem>
{
	/// <summary>All items (not just current page).</summary>
	public required IReadOnlyList<TItem> Items { get; init; }

	/// <summary>Visible column definitions.</summary>
	public required IReadOnlyList<MokaColumn<TItem>> Columns { get; init; }
}
