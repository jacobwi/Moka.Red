using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Moka.Red.Forms.SchemaForm;

/// <summary>
///     One setting of a <see cref="MokaFormSchema" />, read from a property of the schema. Only
///     <see cref="MokaFormSchema.Parse(string)" /> makes them.
/// </summary>
public sealed class MokaSchemaField
{
	private readonly IReadOnlyList<MokaSchemaChoice> _choices = [];
	private readonly Dictionary<string, MokaSchemaChoice> _choicesByKey = new(StringComparer.Ordinal);

	internal MokaSchemaField()
	{
	}

	/// <summary>The property's name, the key its value has in the settings object.</summary>
	public string Name { get; internal init; } = string.Empty;

	/// <summary>What the field is labelled: the property's <c>title</c>, else its name.</summary>
	public string Title { get; internal init; } = string.Empty;

	/// <summary>The property's <c>description</c>, shown under the field and tied to it for screen readers.</summary>
	public string? Description { get; internal init; }

	/// <summary>What kind of control the field is.</summary>
	public MokaSchemaFieldKind Kind { get; internal init; }

	/// <summary>Whether the schema's <c>required</c> names the property.</summary>
	public bool Required { get; internal init; }

	/// <summary><c>readOnly</c>: the field shows its value and can't be changed, and nothing checks it.</summary>
	public bool ReadOnly { get; internal init; }

	/// <summary>
	///     <c>writeOnly</c>: a secret. The form never shows its value, only whether one is set, and reports a change to it
	///     apart from the other settings.
	/// </summary>
	public bool WriteOnly { get; internal init; }

	/// <summary><c>x-advanced</c>: the field sits in the advanced group, hidden until "Show advanced".</summary>
	public bool Advanced { get; internal init; }

	/// <summary><c>x-multiline</c>: text drawn as a text area.</summary>
	public bool Multiline { get; internal init; }

	/// <summary>The property's <c>default</c>, which a setting that isn't set takes.</summary>
	public JsonElement? Default { get; private set; }

	/// <summary>The first of the property's <c>examples</c>, shown as the field's placeholder.</summary>
	public string? Placeholder { get; internal init; }

	/// <summary>
	///     The property's <c>format</c>. <c>email</c>, <c>uri</c>, <c>hostname</c>, <c>ipv4</c>, <c>ipv6</c> and
	///     <c>uuid</c> are checked; any other is kept as written and not checked.
	/// </summary>
	public string? Format { get; internal init; }

	/// <summary>The property's <c>pattern</c>, a regular expression the text has to match somewhere.</summary>
	public string? Pattern { get; internal init; }

	/// <summary><c>minLength</c>: the fewest characters the text takes, counted as Unicode code points.</summary>
	public int? MinLength { get; internal init; }

	/// <summary><c>maxLength</c>: the most characters the text takes, counted as Unicode code points.</summary>
	public int? MaxLength { get; internal init; }

	/// <summary><c>minimum</c>: the smallest number allowed.</summary>
	public double? Minimum { get; internal init; }

	/// <summary><c>maximum</c>: the largest number allowed.</summary>
	public double? Maximum { get; internal init; }

	/// <summary><c>exclusiveMinimum</c>: the number has to be larger than this.</summary>
	public double? ExclusiveMinimum { get; internal init; }

	/// <summary><c>exclusiveMaximum</c>: the number has to be smaller than this.</summary>
	public double? ExclusiveMaximum { get; internal init; }

	/// <summary><c>multipleOf</c>: the number is a whole multiple of this.</summary>
	public double? MultipleOf { get; internal init; }

	/// <summary>The choices of a <see cref="MokaSchemaFieldKind.Choice" /> field, in the order the schema lists them. Empty otherwise.</summary>
	public IReadOnlyList<MokaSchemaChoice> Choices
	{
		get => _choices;
		internal init
		{
			_choices = value;
			_choicesByKey = value.ToDictionary(choice => choice.Key, StringComparer.Ordinal);
		}
	}

	internal JsonNode? DefaultNode { get; private set; }

	internal Regex? PatternRegex { get; init; }

	// A number the schema also takes as text, such as "8080".
	internal bool TakesNumberText { get; init; }

	internal decimal? ExactMultipleOf { get; init; }

	internal string InputType => MokaSchemaFormats.InputType(Format);

	internal bool Mono => MokaSchemaFormats.IsMono(Format);

	// The inclusive bounds a whole-number field can take, for the number field's own limits.
	internal long? WholeMinimum => WholeBound(Minimum, ExclusiveMinimum, true);

	internal long? WholeMaximum => WholeBound(Maximum, ExclusiveMaximum, false);

	internal long? WholeStep => ExactMultipleOf is { } step && decimal.Truncate(step) == step && step <= long.MaxValue
		? (long)step
		: 1;

	internal double? NumberStep => MultipleOf;

	internal MokaSchemaChoice? ChoiceFor(JsonNode? value) =>
		value is not null && MokaSchemaJson.KeyOf(value) is { } key ? _choicesByKey.GetValueOrDefault(key) : null;

	internal void TakeDefault(JsonNode fallback)
	{
		DefaultNode = fallback.DeepClone();
		Default = MokaSchemaJson.ToElement(fallback);
	}

	// What's wrong with a value that is set, or null when it fits. Whether it has to be set is the caller's to say.
	internal string? Problem(JsonNode value, MokaSchemaFormText words) => Kind switch
	{
		MokaSchemaFieldKind.Toggle => value.GetValueKind() is JsonValueKind.True or JsonValueKind.False
			? null
			: words.NotToggle(Title),
		MokaSchemaFieldKind.Choice => ChoiceFor(value) is null ? words.NotChoice(Title) : null,
		MokaSchemaFieldKind.WholeNumber or MokaSchemaFieldKind.Number => NumberProblem(value, words),
		_ => MokaSchemaJson.TryGetText(value, out string? text) ? TextProblem(text, words) : words.NotText(Title)
	};

	internal string? TextProblem(string text, MokaSchemaFormText words)
	{
		int length = CountCharacters(text);
		if (MinLength is { } fewest && length < fewest)
		{
			return words.TooShort(Title, Write(fewest));
		}

		if (MaxLength is { } most && length > most)
		{
			return words.TooLong(Title, Write(most));
		}

		if (PatternRegex is { } pattern && !Matches(pattern, text))
		{
			return words.NoMatch(Title);
		}

		return MokaSchemaFormats.Problem(Format, text, Title, words);
	}

	private string? NumberProblem(JsonNode value, MokaSchemaFormText words)
	{
		if (!TryReadNumber(value, out double number, out string raw))
		{
			return words.NotNumber(Title);
		}

		if (Kind == MokaSchemaFieldKind.WholeNumber && !IsWhole(number))
		{
			return words.NotWholeNumber(Title);
		}

		if (Minimum is { } minimum && number < minimum)
		{
			return words.TooSmall(Title, Write(minimum));
		}

		if (ExclusiveMinimum is { } above && number <= above)
		{
			return words.TooSmallExclusive(Title, Write(above));
		}

		if (Maximum is { } maximum && number > maximum)
		{
			return words.TooLarge(Title, Write(maximum));
		}

		if (ExclusiveMaximum is { } below && number >= below)
		{
			return words.TooLargeExclusive(Title, Write(below));
		}

		return MultipleOf is { } step && !IsMultiple(number, raw, step) ? words.NotMultiple(Title, Write(step)) : null;
	}

	internal bool TryReadNumber(JsonNode value, out double number, out string raw)
	{
		if (MokaSchemaJson.TryGetNumber(value, out number, out raw))
		{
			return true;
		}

		if (TakesNumberText && MokaSchemaJson.TryGetText(value, out string? text) &&
		    double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && double.IsFinite(number))
		{
			raw = text;
			return true;
		}

		return false;
	}

	// Decimal where both fit, so 0.3 is a multiple of 0.1; a double ratio otherwise.
	private bool IsMultiple(double number, string raw, double step)
	{
		if (ExactMultipleOf is { } exactStep &&
		    decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal exact))
		{
			return exact % exactStep == 0;
		}

		double ratio = number / step;
		return double.IsFinite(ratio) && Math.Abs(ratio - Math.Round(ratio)) < 1e-9;
	}

	// Whole and inside a long, which the number field holds it in.
	internal static bool IsWhole(double number) =>
		Math.Floor(number) == number && number >= -9.2233720368547758E18 && number < 9.2233720368547758E18;

	private static long? WholeBound(double? inclusive, double? exclusive, bool lower)
	{
		double? bound = null;
		if (inclusive is { } given)
		{
			bound = lower ? Math.Ceiling(given) : Math.Floor(given);
		}

		if (exclusive is { } past)
		{
			double next = lower ? Math.Floor(past) + 1 : Math.Ceiling(past) - 1;
			bound = bound is { } current ? lower ? Math.Max(current, next) : Math.Min(current, next) : next;
		}

		return bound is { } value && IsWhole(value) ? (long)value : null;
	}

	private static bool Matches(Regex pattern, string text)
	{
		try
		{
			return pattern.IsMatch(text);
		}
		catch (RegexMatchTimeoutException)
		{
			return false;
		}
	}

	// JSON Schema counts characters as RFC 8259 does: code points, so an emoji is one.
	private static int CountCharacters(string text)
	{
		int count = 0;
		foreach (Rune _ in text.EnumerateRunes())
		{
			count++;
		}

		return count;
	}

	private static string Write(double number) => number.ToString(CultureInfo.CurrentCulture);

	private static string Write(int number) => number.ToString(CultureInfo.CurrentCulture);
}
