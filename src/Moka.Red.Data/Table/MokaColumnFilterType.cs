namespace Moka.Red.Data.Table;

/// <summary>What a column's filter controls take, in the filter row, a heading's menu and the builder.</summary>
public enum MokaColumnFilterType
{
	/// <summary>
	///     From the column's values: numbers take a range, dates a date range, yes or no a choice of the two, an enum a list,
	///     and anything else text. The default.
	/// </summary>
	Auto,

	/// <summary>Free text the cell's text holds.</summary>
	Text,

	/// <summary>A range of numbers, either end optional.</summary>
	Number,

	/// <summary>A range of dates, either end optional, compared by the day.</summary>
	Date,

	/// <summary>One value picked from a list of the column's values.</summary>
	Select,

	/// <summary>Any number of values picked from a list of the column's values.</summary>
	MultiSelect,

	/// <summary>Yes, no or either.</summary>
	YesNo
}
