using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Stat;

/// <summary>
///     Metric/statistic display component showing a value with optional label, trend indicator, and icon.
/// </summary>
public partial class MokaStat
{
	/// <summary>The main number/value to display. Required.</summary>
	[Parameter]
	[EditorRequired]
	public string Value { get; set; } = default!;

	/// <summary>Description text below the value.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Additional context text.</summary>
	[Parameter]
	public string? HelpText { get; set; }

	/// <summary>Percentage change. Positive = up (green), negative = down (red).</summary>
	[Parameter]
	public double? Trend { get; set; }

	/// <summary>Label for the trend (e.g., "vs last month").</summary>
	[Parameter]
	public string? TrendLabel { get; set; }

	/// <summary>Icon displayed above the value.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     Anything drawn in the icon's place, such as an emoji, a small picture or a <c>MokaIdenticon</c>, in the same
	///     box as <see cref="Icon" />, which it wins over. Pictures and SVG fill the box; text is set at the box's size.
	///     Hidden from screen readers like the icon.
	/// </summary>
	[Parameter]
	public RenderFragment? IconContent { get; set; }

	private static readonly string IconSlotStyle =
		new StyleBuilder().AddStyle("--moka-icon-slot", MokaEnumHelpers.ToPixels(MokaSize.Lg)).Build()!;

	/// <summary>Prefix string (e.g., "$", "EUR").</summary>
	[Parameter]
	public string? Prefix { get; set; }

	/// <summary>Suffix string (e.g., "%", "users").</summary>
	[Parameter]
	public string? Suffix { get; set; }

	/// <summary>Text alignment. Default Left.</summary>
	[Parameter]
	public MokaTextAlign? Align { get; set; }

	/// <summary>
	///     Draws the stat as a fact tile: a small panel with a smaller value, and <c>Color</c> tints the value.
	///     <c>Size</c> is ignored. Lay a row of them out with a <c>MokaGrid</c>.
	/// </summary>
	[Parameter]
	public bool Tile { get; set; }

	/// <summary>
	///     A small picture beside the value, such as a <c>MokaSparkline</c> or a <c>MokaMeter</c>. The value and the
	///     visual share one row: the visual takes the width the value leaves, and drops under it when too little is
	///     left. Works the same on a tile.
	/// </summary>
	[Parameter]
	public RenderFragment? Visual { get; set; }

	/// <summary>Draws the value in the mono font with tabular figures, for numbers that change in place.</summary>
	[Parameter]
	public bool Mono { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-stat";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-stat--{SizeToKebab(Size)}", !Tile)
		.AddClass("moka-stat--tile", Tile)
		.AddClass(Color.HasValue ? $"moka-stat--{ColorToKebab(Color.Value)}" : null, Tile)
		.AddClass("moka-stat--mono", Mono)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("text-align", Align.HasValue ? MokaEnumHelpers.ToCssValue(Align.Value) : null)
		.AddStyle(Style)
		.Build();

	private string FormatTrend()
	{
		if (!Trend.HasValue)
		{
			return "";
		}

		double abs = Math.Abs(Trend.Value);
		return abs.ToString("F1", CultureInfo.InvariantCulture) + "%";
	}
}
