using System.Text.Json.Nodes;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Forms.SchemaForm;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

public class MokaSchemaFormTests : BunitContext
{
	internal const string Module = "./_content/Moka.Red.Forms/SchemaForm/MokaSchemaForm.razor.js";

	// Field ids follow the schema's order: cfg-0 host, cfg-1 port, cfg-2 tls, cfg-3 mode, cfg-4 timeout (advanced).
	internal const string MailSchema = """
		{
			"type": "object",
			"title": "Mail server",
			"description": "Where reports are sent from.",
			"required": ["host"],
			"properties": {
				"host": { "type": "string", "title": "Host", "format": "hostname", "description": "The server's name.",
				          "examples": ["mail.example.com"] },
				"port": { "type": "integer", "title": "Port", "minimum": 1, "maximum": 65535, "default": 587 },
				"tls": { "type": "boolean", "title": "Use TLS", "default": true },
				"mode": { "type": "string", "title": "Sign in",
				          "oneOf": [ { "const": "none", "title": "No sign-in" }, { "const": "login", "title": "User name and password" } ] },
				"timeout": { "type": "number", "title": "Timeout", "exclusiveMinimum": 0, "x-advanced": true }
			}
		}
		""";

	private readonly BunitJSModuleInterop _module;

	public MokaSchemaFormTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
	}

	internal static MokaFormSchema Schema(string json) => MokaFormSchema.Parse(json).Schema!;

	internal static JsonObject Json(string json) => JsonNode.Parse(json)!.AsObject();

	internal static IElement Button<T>(IRenderedComponent<T> cut, string text) where T : IComponent =>
		cut.FindAll("button").First(button => button.TextContent.Trim().StartsWith(text, StringComparison.Ordinal));

	private IRenderedComponent<MokaSchemaForm> Form(string json = MailSchema, string? value = null,
		Action<ComponentParameterCollectionBuilder<MokaSchemaForm>>? more = null) =>
		Render<MokaSchemaForm>(p =>
		{
			p.Add(x => x.Schema, Schema(json));
			p.Add(x => x.Id, "cfg");
			if (value is not null)
			{
				p.Add(x => x.Value, Json(value));
			}

			more?.Invoke(p);
		});

	private IRenderedComponent<ValueParent<MokaSchemaForm, JsonObject>> Bound(string value, Func<JsonObject?, JsonObject?>? keep = null,
		string json = MailSchema) =>
		Render<ValueParent<MokaSchemaForm, JsonObject>>(p => p
			.Add(x => x.Value, Json(value))
			.Add(x => x.Keep, keep)
			.Add(x => x.With, new Dictionary<string, object?> { ["Schema"] = Schema(json), ["Id"] = "cfg" }));

	private IRenderedComponent<SaveHost> Host(string value = """{ "host": "mail.example.com" }""", string json = MailSchema,
		Func<MokaSchemaFormSave, (string?, IReadOnlyDictionary<string, string>?)>? answer = null) =>
		Render<SaveHost>(p => p
			.Add(x => x.Schema, Schema(json))
			.Add(x => x.Value, Json(value))
			.Add(x => x.Answer, answer));

	internal static bool Dirty<T>(IRenderedComponent<T> cut) where T : IComponent => cut.FindAll(".moka-settings-form__foot").Count > 0;

	internal static string? Helper<T>(IRenderedComponent<T> cut, string id) where T : IComponent =>
		cut.FindAll($"#{id}-help").SingleOrDefault()?.TextContent.Trim();

	internal static string? FormMessage<T>(IRenderedComponent<T> cut) where T : IComponent =>
		cut.FindAll(".moka-settings-form__refusal-text").SingleOrDefault()?.TextContent;

	[Fact]
	public void TheHeader_ComesFromTheSchema_UnlessTheFormNamesIt()
	{
		IRenderedComponent<MokaSchemaForm> cut = Form();

		Assert.Equal("Mail server", cut.Find("h2").TextContent);
		Assert.Equal("Where reports are sent from.", cut.Find(".moka-settings-form__lead").TextContent);
		Assert.Contains("moka-schema-form", cut.Find("section").ClassName, StringComparison.Ordinal);

		Assert.Equal("Outgoing mail", Form(more: p => p.Add(x => x.Title, "Outgoing mail")).Find("h2").TextContent);
	}

	[Fact]
	public void EachSetting_IsLabelled_AndItsDescriptionIsTiedToIt()
	{
		IRenderedComponent<MokaSchemaForm> cut = Form();

		IElement host = cut.Find("#cfg-0");
		Assert.StartsWith("Host", cut.Find("label[for='cfg-0']").TextContent.Trim(), StringComparison.Ordinal);
		Assert.Contains("cfg-0-help", host.GetAttribute("aria-describedby"), StringComparison.Ordinal);
		Assert.Equal("The server's name.", Helper(cut, "cfg-0"));
		Assert.Equal("mail.example.com", host.GetAttribute("placeholder"));
		Assert.Equal("true", host.GetAttribute("aria-required"));
		Assert.Null(cut.Find("#cfg-1").GetAttribute("aria-required"));
		Assert.Equal("Use TLS", cut.Find("#cfg-2").ParentElement!.TextContent.Trim());
	}

	[Fact]
	public void EachKind_GetsItsControl()
	{
		IRenderedComponent<MokaSchemaForm> cut = Form();

		IElement port = cut.Find("#cfg-1");
		Assert.Equal("spinbutton", port.GetAttribute("role"));
		Assert.Equal("1", port.GetAttribute("aria-valuemin"));
		Assert.Equal("65535", port.GetAttribute("aria-valuemax"));
		Assert.Equal("switch", cut.Find("#cfg-2").GetAttribute("role"));
		Assert.Equal(["Not set", "No sign-in", "User name and password"],
			cut.FindAll("#cfg-3 option").Select(option => option.TextContent));
		Assert.Empty(cut.FindAll("#cfg-4"));
	}

	[Fact]
	public void TextKeywords_PickTheBox()
	{
		IRenderedComponent<MokaSchemaForm> cut = Form("""
			{ "type": "object", "properties": {
				"notes": { "type": "string", "x-multiline": true },
				"email": { "type": "string", "format": "email" },
				"site": { "type": "string", "format": "uri" },
				"id": { "type": "string", "readOnly": true, "title": "Id" } } }
			""", """{ "id": "job-1" }""");

		Assert.Equal("textarea", cut.Find("#cfg-0").TagName, StringComparer.OrdinalIgnoreCase);
		Assert.Equal("email", cut.Find("#cfg-1").GetAttribute("type"));
		Assert.Equal("url", cut.Find("#cfg-2").GetAttribute("type"));
		Assert.True(cut.Find("#cfg-3").HasAttribute("readonly"));
		Assert.Equal("job-1", cut.Find("#cfg-3").GetAttribute("value"));
	}

	[Fact]
	public void ARequiredChoice_AsksForOne_InsteadOfOfferingNotSet()
	{
		IRenderedComponent<MokaSchemaForm> cut = Form("""
			{ "type": "object", "required": ["size"], "properties": { "size": { "enum": ["s", "m"] } } }
			""");

		Assert.Equal(["Choose one", "s", "m"], cut.FindAll("#cfg-0 option").Select(option => option.TextContent));
		Assert.True(cut.Find("#cfg-0 option").HasAttribute("disabled"));
	}

	[Fact]
	public void Defaults_ShowForWhatTheValueLeavesOut()
	{
		IRenderedComponent<MokaSchemaForm> cut = Form(value: """{ "host": "smtp.example.com", "tls": false }""");

		Assert.Equal("smtp.example.com", cut.Find("#cfg-0").GetAttribute("value"));
		Assert.Equal("587", cut.Find("#cfg-1").GetAttribute("value"));
		Assert.False(cut.Find("#cfg-2").HasAttribute("checked"));
		Assert.False(Dirty(cut));
	}

	[Fact]
	public void Editing_ReportsTheWholeObject_DefaultsFilledAndOtherPropertiesKept()
	{
		IRenderedComponent<ValueParent<MokaSchemaForm, JsonObject>> cut = Bound("""{ "host": "a.example", "other": [1] }""");
		Assert.Empty(cut.Instance.Asked);

		cut.Find("#cfg-0").Input("b.example");

		JsonObject sent = Assert.Single(cut.Instance.Asked)!;
		Assert.True(JsonNode.DeepEquals(Json("""{ "host": "b.example", "other": [1], "port": 587, "tls": true }"""), sent));
		Assert.True(Dirty(cut));
	}

	[Fact]
	public void EachControl_WritesItsJsonType()
	{
		IRenderedComponent<ValueParent<MokaSchemaForm, JsonObject>> cut = Bound("""{ "host": "a.example" }""");

		cut.Find("#cfg-1").Input("2525");
		cut.Find("#cfg-2").Change(false);
		cut.Find("#cfg-3").Change("2");

		JsonObject held = cut.Instance.Held!;
		Assert.Equal(2525, held["port"]!.GetValue<long>());
		Assert.False(held["tls"]!.GetValue<bool>());
		Assert.Equal("login", held["mode"]!.GetValue<string>());

		cut.Find("#cfg-3").Change("0");
		cut.Find("#cfg-1").Input("");
		Assert.False(cut.Instance.Held!.ContainsKey("mode"));
		Assert.False(cut.Instance.Held!.ContainsKey("port"));
	}

	[Fact]
	public void AParentThatKeepsItsValue_UndoesTheEdit()
	{
		JsonObject original = Json("""{ "host": "a.example" }""");
		IRenderedComponent<ValueParent<MokaSchemaForm, JsonObject>> cut =
			Bound("""{ "host": "a.example" }""", _ => original.DeepClone().AsObject());

		cut.Find("#cfg-0").Input("b.example");

		Assert.Equal("a.example", cut.Find("#cfg-0").GetAttribute("value"));
		Assert.False(Dirty(cut));
	}

	[Fact]
	public void PutBack_RestoresTheSavedValues_AndSaysSo()
	{
		IRenderedComponent<ValueParent<MokaSchemaForm, JsonObject>> cut = Bound("""{ "host": "a.example" }""");
		cut.Find("#cfg-0").Input("b.example");

		Button(cut, "Put back").Click();

		Assert.False(Dirty(cut));
		Assert.Equal("a.example", cut.Find("#cfg-0").GetAttribute("value"));
		Assert.Equal("a.example", cut.Instance.Held!["host"]!.GetValue<string>());
	}

	[Fact]
	public void AFieldIsChecked_OnceLeft_AndThenAsItsEdited()
	{
		IRenderedComponent<MokaSchemaForm> cut = Form(value: """{ "host": "a.example" }""");
		IElement host = cut.Find("#cfg-0");

		host.Input("");
		Assert.Equal("The server's name.", Helper(cut, "cfg-0"));

		cut.Find("#cfg-0").Blur();
		Assert.Equal("Host is needed.", Helper(cut, "cfg-0"));

		cut.Find("#cfg-0").Input("bad_host");
		Assert.Equal("Host is a host name, like mail.example.com.", Helper(cut, "cfg-0"));

		cut.Find("#cfg-0").Input("good.example");
		Assert.Equal("The server's name.", Helper(cut, "cfg-0"));
	}

	[Fact]
	public void Save_IsHeldBack_UntilEveryFieldFits_AndFocusGoesToTheFirstProblem()
	{
		IRenderedComponent<SaveHost> cut = Host("{}");
		cut.Find("#cfg-1").Input("25");

		Button(cut, "Save").Click();

		Assert.Empty(cut.Instance.Saves);
		Assert.Equal("Host is needed.", Helper(cut, "cfg-0"));
		Assert.Equal("Check the setting marked above.", FormMessage(cut));
		Assert.Equal("cfg-0", _module.VerifyInvoke("focusField").Arguments[0]);

		cut.Find("#cfg-0").Input("mail.example.com");
		Assert.Null(FormMessage(cut));
	}

	[Fact]
	public void ANumberTheFieldCantRead_HoldsBackSave()
	{
		IRenderedComponent<SaveHost> cut = Host();
		cut.Find("#cfg-2").Change(false);
		cut.Find("#cfg-1").Input("12x");

		Button(cut, "Save").Click();

		Assert.Empty(cut.Instance.Saves);
		Assert.Equal("Check the setting marked above.", FormMessage(cut));
		Assert.Equal("cfg-1", _module.VerifyInvoke("focusField").Arguments[0]);
	}

	[Fact]
	public void Save_HandsOverTheSettings_AndWhatChanged_ThenTheyreSaved()
	{
		IRenderedComponent<SaveHost> cut = Host("""{ "host": "mail.example.com", "other": "kept" }""");
		cut.Find("#cfg-1").Input("2525");

		Button(cut, "Save").Click();

		MokaSchemaFormSave save = Assert.Single(cut.Instance.Saves);
		Assert.True(JsonNode.DeepEquals(Json("""{ "host": "mail.example.com", "other": "kept", "port": 2525, "tls": true }"""),
			save.Values));
		Assert.Equal(["port"], save.Changed);
		Assert.Empty(save.Secrets);
		Assert.False(Dirty(cut));
	}

	[Fact]
	public void ARefusedSave_StaysUnsaved_AndSaysWhy()
	{
		int tries = 0;
		IRenderedComponent<SaveHost> cut = Host(answer: _ => ++tries == 1 ? ("The server is busy.", null) : (null, null));
		cut.Find("#cfg-1").Input("2525");

		Button(cut, "Save").Click();
		Assert.Equal("The server is busy.", FormMessage(cut));
		Assert.True(Dirty(cut));

		Button(cut, "Save").Click();
		Assert.Null(FormMessage(cut));
		Assert.False(Dirty(cut));
	}

	[Fact]
	public void ProblemsFromTheServer_ShowAtTheirFields_UntilEdited()
	{
		IRenderedComponent<SaveHost> cut = Host(answer: _ =>
			(null, new Dictionary<string, string> { ["host"] = "Nothing answers there.", ["zone"] = "No such zone." }));
		cut.Find("#cfg-1").Input("2525");

		Button(cut, "Save").Click();

		Assert.Equal("Nothing answers there.", Helper(cut, "cfg-0"));
		Assert.Equal("No such zone.", FormMessage(cut));
		Assert.Equal("cfg-0", _module.VerifyInvoke("focusField").Arguments[0]);
		Assert.True(Dirty(cut));

		cut.Find("#cfg-0").Input("smtp.example.com");
		Assert.Equal("The server's name.", Helper(cut, "cfg-0"));
	}

	[Fact]
	public void AProblemsParameter_ShowsWithoutASave()
	{
		IRenderedComponent<MokaSchemaForm> cut = Form(more: p => p
			.Add(x => x.Problems, new Dictionary<string, string> { ["port"] = "Blocked by the firewall." }));

		Assert.Equal("Blocked by the firewall.", Helper(cut, "cfg-1"));
	}

	[Fact]
	public void AdvancedSettings_WaitForShowAdvanced()
	{
		IRenderedComponent<MokaSchemaForm> cut = Form();
		Assert.Empty(cut.FindAll("#cfg-4"));

		Button(cut, "Show advanced").Click();

		Assert.Equal("More settings", cut.FindAll(".moka-settings-group__heading")[1].FirstChild!.TextContent.Trim());
		Assert.NotNull(cut.Find("#cfg-4"));
	}

	[Fact]
	public void AProblemInAnAdvancedSetting_ShowsTheAdvancedSettings()
	{
		IRenderedComponent<MokaSchemaForm> cut = Form(more: p => p
			.Add(x => x.Problems, new Dictionary<string, string> { ["timeout"] = "Too short." }));

		Assert.Equal("Too short.", Helper(cut, "cfg-4"));
	}

	[Fact]
	public void TheSameSchemaReadAgain_KeepsWhatsTyped()
	{
		IRenderedComponent<MokaSchemaForm> cut = Form(value: """{ "host": "a.example" }""");
		cut.Find("#cfg-0").Input("typed.example");

		cut.Render(p => p.Add(x => x.Schema, Schema(MailSchema)));

		Assert.Equal("typed.example", cut.Find("#cfg-0").GetAttribute("value"));
		Assert.True(Dirty(cut));
	}

	[Fact]
	public void ANewSchema_StartsOver()
	{
		IRenderedComponent<MokaSchemaForm> cut = Form(value: """{ "host": "a.example" }""");
		cut.Find("#cfg-0").Input("typed.example");

		cut.Render(p => p.Add(x => x.Schema, Schema("""{ "type": "object", "properties": { "host": { "type": "string" } } }""")));

		Assert.Equal("a.example", cut.Find("#cfg-0").GetAttribute("value"));
		Assert.False(Dirty(cut));
	}

	[Fact]
	public void NewSettingsFromTheParent_AreTheSavedOnes_WhileNothingIsEdited()
	{
		IRenderedComponent<MokaSchemaForm> cut = Form(value: """{ "host": "a.example" }""");

		cut.Render(p => p.Add(x => x.Value, Json("""{ "host": "loaded.example" }""")));

		Assert.Equal("loaded.example", cut.Find("#cfg-0").GetAttribute("value"));
		Assert.False(Dirty(cut));
	}

	[Fact]
	public void Disabled_ShowsTheSettings_AndChangesNothing()
	{
		IRenderedComponent<MokaSchemaForm> cut = Form(value: """{ "host": "a.example" }""", more: p => p.Add(x => x.Disabled, true));

		Assert.All(cut.FindAll("input, select"), control => Assert.True(control.HasAttribute("disabled")));
		Assert.Equal("a.example", cut.Find("#cfg-0").GetAttribute("value"));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void WithoutSettings_TheFormSaysSo(bool emptySchema)
	{
		IRenderedComponent<MokaSchemaForm> cut = Render<MokaSchemaForm>(p => p
			.Add(x => x.Schema, emptySchema ? Schema("""{ "type": "object" }""") : null));

		Assert.Equal("There's nothing to set.", cut.Find(".moka-schema-form__empty").TextContent);
		Assert.Equal("Settings", cut.Find("h2").TextContent);
	}

	[Fact]
	public void TheWords_CanBeReplaced()
	{
		IRenderedComponent<MokaSchemaForm> cut = Form(more: p => p
			.Add(x => x.Text, MokaSchemaFormText.Default with { GroupTitle = "Connection", NotSet = "None" }));

		Assert.Equal("Connection", cut.Find(".moka-settings-group__heading").TextContent.Trim());
		Assert.Equal("None", cut.Find("#cfg-3 option").TextContent);
	}

	[Fact]
	public void AStaticRender_ShowsTheSettings()
	{
		SetRendererInfo(new RendererInfo("Static", false));

		IRenderedComponent<MokaSchemaForm> cut = Form(value: """{ "host": "a.example", "mode": "login" }""");

		Assert.Equal("a.example", cut.Find("#cfg-0").GetAttribute("value"));
		Assert.Equal("587", cut.Find("#cfg-1").GetAttribute("value"));
		Assert.Equal("2", cut.Find("#cfg-3").GetAttribute("value"));
	}

	internal sealed class SaveHost : ComponentBase
	{
		private IReadOnlyDictionary<string, string>? _problems;
		private string? _refusal;

		[Parameter]
		public MokaFormSchema? Schema { get; set; }

		[Parameter]
		public JsonObject? Value { get; set; }

		[Parameter]
		public IReadOnlyCollection<string>? SetSecrets { get; set; }

		[Parameter]
		public Func<MokaSchemaFormSave, (string?, IReadOnlyDictionary<string, string>?)>? Answer { get; set; }

		public List<MokaSchemaFormSave> Saves { get; } = [];

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaSchemaForm>(0);
			builder.AddComponentParameter(1, nameof(MokaSchemaForm.Schema), Schema);
			builder.AddComponentParameter(2, nameof(MokaSchemaForm.Value), Value);
			builder.AddComponentParameter(3, nameof(MokaSchemaForm.Refusal), _refusal);
			builder.AddComponentParameter(4, nameof(MokaSchemaForm.Problems), _problems);
			builder.AddComponentParameter(5, nameof(MokaSchemaForm.SetSecrets), SetSecrets);
			builder.AddComponentParameter(6, nameof(MokaSchemaForm.OnSave),
				EventCallback.Factory.Create<MokaSchemaFormSave>(this, Save));
			builder.AddComponentParameter(7, nameof(MokaSchemaForm.Id), "cfg");
			builder.CloseComponent();
		}

		private void Save(MokaSchemaFormSave save)
		{
			Saves.Add(save);
			(_refusal, _problems) = Answer?.Invoke(save) ?? (null, null);
		}
	}
}
