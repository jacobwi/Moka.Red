using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Parallax;

/// <summary>
///     Parallax scroll effect wrapper - the background moves slower than the foreground content.
///     Zero JS: a scroll-driven CSS animation drives the offset where the browser supports
///     <c>animation-timeline: view()</c>, falling back to <c>background-attachment: fixed</c>.
/// </summary>
public partial class MokaParallax : MokaComponentBase
{
	/// <summary>
	///     Travel of the background layer, in pixels, across the element's full scroll range at
	///     <see cref="Speed" /> 0. Also the vertical overscan, so the drifting layer never
	///     exposes an edge.
	/// </summary>
	private const double MaxShiftPx = 100;

	/// <summary>Foreground content rendered on top of the parallax background.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Custom background content rendered as the parallax layer. Takes priority over <see cref="BackgroundImage" />.</summary>
	[Parameter]
	public RenderFragment? BackgroundContent { get; set; }

	/// <summary>
	///     URL of a background image for the parallax effect. It is written as a quoted CSS string with quotes
	///     and backslashes escaped, so it cannot add CSS declarations. Ignored when
	///     <see cref="BackgroundContent" /> is set.
	/// </summary>
	[Parameter]
	public string? BackgroundImage { get; set; }

	/// <summary>
	///     Parallax speed factor, clamped to 0-1. 0 holds the background still while the page
	///     scrolls past; 1 scrolls it with the content (no parallax). Default 0.5.
	///     Emitted as the <c>--moka-parallax-shift</c> variable that drives the background offset.
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
		.AddStyle("--moka-parallax-shift", ShiftValue)
		.AddStyle(Style)
		.Build();

	// Speed 0 gives the full counter-scroll, speed 1 pins the background to the content.
	private string ShiftValue =>
		$"{((1 - Math.Clamp(Speed, 0d, 1d)) * MaxShiftPx).ToString("F1", CultureInfo.InvariantCulture)}px";

	private string? BackgroundStyle
	{
		get
		{
			if (BackgroundContent is not null || string.IsNullOrEmpty(BackgroundImage))
			{
				return null;
			}

			// Quoted and escaped, so a quote or parenthesis in the URL cannot end the url() and add
			// declarations after it.
			return new StyleBuilder()
				.AddStyle("background-image", CssValues.Url(BackgroundImage))
				.Build();
		}
	}

	private string? OverlayStyle => Overlay
		? new StyleBuilder()
			.AddStyle("opacity", OverlayOpacity.ToString("F2", CultureInfo.InvariantCulture))
			.Build()
		: null;
}
