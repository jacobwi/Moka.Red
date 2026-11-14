namespace Moka.Red.Data.Table;

/// <summary>How a <see cref="MokaTableFilter" /> compares a column's values with its own.</summary>
public enum MokaFilterOperator
{
	/// <summary>The text holds the value, ignoring case.</summary>
	Contains,

	/// <summary>The text doesn't hold the value, ignoring case.</summary>
	DoesNotContain,

	/// <summary>The value equals it: text ignoring case, numbers, dates and yes or no by value.</summary>
	Is,

	/// <summary>The value differs from it.</summary>
	IsNot,

	/// <summary>The text starts with the value, ignoring case.</summary>
	StartsWith,

	/// <summary>The text ends with the value, ignoring case.</summary>
	EndsWith,

	/// <summary>The value is missing or empty text.</summary>
	IsEmpty,

	/// <summary>The value is there and isn't empty text.</summary>
	IsNotEmpty,

	/// <summary>The value is more than it, or later for a date.</summary>
	GreaterThan,

	/// <summary>The value is at least it.</summary>
	AtLeast,

	/// <summary>The value is less than it, or earlier for a date.</summary>
	LessThan,

	/// <summary>The value is at most it.</summary>
	AtMost,

	/// <summary>The value lies between the first and second value, both included.</summary>
	Between,

	/// <summary>The value equals one of the values.</summary>
	IsAnyOf,

	/// <summary>The value equals none of the values.</summary>
	IsNoneOf
}
