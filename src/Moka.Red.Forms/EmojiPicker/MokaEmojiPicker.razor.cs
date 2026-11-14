using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Forms.Common;
using Moka.Red.Primitives.Emoji;

namespace Moka.Red.Forms.EmojiPicker;

/// <summary>
///     A form field whose value is one emoji: a button showing the choice, or a smile with a plus while empty, that
///     opens an emoji panel. Bind it with <c>@bind-Value</c>; labels, validation and a static page's post work as on
///     every field. <see cref="MokaEmojiOptions.Shape" /> picks how it shows: a popup grid (the default), a popup list,
///     a strip of quick picks with More, or a panel docked under the button.
/// </summary>
public partial class MokaEmojiPicker
{
	private readonly string _generatedId = $"moka-emoji-picker-{Guid.NewGuid():N}";
	private ElementReference _button;
	private bool _open;
	private string? _valueName;
	private string? _valueNameFor;

	/// <summary>Label text displayed above the field.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the field.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>
	///     Content beside the field's box, in its row, such as a button that acts on the value. The label stays above
	///     and the helper text below, and on the new design a button here is as tall as the box.
	/// </summary>
	[Parameter]
	public RenderFragment? Actions { get; set; }

	/// <summary>Error text displayed below the field when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Whether an emoji has to be chosen.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Whether the field can be left empty. Shows a quiet word beside the label. <see cref="Required" /> wins.</summary>
	[Parameter]
	public bool Optional { get; set; }

	/// <summary>Whether a cross beside the button clears the choice.</summary>
	[Parameter]
	public bool Clearable { get; set; }

	/// <summary>The clear button's name and tooltip. Default "Clear emoji".</summary>
	[Parameter]
	public string ClearLabel { get; set; } = "Clear emoji";

	/// <summary>What the button says to a screen reader while nothing is chosen. Default "None".</summary>
	[Parameter]
	public string NoneText { get; set; } = "None";

	/// <summary>The panel's options, its shape among them. Null is <see cref="MokaEmojiOptions.Default" />.</summary>
	[Parameter]
	public MokaEmojiOptions? Options { get; set; }

	/// <summary>Raised when the panel changes the skin tone or the frequently used list, for the app to store.</summary>
	[Parameter]
	public EventCallback<MokaEmojiOptions> OptionsChanged { get; set; }

	/// <summary>Where the popup opens against the button. Default below its start.</summary>
	[Parameter]
	public MokaTooltipPosition Placement { get; set; } = MokaTooltipPosition.BottomStart;

	/// <inheritdoc />
	protected override string RootClass => "moka-emoji-picker";

	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	private string LabelId => MokaFieldWrapper.LabelIdFor(InputId);

	private string ValueTextId => $"{InputId}-value";

	private MokaEmojiOptions PanelOptions => Options ?? MokaEmojiOptions.Default;

	private MokaEmojiText Words => PanelOptions.Text ?? MokaEmojiText.Default;

	private MokaEmojiShape Shape => PanelOptions.Shape;

	private bool Docked => Shape == MokaEmojiShape.Composer;

	private bool HasValue => !string.IsNullOrEmpty(CurrentValue);

	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	private string ValueText => !HasValue
		? NoneText
		: _valueNameFor == CurrentValue && !string.IsNullOrEmpty(_valueName) ? _valueName : CurrentValue!;

	private string LabelledBy => Label is null ? ValueTextId : $"{LabelId} {ValueTextId}";

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass($"moka-emoji-picker--{MokaEnumHelpers.ToCssClass(Shape)}")
		.AddClass($"moka-emoji-picker--{SizeToKebab(Size)}")
		.AddClass("moka-emoji-picker--error", HasError)
		.AddClass("moka-emoji-picker--disabled", Disabled)
		.AddClass(CssClass)
		.AddClass(Class)
		.Build();

	private string ButtonCss => new CssBuilder("moka-emoji-picker__button")
		.AddClass("moka-emoji-picker__button--empty", !HasValue)
		.Build();

	// The field wrapper is the outermost element, so the margin goes there.

	/// <inheritdoc />
	protected override string? ComponentStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle(Style)
		.Build();

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
	{
		result = value ?? string.Empty;
		validationErrorMessage = string.Empty;
		return true;
	}

	private void Toggle() => _open = !_open;

	private void PanelOpenChanged(bool open) => _open = open;

	private void Take(MokaEmojiPick pick)
	{
		_valueName = pick.Name;
		_valueNameFor = pick.Emoji;
		CurrentValue = pick.Emoji;
	}

	private void Clear()
	{
		_valueName = null;
		_valueNameFor = null;
		CurrentValue = null;
		_open = false;
	}
	private string? DescribedBy => MokaAttributes.DescribedBy(AdditionalAttributes,
		MokaFieldWrapper.HelperIdIfShown(InputId, HelperText, ResolvedErrorText));

	private IReadOnlyDictionary<string, object>? ControlAttributes => MokaAttributes.WithoutDescribedBy(AdditionalAttributes);
}
