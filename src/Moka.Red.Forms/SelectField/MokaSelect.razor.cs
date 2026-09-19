using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.SelectField;

/// <summary>
///     A dropdown select component with optional search filtering, custom item templates,
///     keyboard navigation, multiple selection, grouping, clear button, and loading state.
/// </summary>
/// <typeparam name="TValue">The type of the selected value.</typeparam>
public partial class MokaSelect<TValue>
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	// Keys the handlers below consume. Without this the browser acts on them as well: Space and
	// the arrows scroll the page, and Enter in the search box submits a surrounding form.
	// Dictionaries rather than anonymous types, which trimming can strip in WebAssembly apps.
	private static readonly Dictionary<string, object?>[] KeyRules =
	[
		new() { ["selector"] = ".moka-select-trigger", ["keys"] = new[] { " ", "ArrowDown", "ArrowUp" } },
		new() { ["selector"] = ".moka-select-search", ["keys"] = new[] { "Enter", "ArrowDown", "ArrowUp" } }
	];

	private readonly string _inputId = $"moka-select-{Guid.NewGuid():N}";
	private int _focusedIndex = -1;
	private ElementReference _root;
	private ElementReference _triggerRef;
	private ElementReference _searchInputRef;
	private string _searchText = string.Empty;
	private bool _wasOpen;
	private bool _refocusTrigger;
	private bool _innerKey;

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

	private string ListboxId => $"{_inputId}-listbox";

	private int? TriggerTabIndex => Disabled ? null : 0;

	private string OptionId(int index) => $"{ListboxId}-option-{index}";

	// Focus stays on the trigger (or the search box) while the arrows move the highlight, so this
	// is how a screen reader learns which option is highlighted.
	private string? ActiveOptionId => IsOpen && _focusedIndex >= 0 ? OptionId(_focusedIndex) : null;

	// The trigger is a div, which <label for> cannot name, so it is labelled by the wrapper's
	// label id. Without a visible label it falls back to an aria-label the consumer passed
	// (captured but not otherwise rendered), then to the placeholder.
	private string? LabelledBy => Label is null ? null : MokaFieldWrapper.LabelIdFor(_inputId);

	private string? AccessibleName
	{
		get
		{
			if (Label is not null)
			{
				return null;
			}

			if (AdditionalAttributes?.TryGetValue("aria-label", out object? ariaLabel) == true
			    && ariaLabel is string name && !string.IsNullOrWhiteSpace(name))
			{
				return name;
			}

			return Placeholder ?? "Select";
		}
	}

	// ErrorText is the explicit override; without one, fall back to whatever the cascaded
	// EditContext reports, so DataAnnotations messages are actually visible.
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	/// <summary>Explicit <see cref="ErrorText" /> when set, otherwise the EditContext validation message.</summary>
	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-select--error", HasError)
		.AddClass("moka-select--open", IsOpen)
		.AddClass("moka-select--multiple", Multiple)
		.AddClass(CssClass)
		.AddClass(Class)
		.Build();

	private string? ComputedStyle => Style;

	private string TriggerCssClass => new CssBuilder()
		.AddClass($"moka-select-trigger--{SizeToKebab(Size)}")
		.Build();

	// A null value is a real choice when one of the items is null, such as a "use the default"
	// entry. It used to show the placeholder, so that item could never look selected.
	private bool ShowsValue => CurrentValue is not null || Items.Any(item => item is null);

	private string ValueCssClass => new CssBuilder("moka-select-value")
		.AddClass("moka-select-value--placeholder", !ShowsValue)
		.Build();

	private string ChevronCssClass => new CssBuilder("moka-select-chevron")
		.AddClass("moka-select-chevron--open", IsOpen)
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
	private IEnumerable<IGrouping<string?, TValue>> GroupedItems => FilteredItems.GroupBy(item => GroupBy?.Invoke(item));

	// The options in the order they are shown, which is what the arrow keys walk. With GroupBy
	// that is group by group, not the order of Items.
	private List<TValue> DisplayItems => GroupBy is null
		? FilteredItems.ToList()
		: GroupedItems.SelectMany(group => group).ToList();

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
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await SafeModuleInvokeVoidAsync(KeysModule, "preventKeys", _root, KeyRules);
		}

		bool opened = IsOpen && !_wasOpen;
		_wasOpen = IsOpen;

		if (opened)
		{
			_refocusTrigger = false;
			if (Searchable)
			{
				// Typing goes straight to the search box once the list is open.
				await TryFocusAsync(_searchInputRef);
			}
		}
		else if (!IsOpen && _refocusTrigger)
		{
			// Closing took the focused search box out of the page, which drops focus to <body>.
			_refocusTrigger = false;
			await TryFocusAsync(_triggerRef);
		}
	}

	private static async Task TryFocusAsync(ElementReference element)
	{
		try
		{
			await element.FocusAsync();
		}
		catch (JSException)
		{
			// The element left the page before the call landed
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected - nothing to focus
		}
		catch (ObjectDisposedException)
		{
			// JS runtime torn down mid-call
		}
		catch (OperationCanceledException)
		{
			// Covers TaskCanceledException too
		}
	}

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
	{
		// Selection is by object reference, not string parsing.
		result = default!;
		validationErrorMessage = $"Cannot convert '{value}' to {typeof(TValue).Name}: selection is made by object identity, not by parsing a string.";
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
			var next = SelectedValues is null ? [] : new List<TValue>(SelectedValues);

			int existing = next.FindIndex(v => EqualityComparer<TValue>.Default.Equals(v, item));
			if (existing >= 0)
			{
				next.RemoveAt(existing);
			}
			else
			{
				next.Add(item);
			}

			await SelectedValuesChanged.InvokeAsync(next);
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
		if (Items is null)
		{
			return;
		}

		List<TValue> next = [];
		if (!AllSelected)
		{
			foreach (TValue item in Items)
			{
				if (IsOptionDisabled?.Invoke(item) != true)
				{
					next.Add(item);
				}
			}
		}

		await SelectedValuesChanged.InvokeAsync(next);
	}

	private async Task RemoveSelectedItem(TValue item)
	{
		if (SelectedValues is null)
		{
			return;
		}

		var next = new List<TValue>(SelectedValues);
		int index = next.FindIndex(v => EqualityComparer<TValue>.Default.Equals(v, item));
		if (index >= 0)
		{
			next.RemoveAt(index);
			await SelectedValuesChanged.InvokeAsync(next);
		}
	}

	private async Task HandleClear()
	{
		if (Multiple)
		{
			await SelectedValuesChanged.InvokeAsync([]);
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

	// Enter and Space on the clear and chip buttons bubble up to the trigger, which would open the
	// list on top of what the button did. The button's handler runs first and flags the key.
	private void MarkInnerKey(KeyboardEventArgs e) => _innerKey = e.Key is "Enter" or " ";

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (_innerKey)
		{
			_innerKey = false;
			return;
		}

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
					_focusedIndex = -1;
					MoveFocus(1);
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
				_refocusTrigger = true;
				await SelectFocusedItem();
				break;
			case "Escape":
				_refocusTrigger = true;
				await CloseAsync();
				_searchText = string.Empty;
				_focusedIndex = -1;
				break;
			case "Tab":
				// Focus moves on to the next field by itself, and the list closes behind it.
				_refocusTrigger = false;
				await CloseAsync();
				_searchText = string.Empty;
				_focusedIndex = -1;
				break;
		}
	}

	private void MoveFocus(int direction)
	{
		var items = DisplayItems;
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
		var items = DisplayItems;
		if (_focusedIndex >= 0 && _focusedIndex < items.Count)
		{
			await HandleSelectItem(items[_focusedIndex]);
		}
	}
}
