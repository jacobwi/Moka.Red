namespace Moka.Red.Data.Table;

/// <summary>How a <see cref="MokaTable{TItem}" /> edits its rows.</summary>
public enum MokaTableEditMode
{
	/// <summary>
	///     One cell at a time: Enter, F2 or a double click opens the cell's editor, Enter or leaving the cell saves it, Tab
	///     saves it and opens the next editable cell, and Escape puts it back.
	/// </summary>
	Cell,

	/// <summary>
	///     A whole row at a time: every editable cell of the row opens together, with Save and Cancel at the row's end.
	///     Enter saves the row and Escape puts it back.
	/// </summary>
	Row
}

/// <summary>Where a <see cref="MokaTable{TItem}" /> puts the row it adds.</summary>
public enum MokaTableAddPosition
{
	/// <summary>Above the other rows.</summary>
	Top,

	/// <summary>Under the other rows.</summary>
	Bottom
}

/// <summary>The editor a column's cells open with.</summary>
public enum MokaColumnEditor
{
	/// <summary>
	///     Picked from the column: its kind, then its choices (<see cref="MokaColumn{TItem}.EditOptions" /> or
	///     <see cref="MokaColumn{TItem}.FilterOptions" />), then the value's type: a box for yes or no, a number field for a
	///     number, a date picker for a date, a list for an enum, else a text field.
	/// </summary>
	Auto,

	/// <summary>A text field.</summary>
	Text,

	/// <summary>A number field.</summary>
	Number,

	/// <summary>A date picker.</summary>
	Date,

	/// <summary>A list of choices.</summary>
	Select,

	/// <summary>A check box.</summary>
	Checkbox,

	/// <summary>A switch.</summary>
	Switch,

	/// <summary>Stars.</summary>
	Rating
}

/// <summary>Why a <see cref="MokaTable{TItem}" /> asks for a row to be saved.</summary>
public enum MokaTableSaveKind
{
	/// <summary>Someone edited the row.</summary>
	Edit,

	/// <summary>Someone added the row.</summary>
	Add,

	/// <summary>Someone pasted cells into the row.</summary>
	Paste,

	/// <summary>Someone undid the last save, which puts the values it changed back.</summary>
	Undo
}
