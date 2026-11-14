using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;
using Moka.Red.Primitives.Emoji;

namespace Moka.Red.Forms.TextArea;

/// <summary>
///     A multi-line text input (textarea) with optional auto-resize support.
/// </summary>
public partial class MokaTextArea
{
	private const string EmojiModule = "./_content/Moka.Red.Primitives/moka-emoji.js";

	private readonly string _generatedId = $"moka-textarea-{Guid.NewGuid():N}";
	private ElementReference _emojiButton;
	private bool _emojiOpen;

	// Id goes on the textarea, not a wrapper, so a label's for and getElementById reach the control.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	/// <summary>Label text displayed above the textarea.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the textarea.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text displayed below the textarea when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Whether the field is required.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Whether the field can be left empty. Shows a quiet word beside the label. <see cref="Required" /> wins.</summary>
	[Parameter]
	public bool Optional { get; set; }

	/// <summary>Writes the text in the monospace font and turns spell check off, for lists of names or addresses.</summary>
	[Parameter]
	public bool Mono { get; set; }

	/// <summary>Number of visible text lines. Default 3.</summary>
	[Parameter]
	public int Rows { get; set; } = 3;

	/// <summary>Whether the textarea grows with content. Default false.</summary>
	[Parameter]
	public bool AutoResize { get; set; }

	/// <summary>
	///     Whether a smile in the bottom corner opens an emoji panel that puts the pick in at the caret, over any
	///     selection, and hands focus back to the textarea. Default false. For a ":" typeahead, wrap the textarea in
	///     MokaEmojiTypeahead.
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
	protected override string RootClass => "moka-textarea";

	// ErrorText is the explicit override; without one, fall back to whatever the cascaded
	// EditContext reports, so DataAnnotations messages are actually visible.
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	/// <summary>Explicit <see cref="ErrorText" /> when set, otherwise the EditContext validation message.</summary>
	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-textarea--error", HasError)
		.AddClass("moka-textarea--auto-resize", AutoResize)
		.AddClass("moka-textarea--emoji", EmojiButton)
		.AddClass(CssClass)
		.AddClass(Class)
		.Build();

	private MokaEmojiOptions EmojiPanelOptions => EmojiOptions switch
	{
		null => MokaEmojiOptions.Default,
		{ Shape: MokaEmojiShape.Popover or MokaEmojiShape.List } options => options,
		{ } options => options with { Shape = MokaEmojiShape.Popover }
	};

	// The field wrapper is the outermost element, so the margin goes there. The textarea draws the
	// field's border, so it takes the padding and the radius.

	/// <inheritdoc />
	protected override string? ComponentStyle => Style;

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string? TextAreaStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private string TextAreaCssClass => new CssBuilder("moka-textarea-input")
		.AddClass($"moka-textarea-input--{SizeToKebab(Size)}")
		.AddClass("moka-field-input--mono", Mono)
		.Build();

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
	{
		result = value ?? string.Empty;
		validationErrorMessage = string.Empty;
		return true;
	}

	private void ToggleEmoji() => _emojiOpen = !_emojiOpen;

	private void EmojiOpenChanged(bool open) => _emojiOpen = open;

	private async Task InsertEmojiAsync(MokaEmojiPick pick) =>
		await SafeModuleInvokeVoidAsync(EmojiModule, "insertNear", _emojiButton, ".moka-textarea", pick.Emoji);
	private string? DescribedBy => MokaAttributes.DescribedBy(AdditionalAttributes,
		MokaFieldWrapper.HelperIdIfShown(InputId, HelperText, ResolvedErrorText));

	private IReadOnlyDictionary<string, object>? ControlAttributes => MokaAttributes.WithoutDescribedBy(AdditionalAttributes);
}
