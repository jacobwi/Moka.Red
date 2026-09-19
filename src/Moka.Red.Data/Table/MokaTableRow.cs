namespace Moka.Red.Data.Table;

/// <summary>
///     A rendered row of a <see cref="MokaTable{TItem}" />: the item, its index among the rendered
///     rows and its <c>@key</c>. The index is what keyboard navigation and row reordering address,
///     so it has to travel with the item through both the plain and virtualized render paths.
/// </summary>
/// <typeparam name="TItem">The row data type.</typeparam>
/// <param name="Item">The row item.</param>
/// <param name="Index">
///     Zero-based index of the row among the rendered rows: the current page, or every row while
///     the pager is hidden.
/// </param>
/// <param name="Key">
///     The row's <c>@key</c>, or null to render it unkeyed. Null when another rendered row has the
///     same key, because Blazor throws on a key repeated among siblings.
/// </param>
internal readonly record struct MokaTableRow<TItem>(TItem Item, int Index, object? Key);
