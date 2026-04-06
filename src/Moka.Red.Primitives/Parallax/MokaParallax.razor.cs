using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Parallax;

/// <summary>
///     Parallax scroll effect wrapper — the background moves slower than the foreground content.
///     Uses CSS <c>background-attachment: fixed</c> for a zero-JS parallax effect.
/// </summary>
public partial class MokaParallax : MokaComponentBase
{
	/// <summary>Foreground content rendered on top of the parallax background.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Custom background content rendered as the parallax layer. Takes priority over <see cref="BackgroundImage" />.</summary>
	[Parameter]
	public RenderFragment? BackgroundContent { get; set; }

	/// <summary>URL of a background image for the parallax effect. Ignored when <see cref="BackgroundContent" /> is set.</summary>
	[Parameter]
	public string? BackgroundImage { get; set; }

	/// <summary>
	///     Parallax speed factor. 0 = fixed background, 1 = normal scroll speed.
	///     Default 0.5. Only affects the CSS-based parallax perspective.
	/// </summary>
	[Parameter]
	public double Speed { get; set; } = 0.5;

	/// <summary>Height of the parallax container. Any CSS height value. Default "400px".</summary>
	[Parameter]
	public string Height { get; set; } = "400px";

	/// <summary>Whether to show a dark overlay on the background. Default false.</summary>
	[Parameter]
	public bool Overlay { get; set; }

	/// <summary>Opacity of the dark overlay (0.0 to 1.0). Default 0.3.</summary>
	[Parameter]
	public double OverlayOpacity { get; set; } = 0.3;

	/// <inheritdoc />
	protected override string RootClass => "moka-parallax";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-parallax--has-overlay", Overlay)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("height", Height)
		.AddStyle(Style)
		.Build();

	private string? BackgroundStyle
	{
		get
		{
			if (BackgroundContent is not null || string.IsNullOrEmpty(BackgroundImage))
			{
				return null;
			}

			return new StyleBuilder()
				.AddStyle("background-image", $"url('{BackgroundImage}')")
				.Build();
		}
	}

	private string? OverlayStyle => Overlay
		? new StyleBuilder()
			.AddStyle("opacity", OverlayOpacity.ToString("F2", CultureInfo.InvariantCulture))
			.Build()
		: null;
}
