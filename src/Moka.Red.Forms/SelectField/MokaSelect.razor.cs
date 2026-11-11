using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Theming;
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
	private const string HoverModule = "./_content/Moka.Red.Core/moka-hover.js";
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	// Keys the handlers below consume. Without this the browser acts on them as well: Space and
	// the arrows scroll the page, and Enter in the search box submits a surrounding form.
	// Dictionaries rather than anonymous types, which trimming can strip in WebAssembly apps.
	private static readonly Dictionary<string, object?>[] KeyRules =
	[
		new() { ["selector"] = ".moka-select-trigger", ["keys"] = new[] { " ", "ArrowDown", "ArrowUp" } },
		new() { ["selector"] = ".moka-select-search", ["keys"] = new[] { "Enter", "ArrowDown", "ArrowUp" } }
	];

	// With TypeToJump, Home and End on the closed field pick the first and last item instead of scrolling the page.
	private static readonly Dictionary<string, object?>[] JumpKeyRules =
	[
		new() { ["selector"] = ".moka-select-trigger", ["keys"] = new[] { " ", "ArrowDown", "ArrowUp", "Home", "End" } },
		new() { ["selector"] = ".moka-select-search", ["keys"] = new[] { "Enter", "ArrowDown", "ArrowUp" } }
	];

	private static readonly TimeSpan TypingPause = TimeSpan.FromSeconds(1);

	private readonly string _generatedId = $"moka-select-{Guid.NewGuid():N}";
	private ElementReference _backdrop;
	private DotNetObjectReference<MokaSelect<TValue>>? _dotNetRef;
	private int _focusedIndex = -1;
	private ElementReference _optionsRef;
	private ElementReference _panel;
	private bool _panelShown;
	private ElementReference _root;
	private ElementReference _triggerRef;
	private ElementReference _searchInputRef;
	private string _searchText = string.Empty;
	private bool _wasOpen;
	private bool _refocusTrigger;
	private bool _innerKey;
	private bool _watchingSide;
	private bool? _boundJumpKeys;
	private string _typed = string.Empty;
	private long _typedAt;
	private string? _jumpAnnouncement;
	private TimeProvider? _clock;

	[CascadingParameter]
	private MokaTheme? CascadedTheme { get; set; }

	// Optional, so looked up rather than injected: [Inject] throws when a service is missing.
	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	private TimeProvider Clock => _clock ??= Services.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System;

	// Id goes on the trigger, not a wrapper, and the label, listbox and option ids are built from it.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

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

	/// <summary>Whether the field can be left empty. Shows a quiet word beside the label. <see cref="Required" /> wins.</summary>
	[Parameter]
	public bool Optional { get; set; }

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

	/// <summary>
	///     Draws the chosen item in the closed box of a single select, the way <c>ItemTemplate</c> draws it in the list,
	///     so an icon stays once it's picked. Without it the box shows the item's text. The placeholder is unaffected.
	/// </summary>
	[Parameter]
	public RenderFragment<TValue>? ValueTemplate { get; set; }

	/// <summary>Show a "Select All" checkbox at the top of the dropdown when <see cref="Multiple" /> is true.</summary>
	[Parameter]
	public bool SelectAll { get; set; }

	/// <summary>
	///     How the rows of the open list are drawn. Null takes the theme's choice (<see cref="MokaTheme.WithLists" />),
	///     which is <see cref="MokaListLook.Standard" /> unless the app set one. The box around the rows follows the theme's
	///     <see cref="MokaTheme.WithPopups" />. Looks belong to the new design: on <see cref="MokaTheme.Dark" /> and
	///     <see cref="MokaTheme.Light" /> the list stays as it is, whatever this says.
	/// </summary>
	[Parameter]
	public MokaListLook? Look { get; set; }

	/// <summary>The word after the arrow keys in the key hints <see cref="MokaListLook.Compact" /> draws. Default "move".</summary>
	[Parameter]
	public string MoveHintText { get; set; } = "move";

	/// <summary>The word after Enter in the key hints <see cref="MokaListLook.Compact" /> draws. Default "pick".</summary>
	[Parameter]
	public string PickHintText { get; set; } = "pick";

	/// <summary>The word after Esc in the key hints <see cref="MokaListLook.Compact" /> draws. Default "close".</summary>
	[Parameter]
	public string CloseHintText { get; set; } = "close";

	/// <summary>
	///     What search matches for each item, when that should be more than the text shown, such as a code or other
	///     names beside a name. Null matches the shown text (<see cref="ValueSelector" />).
	/// </summary>
	[Parameter]
	public Func<TValue, string>? SearchText { get; set; }

	/// <summary>
	///     Whether search sets accents and apostrophes aside as well as case, so "cote" finds "Côte d'Ivoire" and "a"
	///     finds "Åland". Folded by the library's own table (<see cref="MokaTextFold" />), so it works under invariant
	///     globalization, where an accent-blind compare would need ICU. Default false.
	/// </summary>
	[Parameter]
	public bool IgnoreAccents { get; set; }

	/// <summary>
	///     Runs the search in place of matching text: takes what was typed, trimmed and never blank, and returns the
	///     items to show in the order to show them, so the best match can come first. Wins over
	///     <see cref="SearchText" /> and <see cref="IgnoreAccents" />. While it is searching, the list keeps its order
	///     and leaves out <see cref="GroupBy" />'s headings.
	/// </summary>
	[Parameter]
	public Func<string, IEnumerable<TValue>>? SearchItems { get; set; }

	/// <summary>
	///     Lets a closed single select change its choice from the keyboard, as a native select does. Typing picks the
	///     first item whose text starts with what was typed, accents set aside; the same letter again moves on to the
	///     next item starting with it, and a pause of a second starts over. Up and Down step to the next or previous
	///     item, Home and End to the first and last, and Alt with Down or Up opens the list. Enter and Space still open
	///     it. Default false, which keeps Down opening the list.
	/// </summary>
	[Parameter]
	public bool TypeToJump { get; set; }

	/// <summary>
	///     In a <see cref="Multiple" /> select, Backspace in the empty search box takes the last chosen item off.
	///     Default false.
	/// </summary>
	[Parameter]
	public bool BackspaceRemovesLast { get; set; }

	/// <summary>
	///     A short tag at the end of an option while the list is searched, saying why it matched when that isn't its
	///     shown text (a code, another name). Takes the item and what was typed, trimmed; null or empty draws no tag.
	///     Nothing is drawn while the search box is empty.
	/// </summary>
	[Parameter]
	public Func<TValue, string, string?>? SearchTag { get; set; }

	/// <summary>
	///     Draws the closed box while nothing is chosen, in place of <see cref="MokaSelectBase{TValue}.Placeholder" />'s
	///     text, such as an icon. The placeholder still names the field when there is no label.
	/// </summary>
	[Parameter]
	public RenderFragment? PlaceholderTemplate { get; set; }

	/// <summary>The search box's placeholder. Default "Search...".</summary>
	[Parameter]
	public string SearchPlaceholder { get; set; } = "Search...";

	/// <summary>The search box's accessible name. Default "Search options".</summary>
	[Parameter]
	public string SearchLabel { get; set; } = "Search options";

	/// <inheritdoc />
	protected override string RootClass => "moka-select";

	private MokaListLook ListLook => MokaListLooks.Resolve(Look, CascadedTheme);

	private bool DrawsMark => MokaListLooks.DrawsMark(ListLook);

	private bool DrawsHints => ListLook == MokaListLook.Compact && !Loading && FilteredItems.Any();

	private bool Attached => ListLook == MokaListLook.Attached;

	private string ListboxId => $"{InputId}-listbox";

	private int? TriggerTabIndex => Disabled ? null : 0;

	private string OptionId(int index) => $"{ListboxId}-option-{index}";

	// Focus stays on the trigger (or the search box) while the arrows move the highlight, so this
	// is how a screen reader learns which option is highlighted.
	private string? ActiveOptionId => DropdownOpen && _focusedIndex >= 0 ? OptionId(_focusedIndex) : null;

	// The trigger is a div, which <label for> cannot name, so it is labelled by the wrapper's
	// label id. Without a visible label it falls back to an aria-label the consumer passed
	// (captured but not otherwise rendered), then to the placeholder.
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

	// The trigger is the control, so unmatched attributes go on it. aria-label is left out: the
	// trigger already renders it through AccessibleName, which also names the listbox.
	private IReadOnlyDictionary<string, object>? TriggerAttributes =>
		MokaAttributes.Without(AdditionalAttributes, "aria-label");

	// ErrorText is the explicit override; without one, fall back to whatever the cascaded
	// EditContext reports, so DataAnnotations messages are actually visible.
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	/// <summary>Explicit <see cref="ErrorText" /> when set, otherwise the EditContext validation message.</summary>
	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-select--error", HasError)
		.AddClass("moka-select--open", DropdownOpen)
		.AddClass("moka-select--multiple", Multiple)
		.AddClass(MokaListLooks.RootClass(RootClass, ListLook))
		.AddClass(CssClass)
		.AddClass(Class)
		.Build();

	// The field wrapper is the outermost element, so the margin goes there. The trigger draws the
	// field's border, so it takes the padding and the radius.

	/// <inheritdoc />
	protected override string? ComponentStyle => Style;

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string? TriggerStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

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
		.AddClass("moka-select-chevron--open", DropdownOpen)
		.Build();

	private string MarkCssClass(bool on) => new CssBuilder("moka-select-mark")
		.AddClass("moka-select-mark--on", on)
		.AddClass("moka-select-mark--box", Multiple)
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

			if (SearchItems is not null)
			{
				return SearchItems(_searchText.Trim()) ?? [];
			}

			if (IgnoreAccents)
			{
				string query = MokaTextFold.Fold(_searchText);
				return Items.Where(item => MokaTextFold.Fold(SearchableText(item)).Contains(query, StringComparison.Ordinal));
			}

			return Items.Where(item =>
				SearchableText(item).Contains(_searchText, StringComparison.OrdinalIgnoreCase));
		}
	}

	// A ranked search orders its results itself, which headings would break up.
	private bool Grouped => GroupBy is not null &&
	                        !(SearchItems is not null && Searchable && !string.IsNullOrWhiteSpace(_searchText));

	/// <summary>Items grouped by the GroupBy function.</summary>
	private IEnumerable<IGrouping<string?, TValue>> GroupedItems => FilteredItems.GroupBy(item => GroupBy?.Invoke(item));

	// The options in the order they are shown, which is what the arrow keys walk. With GroupBy
	// that is group by group, not the order of Items.
	private List<TValue> DisplayItems => !Grouped
		? FilteredItems.ToList()
		: GroupedItems.SelectMany(group => group).ToList();

	private bool JumpsOnKeys => TypeToJump && !Multiple && !Disabled;

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
		if (_boundJumpKeys != TypeToJump)
		{
			_boundJumpKeys = TypeToJump;
			await SafeModuleInvokeVoidAsync(KeysModule, "preventKeys", _root, TypeToJump ? JumpKeyRules : KeyRules);
		}

		// The list goes to the top layer, out of reach of an ancestor's overflow and of later stacking contexts.
		// Before the focus below: a list not shown yet can't take focus.
		if (_panelShown != DropdownOpen)
		{
			_panelShown = DropdownOpen;
			if (DropdownOpen)
			{
				// An attached list is one shape with the field, so it can't be wider.
				await SafeModuleInvokeVoidAsync(PopupModule, "open", _generatedId, _panel, _root, _backdrop,
					new Dictionary<string, object?> { ["placement"] = "bottom-start", ["width"] = Attached ? "match" : "min" });
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", _generatedId);
			}
		}

		bool watchSide = DropdownOpen && Attached;
		if (_watchingSide != watchSide)
		{
			_watchingSide = watchSide;
			if (watchSide)
			{
				await SafeModuleInvokeVoidAsync(MokaListLooks.SideModule, "watch", _generatedId, _root, _root, _panel);
			}
			else
			{
				await SafeModuleInvokeVoidAsync(MokaListLooks.SideModule, "unwatch", _generatedId);
			}
		}

		bool opened = DropdownOpen && !_wasOpen;
		_wasOpen = DropdownOpen;

		if (opened)
		{
			_refocusTrigger = false;
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await SafeModuleInvokeVoidAsync(HoverModule, "trackHover", _optionsRef, _dotNetRef);
			if (Searchable)
			{
				// Typing goes straight to the search box once the list is open.
				await TryFocusAsync(_searchInputRef);
			}
		}
		else if (!DropdownOpen && _refocusTrigger)
		{
			// Closing took the focused search box out of the page, which drops focus to <body>.
			_refocusTrigger = false;
			await TryFocusAsync(_triggerRef);
		}
	}

	/// <summary>Called from JS just before a key press, with the option the mouse is on, so the key carries on from there.</summary>
	/// <param name="index">The option's position in display order.</param>
	[JSInvokable]
	public void OnItemHovered(int index) => _focusedIndex = index;

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_dotNetRef?.Dispose();
		if (_watchingSide)
		{
			_watchingSide = false;
			await SafeModuleInvokeVoidAsync(MokaListLooks.SideModule, "unwatch", _generatedId);
		}

		if (_panelShown)
		{
			_panelShown = false;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _generatedId);
		}

		await base.DisposeAsyncCore();
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
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
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

	private string SearchableText(TValue item) => SearchText?.Invoke(item) ?? GetDisplayText(item);

	private string? TagFor(TValue item) =>
		SearchTag is null || !Searchable || string.IsNullOrWhiteSpace(_searchText)
			? null
			: SearchTag(item, _searchText.Trim()) is { Length: > 0 } tag ? tag : null;

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

		if (!DropdownOpen && JumpsOnKeys && await TryJumpAsync(e))
		{
			return;
		}

		switch (e.Key)
		{
			case "Enter":
			case " ":
				if (!DropdownOpen)
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
				if (DropdownOpen)
				{
					await CloseAsync();
					_searchText = string.Empty;
					_focusedIndex = -1;
				}

				break;

			case "ArrowDown":
				if (!DropdownOpen)
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
				if (DropdownOpen)
				{
					MoveFocus(-1);
				}

				break;

			case "Tab":
				if (DropdownOpen)
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
			case "Backspace" when BackspaceRemovesLast && Multiple && _searchText.Length == 0 && SelectedValues is { Count: > 0 }:
				await RemoveSelectedItem(SelectedValues[^1]);
				break;
		}
	}

	// Keys a closed single select takes itself under TypeToJump. False leaves the key to the usual handling.
	private async Task<bool> TryJumpAsync(KeyboardEventArgs e)
	{
		switch (e.Key)
		{
			case "ArrowDown" or "ArrowUp" when e.AltKey:
				await OpenAsync();
				_focusedIndex = -1;
				MoveFocus(1);
				return true;
			case "ArrowDown" or "ArrowUp":
				StepValue(e.Key == "ArrowDown" ? 1 : -1);
				return true;
			case "Home" or "End":
				List<TValue> ends = JumpCandidates();
				if (ends.Count > 0)
				{
					JumpTo(e.Key == "Home" ? ends[0] : ends[^1]);
				}

				return true;
		}

		bool printable = e.Key.Length == 1 || (e.Key.Length == 2 && char.IsSurrogatePair(e.Key, 0));
		if (!printable || e.CtrlKey || e.MetaKey || e.AltKey)
		{
			return false;
		}

		long now = Clock.GetTimestamp();
		bool paused = _typed.Length == 0 || Clock.GetElapsedTime(_typedAt, now) > TypingPause;
		// Space opens the list, as it always has, unless it falls inside a name being typed ("united k").
		if (e.Key == " " && paused)
		{
			return false;
		}

		_typed = (paused ? string.Empty : _typed) + MokaTextFold.Fold(e.Key);
		_typedAt = now;

		List<TValue> candidates = JumpCandidates();
		bool oneLetterAgain = _typed.Length > 1 && _typed.All(c => c == _typed[0]);
		if (_typed.Length == 1 || oneLetterAgain)
		{
			string letter = _typed[..1];
			List<TValue> starting = candidates.FindAll(item => MokaTextFold.StartsWith(GetDisplayText(item), letter));
			int at = starting.FindIndex(item => EqualityComparer<TValue>.Default.Equals(item, CurrentValue));
			if (starting.Count > 0)
			{
				JumpTo(starting[(at + 1) % starting.Count]);
			}
		}
		else
		{
			int match = candidates.FindIndex(item => MokaTextFold.StartsWith(GetDisplayText(item), _typed));
			if (match >= 0)
			{
				JumpTo(candidates[match]);
			}
		}

		return true;
	}

	private List<TValue> JumpCandidates() => DisplayItems.FindAll(item => !IsItemDisabled(item));

	private void StepValue(int direction)
	{
		List<TValue> candidates = JumpCandidates();
		if (candidates.Count == 0)
		{
			return;
		}

		int at = candidates.FindIndex(item => EqualityComparer<TValue>.Default.Equals(item, CurrentValue));
		int next = at < 0 ? (direction > 0 ? 0 : candidates.Count - 1) : Math.Clamp(at + direction, 0, candidates.Count - 1);
		JumpTo(candidates[next]);
	}

	private void JumpTo(TValue item)
	{
		if (EqualityComparer<TValue>.Default.Equals(item, CurrentValue))
		{
			return;
		}

		CurrentValue = item;
		_jumpAnnouncement = GetDisplayText(item);
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
