using System.Text.Json.Nodes;
using Moka.Red.Forms.SchemaForm;

namespace Moka.Red.Forms.Tests.Components;

public class MokaFormSchemaCheckTests
{
	private static MokaFormSchema Schema(string json)
	{
		MokaSchemaParseResult result = MokaFormSchema.Parse(json);
		Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Problems));
		return result.Schema;
	}

	private static MokaFormSchema One(string property, bool required = false) =>
		Schema($$"""{ "type": "object", {{(required ? "\"required\": [\"p\"]," : "")}} "properties": { "p": {{property}} } }""");

	private static string? ProblemOf(string property, string value)
	{
		IReadOnlyDictionary<string, string> problems = One(property).Check(Values(value));
		return problems.GetValueOrDefault("p");
	}

	private static JsonObject Values(string value) => JsonNode.Parse($$"""{ "p": {{value}} }""")!.AsObject();

	[Theory]
	[InlineData("{}")]
	[InlineData("""{ "p": null }""")]
	[InlineData("""{ "p": "" }""")]
	public void ARequiredSettingNotSet_IsNeeded(string values)
	{
		MokaFormSchema schema = One("""{ "type": "string", "title": "Host" }""", true);

		Assert.Equal("Host is needed.", schema.Check(JsonNode.Parse(values)!.AsObject())["p"]);
	}

	[Fact]
	public void ASettingNotRequired_MayBeLeftOut() =>
		Assert.Empty(One("""{ "type": "string", "minLength": 3 }""").Check([]));

	[Theory]
	[InlineData("""{ "type": "string" }""", "5", "p takes text.")]
	[InlineData("""{ "type": "string", "minLength": 3 }""", "\"ab\"", "p needs at least 3 characters.")]
	[InlineData("""{ "type": "string", "maxLength": 2 }""", "\"abc\"", "p takes at most 2 characters.")]
	[InlineData("""{ "type": "string", "pattern": "^[0-9]+$" }""", "\"12a\"", "p isn't in the form it takes.")]
	[InlineData("""{ "type": "integer" }""", "1.5", "p takes a whole number.")]
	[InlineData("""{ "type": "integer" }""", "\"5\"", "p takes a number.")]
	[InlineData("""{ "type": "integer" }""", "1e30", "p takes a whole number.")]
	[InlineData("""{ "type": "number", "minimum": 1 }""", "0.5", "p is 1 or more.")]
	[InlineData("""{ "type": "number", "maximum": 1 }""", "1.5", "p is 1 or less.")]
	[InlineData("""{ "type": "number", "exclusiveMinimum": 0 }""", "0", "p is more than 0.")]
	[InlineData("""{ "type": "number", "exclusiveMaximum": 10 }""", "10", "p is less than 10.")]
	[InlineData("""{ "type": "integer", "multipleOf": 5 }""", "7", "p goes in steps of 5.")]
	[InlineData("""{ "type": "boolean" }""", "\"yes\"", "p is on or off.")]
	[InlineData("""{ "type": ["integer", "string"] }""", "\"80x\"", "p takes a number.")]
	[InlineData("""{ "enum": ["a", "b"] }""", "\"c\"", "p takes one of its choices.")]
	[InlineData("""{ "enum": [1, 2] }""", "\"1\"", "p takes one of its choices.")]
	public void AValueThatDoesntFit_SaysWhy(string property, string value, string problem) =>
		Assert.Equal(problem, ProblemOf(property, value));

	[Theory]
	[InlineData("""{ "type": "string", "pattern": "[0-9]" }""", "\"a1b\"")]
	[InlineData("""{ "type": "string", "minLength": 2, "maxLength": 2 }""", "\"\uD83D\uDE00\uD83D\uDE00\"")]
	[InlineData("""{ "type": "integer" }""", "2.0")]
	[InlineData("""{ "type": "integer", "minimum": 1, "maximum": 3 }""", "3")]
	[InlineData("""{ "type": "number", "multipleOf": 0.1 }""", "0.3")]
	[InlineData("""{ "type": "number", "multipleOf": 0.01 }""", "19.99")]
	[InlineData("""{ "type": "boolean" }""", "false")]
	[InlineData("""{ "type": ["string", "integer", "null"] }""", "\"8080\"")]
	[InlineData("""{ "enum": [1, 2] }""", "1.0")]
	[InlineData("""{ "type": "number", "oneOf": [ { "const": 2.5, "title": "Half" } ] }""", "2.50")]
	[InlineData("""{ "type": "string", "format": "date-time" }""", "\"not a date\"")]
	public void AValueThatFits_HasNoProblem(string property, string value) => Assert.Null(ProblemOf(property, value));

	[Theory]
	[InlineData("email", "name@example.com", true)]
	[InlineData("email", "Name <name@example.com>", false)]
	[InlineData("email", "name.example.com", false)]
	[InlineData("uri", "https://example.com/path?q=1", true)]
	[InlineData("uri", "mailto:name@example.com", true)]
	[InlineData("uri", "example.com", false)]
	[InlineData("uri", "/relative/path", false)]
	[InlineData("uri", "C:\\\\backups", false)]
	[InlineData("uri", "https://exa mple.com", false)]
	[InlineData("hostname", "mail.example.com", true)]
	[InlineData("hostname", "localhost", true)]
	[InlineData("hostname", "-bad.example.com", false)]
	[InlineData("hostname", "under_score.example", false)]
	[InlineData("hostname", "a..b", false)]
	[InlineData("ipv4", "192.0.2.10", true)]
	[InlineData("ipv4", "192.0.2.256", false)]
	[InlineData("ipv4", "192.0.02.1", false)]
	[InlineData("ipv4", "1", false)]
	[InlineData("ipv6", "2001:db8::1", true)]
	[InlineData("ipv6", "192.0.2.10", false)]
	[InlineData("ipv6", "fe80::1%eth0", false)]
	[InlineData("uuid", "123e4567-e89b-12d3-a456-426614174000", true)]
	[InlineData("uuid", "123e4567e89b12d3a456426614174000", false)]
	public void Formats_AreChecked(string format, string text, bool fits)
	{
		string? problem = ProblemOf($$"""{ "type": "string", "format": "{{format}}" }""", $"\"{text}\"");

		Assert.Equal(fits, problem is null);
	}

	[Fact]
	public void ReadOnlySettings_AreNotChecked() =>
		Assert.Empty(One("""{ "type": "integer", "minimum": 5, "readOnly": true }""", true).Check(Values("1")));

	[Fact]
	public void AWriteOnlySettingLeftOut_IsFine_WhenItsSetWhereItsKept()
	{
		MokaFormSchema schema = One("""{ "type": "string", "writeOnly": true, "title": "API key" }""", true);

		Assert.Equal("API key is needed.", schema.Check([])["p"]);
		Assert.Empty(schema.Check([], ["p"]));
	}

	[Fact]
	public void OtherProperties_AreRefused_OnlyWhenTheSchemaSaysSo()
	{
		JsonObject values = JsonNode.Parse("""{ "p": "a", "extra": 1 }""")!.AsObject();

		Assert.Empty(One("""{ "type": "string" }""").Check(values));
		Assert.Equal("There's no setting called extra.",
			Schema("""{ "type": "object", "additionalProperties": false, "properties": { "p": { "type": "string" } } }""")
				.Check(values)["extra"]);
	}

	[Fact]
	public void TheWords_CanBeReplaced()
	{
		MokaSchemaFormText text = MokaSchemaFormText.Default with { Needed = title => $"Fill in {title}" };

		Assert.Equal("Fill in Host", One("""{ "type": "string", "title": "Host" }""", true).Check([], null, text)["p"]);
	}

	[Fact]
	public void ValuesBuiltInCode_AreReadLikeParsedOnes()
	{
		MokaFormSchema schema = Schema("""
			{ "type": "object", "properties": { "n": { "type": "integer", "maximum": 5 }, "c": { "type": "string", "maxLength": 1 },
			  "g": { "type": "string", "format": "uuid" }, "d": { "type": "number", "multipleOf": 0.5 } } }
			""");
		JsonObject values = new()
		{
			["n"] = JsonValue.Create(4),
			["c"] = JsonValue.Create('x'),
			["g"] = JsonValue.Create(Guid.Parse("123e4567-e89b-12d3-a456-426614174000")),
			["d"] = JsonValue.Create(1.5m)
		};

		Assert.Empty(schema.Check(values));
	}

	[Fact]
	public void WithDefaults_FillsWhatIsntSet_AndKeepsTheRest()
	{
		MokaFormSchema schema = Schema("""
			{ "type": "object", "properties": {
				"a": { "type": "string", "default": "x" }, "b": { "type": "integer", "default": 3 },
				"c": { "type": "boolean", "default": true }, "d": { "type": "string", "default": "y" },
				"e": { "type": "string" }, "k": { "type": "string", "writeOnly": true } } }
			""");
		JsonObject values = JsonNode.Parse("""{ "a": "", "b": null, "d": "kept", "other": [1] }""")!.AsObject();
		string before = values.ToJsonString();

		JsonObject filled = schema.WithDefaults(values);

		Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{ "a": "x", "b": 3, "d": "kept", "other": [1], "c": true }"""), filled));
		Assert.Equal(before, values.ToJsonString());
		Assert.DoesNotContain(schema.WithDefaults(null), entry => entry.Key is "e" or "k");
	}
}
