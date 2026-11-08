using Moka.Red.ContextMenu;

namespace Moka.Red.Data.FileBrowser;

/// <summary>What a menu is being opened for. Return <see cref="Standard" /> with your own items added or removed.</summary>
/// <typeparam name="TItem">The item type of the browser.</typeparam>
/// <param name="Items">The items the menu acts on. Empty for the background menu.</param>
/// <param name="Standard">The items the browser would show, from the actions it was given.</param>
public sealed record MokaFileMenuContext<TItem>(IReadOnlyList<TItem> Items, IReadOnlyList<MokaContextMenuItem> Standard);
