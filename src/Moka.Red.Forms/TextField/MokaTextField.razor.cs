using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;
using Moka.Red.Primitives.Emoji;

namespace Moka.Red.Forms.TextField;

/// <summary>
///     A single-line text input field with optional icons, clear button, and validation support. With
///     <see cref="Suggestions" /> it also suggests values from a list while the person types anything they like.
/// </summary>
public partial class MokaTextField
{
	private const string EmojiModule = "./_content/Moka.Red.Primitives/moka-emoji.js";
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";
	private const string HoverModule = "./_content/Moka.Red.Core/moka-hover.js";

	// Enter puts the highlighted suggestion in the box, so it must not also submit a surrounding form, and the arrows move
	// the highlight rather than the caret. With nothing highlighted Enter is left alone and submits as usual.
	private static readonly Dictionary<string, object?>[] SuggestionKeyRules =
	[
		new() { ["selector"] = "input[role=combobox]", ["keys"] = new[] { "Enter" }, ["when"] = ".moka-textfield-option--highlighted" },
		new() { ["selector"] = "input[role=combobox]", ["keys"] = new[] { "ArrowDown", "ArrowUp" }, ["when"] = ".moka-textfield-suggestions" }
	];

	private readonly string _generatedId = $"moka-textfield-{Guid.NewGuid():N}";
	private ElementReference _emojiButton;
	private bool _emojiOpen;
	private ElementReference _root;
	private ElementReference _backdrop;
	private ElementReference _panel;
	private DotNetObjectReference<MokaTextField>? _dotNetRef;
	private bool _keysBound;
	private bool _panelShown;
	private bool _watchingSide;
	private bool _suggestionsOpen;
	private int _highlighted = -1;
	private List<string> _matches = [];

	// Id goes on the input, not a wrapper, so a label's for and getElementById reach the control.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	/// <summary>Label text displayed above the input.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the input.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>
	///     Content beside the field's box, in its row, such as a button that acts on the value. The label stays above
	///     and the helper text below, and on the new design a button here is as tall as the box.
	/// </summary>
	[Parameter]
	public RenderFragment? Actions { get; set; }

	/// <summary>Error text displayed below the input when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Whether the field is required.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Whether the field can be left empty. Shows a quiet word beside the label. <see cref="Required" /> wins.</summary>
	[Parameter]
	public bool Optional { get; set; }

	/// <summary>Writes the value in the monospace font and turns spell check off, for addresses, names and keys.</summary>
	[Parameter]
	public bool Mono { get; set; }

	/// <summary>A unit inside the box at the right, such as "Mbit/s". The input's <c>aria-describedby</c> points at it.</summary>
	[Parameter]
	public string? Suffix { get; set; }

	/// <summary>Icon displayed before the input text.</summary>
	[Parameter]
	public MokaIconDefinition? StartIcon { get; set; }

	/// <summary>Icon displayed after the input text.</summary>
	[Parameter]
	public MokaIconDefinition? EndIcon { get; set; }

	/// <summary>Whether to show a clear button when the input has a value.</summary>
	[Parameter]
	public bool Clearable { get; set; }

	/// <summary>Callback invoked on every keystroke.</summary>
	[Parameter]
	public EventCallback<string> OnInput { get; set; }

	/// <summary>
	///     Whether a smile at the end of the box opens an emoji panel that puts the pick in at the caret, over any
	///     selection, and hands focus back to the field. Default false.
	/// </summary>
	[Parameter]
	public bool EmojiButton { get; set; }

	/// <summary>
	///     The emoji panel's options. A popup either way: <see cref="MokaEmojiShape.List" /> opens the list and every other
	///     shape the popover grid.
	/// </summary>
	[Parameter]
	public MokaEmojiOptions? EmojiOptions { get; set; }

	/// <summary>Raised when the panel changes the skin tone or the frequently used list, for the app to store.</summary>
	[Parameter]
	public EventCallback<MokaEmojiOptions> EmojiOptionsChanged { get; set; }

	/// <summary>The emoji button's name and tooltip. Default "Insert emoji".</summary>
	[Parameter]
	public string EmojiButtonLabel { get; set; } = "Insert emoji";

	/// <summary>
	///     Values to suggest while the person types, such as known model names. The field still takes anything typed:
	///     the list shows the suggestions that contain the text (all of them while the box is empty), and picking one puts
	///     it in the box. To screen readers the field is a combobox with a list of suggestions. Down or Up opens the list
	///     and moves through it, Alt+Down opens it in place, Enter or a click picks the highlighted suggestion, and Escape
	///     or Tab closes it. Null, the default, is a plain text field with its markup unchanged.
	/// </summary>
	[Parameter]
	public IReadOnlyList<string>? Suggestions { get; set; }

	/// <summary>The most suggestions the list shows at once. Default 10.</summary>
	[Parameter]
	public int MaxSuggestions { get; set; } = 10;

	/// <summary>
	///     How the rows of the suggestion list are drawn. Null takes the theme's choice (<see cref="MokaTheme.WithLists" />),
	///     as <c>MokaAutoComplete</c> and <c>MokaSelect</c> do. The looks that mark a chosen row mark the suggestion equal to
	///     the value. Looks belong to the new design: on <see cref="MokaTheme.Dark" /> and <see cref="MokaTheme.Light" /> the
	///     list stays standard.
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

	/// <inheritdoc />
	protected override string RootClass => "moka-textfield";

	// ErrorText is the explicit override; without one, fall back to whatever the cascaded
	// EditContext reports, so DataAnnotations messages are actually visible.
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	/// <summary>Explicit <see cref="ErrorText" /> when set, otherwise the EditContext validation message.</summary>
	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	// Built on every render: whether the suggestion list is open changes without a parameter set.
	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-textfield--error", HasError)
		.AddClass("moka-textfield--suffix-inset", HasSuffix && (EndTaken || EmojiButton))
		.AddClass("moka-textfield--emoji", EmojiButton)
		.AddClass("moka-textfield--emoji-inset", EmojiButton && EndTaken)
		.AddClass(HasSuggestions ? MokaListLooks.RootClass(RootClass, ListLook) : null)
		.AddClass("moka-textfield--open", SuggestionsShown)
		.AddClass(CssClass)
		.AddClass(Class)
		.Build();

	private bool EndTaken => EndIcon is not null || Clearable;

	private bool HasSuggestions => Suggestions is not null;

	private bool SuggestionsShown => HasSuggestions && _suggestionsOpen && _matches.Count > 0;

	// The theme is read when the list draws rather than through a cascading parameter, so a page of text fields doesn't
	// subscribe each one to the theme for a list most of them never open.
	private MokaListLook ListLook => MokaListLooks.Resolve(Look, Theme);

	private bool DrawsMark => MokaListLooks.DrawsMark(ListLook);

	private string ListboxId => $"{InputId}-listbox";

	private string OptionId(int index) => $"{ListboxId}-option-{index}";

	// Focus stays in the box while the arrows move the highlight, so this is how a screen reader hears which suggestion
	// is highlighted.
	private string? HighlightedOptionId => SuggestionsShown && _highlighted >= 0 && _highlighted < _matches.Count
		? OptionId(_highlighted)
		: null;

	private string? AppLabel => AdditionalAttributes?.GetValueOrDefault("aria-label") as string;

	private EventCallback<KeyboardEventArgs> SuggestionKeys => HasSuggestions
		? EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleSuggestionKey)
		: default;

	// The field wrapper is the outermost element, so the margin goes there. The input draws the
	// field's border, so it takes the padding and the radius.

	/// <inheritdoc />
	protected override string? ComponentStyle => Style;

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string? InputStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("--moka-field-suffix-ch", SuffixWidth)
		.Build();

	private string InputCssClass { get; set; } = "";

	private bool HasSuffix => !string.IsNullOrEmpty(Suffix);

	private string SuffixId => $"{InputId}-suffix";

	private string? SuffixWidth => HasSuffix ? FormattableString.Invariant($"{Suffix!.Length}ch") : null;

	private string? DescribedBy => MokaAttributes.DescribedBy(AdditionalAttributes, HasSuffix ? SuffixId : null,
		MokaFieldWrapper.HelperIdIfShown(InputId, HelperText, ResolvedErrorText));

	private IReadOnlyDictionary<string, object>? InputAttributes => MokaAttributes.WithoutDescribedBy(AdditionalAttributes);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		InputCssClass = new CssBuilder("moka-textfield-input")
			.AddClass($"moka-textfield-input--{SizeToKebab(Size)}")
			.AddClass("moka-textfield-input--has-start-icon", StartIcon is not null)
			.AddClass("moka-textfield-input--has-end-icon", EndTaken)
			.AddClass("moka-textfield-input--has-emoji", EmojiButton)
			.AddClass("moka-field-input--has-suffix", HasSuffix)
			.AddClass("moka-field-input--mono", Mono)
			.Build();

		if (!_suggestionsOpen)
		{
			return;
		}

		if (HasSuggestions)
		{
			FilterSuggestions(InputText);
		}
		else
		{
			CloseSuggestions();
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (!HasSuggestions)
		{
			// The root that listened is gone with the suggestions, so a later list binds the new one.
			_keysBound = false;
			await HidePanelAsync();
			return;
		}

		if (!_keysBound)
		{
			_keysBound = true;
			await SafeModuleInvokeVoidAsync(KeysModule, "preventKeys", _root, SuggestionKeyRules);
		}

		// The suggestions go to the top layer, out of reach of an ancestor's overflow and of later stacking contexts.
		bool shown = SuggestionsShown;
		if (_panelShown != shown)
		{
			if (shown)
			{
				_panelShown = true;
				_dotNetRef ??= DotNetObjectReference.Create(this);
				await SafeModuleInvokeVoidAsync(PopupModule, "open", _generatedId, _panel, _root, _backdrop,
					new Dictionary<string, object?> { ["placement"] = "bottom-start", ["width"] = "match" });
				await SafeModuleInvokeVoidAsync(HoverModule, "trackHover", _panel, _dotNetRef);
			}
			else
			{
				await HidePanelAsync();
			}
		}

		bool watchSide = shown && ListLook == MokaListLook.Attached;
		if (_watchingSide != watchSide)
		{
			_watchingSide = watchSide;
			await SafeModuleInvokeVoidAsync(MokaListLooks.SideModule, watchSide ? "watch" : "unwatch", _generatedId,
				_root, _root, _panel);
		}
	}

	/// <summary>Called from JS just before a key press, with the suggestion the mouse is on, so the key carries on from there.</summary>
	/// <param name="index">The suggestion's position in the list.</param>
	[JSInvokable]
	public void OnItemHovered(int index) => _highlighted = index;

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
	{
		result = value ?? string.Empty;
		validationErrorMessage = string.Empty;
		return true;
	}

	/// <inheritdoc />
	protected override void OnDebouncedInput(string? value)
	{
		CurrentValueAsString = value;

		if (OnInput.HasDelegate)
		{
			RunDetached(() => OnInput.InvokeAsync(value ?? string.Empty));
		}
	}

	private MokaEmojiOptions EmojiPanelOptions => EmojiOptions switch
	{
		null => MokaEmojiOptions.Default,
		{ Shape: MokaEmojiShape.Popover or MokaEmojiShape.List } options => options,
		{ } options => options with { Shape = MokaEmojiShape.Popover }
	};

	private void ToggleEmoji() => _emojiOpen = !_emojiOpen;

	private void EmojiOpenChanged(bool open) => _emojiOpen = open;

	private async Task InsertEmojiAsync(MokaEmojiPick pick) =>
		await SafeModuleInvokeVoidAsync(EmojiModule, "insertNear", _emojiButton, ".moka-textfield", pick.Emoji);

	private async Task HandleClear()
	{
		CurrentValueAsString = string.Empty;
		CloseSuggestions();

		if (OnInput.HasDelegate)
		{
			await OnInput.InvokeAsync(string.Empty);
		}
	}

	private void HandleTyped(string? value)
	{
		HandleInputWithDebounce(value);
		if (HasSuggestions)
		{
			FilterSuggestions(value);
			_suggestionsOpen = true;
			_highlighted = -1;
		}
	}

	private void FilterSuggestions(string? text)
	{
		string typed = text ?? string.Empty;
		_matches = [.. Suggestions!.Where(s => s.Contains(typed, StringComparison.OrdinalIgnoreCase)).Take(Math.Max(1, MaxSuggestions))];
		if (_highlighted >= _matches.Count)
		{
			_highlighted = -1;
		}
	}

	private void OpenSuggestions()
	{
		FilterSuggestions(InputText);
		_suggestionsOpen = true;
	}

	private void CloseSuggestions()
	{
		_suggestionsOpen = false;
		_highlighted = -1;
	}

	private void HandleSuggestionKey(KeyboardEventArgs e)
	{
		if (ReadOnly || Disabled)
		{
			return;
		}

		switch (e.Key)
		{
			case "ArrowDown":
				if (!SuggestionsShown)
				{
					OpenSuggestions();
					_highlighted = e.AltKey || _matches.Count == 0 ? -1 : 0;
				}
				else if (!e.AltKey)
				{
					_highlighted = (_highlighted + 1) % _matches.Count;
				}

				break;

			case "ArrowUp":
				if (!SuggestionsShown)
				{
					OpenSuggestions();
					_highlighted = _matches.Count - 1;
				}
				else
				{
					_highlighted = _highlighted <= 0 ? _matches.Count - 1 : _highlighted - 1;
				}

				break;

			case "Enter":
				if (SuggestionsShown && _highlighted >= 0 && _highlighted < _matches.Count)
				{
					PickSuggestion(_matches[_highlighted]);
				}
				else
				{
					CloseSuggestions();
				}

				break;

			case "Escape":
			case "Tab":
				CloseSuggestions();
				break;
		}
	}

	// The same path as typing, so the box, the value and the change callback agree whichever way the text arrived.
	private void PickSuggestion(string suggestion)
	{
		CloseSuggestions();
		HandleInputWithDebounce(suggestion);
	}

	private bool IsChosen(string suggestion) => string.Equals(suggestion, CurrentValue, StringComparison.Ordinal);

	private string OptionCssClass(int index, string suggestion) => new CssBuilder("moka-textfield-option")
		.AddClass("moka-textfield-option--highlighted", index == _highlighted)
		.AddClass("moka-textfield-option--chosen", ListLook != MokaListLook.Standard && IsChosen(suggestion))
		.Build();

	private string MarkCssClass(string suggestion) => new CssBuilder("moka-textfield-mark")
		.AddClass("moka-textfield-mark--on", IsChosen(suggestion))
		.Build();

	private async Task HidePanelAsync()
	{
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
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		await HidePanelAsync();
		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
