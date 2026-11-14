using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Moka.Red.Layout.DockLayout;

/// <summary>Every word a <see cref="MokaDockLayout" /> writes, for <see cref="MokaDockLayout.Text" />. English by default.</summary>
public sealed record MokaDockText
{
	/// <summary>The default words.</summary>
	public static MokaDockText Default { get; } = new();

	/// <summary>The button that takes a docked group out into a floating window.</summary>
	public string Undock { get; init; } = "Undock";

	/// <summary>The button that docks a floating or pinned group again.</summary>
	public string Dock { get; init; } = "Dock";

	/// <summary>The button that folds a group to a strip.</summary>
	public string Collapse { get; init; } = "Collapse";

	/// <summary>The button that opens a folded group.</summary>
	public string Expand { get; init; } = "Expand";

	/// <summary>The button that makes a group fill the layout.</summary>
	public string Maximize { get; init; } = "Maximize";

	/// <summary>The button that brings a maximized group back.</summary>
	public string Restore { get; init; } = "Restore";

	/// <summary>The button that closes the showing panel.</summary>
	public string Close { get; init; } = "Close";

	/// <summary>A tab's close button, given the panel's title.</summary>
	public Func<string, string> CloseTab { get; init; } = title => $"Close {title}";

	/// <summary>The button that opens the panel's menu.</summary>
	public string Menu { get; init; } = "Panel options";

	/// <summary>The menu item that pins the group to a side bar.</summary>
	public string AutoHide { get; init; } = "Auto-hide";

	/// <summary>The menu item that opens the panel in another browser window.</summary>
	public string PopOut { get; init; } = "Open in new window";

	/// <summary>What the main window says while a panel is in another window.</summary>
	public string InOtherWindow { get; init; } = "Shown in another window";

	/// <summary>The button that brings a panel back from another window.</summary>
	public string BringBack { get; init; } = "Bring back";

	/// <summary>What a panel's window says once the panel has gone back, or when its session can't be found.</summary>
	public string WindowGone { get; init; } = "This panel is no longer open in this window.";

	/// <summary>The menu items that dock the panel at the layout's edges.</summary>
	public string DockLeft { get; init; } = "Dock left";

	/// <inheritdoc cref="DockLeft" />
	public string DockRight { get; init; } = "Dock right";

	/// <inheritdoc cref="DockLeft" />
	public string DockTop { get; init; } = "Dock top";

	/// <inheritdoc cref="DockLeft" />
	public string DockBottom { get; init; } = "Dock bottom";

	/// <summary>The menu item that moves the panel into another group, given that group's name.</summary>
	public Func<string, string> MoveTo { get; init; } = group => $"Move to {group}";

	/// <summary>The button that lists every tab of a group whose tabs don't all fit.</summary>
	public string AllTabs { get; init; } = "All tabs";

	/// <summary>The button that lists the other panels of a group drawn with <see cref="MokaDockTabStyle.Switcher" />.</summary>
	public string SwitchPanel { get; init; } = "Switch panel";

	/// <summary>A tab strip's name, given its group's name.</summary>
	public Func<string, string> TabsOf { get; init; } = group => $"{group} tabs";

	/// <summary>The name of a group with nothing else to go by.</summary>
	public string Group { get; init; } = "Panels";

	/// <summary>The main area's name, unless <see cref="MokaDockContent.Title" /> gives one.</summary>
	public string Content { get; init; } = "Content";

	/// <summary>A splitter's name, given the names of the two groups beside it.</summary>
	public Func<string, string, string> Resize { get; init; } = (first, second) => $"Resize {first} and {second}";

	/// <summary>The bar of icons for hidden and pinned panels, and the narrow layout's bar.</summary>
	public string Panels { get; init; } = "Panels";

	/// <summary>A side bar of pinned groups.</summary>
	public string SideBar { get; init; } = "Pinned panels";

	/// <summary>What the layout announces when a panel moves, given its title.</summary>
	public Func<string, string> Moved { get; init; } = title => $"{title} moved";

	/// <summary>What the layout announces when a panel floats, given its title.</summary>
	public Func<string, string> Floated { get; init; } = title => $"{title} floating";

	/// <summary>What the layout announces when a panel docks, given its title.</summary>
	public Func<string, string> Docked { get; init; } = title => $"{title} docked";

	/// <summary>What the layout announces when a panel closes, given its title.</summary>
	public Func<string, string> Closed { get; init; } = title => $"{title} closed";

	/// <summary>What the layout announces when a group is pinned to a side bar, given its title.</summary>
	public Func<string, string> Pinned { get; init; } = title => $"{title} pinned to the side";
}

/// <summary>Where a <see cref="MokaDockLayout" /> with a <see cref="MokaDockLayout.StorageKey" /> keeps its state.</summary>
public interface IMokaDockStorage
{
	/// <summary>Saves a value under a key.</summary>
	/// <param name="key">The key.</param>
	/// <param name="value">The value.</param>
	/// <returns>A task that completes once it is saved.</returns>
	Task SaveAsync(string key, string value);

	/// <summary>Loads the value saved under a key.</summary>
	/// <param name="key">The key.</param>
	/// <returns>The value, or null when there is none.</returns>
	Task<string?> LoadAsync(string key);

	/// <summary>Removes the value saved under a key.</summary>
	/// <param name="key">The key.</param>
	/// <returns>A task that completes once it is removed.</returns>
	Task RemoveAsync(string key);
}

/// <summary>Registers the services a <see cref="MokaDockLayout" /> can use.</summary>
public static class MokaDockServiceCollectionExtensions
{
	/// <summary>
	///     Saves dock layouts with a storage of the app's own (a database, the user's profile) in place of the browser's
	///     local storage, which a layout uses when none is registered.
	/// </summary>
	/// <typeparam name="TStorage">The storage.</typeparam>
	/// <param name="services">The services.</param>
	/// <returns>The services.</returns>
	public static IServiceCollection AddMokaDockStorage<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStorage>(
		this IServiceCollection services)
		where TStorage : class, IMokaDockStorage
	{
		ArgumentNullException.ThrowIfNull(services);
		services.Replace(ServiceDescriptor.Scoped<IMokaDockStorage, TStorage>());
		return services;
	}

	/// <summary>
	///     Lets panels open in another browser window on Blazor Server: the window's circuit finds the panel through a
	///     service the two circuits share. A Blazor Server host also needs
	///     <c>AddAdditionalAssemblies(typeof(MokaDockWindowPage).Assembly)</c> for the window's page to load. WebAssembly
	///     needs no service: the windows talk over a <c>BroadcastChannel</c>.
	/// </summary>
	/// <param name="services">The services.</param>
	/// <returns>The services.</returns>
	public static IServiceCollection AddMokaDockWindows(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		services.TryAddSingleton<MokaDockWindows>();
		return services;
	}
}
