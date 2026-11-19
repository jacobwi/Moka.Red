using System.Text.Json.Nodes;

namespace Moka.Red.Forms.SchemaForm;

/// <summary>
///     A form read from a JSON Schema (draft 2020-12 keywords): an object schema whose properties are text, whole numbers,
///     numbers, switches and choices. Read one with <see cref="Parse(string)" />, show it with <see cref="MokaSchemaForm" />,
///     and check what comes back on the server with <see cref="Check" />.
/// </summary>
/// <remarks>
///     A schema from a plugin or a connector is untrusted input, so reading one is bounded: at most
///     <see cref="MaxJsonLength" /> characters nested <see cref="MaxDepth" /> deep, <see cref="MaxProperties" /> properties,
///     <see cref="MaxChoices" /> choices each, names of <see cref="MaxNameLength" /> characters, texts of
///     <see cref="MaxTextLength" />, and patterns of <see cref="MaxPatternLength" /> that run without backtracking and stop
///     after <see cref="PatternTimeout" />. Past any limit the schema is refused with a problem; nothing throws.
/// </remarks>
public sealed class MokaFormSchema
{
	/// <summary>The most characters of JSON <see cref="Parse(string)" /> reads.</summary>
	public const int MaxJsonLength = 64 * 1024;

	/// <summary>How deep the JSON may nest. The deepest a form needs is a labelled choice, six levels down.</summary>
	public const int MaxDepth = 16;

	/// <summary>The most properties a schema may have.</summary>
	public const int MaxProperties = 100;

	/// <summary>The most choices one property may have.</summary>
	public const int MaxChoices = 250;

	/// <summary>The longest property name.</summary>
	public const int MaxNameLength = 128;

	/// <summary>The longest title, description, example, default or choice text.</summary>
	public const int MaxTextLength = 4000;

	/// <summary>The longest <c>pattern</c>.</summary>
	public const int MaxPatternLength = 1000;

	/// <summary>
	///     The extension keyword that puts a setting in the advanced group, hidden until "Show advanced":
	///     <c>"x-advanced": true</c>. JSON Schema has no keyword for it, and the <c>x-</c> prefix keeps it apart from any it
	///     may get.
	/// </summary>
	public const string AdvancedKeyword = "x-advanced";

	/// <summary>The extension keyword that draws a text setting as a text area: <c>"x-multiline": true</c>.</summary>
	public const string MultilineKeyword = "x-multiline";

	private readonly Dictionary<string, MokaSchemaField> _byName;
	private readonly string _source;

	internal MokaFormSchema(string? title, string? description, IReadOnlyList<MokaSchemaField> fields, bool allowsOtherProperties,
		string source)
	{
		Title = title;
		Description = description;
		Fields = fields;
		AllowsOtherProperties = allowsOtherProperties;
		_source = source;
		_byName = fields.ToDictionary(field => field.Name, StringComparer.Ordinal);
	}

	/// <summary>How long a <c>pattern</c> may run on one value before the value counts as not matching.</summary>
	public static TimeSpan PatternTimeout { get; } = TimeSpan.FromMilliseconds(50);

	/// <summary>The schema's <c>title</c>, the form's title unless the form is given one.</summary>
	public string? Title { get; }

	/// <summary>The schema's <c>description</c>, the form's lead unless the form is given one.</summary>
	public string? Description { get; }

	/// <summary>The settings, in the order the schema's <c>properties</c> list them.</summary>
	public IReadOnlyList<MokaSchemaField> Fields { get; }

	/// <summary>False when the schema says <c>"additionalProperties": false</c>, so <see cref="Check" /> refuses other properties.</summary>
	public bool AllowsOtherProperties { get; }

	/// <summary>The setting with this property name, or null.</summary>
	/// <param name="name">The property's name, compared exactly.</param>
	public MokaSchemaField? Field(string name) => _byName.GetValueOrDefault(name);

	/// <summary>Reads a schema from JSON text. Bad input gives problems, never an exception.</summary>
	/// <param name="json">The schema as JSON.</param>
	/// <returns>The schema, or the problems that kept it from being one.</returns>
	public static MokaSchemaParseResult Parse(string json)
	{
		ArgumentNullException.ThrowIfNull(json);
		return MokaSchemaReader.Read(json);
	}

	/// <summary>
	///     Reads a schema already parsed, such as one .NET's <c>JsonSchemaExporter</c> made from a type
	///     (<c>GetJsonSchemaAsNode(typeof(T)).AsObject()</c>). Bad input gives problems, never an exception.
	/// </summary>
	/// <param name="schema">The schema's root object. It isn't changed or kept.</param>
	/// <returns>The schema, or the problems that kept it from being one.</returns>
	public static MokaSchemaParseResult Parse(JsonObject schema)
	{
		ArgumentNullException.ThrowIfNull(schema);
		return MokaSchemaReader.Read(schema);
	}

	/// <summary>
	///     A copy of <paramref name="values" /> where each setting that isn't set takes its <c>default</c>. Missing,
	///     <c>null</c> and empty text all count as not set. Write-only settings get no default, and other properties are
	///     copied as they are.
	/// </summary>
	/// <param name="values">The settings, or null for none.</param>
	/// <returns>A new object; <paramref name="values" /> isn't changed.</returns>
	public JsonObject WithDefaults(JsonObject? values)
	{
		JsonObject filled = values?.DeepClone().AsObject() ?? new JsonObject();
		foreach (MokaSchemaField field in Fields)
		{
			if (field.DefaultNode is { } fallback && MokaSchemaJson.IsUnset(filled[field.Name]))
			{
				filled[field.Name] = fallback.DeepClone();
			}
		}

		return filled;
	}

	/// <summary>
	///     What's wrong with <paramref name="values" />, by property name, with the same rules and words the form uses, so
	///     a server can check what it's sent. Missing, <c>null</c> and empty text count as not set. Read-only settings
	///     aren't checked, since nobody can change them. Call <see cref="WithDefaults" /> first when a setting left out
	///     means its default.
	/// </summary>
	/// <param name="values">The settings to check.</param>
	/// <param name="setSecrets">Write-only settings that hold a value where they're kept, so leaving them out is fine.</param>
	/// <param name="text">The words of the problems. Null takes <see cref="MokaSchemaFormText.Default" />.</param>
	/// <returns>The first problem of each setting that has one; empty when everything fits.</returns>
	public IReadOnlyDictionary<string, string> Check(JsonObject values, IReadOnlyCollection<string>? setSecrets = null,
		MokaSchemaFormText? text = null)
	{
		ArgumentNullException.ThrowIfNull(values);
		MokaSchemaFormText words = text ?? MokaSchemaFormText.Default;
		Dictionary<string, string> problems = new(StringComparer.Ordinal);
		foreach (MokaSchemaField field in Fields)
		{
			if (field.ReadOnly)
			{
				continue;
			}

			JsonNode? value = values[field.Name];
			if (MokaSchemaJson.IsUnset(value))
			{
				bool kept = field.WriteOnly && setSecrets?.Contains(field.Name) == true;
				if (field.Required && !kept)
				{
					problems[field.Name] = words.Needed(field.Title);
				}

				continue;
			}

			if (field.Problem(value!, words) is { } problem)
			{
				problems[field.Name] = problem;
			}
		}

		if (!AllowsOtherProperties)
		{
			foreach ((string name, _) in values)
			{
				if (!_byName.ContainsKey(name))
				{
					problems[name] = words.NotAllowed(name);
				}
			}
		}

		return problems;
	}

	// Two schemas read from the same JSON are the same form, so a page that reads its schema again keeps what's typed.
	internal bool SameAs(MokaFormSchema? other) =>
		other is not null && (ReferenceEquals(this, other) || string.Equals(_source, other._source, StringComparison.Ordinal));
}
