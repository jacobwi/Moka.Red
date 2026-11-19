using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Moka.Red.Forms.SchemaForm;

// Reads the JSON Schema 2020-12 subset a form shows. Keywords it doesn't know are ignored, as the core spec lets an
// implementation do with unknown keywords; keywords that change what a value may be and that a form can't honour (a
// nested schema, a reference, a condition) are problems, so no rule is dropped without saying so.
internal static class MokaSchemaReader
{
	private const string SchemaWhere = "The schema";

	private static readonly string[] RootKeywordsNotSupported =
	[
		"$ref", "$dynamicRef", "allOf", "anyOf", "oneOf", "not", "if", "then", "else", "dependentRequired",
		"dependentSchemas", "patternProperties", "propertyNames", "unevaluatedProperties", "minProperties", "maxProperties"
	];

	private static readonly string[] PropertyKeywordsNotSupported =
	[
		"$ref", "$dynamicRef", "allOf", "not", "if", "then", "else", "const", "properties", "additionalProperties",
		"patternProperties", "propertyNames", "required", "dependentRequired", "dependentSchemas", "unevaluatedProperties",
		"minProperties", "maxProperties", "items", "prefixItems", "contains", "minContains", "maxContains", "minItems",
		"maxItems", "uniqueItems", "unevaluatedItems"
	];

	private static readonly HashSet<string> ChoiceKeywords = new(["const", "title", "description"], StringComparer.Ordinal);

	internal static MokaSchemaParseResult Read(string json)
	{
		if (json.Length > MokaFormSchema.MaxJsonLength)
		{
			return Refused(Invariant($"The schema is {json.Length} characters long; at most {MokaFormSchema.MaxJsonLength} are read."));
		}

		JsonNode? root;
		try
		{
			root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions
			{
				MaxDepth = MokaFormSchema.MaxDepth,
				AllowDuplicateProperties = false
			});
		}
		catch (JsonException exception)
		{
			return Refused($"The schema can't be read: {exception.Message}");
		}

		return root is JsonObject schema ? ReadRoot(schema, json) : Refused(NotAnObjectSchema());
	}

	internal static MokaSchemaParseResult Read(JsonObject schema)
	{
		string source;
		try
		{
			source = schema.ToJsonString();
		}
		catch (InvalidOperationException exception)
		{
			return Refused($"The schema can't be read: {exception.Message}");
		}

		return source.Length > MokaFormSchema.MaxJsonLength
			? Refused(Invariant($"The schema is {source.Length} characters long; at most {MokaFormSchema.MaxJsonLength} are read."))
			: ReadRoot(schema, source);
	}

	private static MokaSchemaParseResult ReadRoot(JsonObject root, string source)
	{
		List<string> problems = [];
		if (!TryReadType(root["type"], out string? type, out _) || type != "object")
		{
			problems.Add(NotAnObjectSchema());
		}

		foreach (string keyword in RootKeywordsNotSupported.Where(root.ContainsKey))
		{
			problems.Add($"The schema's \"{keyword}\" isn't supported. A form reads one object schema with each property written in place.");
		}

		string? title = ReadText(root, "title", SchemaWhere, problems);
		string? description = ReadText(root, "description", SchemaWhere, problems);
		bool allowsOthers = ReadAdditionalProperties(root, problems);

		JsonObject properties;
		switch (root["properties"])
		{
			case null:
				properties = new JsonObject();
				break;
			case JsonObject given:
				properties = given;
				break;
			default:
				problems.Add("The schema's \"properties\" is an object.");
				return Refused(problems);
		}

		if (properties.Count > MokaFormSchema.MaxProperties)
		{
			problems.Add(Invariant($"The schema has {properties.Count} properties; a form shows at most {MokaFormSchema.MaxProperties}."));
			return Refused(problems);
		}

		HashSet<string> required = ReadRequired(root, properties, problems);
		List<MokaSchemaField> fields = [];
		foreach ((string name, JsonNode? node) in properties)
		{
			if (ReadField(name, node, required.Contains(name), problems) is { } field)
			{
				fields.Add(field);
			}
		}

		return problems.Count == 0
			? new MokaSchemaParseResult(new MokaFormSchema(title, description, fields, allowsOthers, source), [])
			: Refused(problems);
	}

	private static bool ReadAdditionalProperties(JsonObject root, List<string> problems)
	{
		switch (root["additionalProperties"]?.GetValueKind())
		{
			case null or JsonValueKind.True:
				return true;
			case JsonValueKind.False:
				return false;
			default:
				problems.Add("The schema's \"additionalProperties\" is true or false; a schema for other properties isn't supported.");
				return true;
		}
	}

	private static HashSet<string> ReadRequired(JsonObject root, JsonObject properties, List<string> problems)
	{
		HashSet<string> required = new(StringComparer.Ordinal);
		if (root["required"] is not { } node)
		{
			return required;
		}

		if (node is not JsonArray names)
		{
			problems.Add("The schema's \"required\" is a list of property names.");
			return required;
		}

		foreach (JsonNode? item in names)
		{
			if (item is null || !MokaSchemaJson.TryGetText(item, out string? name))
			{
				problems.Add("The schema's \"required\" is a list of property names.");
			}
			else if (!required.Add(name))
			{
				problems.Add($"The schema's \"required\" names {Quote(name)} twice.");
			}
			else if (!properties.ContainsKey(name))
			{
				problems.Add($"The schema's \"required\" names {Quote(name)}, which isn't one of its properties.");
			}
		}

		return required;
	}

	private static MokaSchemaField? ReadField(string name, JsonNode? node, bool required, List<string> problems)
	{
		string where = Quote(name);
		if (name.Length is 0 or > MokaFormSchema.MaxNameLength || name.Any(char.IsControl))
		{
			problems.Add(Invariant(
				$"{where} isn't a property name a form takes: 1 to {MokaFormSchema.MaxNameLength} characters, none of them control characters."));
			return null;
		}

		if (node is not JsonObject property)
		{
			problems.Add($"{where}: its schema is an object.");
			return null;
		}

		bool hasType = TryReadType(property["type"], out string? type, out bool numberText);
		if (type is "object" or "array")
		{
			problems.Add(type == "object"
				? $"{where}: nested objects aren't supported. Give each setting its own property."
				: $"{where}: lists aren't supported. A setting is text, a number, a switch or a choice.");
			return null;
		}

		int before = problems.Count;
		foreach (string keyword in PropertyKeywordsNotSupported.Where(property.ContainsKey))
		{
			problems.Add(keyword == "$ref"
				? $"{where}: \"$ref\" isn't supported. Write the property's schema in place."
				: $"{where}: \"{keyword}\" isn't supported. A setting is text, a number, a switch or a choice.");
		}

		if (problems.Count > before)
		{
			return null;
		}

		if (!hasType)
		{
			problems.Add($"{where}: \"type\" is one type name, or a list of one type name and \"null\".");
		}

		int beforeChoices = problems.Count;
		List<MokaSchemaChoice>? choices = ReadChoices(where, property, problems);
		MokaSchemaFieldKind? kind = problems.Count > beforeChoices ? null : KindOf(where, type ?? InferType(choices), choices, problems);
		bool writeOnly = ReadFlag(property, "writeOnly", where, problems);
		bool readOnly = ReadFlag(property, "readOnly", where, problems);
		bool multiline = ReadFlag(property, MokaFormSchema.MultilineKeyword, where, problems);
		bool text = kind == MokaSchemaFieldKind.Text;
		if (writeOnly && !text)
		{
			problems.Add($"{where}: only text can be \"writeOnly\".");
		}

		if (writeOnly && readOnly)
		{
			problems.Add($"{where}: a setting can't be both \"readOnly\" and \"writeOnly\".");
		}

		if (multiline && (!text || writeOnly))
		{
			problems.Add($"{where}: only text that isn't \"writeOnly\" can be \"{MokaFormSchema.MultilineKeyword}\".");
		}

		string? title = ReadText(property, "title", where, problems);
		int? minLength = text ? ReadCount(property, "minLength", where, problems) : null;
		int? maxLength = text ? ReadCount(property, "maxLength", where, problems) : null;
		if (minLength > maxLength)
		{
			problems.Add($"{where}: \"minLength\" is more than \"maxLength\".");
		}

		string? pattern = text ? ReadText(property, "pattern", where, problems, MokaFormSchema.MaxPatternLength) : null;
		bool numeric = kind is MokaSchemaFieldKind.WholeNumber or MokaSchemaFieldKind.Number;
		double? multipleOf = numeric ? ReadNumber(property, "multipleOf", where, problems) : null;
		if (multipleOf <= 0)
		{
			problems.Add($"{where}: \"multipleOf\" is more than 0.");
		}

		MokaSchemaField field = new()
		{
			Name = name,
			Title = string.IsNullOrEmpty(title) ? name : title,
			Description = ReadText(property, "description", where, problems),
			Kind = kind ?? MokaSchemaFieldKind.Text,
			TakesNumberText = numberText && numeric,
			Required = required,
			ReadOnly = readOnly,
			WriteOnly = writeOnly,
			Advanced = ReadFlag(property, MokaFormSchema.AdvancedKeyword, where, problems),
			Multiline = multiline,
			Placeholder = ReadPlaceholder(property, where, problems),
			Format = text ? ReadText(property, "format", where, problems) : null,
			Pattern = pattern,
			PatternRegex = ReadPattern(pattern, where, problems),
			MinLength = minLength,
			MaxLength = maxLength,
			Minimum = numeric ? ReadNumber(property, "minimum", where, problems) : null,
			Maximum = numeric ? ReadNumber(property, "maximum", where, problems) : null,
			ExclusiveMinimum = numeric ? ReadExclusive(property, "exclusiveMinimum", where, problems) : null,
			ExclusiveMaximum = numeric ? ReadExclusive(property, "exclusiveMaximum", where, problems) : null,
			MultipleOf = multipleOf,
			ExactMultipleOf = multipleOf > 0 ? ExactNumber(property["multipleOf"]!) : null,
			Choices = choices ?? []
		};

		if (problems.Count > before || kind is null)
		{
			return null;
		}

		return ReadDefault(field, property, where, problems);
	}

	private static MokaSchemaField? ReadDefault(MokaSchemaField field, JsonObject property, string where, List<string> problems)
	{
		if (property["default"] is not { } fallback || fallback.GetValueKind() == JsonValueKind.Null)
		{
			return field;
		}

		if (field.WriteOnly)
		{
			problems.Add($"{where}: a \"writeOnly\" setting has no \"default\".");
			return null;
		}

		string? problem = MokaSchemaJson.IsPrimitive(fallback)
			? field.Problem(fallback, MokaSchemaFormText.Default)
			: "It's text, a number, or true or false.";
		if (problem is not null)
		{
			problems.Add($"{where}: its \"default\" doesn't fit the setting. {problem}");
			return null;
		}

		field.TakeDefault(fallback);
		return field;
	}

	// One type name, or a list of one and "null", as a nullable property exported from .NET is written. .NET's
	// JsonSchemaExporter writes a number that may also be read from text, under JsonNumberHandling.AllowReadingFromString
	// (the web defaults), as ["string", "integer"] with a pattern for the text; that is still a number to the form.
	private static bool TryReadType(JsonNode? node, out string? type, out bool numberText)
	{
		type = null;
		numberText = false;
		if (node is null)
		{
			return true;
		}

		if (MokaSchemaJson.TryGetText(node, out string? single))
		{
			type = single;
			return single != "null";
		}

		if (node is not JsonArray { Count: >= 1 and <= 3 } list)
		{
			return false;
		}

		HashSet<string> names = new(StringComparer.Ordinal);
		foreach (JsonNode? item in list)
		{
			if (item is null || !MokaSchemaJson.TryGetText(item, out string? name) || !names.Add(name))
			{
				return false;
			}
		}

		names.Remove("null");
		if (names.Count == 1)
		{
			type = names.Single();
			return true;
		}

		if (names.Count == 2 && names.Contains("string") && (names.Contains("integer") || names.Contains("number")))
		{
			type = names.Contains("integer") ? "integer" : "number";
			numberText = true;
			return true;
		}

		return false;
	}

	private static List<MokaSchemaChoice>? ReadChoices(string where, JsonObject property, List<string> problems)
	{
		bool hasEnum = property.ContainsKey("enum");
		string? labelled = property.ContainsKey("oneOf") ? "oneOf" : property.ContainsKey("anyOf") ? "anyOf" : null;
		if (property.ContainsKey("oneOf") && property.ContainsKey("anyOf"))
		{
			problems.Add($"{where}: give the choices as \"oneOf\" or \"anyOf\", not both.");
			return null;
		}

		if (hasEnum && labelled is not null)
		{
			problems.Add($"{where}: give the choices as \"enum\" or \"{labelled}\", not both.");
			return null;
		}

		if (!hasEnum && labelled is null)
		{
			return null;
		}

		string keyword = labelled ?? "enum";
		if (property[keyword] is not JsonArray { Count: > 0 } items)
		{
			problems.Add($"{where}: \"{keyword}\" is a list with at least one choice.");
			return null;
		}

		if (items.Count > MokaFormSchema.MaxChoices)
		{
			problems.Add(Invariant($"{where}: \"{keyword}\" has {items.Count} choices; a setting takes at most {MokaFormSchema.MaxChoices}."));
			return null;
		}

		List<MokaSchemaChoice> choices = [];
		HashSet<string> seen = new(StringComparer.Ordinal);
		foreach (JsonNode? item in items)
		{
			int before = problems.Count;
			(JsonNode Value, string Label)? read = labelled is not null
				? ReadLabelledChoice(where, labelled, item, problems)
				: ReadEnumChoice(where, item, problems);
			if (problems.Count > before)
			{
				return null;
			}

			if (read is not { } choice)
			{
				continue;
			}

			string key = MokaSchemaJson.KeyOf(choice.Value)!;
			if (!seen.Add(key))
			{
				problems.Add($"{where}: \"{keyword}\" lists {MokaSchemaJson.Display(choice.Value)} twice.");
				return null;
			}

			choices.Add(new MokaSchemaChoice(choice.Value.DeepClone(), key, choice.Label));
		}

		return choices;
	}

	// null in the list means the setting may be left unset, as .NET writes a nullable enum. The form shows that as "Not set".
	private static (JsonNode Value, string Label)? ReadEnumChoice(string where, JsonNode? item, List<string> problems)
	{
		if (item is null || item.GetValueKind() == JsonValueKind.Null)
		{
			return null;
		}

		if (MokaSchemaJson.KeyOf(item) is null)
		{
			problems.Add($"{where}: \"enum\" lists text or numbers.");
			return null;
		}

		return ChoiceText(where, item, null, problems);
	}

	private static (JsonNode Value, string Label)? ReadLabelledChoice(string where, string keyword, JsonNode? item,
		List<string> problems)
	{
		if (item is not JsonObject choice || !choice.ContainsKey("const") || choice.Any(entry => !ChoiceKeywords.Contains(entry.Key)))
		{
			problems.Add($"{where}: \"{keyword}\" is read as labelled choices only: {{ \"const\": value, \"title\": \"Label\" }}.");
			return null;
		}

		JsonNode? value = choice["const"];
		if (value is null || value.GetValueKind() == JsonValueKind.Null)
		{
			return null;
		}

		if (MokaSchemaJson.KeyOf(value) is null)
		{
			problems.Add($"{where}: each \"const\" in \"{keyword}\" is text or a number.");
			return null;
		}

		return ChoiceText(where, value, ReadText(choice, "title", where, problems), problems);
	}

	private static (JsonNode Value, string Label)? ChoiceText(string where, JsonNode value, string? title, List<string> problems)
	{
		string label = string.IsNullOrEmpty(title) ? MokaSchemaJson.Display(value) : title;
		if (label.Length > MokaFormSchema.MaxTextLength)
		{
			problems.Add(Invariant($"{where}: a choice is longer than {MokaFormSchema.MaxTextLength} characters."));
			return null;
		}

		return (value, label);
	}

	private static string? InferType(IReadOnlyList<MokaSchemaChoice>? choices)
	{
		if (choices is not { Count: > 0 })
		{
			return null;
		}

		if (choices.All(choice => MokaSchemaJson.TryGetText(choice.Node, out _)))
		{
			return "string";
		}

		return choices.All(choice => MokaSchemaJson.TryGetNumber(choice.Node, out _, out _))
			? choices.All(choice => MokaSchemaJson.TryGetNumber(choice.Node, out double number, out _) && MokaSchemaField.IsWhole(number))
				? "integer"
				: "number"
			: null;
	}

	private static MokaSchemaFieldKind? KindOf(string where, string? type, IReadOnlyList<MokaSchemaChoice>? choices,
		List<string> problems)
	{
		if (choices is not null)
		{
			bool fits = type switch
			{
				"string" => choices.All(choice => MokaSchemaJson.TryGetText(choice.Node, out _)),
				"integer" => choices.All(choice =>
					MokaSchemaJson.TryGetNumber(choice.Node, out double number, out _) && MokaSchemaField.IsWhole(number)),
				"number" => choices.All(choice => MokaSchemaJson.TryGetNumber(choice.Node, out _, out _)),
				_ => false
			};
			if (fits)
			{
				return MokaSchemaFieldKind.Choice;
			}

			problems.Add(type is null
				? $"{where}: its choices are all text or all numbers."
				: $"{where}: its choices don't all fit \"type\": \"{type}\".");
			return null;
		}

		switch (type)
		{
			case "string":
				return MokaSchemaFieldKind.Text;
			case "integer":
				return MokaSchemaFieldKind.WholeNumber;
			case "number":
				return MokaSchemaFieldKind.Number;
			case "boolean":
				return MokaSchemaFieldKind.Toggle;
			case null:
				problems.Add($"{where}: it has no \"type\".");
				return null;
			default:
				problems.Add($"{where}: \"{type}\" isn't a type a setting takes.");
				return null;
		}
	}

	private static string? ReadPlaceholder(JsonObject property, string where, List<string> problems)
	{
		if (property["examples"] is not { } examples)
		{
			return null;
		}

		if (examples is not JsonArray list)
		{
			problems.Add($"{where}: \"examples\" is a list.");
			return null;
		}

		if (list.Count == 0 || list[0] is not { } first || !MokaSchemaJson.IsPrimitive(first))
		{
			return null;
		}

		string placeholder = MokaSchemaJson.Display(first);
		if (placeholder.Length > MokaFormSchema.MaxTextLength)
		{
			problems.Add(Invariant($"{where}: its first example is longer than {MokaFormSchema.MaxTextLength} characters."));
			return null;
		}

		return placeholder;
	}

	private static Regex? ReadPattern(string? pattern, string where, List<string> problems)
	{
		if (pattern is null)
		{
			return null;
		}

		try
		{
			return new Regex(pattern, RegexOptions.NonBacktracking | RegexOptions.CultureInvariant, MokaFormSchema.PatternTimeout);
		}
		catch (ArgumentException)
		{
			problems.Add($"{where}: its \"pattern\" isn't a regular expression.");
		}
		catch (NotSupportedException)
		{
			problems.Add($"{where}: its \"pattern\" needs backtracking (a lookaround or a backreference) or is too large to run safely.");
		}

		return null;
	}

	private static string? ReadText(JsonObject node, string keyword, string where, List<string> problems,
		int longest = MokaFormSchema.MaxTextLength)
	{
		if (node[keyword] is not { } value)
		{
			return null;
		}

		if (!MokaSchemaJson.TryGetText(value, out string? text))
		{
			problems.Add($"{where}: \"{keyword}\" is text.");
			return null;
		}

		if (text.Length > longest)
		{
			problems.Add(Invariant($"{where}: \"{keyword}\" is longer than {longest} characters."));
			return null;
		}

		return text;
	}

	private static bool ReadFlag(JsonObject node, string keyword, string where, List<string> problems)
	{
		switch (node[keyword]?.GetValueKind())
		{
			case null or JsonValueKind.False:
				return false;
			case JsonValueKind.True:
				return true;
			default:
				problems.Add($"{where}: \"{keyword}\" is true or false.");
				return false;
		}
	}

	private static double? ReadNumber(JsonObject node, string keyword, string where, List<string> problems)
	{
		if (node[keyword] is not { } value)
		{
			return null;
		}

		if (MokaSchemaJson.TryGetNumber(value, out double number, out _))
		{
			return number;
		}

		problems.Add($"{where}: \"{keyword}\" is a number.");
		return null;
	}

	// Draft 4 wrote exclusiveMinimum as true beside minimum; 2020-12 writes the limit itself.
	private static double? ReadExclusive(JsonObject node, string keyword, string where, List<string> problems)
	{
		if (node[keyword]?.GetValueKind() is JsonValueKind.True or JsonValueKind.False)
		{
			problems.Add($"{where}: \"{keyword}\" is the limit itself, a number, as JSON Schema 2020-12 writes it.");
			return null;
		}

		return ReadNumber(node, keyword, where, problems);
	}

	private static int? ReadCount(JsonObject node, string keyword, string where, List<string> problems)
	{
		if (node[keyword] is not { } value)
		{
			return null;
		}

		if (MokaSchemaJson.TryGetNumber(value, out double number, out _) && number is >= 0 and <= int.MaxValue &&
		    Math.Floor(number) == number)
		{
			return (int)number;
		}

		problems.Add($"{where}: \"{keyword}\" is a whole number of 0 or more.");
		return null;
	}

	private static decimal? ExactNumber(JsonNode node) =>
		MokaSchemaJson.TryGetNumber(node, out _, out string raw) &&
		decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal exact)
			? exact
			: null;

	private static string NotAnObjectSchema() =>
		"The schema is an object with \"type\": \"object\" and its settings under \"properties\".";

	private static string Quote(string name) => name.Length > 60 ? $"\"{name[..60]}...\"" : $"\"{name}\"";

	private static string Invariant(FormattableString text) => FormattableString.Invariant(text);

	private static MokaSchemaParseResult Refused(string problem) => new(null, [problem]);

	private static MokaSchemaParseResult Refused(List<string> problems) => new(null, problems);
}
