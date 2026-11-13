using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Gauge;

/// <summary>
///     A semicircular arc gauge for displaying a value within a range, similar to a speedometer.
///     Renders as an SVG arc using stroke-dasharray/dashoffset with animated transitions.
///     With a <see cref="Word" /> it is a full ring around the number, and with <see cref="AutoScale" /> a half
///     dial whose scale grows with the value.
/// </summary>
public partial class MokaGauge : MokaVisualComponentBase
{
	private const double SvgSize = 200;
	private const double StrokeWidth = 16;
	private const double DefaultStartAngle = -135;
	private const double DefaultEndAngle = 135;

	private const double DialWidth = 280;
	private const double DialHeight = 150;
	private const double DialStrokeWidth = 12;
	private const double DialRadius = 120;
	private const double DialCenter = 140;

	private static readonly double[] RoundSteps = [1, 2.5, 5, 10];

	private double _peak;
	private double _peakMax;

	/// <summary>The current gauge value.</summary>
	[Parameter]
	public double Value { get; set; }

	/// <summary>Minimum value. Defaults to 0.</summary>
	[Parameter]
	public double Min { get; set; }

	/// <summary>
	///     Maximum value. Defaults to 100. With <see cref="AutoScale" /> it is where the scale starts, and changing it
	///     starts the scale over.
	/// </summary>
	[Parameter]
	public double Max { get; set; } = 100;

	/// <summary>
	///     Optional label text displayed below the value, and the gauge's accessible name. With a <see cref="Word" /> or a
	///     <see cref="Display" /> it is only the name, unless the gauge is a dial, since the ring has room for two lines.
	/// </summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>
	///     A word under the number, such as "Good". Unless <see cref="StartAngle" /> or <see cref="EndAngle" /> are set,
	///     the gauge becomes a full ring, 132 px at the default <see cref="MokaVisualComponentBase.Size" />.
	/// </summary>
	[Parameter]
	public string? Word { get; set; }

	/// <summary>The color of <see cref="Word" />. Defaults to the gauge's own color.</summary>
	[Parameter]
	public MokaColor? WordColor { get; set; }

	/// <summary>
	///     Short text drawn large in the center in place of the number, such as a letter grade, with the number as a small
	///     line under it (<see cref="ShowValue" /> still decides whether it shows, and <see cref="Format" /> how it reads, so
	///     <c>"0' of 100'"</c> writes "92 of 100"). It takes the number's place, size and font: on a ring or a dial that is the
	///     theme's number font, so a theme with <c>WithNumberFont(MokaNumberFont.Mono)</c> draws it in mono. Like
	///     <see cref="Word" />, it makes the gauge a full ring unless <see cref="StartAngle" /> or <see cref="EndAngle" /> are
	///     set, and a screen reader hears it before the value. Null (the default) draws the number.
	/// </summary>
	[Parameter]
	public string? Display { get; set; }

	/// <summary>The color of <see cref="Display" />. Null (the default) draws it as the number would be drawn.</summary>
	[Parameter]
	public MokaColor? DisplayColor { get; set; }

	/// <summary>
	///     The size of <see cref="Display" />'s text, so a small ring can hold a large grade: <c>"1.75rem"</c> draws a
	///     28 px letter in a 64 px ring, where it would be 18 px. Any CSS length, or a <c>calc()</c>, <c>var()</c>,
	///     <c>min()</c>, <c>max()</c> or <c>clamp()</c> of lengths. Null (the default) keeps it in proportion to the gauge,
	///     at the size the number would be.
	/// </summary>
	[Parameter]
	public string? DisplaySize { get; set; }

	/// <summary>
	///     Raises the scale to the next round number above the value (1, 2.5 or 5 times a power of ten), and draws a half
	///     dial at most 280 px wide with both ends of the scale under it. The scale stays up when the value falls, so a
	///     live rate doesn't make it jump, and comes back down only when <see cref="Max" /> changes.
	///     <see cref="StartAngle" /> and <see cref="EndAngle" /> don't apply.
	/// </summary>
	[Parameter]
	public bool AutoScale { get; set; }

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
	public double StartAngle { get; set; } = DefaultStartAngle;

	/// <summary>
	///     Arc end angle in degrees from 12 o'clock position. Defaults to 135.
	///     Together with <see cref="StartAngle" /> defines the arc sweep.
	/// </summary>
	[Parameter]
	public double EndAngle { get; set; } = DefaultEndAngle;

	/// <inheritdoc />
	protected override string RootClass => "moka-gauge";

	private MokaColor ResolvedColor => Color ?? MokaColor.Primary;

	private bool HasWord => !string.IsNullOrEmpty(Word);

	private bool HasDisplay => !string.IsNullOrEmpty(Display);

	private bool HasCenterText => HasWord || HasDisplay;

	private bool IsRing => HasCenterText && !AutoScale;

	private bool ShowLabel => Label is not null && (AutoScale || !HasCenterText);

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-gauge--{SizeToKebab(Size)}")
		.AddClass($"moka-gauge--{ColorToKebab(ResolvedColor)}")
		.AddClass("moka-gauge--sized", SizeValue is not null)
		.AddClass("moka-gauge--ring", IsRing)
		.AddClass("moka-gauge--dial", AutoScale)
		.AddClass(Class)
		.Build();

	// A dial takes its height from its width, so only a ring or an arc gets one.
	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("width", SizeValue is null ? null : ResolvedSize)
		.AddStyle("height", SizeValue is null ? null : ResolvedSize, !AutoScale)
		.AddStyle(Style)
		.Build();

	private string WordClass => new CssBuilder("moka-gauge-word")
		.AddClass($"moka-gauge-word--{ColorToKebab(WordColor ?? ResolvedColor)}")
		.Build();

	private string DisplayClass => new CssBuilder("moka-gauge-value moka-gauge-display")
		.AddClass($"moka-gauge-display--{ColorToKebab(DisplayColor ?? MokaColor.Surface)}", DisplayColor is not null)
		.Build();

	private string? DisplayStyle => new StyleBuilder()
		.AddStyle("font-size", DisplaySize, CssValues.IsLengthExpression(DisplaySize))
		.Build();

	private string? ValueText => (HasDisplay, HasWord) switch
	{
		(true, true) => $"{Display}, {FormattedValue}, {Word}",
		(true, false) => $"{Display}, {FormattedValue}",
		(false, true) => $"{FormattedValue}, {Word}",
		_ => null
	};

	/// <summary>The top of the scale: <see cref="Max" />, or what <see cref="AutoScale" /> raised it to.</summary>
	private double ScaleMax => AutoScale && _peak > Max ? NextRound(_peak) : Max;

	/// <summary>Clamped proportion of value within [Min, ScaleMax], from 0 to 1.</summary>
	private double NormalizedValue
	{
		get
		{
			if (ScaleMax <= Min)
			{
				return 0;
			}

			return Math.Clamp((Value - Min) / (ScaleMax - Min), 0, 1);
		}
	}

	private double ArcStartAngle => AutoScale ? -90 : IsDefaultRing ? 0 : StartAngle;

	private double ArcEndAngle => AutoScale ? 90 : IsDefaultRing ? 360 : EndAngle;

	private bool IsDefaultRing => IsRing && StartAngle == DefaultStartAngle && EndAngle == DefaultEndAngle;

	private double ArcStroke => AutoScale ? DialStrokeWidth : StrokeWidth;

	private double Radius => AutoScale ? DialRadius : (SvgSize - StrokeWidth) / 2;

	private double CenterX => AutoScale ? DialCenter : SvgSize / 2;

	private double CenterY => AutoScale ? DialCenter : SvgSize / 2;

	private string ViewBox => AutoScale
		? string.Create(CultureInfo.InvariantCulture, $"0 0 {DialWidth} {DialHeight}")
		: string.Create(CultureInfo.InvariantCulture, $"0 0 {SvgSize} {SvgSize}");

	/// <summary>Total sweep angle of the arc in degrees.</summary>
	private double SweepAngle => ArcEndAngle - ArcStartAngle;

	/// <summary>Arc circumference for the given sweep.</summary>
	private double ArcLength => Math.Min(Math.Abs(SweepAngle), 360) / 360.0 * 2 * Math.PI * Radius;

	/// <summary>Dash offset to reveal the filled portion of the arc.</summary>
	private double DashOffset => ArcLength * (1 - NormalizedValue);

	/// <summary>Formatted value string for display.</summary>
	private string FormattedValue => Value.ToString(Format, CultureInfo.CurrentCulture);

	/// <summary>Computes the SVG arc path d attribute for the background track.</summary>
	private string ArcPath
	{
		get
		{
			double startRad = (ArcStartAngle - 90) * Math.PI / 180;
			double endRad = (ArcEndAngle - 90) * Math.PI / 180;

			double x1 = CenterX + Radius * Math.Cos(startRad);
			double y1 = CenterY + Radius * Math.Sin(startRad);
			double x2 = CenterX + Radius * Math.Cos(endRad);
			double y2 = CenterY + Radius * Math.Sin(endRad);

			int largeArc = Math.Abs(SweepAngle) > 180 ? 1 : 0;
			int sweepDir = SweepAngle > 0 ? 1 : 0;

			if (Math.Abs(SweepAngle) >= 360)
			{
				// One arc that ends where it starts has no length, so a full circle is two half circles.
				double xOpposite = 2 * CenterX - x1;
				double yOpposite = 2 * CenterY - y1;
				return string.Create(CultureInfo.InvariantCulture,
					$"M {x1:F2} {y1:F2} A {Radius:F2} {Radius:F2} 0 1 {sweepDir} {xOpposite:F2} {yOpposite:F2} A {Radius:F2} {Radius:F2} 0 1 {sweepDir} {x1:F2} {y1:F2}");
			}

			return string.Create(CultureInfo.InvariantCulture,
				$"M {x1:F2} {y1:F2} A {Radius:F2} {Radius:F2} 0 {largeArc} {sweepDir} {x2:F2} {y2:F2}");
		}
	}

	private string ArcStrokeStr => ArcStroke.ToString(CultureInfo.InvariantCulture);
	private string ArcLengthStr => ArcLength.ToString("F2", CultureInfo.InvariantCulture);
	private string DashOffsetStr => DashOffset.ToString("F2", CultureInfo.InvariantCulture);

	/// <summary>The first of 1, 2.5 and 5 times a power of ten that is at least <paramref name="value" />.</summary>
	internal static double NextRound(double value)
	{
		if (value <= 0)
		{
			return value;
		}

		double magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)));
		foreach (double step in RoundSteps)
		{
			if (step * magnitude >= value)
			{
				return step * magnitude;
			}
		}

		return 10 * magnitude;
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (!AutoScale)
		{
			return;
		}

		if (Max != _peakMax)
		{
			_peakMax = Max;
			_peak = Max;
		}

		if (double.IsFinite(Value) && Value > _peak)
		{
			_peak = Value;
		}
	}

	// F0 would print a scale of 2.5 as 3.
	private string ScaleText(double value) => value == Math.Floor(value)
		? value.ToString(Format, CultureInfo.CurrentCulture)
		: value.ToString("G", CultureInfo.CurrentCulture);
}
