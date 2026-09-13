using Microsoft.AspNetCore.Components.Web;

namespace Moka.Red.Core.Interactions;

/// <summary>
///     Payload for a context-menu (right-click) event raised by a collection component on one of
///     its items (a table row, a kanban card, ...). Carries the item plus the originating mouse
///     event so a handler can open a menu at the cursor. Pairs naturally with a context-menu
///     service: <c>OnRowContextMenu="a =&gt; Menu.Show(a.MouseEvent, ItemsFor(a.Item))"</c>.
/// </summary>
/// <typeparam name="TItem">The item type of the collection.</typeparam>
/// <param name="Item">The item that was right-clicked.</param>
/// <param name="MouseEvent">The originating mouse event, for cursor position.</param>
public sealed record MokaItemContextMenuArgs<TItem>(TItem Item, MouseEventArgs MouseEvent);
