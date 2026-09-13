using System.Text.Json;
using Moka.Red.Navigation.Tabs.Models;
using Moka.Red.Navigation.Tabs.Plugins;

namespace Moka.Red.Navigation.Tabs.Services;

/// <summary>
///     Default implementation of <see cref="IMokaTabSessionState{TValue}" />.
/// </summary>
/// <typeparam name="TValue">The type of value stored by tabs.</typeparam>
public sealed class MokaTabSessionState<TValue> : IMokaTabSessionState<TValue>
{
	#region Constructor

	/// <summary>
	///     Initializes a new instance of the <see cref="MokaTabSessionState{TValue}" /> class.
	/// </summary>
	public MokaTabSessionState(MokaTabPluginRegistry pluginRegistry)
	{
		_pluginRegistry = pluginRegistry;
	}

	#endregion

	#region Query

	/// <inheritdoc />
	public TabInfo<TValue>? GetTab(string tabId) => _tabIndex.GetValueOrDefault(tabId);

	#endregion

	#region Private

	private void NotifyStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

	#endregion

	#region Fields

	private readonly List<TabInfo<TValue>> _tabs = [];
	private readonly Dictionary<string, TabInfo<TValue>> _tabIndex = new();
	private readonly List<TabGroupInfo> _groups = [];
	private readonly List<string> _restoreWarnings = [];
	private readonly MokaTabPluginRegistry _pluginRegistry;

	#endregion

	#region Properties

	/// <inheritdoc />
	public IReadOnlyList<TabInfo<TValue>> Tabs => _tabs;

	/// <inheritdoc />
	public TabInfo<TValue>? ActiveTab => ActiveTabId is not null ? _tabIndex.GetValueOrDefault(ActiveTabId) : null;

	/// <inheritdoc />
	public string? ActiveTabId { get; private set; }

	/// <inheritdoc />
	public IReadOnlyList<TabGroupInfo> Groups => _groups;

	/// <inheritdoc />
	public Func<TValue, string>? ValueSerializer { get; set; }

	/// <inheritdoc />
	public Func<string, TValue?>? ValueDeserializer { get; set; }

	/// <inheritdoc />
	public IReadOnlyList<string> LastRestoreWarnings => _restoreWarnings;

	/// <inheritdoc />
	public event EventHandler? StateChanged;

	#endregion

	#region Tab Operations

	/// <inheritdoc />
	public async Task<bool> AddTabAsync(TabInfo<TValue> tab, int? index = null)
	{
		ArgumentNullException.ThrowIfNull(tab);
		var args = new TabCreatingEventArgs { TabId = tab.Id, Index = index ?? _tabs.Count };
		if (await _pluginRegistry.NotifyTabCreatingAsync(tab, args))
		{
			return false;
		}

		if (index.HasValue && index.Value >= 0 && index.Value <= _tabs.Count)
		{
			_tabs.Insert(index.Value, tab);
		}
		else
		{
			_tabs.Add(tab);
		}

		_tabIndex[tab.Id] = tab;

		if (ActiveTabId is null)
		{
			ActiveTabId = tab.Id;
			tab.LastActivatedAt = DateTimeOffset.UtcNow;
			await _pluginRegistry.NotifyTabActivatedAsync(tab);
		}

		NotifyStateChanged();
		return true;
	}

	/// <inheritdoc />
	public Task<bool> RemoveTabAsync(string tabId) => RemoveTabAsync(tabId, false);

	/// <inheritdoc />
	public async Task<bool> RemoveTabAsync(string tabId, bool force)
	{
		if (!_tabIndex.TryGetValue(tabId, out TabInfo<TValue>? tab))
		{
			return false;
		}

		if (!force && (!tab.IsClosable || tab.IsPinned))
		{
			return false;
		}

		int tabIndex = _tabs.IndexOf(tab);
		var args = new TabClosingEventArgs { TabId = tabId, Index = tabIndex };
		if (await _pluginRegistry.NotifyTabClosingAsync(tab, args))
		{
			return false;
		}

		_tabs.RemoveAt(tabIndex);
		_tabIndex.Remove(tabId);

		if (ActiveTabId == tabId)
		{
			if (_tabs.Count > 0)
			{
				int newIndex = Math.Min(tabIndex, _tabs.Count - 1);
				ActiveTabId = _tabs[newIndex].Id;
				_tabs[newIndex].LastActivatedAt = DateTimeOffset.UtcNow;
				await _pluginRegistry.NotifyTabActivatedAsync(_tabs[newIndex]);
			}
			else
			{
				ActiveTabId = null;
			}
		}

		NotifyStateChanged();
		return true;
	}

	/// <inheritdoc />
	public async Task ActivateTabAsync(string tabId)
	{
		if (!_tabIndex.TryGetValue(tabId, out TabInfo<TValue>? tab) || ActiveTabId == tabId)
		{
			return;
		}

		TabInfo<TValue>? previousTab = ActiveTab;
		if (previousTab is not null)
		{
			await _pluginRegistry.NotifyTabDeactivatedAsync(previousTab);
		}

		ActiveTabId = tabId;
		tab.LastActivatedAt = DateTimeOffset.UtcNow;
		await _pluginRegistry.NotifyTabActivatedAsync(tab);

		NotifyStateChanged();
	}

	/// <inheritdoc />
	public Task ReorderTabAsync(string tabId, int newIndex)
	{
		if (!_tabIndex.TryGetValue(tabId, out TabInfo<TValue>? tab))
		{
			return Task.CompletedTask;
		}

		int oldIndex = _tabs.IndexOf(tab);
		if (oldIndex == newIndex)
		{
			return Task.CompletedTask;
		}

		_tabs.RemoveAt(oldIndex);
		int clampedIndex = Math.Clamp(newIndex, 0, _tabs.Count);
		_tabs.Insert(clampedIndex, tab);

		NotifyStateChanged();
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public void TogglePin(string tabId)
	{
		if (!_tabIndex.TryGetValue(tabId, out TabInfo<TValue>? tab))
		{
			return;
		}

		int index = _tabs.IndexOf(tab);
		tab.IsPinned = !tab.IsPinned;

		if (tab.IsPinned)
		{
			// Pinning always parks the tab at the end of the pinned run.
			_tabs.RemoveAt(index);
			int lastPinnedIndex = _tabs.FindLastIndex(t => t.IsPinned);
			_tabs.Insert(lastPinnedIndex + 1, tab);
		}
		else
		{
			// Unpinning only relocates a tab that is still sitting inside the pinned run;
			// a tab the user dragged elsewhere stays put.
			int pinnedRunLength = LeadingPinnedCount(tab);
			if (index <= pinnedRunLength)
			{
				_tabs.RemoveAt(index);
				_tabs.Insert(pinnedRunLength, tab);
			}
		}

		NotifyStateChanged();
	}

	/// <summary>
	///     Counts the leading run of pinned tabs, ignoring <paramref name="exclude" />.
	/// </summary>
	private int LeadingPinnedCount(TabInfo<TValue> exclude)
	{
		int count = 0;
		foreach (TabInfo<TValue> tab in _tabs)
		{
			if (ReferenceEquals(tab, exclude))
			{
				continue;
			}

			if (!tab.IsPinned)
			{
				break;
			}

			count++;
		}

		return count;
	}

	#endregion

	#region Group Operations

	/// <inheritdoc />
	public Task SetTabGroupAsync(string tabId, string? groupName)
	{
		if (!_tabIndex.TryGetValue(tabId, out TabInfo<TValue>? tab))
		{
			return Task.CompletedTask;
		}

		tab.GroupName = groupName;
		NotifyStateChanged();
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public void UpsertGroup(TabGroupInfo group)
	{
		int existing = _groups.FindIndex(g => g.Name == group.Name);
		if (existing >= 0)
		{
			_groups[existing] = group;
		}
		else
		{
			_groups.Add(group);
		}

		NotifyStateChanged();
	}

	/// <inheritdoc />
	public void ToggleGroupCollapse(string groupName)
	{
		TabGroupInfo? group = _groups.Find(g => g.Name == groupName);
		if (group is not null)
		{
			group.IsCollapsed = !group.IsCollapsed;
			NotifyStateChanged();
		}
	}

	#endregion

	#region Bulk Operations

	/// <inheritdoc />
	public Task CloseOtherTabsAsync(string tabId) =>
		CloseTabsAsync(_tabs.Where(t => t.Id != tabId && t.IsClosable && !t.IsPinned).ToList(), tabId);

	/// <inheritdoc />
	public Task CloseTabsToTheRightAsync(string tabId)
	{
		int index = _tabs.FindIndex(t => t.Id == tabId);
		return index < 0
			? Task.CompletedTask
			: CloseTabsAsync(_tabs.Skip(index + 1).Where(t => t.IsClosable && !t.IsPinned).ToList(), tabId);
	}

	/// <inheritdoc />
	public Task CloseAllTabsAsync() =>
		CloseTabsAsync(_tabs.Where(t => t.IsClosable && !t.IsPinned).ToList(), null);

	/// <summary>
	///     Closes each candidate that no plugin vetoes, then activates a replacement tab (notifying
	///     plugins) when the tab that was active got closed.
	/// </summary>
	/// <param name="candidates">Tabs to close, already filtered for closability.</param>
	/// <param name="preferredActiveId">Tab to activate if the active one is closed, when it survives.</param>
	private async Task CloseTabsAsync(List<TabInfo<TValue>> candidates, string? preferredActiveId)
	{
		bool removedAny = false;

		foreach (TabInfo<TValue> tab in candidates)
		{
			int index = _tabs.IndexOf(tab);
			if (index < 0)
			{
				continue;
			}

			var closing = new TabClosingEventArgs { TabId = tab.Id, Index = index };
			if (await _pluginRegistry.NotifyTabClosingAsync(tab, closing))
			{
				continue;
			}

			_tabs.RemoveAt(index);
			_tabIndex.Remove(tab.Id);
			removedAny = true;
		}

		if (!removedAny)
		{
			return;
		}

		if (ActiveTabId is not null && !_tabIndex.ContainsKey(ActiveTabId))
		{
			string? replacementId = preferredActiveId is not null && _tabIndex.ContainsKey(preferredActiveId)
				? preferredActiveId
				: _tabs.Count > 0
					? _tabs[0].Id
					: null;

			ActiveTabId = replacementId;

			if (replacementId is not null)
			{
				TabInfo<TValue> replacement = _tabIndex[replacementId];
				replacement.LastActivatedAt = DateTimeOffset.UtcNow;
				await _pluginRegistry.NotifyTabActivatedAsync(replacement);
			}
		}

		NotifyStateChanged();
	}

	#endregion

	#region Serialization

	/// <inheritdoc />
	public string SerializeState()
	{
		var snapshot = new TabStateSnapshot
		{
			ActiveTabId = ActiveTabId,
			Tabs = _tabs.Select(t => new TabSnapshot
			{
				Id = t.Id,
				Title = t.Title,
				GroupName = t.GroupName,
				IsPinned = t.IsPinned,
				IsClosable = t.IsClosable,
				IsDraggable = t.IsDraggable,
				KeepAlive = t.KeepAlive,
				IconClass = t.IconClass,
				Tooltip = t.Tooltip,
				CssClass = t.CssClass,
				ActiveColor = t.ActiveColor,
				CreatedAt = t.CreatedAt,
				LastActivatedAt = t.LastActivatedAt,
				ContentComponentTypeName = t.ContentComponentType?.AssemblyQualifiedName,
				Value = SerializeValue(t.Value),
				BadgeCount = t.Badge?.Count,
				BadgeShowDot = t.Badge?.ShowDot ?? false,
				BadgeCssClass = t.Badge?.CssClass
			}).ToList(),
			Groups = _groups.Select(g => new GroupSnapshot
			{
				Name = g.Name,
				DisplayTitle = g.DisplayTitle,
				IsCollapsed = g.IsCollapsed,
				Order = g.Order,
				Color = g.Color,
				CssClass = g.CssClass,
				BorderPosition = g.BorderPosition
			}).ToList()
		};

		return JsonSerializer.Serialize(snapshot, TabStateJsonContext.Default.TabStateSnapshot);
	}

	/// <inheritdoc />
	public async Task RestoreStateAsync(string json)
	{
		_restoreWarnings.Clear();

		TabStateSnapshot? snapshot = JsonSerializer.Deserialize(json, TabStateJsonContext.Default.TabStateSnapshot);
		if (snapshot is null)
		{
			return;
		}

		_tabs.Clear();
		_tabIndex.Clear();
		_groups.Clear();

		foreach (GroupSnapshot gs in snapshot.Groups)
		{
			_groups.Add(new TabGroupInfo
			{
				Name = gs.Name,
				DisplayTitle = gs.DisplayTitle,
				IsCollapsed = gs.IsCollapsed,
				Order = gs.Order,
				Color = gs.Color,
				CssClass = gs.CssClass,
				BorderPosition = gs.BorderPosition
			});
		}

		foreach (TabSnapshot ts in snapshot.Tabs)
		{
			var tab = new TabInfo<TValue>
			{
				Id = ts.Id,
				Title = ts.Title,
				GroupName = ts.GroupName,
				IsPinned = ts.IsPinned,
				IsClosable = ts.IsClosable,
				IsDraggable = ts.IsDraggable,
				KeepAlive = ts.KeepAlive,
				IconClass = ts.IconClass,
				Tooltip = ts.Tooltip,
				CssClass = ts.CssClass,
				ActiveColor = ts.ActiveColor,
				CreatedAt = ts.CreatedAt,
				LastActivatedAt = ts.LastActivatedAt,
				ContentComponentType = ResolveContentType(ts),
				Value = DeserializeValue(ts),
				Badge = ts.BadgeCount.HasValue || ts.BadgeShowDot
					? new TabBadgeInfo { Count = ts.BadgeCount, ShowDot = ts.BadgeShowDot, CssClass = ts.BadgeCssClass }
					: null
			};

			_tabs.Add(tab);
			_tabIndex[tab.Id] = tab;
		}

		if (snapshot.ActiveTabId is not null && _tabIndex.ContainsKey(snapshot.ActiveTabId))
		{
			ActiveTabId = snapshot.ActiveTabId;
		}
		else if (_tabs.Count > 0)
		{
			ActiveTabId = _tabs[0].Id;
		}
		else
		{
			ActiveTabId = null;
		}

		if (ActiveTabId is not null)
		{
			await _pluginRegistry.NotifyTabActivatedAsync(_tabIndex[ActiveTabId]);
		}

		NotifyStateChanged();
	}

	private string? SerializeValue(TValue? value)
	{
		if (value is null || ValueSerializer is null)
		{
			return null;
		}

		return ValueSerializer(value);
	}

	private TValue? DeserializeValue(TabSnapshot snapshot)
	{
		if (snapshot.Value is null)
		{
			return default;
		}

		if (ValueDeserializer is null)
		{
			_restoreWarnings.Add(
				$"Tab '{snapshot.Id}' carries a persisted value but no ValueDeserializer is configured; Value was not restored.");
			return default;
		}

		return ValueDeserializer(snapshot.Value);
	}

	/// <summary>
	///     Resolves a persisted assembly-qualified type name. Records a warning instead of failing
	///     silently when the assembly is not loaded or the type was trimmed away.
	/// </summary>
	private Type? ResolveContentType(TabSnapshot snapshot)
	{
		if (snapshot.ContentComponentTypeName is null)
		{
			return null;
		}

		Type? type = Type.GetType(snapshot.ContentComponentTypeName, false);
		if (type is null)
		{
			_restoreWarnings.Add(
				$"Tab '{snapshot.Id}' references content component type '{snapshot.ContentComponentTypeName}', which could not be resolved. The tab was restored without content.");
		}

		return type;
	}

	#endregion
}

#region Serialization Models

internal sealed class TabStateSnapshot
{
	public string? ActiveTabId { get; set; }
	public List<TabSnapshot> Tabs { get; set; } = [];
	public List<GroupSnapshot> Groups { get; set; } = [];
}

internal sealed class TabSnapshot
{
	public string Id { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string? GroupName { get; set; }
	public bool IsPinned { get; set; }
	public bool IsClosable { get; set; } = true;
	public bool IsDraggable { get; set; } = true;
	public bool KeepAlive { get; set; }
	public string? IconClass { get; set; }
	public string? Tooltip { get; set; }
	public string? CssClass { get; set; }
	public string? ActiveColor { get; set; }
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset? LastActivatedAt { get; set; }
	public string? ContentComponentTypeName { get; set; }
	public string? Value { get; set; }
	public int? BadgeCount { get; set; }
	public bool BadgeShowDot { get; set; }
	public string? BadgeCssClass { get; set; }
}

internal sealed class GroupSnapshot
{
	public string Name { get; set; } = string.Empty;
	public string? DisplayTitle { get; set; }
	public bool IsCollapsed { get; set; }
	public int Order { get; set; }
	public string? Color { get; set; }
	public string? CssClass { get; set; }
	public BorderPosition? BorderPosition { get; set; }
}

#endregion
