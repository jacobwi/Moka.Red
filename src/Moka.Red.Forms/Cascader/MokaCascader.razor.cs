using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Base;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.Cascader;

/// <summary>
///     A drill-down selector over a tree of options: each choice opens the column of its children,
///     and the value is the path from the root to what was chosen, shown as a breadcrumb in the
///     trigger.
/// </summary>
public partial class MokaCascader : MokaVisualInputBase<IReadOnlyList<string>>
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";
	private const string HoverModule = "./_content/Moka.Red.Core/moka-hover.js";
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	// Keys the handler below consumes. Without this the browser acts on them as well: Space and
	// the arrows scroll the page, and Enter submits a surrounding form. Dictionaries rather than
	// anonymous types, which trimming can strip in WebAssembly apps.
	private static readonly Dictionary<string, object?>[] KeyRules =
	[
		new() { ["selector"] = ".moka-cascader-trigger", ["keys"] = new[] { " ", "Enter", "ArrowDown", "ArrowUp", "ArrowLeft", "ArrowRight" } }
	];

	private readonly string _generatedId = $"moka-cascader-{Guid.NewGuid():N}";
	private ElementReference _backdrop;
	private DotNetObjectReference<MokaCascader>? _dotNetRef;
	private ElementReference _panelRef;
	private bool _panelShown;
	private ElementReference _root;
	private bool _wasOpen;

	// The drill-down: the options whose children each column shows, root first.
	private readonly List<MokaCascaderOption> _expanded = [];

	// The mirror of the Value parameter, so a re-render passing the same path does not have to
	// walk the tree again.
	private IReadOnlyList<string> _selected = [];

	// The keyboard highlight: which column it is in, and which option of that column.
	private int _column;
	private int _focusedIndex = -1;
	private bool _lastIsOpenParameter;

	/// <summary>The tree of options to choose from.</summary>
	[Parameter]
	public IReadOnlyList<MokaCascaderOption>? Options { get; set; }

	/// <summary>Whether the dropdown is open. Two-way bindable. The dropdown still opens and closes
	/// itself; a value from the parent only takes effect when it differs from the last one passed.</summary>
	[Parameter]
	public bool IsOpen { get; set; }

	/// <summary>Callback invoked when the dropdown opens or closes.</summary>
	[Parameter]
	public EventCallback<bool> IsOpenChanged { get; set; }

	/// <summary>Label text displayed above the field.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the field.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text displayed below the field when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Whether the field is required.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Text shown in the trigger before anything is chosen. Defaults to "Select...".</summary>
	[Parameter]
	public string? Placeholder { get; set; }

	/// <summary>Whether the selection can be cleared. Defaults to false.</summary>
	[Parameter]
	public bool Clearable { get; set; }

	/// <summary>
	///     Whether choosing a node with children selects its path right away, instead of waiting
	///     for a leaf. Defaults to false.
	/// </summary>
	[Parameter]
	public bool ChangeOnSelect { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-cascader";

	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	private string PanelId => $"{InputId}-panel";

	private int? TriggerTabIndex => Disabled ? null : 0;

	private bool DropdownOpen { get; set; }

	// ErrorText is the explicit override; without one, fall back to whatever the cascaded
	// EditContext reports, so DataAnnotations messages are actually visible.
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-cascader--error", HasError)
		.AddClass("moka-cascader--open", DropdownOpen)
		.AddClass("moka-cascader--disabled", Disabled)
		.AddClass(Class)
		.Build();

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string? TriggerStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private string TriggerCssClass => new CssBuilder("moka-cascader-trigger")
		.AddClass($"moka-cascader-trigger--{SizeToKebab(Size)}")
		.Build();

	private string? LabelledBy => Label is null ? null : MokaFieldWrapper.LabelIdFor(InputId);

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

	private IReadOnlyDictionary<string, object>? TriggerAttributes =>
		MokaAttributes.Without(AdditionalAttributes, "aria-label");

	private IReadOnlyList<MokaCascaderOption> RootOptions => Options ?? [];

	// The columns the panel shows: the root options, then the children of every expanded option.
	private List<IReadOnlyList<MokaCascaderOption>> Columns
	{
		get
		{
			List<IReadOnlyList<MokaCascaderOption>> columns = [RootOptions];
			foreach (MokaCascaderOption option in _expanded)
			{
				if (option.Children is { Count: > 0 } children)
				{
					columns.Add(children);
				}
			}

			return columns;
		}
	}

	private string ChevronCssClass => new CssBuilder("moka-cascader-chevron")
		.AddClass("moka-cascader-chevron--open", DropdownOpen)
		.Build();

	private IReadOnlyList<string> SelectedLabels
	{
		get
		{
			List<string> labels = [];
			IReadOnlyList<MokaCascaderOption> level = RootOptions;
			foreach (string value in _selected)
			{
				MokaCascaderOption? match = null;
				foreach (MokaCascaderOption option in level)
				{
					if (string.Equals(option.Value, value, StringComparison.Ordinal))
					{
						match = option;
						break;
					}
				}

				if (match is null)
				{
					return labels;
				}

				labels.Add(match.Text);
				level = match.Children ?? [];
			}

			return labels;
		}
	}

	private bool HasSelection => _selected.Count > 0;

	/// <summary>Allows usage without EditForm by providing a default ValueExpression.</summary>
	public override Task SetParametersAsync(ParameterView parameters)
	{
		if (!parameters.TryGetValue<Expression<Func<IReadOnlyList<string>>>>(nameof(ValueExpression), out _))
		{
			ValueExpression = () => Value!;
		}

		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (IsOpen != _lastIsOpenParameter)
		{
			_lastIsOpenParameter = IsOpen;
			DropdownOpen = IsOpen && !Disabled;
		}

		if (Value is not null
		    && (Value.Count != _selected.Count || !Value.SequenceEqual(_selected)))
		{
			_selected = Value;
		}
	}

	private static string PostedName(string field, int index) =>
		string.Create(CultureInfo.InvariantCulture, $"{field}[{index}]");

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out IReadOnlyList<string> result,
		out string validationErrorMessage)
	{
		// Selection is by picking options, not by parsing text.
		result = [];
		validationErrorMessage = "Selection is made by picking options, not by parsing text.";
		return false;
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

		// The panel goes to the top layer, out of reach of an ancestor's overflow and of later stacking contexts.
		if (_panelShown != DropdownOpen)
		{
			_panelShown = DropdownOpen;
			if (DropdownOpen)
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "open", _generatedId, _panelRef, _root, _backdrop,
					new Dictionary<string, object?> { ["placement"] = "bottom-start", ["width"] = "min" });
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", _generatedId);
			}
		}

		if (DropdownOpen && !_wasOpen)
		{
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await SafeModuleInvokeVoidAsync(HoverModule, "trackHover", _panelRef, _dotNetRef);
		}

		_wasOpen = DropdownOpen;
	}

	/// <summary>Called from JS just before a key press, with the option the mouse is on in the current column.</summary>
	/// <param name="index">The option's position in the column.</param>
	[JSInvokable]
	public void OnItemHovered(int index) => _focusedIndex = index;

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_dotNetRef?.Dispose();
		if (_panelShown)
		{
			_panelShown = false;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _generatedId);
		}

		await base.DisposeAsyncCore();
	}

	private async Task OpenAsync()
	{
		if (Disabled || DropdownOpen)
		{
			return;
		}

		DropdownOpen = true;
		ExpandToSelection();
		_column = Columns.Count - 1;
		_focusedIndex = -1;
		await IsOpenChanged.InvokeAsync(true);
	}

	private async Task CloseAsync()
	{
		if (!DropdownOpen)
		{
			return;
		}

		DropdownOpen = false;
		await IsOpenChanged.InvokeAsync(false);
	}

	// Opening a field whose value was set from code drills straight down to that path, so the
	// chosen node is visible without walking there again.
	private void ExpandToSelection()
	{
		_expanded.Clear();
		IReadOnlyList<MokaCascaderOption> level = RootOptions;
		foreach (string value in _selected)
		{
			MokaCascaderOption? match = null;
			foreach (MokaCascaderOption option in level)
			{
				if (string.Equals(option.Value, value, StringComparison.Ordinal))
				{
					match = option;
					break;
				}
			}

			if (match?.Children is not { Count: > 0 } children)
			{
				return;
			}

			_expanded.Add(match);
			level = children;
		}
	}

	private bool IsExpanded(int column, MokaCascaderOption option) =>
		column < _expanded.Count && ReferenceEquals(_expanded[column], option);

	private string OptionCssClass(int column, MokaCascaderOption option, int index) => new CssBuilder("moka-cascader-option")
		.AddClass("moka-cascader-option--expanded", IsExpanded(column, option))
		.AddClass("moka-cascader-option--focused", column == _column && index == _focusedIndex)
		.AddClass("moka-cascader-option--disabled", option.Disabled)
		.Build();

	private async Task HandleTriggerClick()
	{
		if (DropdownOpen)
		{
			await CloseAsync();
		}
		else
		{
			await OpenAsync();
		}
	}

	private async Task HandleBackdropClick() => await CloseAsync();

	private async Task HandleClickOption(int column, MokaCascaderOption option)
	{
		if (option.Disabled)
		{
			return;
		}

		if (option.Children is { Count: > 0 })
		{
			if (ChangeOnSelect)
			{
				await SelectAsync(column, option);
				await CloseAsync();
				return;
			}

			_expanded.RemoveRange(column, _expanded.Count - column);
			_expanded.Add(option);
			_column = column + 1;
			_focusedIndex = -1;
			return;
		}

		await SelectAsync(column, option);
		await CloseAsync();
	}

	// The path the chosen option completes: everything expanded up to its column, then the option.
	private async Task SelectAsync(int column, MokaCascaderOption option)
	{
		List<string> path = [.. _expanded.Take(column).Select(o => o.Value), option.Value];
		_selected = path;
		CurrentValue = path;
	}

	private async Task HandleClear()
	{
		_selected = [];
		CurrentValue = [];
		await CloseAsync();
	}

	// The keys the handlers below consume; the browser is kept from acting on them by the
	// preventKeys rule set up on first render.
	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		switch (e.Key)
		{
			case "Enter" or " " when !DropdownOpen:
				await OpenAsync();
				break;

			case "Escape" or "Tab" when DropdownOpen:
				await CloseAsync();
				break;

			case "ArrowDown" when !DropdownOpen:
				await OpenAsync();
				MoveFocus(1);
				break;

			case "ArrowDown" when DropdownOpen:
				MoveFocus(1);
				break;

			case "ArrowUp":
				MoveFocus(-1);
				break;

			case "ArrowRight":
				ExpandFocused();
				break;

			case "ArrowLeft":
				if (_column > 0)
				{
					_column--;
					_expanded.RemoveRange(_column, _expanded.Count - _column);
					_focusedIndex = -1;
				}

				break;

			case "Enter" or " ":
				await ActivateFocused();
				break;
		}
	}

	private void MoveFocus(int direction)
	{
		if (_column >= Columns.Count)
		{
			return;
		}

		IReadOnlyList<MokaCascaderOption> column = Columns[_column];
		if (column.Count == 0)
		{
			return;
		}

		int newIndex = _focusedIndex + direction;
		while (newIndex >= 0 && newIndex < column.Count && column[newIndex].Disabled)
		{
			newIndex += direction;
		}

		if (newIndex >= 0 && newIndex < column.Count)
		{
			_focusedIndex = newIndex;
		}
	}

	private void ExpandFocused()
	{
		if (_column >= Columns.Count || _focusedIndex < 0)
		{
			return;
		}

		IReadOnlyList<MokaCascaderOption> column = Columns[_column];
		if (_focusedIndex >= column.Count)
		{
			return;
		}

		MokaCascaderOption option = column[_focusedIndex];
		if (option.Disabled || option.Children is not { Count: > 0 })
		{
			return;
		}

		_expanded.RemoveRange(_column, _expanded.Count - _column);
		_expanded.Add(option);
		_column++;
		_focusedIndex = -1;
	}

	private async Task ActivateFocused()
	{
		if (_column >= Columns.Count || _focusedIndex < 0 || _focusedIndex >= Columns[_column].Count)
		{
			return;
		}

		await HandleClickOption(_column, Columns[_column][_focusedIndex]);
	}
}
