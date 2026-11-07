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
	// A type whose value survives being written as text and read back, which a form post needs.
	private static readonly bool PostableType = IsPostable(Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue));

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

	// On a static page the browser posts the chosen option's value and the server binds that text to the field, so a
	// bound select's options carry the values themselves. A type that can't be read back from text keeps positions,
	// which the select can always map back to an item, and isn't posted.
	private bool PostsValues => PostableType && FieldName is not null;

	private string? PostedName => PostsValues ? FieldName : null;

	/// <inheritdoc />
	/// <remarks>
	///     The select's value is the item's own text when the select is bound to a field and its type can be read back from
	///     text, otherwise the position of the item. It's an empty string when no item matches.
	/// </remarks>
	protected override string? FormatValueAsString(TValue? value)
	{
		int index = IndexOf(value);
		return index < 0 ? string.Empty : OptionValue(index);
	}

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
	{
		int index = PostsValues ? IndexOfText(value) : ParseIndex(value);
		if (index >= 0)
		{
			result = Items[index];
			validationErrorMessage = string.Empty;
			return true;
		}

		result = default!;
		validationErrorMessage = $"'{value}' is not one of the options.";
		return false;
	}

	private string OptionValue(int index) => PostsValues
		? TextOf(Items[index])
		: index.ToString(CultureInfo.InvariantCulture);

	private static string TextOf(TValue item) => Convert.ToString(item, CultureInfo.InvariantCulture) ?? string.Empty;

	private int ParseIndex(string? value) =>
		int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int index) && index < Items.Count ? index : -1;

	private int IndexOfText(string? value)
	{
		for (int index = 0; index < Items.Count; index++)
		{
			if (string.Equals(TextOf(Items[index]), value ?? string.Empty, StringComparison.Ordinal))
			{
				return index;
			}
		}

		return -1;
	}

	private static bool IsPostable(Type type) =>
		type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(Guid) ||
		type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly) ||
		type == typeof(TimeSpan);

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
