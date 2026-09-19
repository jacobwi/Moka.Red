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
///     It is a WAI-ARIA slider: one tab stop, with the arrow keys, Page Up, Page Down, Home and
///     End changing the value.
/// </summary>
public partial class MokaKnob : MokaVisualComponentBase
{
	private const double SvgSize = 100;
	private const double TrackWidth = 6;
	private const double Radius = (SvgSize - TrackWidth) / 2;
	private const double CenterX = SvgSize / 2;
	private const double CenterY = SvgSize / 2;
	private const double IndicatorRadius = 5;

	// Page Up and Page Down move this many steps.
	private const int PageSteps = 10;

	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	// The keys the slider consumes. Without this the arrows, Page Up, Page Down, Home and End also
	// scroll the page. HandleKeyDown ignores them with Ctrl, Alt or Meta, so those stay with the
	// browser (Alt+Left is back). Dictionaries rather than anonymous types, which trimming can strip
	// in WebAssembly apps.
	private static readonly Dictionary<string, object?>[] SliderKeyRules =
	[
		new()
		{
			["selector"] = null,
			["keys"] = new[] { "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "PageUp", "PageDown", "Home", "End" },
			["unlessModified"] = true
		}
	];

	private static readonly Dictionary<string, object?>[] NoKeyRules = [];

	private bool _dragging;
	private double _dragStartValue;
	private double _dragStartY;
	private bool _keysCancelled;
	private double? _lastValue;
	private ElementReference _slider;
	private double _value;

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

	/// <summary>
	///     Optional label displayed below the knob. It also names the slider for screen readers.
	///     Without a label, pass <c>aria-label</c>.
	/// </summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Whether to display the current value in the center. Defaults to true.</summary>
	[Parameter]
	public bool ShowValue { get; set; } = true;

	/// <summary>
	///     .NET number format string for the displayed value. Defaults to "F0". Screen readers read
	///     the value in this format too.
	/// </summary>
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

	private bool IsInteractive => !Disabled;

	// A disabled knob leaves the tab order.
	private int? TabIndex => Disabled ? null : 0;

	/// <summary>Clamped proportion of value within [Min, Max], from 0 to 1.</summary>
	private double NormalizedValue
	{
		get
		{
			if (Max <= Min)
			{
				return 0;
			}

			return Math.Clamp((_value - Min) / (Max - Min), 0, 1);
		}
	}

	/// <summary>Total sweep angle of the arc in degrees.</summary>
	private double SweepAngle => EndAngle - StartAngle;

	/// <summary>Arc circumference for the given sweep.</summary>
	private double ArcLength => Math.Abs(SweepAngle) / 360.0 * 2 * Math.PI * Radius;

	/// <summary>Dash offset to reveal the filled portion of the arc.</summary>
	private double DashOffset => ArcLength * (1 - NormalizedValue);

	/// <summary>Formatted value string for display.</summary>
	private string FormattedValue => _value.ToString(Format, CultureInfo.CurrentCulture);

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
	private static string TrackWidthStr => TrackWidth.ToString(CultureInfo.InvariantCulture);

	// Min + n * Step picks up binary noise (7 * 0.1 is 0.7000000000000001), which would reach
	// ValueChanged and aria-valuenow. A value on the grid has no more decimals than Min and Step.
	private int GridDecimals => Math.Max(DecimalPlaces(Step), DecimalPlaces(Min));

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Value only moves the knob when the parent passes a new value. Copying it on every parent
		// render turned an unbound knob back to where it started.
		if (!EqualityComparer<double?>.Default.Equals(_lastValue, Value))
		{
			_lastValue = Value;
			_value = Value;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// The value keys are cancelled in the browser only while they do something. A disabled knob
		// leaves them to the page, and never loads the script if it starts that way.
		if (IsInteractive == _keysCancelled)
		{
			return;
		}

		_keysCancelled = IsInteractive;
		await SafeModuleInvokeVoidAsync(KeysModule, "preventKeys", _slider,
			IsInteractive ? SliderKeyRules : NoKeyRules);
	}

	// ARIA wants plain numbers. Blazor writes a double with the current culture, which gives "0,5".
	private static string AriaNumber(double value) => value.ToString(CultureInfo.InvariantCulture);

	private static int DecimalPlaces(double value) =>
		double.IsFinite(value) && Math.Abs(value) < 1e15 ? Math.Min((int)((decimal)value).Scale, 15) : 0;

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (!IsInteractive || e.AltKey || e.CtrlKey || e.MetaKey)
		{
			return;
		}

		double? next = e.Key switch
		{
			"ArrowRight" or "ArrowUp" => SnapToStep(_value + KeyStep),
			"ArrowLeft" or "ArrowDown" => SnapToStep(_value - KeyStep),
			"PageUp" => SnapToStep(_value + KeyStep * PageSteps),
			"PageDown" => SnapToStep(_value - KeyStep * PageSteps),
			"Home" => Low,
			"End" => High,
			_ => null
		};

		if (next is { } newValue && Math.Abs(newValue - _value) > double.Epsilon)
		{
			await SetValueAsync(newValue);
		}
	}

	private void OnPointerDown(PointerEventArgs e)
	{
		if (Disabled)
		{
			return;
		}

		_dragging = true;
		_dragStartY = e.ClientY;
		_dragStartValue = _value;
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

		if (Math.Abs(newValue - _value) > double.Epsilon)
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
		double newValue = SnapToStep(_value + direction * KeyStep);

		if (Math.Abs(newValue - _value) > double.Epsilon)
		{
			await SetValueAsync(newValue);
		}
	}

	// Math.Clamp throws when Max is below Min, so the bounds are put in order first.
	private double Low => Math.Min(Min, Max);

	private double High => Math.Max(Min, Max);

	// A Step of zero or less would leave the keys and the wheel doing nothing; they move by a
	// hundredth of the range instead.
	private double KeyStep => Step > 0 ? Step : (High - Low) / 100;

	private double SnapToStep(double value)
	{
		double clamped = Math.Clamp(value, Low, High);

		if (Step > 0)
		{
			clamped = Math.Round((clamped - Low) / Step) * Step + Low;
			clamped = Math.Clamp(Math.Round(clamped, GridDecimals), Low, High);
		}

		return clamped;
	}

	private async Task SetValueAsync(double newValue)
	{
		_value = newValue;
		await ValueChanged.InvokeAsync(newValue);
	}
}
