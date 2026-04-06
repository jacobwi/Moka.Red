using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Motion;

/// <summary>
///     Slides child content in from an edge of its container.
///     Uses overflow:hidden to clip the sliding content.
///     Pure CSS animation — zero JavaScript.
/// </summary>
public partial class MokaSlideIn : MokaComponentBase
{
	/// <summary>The content to slide in.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Animation duration in milliseconds. Defaults to 400.</summary>
	[Parameter]
	public int Duration { get; set; } = 400;

	/// <summary>Delay before the animation starts, in milliseconds. Defaults to 0.</summary>
	[Parameter]
	public int Delay { get; set; }

	/// <summary>Edge from which the content slides in. Defaults to <see cref="MokaSlideFrom.Left" />.</summary>
	[Parameter]
	public MokaSlideFrom From { get; set; } = MokaSlideFrom.Left;

	/// <summary>Distance the content travels. Defaults to "100%".</summary>
	[Parameter]
	public string Distance { get; set; } = "100%";

	/// <inheritdoc />
	protected override string RootClass => "moka-slide-in";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-slide-in--left", From == MokaSlideFrom.Left)
		.AddClass("moka-slide-in--right", From == MokaSlideFrom.Right)
		.AddClass("moka-slide-in--top", From == MokaSlideFrom.Top)
		.AddClass("moka-slide-in--bottom", From == MokaSlideFrom.Bottom)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-slide-duration", $"{Duration}ms")
		.AddStyle("--moka-slide-delay", $"{Delay}ms")
		.AddStyle("--moka-slide-distance", Distance)
		.AddStyle(Style)
		.Build();
}
