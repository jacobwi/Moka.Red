using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;

namespace Moka.Red.Layout.Base;

/// <summary>
///     Abstract base class for layout container components (Card, Paper, Stack, etc.).
///     Provides child content rendering, elevation/shadow levels, and
///     padding/margin from theme spacing tokens.
/// </summary>
public abstract class MokaContainerBase : MokaVisualComponentBase
{
	/// <summary>The child content to render inside the container.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Elevation level controlling the shadow depth.
	///     0 means no shadow. Higher values produce deeper shadows.
	///     Defaults to 0.
	/// </summary>
	[Parameter]
	public int Elevation { get; set; }

	/// <summary>
	///     Gets the CSS class for the current elevation level.
	///     Returns an empty string when elevation is 0.
	/// </summary>
	protected string ElevationClass =>
		Elevation > 0 ? $"moka-elevation-{Math.Clamp(Elevation, 1, 24)}" : string.Empty;
}
