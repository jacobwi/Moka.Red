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
	///     Gets or sets the hook that turns a tab's <typeparamref name="TValue" /> into a string for
	///     <see cref="SerializeState" />. <typeparamref name="TValue" /> is unconstrained, so values are
	///     dropped from the snapshot until this is set.
	/// </summary>
	Func<TValue, string>? ValueSerializer { get; set; }

	/// <summary>
	///     Gets or sets the hook that rebuilds a tab's <typeparamref name="TValue" /> from the string
	///     produced by <see cref="ValueSerializer" />. Pair it with <see cref="ValueSerializer" />:
	///     without it, persisted values are reported in <see cref="LastRestoreWarnings" /> and discarded.
	/// </summary>
	Func<string, TValue?>? ValueDeserializer { get; set; }

	/// <summary>
	///     Gets the warnings collected by the most recent <see cref="RestoreStateAsync" /> call:
	///     content component types that could not be resolved, and values that could not be rebuilt.
	///     Empty when the last restore was clean.
	/// </summary>
	IReadOnlyList<string> LastRestoreWarnings { get; }

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
	///     Returns false if a plugin cancelled closure, the tab was not found, or the tab is
	///     pinned or not closable.
	/// </summary>
	Task<bool> RemoveTabAsync(string tabId);

	/// <summary>
	///     Removes the tab with the specified ID, optionally ignoring its
	///     <see cref="TabInfo{TValue}.IsClosable" /> and <see cref="TabInfo{TValue}.IsPinned" /> guards.
	///     Plugins can still cancel a forced close.
	/// </summary>
	/// <param name="tabId">The tab to remove.</param>
	/// <param name="force">When true, closes a pinned or non-closable tab as well.</param>
	Task<bool> RemoveTabAsync(string tabId, bool force);

	/// <summary>
	///     Activates the tab with the specified ID.
	/// </summary>
	Task ActivateTabAsync(string tabId);

	/// <summary>
	///     Moves a tab from one index to another.
	/// </summary>
	Task ReorderTabAsync(string tabId, int newIndex);

	/// <summary>
	///     Toggles the pinned state of the specified tab. Pinning moves the tab to the end of the
	///     pinned run; unpinning moves it to the start of the unpinned run only when it still sits
	///     inside the pinned run.
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
	///     Closes all tabs except the specified one. Pinned and non-closable tabs are kept.
	/// </summary>
	Task CloseOtherTabsAsync(string tabId);

	/// <summary>
	///     Closes all tabs to the right of the specified tab. Pinned and non-closable tabs are kept.
	/// </summary>
	Task CloseTabsToTheRightAsync(string tabId);

	/// <summary>
	///     Closes all closable tabs. Pinned and non-closable tabs are kept.
	/// </summary>
	Task CloseAllTabsAsync();

	/// <summary>
	///     Gets a tab by its identifier.
	/// </summary>
	TabInfo<TValue>? GetTab(string tabId);

	/// <summary>
	///     Serializes the current tab state to JSON for persistence.
	/// </summary>
	/// <remarks>
	///     Round-trips: id, title, group membership, pinned/closable/draggable/keep-alive flags,
	///     icon class, tooltip, CSS class, active color, created and last-activated timestamps,
	///     badge count/dot/class, the content component's assembly-qualified type name, group
	///     definitions (name, title, collapsed, order, color, CSS class, border position), and the
	///     active tab id. <c>Value</c> round-trips only when <see cref="ValueSerializer" /> and
	///     <see cref="ValueDeserializer" /> are set. Render fragments (<c>IconContent</c>,
	///     <c>ActionContent</c>) and <c>ContentParameters</c> are not serializable and are dropped.
	/// </remarks>
	string SerializeState();

	/// <summary>
	///     Restores tab state from a JSON string previously produced by <see cref="SerializeState" />.
	/// </summary>
	/// <remarks>
	///     Replaces the current tabs and groups. Content component types are looked up by
	///     assembly-qualified name, which fails when the assembly is not loaded or the type was
	///     trimmed; such tabs restore without content and are listed in
	///     <see cref="LastRestoreWarnings" />, as are values that could not be rebuilt. Check that
	///     list after awaiting. See <see cref="SerializeState" /> for what does and does not
	///     round-trip.
	/// </remarks>
	Task RestoreStateAsync(string json);
}
