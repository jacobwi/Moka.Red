namespace Moka.Red.Feedback.CommandPalette;

/// <summary>
///     Default implementation of <see cref="IMokaCommandPaletteService" />.
///     Maintains a list of commands and notifies subscribers when the palette toggles.
/// </summary>
public sealed class MokaCommandPaletteService : IMokaCommandPaletteService
{
	private readonly List<MokaCommand> _commands = [];

	/// <inheritdoc />
	public IReadOnlyList<MokaCommand> Commands => _commands;

	/// <inheritdoc />
	public bool IsOpen { get; set; }

	/// <inheritdoc />
	public event Action? OnToggle;

	/// <inheritdoc />
	public void Register(MokaCommand command)
	{
		if (_commands.All(c => c.Id != command.Id))
		{
			_commands.Add(command);
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
	public void Unregister(string id) => _commands.RemoveAll(c => c.Id == id);

	/// <inheritdoc />
	public void Open()
	{
		if (!IsOpen)
		{
			IsOpen = true;
			OnToggle?.Invoke();
		}
	}

	/// <inheritdoc />
	public void Close()
	{
		if (IsOpen)
		{
			IsOpen = false;
			OnToggle?.Invoke();
		}
	}

	/// <inheritdoc />
	public void Toggle()
	{
		IsOpen = !IsOpen;
		OnToggle?.Invoke();
	}
}
