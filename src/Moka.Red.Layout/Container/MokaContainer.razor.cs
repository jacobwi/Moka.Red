using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Container;

/// <summary>
///     A centered max-width content wrapper. Like Bootstrap's container but with Moka theming.
///     Provides horizontal gutters, optional fluid (full-width) mode, and auto-centering.
/// </summary>
public partial class MokaContainer : MokaVisualComponentBase
{
	/// <summary>The child content to render inside the container.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Custom max-width value (e.g., "1200px", "80rem"). Default "1200px".</summary>
	[Parameter]
	public string MaxWidth { get; set; } = "1200px";

	/// <summary>When true, removes max-width constraint for full-width layout with padding.</summary>
	[Parameter]
	public bool Fluid { get; set; }

	/// <summary>When true (default), centers the container with auto margins.</summary>
	[Parameter]
	public bool Centered { get; set; } = true;

	/// <summary>Horizontal padding (left/right) from the spacing scale. Default Lg.</summary>
	[Parameter]
	public MokaSpacingScale? GutterX { get; set; } = MokaSpacingScale.Lg;

	/// <summary>Custom horizontal padding override. Overrides <see cref="GutterX" />.</summary>
	[Parameter]
	public string? GutterXValue { get; set; }

	/// <summary>Vertical padding (top/bottom) from the spacing scale. Null by default.</summary>
	[Parameter]
	public MokaSpacingScale? GutterY { get; set; }

	/// <summary>Custom vertical padding override. Overrides <see cref="GutterY" />.</summary>
	[Parameter]
	public string? GutterYValue { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-container";

	/// <inheritdoc />
	protected override string? CssStyle
	{
		get
		{
			bool hasUserMargin = MarginValue is not null || Margin.HasValue;

			return new StyleBuilder()
				.AddStyle("max-width", Fluid ? "none" : MaxWidth)
				.AddStyle("width", "100%")
				.AddStyle("margin-left", "auto", Centered && !hasUserMargin)
				.AddStyle("margin-right", "auto", Centered && !hasUserMargin)
				.AddStyle("margin", ResolvedMargin, hasUserMargin)
				.AddStyle("padding-left", ResolvedGutterX)
				.AddStyle("padding-right", ResolvedGutterX)
				.AddStyle("padding-top", ResolvedGutterY)
				.AddStyle("padding-bottom", ResolvedGutterY)
				.AddStyle(Style)
				.Build();
		}
	}

	private string? ResolvedGutterX =>
		GutterXValue ?? (GutterX.HasValue ? MokaEnumHelpers.ToCssValue(GutterX.Value) : null);

	private string? ResolvedGutterY =>
		GutterYValue ?? (GutterY.HasValue ? MokaEnumHelpers.ToCssValue(GutterY.Value) : null);
}
