namespace Moka.Red.Primitives.Kanban;

/// <summary>
///     Event arguments raised when a Kanban item is moved, to another column or to another place in its own.
/// </summary>
/// <typeparam name="TItem">The type of the moved item.</typeparam>
/// <param name="Item">The item that was moved.</param>
/// <param name="FromColumnIndex">The index of the source column.</param>
/// <param name="ToColumnIndex">The index of the destination column, the same as the source for a move within a column.</param>
public record MokaKanbanItemMovedArgs<TItem>(TItem Item, int FromColumnIndex, int ToColumnIndex)
{
	/// <summary>Where the item was in its source column, from 0.</summary>
	public int FromIndex { get; init; }

	/// <summary>Where the item is now in its destination column, from 0.</summary>
	public int ToIndex { get; init; }
}
