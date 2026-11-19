using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Moka.Red.Forms.SchemaForm;

namespace Moka.Red.Forms.Tests.Components;

public class MokaFormSchemaParseTests
{
	private static MokaFormSchema Read(string json)
	{
		MokaSchemaParseResult result = MokaFormSchema.Parse(json);
		Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Problems));
		Assert.Empty(result.Problems);
		return result.Schema;
	}

	private static MokaSchemaField One(string property) =>
		Read($$"""{ "type": "object", "properties": { "p": {{property}} } }""").Fields.Single();

	private static string Problem(string json)
	{
		MokaSchemaParseResult result = MokaFormSchema.Parse(json);
		Assert.False(result.Succeeded);
		Assert.Null(result.Schema);
		return Assert.Single(result.Problems);
	}

	private static string PropertyProblem(string property) =>
		Problem($$"""{ "type": "object", "properties": { "p": {{property}} } }""");

	[Fact]
	public void TheSchema_KeepsItsTitleDescriptionAndPropertyOrder()
	{
		MokaFormSchema schema = Read("""
			{
				"type": "object",
				"title": "Backup job",
				"description": "Where copies go.",
				"properties": { "target": { "type": "string" }, "keep": { "type": "integer" }, "on": { "type": "boolean" } }
			}
			""");

		Assert.Equal("Backup job", schema.Title);
		Assert.Equal("Where copies go.", schema.Description);
		Assert.Equal(["target", "keep", "on"], schema.Fields.Select(field => field.Name));
		Assert.Same(schema.Fields[1], schema.Field("keep"));
		Assert.Null(schema.Field("Keep"));
		Assert.True(schema.AllowsOtherProperties);
	}

	[Theory]
	[InlineData("""{ "type": "string" }""", MokaSchemaFieldKind.Text)]
	[InlineData("""{ "type": "integer" }""", MokaSchemaFieldKind.WholeNumber)]
	[InlineData("""{ "type": "number" }""", MokaSchemaFieldKind.Number)]
	[InlineData("""{ "type": "boolean" }""", MokaSchemaFieldKind.Toggle)]
	[InlineData("""{ "type": "string", "enum": ["a", "b"] }""", MokaSchemaFieldKind.Choice)]
	[InlineData("""{ "type": ["string", "null"] }""", MokaSchemaFieldKind.Text)]
	[InlineData("""{ "type": ["null", "integer"] }""", MokaSchemaFieldKind.WholeNumber)]
	[InlineData("""{ "enum": ["a", "b", null] }""", MokaSchemaFieldKind.Choice)]
	public void EachType_GivesItsKind(string property, MokaSchemaFieldKind kind) => Assert.Equal(kind, One(property).Kind);

	[Fact]
	public void AFieldWithoutATitle_IsNamedAfterItsProperty()
	{
		MokaSchemaField field = One("""{ "type": "string", "description": "Where copies go." }""");

		Assert.Equal("p", field.Title);
		Assert.Equal("Where copies go.", field.Description);
	}

	[Fact]
	public void TheKeywords_AreRead()
	{
		MokaFormSchema schema = Read("""
			{
				"type": "object",
				"required": ["host"],
				"properties": {
					"host": { "type": "string", "title": "Host", "format": "hostname", "minLength": 3, "maxLength": 20,
					          "pattern": "^[a-z.]+$", "examples": ["mail.example.com", "other"], "default": "smtp.example" },
					"port": { "type": "integer", "minimum": 1, "maximum": 65535, "multipleOf": 1, "readOnly": true },
					"ratio": { "type": "number", "exclusiveMinimum": 0, "exclusiveMaximum": 1, "examples": [0.5] },
					"key": { "type": "string", "writeOnly": true, "x-advanced": true },
					"notes": { "type": "string", "x-multiline": true }
				}
			}
			""");

		MokaSchemaField host = schema.Field("host")!;
		Assert.True(host.Required);
		Assert.Equal("Host", host.Title);
		Assert.Equal("hostname", host.Format);
		Assert.Equal(3, host.MinLength);
		Assert.Equal(20, host.MaxLength);
		Assert.Equal("^[a-z.]+$", host.Pattern);
		Assert.Equal("mail.example.com", host.Placeholder);
		Assert.Equal("smtp.example", host.Default!.Value.GetString());

		MokaSchemaField port = schema.Field("port")!;
		Assert.False(port.Required);
		Assert.True(port.ReadOnly);
		Assert.Equal(1, port.Minimum);
		Assert.Equal(65535, port.Maximum);
		Assert.Equal(1, port.MultipleOf);
		Assert.Null(port.Default);

		MokaSchemaField ratio = schema.Field("ratio")!;
		Assert.Equal(0, ratio.ExclusiveMinimum);
		Assert.Equal(1, ratio.ExclusiveMaximum);
		Assert.Equal("0.5", ratio.Placeholder);

		MokaSchemaField key = schema.Field("key")!;
		Assert.True(key.WriteOnly);
		Assert.True(key.Advanced);
		Assert.True(schema.Field("notes")!.Multiline);
	}

	[Fact]
	public void LabelledChoices_TakeTheirTitles_AndEnumsTheirValues()
	{
		MokaSchemaField labelled = One("""
			{ "type": "string", "oneOf": [ { "const": "daily", "title": "Every day" }, { "const": "weekly" } ] }
			""");
		MokaSchemaField listed = One("""{ "type": "integer", "anyOf": [ { "const": 1, "title": "One", "description": "x" } ] }""");
		MokaSchemaField plain = One("""{ "enum": [10, 2.5] }""");

		Assert.Equal(["Every day", "weekly"], labelled.Choices.Select(choice => choice.Label));
		Assert.Equal("daily", labelled.Choices[0].Value.GetString());
		Assert.Equal(1, listed.Choices.Single().Value.GetInt32());
		Assert.Equal(["10", "2.5"], plain.Choices.Select(choice => choice.Label));
		Assert.Equal(MokaSchemaFieldKind.Choice, plain.Kind);
	}

	[Fact]
	public void UnknownKeywords_AreIgnored()
	{
		MokaFormSchema schema = Read("""
			{
				"$schema": "https://json-schema.org/draft/2020-12/schema",
				"$id": "https://example.com/backup",
				"$comment": "ignored",
				"type": "object",
				"x-owner": "backups",
				"properties": { "p": { "type": "string", "deprecated": true, "x-widget": "fancy", "format": "date-time" } }
			}
			""");

		Assert.Equal("date-time", schema.Fields.Single().Format);
	}

	[Fact]
	public void AnObjectTypeWithNull_IsTheRootAJsonSchemaExporterWrites()
	{
		MokaFormSchema schema = Read("""{ "type": ["object", "null"], "properties": { "p": { "type": "boolean" } } }""");

		Assert.Single(schema.Fields);
	}

	[Fact]
	public void NoProperties_IsAFormWithNothingToSet() =>
		Assert.Empty(Read("""{ "type": "object" }""").Fields);

	[Fact]
	public void AdditionalPropertiesFalse_IsKept() =>
		Assert.False(Read("""{ "type": "object", "additionalProperties": false, "properties": {} }""").AllowsOtherProperties);

	[Fact]
	public void ParsingAnObject_ReadsTheSame_AndLeavesItAlone()
	{
		JsonObject node = JsonNode.Parse("""{ "type": "object", "properties": { "p": { "type": "number", "default": 2 } } }""")!
			.AsObject();
		string before = node.ToJsonString();

		MokaSchemaParseResult result = MokaFormSchema.Parse(node);

		Assert.True(result.Succeeded);
		Assert.Equal(2, result.Schema.Fields.Single().Default!.Value.GetDouble());
		Assert.Equal(before, node.ToJsonString());
	}

	[Theory]
	[InlineData("not json")]
	[InlineData("{ \"type\": \"object\", ")]
	[InlineData("[1, 2]")]
	[InlineData("\"text\"")]
	[InlineData("""{ "type": "string" }""")]
	[InlineData("""{ "properties": {} }""")]
	public void BadInput_GivesAProblem_NotAnException(string json) => Assert.NotEmpty(Problem(json));

	[Fact]
	public void ARepeatedPropertyName_IsRefused() =>
		Assert.Contains("duplicate", Problem("""{ "type": "object", "properties": { "p": {}, "p": {} } }"""),
			StringComparison.OrdinalIgnoreCase);

	[Theory]
	[InlineData("""{ "type": "object", "properties": {} }""", "nested objects")]
	[InlineData("""{ "type": "array", "items": { "type": "string" } }""", "lists")]
	[InlineData("""{ "type": "string", "items": { "type": "string" } }""", "\"items\"")]
	[InlineData("""{ "type": "array" }""", "lists")]
	[InlineData("""{ "$ref": "#/$defs/host" }""", "$ref")]
	[InlineData("""{ "type": "string", "allOf": [ { "minLength": 1 } ] }""", "allOf")]
	[InlineData("""{ "type": "string", "if": { "minLength": 1 }, "then": {} }""", "\"if\"")]
	[InlineData("""{ "type": "string", "const": "a" }""", "const")]
	[InlineData("""{ "type": ["string", "boolean"] }""", "type")]
	[InlineData("""{ "type": ["integer", "number"] }""", "type")]
	[InlineData("""{ "type": "null" }""", "type")]
	[InlineData("""{ "type": "date" }""", "date")]
	[InlineData("""{ "description": "no type" }""", "no \"type\"")]
	[InlineData("true", "object")]
	public void UnsupportedShapes_AreProblems(string property, string mentions)
	{
		MokaSchemaParseResult result = MokaFormSchema.Parse($$"""{ "type": "object", "properties": { "p": {{property}} } }""");

		Assert.False(result.Succeeded);
		Assert.Contains(result.Problems, problem => problem.Contains(mentions, StringComparison.Ordinal));
		Assert.All(result.Problems, problem => Assert.StartsWith("\"p\"", problem, StringComparison.Ordinal));
	}

	[Theory]
	[InlineData("$ref")]
	[InlineData("allOf")]
	[InlineData("oneOf")]
	[InlineData("if")]
	[InlineData("patternProperties")]
	public void UnsupportedRootKeywords_AreProblems(string keyword) =>
		Assert.Contains(keyword, Problem($$"""{ "type": "object", "properties": {}, "{{keyword}}": {} }"""), StringComparison.Ordinal);

	[Theory]
	[InlineData("""{ "type": "string", "minLength": -1 }""", "minLength")]
	[InlineData("""{ "type": "string", "maxLength": "5" }""", "maxLength")]
	[InlineData("""{ "type": "string", "minLength": 1.5 }""", "minLength")]
	[InlineData("""{ "type": "string", "minLength": 5, "maxLength": 2 }""", "minLength")]
	[InlineData("""{ "type": "string", "pattern": "([a-z" }""", "pattern")]
	[InlineData("""{ "type": "string", "pattern": "(?=a)b" }""", "backtracking")]
	[InlineData("""{ "type": "string", "pattern": "(a)\\1" }""", "backtracking")]
	[InlineData("""{ "type": "string", "pattern": 5 }""", "pattern")]
	[InlineData("""{ "type": "integer", "minimum": "1" }""", "minimum")]
	[InlineData("""{ "type": "integer", "minimum": 1, "exclusiveMinimum": true }""", "2020-12")]
	[InlineData("""{ "type": "number", "multipleOf": 0 }""", "multipleOf")]
	[InlineData("""{ "type": "number", "multipleOf": -2 }""", "multipleOf")]
	[InlineData("""{ "type": "string", "readOnly": "yes" }""", "readOnly")]
	[InlineData("""{ "type": "string", "x-advanced": 1 }""", "x-advanced")]
	[InlineData("""{ "type": "string", "title": 5 }""", "title")]
	[InlineData("""{ "type": "string", "examples": "a" }""", "examples")]
	[InlineData("""{ "type": "integer", "writeOnly": true }""", "writeOnly")]
	[InlineData("""{ "type": "string", "writeOnly": true, "readOnly": true }""", "both")]
	[InlineData("""{ "type": "string", "writeOnly": true, "x-multiline": true }""", "x-multiline")]
	[InlineData("""{ "type": "number", "x-multiline": true }""", "x-multiline")]
	[InlineData("""{ "type": "string", "writeOnly": true, "default": "hunter2" }""", "default")]
	[InlineData("""{ "type": "integer", "default": "abc" }""", "default")]
	[InlineData("""{ "type": "integer", "minimum": 5, "default": 1 }""", "default")]
	[InlineData("""{ "type": "string", "enum": ["a"], "default": "b" }""", "default")]
	[InlineData("""{ "type": "boolean", "default": "true" }""", "default")]
	[InlineData("""{ "type": "string", "default": { "a": 1 } }""", "default")]
	public void BadKeywordValues_AreProblems(string property, string mentions) =>
		Assert.Contains(mentions, PropertyProblem(property), StringComparison.Ordinal);

	[Theory]
	[InlineData("""{ "type": "string", "enum": [] }""", "at least one")]
	[InlineData("""{ "type": "string", "enum": "a" }""", "at least one")]
	[InlineData("""{ "type": "string", "enum": ["a", "a"] }""", "twice")]
	[InlineData("""{ "enum": [1, 1.0] }""", "twice")]
	[InlineData("""{ "type": "number", "enum": [2.5, 2.50] }""", "twice")]
	[InlineData("""{ "enum": [true, false] }""", "text or numbers")]
	[InlineData("""{ "enum": ["a", 1] }""", "all text or all numbers")]
	[InlineData("""{ "type": "integer", "enum": [1, 1.5] }""", "\"integer\"")]
	[InlineData("""{ "type": "boolean", "enum": ["a"] }""", "\"boolean\"")]
	[InlineData("""{ "type": "string", "enum": ["a"], "oneOf": [ { "const": "a" } ] }""", "not both")]
	[InlineData("""{ "type": "string", "oneOf": [ { "const": "a" } ], "anyOf": [ { "const": "a" } ] }""", "not both")]
	[InlineData("""{ "type": "string", "oneOf": [ { "const": "a", "minLength": 1 } ] }""", "labelled choices")]
	[InlineData("""{ "type": "string", "oneOf": [ { "title": "A" } ] }""", "labelled choices")]
	[InlineData("""{ "type": "string", "anyOf": [ { "const": ["a"] } ] }""", "text or a number")]
	public void BadChoices_AreProblems(string property, string mentions) =>
		Assert.Contains(mentions, PropertyProblem(property), StringComparison.Ordinal);

	[Theory]
	[InlineData("""{ "type": "object", "required": "p", "properties": { "p": { "type": "string" } } }""", "list of property names")]
	[InlineData("""{ "type": "object", "required": [1], "properties": { "p": { "type": "string" } } }""", "list of property names")]
	[InlineData("""{ "type": "object", "required": ["p", "p"], "properties": { "p": { "type": "string" } } }""", "twice")]
	[InlineData("""{ "type": "object", "required": ["q"], "properties": { "p": { "type": "string" } } }""", "isn't one of its properties")]
	[InlineData("""{ "type": "object", "properties": [] }""", "\"properties\" is an object")]
	[InlineData("""{ "type": "object", "additionalProperties": { "type": "string" } }""", "additionalProperties")]
	[InlineData("""{ "type": "object", "title": [] }""", "title")]
	public void BadRootKeywords_AreProblems(string json, string mentions) =>
		Assert.Contains(mentions, Problem(json), StringComparison.Ordinal);

	[Fact]
	public void EveryProblem_IsReported_NotJustTheFirst()
	{
		MokaSchemaParseResult result = MokaFormSchema.Parse("""
			{ "type": "object", "properties": { "a": { "type": "array" }, "b": { "$ref": "#/x" }, "c": { "type": "string" } } }
			""");

		Assert.Equal(2, result.Problems.Count);
		Assert.StartsWith("\"a\"", result.Problems[0], StringComparison.Ordinal);
		Assert.StartsWith("\"b\"", result.Problems[1], StringComparison.Ordinal);
	}

	[Fact]
	public void TooLongAJson_IsRefusedBeforeReading()
	{
		string json = $$"""{ "type": "object", "description": "{{new string('a', MokaFormSchema.MaxJsonLength)}}" }""";

		Assert.Contains("characters long", Problem(json), StringComparison.Ordinal);
	}

	[Fact]
	public void TooDeepAJson_IsRefused()
	{
		string deep = string.Concat(Enumerable.Repeat("[", MokaFormSchema.MaxDepth + 1)) +
		              string.Concat(Enumerable.Repeat("]", MokaFormSchema.MaxDepth + 1));

		Assert.Contains("depth", Problem($$"""{ "type": "object", "x-deep": {{deep}} }"""), StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void TooManyProperties_AreRefused()
	{
		string properties = string.Join(", ", Enumerable.Range(0, MokaFormSchema.MaxProperties + 1)
			.Select(index => $$"""
			                   "p{{index}}": { "type": "string" }
			                   """));

		Assert.Contains("properties", Problem($$"""{ "type": "object", "properties": { {{properties}} } }"""), StringComparison.Ordinal);
	}

	[Fact]
	public void TheMostProperties_AreRead()
	{
		string properties = string.Join(", ", Enumerable.Range(0, MokaFormSchema.MaxProperties)
			.Select(index => $$"""
			                   "p{{index}}": { "type": "string" }
			                   """));

		Assert.Equal(MokaFormSchema.MaxProperties, Read($$"""{ "type": "object", "properties": { {{properties}} } }""").Fields.Count);
	}

	[Fact]
	public void TooManyChoices_AreRefused()
	{
		string values = string.Join(", ", Enumerable.Range(0, MokaFormSchema.MaxChoices + 1));

		Assert.Contains("choices", PropertyProblem($$"""{ "type": "integer", "enum": [{{values}}] }"""), StringComparison.Ordinal);
	}

	[Fact]
	public void TooLongANameOrText_IsRefused()
	{
		string name = new('n', MokaFormSchema.MaxNameLength + 1);
		string title = new('t', MokaFormSchema.MaxTextLength + 1);
		string pattern = new('a', MokaFormSchema.MaxPatternLength + 1);

		Assert.Contains("property name", Problem($$"""{ "type": "object", "properties": { "{{name}}": { "type": "string" } } }"""),
			StringComparison.Ordinal);
		Assert.Contains("longer than", PropertyProblem($$"""{ "type": "string", "title": "{{title}}" }"""), StringComparison.Ordinal);
		Assert.Contains("longer than", PropertyProblem($$"""{ "type": "string", "pattern": "{{pattern}}" }"""), StringComparison.Ordinal);
	}

	[Fact]
	public void AnEmptyOrControlCharacterName_IsRefused()
	{
		Assert.Contains("property name", Problem("""{ "type": "object", "properties": { "": { "type": "string" } } }"""),
			StringComparison.Ordinal);
		Assert.Contains("property name", Problem("""{ "type": "object", "properties": { "a\u0007": { "type": "string" } } }"""),
			StringComparison.Ordinal);
	}

	[Fact]
	public void APatternTooLargeToRunSafely_IsRefused() =>
		Assert.Contains("pattern", PropertyProblem("""{ "type": "string", "pattern": "(a{1000}){1000}" }"""), StringComparison.Ordinal);

	[Fact]
	public void AnObjectTooLarge_IsRefused()
	{
		JsonObject schema = new()
		{
			["type"] = "object",
			["description"] = new string('d', MokaFormSchema.MaxJsonLength)
		};

		MokaSchemaParseResult result = MokaFormSchema.Parse(schema);

		Assert.False(result.Succeeded);
		Assert.Contains("characters long", Assert.Single(result.Problems), StringComparison.Ordinal);
	}

	[Fact]
	public void IntegerBounds_BecomeTheNumberFieldsLimits()
	{
		MokaSchemaField field = One("""{ "type": "integer", "minimum": 0.5, "exclusiveMinimum": 2, "maximum": 10.5, "exclusiveMaximum": 9.5, "multipleOf": 3 }""");

		Assert.Equal(3, field.WholeMinimum);
		Assert.Equal(9, field.WholeMaximum);
		Assert.Equal(3, field.WholeStep);
		Assert.Equal(1, One("""{ "type": "integer" }""").WholeStep);
	}

	[Fact]
	public void ASchemaExportedFromADotNetType_IsRead()
	{
		JsonSerializerOptions options = new(JsonSerializerOptions.Web);
		JsonObject exported = options.GetJsonSchemaAsNode(typeof(BackupJob)).AsObject();

		MokaSchemaParseResult result = MokaFormSchema.Parse(exported);

		Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Problems));
		Assert.Equal(["target", "keep", "compress", "note"], result.Schema.Fields.Select(field => field.Name));
		Assert.Equal([MokaSchemaFieldKind.Text, MokaSchemaFieldKind.WholeNumber, MokaSchemaFieldKind.Toggle, MokaSchemaFieldKind.Text],
			result.Schema.Fields.Select(field => field.Kind));
	}

	[Fact]
	public void TheChoiceValue_IsItsOwnCopy()
	{
		MokaSchemaField field = One("""{ "type": "string", "enum": ["a"], "default": "a" }""");

		Assert.Equal(JsonValueKind.String, field.Choices[0].Value.ValueKind);
		Assert.Equal("a", field.Default!.Value.GetString());
	}

	private sealed record BackupJob(string Target, int Keep, bool Compress, string? Note);
}
