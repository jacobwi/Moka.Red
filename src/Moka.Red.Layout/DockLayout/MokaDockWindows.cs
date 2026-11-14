using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Moka.Red.Layout.DockLayout;

/// <summary>
///     Links a panel opened in another browser window to the layout it came from, on Blazor Server. Both windows are
///     circuits of the same app in the same process, so they share this singleton: the window's page finds the panel
///     by the session key in its address and follows its parameters, and the layout hears when the window closes.
///     Registered by <see cref="MokaDockServiceCollectionExtensions.AddMokaDockWindows" />.
/// </summary>
public sealed class MokaDockWindows
{
	private readonly ConcurrentDictionary<string, MokaDockWindowSession> _sessions = new(StringComparer.Ordinal);

	/// <summary>Starts a session for a panel and returns it, with the key the window's address carries.</summary>
	/// <param name="panel">The panel's id in its layout.</param>
	/// <param name="title">The window's title.</param>
	/// <param name="component">The component the window shows, with its parameters.</param>
	/// <returns>The session.</returns>
	public MokaDockWindowSession Open(string panel, string title, MokaDockComponent component)
	{
		ArgumentNullException.ThrowIfNull(component);
		string key = RandomNumberGenerator.GetHexString(32, lowercase: true);
		MokaDockWindowSession session = new(this, key, panel, title, component);
		_sessions[key] = session;
		return session;
	}

	/// <summary>The session with a key, or null.</summary>
	/// <param name="key">The key from the window's address.</param>
	/// <returns>The session, or null.</returns>
	public MokaDockWindowSession? Find(string? key) =>
		key is not null && _sessions.TryGetValue(key, out MokaDockWindowSession? session) ? session : null;

	internal void Remove(string key) => _sessions.TryRemove(key, out _);
}

/// <summary>One panel shown in another window. See <see cref="MokaDockWindows" />.</summary>
public sealed class MokaDockWindowSession
{
	private readonly MokaDockWindows _owner;

	internal MokaDockWindowSession(MokaDockWindows owner, string key, string panel, string title, MokaDockComponent component)
	{
		_owner = owner;
		Key = key;
		Panel = panel;
		Title = title;
		Component = component;
	}

	/// <summary>Raised, on the caller's thread, when the layout passes the panel new parameters.</summary>
	public event EventHandler? Changed;

	/// <summary>Raised, on the caller's thread, when the window asks to put the panel back in its layout.</summary>
	public event EventHandler? ReturnRequested;

	/// <summary>The key the window's address carries.</summary>
	public string Key { get; }

	/// <summary>The panel's id in its layout.</summary>
	public string Panel { get; }

	/// <summary>The window's title.</summary>
	public string Title { get; private set; }

	/// <summary>The component the window shows, with its parameters.</summary>
	public MokaDockComponent Component { get; private set; }

	/// <summary>Whether the session has ended: the window closed, or the panel went back.</summary>
	public bool Ended { get; private set; }

	/// <summary>Asks the layout to take the panel back, as the window's Bring back button does.</summary>
	public void RequestReturn() => ReturnRequested?.Invoke(this, EventArgs.Empty);

	internal void Update(string title, MokaDockComponent component)
	{
		if (Title == title && Equals(Component, component))
		{
			return;
		}

		Title = title;
		Component = component;
		Changed?.Invoke(this, EventArgs.Empty);
	}

	internal void End()
	{
		Ended = true;
		_owner.Remove(Key);
		Changed?.Invoke(this, EventArgs.Empty);
	}
}
