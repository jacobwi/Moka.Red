using System.Text.Json.Nodes;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Forms.SchemaForm;

namespace Moka.Red.Forms.Tests.Components;

public class MokaSchemaAnyFormatTests : BunitContext
{
	private const string Server = """
		{ "type": "string", "title": "Server", "description": "A name or an address.",
		  "anyOf": [ { "format": "hostname" }, { "format": "ipv4" }, { "format": "ipv6" } ] }
		""";

	private const string ServerMessage = "Server is a host name, an IPv4 address or an IPv6 address.";

	private const string Relay = $$"""
		{ "type": "object", "title": "Mail relay", "required": ["server"],
		  "properties": { "server": {{Server}}, "port": { "type": "integer", "title": "Port", "default": 25 } } }
		""";

	public MokaSchemaAnyFormatTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule(MokaSchemaFormTests.Module).Mode = JSRuntimeMode.Loose;
	}

	private static MokaSchemaParseResult Parse(string property) =>
		MokaFormSchema.Parse($$"""{ "type": "object", "properties": { "p": {{property}} } }""");

	private static MokaFormSchema Schema(string property)
	{
		MokaSchemaParseResult result = Parse(property);
		Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Problems));
		return result.Schema;
	}

	private static MokaSchemaField One(string property) => Schema(property).Fields.Single();

	private static string? ProblemOf(string property, string text) =>
		Schema(property).Check(new JsonObject { ["p"] = text }).GetValueOrDefault("p");

	private static string Formats(params string[] formats) =>
		"{ \"type\": \"string\", \"anyOf\": [ " + string.Join(", ", formats.Select(format => $"{{ \"format\": \"{format}\" }}")) + " ] }";

	[Fact]
	public void AnAnyOfOfFormats_IsOneTextSetting()
	{
		MokaSchemaField field = One(Server);

		Assert.Equal(MokaSchemaFieldKind.Text, field.Kind);
		Assert.Equal(["hostname", "ipv4", "ipv6"], field.Formats);
		Assert.Null(field.Format);
		Assert.Empty(field.Choices);
		Assert.Equal("A name or an address.", field.Description);
	}

	[Theory]
	[InlineData("""{ "type": "string", "anyOf": [ { "format": "ipv4", "title": "IPv4", "description": "Four numbers." }, { "format": "ipv6" } ] }""",
		new[] { "ipv4", "ipv6" })]
	[InlineData("""{ "type": ["string", "null"], "anyOf": [ { "format": "hostname" } ] }""", new[] { "hostname" })]
	[InlineData("""{ "type": "string", "maxLength": 253, "pattern": "^\\S+$", "anyOf": [ { "format": "hostname" }, { "format": "x-relay" } ] }""",
		new[] { "hostname", "x-relay" })]
	[InlineData("""{ "type": "string", "writeOnly": true, "anyOf": [ { "format": "uuid" }, { "format": "email" } ] }""",
		new[] { "uuid", "email" })]
	[InlineData("""{ "type": "string", "x-multiline": true, "anyOf": [ { "format": "uri" }, { "format": "date-time" } ] }""",
		new[] { "uri", "date-time" })]
	public void AcceptedShapes_KeepTheirFormatsInOrder(string property, string[] formats) =>
		Assert.Equal(formats, One(property).Formats);

	[Fact]
	public void TheOwnFormat_IsTheOnlyOneInFormats()
	{
		MokaSchemaField email = One("""{ "type": "string", "format": "email" }""");

		Assert.Equal("email", email.Format);
		Assert.Equal(["email"], email.Formats);
		Assert.Empty(One("""{ "type": "string" }""").Formats);
		Assert.Empty(One("""{ "type": "integer", "format": "int32" }""").Formats);
	}

	[Fact]
	public void LabelledChoices_AreStillChoices()
	{
		MokaSchemaField field = One("""
			{ "type": "string", "anyOf": [ { "const": "smtp", "title": "Mail", "description": "Port 25." }, { "const": "ups" } ] }
			""");

		Assert.Equal(MokaSchemaFieldKind.Choice, field.Kind);
		Assert.Equal(["Mail", "ups"], field.Choices.Select(choice => choice.Label));
		Assert.Empty(field.Formats);
	}

	[Theory]
	[InlineData("""{ "type": "string", "anyOf": [ { "const": "a" }, { "format": "hostname" } ] }""", "mixes labelled choices and formats")]
	[InlineData("""{ "type": "string", "oneOf": [ { "format": "hostname" }, { "const": "a" } ] }""", "mixes labelled choices and formats")]
	[InlineData("""{ "type": "string", "oneOf": [ { "format": "hostname" }, { "format": "ipv4" } ] }""", "Give them as \"anyOf\"")]
	[InlineData("""{ "type": "string", "format": "hostname", "anyOf": [ { "format": "ipv4" } ] }""", "\"format\" or \"anyOf\", not both")]
	[InlineData("""{ "type": "string", "enum": ["a"], "anyOf": [ { "format": "ipv4" } ] }""", "has no \"enum\"")]
	[InlineData("""{ "type": "string", "oneOf": [ { "const": "a" } ], "anyOf": [ { "format": "ipv4" } ] }""", "has no \"oneOf\"")]
	[InlineData("""{ "type": "string", "anyOf": [ { "format": "hostname", "pattern": "^m" } ] }""", "items like")]
	[InlineData("""{ "type": "string", "anyOf": [ { "format": "hostname", "type": "string" } ] }""", "items like")]
	[InlineData("""{ "type": "string", "anyOf": [ { "format": "hostname" }, "ipv4" ] }""", "items like")]
	[InlineData("""{ "type": "string", "anyOf": [ { "format": "hostname" }, { "title": "Address" } ] }""", "items like")]
	[InlineData("""{ "type": "string", "anyOf": [ { "format": 4 } ] }""", "\"format\" is text")]
	[InlineData("""{ "type": "string", "anyOf": [ { "format": null } ] }""", "\"format\" is text")]
	[InlineData("""{ "type": "string", "anyOf": [ { "format": "ipv4", "title": 5 } ] }""", "\"title\" is text")]
	[InlineData("""{ "type": "string", "anyOf": [ { "format": "ipv4" }, { "format": "ipv4" } ] }""", "\"ipv4\" twice")]
	[InlineData("""{ "type": "integer", "anyOf": [ { "format": "ipv4" } ] }""", "are for text")]
	[InlineData("""{ "type": "boolean", "anyOf": [ { "format": "ipv4" } ] }""", "are for text")]
	[InlineData("""{ "anyOf": [ { "format": "ipv4" } ] }""", "no \"type\"")]
	[InlineData("""{ "type": "string", "anyOf": [ { "title": "A" } ] }""", "or as formats")]
	[InlineData("""{ "type": "string", "anyOf": [ { "format": "hostname" }, { "format": "ipv4" } ], "default": "not a host!" }""",
		"p is a host name or an IPv4 address.")]
	public void RefusedShapes_NameTheirProblem(string property, string mentions)
	{
		MokaSchemaParseResult result = Parse(property);

		Assert.False(result.Succeeded);
		string problem = Assert.Single(result.Problems);
		Assert.StartsWith("\"p\"", problem, StringComparison.Ordinal);
		Assert.Contains(mentions, problem, StringComparison.Ordinal);
	}

	[Fact]
	public void MoreFormatsThanTheLimit_AreRefused()
	{
		string property = Formats("hostname", "ipv4", "ipv6", "email", "uri", "uuid", "date", "time", "duration");

		Assert.Contains($"at most {MokaFormSchema.MaxFormats}", Assert.Single(Parse(property).Problems), StringComparison.Ordinal);
		Assert.Equal(MokaFormSchema.MaxFormats, One(Formats("hostname", "ipv4", "ipv6", "email", "uri", "uuid", "date", "time")).Formats.Count);
	}

	[Theory]
	[InlineData("mail.example.com")]
	[InlineData("localhost")]
	[InlineData("192.0.2.10")]
	[InlineData("2001:db8::25")]
	[InlineData("::1")]
	public void EachFormat_IsEnoughOnItsOwn(string text) =>
		Assert.Null(ProblemOf(Server, text));

	[Theory]
	[InlineData("mail server")]
	[InlineData("under_score.example")]
	[InlineData("-relay.example.com")]
	[InlineData("2001:db8::zz")]
	[InlineData("fe80::1%eth0")]
	public void TextThatFitsNone_IsToldWhatTheSettingTakes(string text) =>
		Assert.Equal(ServerMessage, ProblemOf(Server, text));

	[Theory]
	[InlineData(new[] { "ipv4", "ipv6" }, "p is an IPv4 address or an IPv6 address.")]
	[InlineData(new[] { "email", "uri" }, "p is an email address or a full address.")]
	[InlineData(new[] { "uuid", "hostname", "email" }, "p is a UUID, a host name or an email address.")]
	[InlineData(new[] { "ipv6" }, "p is an IPv6 address, like 2001:db8::1.")]
	public void TheMessage_NamesEachFormat(string[] formats, string message) =>
		Assert.Equal(message, ProblemOf(Formats(formats), "not one of them"));

	[Fact]
	public void AFormatTheFormDoesntCheck_TakesAnyText() =>
		Assert.Null(ProblemOf(Formats("ipv4", "x-relay"), "not an address"));

	[Fact]
	public void AHostNameTakesADottedQuad_WhichIsWhyOneOfIsRefused()
	{
		Assert.Null(ProblemOf("""{ "type": "string", "format": "hostname" }""", "192.0.2.10"));
		Assert.Null(ProblemOf("""{ "type": "string", "format": "ipv4" }""", "192.0.2.10"));
	}

	[Fact]
	public void TheLengthAndPattern_StillApply()
	{
		Assert.Equal("p takes at most 5 characters.",
			ProblemOf("""{ "type": "string", "maxLength": 5, "anyOf": [ { "format": "hostname" }, { "format": "ipv4" } ] }""",
				"mail.example.com"));
		Assert.Equal("p isn't in the form it takes.",
			ProblemOf("""{ "type": "string", "pattern": "^mail", "anyOf": [ { "format": "hostname" }, { "format": "ipv4" } ] }""",
				"192.0.2.10"));
	}

	[Fact]
	public void TheWords_CanBeReplaced()
	{
		MokaSchemaFormText text = MokaSchemaFormText.Default with
		{
			HostNameFormatName = "a name",
			NotAnyFormat = (title, names) => $"{title}: {string.Join(" / ", names)}"
		};

		IReadOnlyDictionary<string, string> problems = MokaFormSchema.Parse(Relay).Schema!
			.Check(new JsonObject { ["server"] = "mail server" }, null, text);

		Assert.Equal("Server: a name / an IPv4 address / an IPv6 address", problems["server"]);
	}

	[Fact]
	public void CheckOnAServer_AppliesTheSameRule()
	{
		MokaFormSchema schema = MokaFormSchema.Parse(Relay).Schema!;

		Assert.Empty(schema.Check(schema.WithDefaults(new JsonObject { ["server"] = "2001:db8::25" })));
		Assert.Empty(schema.Check(schema.WithDefaults(new JsonObject { ["server"] = "relay.example.com" })));
		Assert.Equal(ServerMessage, schema.Check(schema.WithDefaults(new JsonObject { ["server"] = "smtp relay" }))["server"]);
		Assert.Equal("Server is needed.", schema.Check(schema.WithDefaults(new JsonObject()))["server"]);
	}

	[Fact]
	public void TheSetting_IsOneTextBox_WithTheMessageUnderIt()
	{
		IRenderedComponent<MokaSchemaForm> cut = Render<MokaSchemaForm>(p => p
			.Add(x => x.Schema, MokaFormSchema.Parse(Relay).Schema!)
			.Add(x => x.Id, "cfg"));

		IElement server = cut.Find("#cfg-0");
		Assert.Equal("input", server.TagName, StringComparer.OrdinalIgnoreCase);
		Assert.Equal("text", server.GetAttribute("type"));
		Assert.Contains("moka-field-input--mono", server.ClassName, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll("select"));
		Assert.Equal("A name or an address.", MokaSchemaFormTests.Helper(cut, "cfg-0"));

		server.Input("smtp relay");
		cut.Find("#cfg-0").Blur();
		Assert.Equal(ServerMessage, MokaSchemaFormTests.Helper(cut, "cfg-0"));

		cut.Find("#cfg-0").Input("2001:db8::25");
		Assert.Equal("A name or an address.", MokaSchemaFormTests.Helper(cut, "cfg-0"));
	}

	[Fact]
	public void TheBox_TakesWhatEveryFormatShares()
	{
		IRenderedComponent<MokaSchemaForm> cut = Render<MokaSchemaForm>(p => p
			.Add(x => x.Schema, MokaSchemaFormTests.Schema($$"""
				{ "type": "object", "properties": {
					"a": {{Formats("email")}}, "b": {{Formats("email", "uri")}}, "c": {{Formats("hostname", "email")}} } }
				"""))
			.Add(x => x.Id, "cfg"));

		Assert.Equal("email", cut.Find("#cfg-0").GetAttribute("type"));
		Assert.Equal("text", cut.Find("#cfg-1").GetAttribute("type"));
		Assert.DoesNotContain("moka-field-input--mono", cut.Find("#cfg-2").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Save_WaitsForAServerItTakes()
	{
		IRenderedComponent<MokaSchemaFormTests.SaveHost> cut = Render<MokaSchemaFormTests.SaveHost>(p => p
			.Add(x => x.Schema, MokaFormSchema.Parse(Relay).Schema!)
			.Add(x => x.Value, new JsonObject { ["server"] = "relay.example.com" }));
		cut.Find("#cfg-0").Input("smtp relay");

		MokaSchemaFormTests.Button(cut, "Save").Click();

		Assert.Empty(cut.Instance.Saves);
		Assert.Equal(ServerMessage, MokaSchemaFormTests.Helper(cut, "cfg-0"));

		cut.Find("#cfg-0").Input("192.0.2.25");
		MokaSchemaFormTests.Button(cut, "Save").Click();

		Assert.Equal("192.0.2.25", Assert.Single(cut.Instance.Saves).Values["server"]!.GetValue<string>());
	}
}
