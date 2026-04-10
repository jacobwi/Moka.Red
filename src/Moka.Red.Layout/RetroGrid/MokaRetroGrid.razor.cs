using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.RetroGrid;

/// <summary>
///     A perspective vanishing-point grid background — the classic "running into the horizon"
///     retro/cyberpunk effect. Features a glowing horizon line, animated grid scroll,
///     and radial glow. Pure CSS, zero JS.
/// </summary>
public partial class MokaRetroGrid : MokaComponentBase
{
	/// <summary>Content rendered above the grid.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Grid line color. Default uses the primary-border token.</summary>
	[Parameter]
	public string? LineColor { get; set; }

	/// <summary>Grid cell size in pixels (before perspective). Default 60.</summary>
	[Parameter]
	public int CellSize { get; set; } = 60;

	/// <summary>Grid line thickness in pixels. Default 1.</summary>
	[Parameter]
	public int LineWidth { get; set; } = 1;

	/// <summary>Perspective depth in pixels. Lower = more dramatic. Default 300.</summary>
	[Parameter]
	public int Perspective { get; set; } = 300;

	/// <summary>X-axis rotation angle in degrees. Default 60.</summary>
	[Parameter]
	public int Angle { get; set; } = 60;

	/// <summary>Vertical position of the horizon line (0% = top, 100% = bottom). Default 65.</summary>
	[Parameter]
	public int HorizonPosition { get; set; } = 65;

	/// <summary>Whether to show a glowing line at the horizon. Default true.</summary>
	[Parameter]
	public bool ShowHorizonGlow { get; set; } = true;

	/// <summary>Horizon glow color. Defaults to the grid line color.</summary>
	[Parameter]
	public string? HorizonGlowColor { get; set; }

	/// <summary>Whether the grid animates (scrolls toward the viewer). Default true.</summary>
	[Parameter]
	public bool Animated { get; set; } = true;

	/// <summary>Animation duration in seconds. Default 8.</summary>
	[Parameter]
	public double Duration { get; set; } = 8;

	/// <summary>Background color behind the grid. Defaults to theme background.</summary>
	[Parameter]
	public string? BackgroundColor { get; set; }

	/// <summary>Minimum height of the container. Default "400px".</summary>
	[Parameter]
	public string MinHeight { get; set; } = "400px";

	/// <summary>When true, sets min-height to 100vh.</summary>
	[Parameter]
	public bool FullScreen { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-retro-grid";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-retro-grid--fullscreen", FullScreen)
		.AddClass("moka-retro-grid--animated", Animated)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("min-height", MinHeight, !FullScreen)
		.AddStyle("background-color", BackgroundColor, !string.IsNullOrEmpty(BackgroundColor))
		.AddStyle(Style)
		.Build();

	/// <summary>Inline style for the perspective grid plane.</summary>
	private string GridStyle
	{
		get
		{
			var color = LineColor ?? "var(--moka-color-primary-border)";
			var dur = Duration.ToString("F1", CultureInfo.InvariantCulture);
			return new StyleBuilder()
				.AddStyle("--retro-color", color)
				.AddStyle("--retro-size", $"{CellSize}px")
				.AddStyle("--retro-line-width", $"{LineWidth}px")
				.AddStyle("--retro-perspective", $"{Perspective}px")
				.AddStyle("--retro-angle", $"{Angle}deg")
				.AddStyle("--retro-horizon", $"{HorizonPosition}%")
				.AddStyle("--retro-duration", $"{dur}s")
				.Build()!;
		}
	}

	/// <summary>Inline style for the horizon glow line.</summary>
	private string? HorizonStyle
	{
		get
		{
			if (!ShowHorizonGlow) return null;
			var glowColor = HorizonGlowColor ?? LineColor ?? "var(--moka-color-primary)";
			return new StyleBuilder()
				.AddStyle("top", $"{HorizonPosition}%")
				.AddStyle("--retro-glow-color", glowColor)
				.Build()!;
		}
	}
}
