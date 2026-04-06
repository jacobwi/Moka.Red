namespace Moka.Red.Navigation.Tabs.Services;

/// <summary>
///     Abstraction for persisting tab state to a storage medium (sessionStorage, localStorage, server-side cache, etc.).
/// </summary>
public interface ITabStorageProvider
{
	/// <summary>
	///     Saves a string value under the specified key.
	/// </summary>
	Task SaveAsync(string key, string value);

	/// <summary>
	///     Loads the string value stored under the specified key, or null if not found.
	/// </summary>
	Task<string?> LoadAsync(string key);

	/// <summary>
	///     Removes the value stored under the specified key.
	/// </summary>
	Task RemoveAsync(string key);
}
