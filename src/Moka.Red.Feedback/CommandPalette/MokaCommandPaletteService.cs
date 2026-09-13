namespace Moka.Red.Feedback.CommandPalette;

/// <summary>
///     Default implementation of <see cref="IMokaCommandPaletteService" />.
///     Thread-safe; maintains a list of commands and notifies subscribers when the palette toggles.
/// </summary>
public sealed class MokaCommandPaletteService : IMokaCommandPaletteService
{
	private readonly Dictionary<string, MokaCommand> _byId = new(StringComparer.Ordinal);
	private readonly object _lock = new();

	// The dictionary answers "is this Id taken?" in O(1); the list keeps registration order,
	// which is what the palette shows before the user types anything.
	private readonly List<MokaCommand> _ordered = [];
	private bool _isOpen;

	/// <inheritdoc />
	public IReadOnlyList<MokaCommand> Commands
	{
		get
		{
			lock (_lock)
			{
				return _ordered.ToArray();
			}
		}
	}

	/// <inheritdoc />
	public bool IsOpen
	{
		get
		{
			lock (_lock)
			{
				return _isOpen;
			}
		}
		set => SetOpen(value);
	}

	/// <inheritdoc />
	public event Action? OnToggle;

	/// <inheritdoc />
	public void Register(MokaCommand command)
	{
		ArgumentNullException.ThrowIfNull(command);

		lock (_lock)
		{
			if (_byId.TryAdd(command.Id, command))
			{
				_ordered.Add(command);
			}
		}
	}

	/// <inheritdoc />
	public void RegisterMany(IEnumerable<MokaCommand> commands)
	{
		ArgumentNullException.ThrowIfNull(commands);

		foreach (MokaCommand command in commands)
		{
			Register(command);
		}
	}

	/// <inheritdoc />
	public void Unregister(string id)
	{
		lock (_lock)
		{
			if (_byId.Remove(id))
			{
				_ordered.RemoveAll(c => string.Equals(c.Id, id, StringComparison.Ordinal));
			}
		}
	}

	/// <inheritdoc />
	public void Open() => SetOpen(true);

	/// <inheritdoc />
	public void Close() => SetOpen(false);

	/// <inheritdoc />
	public void Toggle()
	{
		lock (_lock)
		{
			_isOpen = !_isOpen;
		}

		OnToggle?.Invoke();
	}

	// Raised outside the lock: subscribers render, and rendering can call back in.
	private void SetOpen(bool value)
	{
		bool changed;

		lock (_lock)
		{
			changed = _isOpen != value;
			_isOpen = value;
		}

		if (changed)
		{
			OnToggle?.Invoke();
		}
	}
}
