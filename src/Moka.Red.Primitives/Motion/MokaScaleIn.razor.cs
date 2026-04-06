using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Motion;

/// <summary>
///     Scales child content in from a smaller size to full size.
///     Pure CSS animation — zero JavaScript.
/// </summary>
public partial class MokaScaleIn : MokaComponentBase
{
	/// <summary>The content to scale in.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Animation duration in milliseconds. Defaults to 300.</summary>
	[Parameter]
	public int Duration { get; set; } = 300;

	/// <summary>Delay before the animation starts, in milliseconds. Defaults to 0.</summary>
	[Parameter]
	public int Delay { get; set; }

	/// <summary>Initial scale factor (0.0 to 1.0). Defaults to 0.8.</summary>
	[Parameter]
	public double InitialScale { get; set; } = 0.8;

	/// <inheritdoc />
	protected override string RootClass => "moka-scale-in";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-scale-duration", $"{Duration}ms")
		.AddStyle("--moka-scale-delay", $"{Delay}ms")
		.AddStyle("--moka-scale-initial", InitialScale.ToString("F2", CultureInfo.InvariantCulture))
		.AddStyle(Style)
		.Build();
}
