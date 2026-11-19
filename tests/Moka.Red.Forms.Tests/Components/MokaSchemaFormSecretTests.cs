using System.Text.Json.Nodes;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Forms.SchemaForm;
using static Moka.Red.Forms.Tests.Components.MokaSchemaFormTests;

namespace Moka.Red.Forms.Tests.Components;

public class MokaSchemaFormSecretTests : BunitContext
{
	// cfg-0 is the API key, cfg-1 the token.
	private const string SecretSchema = """
		{
			"type": "object",
			"required": ["token"],
			"properties": {
				"key": { "type": "string", "title": "API key", "writeOnly": true, "minLength": 8, "examples": ["sk-..."] },
				"token": { "type": "string", "title": "Token", "writeOnly": true },
				"name": { "type": "string", "title": "Name" }
			}
		}
		""";

	private readonly BunitJSModuleInterop _module;

	public MokaSchemaFormSecretTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
	}

	private IRenderedComponent<SaveHost> Host(string[] set, string value = """{ "name": "Backups" }""") =>
		Render<SaveHost>(p => p
			.Add(x => x.Schema, Schema(SecretSchema))
			.Add(x => x.Value, Json(value))
			.Add(x => x.SetSecrets, set));

	[Fact]
	public void ASecretNotSet_IsAPasswordField()
	{
		IRenderedComponent<SaveHost> cut = Host([]);
		IElement key = cut.Find("#cfg-0");

		Assert.Equal("password", key.GetAttribute("type"));
		Assert.Equal("new-password", key.GetAttribute("autocomplete"));
		Assert.Equal("sk-...", key.GetAttribute("placeholder"));
		Assert.Equal("It isn't shown again once saved.", Helper(cut, "cfg-0"));
		Assert.DoesNotContain(cut.FindAll("button"), button => button.TextContent.Contains("Change", StringComparison.Ordinal));
	}

	[Fact]
	public void ASetSecret_SaysItsSet_AndNeverShowsItsValue()
	{
		IRenderedComponent<SaveHost> cut = Host(["token"], """{ "name": "Backups", "key": "hunter2-secret" }""");

		Assert.DoesNotContain("hunter2", cut.Markup, StringComparison.Ordinal);
		IElement key = cut.Find("#cfg-0");
		Assert.True(key.HasAttribute("readonly"));
		Assert.Equal("Set", key.GetAttribute("value"));
		Assert.Equal("Change API key", Normal(Button(cut, "Change").TextContent));
		Assert.Equal("Clear API key", Normal(Button(cut, "Clear").TextContent));
	}

	[Fact]
	public void Change_OpensAPasswordField_ThenSaveSetsTheNewValue()
	{
		IRenderedComponent<SaveHost> cut = Host(["key", "token"]);

		Button(cut, "Change").Click();
		IElement key = cut.Find("#cfg-0");
		Assert.Equal("password", key.GetAttribute("type"));
		Assert.Equal("cfg-0", _module.VerifyInvoke("focusField").Arguments[0]);
		Assert.NotNull(Button(cut, "Keep the saved one"));
		Assert.False(Dirty(cut));

		cut.Find("#cfg-0").Input("new-secret-1");
		Assert.True(Dirty(cut));
		Button(cut, "Save").Click();

		MokaSchemaFormSave save = Assert.Single(cut.Instance.Saves);
		Assert.Equal(MokaSecretChange.Set("new-secret-1"), save.Secrets["key"]);
		Assert.Equal(MokaSecretChange.Unchanged, save.Secrets["token"]);
		Assert.False(save.Values.ContainsKey("key"));
		Assert.Empty(save.Changed);
		Assert.False(Dirty(cut));
		Assert.Equal("Set", cut.Find("#cfg-0").GetAttribute("value"));
	}

	[Fact]
	public void ANewSecret_IsCheckedLikeText()
	{
		IRenderedComponent<SaveHost> cut = Host(["key", "token"]);
		Button(cut, "Change").Click();

		cut.Find("#cfg-0").Input("short");
		cut.Find("#cfg-0").Blur();

		Assert.Equal("API key needs at least 8 characters.", Helper(cut, "cfg-0"));
	}

	[Fact]
	public void Clear_SavesAsCleared_AndTheSecretIsThenUnset()
	{
		IRenderedComponent<SaveHost> cut = Host(["key", "token"]);

		Button(cut, "Clear").Click();
		Assert.Equal("Cleared when you save", cut.Find("#cfg-0").GetAttribute("value"));
		Assert.Equal("cfg-0", _module.VerifyInvoke("focusField").Arguments[0]);
		Assert.True(Dirty(cut));

		Button(cut, "Save").Click();

		Assert.Equal(MokaSecretChange.Cleared, Assert.Single(cut.Instance.Saves).Secrets["key"]);
		Assert.Equal("password", cut.Find("#cfg-0").GetAttribute("type"));
		Assert.False(Dirty(cut));
	}

	[Fact]
	public void Keep_PutsTheSavedSecretBack()
	{
		IRenderedComponent<SaveHost> cut = Host(["key", "token"]);
		Button(cut, "Change").Click();
		cut.Find("#cfg-0").Input("new-secret-1");

		Button(cut, "Keep the saved one").Click();

		Assert.Equal("Set", cut.Find("#cfg-0").GetAttribute("value"));
		Assert.False(Dirty(cut));
	}

	[Fact]
	public void ARequiredSecret_IsNeededUntilSet_AndCantBeCleared()
	{
		IRenderedComponent<SaveHost> unset = Host([]);
		unset.Find("#cfg-0").Input("long-enough-1");
		Button(unset, "Save").Click();

		Assert.Empty(unset.Instance.Saves);
		Assert.Equal("Token is needed.", Helper(unset, "cfg-1"));

		IRenderedComponent<SaveHost> set = Host(["token"]);
		Assert.DoesNotContain(set.FindAll("button"), button => Normal(button.TextContent) == "Clear Token");
	}

	[Fact]
	public void ASecretChange_NeverWritesItsValue()
	{
		MokaSecretChange change = MokaSecretChange.Set("hunter2");
		MokaSchemaFormSave save = new(new JsonObject(), [], new Dictionary<string, MokaSecretChange> { ["key"] = change });

		Assert.Equal("Set", change.ToString());
		Assert.Equal("hunter2", change.Value);
		Assert.DoesNotContain("hunter2", save.ToString(), StringComparison.Ordinal);
		Assert.Null(MokaSecretChange.Cleared.Value);
		Assert.Equal(MokaSecretEdit.Unchanged, default(MokaSecretChange).Edit);
	}

	[Fact]
	public void ADisabledForm_OffersNoChange()
	{
		IRenderedComponent<MokaSchemaForm> cut = Render<MokaSchemaForm>(p => p
			.Add(x => x.Schema, Schema(SecretSchema))
			.Add(x => x.SetSecrets, ["key"])
			.Add(x => x.Disabled, true));

		Assert.DoesNotContain(cut.FindAll("button"), button => button.TextContent.Contains("Change", StringComparison.Ordinal));
	}

	private static string Normal(string text) =>
		string.Join(' ', text.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries));
}
