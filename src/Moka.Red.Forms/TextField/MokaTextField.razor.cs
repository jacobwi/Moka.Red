using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;
using Moka.Red.Primitives.Emoji;

namespace Moka.Red.Forms.TextField;

/// <summary>
///     A single-line text input field with optional icons, clear button, and validation support.
/// </summary>
public partial class MokaTextField
{
	private const string EmojiModule = "./_content/Moka.Red.Primitives/moka-emoji.js";

	private readonly string _generatedId = $"moka-textfield-{Guid.NewGuid():N}";
	private ElementReference _emojiButton;
	private bool _emojiOpen;

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

	/// <inheritdoc />
	protected override string RootClass => "moka-textfield";

	// ErrorText is the explicit override; without one, fall back to whatever the cascaded
	// EditContext reports, so DataAnnotations messages are actually visible.
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	/// <summary>Explicit <see cref="ErrorText" /> when set, otherwise the EditContext validation message.</summary>
	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	private string ComputedCssClass { get; set; } = "";

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
		bool endTaken = EndIcon is not null || Clearable;
		ComputedCssClass = new CssBuilder(RootClass)
			.AddClass("moka-textfield--error", HasError)
			.AddClass("moka-textfield--suffix-inset", HasSuffix && (endTaken || EmojiButton))
			.AddClass("moka-textfield--emoji", EmojiButton)
			.AddClass("moka-textfield--emoji-inset", EmojiButton && endTaken)
			.AddClass(CssClass)
			.AddClass(Class)
			.Build();
		InputCssClass = new CssBuilder("moka-textfield-input")
			.AddClass($"moka-textfield-input--{SizeToKebab(Size)}")
			.AddClass("moka-textfield-input--has-start-icon", StartIcon is not null)
			.AddClass("moka-textfield-input--has-end-icon", endTaken)
			.AddClass("moka-textfield-input--has-emoji", EmojiButton)
			.AddClass("moka-field-input--has-suffix", HasSuffix)
			.AddClass("moka-field-input--mono", Mono)
			.Build();
	}

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

		if (OnInput.HasDelegate)
		{
			await OnInput.InvokeAsync(string.Empty);
		}
	}
}
