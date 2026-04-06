using Moka.Red.Core.Enums;

namespace Moka.Red.Core.Layout;

/// <summary>
///     Defines responsive overrides that apply at a minimum viewport width.
///     Only set the properties you want to change at this breakpoint — null values inherit from the base component.
/// </summary>
public sealed record MokaBreakpoint
{
	/// <summary>Minimum viewport width where this breakpoint activates. Any CSS width (e.g., "768px", "48rem").</summary>
	public required string MinWidth { get; init; }

	// Grid overrides

	/// <summary>Number of grid columns at this breakpoint.</summary>
	public int? Columns { get; init; }

	/// <summary>Custom grid-template-columns value at this breakpoint.</summary>
	public string? ColumnsValue { get; init; }

	/// <summary>Number of grid rows at this breakpoint.</summary>
	public int? Rows { get; init; }

	/// <summary>Custom grid-template-rows value at this breakpoint.</summary>
	public string? RowsValue { get; init; }

	// Flexbox overrides

	/// <summary>Flex direction at this breakpoint.</summary>
	public MokaDirection? Direction { get; init; }

	/// <summary>Whether items wrap at this breakpoint.</summary>
	public bool? Wrap { get; init; }

	// Shared layout overrides

	/// <summary>Justify-content/justify-items at this breakpoint.</summary>
	public MokaJustify? Justify { get; init; }

	/// <summary>Align-items at this breakpoint.</summary>
	public MokaAlign? Align { get; init; }

	/// <summary>Gap from spacing scale at this breakpoint.</summary>
	public MokaSpacingScale? Gap { get; init; }

	/// <summary>Custom gap value at this breakpoint.</summary>
	public string? GapValue { get; init; }

	/// <summary>Row gap from spacing scale at this breakpoint.</summary>
	public MokaSpacingScale? RowGap { get; init; }

	/// <summary>Custom row gap value at this breakpoint.</summary>
	public string? RowGapValue { get; init; }

	// Visibility

	/// <summary>Whether the component is hidden at this breakpoint.</summary>
	public bool? Hidden { get; init; }
}
