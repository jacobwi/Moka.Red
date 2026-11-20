namespace Moka.Red.Core.Theming;

/// <summary>
///     How a <c>MokaTable</c> draws its rows. Every look but <see cref="Lined" /> and <see cref="List" /> is drawn on the
///     new design only; on the original design the table draws <see cref="Lined" /> whatever is asked. A table takes its
///     own <c>Look</c>, else the theme's <c>Tables</c>, else <see cref="Lined" />.
/// </summary>
public enum MokaTableLook
{
	/// <summary>A line under each row. The default, and the table as it has always looked.</summary>
	Lined,

	/// <summary>Every other row softly tinted, with no lines between them.</summary>
	SoftStripes,

	/// <summary>Each row its own small card, with a gap between them.</summary>
	Cards,

	/// <summary>Boxed cells, row numbers, figures in monospace and tight rows, like a spreadsheet.</summary>
	Sheet,

	/// <summary>Roomy rows with no lines, for a few rows people read. Comfortable unless a density is set.</summary>
	Airy,

	/// <summary>A bordered panel with a tinted band behind the headings.</summary>
	Framed,

	/// <summary>No heading row: two lines per row from the columns' phone slots, at every width, on both designs.</summary>
	List
}

/// <summary>
///     How tightly a <c>MokaTable</c> spaces its rows. A table takes its state's density, else its own <c>Density</c>,
///     else the theme's <c>TableDensity</c>, else its look's own.
/// </summary>
public enum MokaTableDensity
{
	/// <summary>Tight rows, for lists people scan. The default on most looks.</summary>
	Compact,

	/// <summary>A step roomier.</summary>
	Standard,

	/// <summary>The roomiest, for rows people read.</summary>
	Comfortable
}
