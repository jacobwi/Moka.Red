using Moka.Red.Navigation.Tabs.Models;

namespace Moka.Red.Navigation.Tabs.Plugins;

/// <summary>
///     Registry that manages all registered <see cref="IMokaTabPlugin" /> instances and dispatches lifecycle events.
/// </summary>
public sealed class MokaTabPluginRegistry
{
	/// <summary>
	///     Initializes the registry with all DI-registered plugins.
	/// </summary>
	public MokaTabPluginRegistry(IEnumerable<IMokaTabPlugin> plugins)
	{
		Plugins = plugins.ToList();
	}

	/// <summary>
	///     Gets the registered plugins.
	/// </summary>
	public IReadOnlyList<IMokaTabPlugin> Plugins { get; }

	/// <summary>
	///     Dispatches the OnTabCreating hook to all plugins. Returns true if any plugin cancelled the operation.
	/// </summary>
	public async Task<bool> NotifyTabCreatingAsync<TValue>(TabInfo<TValue> tab, TabCreatingEventArgs args)
	{
		ArgumentNullException.ThrowIfNull(args);
		foreach (IMokaTabPlugin plugin in Plugins)
		{
			await plugin.OnTabCreatingAsync(tab, args);
			if (args.Cancel)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	///     Dispatches the OnTabActivated hook to all plugins.
	/// </summary>
	public async Task NotifyTabActivatedAsync<TValue>(TabInfo<TValue> tab)
	{
		foreach (IMokaTabPlugin plugin in Plugins)
		{
			await plugin.OnTabActivatedAsync(tab);
		}
	}

	/// <summary>
	///     Dispatches the OnTabDeactivated hook to all plugins.
	/// </summary>
	public async Task NotifyTabDeactivatedAsync<TValue>(TabInfo<TValue> tab)
	{
		foreach (IMokaTabPlugin plugin in Plugins)
		{
			await plugin.OnTabDeactivatedAsync(tab);
		}
	}

	/// <summary>
	///     Dispatches the OnTabClosing hook to all plugins. Returns true if any plugin cancelled the operation.
	/// </summary>
	public async Task<bool> NotifyTabClosingAsync<TValue>(TabInfo<TValue> tab, TabClosingEventArgs args)
	{
		ArgumentNullException.ThrowIfNull(args);
		foreach (IMokaTabPlugin plugin in Plugins)
		{
			await plugin.OnTabClosingAsync(tab, args);
			if (args.Cancel)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	///     Collects context menu items from all plugins for a given tab.
	/// </summary>
	public IReadOnlyList<ContextMenuItem> GetContextMenuItems<TValue>(TabInfo<TValue> tab)
	{
		var items = new List<ContextMenuItem>();
		foreach (IMokaTabPlugin plugin in Plugins)
		{
			items.AddRange(plugin.GetContextMenuItems(tab));
		}

		return items;
	}
}
