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

	/// <summary>Prefix string (e.g., "$", "EUR").</summary>
	[Parameter]
	public string? Prefix { get; set; }

	/// <summary>Suffix string (e.g., "%", "users").</summary>
	[Parameter]
	public string? Suffix { get; set; }

	/// <summary>Text alignment. Default Left.</summary>
	[Parameter]
	public MokaTextAlign? Align { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-stat";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-stat--{SizeToKebab(Size)}")
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("text-align", Align.HasValue ? MokaEnumHelpers.ToCssValue(Align.Value) : null)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
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
