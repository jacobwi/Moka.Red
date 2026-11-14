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

	// The ends the thumbs show, apart from ValueStart and ValueEnd, each owned by a parent that passes it and listens.
	private readonly MokaInputValue<double> _end;
	private readonly MokaInputValue<double> _start;

	// What a thumb's input renders for one pass after the other thumb held it back (see
	// RenderDraggedValueOnceAsync). Null the rest of the time.
	private string? _endEcho;
	private string? _startEcho;

	/// <summary>Creates the slider. Each end shows its own value until the parent passes one.</summary>
	public MokaRangeSlider()
	{
		_start = new MokaInputValue<double>(StateHasChanged, DispatchExceptionAsync, ValueStart);
		_end = new MokaInputValue<double>(StateHasChanged, DispatchExceptionAsync, ValueEnd);
	}

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

	private double ShownStart => _start.Shown;

	private double ShownEnd => _end.Shown;

	private double StartPercent => Max > Min
		? (ShownStart - Min) / (Max - Min) * 100
		: 0;

	private double EndPercent => Max > Min
		? (ShownEnd - Min) / (Max - Min) * 100
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

	private bool StartOnTop => ShownStart >= Max || (ShownStart >= ShownEnd && (ShownStart - Min) * 2 >= Max - Min);

	private string StartInputValue => _startEcho ?? ShownStart.ToString(CultureInfo.InvariantCulture);

	private string EndInputValue => _endEcho ?? ShownEnd.ToString(CultureInfo.InvariantCulture);

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
	public override Task SetParametersAsync(ParameterView parameters)
	{
		_start.Take(parameters, nameof(ValueStart));
		_end.Take(parameters, nameof(ValueEnd));
		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);
		_start.AfterRender();
		_end.AfterRender();
	}

	/// <inheritdoc />
	protected override ValueTask DisposeAsyncCore()
	{
		_start.Stop();
		_end.Stop();
		return base.DisposeAsyncCore();
	}

	private async Task HandleStartInput(string? typed)
	{
		string? raw = typed;
		_startEcho = null;
		if (!double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
		{
			return;
		}

		// The start thumb stops at the end one.
		double start = Math.Min(value, ShownEnd);
		if (start == ShownStart)
		{
			if (start != value)
			{
				_startEcho = raw;
				if (await RenderDraggedValueOnceAsync(() => _startEcho == raw))
				{
					_startEcho = null;
				}
			}

			return;
		}

		_start.Pick(start, ValueStartChanged);
	}

	private async Task HandleEndInput(string? typed)
	{
		string? raw = typed;
		_endEcho = null;
		if (!double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
		{
			return;
		}

		// The end thumb stops at the start one.
		double end = Math.Max(value, ShownStart);
		if (end == ShownEnd)
		{
			if (end != value)
			{
				_endEcho = raw;
				if (await RenderDraggedValueOnceAsync(() => _endEcho == raw))
				{
					_endEcho = null;
				}
			}

			return;
		}

		_end.Pick(end, ValueEndChanged);
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
