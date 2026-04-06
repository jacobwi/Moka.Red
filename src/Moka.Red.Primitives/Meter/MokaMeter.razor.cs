using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Meter;

/// <summary>
///     A horizontal gauge/meter visualization showing a value within a range.
///     Supports colored segments for zone indicators (e.g., green/yellow/red).
/// </summary>
public partial class MokaMeter : MokaVisualComponentBase
{
	/// <summary>The current meter value.</summary>
	[Parameter]
	public double Value { get; set; }

	/// <summary>Minimum value of the meter range. Defaults to 0.</summary>
	[Parameter]
	public double Min { get; set; }

	/// <summary>Maximum value of the meter range. Defaults to 100.</summary>
	[Parameter]
	public double Max { get; set; } = 100;

	/// <summary>Optional label displayed above the meter.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Whether to display the current value. Defaults to true.</summary>
	[Parameter]
	public bool ShowValue { get; set; } = true;

	/// <summary>Numeric format string for the displayed value. Defaults to "F0".</summary>
	[Parameter]
	public string Format { get; set; } = "F0";

	/// <summary>Optional colored segments for zone indicators.</summary>
	[Parameter]
	public IReadOnlyList<MokaMeterSegment>? Segments { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-meter";

	private MokaColor ResolvedColor => Color ?? MokaColor.Primary;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-meter--{SizeToKebab(Size)}")
		.AddClass($"moka-meter--{ColorToKebab(ResolvedColor)}")
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle(Style)
		.Build();

	/// <summary>Gets the fill percentage clamped between 0 and 100.</summary>
	private double FillPercent
	{
		get
		{
			if (Max <= Min)
			{
				return 0;
			}

			double pct = (Value - Min) / (Max - Min) * 100;
			return Math.Clamp(pct, 0, 100);
		}
	}

	/// <summary>Gets the formatted value string.</summary>
	private string FormattedValue => Value.ToString(Format, CultureInfo.CurrentCulture);

	private bool HasSegments => Segments is { Count: > 0 };

	/// <summary>Builds the CSS gradient for segment backgrounds.</summary>
	private string? SegmentGradient
	{
		get
		{
			if (!HasSegments)
			{
				return null;
			}

			var stops = new List<string>();
			foreach (MokaMeterSegment seg in Segments!)
			{
				stops.Add($"{seg.Color} {seg.FromPercent.ToString(CultureInfo.InvariantCulture)}%");
				stops.Add($"{seg.Color} {seg.ToPercent.ToString(CultureInfo.InvariantCulture)}%");
			}

			return $"linear-gradient(to right, {string.Join(", ", stops)})";
		}
	}
}
