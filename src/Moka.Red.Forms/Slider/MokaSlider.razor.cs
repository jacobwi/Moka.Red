using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.Slider;

/// <summary>
///     A range slider input using a native HTML range input with custom CSS styling.
///     Provides keyboard support and accessibility via the native element.
/// </summary>
public partial class MokaSlider : MokaVisualComponentBase
{
	private readonly string _generatedId = $"moka-slider-{Guid.NewGuid():N}";
	private double? _lastValue;
	private double _value;

	// The consumer's Id names the range input, and the label's for follows it, so the label names
	// the slider.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	/// <summary>The current slider value. Two-way bindable.</summary>
	[Parameter]
	public double Value { get; set; }

	/// <summary>Callback invoked when <see cref="Value" /> changes.</summary>
	[Parameter]
	public EventCallback<double> ValueChanged { get; set; }

	/// <summary>Minimum value. Default 0.</summary>
	[Parameter]
	public double Min { get; set; }

	/// <summary>Maximum value. Default 100.</summary>
	[Parameter]
	public double Max { get; set; } = 100;

	/// <summary>Step increment. Default 1.</summary>
	[Parameter]
	public double Step { get; set; } = 1;

	/// <summary>Label text displayed above the slider.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the slider.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Whether to show the current value next to the slider. Default true.</summary>
	[Parameter]
	public bool ShowValue { get; set; } = true;

	/// <summary>Whether to show tick marks at step intervals. Default false.</summary>
	[Parameter]
	public bool ShowTicks { get; set; }

	/// <summary>Whether to show min/max labels. Default false.</summary>
	[Parameter]
	public bool ShowMinMax { get; set; }

	/// <summary>Format string for display value (e.g., "F0", "P0").</summary>
	[Parameter]
	public string? ValueFormat { get; set; }

	/// <summary>Whether the slider is vertical. Default false.</summary>
	[Parameter]
	public bool Vertical { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-slider";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-slider--vertical", Vertical)
		.AddClass("moka-slider--disabled", Disabled)
		.AddClass("moka-slider--show-ticks", ShowTicks)
		.AddClass(Class)
		.Build();

	// The field wrapper is the outermost element, so the margin goes there. Padding widens the
	// slider's row. The radius shapes the track, since the row has no outline.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string? InputStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private string FormattedValue => ValueFormat is not null
		? _value.ToString(ValueFormat, CultureInfo.CurrentCulture)
		: _value.ToString("G", CultureInfo.CurrentCulture);

	private string FormattedMin => ValueFormat is not null
		? Min.ToString(ValueFormat, CultureInfo.CurrentCulture)
		: Min.ToString("G", CultureInfo.CurrentCulture);

	private string FormattedMax => ValueFormat is not null
		? Max.ToString(ValueFormat, CultureInfo.CurrentCulture)
		: Max.ToString("G", CultureInfo.CurrentCulture);

	/// <summary>Percentage of the filled track (0-100).</summary>
	private double FillPercent => Max > Min
		? (_value - Min) / (Max - Min) * 100
		: 0;

	private string TrackStyle => $"--moka-slider-fill: {FillPercent.ToString("F2", CultureInfo.InvariantCulture)}%";

	/// <summary>Slider has internal value state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Value only moves the thumb when the parent passes a new value. Copying it on every parent
		// render sent an unbound slider back to where it started.
		if (!EqualityComparer<double?>.Default.Equals(_lastValue, Value))
		{
			_lastValue = Value;
			_value = Value;
		}
	}

	private async Task HandleInput(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), NumberStyles.Any,
			    CultureInfo.InvariantCulture, out double value))
		{
			_value = value;
			await ValueChanged.InvokeAsync(value);
		}
	}
}
