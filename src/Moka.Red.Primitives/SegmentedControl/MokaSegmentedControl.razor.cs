using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.SegmentedControl;

/// <summary>
///     A row of connected segments for picking one option (segmented control / pill toggle).
///     Children are <see cref="MokaSegment" /> components. It is a radio group: each segment is a
///     native radio button, so Tab reaches the selected segment and the arrow keys move the selection.
/// </summary>
public partial class MokaSegmentedControl
{
	private string? _browserChecked;
	private string? _lastValue;
	private string? _value;

	/// <summary>Child content containing <see cref="MokaSegment" /> elements.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Currently selected segment value. Two-way bindable.</summary>
	[Parameter]
	public string? Value { get; set; }

	/// <summary>Callback for when <see cref="Value" /> changes.</summary>
	[Parameter]
	public EventCallback<string> ValueChanged { get; set; }

	/// <summary>
	///     After raising its change callback, the control shows what the parent passes back, so a refused change reverts.
	///     Here the callback is <see cref="ValueChanged" />, and the control always shows <see cref="Value" />. Default
	///     false: the control shows the user's pick at once and follows the parent only when it passes a different value.
	/// </summary>
	[Parameter]
	public bool Controlled { get; set; }

	/// <summary>Whether the control stretches to fill its container width. Default false.</summary>
	[Parameter]
	public bool FullWidth { get; set; }

	/// <summary>
	///     On the new design, the control is exactly as tall as a text field, select or search box of the same
	///     <c>Size</c> (<c>--moka-control-height</c> and the steps those fields take from it), so it lines up beside them
	///     in a toolbar: 32 px at <c>MokaSize.Sm</c> where a small field is 32 px. Off by default, when the segments keep
	///     their own height (<c>--moka-segment-height</c>) and the track adds its padding to it. The original design has
	///     no shared control height, so there it changes nothing.
	/// </summary>
	[Parameter]
	public bool MatchControlHeight { get; set; }

	/// <summary>
	///     Whether segments that don't fit move to another row. Default true. False keeps them on one row: short labels
	///     keep their full width, the longer ones share what's left and end in an ellipsis, and each label's full text is
	///     in a <c>title</c>.
	/// </summary>
	[Parameter]
	public bool Wrap { get; set; } = true;

	/// <summary>
	///     Accessible name of the group, such as "Time range". Screen readers announce it when focus
	///     enters the control. To use visible text as the name instead, pass <c>aria-labelledby</c>.
	/// </summary>
	[Parameter]
	public string? AriaLabel { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-segmented";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-segmented--{SizeToKebab(Size)}")
		.AddClass("moka-segmented--full-width", FullWidth)
		.AddClass("moka-fill-width", FullWidth)
		.AddClass("moka-segmented--disabled", Disabled)
		.AddClass("moka-segmented--rounded", ResolvedRounding is not null)
		.AddClass("moka-segmented--control-height", MatchControlHeight)
		.AddClass("moka-segmented--one-row", !Wrap)
		.AddClass(Class)
		.Build();

	/// <summary>
	///     The <c>name</c> every segment's radio shares. The browser needs it to treat them as one
	///     group: one checked at a time, one tab stop, and the arrow keys moving between them.
	/// </summary>
	internal string GroupName { get; } = $"moka-segmented-{Guid.NewGuid():N}";

	/// <summary>Has internal selection state.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Uncontrolled, Value only seeds the selection when the parent passes a new value, so a parent render that
		// passes the old one again cannot undo the user's choice.
		if (Controlled || !string.Equals(_lastValue, Value, StringComparison.Ordinal))
		{
			_lastValue = Value;
			_value = Value;
		}
	}

	/// <summary>Sets the selected segment value.</summary>
	internal async Task SelectAsync(string value)
	{
		if (Disabled || string.Equals(_value, value, StringComparison.Ordinal))
		{
			return;
		}

		if (Controlled)
		{
			await AskAsync(value);
			return;
		}

		_value = value;

		// A segment calls this from its own change handler, which re-renders only that segment.
		// Rendering the control updates every segment, including the one that lost the selection.
		StateHasChanged();

		if (ValueChanged.HasDelegate)
		{
			await ValueChanged.InvokeAsync(value);
		}
	}

	// The browser checks the radio before .NET hears of it, and Blazor's tree still holds the old one checked, so
	// rendering the old choice again would send nothing and leave the refused radio checked. One render with the
	// browser's radio checked, sent on its own, gives the next render a change to make, and the browser checks the old
	// radio again. The segments' look follows the value throughout, so the refused one is never drawn as chosen.
	private async Task AskAsync(string value)
	{
		_browserChecked = value;
		StateHasChanged();
		Task answer = ValueChanged.InvokeAsync(value);

		await Task.Yield();
		_browserChecked = null;
		StateHasChanged();

		await answer;
	}

	/// <summary>Checks whether a given segment value is currently selected.</summary>
	internal bool IsSelected(string value) => string.Equals(_value, value, StringComparison.Ordinal);

	// Apart from IsSelected for the moment after a controlled click, when the radio follows the browser and the look
	// still follows the value.
	internal bool IsChecked(string value) =>
		string.Equals(_browserChecked ?? _value, value, StringComparison.Ordinal);
}
