using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.Slider;

/// <summary>
///     A dual-handle range slider for selecting a min-max range.
///     Uses two overlapping native range inputs with CSS to highlight the selected range.
/// </summary>
public partial class MokaRangeSlider : MokaVisualComponentBase
{
	private readonly string _startId = $"moka-range-slider-{Guid.NewGuid():N}";

	// What a thumb's input renders for one pass after the other thumb held it back (see
	// RenderDraggedValueOnceAsync). Null the rest of the time.
	private string? _endEcho;
	private double? _lastValueEnd;
	private double? _lastValueStart;
	private string? _startEcho;
	private double _valueEnd = 100;
	private double _valueStart;

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

	/// <summary>
	///     Label text displayed above the slider. It names the pair of thumbs, and each thumb is named
	///     after it ("Price start", "Price end"). Without a label, pass <c>aria-label</c>.
	/// </summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Whether to show the current range values. Default true.</summary>
	[Parameter]
	public bool ShowValues { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-range-slider";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-range-slider--disabled", Disabled)
		.AddClass(Class)
		.Build();

	private double StartPercent => Max > Min
		? (_valueStart - Min) / (Max - Min) * 100
		: 0;

	private double EndPercent => Max > Min
		? (_valueEnd - Min) / (Max - Min) * 100
		: 100;

	// The field wrapper is the outermost element, so the margin goes there. Padding widens the
	// group around the thumbs. The radius shapes the track, which draws the bar through ::before.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string? TrackStyle => new StyleBuilder()
		.AddStyle("--moka-range-start", Percent(StartPercent))
		.AddStyle("--moka-range-end", Percent(EndPercent))
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private static string Percent(double value) => string.Create(CultureInfo.InvariantCulture, $"{value:F2}%");

	// Where the thumbs meet, the end one is drawn on top and catches every drag. In the upper half
	// of the range the start thumb goes on top, since the room to move is below; at Max it is the
	// only one that can move at all.
	private string StartInputCssClass => new CssBuilder("moka-range-slider-input")
		.AddClass("moka-range-slider-input--start")
		.AddClass("moka-range-slider-input--on-top", StartOnTop)
		.Build();

	private bool StartOnTop => _valueStart >= Max || (_valueStart >= _valueEnd && (_valueStart - Min) * 2 >= Max - Min);

	private string StartInputValue => _startEcho ?? _valueStart.ToString(CultureInfo.InvariantCulture);

	private string EndInputValue => _endEcho ?? _valueEnd.ToString(CultureInfo.InvariantCulture);

	// The label cannot name two inputs, so it names the group and each thumb carries its own name.
	private string? GroupLabelledBy => string.IsNullOrWhiteSpace(Label) ? null : MokaFieldWrapper.LabelIdFor(_startId);

	private string StartName => $"{BaseName} start";

	private string EndName => $"{BaseName} end";

	private string BaseName
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(Label))
			{
				return Label;
			}

			if (AdditionalAttributes?.TryGetValue("aria-label", out object? ariaLabel) == true
			    && ariaLabel is string name && !string.IsNullOrWhiteSpace(name))
			{
				return name;
			}

			return "Range";
		}
	}

	/// <summary>RangeSlider has internal state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Each end only moves when the parent passes a new value for it. Copying them on every
		// parent render sent the thumbs of an unbound slider back to where they started.
		if (!EqualityComparer<double?>.Default.Equals(_lastValueStart, ValueStart))
		{
			_lastValueStart = ValueStart;
			_valueStart = ValueStart;
		}

		if (!EqualityComparer<double?>.Default.Equals(_lastValueEnd, ValueEnd))
		{
			_lastValueEnd = ValueEnd;
			_valueEnd = ValueEnd;
		}
	}

	private async Task HandleStartInput(ChangeEventArgs e)
	{
		string? raw = e.Value?.ToString();
		_startEcho = null;
		if (!double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
		{
			return;
		}

		// The start thumb stops at the end one.
		double start = Math.Min(value, _valueEnd);
		if (start != value && start == _valueStart)
		{
			_startEcho = raw;
			if (!await RenderDraggedValueOnceAsync(() => _startEcho == raw))
			{
				return;
			}

			_startEcho = null;
		}

		_valueStart = start;
		await ValueStartChanged.InvokeAsync(start);
	}

	private async Task HandleEndInput(ChangeEventArgs e)
	{
		string? raw = e.Value?.ToString();
		_endEcho = null;
		if (!double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
		{
			return;
		}

		// The end thumb stops at the start one.
		double end = Math.Max(value, _valueStart);
		if (end != value && end == _valueEnd)
		{
			_endEcho = raw;
			if (!await RenderDraggedValueOnceAsync(() => _endEcho == raw))
			{
				return;
			}

			_endEcho = null;
		}

		_valueEnd = end;
		await ValueEndChanged.InvokeAsync(end);
	}

	// A thumb held back at the other one keeps the value the last render wrote, so Blazor sends no
	// change and the browser leaves the thumb where it was dragged, past the other one (gotcha #24).
	// One render with the dragged value gives the next render a value to change. False when a later
	// input to the same thumb got there first.
	private async Task<bool> RenderDraggedValueOnceAsync(Func<bool> stillLatest)
	{
		StateHasChanged();
		await Task.Yield();
		return stillLatest();
	}
}
