using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Layout;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Flexbox;

/// <summary>
///     Flexbox layout component. Renders a flex container with configurable
///     direction, alignment, gap, and wrapping. Column direction by default.
///     Supports fully programmable responsive breakpoints via <see cref="Breakpoints" />.
/// </summary>
public partial class MokaFlexbox : MokaVisualComponentBase
{
	private static int _nextId;
	private readonly string _uniqueClass = $"moka-flex-{Interlocked.Increment(ref _nextId)}";
	private string? _responsiveStyles;

	/// <summary>The child content to render inside the flex container.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Flex direction. Defaults to Column (vertical).</summary>
	[Parameter]
	public MokaDirection Direction { get; set; } = MokaDirection.Column;

	/// <summary>Justify-content alignment along the main axis.</summary>
	[Parameter]
	public MokaJustify Justify { get; set; } = MokaJustify.Start;

	/// <summary>Align-items alignment along the cross axis.</summary>
	[Parameter]
	public MokaAlign Align { get; set; } = MokaAlign.Stretch;

	/// <summary>Gap between items from the spacing scale.</summary>
	[Parameter]
	public MokaSpacingScale? Gap { get; set; }

	/// <summary>Custom gap value. Overrides <see cref="Gap" /> enum.</summary>
	[Parameter]
	public string? GapValue { get; set; }

	/// <summary>Whether items should wrap when they overflow.</summary>
	[Parameter]
	public bool Wrap { get; set; }

	/// <summary>Whether to use inline-flex instead of flex.</summary>
	[Parameter]
	public bool Inline { get; set; }

	/// <summary>
	///     Custom responsive breakpoints. Each breakpoint defines what changes at a specific viewport width.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaBreakpoint>? Breakpoints { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-flexbox";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(_uniqueClass, HasBreakpoints)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("display", Inline ? "inline-flex" : "flex")
		.AddStyle("flex-direction", MokaEnumHelpers.ToCssValue(Direction))
		.AddStyle("justify-content", MokaEnumHelpers.ToCssValue(Justify))
		.AddStyle("align-items", MokaEnumHelpers.ToCssValue(Align))
		.AddStyle("flex-wrap", "wrap", Wrap)
		.AddStyle("gap", ResolvedGap)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private bool HasBreakpoints => Breakpoints is not null && Breakpoints.Count > 0;
	private string? ResolvedGap => GapValue ?? (Gap.HasValue ? MokaEnumHelpers.ToCssValue(Gap.Value) : null);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		_responsiveStyles = HasBreakpoints
			? MokaResponsiveStyleBuilder.BuildFlexboxStyles(_uniqueClass, Breakpoints!)
			: null;
	}
}
