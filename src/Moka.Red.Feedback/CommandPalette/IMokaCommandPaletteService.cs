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

	/// <summary>
	///     The <see cref="MokaCommand.Id" />s of the commands run from the palette, the most recent first, each once. The
	///     palette lists them under its <see cref="MokaCommandPaletteText.Recent" /> heading with
	///     <see cref="MokaCommandPalette.ShowRecent" />. Ids whose command is gone stay in the list and are skipped there,
	///     so a command registered again later comes back. An implementation that doesn't keep them answers an empty list.
	/// </summary>
	IReadOnlyList<string> Recent => [];

	/// <summary>Raised when <see cref="Recent" /> changes, so an app can store the list.</summary>
	event Action? RecentChanged
	{
		add { }
		remove { }
	}

	/// <summary>
	///     Replaces <see cref="Recent" />: to restore a list the app stored, or to clear it with an empty one. Repeated ids
	///     keep their first place. The palette calls it with the command it runs moved to the front.
	/// </summary>
	/// <param name="ids">The ids, the most recent first.</param>
	void SetRecent(IEnumerable<string> ids)
	{
	}
}
