namespace Moka.Red.Primitives.Meter;

/// <summary>How a <see cref="MokaMeter" /> lays out its name, bar and figure.</summary>
public enum MokaMeterLayout
{
	/// <summary>The name and the figure on a line, the bar under them. Today's meter.</summary>
	Stacked,

	/// <summary>
	///     Name, bar and figure on one line. Rows inside an element with the <c>moka-meter-rows</c> class line up their
	///     columns, so a list of volumes or quotas reads as a table.
	/// </summary>
	Row
}
