namespace Moka.Red.Navigation.Tabs.Models;

/// <summary>
///     Event arguments for tab lifecycle events.
/// </summary>
public class TabEventArgs : EventArgs
{
	/// <summary>
	///     Gets the unique identifier of the tab involved in the event.
	/// </summary>
	public required string TabId { get; init; }

	/// <summary>
	///     Gets the index of the tab in the tab strip.
	/// </summary>
	public int Index { get; init; }
}

/// <summary>
///     Event arguments for tab activation events.
/// </summary>
public sealed class TabActivatedEventArgs : TabEventArgs
{
	/// <summary>
	///     Gets the identifier of the previously active tab, if any.
	/// </summary>
	public string? PreviousTabId { get; init; }
}

/// <summary>
///     Event arguments for tab reorder events.
/// </summary>
public sealed class TabReorderedEventArgs : TabEventArgs
{
	/// <summary>
	///     Gets the previous index before the reorder.
	/// </summary>
	public int OldIndex { get; init; }

	/// <summary>
	///     Gets the new index after the reorder.
	/// </summary>
	public int NewIndex { get; init; }
}

/// <summary>
///     Event arguments for tab group change events.
/// </summary>
public sealed class TabGroupChangedEventArgs : TabEventArgs
{
	/// <summary>
	///     Gets the previous group name, if any.
	/// </summary>
	public string? OldGroup { get; init; }

	/// <summary>
	///     Gets the new group name, if any.
	/// </summary>
	public string? NewGroup { get; init; }
}

/// <summary>
///     Event arguments for tab closing events. Supports cancellation.
/// </summary>
public sealed class TabClosingEventArgs : TabEventArgs
{
	/// <summary>
	///     Set to <c>true</c> to cancel the close operation.
	/// </summary>
	public bool Cancel { get; set; }
}

/// <summary>
///     Event arguments for tab creation events. Supports cancellation.
/// </summary>
public sealed class TabCreatingEventArgs : TabEventArgs
{
	/// <summary>
	///     Set to <c>true</c> to cancel the tab creation.
	/// </summary>
	public bool Cancel { get; set; }
}
