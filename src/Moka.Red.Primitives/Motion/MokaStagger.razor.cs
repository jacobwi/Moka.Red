using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Motion;

/// <summary>
///     Animates a list of items one-by-one with staggered delays.
///     Each item receives an increasing animation-delay.
///     Pure CSS animation — zero JavaScript.
/// </summary>
/// <typeparam name="TItem">The type of items to render.</typeparam>
public partial class MokaStagger<TItem> : MokaComponentBase
{
	/// <summary>The items to render with staggered animation.</summary>
	[Parameter]
	public IReadOnlyList<TItem>? Items { get; set; }

	/// <summary>Template used to render each item.</summary>
	[Parameter]
	public RenderFragment<TItem>? ItemTemplate { get; set; }

	/// <summary>Delay in milliseconds between each item's animation start. Defaults to 50.</summary>
	[Parameter]
	public int StaggerDelay { get; set; } = 50;

	/// <summary>The animation style applied to each item. Defaults to <see cref="MokaStaggerAnimation.FadeIn" />.</summary>
	[Parameter]
	public MokaStaggerAnimation Animation { get; set; } = MokaStaggerAnimation.FadeIn;

	/// <summary>Duration of each item's animation in milliseconds. Defaults to 300.</summary>
	[Parameter]
	public int Duration { get; set; } = 300;

	/// <inheritdoc />
	protected override string RootClass => "moka-stagger";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-stagger--fade-in", Animation == MokaStaggerAnimation.FadeIn)
		.AddClass("moka-stagger--slide-up", Animation == MokaStaggerAnimation.SlideUp)
		.AddClass("moka-stagger--slide-left", Animation == MokaStaggerAnimation.SlideLeft)
		.AddClass("moka-stagger--scale-in", Animation == MokaStaggerAnimation.ScaleIn)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-stagger-duration", $"{Duration}ms")
		.AddStyle(Style)
		.Build();
}
