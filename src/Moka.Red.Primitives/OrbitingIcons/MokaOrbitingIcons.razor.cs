using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.OrbitingIcons;

/// <summary>
///     Renders icons orbiting in a circle around a center element.
///     Pure CSS animation — zero JS. Each icon revolves at a constant radius.
/// </summary>
public partial class MokaOrbitingIcons : MokaComponentBase
{
	/// <summary>Center content (e.g., a logo, avatar, or icon).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Icons to orbit around the center. Each will be evenly distributed.</summary>
	[Parameter]
	public IReadOnlyList<MokaIconDefinition> Icons { get; set; } = [];

	/// <summary>Orbit radius in pixels. Default 120.</summary>
	[Parameter]
	public int Radius { get; set; } = 120;

	/// <summary>Full revolution duration in seconds. Default 20.</summary>
	[Parameter]
	public double Duration { get; set; } = 20;

	/// <summary>When true, icons orbit counter-clockwise. Default false.</summary>
	[Parameter]
	public bool Reverse { get; set; }

	/// <summary>Icon size in pixels. Default 20.</summary>
	[Parameter]
	public int IconSize { get; set; } = 20;

	/// <summary>Icon color. Default uses on-surface-variant.</summary>
	[Parameter]
	public string? IconColor { get; set; }

	/// <summary>Whether to show a subtle orbit ring path. Default true.</summary>
	[Parameter]
	public bool ShowPath { get; set; } = true;

	/// <summary>Orbit path ring color. Default uses outline-variant.</summary>
	[Parameter]
	public string? PathColor { get; set; }

	/// <summary>Whether to pause the animation. Default false.</summary>
	[Parameter]
	public bool Paused { get; set; }

	/// <summary>Container size in pixels (width and height). Default auto-calculated from radius.</summary>
	[Parameter]
	public int? Size { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-orbiting-icons";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-orbiting-icons--paused", Paused)
		.AddClass("moka-orbiting-icons--reverse", Reverse)
		.AddClass(Class)
		.Build();

	private int ContainerSize => Size ?? (Radius * 2 + IconSize + 24);

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("width", $"{ContainerSize}px")
		.AddStyle("height", $"{ContainerSize}px")
		.AddStyle("--orbit-duration", $"{Duration.ToString("F1", CultureInfo.InvariantCulture)}s")
		.AddStyle(Style)
		.Build();

	private string PathStyle => new StyleBuilder()
		.AddStyle("width", $"{Radius * 2}px")
		.AddStyle("height", $"{Radius * 2}px")
		.AddStyle("border-color", PathColor ?? "var(--moka-color-outline-variant)")
		.Build()!;

	private string OrbitItemStyle(int index)
	{
		var count = Icons.Count;
		if (count == 0) return "";

		var angleDeg = 360.0 / count * index;
		var delay = -(Duration / count * index);

		return new StyleBuilder()
			.AddStyle("--orbit-start-angle", $"{angleDeg.ToString("F1", CultureInfo.InvariantCulture)}deg")
			.AddStyle("--orbit-radius", $"{Radius}px")
			.AddStyle("animation-delay", $"{delay.ToString("F2", CultureInfo.InvariantCulture)}s")
			.Build()!;
	}

	private string IconStyle => new StyleBuilder()
		.AddStyle("color", IconColor ?? "var(--moka-color-on-surface-variant)")
		.Build()!;
}
