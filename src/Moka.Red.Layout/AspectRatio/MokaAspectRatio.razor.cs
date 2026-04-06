using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.AspectRatio;

/// <summary>
///     Forces child content to maintain a specific aspect ratio.
///     Uses the CSS <c>aspect-ratio</c> property.
/// </summary>
public partial class MokaAspectRatio : MokaComponentBase
{
	/// <summary>The content to constrain to the given aspect ratio.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>CSS aspect-ratio value. Defaults to "16/9".</summary>
	[Parameter]
	public string Ratio { get; set; } = "16/9";

	/// <inheritdoc />
	protected override string RootClass => "moka-aspect-ratio";

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("aspect-ratio", Ratio)
		.AddStyle("overflow", "hidden")
		.AddStyle(Style)
		.Build();
}
