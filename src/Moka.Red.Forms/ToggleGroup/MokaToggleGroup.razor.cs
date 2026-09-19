using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.ToggleGroup;

/// <summary>
///     A group of connected toggle buttons where one or more can be selected.
///     In single-select mode, use <see cref="Value" />/<see cref="ValueChanged" />.
///     In multi-select mode, set <see cref="Multiple" /> and use <see cref="Values" />/<see cref="ValuesChanged" />.
///     Children are <see cref="MokaToggleGroupItem" /> components.
/// </summary>
public partial class MokaToggleGroup : MokaVisualComponentBase
{
	private string? _lastValue;

	// A copy of the last Values the parent passed, compared by content: a parent that builds a new
	// list on every render, or edits its own list in place, is still recognized.
	private string[]? _lastValues;
	private string? _value;
	private IReadOnlyList<string>? _values;

	/// <summary>Child content containing <see cref="MokaToggleGroupItem" /> elements.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Currently selected value in single-select mode. Two-way bindable.</summary>
	[Parameter]
	public string? Value { get; set; }

	/// <summary>Callback for when <see cref="Value" /> changes (single-select mode).</summary>
	[Parameter]
	public EventCallback<string?> ValueChanged { get; set; }

	/// <summary>Currently selected values in multi-select mode. Two-way bindable.</summary>
	[Parameter]
	public IReadOnlyList<string>? Values { get; set; }

	/// <summary>Callback for when <see cref="Values" /> changes (multi-select mode).</summary>
	[Parameter]
	public EventCallback<IReadOnlyList<string>?> ValuesChanged { get; set; }

	/// <summary>Whether multiple items can be selected simultaneously. Default is false.</summary>
	[Parameter]
	public bool Multiple { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-toggle-group";

	private MokaColor ResolvedColor => Color ?? MokaColor.Primary;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-toggle-group--{SizeToKebab(Size)}")
		.AddClass($"moka-toggle-group--{ColorToKebab(ResolvedColor)}")
		.AddClass("moka-toggle-group--disabled", Disabled)
		.AddClass(Class)
		.Build();

	/// <summary>Has internal selection state.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Value and Values only seed the selection when the parent passes a new value. Copying them
		// on every parent render undid the user's clicks in an unbound group.
		if (!string.Equals(_lastValue, Value, StringComparison.Ordinal))
		{
			_lastValue = Value;
			_value = Value;
		}

		if (!SameValues(_lastValues, Values))
		{
			_lastValues = Values?.ToArray();
			_values = Values;
		}
	}

	private static bool SameValues(string[]? last, IReadOnlyList<string>? current)
	{
		if (last is null || current is null)
		{
			return last is null && current is null;
		}

		return last.SequenceEqual(current, StringComparer.Ordinal);
	}

	/// <summary>Checks whether a given item value is currently selected.</summary>
	internal bool IsSelected(string value)
	{
		if (Multiple)
		{
			return _values?.Contains(value) == true;
		}

		return _value == value;
	}

	/// <summary>Toggles the selection of an item.</summary>
	/// <remarks>
	///     An item calls this from its own click handler, which re-renders only that item. The group
	///     renders itself so every item updates, including the one a single-select click turned off.
	/// </remarks>
#pragma warning disable CA1868 // Remove/Contains pattern - false positive: Remove return value is used for toggle logic
	internal async Task ToggleAsync(string value)
	{
		if (Multiple)
		{
			List<string> current = _values?.ToList() ?? [];
			if (!current.Remove(value))
			{
				current.Add(value);
			}

			_values = current.AsReadOnly();
			StateHasChanged();
			await ValuesChanged.InvokeAsync(_values);
		}
		else
		{
			// Single select: toggle off if same value clicked
			_value = _value == value ? null : value;
			StateHasChanged();
			await ValueChanged.InvokeAsync(_value);
		}
	}
#pragma warning restore CA1868
}
