namespace Moka.Red.Data.Table;

/// <summary>
///     Data for inline cell editing in a <see cref="MokaTable{TItem}" />.
/// </summary>
/// <typeparam name="TItem">The row data type.</typeparam>
/// <param name="Item">The row item being edited.</param>
/// <param name="ColumnTitle">Title of the column being edited.</param>
/// <param name="OldValue">The previous cell value.</param>
/// <param name="NewValue">The new cell value entered by the user.</param>
public sealed record MokaTableCellEditResult<TItem>(TItem Item, string ColumnTitle, object? OldValue, string NewValue);
