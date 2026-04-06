namespace Moka.Red.Data.Table;

/// <summary>
///     Aggregation function type for table column footers.
/// </summary>
public enum MokaAggregateType
{
	/// <summary>No aggregation.</summary>
	None,

	/// <summary>Sum of numeric values.</summary>
	Sum,

	/// <summary>Average of numeric values.</summary>
	Average,

	/// <summary>Count of non-null values.</summary>
	Count,

	/// <summary>Minimum value.</summary>
	Min,

	/// <summary>Maximum value.</summary>
	Max
}
