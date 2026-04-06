using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.SelectField;

/// <summary>
///     A dropdown select component with optional search filtering, custom item templates,
///     keyboard navigation, multiple selection, grouping, clear button, and loading state.
/// </summary>
/// <typeparam name="TValue">The type of the selected value.</typeparam>
public partial class MokaSelect<TValue>
{
	private readonly string _inputId = $"moka-select-{Guid.NewGuid():N}";
	private List<TValue> _flatItems = [];
	private int _focusedIndex = -1;
	private ElementReference _searchInputRef;
	private string _searchText = string.Empty;

	/// <summary>Label text displayed above the select.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the select.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text displayed below the select when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Whether the field is required.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Converts an item to a display string. Default: ToString().</summary>
	[Parameter]
	public Func<TValue, string>? ValueSelector { get; set; }

	/// <summary>Whether the dropdown includes a search input. Default false.</summary>
	[Parameter]
	public bool Searchable { get; set; }

	/// <summary>Whether the selection can be cleared. Default false.</summary>
	[Parameter]
	public bool Clearable { get; set; }

	/// <summary>Whether multiple items can be selected. Default false.</summary>
	[Parameter]
	public bool Multiple { get; set; }

	/// <summary>The collection of selected values when <see cref="Multiple" /> is true.</summary>
	[Parameter]
#pragma warning disable CA2227 // Blazor two-way binding requires setter
	public IList<TValue>? SelectedValues { get; set; }
#pragma warning restore CA2227

	/// <summary>Callback when <see cref="SelectedValues" /> changes.</summary>
	[Parameter]
	public EventCallback<IList<TValue>> SelectedValuesChanged { get; set; }

	/// <summary>Whether the dropdown is in a loading state. Default false.</summary>
	[Parameter]
	public bool Loading { get; set; }

	/// <summary>Text displayed when search yields no matches.</summary>
	[Parameter]
	public string NoResultsText { get; set; } = "No options found";

	/// <summary>Groups options by the returned string. Options with the same group are grouped under a header.</summary>
	[Parameter]
	public Func<TValue, string?>? GroupBy { get; set; }

	/// <summary>Determines whether an individual option is disabled.</summary>
	[Parameter]
	public Func<TValue, bool>? IsOptionDisabled { get; set; }

	/// <summary>Custom template for rendering chips in multi-select mode.</summary>
	[Parameter]
	public RenderFragment<TValue>? ChipTemplate { get; set; }

	/// <summary>Show a "Select All" checkbox at the top of the dropdown when <see cref="Multiple" /> is true.</summary>
	[Parameter]
	public bool SelectAll { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-select";

	private bool HasError => !string.IsNullOrEmpty(ErrorText);

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-select--error", HasError)
		.AddClass("moka-select--open", IsOpen)
		.AddClass("moka-select--multiple", Multiple)
		.AddClass(Class)
		.Build();

	private string? ComputedStyle => Style;

	private string TriggerCssClass => new CssBuilder()
		.AddClass($"moka-select-trigger--{SizeToKebab(Size)}")
		.Build();

	/// <summary>Items filtered by the current search text.</summary>
	private IEnumerable<TValue> FilteredItems
	{
		get
		{
			if (!Searchable || string.IsNullOrWhiteSpace(_searchText))
			{
				return Items;
			}

			return Items.Where(item =>
				GetDisplayText(item).Contains(_searchText, StringComparison.OrdinalIgnoreCase));
		}
	}

	/// <summary>Items grouped by the GroupBy function.</summary>
	private IEnumerable<IGrouping<string?, TValue>> GroupedItems
	{
		get
		{
			IEnumerable<TValue> filtered = FilteredItems;
			_flatItems = filtered.ToList();
			return filtered.GroupBy(item => GroupBy?.Invoke(item));
		}
	}

	private bool AllSelected => Multiple && SelectedValues is not null && Items is not null
	                            && Items.All(item =>
		                            SelectedValues.Any(v => EqualityComparer<TValue>.Default.Equals(v, item)));

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		// Allow usage without EditForm by providing a default ValueExpression
		if (!parameters.TryGetValue<Expression<Func<TValue>>>(nameof(ValueExpression), out _))
		{
			ValueExpression = () => Value!;
		}

		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
	{
		// Selection is by object reference, not string parsing.
		result = default!;
		validationErrorMessage = string.Empty;
		return false;
	}

	private string GetDisplayText(TValue item) => ValueSelector?.Invoke(item) ?? item?.ToString() ?? string.Empty;

	private bool IsSelected(TValue item)
	{
		if (Multiple && SelectedValues is not null)
		{
			return SelectedValues.Any(v => EqualityComparer<TValue>.Default.Equals(v, item));
		}

		return EqualityComparer<TValue>.Default.Equals(item, CurrentValue);
	}

	private bool IsItemDisabled(TValue item) => IsOptionDisabled?.Invoke(item) ?? false;

	private string OptionCssClass(TValue item, int index) => new CssBuilder()
		.AddClass("moka-select-option--selected", IsSelected(item))
		.AddClass("moka-select-option--focused", index == _focusedIndex)
		.AddClass("moka-select-option--disabled", IsItemDisabled(item))
		.Build();

	private async Task HandleTriggerClick()
	{
		await ToggleAsync();
		_searchText = string.Empty;
		_focusedIndex = -1;
	}

	private async Task HandleBackdropClick()
	{
		await CloseAsync();
		_searchText = string.Empty;
		_focusedIndex = -1;
	}

	private async Task HandleSelectItem(TValue item)
	{
		if (IsItemDisabled(item))
		{
			return;
		}

		if (Multiple)
		{
			SelectedValues ??= new List<TValue>();

			if (SelectedValues.Any(v => EqualityComparer<TValue>.Default.Equals(v, item)))
			{
				TValue toRemove = SelectedValues.First(v => EqualityComparer<TValue>.Default.Equals(v, item));
				SelectedValues.Remove(toRemove);
			}
			else
			{
				SelectedValues.Add(item);
			}

			await SelectedValuesChanged.InvokeAsync(SelectedValues);
			// Don't close dropdown in multiple mode
		}
		else
		{
			await SelectItemAsync(item);
			_searchText = string.Empty;
			_focusedIndex = -1;
		}
	}

	private async Task HandleSelectAll()
	{
		if (SelectedValues is null || Items is null)
		{
			return;
		}

		if (AllSelected)
		{
			SelectedValues.Clear();
		}
		else
		{
			SelectedValues.Clear();
			foreach (TValue item in Items)
			{
				if (IsOptionDisabled?.Invoke(item) != true)
				{
					SelectedValues.Add(item);
				}
			}
		}

		await SelectedValuesChanged.InvokeAsync(SelectedValues);
	}

	private async Task RemoveSelectedItem(TValue item)
	{
		if (SelectedValues is null)
		{
			return;
		}

		TValue? toRemove = SelectedValues.FirstOrDefault(v => EqualityComparer<TValue>.Default.Equals(v, item));
		if (toRemove is not null)
		{
			SelectedValues.Remove(toRemove);
			await SelectedValuesChanged.InvokeAsync(SelectedValues);
		}
	}

	private async Task HandleClear()
	{
		if (Multiple)
		{
			SelectedValues?.Clear();
			await SelectedValuesChanged.InvokeAsync(SelectedValues);
		}
		else
		{
			CurrentValue = default!;
			await ValueChanged.InvokeAsync(CurrentValue);
		}
	}

	private void HandleSearch(ChangeEventArgs e)
	{
		_searchText = e.Value?.ToString() ?? string.Empty;
		_focusedIndex = -1;
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		switch (e.Key)
		{
			case "Enter":
			case " ":
				if (!IsOpen)
				{
					await OpenAsync();
					_focusedIndex = -1;
				}
				else
				{
					await SelectFocusedItem();
				}

				break;

			case "Escape":
				if (IsOpen)
				{
					await CloseAsync();
					_searchText = string.Empty;
					_focusedIndex = -1;
				}

				break;

			case "ArrowDown":
				if (!IsOpen)
				{
					await OpenAsync();
					_focusedIndex = 0;
				}
				else
				{
					MoveFocus(1);
				}

				break;

			case "ArrowUp":
				if (IsOpen)
				{
					MoveFocus(-1);
				}

				break;

			case "Tab":
				if (IsOpen)
				{
					await CloseAsync();
					_searchText = string.Empty;
					_focusedIndex = -1;
				}

				break;
		}
	}

	private async Task HandleDropdownKeyDown(KeyboardEventArgs e)
	{
		switch (e.Key)
		{
			case "ArrowDown":
				MoveFocus(1);
				break;
			case "ArrowUp":
				MoveFocus(-1);
				break;
			case "Enter":
				await SelectFocusedItem();
				break;
			case "Escape":
				await CloseAsync();
				_searchText = string.Empty;
				_focusedIndex = -1;
				break;
		}
	}

	private void MoveFocus(int direction)
	{
		var items = FilteredItems.ToList();
		if (items.Count == 0)
		{
			return;
		}

		int newIndex = _focusedIndex + direction;
		// Skip disabled items
		while (newIndex >= 0 && newIndex < items.Count && IsItemDisabled(items[newIndex]))
		{
			newIndex += direction;
		}

		if (newIndex >= 0 && newIndex < items.Count)
		{
			_focusedIndex = newIndex;
		}
	}

	private async Task SelectFocusedItem()
	{
		var items = FilteredItems.ToList();
		if (_focusedIndex >= 0 && _focusedIndex < items.Count)
		{
			await HandleSelectItem(items[_focusedIndex]);
		}
	}
}
