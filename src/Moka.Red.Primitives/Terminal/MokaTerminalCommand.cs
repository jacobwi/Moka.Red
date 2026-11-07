namespace Moka.Red.Primitives.Terminal;

/// <summary>Where a <see cref="MokaTerminalCommand" /> has got to.</summary>
public enum MokaTerminalCommandState
{
	/// <summary>Still running. The dot pulses, and Edit and Run again wait until it ends.</summary>
	Running,

	/// <summary>Ended well.</summary>
	Succeeded,

	/// <summary>Ended badly. The block gets a red border.</summary>
	Failed
}

/// <summary>
///     One command and what it wrote, drawn as a block by <see cref="MokaTerminal.Commands" />. The app runs commands, and
///     the terminal only draws them: pass a new record when the state or the lines change.
/// </summary>
/// <param name="Command">The command as typed.</param>
/// <param name="State">Where the command has got to.</param>
/// <param name="Lines">What it wrote so far.</param>
public sealed record MokaTerminalCommand(
	string Command,
	MokaTerminalCommandState State,
	IReadOnlyList<MokaTerminalLine>? Lines = null);
