using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.NativeSelect;

/// <summary>
///     A real <c>&lt;select&gt;</c>, for a phone's own picker and for long lists. The options carry their position in
///     <see cref="Items" /> as their value, so any <typeparamref name="TValue" /> works and a null item is a real option.
/// </summary>
/// <typeparam name="TValue">The type of the selected value.</typeparam>
public partial class MokaNativeSelect<TValue>
{
	private readonly string _generatedId = $"moka-nativeselect-{Guid.NewGuid():N}";

	// Id goes on the select, not a wrapper, so a label's for and getElementById reach the control.
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

	/// <summary>The options, in the order the select lists them.</summary>
	[Parameter]
	public IReadOnlyList<TValue> Items { get; set; } = [];

	/// <summary>Converts an item to the text of its option. Default: ToString().</summary>
	[Parameter]
	public Func<TValue, string>? TextSelector { get; set; }

	/// <summary>
	///     Text of a first option that can't be chosen, shown while the value matches no item. Without one, such a
	///     value shows an empty select rather than a first option nobody picked.
	/// </summary>
	[Parameter]
	public string? Placeholder { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-native-select";

	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	/// <inheritdoc />
	protected override string ComponentCssClass => new CssBuilder(RootClass)
		.AddClass("moka-native-select--error", HasError)
		.AddClass(CssClass)
		.AddClass(Class)
		.Build();

	// The field wrapper is the outermost element, so the margin goes there. The select draws the
	// field's border, so it takes the padding and the radius.

	/// <inheritdoc />
	protected override string? ComponentStyle => Style;

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string? SelectStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private string SelectCssClass => new CssBuilder("moka-native-select-input")
		.AddClass($"moka-native-select-input--{SizeToKebab(Size)}")
		.AddClass("moka-native-select-input--placeholder", Placeholder is not null && IndexOf(CurrentValue) < 0)
		.Build();

	/// <inheritdoc />
	/// <remarks>The select's value is the position of the item, and an empty string when no item matches.</remarks>
	protected override string? FormatValueAsString(TValue? value)
	{
		int index = IndexOf(value);
		return index < 0 ? string.Empty : OptionValue(index);
	}

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
	{
		if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int index) && index < Items.Count)
		{
			result = Items[index];
			validationErrorMessage = string.Empty;
			return true;
		}

		result = default!;
		validationErrorMessage = $"'{value}' is not one of the options.";
		return false;
	}

	private static string OptionValue(int index) => index.ToString(CultureInfo.InvariantCulture);

	private string OptionText(TValue item) => TextSelector?.Invoke(item) ?? item?.ToString() ?? string.Empty;

	private int IndexOf(TValue? value)
	{
		for (int index = 0; index < Items.Count; index++)
		{
			if (EqualityComparer<TValue>.Default.Equals(Items[index], value))
			{
				return index;
			}
		}

		return -1;
	}

	private void HandleChange(ChangeEventArgs e) => CurrentValueAsString = e.Value?.ToString();
}
