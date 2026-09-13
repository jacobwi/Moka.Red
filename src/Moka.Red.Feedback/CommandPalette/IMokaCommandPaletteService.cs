using System.Diagnostics.CodeAnalysis;

namespace Moka.Red.Feedback.CommandPalette;

/// <summary>
///     Service for managing commands and controlling the command palette overlay.
/// </summary>
[SuppressMessage("Design", "CA1003:Use generic event handler instances",
	Justification = "Action delegates are simpler for lightweight service events.")]
public interface IMokaCommandPaletteService
{
	/// <summary>Snapshot of all registered commands, in registration order.</summary>
	IReadOnlyList<MokaCommand> Commands { get; }

	/// <summary>
	///     Whether the palette is currently open. Assigning a different value raises
	///     <see cref="OnToggle" />, so setting this is equivalent to calling
	///     <see cref="Open" /> or <see cref="Close" />.
	/// </summary>
	bool IsOpen { get; set; }

	/// <summary>Fires when the palette should open/close.</summary>
	event Action? OnToggle;

	/// <summary>Register a command. Duplicates by Id are ignored.</summary>
	void Register(MokaCommand command);

	/// <summary>Register multiple commands at once.</summary>
	void RegisterMany(IEnumerable<MokaCommand> commands);

	/// <summary>Remove a command by Id.</summary>
	void Unregister(string id);

	/// <summary>Open the palette.</summary>
	void Open();

	/// <summary>Close the palette.</summary>
	void Close();

	/// <summary>Toggle the palette.</summary>
	void Toggle();
}
