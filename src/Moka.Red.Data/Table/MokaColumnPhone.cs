namespace Moka.Red.Data.Table;

/// <summary>
///     Where a column's cell goes when a <see cref="MokaTable{TItem}" /> turns into a list below 720 px. A table
///     becomes a list as soon as one of its columns names a slot.
/// </summary>
public enum MokaColumnPhone
{
	/// <summary>The first line of the row, larger.</summary>
	Primary,

	/// <summary>The second line. Several secondary columns share it, joined with a dot.</summary>
	Secondary,

	/// <summary>At the right, beside both lines: a value, a status or a button.</summary>
	Trailing,

	/// <summary>Not shown in the list.</summary>
	Hidden
}
