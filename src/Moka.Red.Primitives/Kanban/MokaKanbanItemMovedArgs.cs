namespace Moka.Red.Primitives.Kanban;

/// <summary>
///     Event arguments raised when a Kanban item is moved between columns.
/// </summary>
/// <typeparam name="TItem">The type of the moved item.</typeparam>
/// <param name="Item">The item that was moved.</param>
/// <param name="FromColumnIndex">The index of the source column.</param>
/// <param name="ToColumnIndex">The index of the destination column.</param>
public record MokaKanbanItemMovedArgs<TItem>(TItem Item, int FromColumnIndex, int ToColumnIndex);
