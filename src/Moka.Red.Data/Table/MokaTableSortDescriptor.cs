namespace Moka.Red.Data.Table;

/// <summary>
///     Describes a single sort column in a multi-column sort.
/// </summary>
public sealed class MokaTableSortDescriptor
{
	/// <summary>The column title being sorted.</summary>
	public string Column { get; set; } = string.Empty;

	/// <summary>Sort direction.</summary>
	public MokaSortDirection Direction { get; set; } = MokaSortDirection.Ascending;

	/// <summary>1-based priority (lower = sorted first).</summary>
	public int Priority { get; set; }
}
