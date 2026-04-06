using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.Knob;

/// <summary>
///     A rotary dial/knob input for selecting numeric values by dragging vertically or
///     scrolling with the mouse wheel. Renders as an SVG arc with a filled indicator.
/// </summary>
public partial class MokaKnob : MokaVisualComponentBase
{
	private const double SvgSize = 100;
	private const double TrackWidth = 6;
	private const double Radius = (SvgSize - TrackWidth) / 2;
	private const double CenterX = SvgSize / 2;
	private const double CenterY = SvgSize / 2;
	private const double IndicatorRadius = 5;

	private bool _dragging;
	private double _dragStartValue;
	private double _dragStartY;

	/// <summary>The current knob value. Two-way bindable.</summary>
	[Parameter]
	public double Value { get; set; }

	/// <summary>Callback invoked when <see cref="Value" /> changes.</summary>
	[Parameter]
	public EventCallback<double> ValueChanged { get; set; }

	/// <summary>Minimum value. Defaults to 0.</summary>
	[Parameter]
	public double Min { get; set; }

	/// <summary>Maximum value. Defaults to 100.</summary>
	[Parameter]
	public double Max { get; set; } = 100;

	/// <summary>Step increment. Defaults to 1.</summary>
	[Parameter]
	public double Step { get; set; } = 1;

	/// <summary>Optional label displayed below the knob.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Whether to display the current value in the center. Defaults to true.</summary>
	[Parameter]
	public bool ShowValue { get; set; } = true;

	/// <summary>.NET number format string for the displayed value. Defaults to "F0".</summary>
	[Parameter]
	public string Format { get; set; } = "F0";

	/// <summary>
	///     Arc start angle in degrees from 12 o'clock position. Defaults to -135.
	///     Together with <see cref="EndAngle" /> defines the rotation sweep.
	/// </summary>
	[Parameter]
	public double StartAngle { get; set; } = -135;

	/// <summary>
	///     Arc end angle in degrees from 12 o'clock position. Defaults to 135.
	///     Together with <see cref="StartAngle" /> defines the rotation sweep.
	/// </summary>
	[Parameter]
	public double EndAngle { get; set; } = 135;

	/// <inheritdoc />
	protected override string RootClass => "moka-knob";

	private MokaColor ResolvedColor => Color ?? MokaColor.Primary;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-knob--{SizeToKebab(Size)}")
		.AddClass($"moka-knob--{ColorToKebab(ResolvedColor)}")
		.AddClass("moka-knob--disabled", Disabled)
		.AddClass("moka-knob--dragging", _dragging)
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

	/// <summary>Computes the SVG arc path d attribute for the track.</summary>
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

	/// <summary>X coordinate of the indicator dot at the current value position.</summary>
	private string IndicatorX
	{
		get
		{
			double angle = StartAngle + SweepAngle * NormalizedValue;
			double rad = (angle - 90) * Math.PI / 180;
			return (CenterX + Radius * Math.Cos(rad)).ToString("F2", CultureInfo.InvariantCulture);
		}
	}

	/// <summary>Y coordinate of the indicator dot at the current value position.</summary>
	private string IndicatorY
	{
		get
		{
			double angle = StartAngle + SweepAngle * NormalizedValue;
			double rad = (angle - 90) * Math.PI / 180;
			return (CenterY + Radius * Math.Sin(rad)).ToString("F2", CultureInfo.InvariantCulture);
		}
	}

	private string ArcLengthStr => ArcLength.ToString("F2", CultureInfo.InvariantCulture);
	private string DashOffsetStr => DashOffset.ToString("F2", CultureInfo.InvariantCulture);
	private static string IndicatorRadiusStr => IndicatorRadius.ToString("F2", CultureInfo.InvariantCulture);

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private void OnPointerDown(PointerEventArgs e)
	{
		if (Disabled)
		{
			return;
		}

		_dragging = true;
		_dragStartY = e.ClientY;
		_dragStartValue = Value;
	}

	private async Task OnPointerMove(PointerEventArgs e)
	{
		if (!_dragging || Disabled)
		{
			return;
		}

		// Vertical drag: moving up increases, moving down decreases
		double deltaY = _dragStartY - e.ClientY;
		double range = Max - Min;

		// 200px of vertical movement covers the full range
		double valueDelta = deltaY / 200.0 * range;
		double newValue = SnapToStep(_dragStartValue + valueDelta);

		if (Math.Abs(newValue - Value) > double.Epsilon)
		{
			await SetValueAsync(newValue);
		}
	}

	private void OnPointerUp(PointerEventArgs e) => _dragging = false;

	private async Task OnWheel(WheelEventArgs e)
	{
		if (Disabled)
		{
			return;
		}

		// Scroll up = increase, scroll down = decrease
		double direction = e.DeltaY < 0 ? 1 : -1;
		double newValue = SnapToStep(Value + direction * Step);

		if (Math.Abs(newValue - Value) > double.Epsilon)
		{
			await SetValueAsync(newValue);
		}
	}

	private double SnapToStep(double value)
	{
		double clamped = Math.Clamp(value, Min, Max);

		if (Step > 0)
		{
			clamped = Math.Round((clamped - Min) / Step) * Step + Min;
			clamped = Math.Clamp(clamped, Min, Max);
		}

		return clamped;
	}

	private async Task SetValueAsync(double newValue)
	{
		Value = newValue;
		await ValueChanged.InvokeAsync(Value);
	}
}
