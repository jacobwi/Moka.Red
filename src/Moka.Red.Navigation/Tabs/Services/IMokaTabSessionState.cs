using Moka.Red.Navigation.Tabs.Models;

namespace Moka.Red.Navigation.Tabs.Services;

/// <summary>
///     Scoped service that tracks the full state of the tab system including open tabs,
///     active tab, ordering, group membership, and per-tab values.
/// </summary>
/// <typeparam name="TValue">The type of value stored by tabs.</typeparam>
public interface IMokaTabSessionState<TValue>
{
	/// <summary>
	///     Gets the read-only list of currently open tabs in display order.
	/// </summary>
	IReadOnlyList<TabInfo<TValue>> Tabs { get; }

	/// <summary>
	///     Gets the currently active tab, if any.
	/// </summary>
	TabInfo<TValue>? ActiveTab { get; }

	/// <summary>
	///     Gets the active tab's identifier, or null.
	/// </summary>
	string? ActiveTabId { get; }

	/// <summary>
	///     Gets the read-only list of tab groups.
	/// </summary>
	IReadOnlyList<TabGroupInfo> Groups { get; }

	/// <summary>
	///     Raised when the tab collection or active tab changes.
	/// </summary>
	event EventHandler? StateChanged;

	/// <summary>
	///     Adds a new tab at the specified index (or at the end if index is null).
	///     Returns false if a plugin cancelled creation.
	/// </summary>
	Task<bool> AddTabAsync(TabInfo<TValue> tab, int? index = null);

	/// <summary>
	///     Removes the tab with the specified ID.
	///     Returns false if a plugin cancelled closure or the tab was not found.
	/// </summary>
	Task<bool> RemoveTabAsync(string tabId);

	/// <summary>
	///     Activates the tab with the specified ID.
	/// </summary>
	Task ActivateTabAsync(string tabId);

	/// <summary>
	///     Moves a tab from one index to another.
	/// </summary>
	Task ReorderTabAsync(string tabId, int newIndex);

	/// <summary>
	///     Toggles the pinned state of the specified tab.
	/// </summary>
	void TogglePin(string tabId);

	/// <summary>
	///     Assigns a tab to a named group (or removes from group if null).
	/// </summary>
	Task SetTabGroupAsync(string tabId, string? groupName);

	/// <summary>
	///     Adds or updates a tab group definition.
	/// </summary>
	void UpsertGroup(TabGroupInfo group);

	/// <summary>
	///     Toggles the collapsed state of a group.
	/// </summary>
	void ToggleGroupCollapse(string groupName);

	/// <summary>
	///     Closes all tabs except the specified one.
	/// </summary>
	Task CloseOtherTabsAsync(string tabId);

	/// <summary>
	///     Closes all tabs to the right of the specified tab.
	/// </summary>
	Task CloseTabsToTheRightAsync(string tabId);

	/// <summary>
	///     Closes all closable tabs.
	/// </summary>
	Task CloseAllTabsAsync();

	/// <summary>
	///     Gets a tab by its identifier.
	/// </summary>
	TabInfo<TValue>? GetTab(string tabId);

	/// <summary>
	///     Serializes the current tab state to JSON for persistence.
	/// </summary>
	string SerializeState();

	/// <summary>
	///     Restores tab state from a JSON string previously produced by <see cref="SerializeState" />.
	/// </summary>
	Task RestoreStateAsync(string json);
}
