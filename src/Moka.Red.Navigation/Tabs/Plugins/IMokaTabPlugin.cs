using Moka.Red.Navigation.Tabs.Models;

namespace Moka.Red.Navigation.Tabs.Plugins;

/// <summary>
///     Interface for tab system plugins that can hook into tab lifecycle events
///     and provide custom tab types, toolbar actions, and content renderers.
/// </summary>
public interface IMokaTabPlugin
{
	/// <summary>
	///     Gets the unique name of this plugin.
	/// </summary>
	string Name { get; }

	/// <summary>
	///     Gets the display description of this plugin.
	/// </summary>
	string? Description { get; }

	/// <summary>
	///     Called when a tab is about to be created. Can cancel creation by setting <see cref="TabCreatingEventArgs.Cancel" />
	///     .
	/// </summary>
	Task OnTabCreatingAsync<TValue>(TabInfo<TValue> tab, TabCreatingEventArgs args) => Task.CompletedTask;

	/// <summary>
	///     Called when a tab becomes the active tab.
	/// </summary>
	Task OnTabActivatedAsync<TValue>(TabInfo<TValue> tab) => Task.CompletedTask;

	/// <summary>
	///     Called when a tab is deactivated (another tab becomes active).
	/// </summary>
	Task OnTabDeactivatedAsync<TValue>(TabInfo<TValue> tab) => Task.CompletedTask;

	/// <summary>
	///     Called when a tab is about to be closed. Can cancel closure by setting <see cref="TabClosingEventArgs.Cancel" />.
	/// </summary>
	Task OnTabClosingAsync<TValue>(TabInfo<TValue> tab, TabClosingEventArgs args) => Task.CompletedTask;

	/// <summary>
	///     Returns additional context menu items contributed by this plugin for the given tab.
	/// </summary>
	IEnumerable<ContextMenuItem> GetContextMenuItems<TValue>(TabInfo<TValue> tab) => [];
}
