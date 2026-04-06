using Moka.Red.Core.Enums;

namespace Moka.Red.Primitives.Kanban;

/// <summary>
///     Represents a column in a <see cref="MokaKanbanBoard{TItem}" />.
/// </summary>
/// <typeparam name="TItem">The type of items in the column.</typeparam>
/// <param name="Title">The column header text.</param>
/// <param name="Items">The items displayed in the column.</param>
/// <param name="Color">Optional semantic color for the column header accent.</param>
public record MokaKanbanColumn<TItem>(string Title, IList<TItem> Items, MokaColor? Color = null);
