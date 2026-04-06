using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.TreeSelect;

/// <summary>
///     A dropdown that displays a tree hierarchy for selection, suitable for
///     folder pickers, category selectors, and other hierarchical data.
///     Supports single and multiple selection modes with optional search filtering.
/// </summary>
/// <typeparam name="TValue">The type of the selectable value.</typeparam>
public partial class MokaTreeSelect<TValue> : MokaVisualComponentBase
{
	private readonly HashSet<int> _expandedNodes = [];
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
		.AddStyle(Style)
		.Build();

	private string DisplayText
	{
		get
		{
			if (Multiple && SelectedValues is { Count: > 0 })
			{
				return $"{SelectedValues.Count} selected";
			}

			if (Value is not null)
			{
				MokaTreeSelectItem<TValue>? item = FindItem(Items, Value);
				return item?.Text ?? Value.ToString() ?? string.Empty;
			}

			return Placeholder ?? string.Empty;
		}
	}

	private bool HasValue => Multiple ? SelectedValues is { Count: > 0 } : Value is not null;

	/// <summary>Tree select has internal open/expand/search state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

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
			List<TValue> current = SelectedValues?.ToList() ?? [];
			if (current.Any(v => EqualityComparer<TValue>.Default.Equals(v, item.Value)))
			{
				current.RemoveAll(v => EqualityComparer<TValue>.Default.Equals(v, item.Value));
			}
			else
			{
				current.Add(item.Value);
			}

			SelectedValues = current;
			await SelectedValuesChanged.InvokeAsync(SelectedValues);
		}
		else
		{
			Value = item.Value;
			await ValueChanged.InvokeAsync(Value);
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
			return SelectedValues?.Any(v => EqualityComparer<TValue>.Default.Equals(v, value)) ?? false;
		}

		return EqualityComparer<TValue>.Default.Equals(Value, value);
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
}
