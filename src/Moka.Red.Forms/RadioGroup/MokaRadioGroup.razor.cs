using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.RadioGroup;

/// <summary>
///     A radio button group component. Contains <see cref="MokaRadioItem{TValue}" /> children
///     that register via CascadingParameter. Supports vertical and horizontal layouts.
/// </summary>
/// <typeparam name="TValue">The type of the selected value.</typeparam>
public partial class MokaRadioGroup<TValue> : MokaVisualInputBase<TValue>
{
	/// <summary>The radio item children.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Label text displayed above the radio group.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>
	///     Accessible name for the group, for a group that already has a heading of its own and should not
	///     draw a second one. Without it the group is named by <see cref="Label" />, which is also the
	///     visible heading.
	/// </summary>
	[Parameter]
	public string? AriaLabel { get; set; }

	/// <summary>Helper text displayed below the radio group.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Layout direction. Default Column (vertical).</summary>
	[Parameter]
	public MokaDirection Orientation { get; set; } = MokaDirection.Column;

	/// <summary>
	///     Draws the options as cards that wrap to fit, each with a picture above its name
	///     (<see cref="MokaRadioItem{TValue}.Preview" />, <see cref="MokaRadioItem{TValue}.Hint" />).
	///     <see cref="Orientation" /> doesn't apply.
	/// </summary>
	[Parameter]
	public bool Cards { get; set; }

	/// <summary>
	///     The narrowest a card gets before the cards wrap, with <see cref="Cards" />. A CSS length, 10rem when unset.
	/// </summary>
	[Parameter]
	public string? MinColumn { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-radiogroup";

	// AriaLabel wins so a group under someone else's heading can be named without drawing a second one.
	private string? GroupAriaLabel => AriaLabel ?? Label;

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-radiogroup--row", Orientation == MokaDirection.Row && !Cards)
		.AddClass("moka-radiogroup--cards", Cards)
		.AddClass("moka-radiogroup--disabled", Disabled)
		.AddClass(CssClass) // InputBase's field classes: modified, valid, invalid
		.AddClass(Class)
		.Build();

	// The field wrapper is the outermost element, so the margin goes there. The group itself is
	// the field's box, so it keeps the padding and the radius.

	/// <inheritdoc />
	protected override string? ComponentStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("grid-template-columns", CardColumns, Cards)
		.AddStyle(Style)
		.Build();

	// auto-fill rather than auto-fit, so two cards in a wide group stay the size a full row of them would be.
	private string CardColumns =>
		$"repeat(auto-fill, minmax(min(100%, {(CssValues.IsLength(MinColumn) ? CssValues.WithPixels(MinColumn.Trim()) : DefaultCardWidth)}), 1fr))";

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
	{
		result = default!;
		validationErrorMessage = $"Cannot convert '{value}' to {typeof(TValue).Name}: the selected value comes from a child MokaRadioItem, not from a string.";
		return false;
	}

	private const string DefaultCardWidth = "10rem";

	private readonly string _generatedName = $"moka-radiogroup-{Guid.NewGuid():N}";

	/// <summary>
	///     The <c>name</c> every radio in the group shares. The browser needs it to treat them as one
	///     group: one checked at a time, one tab stop, and the arrow keys moving between them. In a form
	///     it's the field's name, so a post carries the choice.
	/// </summary>
	/// <remarks>
	///     Outside a form it stays generated, because two groups bound through a loop variable would
	///     both be called <c>row.Plan</c> and the browser would make them one group.
	/// </remarks>
	internal string GroupName =>
		(EditContext is not null || AdditionalAttributes?.ContainsKey("name") == true) && FieldName is { } postedName
			? postedName
			: _generatedName;

	/// <summary>Selects a value from a radio item click.</summary>
	internal void SelectValue(TValue value)
	{
		CurrentValue = value;
		StateHasChanged();
	}

	/// <summary>Checks if a value is currently selected.</summary>
	internal bool IsSelected(TValue value) => EqualityComparer<TValue>.Default.Equals(value, CurrentValue);
}
