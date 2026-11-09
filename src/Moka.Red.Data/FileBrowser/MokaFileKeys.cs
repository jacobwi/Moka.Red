namespace Moka.Red.Data.FileBrowser;

/// <summary>Where focus has to be for a <see cref="MokaFileBrowser{TItem}" /> to take its keys.</summary>
public enum MokaFileKeyScope
{
	/// <summary>On the list. The default.</summary>
	List,

	/// <summary>
	///     Anywhere in the browser, toolbars and panes included. Never in a field or a control with keys of its own, such
	///     as a menu, and Enter and Space stay with a focused button or link.
	/// </summary>
	Browser
}

/// <summary>
///     The keys a <see cref="MokaFileBrowser{TItem}" /> takes. A key left out reaches the page as if the browser weren't
///     there, so a host can keep it for itself.
/// </summary>
[Flags]
public enum MokaFileKeys
{
	/// <summary>None of them.</summary>
	None = 0,

	/// <summary>Arrows, Home, End, Page Up and Page Down, alone or with Shift, and all but the page keys with Control.</summary>
	Move = 1 << 0,

	/// <summary>Space, which toggles the row under the cursor.</summary>
	Toggle = 1 << 1,

	/// <summary>Control+A.</summary>
	SelectAll = 1 << 2,

	/// <summary>Enter.</summary>
	Open = 1 << 3,

	/// <summary>Backspace, which goes up a folder.</summary>
	Up = 1 << 4,

	/// <summary>Alt+Left and Alt+Right.</summary>
	History = 1 << 5,

	/// <summary>F2.</summary>
	Rename = 1 << 6,

	/// <summary>Delete.</summary>
	Delete = 1 << 7,

	/// <summary>Control+C.</summary>
	Copy = 1 << 8,

	/// <summary>Control+X.</summary>
	Cut = 1 << 9,

	/// <summary>Control+V.</summary>
	Paste = 1 << 10,

	/// <summary>Control+D.</summary>
	Duplicate = 1 << 11,

	/// <summary>Escape, which clears the selection and the cursor.</summary>
	Escape = 1 << 12,

	/// <summary>Whether <see cref="Escape" /> also lets go of what is cut or copied. It does nothing without it.</summary>
	EscapeDropsHeld = 1 << 13,

	/// <summary>F5.</summary>
	Refresh = 1 << 14,

	/// <summary>The slash key, which goes to the filter box.</summary>
	Filter = 1 << 15,

	/// <summary>Letters, which jump to the next name starting with them.</summary>
	TypeAhead = 1 << 16,

	/// <summary>Every key. The default.</summary>
	All = Move | Toggle | SelectAll | Open | Up | History | Rename | Delete | Copy | Cut | Paste | Duplicate | Escape |
	      EscapeDropsHeld | Refresh | Filter | TypeAhead
}
