namespace Moka.Red.Core.Hotkeys;

/// <summary>
///     Default <see cref="IMokaHotkeyService" />. Holds the registered set and tells the host when it changes.
/// </summary>
public sealed class MokaHotkeyService : IMokaHotkeyService
{
	// Insertion order is what a shortcut list shows, so the order registrations arrive in is kept.
	private readonly List<MokaHotkey> _hotkeys = [];

	/// <inheritdoc />
	public event Action? OnChanged;

	/// <inheritdoc />
	public IReadOnlyList<MokaHotkey> Hotkeys => _hotkeys;

	/// <inheritdoc />
	public IDisposable Register(MokaHotkey hotkey)
	{
		ArgumentNullException.ThrowIfNull(hotkey);

		int existing = _hotkeys.FindIndex(h => string.Equals(h.Id, hotkey.Id, StringComparison.Ordinal));
		if (existing >= 0)
		{
			// Replacing in place rather than appending: a component that re-registers on every parameter set
			// would otherwise grow the list without bound, and the shortcut list with it.
			_hotkeys[existing] = hotkey;
		}
		else
		{
			_hotkeys.Add(hotkey);
		}

		OnChanged?.Invoke();
		return new Registration(this, hotkey.Id);
	}

	/// <inheritdoc />
	public void Unregister(string id)
	{
		ArgumentNullException.ThrowIfNull(id);

		if (_hotkeys.RemoveAll(h => string.Equals(h.Id, id, StringComparison.Ordinal)) > 0)
		{
			OnChanged?.Invoke();
		}
	}

	/// <inheritdoc />
	public Task InvokeAsync(string id)
	{
		ArgumentNullException.ThrowIfNull(id);

		MokaHotkey? hotkey = _hotkeys.Find(h => string.Equals(h.Id, id, StringComparison.Ordinal));
		if (hotkey is null)
		{
			return Task.CompletedTask;
		}

		if (hotkey.OnInvoke is not null)
		{
			return hotkey.OnInvoke();
		}

		hotkey.OnInvokeSync?.Invoke();
		return Task.CompletedTask;
	}

	// Disposing twice must not unregister whatever took the id in the meantime.
	private sealed class Registration(MokaHotkeyService service, string id) : IDisposable
	{
		private bool _disposed;

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			service.Unregister(id);
		}
	}
}
