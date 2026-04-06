using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Gauge;

/// <summary>
///     A semicircular arc gauge for displaying a value within a range, similar to a speedometer.
///     Renders as an SVG arc using stroke-dasharray/dashoffset with animated transitions.
/// </summary>
public partial class MokaGauge : MokaVisualComponentBase
{
	private const double SvgSize = 200;
	private const double StrokeWidth = 16;
	private const double Radius = (SvgSize - StrokeWidth) / 2;
	private const double CenterX = SvgSize / 2;
	private const double CenterY = SvgSize / 2;

	/// <summary>The current gauge value.</summary>
	[Parameter]
	public double Value { get; set; }

	/// <summary>Minimum value. Defaults to 0.</summary>
	[Parameter]
	public double Min { get; set; }

	/// <summary>Maximum value. Defaults to 100.</summary>
	[Parameter]
	public double Max { get; set; } = 100;

	/// <summary>Optional label text displayed below the value.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Whether to show the numeric value in the center. Defaults to true.</summary>
	[Parameter]
	public bool ShowValue { get; set; } = true;

	/// <summary>.NET number format string for the value display. Defaults to "F0".</summary>
	[Parameter]
	public string Format { get; set; } = "F0";

	/// <summary>
	///     Arc start angle in degrees from 12 o'clock position. Defaults to -135.
	///     Together with <see cref="EndAngle" /> defines the arc sweep.
	/// </summary>
	[Parameter]
	public double StartAngle { get; set; } = -135;

	/// <summary>
	///     Arc end angle in degrees from 12 o'clock position. Defaults to 135.
	///     Together with <see cref="StartAngle" /> defines the arc sweep.
	/// </summary>
	[Parameter]
	public double EndAngle { get; set; } = 135;

	/// <inheritdoc />
	protected override string RootClass => "moka-gauge";

	private MokaColor ResolvedColor => Color ?? MokaColor.Primary;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-gauge--{SizeToKebab(Size)}")
		.AddClass($"moka-gauge--{ColorToKebab(ResolvedColor)}")
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <summary>Clamped proportion of value within [Min, Max], from 0 to 1.</summary>
	private double NormalizedValue
	{
		get
		{
			if (Max <= Min)
			{
				return 0;
			}

			return Math.Clamp((Value - Min) / (Max - Min), 0, 1);
		}
	}

	/// <summary>Total sweep angle of the arc in degrees.</summary>
	private double SweepAngle => EndAngle - StartAngle;

	/// <summary>Arc circumference for the given sweep.</summary>
	private double ArcLength => Math.Abs(SweepAngle) / 360.0 * 2 * Math.PI * Radius;

	/// <summary>Dash offset to reveal the filled portion of the arc.</summary>
	private double DashOffset => ArcLength * (1 - NormalizedValue);

	/// <summary>Formatted value string for display.</summary>
	private string FormattedValue => Value.ToString(Format, CultureInfo.CurrentCulture);

	/// <summary>Computes the SVG arc path d attribute for the background track.</summary>
	private string ArcPath
	{
		get
		{
			double startRad = (StartAngle - 90) * Math.PI / 180;
			double endRad = (EndAngle - 90) * Math.PI / 180;

			double x1 = CenterX + Radius * Math.Cos(startRad);
			double y1 = CenterY + Radius * Math.Sin(startRad);
			double x2 = CenterX + Radius * Math.Cos(endRad);
			double y2 = CenterY + Radius * Math.Sin(endRad);

			int largeArc = Math.Abs(SweepAngle) > 180 ? 1 : 0;
			int sweepDir = SweepAngle > 0 ? 1 : 0;

			return string.Create(CultureInfo.InvariantCulture,
				$"M {x1:F2} {y1:F2} A {Radius:F2} {Radius:F2} 0 {largeArc} {sweepDir} {x2:F2} {y2:F2}");
		}
	}

	private string ArcLengthStr => ArcLength.ToString("F2", CultureInfo.InvariantCulture);
	private string DashOffsetStr => DashOffset.ToString("F2", CultureInfo.InvariantCulture);
}
