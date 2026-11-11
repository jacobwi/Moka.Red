namespace Moka.Red.Data.FileBrowser;

/// <summary>How the rows and the selection of a <see cref="MokaFileBrowser{TItem}" /> look, in every view.</summary>
public enum MokaFileRowStyle
{
	/// <summary>Lines between rows, and the selection a tint with a bar on its edge. The default.</summary>
	Standard,

	/// <summary>Every other row a shade darker and no lines, so the eye follows a wide row across.</summary>
	Zebra,

	/// <summary>Taller rows with no lines, and the selection a rounded pill inset from the edges.</summary>
	Airy,

	/// <summary>The selection an accent ring with no fill, so the row reads as it did.</summary>
	Outlined,

	/// <summary>
	///     Monospace type. The list is laid out like <c>ls -l</c> and the tiles like plain <c>ls</c>, and the selection is
	///     an accent cursor line.
	/// </summary>
	Terminal,

	/// <summary>The selection filled with the accent and its text on top.</summary>
	Solid
}
