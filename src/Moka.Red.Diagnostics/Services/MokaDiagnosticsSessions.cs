using System.Collections.Concurrent;
using System.Security.Cryptography;

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
	private readonly ConcurrentDictionary<string, IMokaDiagnosticsService> _sessions = new(StringComparer.Ordinal);

	/// <summary>Registers <paramref name="service" /> and returns the key that finds it.</summary>
	public string Add(IMokaDiagnosticsService service)
	{
		ArgumentNullException.ThrowIfNull(service);
		string key = RandomNumberGenerator.GetHexString(32, lowercase: true);
		_sessions[key] = service;
		return key;
	}

	/// <summary>Forgets the service registered under <paramref name="key" />.</summary>
	public void Remove(string key) => _sessions.TryRemove(key, out _);

	/// <summary>The service registered under <paramref name="key" />, or <c>null</c>.</summary>
	public IMokaDiagnosticsService? Find(string? key) =>
		key is not null && _sessions.TryGetValue(key, out IMokaDiagnosticsService? service) ? service : null;
}
