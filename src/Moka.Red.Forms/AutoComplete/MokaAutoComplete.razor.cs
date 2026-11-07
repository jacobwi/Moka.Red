using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.AutoComplete;

/// <summary>
///     A search-as-you-type dropdown component. Generic - works with any item type.
///     Calls <see cref="SearchFunc" /> on every keystroke (debounced) and displays matching results.
/// </summary>
/// <typeparam name="TItem">The type of items returned by the search function.</typeparam>
public partial class MokaAutoComplete<TItem> : MokaVisualComponentBase
{
	private TItem? _value;
	private TItem? _lastValueParameter;
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	// Enter picks the highlighted suggestion, so it must not also submit a surrounding form, and
	// the arrows move the highlight rather than the caret. With nothing highlighted Enter is left
	// alone and submits as usual.
	private static readonly Dictionary<string, object?>[] KeyRules =
	[
		new() { ["selector"] = "input[role=combobox]", ["keys"] = new[] { "Enter" }, ["when"] = ".moka-autocomplete-option--focused" },
		new() { ["selector"] = "input[role=combobox]", ["keys"] = new[] { "ArrowDown", "ArrowUp" }, ["when"] = ".moka-autocomplete-dropdown" }
	];

	private readonly string _generatedId = $"moka-autocomplete-{Guid.NewGuid():N}";
	private ElementReference _backdrop;
	private ElementReference _panel;
	private bool _panelShown;
	private ElementReference _root;
	private Timer? _debounceTimer;
	private bool _disposed;
	private int _focusedIndex = -1;
	private bool _isLoading;
	private bool _isOpen;
	private List<TItem> _items = [];
	private int _searchCount;
	private string _searchText = string.Empty;

	// Id goes on the input, not a wrapper, and the label, listbox and option ids are built from it.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	/// <summary>The currently selected value. Two-way bindable.</summary>
	[Parameter]
	public TItem? Value { get; set; }

	/// <summary>Callback invoked when <see cref="Value" /> changes.</summary>
	[Parameter]
	public EventCallback<TItem?> ValueChanged { get; set; }

	/// <summary>
	///     Required. Called on every keystroke (debounced) to fetch search results.
	/// </summary>
	[Parameter]
	[EditorRequired]
	public Func<string, Task<IEnumerable<TItem>>> SearchFunc { get; set; } = default!;

	/// <summary>Custom template for rendering each item in the dropdown.</summary>
	[Parameter]
	public RenderFragment<TItem>? ItemTemplate { get; set; }

	/// <summary>Converts an item to display text. Default: ToString().</summary>
	[Parameter]
	public Func<TItem, string>? ToStringFunc { get; set; }

	/// <summary>Label text displayed above the input.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the input.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text displayed below the input when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Whether the field is required.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Placeholder text for the search input.</summary>
	[Parameter]
	public string Placeholder { get; set; } = "Search...";

	/// <summary>Debounce delay in milliseconds. Default 300.</summary>
	[Parameter]
	public int Debounce { get; set; } = 300;

	/// <summary>Minimum characters before search fires. Default 1.</summary>
	[Parameter]
	public int MinLength { get; set; } = 1;

	/// <summary>Maximum items shown in dropdown. Default 10.</summary>
	[Parameter]
	public int MaxItems { get; set; } = 10;

	/// <summary>Whether to show a clear button. Default true.</summary>
	[Parameter]
	public bool Clearable { get; set; } = true;

	/// <summary>Text shown when search returns no results.</summary>
	[Parameter]
	public string NoResultsText { get; set; } = "No results found";

	/// <summary>Text shown while search is loading.</summary>
	[Parameter]
	public string LoadingText { get; set; } = "Loading...";

	/// <summary>Whether the input is read-only.</summary>
	[Parameter]
	public bool ReadOnly { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-autocomplete";

	private bool HasError => !string.IsNullOrEmpty(ErrorText);

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-autocomplete--error", HasError)
		.AddClass("moka-autocomplete--open", _isOpen)
		.AddClass(Class)
		.Build();

	// The field wrapper is the outermost element, so the margin goes there. The input draws the
	// field's border, so it takes the padding and the radius.

	/// <inheritdoc />
	protected override string? CssStyle => Style;

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string? InputStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private string ListboxId => $"{InputId}-listbox";

	private string OptionId(int index) => $"{ListboxId}-option-{index}";

	// Focus stays in the input while the arrows move the highlight, so this is how a screen
	// reader learns which suggestion is highlighted.
	private string? ActiveOptionId => _isOpen && _focusedIndex >= 0 && _focusedIndex < _items.Count
		? OptionId(_focusedIndex)
		: null;

	private string InputCssClass => new CssBuilder("moka-autocomplete-input")
		.AddClass($"moka-autocomplete-input--{SizeToKebab(Size)}")
		.AddClass("moka-autocomplete-input--has-clear", Clearable && _value is not null)
		.Build();

	/// <summary>AutoComplete has internal state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await SafeModuleInvokeVoidAsync(KeysModule, "preventKeys", _root, KeyRules);
		}

		// The suggestions go to the top layer, out of reach of an ancestor's overflow and of later stacking contexts.
		if (_panelShown != _isOpen)
		{
			_panelShown = _isOpen;
			if (_isOpen)
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "open", _generatedId, _panel, _root, _backdrop,
					new Dictionary<string, object?> { ["placement"] = "bottom-start", ["width"] = "match" });
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", _generatedId);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Value replaces the selection, and the text in the box, only when the parent passes a new
		// one. A re-render that passes the old one again must not undo a pick or wipe what the user
		// is typing (gotcha #9).
		if (!EqualityComparer<TItem>.Default.Equals(Value, _lastValueParameter))
		{
			_lastValueParameter = Value;
			_value = Value;
			_searchText = Value is null ? string.Empty : GetDisplayText(Value);
		}
	}

	private string GetDisplayText(TItem item) => ToStringFunc?.Invoke(item) ?? item?.ToString() ?? string.Empty;

	private void HandleInput(ChangeEventArgs e)
	{
		if (ReadOnly || Disabled)
		{
			return;
		}

		_searchText = e.Value?.ToString() ?? string.Empty;
		_focusedIndex = -1;
		StopSearching();

		if (_searchText.Length < MinLength)
		{
			_isOpen = false;
			_items = [];
			return;
		}

		// Debounce the search
		_isLoading = true;
		_isOpen = true;

		int search = _searchCount;
		_debounceTimer = new Timer(
			_ => RunDetached(() => ExecuteSearch(search)),
			null,
			Debounce,
			Timeout.Infinite);
	}

	// Answers can land in any order, and a slow answer to "ab" used to replace the answer to "abc", so a search
	// checks it's still the latest after waiting. What a replaced one does after that, failing included, no
	// longer matters.
	private async Task ExecuteSearch(int search)
	{
		if (_disposed || search != _searchCount)
		{
			return;
		}

		try
		{
			IEnumerable<TItem> results = await SearchFunc(_searchText);
			if (search == _searchCount)
			{
				_items = results.Take(MaxItems).ToList();
			}
		}
		catch (OperationCanceledException) when (search == _searchCount)
		{
			_items = [];
		}
#pragma warning disable CA1031 // A replaced search's outcome no longer matters, whatever it is.
		catch (Exception) when (search != _searchCount)
#pragma warning restore CA1031
		{
		}
		finally
		{
			if (search == _searchCount)
			{
				_isLoading = false;
				if (!_disposed)
				{
					StateHasChanged();
				}
			}
		}
	}

	// Without this a search waiting on its debounce would still run after a pick or a clear, and a search already
	// running would fill the list again behind the closed popup.
	private void StopSearching()
	{
		_searchCount++;
		_debounceTimer?.Dispose();
		_debounceTimer = null;
		_isLoading = false;
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		switch (e.Key)
		{
			case "ArrowDown":
				if (!_isOpen && _searchText.Length >= MinLength)
				{
					_isOpen = true;
				}
				else if (_items.Count > 0)
				{
					_focusedIndex = (_focusedIndex + 1) % _items.Count;
				}

				break;

			case "ArrowUp":
				if (_items.Count > 0 && _focusedIndex > 0)
				{
					_focusedIndex--;
				}

				break;

			case "Enter":
				if (_focusedIndex >= 0 && _focusedIndex < _items.Count)
				{
					await SelectItem(_items[_focusedIndex]);
				}

				break;

			case "Escape":
				_isOpen = false;
				_focusedIndex = -1;
				break;
		}
	}

	private async Task SelectItem(TItem item)
	{
		StopSearching();
		_value = item;
		_searchText = GetDisplayText(item);
		_isOpen = false;
		_focusedIndex = -1;
		_items = [];
		if (ValueChanged.HasDelegate)
		{
			await ValueChanged.InvokeAsync(_value);
		}
	}

	private async Task HandleClear()
	{
		StopSearching();
		_value = default;
		_searchText = string.Empty;
		_isOpen = false;
		_focusedIndex = -1;
		_items = [];
		if (ValueChanged.HasDelegate)
		{
			await ValueChanged.InvokeAsync(default);
		}
	}

	private void HandleFocus()
	{
		if (!ReadOnly && !Disabled && _searchText.Length >= MinLength && _items.Count > 0)
		{
			_isOpen = true;
		}
	}

	private void HandleBackdropClick()
	{
		_isOpen = false;
		_focusedIndex = -1;

		// If no selection, restore display text
		if (_value is not null)
		{
			_searchText = GetDisplayText(_value);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_disposed = true;

		if (_debounceTimer is not null)
		{
			await _debounceTimer.DisposeAsync();
			_debounceTimer = null;
		}

		if (_panelShown)
		{
			_panelShown = false;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _generatedId);
		}

		await base.DisposeAsyncCore();
	}
}
