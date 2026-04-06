namespace Moka.Red.Data.Table;

/// <summary>
///     Result returned from the ServerData callback.
/// </summary>
/// <typeparam name="TItem">The row data type.</typeparam>
public sealed record MokaTableResult<TItem>
{
	/// <summary>The items for the current page.</summary>
	public required IReadOnlyList<TItem> Items { get; init; }

	/// <summary>Total number of items across all pages (for pagination).</summary>
	public required int TotalItems { get; init; }
}
