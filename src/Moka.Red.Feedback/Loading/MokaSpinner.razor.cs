using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Loading;

/// <summary>
///     An animated loading spinner with multiple visual styles.
///     All animations are pure CSS - zero JavaScript.
/// </summary>
public partial class MokaSpinner : MokaVisualComponentBase
{
	/// <summary>Visual style of the spinner animation. Defaults to <see cref="MokaSpinnerStyle.Circular" />.</summary>
	[Parameter]
	public MokaSpinnerStyle SpinnerStyle { get; set; } = MokaSpinnerStyle.Circular;

	/// <summary>Optional text displayed alongside the spinner.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Placement of the label relative to the spinner. Defaults to <see cref="MokaLabelPlacement.Bottom" />.</summary>
	[Parameter]
	public MokaLabelPlacement LabelPlacement { get; set; } = MokaLabelPlacement.Bottom;

	/// <inheritdoc />
	protected override string RootClass => "moka-spinner";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-spinner--{MokaEnumHelpers.ToCssClass(SpinnerStyle)}")
		.AddClass($"moka-spinner--label-{(LabelPlacement == MokaLabelPlacement.Right ? "right" : "bottom")}",
			Label is not null)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("color",
			Color.HasValue ? $"var(--moka-color-{ColorToKebab(Color.Value)})" : "var(--moka-color-primary)")
		.AddStyle(Style)
		.Build();

	private string? AnimationSizeStyle => SquareStyle();

	private string? PulseElementSizeStyle => SquareStyle();

	// Bars are roughly 20% wide, full height of the wrapper.
	private string? BarElementSizeStyle => new StyleBuilder()
		.AddStyle("height", ResolvedSize)
		.Build();

	private string? RingElementSizeStyle => SquareStyle();

	private string? SquareStyle() => new StyleBuilder()
		.AddStyle("width", ResolvedSize)
		.AddStyle("height", ResolvedSize)
		.Build();
}
