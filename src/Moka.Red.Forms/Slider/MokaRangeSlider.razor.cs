using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.Slider;

/// <summary>
///     A dual-handle range slider for selecting a min-max range.
///     Uses two overlapping native range inputs with CSS to highlight the selected range.
/// </summary>
public partial class MokaRangeSlider : MokaVisualComponentBase
{
	/// <summary>The lower value of the range. Two-way bindable.</summary>
	[Parameter]
	public double ValueStart { get; set; }

	/// <summary>Callback invoked when <see cref="ValueStart" /> changes.</summary>
	[Parameter]
	public EventCallback<double> ValueStartChanged { get; set; }

	/// <summary>The upper value of the range. Two-way bindable.</summary>
	[Parameter]
	public double ValueEnd { get; set; } = 100;

	/// <summary>Callback invoked when <see cref="ValueEnd" /> changes.</summary>
	[Parameter]
	public EventCallback<double> ValueEndChanged { get; set; }

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

	/// <summary>Whether to show the current range values. Default true.</summary>
	[Parameter]
	public bool ShowValues { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-range-slider";

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-range-slider--disabled", Disabled)
		.AddClass(Class)
		.Build();

	private double StartPercent => Max > Min
		? (ValueStart - Min) / (Max - Min) * 100
		: 0;

	private double EndPercent => Max > Min
		? (ValueEnd - Min) / (Max - Min) * 100
		: 100;

	private string TrackStyle => string.Format(
		CultureInfo.InvariantCulture,
		"--moka-range-start: {0:F2}%; --moka-range-end: {1:F2}%",
		StartPercent, EndPercent);

	/// <summary>RangeSlider has internal state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	private async Task HandleStartInput(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), NumberStyles.Any,
			    CultureInfo.InvariantCulture, out double value))
		{
			// Prevent start from exceeding end
			ValueStart = Math.Min(value, ValueEnd);
			await ValueStartChanged.InvokeAsync(ValueStart);
		}
	}

	private async Task HandleEndInput(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), NumberStyles.Any,
			    CultureInfo.InvariantCulture, out double value))
		{
			// Prevent end from going below start
			ValueEnd = Math.Max(value, ValueStart);
			await ValueEndChanged.InvokeAsync(ValueEnd);
		}
	}
}
