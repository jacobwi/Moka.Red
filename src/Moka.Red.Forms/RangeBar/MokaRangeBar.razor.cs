using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.RangeBar;

/// <summary>
///     One track with a range dragged inside it, and colored parts, ticks and pins along it, so the range is chosen
///     against what's already on the scale. A flag over each handle shows its value through <see cref="Formatter" />.
///     Bind <see cref="Start" /> and <see cref="End" />, or <c>Value</c> as a <see cref="MokaRange" /> inside an
///     <c>EditForm</c>. Each handle is a slider: arrows move it a step, Shift or Page Up and Page Down ten, Home and
///     End to its limits. A drag runs in the browser and the bar reports where it was let go. For a plain two-thumb
///     slider with nothing on the track, use <c>MokaRangeSlider</c>.
/// </summary>
public partial class MokaRangeBar
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";
	private const string ModulePath = "./_content/Moka.Red.Forms/RangeBar/MokaRangeBar.razor.js";

	// Shift, Page Up and Page Down move this many steps.
	private const int BigSteps = 10;

	// Left to the browser the arrows, Page Up, Page Down, Home and End scroll the page. Dictionaries rather than
	// anonymous types, which trimming can strip in WebAssembly apps.
	private static readonly Dictionary<string, object?>[] KeyRules =
	[
		new()
		{
			["selector"] = "[role=\"slider\"]",
			["keys"] = new[] { "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "PageUp", "PageDown", "Home", "End" },
			["unlessModified"] = true
		}
	];

	private readonly string _generatedId = $"moka-range-bar-{Guid.NewGuid():N}";

	private ElementReference _bar;
	private bool _bound;
	private DotNetObjectReference<MokaRangeBar>? _dotNetRef;
	private double _end;
	private bool _endGiven;
	private double? _lastEndIn;
	private double? _lastStartIn;
	private double _start;
	private bool _startGiven;
	private bool _valueGiven;

	/// <summary>Where the range starts. Two-way bindable. Without it or <c>Value</c> the range starts at <see cref="Min" />.</summary>
	[Parameter]
	public double Start { get; set; }

	/// <summary>Raised with the new start when a key or a drag moves the start handle.</summary>
	[Parameter]
	public EventCallback<double> StartChanged { get; set; }

	/// <summary>Where the range ends. Two-way bindable. Without it or <c>Value</c> the range ends at <see cref="Max" />.</summary>
	[Parameter]
	public double End { get; set; }

	/// <summary>Raised with the new end when a key or a drag moves the end handle.</summary>
	[Parameter]
	public EventCallback<double> EndChanged { get; set; }

	/// <summary>The low end of the scale, and the lowest the range can start. Default 0.</summary>
	[Parameter]
	public double Min { get; set; }

	/// <summary>The high end of the scale, and the highest the range can end. Default 100.</summary>
	[Parameter]
	public double Max { get; set; } = 100;

	/// <summary>
	///     The values a handle stops at, counted from <see cref="Min" />. Default 1. Zero or less lets a drag stop
	///     anywhere, and the keys then move a hundredth of the scale.
	/// </summary>
	[Parameter]
	public double Step { get; set; } = 1;

	/// <summary>Colored stretches of the track, drawn under the range.</summary>
	[Parameter]
	public IReadOnlyList<MokaRangePart> Parts { get; set; } = [];

	/// <summary>Thin lines under the track, such as addresses in use now.</summary>
	[Parameter]
	public IReadOnlyList<MokaRangeMark> Ticks { get; set; } = [];

	/// <summary>Dots under the track, such as addresses kept for a device.</summary>
	[Parameter]
	public IReadOnlyList<MokaRangeMark> Pins { get; set; } = [];

	/// <summary>
	///     Turns a value into the text of a flag, a scale end, a mark's tooltip and what screen readers hear, such as
	///     an IP address from a number. Default: the number in the current culture.
	/// </summary>
	[Parameter]
	public Func<double, string>? Formatter { get; set; }

	/// <summary>Label text above the bar. It names the group, and each handle is named after it.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text below the bar.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text below the bar, which also shows the error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>The start handle's name for screen readers. Default: <see cref="Label" /> and "start".</summary>
	[Parameter]
	public string? StartLabel { get; set; }

	/// <summary>The end handle's name for screen readers. Default: <see cref="Label" /> and "end".</summary>
	[Parameter]
	public string? EndLabel { get; set; }

	/// <summary>
	///     What the range is, such as "Handed out". Drawn inside the range while the bar shows labels and in the key
	///     when it doesn't.
	/// </summary>
	[Parameter]
	public string? RangeLabel { get; set; }

	/// <summary>
	///     Shows the bar without handles or flags: a single track with the range drawn as one more part in
	///     <c>Color</c>. Default false.
	/// </summary>
	[Parameter]
	public bool ReadOnly { get; set; }

	/// <summary>
	///     Whether labels sit inside the parts and the range, on tinted tones. Off, the parts are drawn in solid
	///     tones and a key under the bar names them. Default true.
	/// </summary>
	[Parameter]
	public bool Labels { get; set; } = true;

	/// <summary>Whether the two ends of the scale are written under the bar. Default true.</summary>
	[Parameter]
	public bool ShowScale { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-range-bar";

	/// <inheritdoc />
	protected override string ComponentCssClass => new CssBuilder(RootClass)
		.AddClass("moka-range-bar--read-only", ReadOnly)
		.AddClass("moka-range-bar--solid", !Labels)
		.AddClass("moka-range-bar--disabled", Disabled)
		.AddClass("moka-range-bar--error", HasError)
		.AddClass(CssClass)
		.AddClass(Class)
		.Build();

	// The field wrapper is the outermost element, so the margin goes there. The track takes the radius.

	/// <inheritdoc />
	protected override string? ComponentStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	// Id goes on the start handle, the first control, and the label's id is built from it. A read-only bar has no
	// control, so the group carries it.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	private string? GroupId => ReadOnly ? InputId : null;

	private string? GroupLabelledBy => string.IsNullOrWhiteSpace(Label) ? null : MokaFieldWrapper.LabelIdFor(InputId);

	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string BarCssClass => new CssBuilder("moka-range-bar__bar")
		.AddClass("moka-range-bar__bar--static", ReadOnly)
		.AddClass("moka-range-bar__bar--no-scale", !ShowScale)
		.Build();

	// Every part of the bar that moves in a drag is placed from these four, so the browser moves them all by
	// setting the same four on the bar.
	private string? BarStyle => new StyleBuilder()
		.AddStyle("--_range-start", Number(Fraction(ShownStart)))
		.AddStyle("--_range-end", Number(Fraction(ShownEnd)))
		.AddStyle("--_range-start-chars", Number(StartText.Length))
		.AddStyle("--_range-end-chars", Number(EndText.Length))
		.Build();

	private string? TrackStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private string RangeCssClass => new CssBuilder("moka-range-bar__range")
		.AddClass(ToneClass(RangeColor))
		.Build();

	private MokaColor RangeColor => Color ?? MokaColor.Primary;

	private int? HandleTabIndex => Disabled ? null : 0;

	private string StartName => !string.IsNullOrWhiteSpace(StartLabel) ? StartLabel : $"{BaseName} start";

	private string EndName => !string.IsNullOrWhiteSpace(EndLabel) ? EndLabel : $"{BaseName} end";

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

	// Math.Clamp throws when Max is below Min, so the ends of the scale are put in order first.
	private double Low => Math.Min(Min, Max);

	private double High => Math.Max(Min, Max);

	// What the bar shows: inside the scale and in order, whatever the parent passed. Nothing is reported back
	// until a key or a drag moves a handle.
	private double ShownStart => Math.Min(Inside(_start, Low), Inside(_end, High));

	private double ShownEnd => Math.Max(Inside(_start, Low), Inside(_end, High));

	private string StartText => Text(ShownStart);

	private string EndText => Text(ShownEnd);

	private double KeyStep => Step > 0 ? Step : (High - Low) / 100;

	// Min + n * Step picks up binary noise (7 * 0.1 is 0.7000000000000001), which would reach StartChanged and
	// aria-valuenow. A value on the grid has no more decimals than Min and Step.
	private int GridDecimals => Step > 0 ? Math.Max(DecimalPlaces(Step), DecimalPlaces(Low)) : 15;

	private IEnumerable<MokaRangePart> ShownParts => Parts.Where(part =>
		Math.Max(part.Start, part.End) >= Low && Math.Min(part.Start, part.End) <= High);

	private IEnumerable<MokaRangeMark> ShownMarks(IReadOnlyList<MokaRangeMark> marks) =>
		marks.Where(mark => mark.Value >= Low && mark.Value <= High);

	private bool HasMarks => ShownMarks(Ticks).Any() || ShownMarks(Pins).Any();

	/// <summary>Turns a value into text through <see cref="Formatter" />. Called by the browser for a flag in a drag.</summary>
	/// <param name="value">The value under the handle.</param>
	/// <returns>The flag's text.</returns>
	[JSInvokable]
	public string FormatValue(double value) => Text(value);

	/// <summary>Called by the browser when a drag lets go of a handle somewhere new.</summary>
	/// <param name="start">Where the start handle is.</param>
	/// <param name="end">Where the end handle is.</param>
	[JSInvokable]
	public void OnDragged(double start, double end) => RunDetached(async () =>
	{
		double low = Snap(Math.Min(start, end));
		double high = Snap(Math.Max(start, end));
		await CommitAsync(low, high);
		StateHasChanged();
	});

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		// Start, End and Value can each say where the range is, so what the parent left out has to be told apart
		// from a parent that passed the default. Once given, a parameter keeps its last value like any other.
		_valueGiven |= parameters.TryGetValue<MokaRange>(nameof(Value), out _);
		_startGiven |= parameters.TryGetValue<double>(nameof(Start), out _);
		_endGiven |= parameters.TryGetValue<double>(nameof(End), out _);
		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		double startIn = _startGiven ? Start : _valueGiven ? Value.Start : Low;
		double endIn = _endGiven ? End : _valueGiven ? Value.End : High;

		// Each end moves only when the parent passes a new value for it, so a parent that doesn't bind can't send a
		// handle back to where it started by rendering again.
		if (!EqualityComparer<double?>.Default.Equals(_lastStartIn, startIn))
		{
			_lastStartIn = startIn;
			_start = startIn;
		}

		if (!EqualityComparer<double?>.Default.Equals(_lastEndIn, endIn))
		{
			_lastEndIn = endIn;
			_end = endIn;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		// A read-only bar has nothing to drag, and loads no script until it gets handles.
		if (ReadOnly || _bound)
		{
			return;
		}

		_bound = true;
		_dotNetRef ??= DotNetObjectReference.Create(this);
		await SafeModuleInvokeVoidAsync(KeysModule, "preventKeys", _bar, KeyRules);
		await SafeModuleInvokeVoidAsync(ModulePath, "bind", _bar, _dotNetRef);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_bound)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "unbind", _bar);
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	/// <inheritdoc />
	protected override string? FormatValueAsString(MokaRange value) =>
		string.Create(CultureInfo.InvariantCulture, $"{value.Start}..{value.End}");

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out MokaRange result, out string validationErrorMessage)
	{
		int dots = value?.IndexOf("..", 1, StringComparison.Ordinal) ?? -1;
		if (value is not null && dots > 0
		                      && double.TryParse(value.AsSpan(0, dots), NumberStyles.Float, CultureInfo.InvariantCulture, out double start)
		                      && double.TryParse(value.AsSpan(dots + 2), NumberStyles.Float, CultureInfo.InvariantCulture, out double end))
		{
			result = new MokaRange(Math.Min(start, end), Math.Max(start, end));
			validationErrorMessage = string.Empty;
			return true;
		}

		result = default;
		validationErrorMessage = $"'{value}' is not a range such as 10..20.";
		return false;
	}

	private string Text(double value) => Formatter?.Invoke(value) ?? value.ToString("G", CultureInfo.CurrentCulture);

	private double Fraction(double value) => High > Low ? Math.Clamp((value - Low) / (High - Low), 0, 1) : 0;

	private static string Percent(double fraction) =>
		string.Create(CultureInfo.InvariantCulture, $"{fraction * 100:0.###}%");

	private string? SpanStyle(double from, double to)
	{
		double left = Fraction(Math.Min(from, to));
		double right = Fraction(Math.Max(from, to));
		return new StyleBuilder()
			.AddStyle("left", Percent(left))
			.AddStyle("width", Percent(right - left))
			.Build();
	}

	private string? MarkStyle(MokaRangeMark mark) => new StyleBuilder()
		.AddStyle("left", Percent(Fraction(mark.Value)))
		.Build();

	private static string Number(double value) => value.ToString("0.#####", CultureInfo.InvariantCulture);

	private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

	// ARIA and the script want plain numbers. Blazor writes a double in the current culture, which gives "0,5".
	private static string Plain(double value) => value.ToString(CultureInfo.InvariantCulture);

	private static string ToneClass(MokaColor color) => $"moka-range-bar__tone--{MokaEnumHelpers.ToCssClass(color)}";

	private static string PartCssClass(MokaColor color, bool range = false) => new CssBuilder("moka-range-bar__part")
		.AddClass("moka-range-bar__part--range", range)
		.AddClass(ToneClass(color))
		.Build();

	private static string TickCssClass(MokaRangeMark mark, bool placed) => new CssBuilder("moka-range-bar__tick")
		.AddClass("moka-range-bar__mark--placed", placed)
		.AddClass("moka-range-bar__tick--toned", mark.Color is not null)
		.AddClass(mark.Color is { } color ? ToneClass(color) : null)
		.Build();

	private static string PinCssClass(MokaRangeMark mark, bool placed) => new CssBuilder("moka-range-bar__pin")
		.AddClass("moka-range-bar__mark--placed", placed)
		.AddClass(ToneClass(mark.Color ?? MokaColor.Primary))
		.Build();

	private string HandleCssClass(bool start) => new CssBuilder("moka-range-bar__handle")
		.AddClass("moka-range-bar__handle--start", start)
		.AddClass("moka-range-bar__handle--end", !start)
		.AddClass(ToneClass(RangeColor))
		.Build();

	private string KeyCssClass(bool hasEntries) => new CssBuilder()
		.AddClass("moka-range-bar__key", hasEntries && !Labels)
		.AddClass("moka-visually-hidden", !hasEntries || Labels)
		.Build();

	private static string KeyDotCssClass(MokaColor color) => new CssBuilder("moka-range-bar__key-dot")
		.AddClass(ToneClass(color))
		.Build();

	private string MarkTip(MokaRangeMark mark) => $"{mark.Label}: {Text(mark.Value)}";

	private string Span(double from, double to) => $"{Text(Math.Min(from, to))} to {Text(Math.Max(from, to))}";

	private double Inside(double value, double fallback) => double.IsNaN(value) ? fallback : Math.Clamp(value, Low, High);

	private static int DecimalPlaces(double value) =>
		double.IsFinite(value) && Math.Abs(value) < 1e15 ? Math.Min((int)((decimal)value).Scale, 15) : 0;

	// The parts and the range in the order they sit on the scale, one entry for each label and tone, so a part
	// drawn in several places is named once. Screen readers hear where each one is; the key shows the names.
	private List<KeyEntry> KeyEntries()
	{
		List<KeyEntry> entries = [];
		IEnumerable<(double From, double To, string Label, MokaColor Color)> spans = ShownParts
			.Where(part => !string.IsNullOrWhiteSpace(part.Label))
			.Select(part => (Math.Min(part.Start, part.End), Math.Max(part.Start, part.End), part.Label!, part.Color));
		if (!string.IsNullOrWhiteSpace(RangeLabel))
		{
			spans = spans.Append((ShownStart, ShownEnd, RangeLabel, RangeColor));
		}

		foreach ((double from, double to, string label, MokaColor color) in spans.OrderBy(span => span.From))
		{
			KeyEntry? entry = entries.Find(e => string.Equals(e.Label, label, StringComparison.Ordinal) && e.Color == color);
			if (entry is null)
			{
				entries.Add(entry = new KeyEntry(label, color));
			}

			entry.Spans.Add(Span(from, to));
		}

		return entries;
	}

	private IEnumerable<MokaRangeMark> NamedMarks() =>
		ShownMarks(Pins).Concat(ShownMarks(Ticks)).Where(mark => !string.IsNullOrWhiteSpace(mark.Label));

	private async Task HandleKeyAsync(KeyboardEventArgs e, bool startHandle)
	{
		if (Disabled || ReadOnly || e.AltKey || e.CtrlKey || e.MetaKey)
		{
			return;
		}

		double start = ShownStart;
		double end = ShownEnd;
		double current = startHandle ? start : end;
		double lowest = startHandle ? Low : start;
		double highest = startHandle ? end : High;
		double step = e.ShiftKey ? KeyStep * BigSteps : KeyStep;

		double? next = e.Key switch
		{
			"ArrowRight" or "ArrowUp" => Snap(current + step),
			"ArrowLeft" or "ArrowDown" => Snap(current - step),
			"PageUp" => Snap(current + KeyStep * BigSteps),
			"PageDown" => Snap(current - KeyStep * BigSteps),
			"Home" => lowest,
			"End" => highest,
			_ => null
		};

		if (next is not { } value)
		{
			return;
		}

		value = Math.Clamp(value, lowest, highest);
		await (startHandle ? CommitAsync(value, end) : CommitAsync(start, value));
	}

	private double Snap(double value)
	{
		double clamped = Inside(value, Low);
		if (Step > 0)
		{
			clamped = Math.Round((clamped - Low) / Step, MidpointRounding.AwayFromZero) * Step + Low;
			clamped = Math.Clamp(Math.Round(clamped, GridDecimals), Low, High);
		}

		return clamped;
	}

	private async Task CommitAsync(double start, double end)
	{
		bool startMoved = start != ShownStart;
		bool endMoved = end != ShownEnd;
		if (!startMoved && !endMoved)
		{
			return;
		}

		_start = start;
		_end = end;

		if (startMoved)
		{
			await StartChanged.InvokeAsync(start);
		}

		if (endMoved)
		{
			await EndChanged.InvokeAsync(end);
		}

		await ValueChanged.InvokeAsync(new MokaRange(start, end));
		EditContext?.NotifyFieldChanged(FieldIdentifier);
	}

	private sealed class KeyEntry(string label, MokaColor color)
	{
		public string Label { get; } = label;

		public MokaColor Color { get; } = color;

		public List<string> Spans { get; } = [];
	}
}
