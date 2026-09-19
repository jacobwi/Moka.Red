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

	/// <summary>Whether the control stretches to fill its container width. Default false.</summary>
	[Parameter]
	public bool FullWidth { get; set; }

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

		// Value only seeds the selection when the parent passes a new value, so a parent render that
		// passes the old one again cannot undo the user's choice.
		if (!string.Equals(_lastValue, Value, StringComparison.Ordinal))
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

		_value = value;

		// A segment calls this from its own change handler, which re-renders only that segment.
		// Rendering the control updates every segment, including the one that lost the selection.
		StateHasChanged();

		if (ValueChanged.HasDelegate)
		{
			await ValueChanged.InvokeAsync(value);
		}
	}

	/// <summary>Checks whether a given segment value is currently selected.</summary>
	internal bool IsSelected(string value) => string.Equals(_value, value, StringComparison.Ordinal);
}
