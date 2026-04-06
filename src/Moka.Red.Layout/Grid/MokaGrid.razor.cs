using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Layout;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Grid;

/// <summary>
///     CSS Grid layout component. Specify columns as a simple count or a custom template.
///     Supports responsive breakpoints via suffixed parameters or the fully programmable <see cref="Breakpoints" /> list.
/// </summary>
public partial class MokaGrid : MokaVisualComponentBase
{
	private static int _nextId;
	private readonly string _uniqueClass = $"moka-grid-{Interlocked.Increment(ref _nextId)}";
	private string? _responsiveStyles;

	/// <summary>The child content to render inside the grid.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Number of equal-width columns. Default 1.</summary>
	[Parameter]
	public int Columns { get; set; } = 1;

	/// <summary>Custom grid-template-columns value. Overrides <see cref="Columns" /> when set.</summary>
	[Parameter]
	public string? ColumnsValue { get; set; }

	/// <summary>Number of equal-height rows. Null = auto rows (content-driven).</summary>
	[Parameter]
	public int? Rows { get; set; }

	/// <summary>Custom grid-template-rows value. Overrides <see cref="Rows" /> when set.</summary>
	[Parameter]
	public string? RowsValue { get; set; }

	/// <summary>Columns at md breakpoint (>=768px).</summary>
	[Parameter]
	public int? ColumnsMd { get; set; }

	/// <summary>Columns at lg breakpoint (>=1024px).</summary>
	[Parameter]
	public int? ColumnsLg { get; set; }

	/// <summary>Columns at xl breakpoint (>=1280px).</summary>
	[Parameter]
	public int? ColumnsXl { get; set; }

	/// <summary>
	///     Custom responsive breakpoints. When set, these override ColumnsMd/ColumnsLg/ColumnsXl.
	///     Each breakpoint defines what changes at a specific viewport width.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaBreakpoint>? Breakpoints { get; set; }

	/// <summary>Gap between grid items from the spacing scale.</summary>
	[Parameter]
	public MokaSpacingScale? Gap { get; set; }

	/// <summary>Custom gap value. Overrides <see cref="Gap" /> enum.</summary>
	[Parameter]
	public string? GapValue { get; set; }

	/// <summary>Row gap from the spacing scale (when different from column gap).</summary>
	[Parameter]
	public MokaSpacingScale? RowGap { get; set; }

	/// <summary>Custom row gap value. Overrides <see cref="RowGap" /> enum.</summary>
	[Parameter]
	public string? RowGapValue { get; set; }

	/// <summary>Justify items within their grid area.</summary>
	[Parameter]
	public MokaJustify JustifyItems { get; set; } = MokaJustify.Start;

	/// <summary>Align items within their grid area.</summary>
	[Parameter]
	public MokaAlign AlignItems { get; set; } = MokaAlign.Stretch;

	/// <summary>Whether to use inline-grid instead of grid.</summary>
	[Parameter]
	public bool Inline { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-grid";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(_uniqueClass, HasBreakpoints)
		.AddClass(ColumnsMd.HasValue && !HasBreakpoints ? $"moka-grid--md-{ColumnsMd.Value}" : null)
		.AddClass(ColumnsLg.HasValue && !HasBreakpoints ? $"moka-grid--lg-{ColumnsLg.Value}" : null)
		.AddClass(ColumnsXl.HasValue && !HasBreakpoints ? $"moka-grid--xl-{ColumnsXl.Value}" : null)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("display", Inline ? "inline-grid" : "grid")
		.AddStyle("grid-template-columns", ColumnsValue ?? $"repeat({Columns}, 1fr)")
		.AddStyle("grid-template-rows", RowsValue ?? (Rows.HasValue ? $"repeat({Rows.Value}, 1fr)" : null))
		.AddStyle("gap", ResolvedGap)
		.AddStyle("row-gap", ResolvedRowGap)
		.AddStyle("justify-items", MokaEnumHelpers.ToCssValue(JustifyItems), JustifyItems != MokaJustify.Start)
		.AddStyle("align-items", MokaEnumHelpers.ToCssValue(AlignItems), AlignItems != MokaAlign.Stretch)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private bool HasBreakpoints => Breakpoints is not null && Breakpoints.Count > 0;
	private string? ResolvedGap => GapValue ?? (Gap.HasValue ? MokaEnumHelpers.ToCssValue(Gap.Value) : null);

	private string? ResolvedRowGap =>
		RowGapValue ?? (RowGap.HasValue ? MokaEnumHelpers.ToCssValue(RowGap.Value) : null);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		_responsiveStyles = HasBreakpoints
			? MokaResponsiveStyleBuilder.BuildGridStyles(_uniqueClass, Breakpoints!)
			: null;
	}
}
