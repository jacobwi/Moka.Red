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

	/// <summary>Whether the loading overlay is visible. Two-way bindable. Defaults to false.</summary>
	[Parameter]
	public bool Loading { get; set; }

	/// <summary>Callback invoked when <see cref="Loading" /> changes.</summary>
	[Parameter]
	public EventCallback<bool> LoadingChanged { get; set; }

	/// <summary>Optional message text displayed alongside the spinner. Two-way bindable.</summary>
	[Parameter]
	public string? Message { get; set; }

	/// <summary>Callback invoked when <see cref="Message" /> changes.</summary>
	[Parameter]
	public EventCallback<string?> MessageChanged { get; set; }

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

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string? OverlayStyle => new StyleBuilder()
		.AddStyle("background",
			OverlayColor ?? $"color-mix(in srgb, var(--moka-color-surface) {(int)(Opacity * 100)}%, transparent)")
		.Build();
}
