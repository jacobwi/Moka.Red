using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Moka.Red.Forms.TreeSelect;

/// <summary>
///     A dropdown that displays a tree hierarchy for selection, suitable for
///     folder pickers, category selectors, and other hierarchical data.
///     Supports single and multiple selection modes with optional search filtering.
/// </summary>
/// <typeparam name="TValue">The type of the selectable value.</typeparam>
public partial class MokaTreeSelect<TValue> : MokaVisualComponentBase
{
	// What the select shows. Value and SelectedValues only replace it when the parent passes new
	// values, so a re-render that passes the old ones cannot undo a pick (gotcha #9).
	private TValue? _value;
	private TValue? _lastValueParameter;
	private IReadOnlyList<TValue>? _selectedValues;
	private IReadOnlyList<TValue>? _lastSelectedValuesParameter;

	private readonly HashSet<int> _expandedNodes = [];
	private readonly string _generatedId = $"moka-tree-select-{Guid.NewGuid():N}";
	private bool _isOpen;
	private string _searchText = string.Empty;

	/// <summary>The currently selected value in single-selection mode.</summary>
	[Parameter]
	public TValue? Value { get; set; }

	/// <summary>Callback invoked when the selected value changes.</summary>
	[Parameter]
	public EventCallback<TValue?> ValueChanged { get; set; }

	/// <summary>The hierarchical items to display in the tree.</summary>
	[Parameter]
	public IReadOnlyList<MokaTreeSelectItem<TValue>>? Items { get; set; }

	/// <summary>Placeholder text shown when no value is selected.</summary>
	[Parameter]
	public string? Placeholder { get; set; }

	/// <summary>Label text displayed above the dropdown.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Whether the dropdown includes a search/filter input.</summary>
	[Parameter]
	public bool Searchable { get; set; }

	/// <summary>Whether multiple items can be selected simultaneously.</summary>
	[Parameter]
	public bool Multiple { get; set; }

	/// <summary>The selected values when <see cref="Multiple" /> is true.</summary>
	[Parameter]
	public IReadOnlyList<TValue>? SelectedValues { get; set; }

	/// <summary>Callback invoked when the selected values change in multiple mode.</summary>
	[Parameter]
	public EventCallback<IReadOnlyList<TValue>?> SelectedValuesChanged { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-tree-select";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-tree-select--{SizeToKebab(Size)}")
		.AddClass("moka-tree-select--open", _isOpen)
		.AddClass("moka-tree-select--disabled", Disabled)
		.AddClass("moka-tree-select--multiple", Multiple)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle(Style)
		.Build();

	// Id goes on the trigger button, the control, and the label's id is built from it.
	private string TriggerId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	private string? LabelId => Label is null ? null : MokaFieldWrapper.LabelIdFor(TriggerId);

	// A button's name replaces its content, so the label alone would hide the current choice from
	// screen readers. The button's own id after the label's adds it: "Department Engineering".
	private string? LabelledBy => Label is null ? null : $"{LabelId} {TriggerId}";

	// With a label, the label names the button and a consumer aria-label is left out, as on MokaSelect.
	private IReadOnlyDictionary<string, object>? TriggerAttributes =>
		Label is null ? AdditionalAttributes : MokaAttributes.Without(AdditionalAttributes, "aria-label");

	// The root also holds the label and the dropdown. The trigger draws the field's border, so it
	// takes the padding and the radius.
	private string? TriggerStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private string DisplayText
	{
		get
		{
			if (Multiple && _selectedValues is { Count: > 0 })
			{
				return $"{_selectedValues.Count} selected";
			}

			if (_value is not null)
			{
				MokaTreeSelectItem<TValue>? item = FindItem(Items, _value);
				return item?.Text ?? _value.ToString() ?? string.Empty;
			}

			return Placeholder ?? string.Empty;
		}
	}

	private bool HasValue => Multiple ? _selectedValues is { Count: > 0 } : _value is not null;

	/// <summary>Tree select has internal open/expand/search state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (!EqualityComparer<TValue>.Default.Equals(Value, _lastValueParameter))
		{
			_lastValueParameter = Value;
			_value = Value;
		}

		if (!ReferenceEquals(SelectedValues, _lastSelectedValuesParameter))
		{
			_lastSelectedValuesParameter = SelectedValues;
			_selectedValues = SelectedValues;
		}
	}

	private void ToggleDropdown()
	{
		if (Disabled)
		{
			return;
		}

		_isOpen = !_isOpen;
		if (!_isOpen)
		{
			_searchText = string.Empty;
		}
	}

	private void CloseDropdown()
	{
		_isOpen = false;
		_searchText = string.Empty;
	}

	private async Task SelectItemAsync(MokaTreeSelectItem<TValue> item)
	{
		if (item.Disabled)
		{
			return;
		}

		if (Multiple)
		{
			List<TValue> current = _selectedValues?.ToList() ?? [];
			if (current.Any(v => EqualityComparer<TValue>.Default.Equals(v, item.Value)))
			{
				current.RemoveAll(v => EqualityComparer<TValue>.Default.Equals(v, item.Value));
			}
			else
			{
				current.Add(item.Value);
			}

			_selectedValues = current;
			await SelectedValuesChanged.InvokeAsync(_selectedValues);
		}
		else
		{
			_value = item.Value;
			await ValueChanged.InvokeAsync(_value);
			CloseDropdown();
		}
	}

	private void ToggleExpand(int hashCode)
	{
		if (!_expandedNodes.Remove(hashCode))
		{
			_expandedNodes.Add(hashCode);
		}
	}

	private bool IsExpanded(int hashCode) => _expandedNodes.Contains(hashCode);

	private bool IsSelected(TValue value)
	{
		if (Multiple)
		{
			return _selectedValues?.Any(v => EqualityComparer<TValue>.Default.Equals(v, value)) ?? false;
		}

		return EqualityComparer<TValue>.Default.Equals(_value, value);
	}

	private bool MatchesSearch(MokaTreeSelectItem<TValue> item)
	{
		if (string.IsNullOrWhiteSpace(_searchText))
		{
			return true;
		}

		if (item.Text.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return item.Children?.Any(MatchesSearch) ?? false;
	}

	private static MokaTreeSelectItem<TValue>? FindItem(IReadOnlyList<MokaTreeSelectItem<TValue>>? items, TValue value)
	{
		if (items is null)
		{
			return null;
		}

		foreach (MokaTreeSelectItem<TValue> item in items)
		{
			if (EqualityComparer<TValue>.Default.Equals(item.Value, value))
			{
				return item;
			}

			if (item.HasChildren)
			{
				MokaTreeSelectItem<TValue>? found = FindItem(item.Children, value);
				if (found is not null)
				{
					return found;
				}
			}
		}

		return null;
	}

	private void OnSearchInput(ChangeEventArgs e) => _searchText = e.Value?.ToString() ?? string.Empty;

	private ElementReference _triggerRef;

	// Escape closes the popup and puts focus back on the field, since the control that had it may
	// have gone with the popup. While open, keys stop at the picker (see the markup), so a MokaDialog
	// around it does not close on the same Escape.
	private async Task HandlePickerKeyDown(KeyboardEventArgs e)
	{
		if (e.Key != "Escape" || !_isOpen)
		{
			return;
		}

		CloseDropdown();
		await FocusTriggerAsync();
	}

	private async Task FocusTriggerAsync()
	{
		try
		{
			await _triggerRef.FocusAsync();
		}
		catch (JSDisconnectedException)
		{
			// Circuit gone.
		}
		catch (InvalidOperationException)
		{
			// Not interactive, or the element is gone.
		}
	}
}
