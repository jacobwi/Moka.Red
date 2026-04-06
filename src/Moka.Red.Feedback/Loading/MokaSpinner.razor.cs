using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Loading;

/// <summary>
///     An animated loading spinner with multiple visual styles.
///     All animations are pure CSS — zero JavaScript.
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
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("color",
			Color.HasValue ? $"var(--moka-color-{ColorToKebab(Color.Value)})" : "var(--moka-color-primary)")
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string AnimationSizeStyle => $"width: {ResolvedSize}; height: {ResolvedSize}";

	private string PulseElementSizeStyle => $"width: {ResolvedSize}; height: {ResolvedSize}";

	private string BarElementSizeStyle =>
		// Bars are roughly 20% wide, full height of the wrapper
		$"height: {ResolvedSize}";

	private string RingElementSizeStyle => $"width: {ResolvedSize}; height: {ResolvedSize}";
}
