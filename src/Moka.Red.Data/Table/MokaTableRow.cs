namespace Moka.Red.Data.Table;

/// <summary>
///     A rendered row of a <see cref="MokaTable{TItem}" />: the item plus its index within the
///     current page. The index is what keyboard navigation and row reordering address, so it has
///     to travel with the item through both the plain and virtualized render paths.
/// </summary>
/// <typeparam name="TItem">The row data type.</typeparam>
/// <param name="Item">The row item.</param>
/// <param name="Index">Zero-based index of the row within the current page.</param>
internal readonly record struct MokaTableRow<TItem>(TItem Item, int Index);
