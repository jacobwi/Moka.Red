namespace Moka.Red.Core.Hotkeys;

/// <summary>
///     Whether a <see cref="MokaHotkey" /> fires while focus is in an editable field: a text input, a text area, a select
///     or anything content-editable.
/// </summary>
public enum MokaHotkeyFields
{
	/// <summary>
	///     Fires in a field unless the shortcut types a character (no Ctrl, Cmd or Alt, such as "/" or "Shift+K"), so the
	///     character still reaches the field. A shortcut with Ctrl, Cmd or Alt fires there too. The default.
	/// </summary>
	UnlessTyping,

	/// <summary>
	///     Never fires while focus is in a field, so the field keeps its own keys: an app-wide Ctrl+Z leaves a text box's
	///     own undo alone.
	/// </summary>
	Never,

	/// <summary>Fires in fields too, even a shortcut that types a character.</summary>
	Always
}
