using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Loading;

/// <summary>
///     Wraps content and shows a semi-transparent overlay with a spinner or skeleton
///     when <see cref="Loading" /> is true. Supports blur, custom opacity, and full-screen mode.
/// </summary>
public partial class MokaLoadingOverlay : MokaVisualComponentBase
{
	/// <summary>The content to overlay.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Whether the loading overlay is visible. Defaults to false. While it is, the content underneath
	///     is inert and marked busy. The overlay never changes it itself, so pass it one way.
	/// </summary>
	[Parameter]
	public bool Loading { get; set; }

	/// <summary>Optional message text displayed alongside the spinner. Also names the overlay for screen readers.</summary>
	[Parameter]
	public string? Message { get; set; }

	/// <summary>Visual style of the spinner. Defaults to <see cref="MokaSpinnerStyle.Circular" />.</summary>
	[Parameter]
	public MokaSpinnerStyle SpinnerStyle { get; set; } = MokaSpinnerStyle.Circular;

	/// <summary>Whether to blur the background content when loading. Defaults to false.</summary>
	[Parameter]
	public bool Blur { get; set; }

	/// <summary>CSS blur amount when <see cref="Blur" /> is true. Defaults to "4px".</summary>
	[Parameter]
	public string BlurAmount { get; set; } = "4px";

	/// <summary>Overlay opacity from 0 to 1. Defaults to 0.7.</summary>
	[Parameter]
	public double Opacity { get; set; } = 0.7;

	/// <summary>Custom overlay background color. Defaults to theme surface color with opacity.</summary>
	[Parameter]
	public string? OverlayColor { get; set; }

	/// <summary>When true, covers the entire viewport using <c>position: fixed</c>. Defaults to false.</summary>
	[Parameter]
	public bool FullScreen { get; set; }

	/// <summary>When true, shows a skeleton placeholder instead of a spinner. Defaults to false.</summary>
	[Parameter]
	public bool ShowSkeleton { get; set; }

	/// <summary>
	///     Shape of the skeleton when <see cref="ShowSkeleton" /> is true. Defaults to
	///     <see cref="MokaSkeletonShape.Text" />.
	/// </summary>
	[Parameter]
	public MokaSkeletonShape SkeletonShape { get; set; } = MokaSkeletonShape.Text;

	/// <summary>Number of skeleton text lines when <see cref="ShowSkeleton" /> is true. Defaults to 3.</summary>
	[Parameter]
	public int SkeletonLines { get; set; } = 3;

	/// <inheritdoc />
	protected override string RootClass => "moka-loading";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-loading--active", Loading)
		.AddClass(Class)
		.Build();

	private string ContentCss => new CssBuilder("moka-loading-content")
		.AddClass("moka-loading-content--blur", Loading && Blur)
		.Build();

	private string? ContentStyle => new StyleBuilder()
		.AddStyle("filter", $"blur({BlurAmount})", Loading && Blur)
		.Build();

	private string OverlayCss => new CssBuilder("moka-loading-overlay")
		.AddClass("moka-loading-overlay--fullscreen", FullScreen)
		.Build();

	// Every skeleton shape but the circle is sized in percentages of its container, so the
	// indicator has to span the overlay for them. Shrunk to fit its content, it gave them no width.
	private string IndicatorCss => new CssBuilder("moka-loading-indicator")
		.AddClass("moka-loading-indicator--fill", ShowSkeleton && SkeletonShape != MokaSkeletonShape.Circle)
		.Build();

	private string? OverlayStyle => new StyleBuilder()
		.AddStyle("background",
			OverlayColor ?? $"color-mix(in srgb, var(--moka-color-surface) {(int)(Opacity * 100)}%, transparent)")
		.Build();
}
