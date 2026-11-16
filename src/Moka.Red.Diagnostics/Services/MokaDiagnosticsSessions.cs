using System.Collections.Concurrent;
using System.Security.Cryptography;
using Moka.Red.Core.Theming;

namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Lets the diagnostics page in a popup show the data of the window that opened it. On Blazor Server
///     the popup runs its own circuit with its own scoped <see cref="IMokaDiagnosticsService" />, so the
///     overlay registers its service here and passes the key in the popup's URL. The key is random, so a
///     window can only reach a session whose URL it was given.
/// </summary>
/// <remarks>
///     A singleton, so this only links windows served by the same process. A WebAssembly popup is a
///     separate app instance with its own registry and falls back to its own data.
/// </remarks>
internal sealed class MokaDiagnosticsSessions
{
	private readonly ConcurrentDictionary<string, MokaDiagnosticsSession> _sessions = new(StringComparer.Ordinal);

	/// <summary>Registers <paramref name="service" /> and returns its session, whose key finds it.</summary>
	public MokaDiagnosticsSession Add(IMokaDiagnosticsService service, MokaTheme? theme = null)
	{
		ArgumentNullException.ThrowIfNull(service);
		var session = new MokaDiagnosticsSession(RandomNumberGenerator.GetHexString(32, lowercase: true), service, theme);
		_sessions[session.Key] = session;
		return session;
	}

	/// <summary>Ends and forgets the session registered under <paramref name="key" />.</summary>
	public void Remove(string key)
	{
		if (_sessions.TryRemove(key, out MokaDiagnosticsSession? session))
		{
			session.End();
		}
	}

	/// <summary>The session registered under <paramref name="key" />, or <c>null</c>.</summary>
	public MokaDiagnosticsSession? Find(string? key) =>
		key is not null && _sessions.TryGetValue(key, out MokaDiagnosticsSession? session) ? session : null;
}

// What the window that opened the diagnostics page shares with it: its service and the theme its overlay inspects. The
// overlay and the page run on different circuits, so Changed is raised on the overlay's thread.
internal sealed class MokaDiagnosticsSession(string key, IMokaDiagnosticsService service, MokaTheme? theme)
{
	private volatile bool _ended;
	private volatile MokaTheme? _theme = theme;

	public string Key { get; } = key;

	public IMokaDiagnosticsService Service { get; } = service;

	public MokaTheme? Theme => _theme;

	// The opener's overlay has gone, so nothing new will arrive.
	public bool Ended => _ended;

	public event EventHandler? Changed;

	public void ShowTheme(MokaTheme? theme)
	{
		if (_ended || ReferenceEquals(_theme, theme))
		{
			return;
		}

		_theme = theme;
		Changed?.Invoke(this, EventArgs.Empty);
	}

	internal void End()
	{
		_ended = true;
		Changed?.Invoke(this, EventArgs.Empty);
	}
}
