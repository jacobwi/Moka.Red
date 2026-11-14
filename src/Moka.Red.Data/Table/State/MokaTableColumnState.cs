namespace Moka.Red.Data.Table;

/// <summary>How someone arranged one column of a <see cref="MokaTable{TItem}" />, named by its <c>Title</c>.</summary>
/// <param name="Title">The column's title.</param>
public sealed record MokaTableColumnState(string Title)
{
	/// <summary>Whether the column is hidden.</summary>
	public bool Hidden { get; init; }

	/// <summary>The width someone dragged it to, in pixels, or null for its own.</summary>
	public double? Width { get; init; }

	/// <summary>The edge it stays at while the rows scroll sideways.</summary>
	public MokaColumnPin Pin { get; init; }
}
