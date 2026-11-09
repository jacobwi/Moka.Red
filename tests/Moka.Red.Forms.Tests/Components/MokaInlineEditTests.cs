using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.InlineEdit;

namespace Moka.Red.Forms.Tests.Components;

public class MokaInlineEditTests : BunitContext
{
	private const string ModulePath = "./_content/Moka.Red.Forms/InlineEdit/MokaInlineEdit.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaInlineEditTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
		_module.SetupVoid("open", _ => true).SetVoidResult();
	}

	private IRenderedComponent<MokaInlineEdit> RenderEdit(Action<ComponentParameterCollectionBuilder<MokaInlineEdit>>? extra = null, string value = "Report") =>
		Render<MokaInlineEdit>(p =>
		{
			p.Add(x => x.Value, value);
			extra?.Invoke(p);
		});

	private static void Open(IRenderedComponent<MokaInlineEdit> cut) =>
		cut.Find(".moka-inline-edit__display").Click(new MouseEventArgs { Detail = 1 });

	private static IElement Field(IRenderedComponent<MokaInlineEdit> cut) => cut.Find("input");

	[Fact]
	public void TheDisplay_IsAButton_NamedAfterTheText()
	{
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit();

		IElement button = cut.Find("button.moka-inline-edit__display");
		Assert.Equal("Edit Report", button.GetAttribute("aria-label"));
		Assert.Equal("Report", cut.Find(".moka-inline-edit__text").TextContent);
		Assert.Empty(cut.FindAll("input"));
	}

	[Fact]
	public void ButtonLabel_ReplacesTheName()
	{
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p.Add(x => x.ButtonLabel, "Rename the report"));

		Assert.Equal("Rename the report", cut.Find("button").GetAttribute("aria-label"));
	}

	[Fact]
	public void AClick_OpensTheField_WithAllOfTheTextSelected()
	{
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit();

		Open(cut);

		Assert.Equal("Report", Field(cut).GetAttribute("value"));
		JSRuntimeInvocation open = _module.VerifyInvoke("open");
		Assert.Equal(0, open.Arguments[2]);
		Assert.Equal(6, open.Arguments[3]);
	}

	[Theory]
	[InlineData("report.final.pdf", MokaInlineEditSelection.Stem, 0, 12)]
	[InlineData("README", MokaInlineEditSelection.Stem, 0, 6)]
	[InlineData(".gitignore", MokaInlineEditSelection.Stem, 0, 10)]
	[InlineData("notes.txt", MokaInlineEditSelection.End, 9, 9)]
	[InlineData("notes.txt", MokaInlineEditSelection.All, 0, 9)]
	public void Selection_PicksTheRangeThatOpens(string text, MokaInlineEditSelection selection, int start, int end)
	{
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p.Add(x => x.Selection, selection), text);

		Open(cut);

		JSRuntimeInvocation open = _module.VerifyInvoke("open");
		Assert.Equal(start, open.Arguments[2]);
		Assert.Equal(end, open.Arguments[3]);
	}

	[Theory]
	[InlineData(4, 4)]
	[InlineData(0, 0)]
	[InlineData(-3, 0)]
	[InlineData(99, 11)]
	public void SelectionEnd_SaysWhereTheSelectionStops(int returned, int end)
	{
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p.Add(x => x.SelectionEnd, _ => returned), "site.tar.gz");

		Open(cut);

		JSRuntimeInvocation open = _module.VerifyInvoke("open");
		Assert.Equal(0, open.Arguments[2]);
		Assert.Equal(end, open.Arguments[3]);
	}

	[Fact]
	public void SelectionEnd_IsGivenTheText_AndWinsOverSelection()
	{
		string? given = null;
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p
			.Add(x => x.Selection, MokaInlineEditSelection.End)
			.Add(x => x.SelectionEnd, text =>
			{
				given = text;
				return 4;
			}), "site.tar.gz");

		Open(cut);

		Assert.Equal("site.tar.gz", given);
		JSRuntimeInvocation open = _module.VerifyInvoke("open");
		Assert.Equal(0, open.Arguments[2]);
		Assert.Equal(4, open.Arguments[3]);
	}

	[Fact]
	public void Enter_SavesAndClosesAndGivesFocusBack()
	{
		string? saved = null;
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p.Add(x => x.ValueChanged, value => saved = value));
		Open(cut);

		Field(cut).Input("Budget");
		Field(cut).KeyDown(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal("Budget", saved);
		Assert.Empty(cut.FindAll("input"));
		JSInterop.VerifyFocusAsyncInvoke();
	}

	[Fact]
	public void Escape_CancelsAndGivesFocusBack()
	{
		bool saved = false;
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p.Add(x => x.ValueChanged, _ => saved = true));
		Open(cut);

		Field(cut).Input("Budget");
		Field(cut).KeyDown(new KeyboardEventArgs { Key = "Escape" });

		Assert.False(saved);
		Assert.Equal("Report", cut.Find(".moka-inline-edit__text").TextContent);
		JSInterop.VerifyFocusAsyncInvoke();
	}

	[Fact]
	public void ClickingAway_Saves_AndLeavesFocusWhereItWent()
	{
		string? saved = null;
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p.Add(x => x.ValueChanged, value => saved = value));
		Open(cut);

		Field(cut).Input("Budget");
		Field(cut).Blur();

		Assert.Equal("Budget", saved);
		Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void EnterWhileComposing_DoesNotSave()
	{
		bool saved = false;
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p.Add(x => x.ValueChanged, _ => saved = true));
		Open(cut);

		Field(cut).Input("日本");
		Field(cut).KeyDown(new KeyboardEventArgs { Key = "Enter", IsComposing = true });

		Assert.False(saved);
		Assert.Single(cut.FindAll("input"));
	}

	// A render with the text typed so far arrives after the next key has landed, and writing it back dropped that key.
	[Fact]
	public void Typing_NeverWritesBackToTheField()
	{
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit();
		Open(cut);

		Field(cut).Input("B");
		Field(cut).Input("Bu");

		Assert.Equal("Report", Field(cut).GetAttribute("value"));
	}

	[Fact]
	public void AParentRender_LeavesAnOpenFieldOpen()
	{
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit();
		Open(cut);

		cut.Render(p => p.Add(x => x.Value, "Report"));

		Assert.Single(cut.FindAll("input"));
	}

	[Fact]
	public void UnchangedText_ClosesWithoutSaving()
	{
		int saves = 0;
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p.Add(x => x.Save, _ =>
		{
			saves++;
			return Task.CompletedTask;
		}));
		Open(cut);

		Field(cut).KeyDown(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal(0, saves);
		Assert.Empty(cut.FindAll("input"));
	}

	[Fact]
	public void SaveUnchanged_SavesASuggestedName()
	{
		string? saved = null;
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p
			.Add(x => x.SaveUnchanged, true)
			.Add(x => x.Save, name =>
			{
				saved = name;
				return Task.CompletedTask;
			}));
		Open(cut);

		Field(cut).KeyDown(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal("Report", saved);
	}

	[Fact]
	public void SpacesAtTheEnds_AreTrimmed_UnlessAskedNotTo()
	{
		string? saved = null;
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p.Add(x => x.ValueChanged, value => saved = value));
		Open(cut);
		Field(cut).Input("  Budget ");
		Field(cut).KeyDown(new KeyboardEventArgs { Key = "Enter" });
		Assert.Equal("Budget", saved);

		IRenderedComponent<MokaInlineEdit> kept = RenderEdit(p => p
			.Add(x => x.Trim, false)
			.Add(x => x.ValueChanged, value => saved = value));
		Open(kept);
		Field(kept).Input("  Budget ");
		Field(kept).KeyDown(new KeyboardEventArgs { Key = "Enter" });
		Assert.Equal("  Budget ", saved);
	}

	[Fact]
	public void Validate_BlocksTheSave_AndShowsTheError()
	{
		bool saved = false;
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p
			.Add(x => x.Validate, text => text.Contains('/', StringComparison.Ordinal) ? "No slashes" : null)
			.Add(x => x.ValueChanged, _ => saved = true));
		Open(cut);

		Field(cut).Input("a/b");
		Field(cut).KeyDown(new KeyboardEventArgs { Key = "Enter" });

		Assert.False(saved);
		IElement error = cut.Find(".moka-inline-edit__error");
		Assert.Equal("No slashes", error.TextContent);
		Assert.Equal("alert", error.GetAttribute("role"));
		Assert.Equal("true", Field(cut).GetAttribute("aria-invalid"));
		Assert.Equal(error.Id, Field(cut).GetAttribute("aria-describedby"));
	}

	[Fact]
	public void BlankText_IsBlocked_UnlessAllowed()
	{
		string? saved = null;
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p
			.Add(x => x.BlankText, "Needs a name")
			.Add(x => x.ValueChanged, value => saved = value));
		Open(cut);
		Field(cut).Input("   ");
		Field(cut).KeyDown(new KeyboardEventArgs { Key = "Enter" });
		Assert.Null(saved);
		Assert.Equal("Needs a name", cut.Find(".moka-inline-edit__error").TextContent);

		IRenderedComponent<MokaInlineEdit> allowed = RenderEdit(p => p
			.Add(x => x.AllowBlank, true)
			.Add(x => x.ValueChanged, value => saved = value));
		Open(allowed);
		Field(allowed).Input("");
		Field(allowed).KeyDown(new KeyboardEventArgs { Key = "Enter" });
		Assert.Equal("", saved);
	}

	[Fact]
	public void TypingAgain_ClearsTheError()
	{
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p.Add(x => x.Validate, _ => "No"));
		Open(cut);
		Field(cut).Input("x");
		Field(cut).KeyDown(new KeyboardEventArgs { Key = "Enter" });
		Assert.Single(cut.FindAll(".moka-inline-edit__error"));

		Field(cut).Input("xy");

		Assert.Empty(cut.FindAll(".moka-inline-edit__error"));
	}

	[Fact]
	public async Task WhileSaving_TheFieldStaysOpenAndReadOnly_AndABlurDoesNotSaveAgain()
	{
		TaskCompletionSource running = new();
		int saves = 0;
		string? value = null;
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p
			.Add(x => x.Save, _ =>
			{
				saves++;
				return running.Task;
			})
			.Add(x => x.ValueChanged, v => value = v));
		Open(cut);
		await Field(cut).InputAsync(new ChangeEventArgs { Value = "Budget" });

		Task saving = Field(cut).KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.True(Field(cut).HasAttribute("readonly"));
		Assert.Equal("true", Field(cut).GetAttribute("aria-busy"));
		Assert.Single(cut.FindAll(".moka-inline-edit__saving"));
		await Field(cut).BlurAsync(new FocusEventArgs());
		Assert.Equal(1, saves);
		Assert.Null(value);

		await cut.InvokeAsync(running.SetResult);
		await saving;

		Assert.Equal("Budget", value);
		Assert.Empty(cut.FindAll("input"));
	}

	[Fact]
	public void AThrownSave_ShowsTheMessage_AndKeepsTheFieldOpen()
	{
		bool changed = false;
		List<string> tried = [];
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p
			.Add(x => x.Save, name =>
			{
				tried.Add(name);
				throw new InvalidOperationException("The name is taken");
			})
			.Add(x => x.ValueChanged, _ => changed = true));
		Open(cut);

		Field(cut).Input("Budget");
		Field(cut).KeyDown(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal("The name is taken", cut.Find(".moka-inline-edit__error").TextContent);
		Assert.False(Field(cut).HasAttribute("readonly"));
		Assert.False(changed);

		Field(cut).KeyDown(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal(["Budget", "Budget"], tried);
	}

	[Fact]
	public void DoubleClickTrigger_IgnoresOneClick_ButTakesTheKeyboard()
	{
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p.Add(x => x.Trigger, MokaInlineEditTrigger.DoubleClick));
		IElement button = cut.Find("button");

		button.Click(new MouseEventArgs { Detail = 1 });
		Assert.Empty(cut.FindAll("input"));

		button.DoubleClick();
		Assert.Single(cut.FindAll("input"));

		IRenderedComponent<MokaInlineEdit> keyboard = RenderEdit(p => p.Add(x => x.Trigger, MokaInlineEditTrigger.DoubleClick));
		keyboard.Find("button").Click(new MouseEventArgs { Detail = 0 });
		Assert.Single(keyboard.FindAll("input"));
	}

	[Fact]
	public void ManualTrigger_IsPlainText_AndOpensFromTheParent()
	{
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p.Add(x => x.Trigger, MokaInlineEditTrigger.Manual));
		Assert.Empty(cut.FindAll("button"));
		Assert.Single(cut.FindAll(".moka-inline-edit__static"));

		cut.Render(p => p.Add(x => x.Editing, true));

		Assert.Single(cut.FindAll("input"));
		_module.VerifyInvoke("open");
	}

	[Fact]
	public void Editing_ReportsWhatThePersonDid_AndNotWhatTheParentDid()
	{
		List<bool> reported = [];
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p.Add(x => x.EditingChanged, reported.Add));

		Open(cut);
		Assert.Equal([true], reported);

		Field(cut).KeyDown(new KeyboardEventArgs { Key = "Escape" });
		Assert.Equal([true, false], reported);

		cut.Render(p => p.Add(x => x.Editing, true));
		Assert.Single(cut.FindAll("input"));
		cut.Render(p => p.Add(x => x.Editing, false));
		Assert.Empty(cut.FindAll("input"));
		Assert.Equal([true, false], reported);
	}

	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public void DisabledOrReadOnly_CannotBeEdited(bool disabled, bool readOnly)
	{
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p
			.Add(x => x.Disabled, disabled)
			.Add(x => x.ReadOnly, readOnly)
			.Add(x => x.Editing, true));

		Assert.Empty(cut.FindAll("button"));
		Assert.Empty(cut.FindAll("input"));
	}

	[Fact]
	public void AnEmptyValue_ShowsThePlaceholder()
	{
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p.Add(x => x.Placeholder, "Add a title"), "");

		Assert.Equal("Add a title", cut.Find(".moka-inline-edit__text--empty").TextContent);
		Assert.Equal("Edit", cut.Find("button").GetAttribute("aria-label"));
	}

	[Fact]
	public void DisplayTemplate_DrawsTheText()
	{
		RenderFragment<string> template = text => builder =>
		{
			builder.OpenElement(0, "em");
			builder.AddContent(1, text.ToUpperInvariant());
			builder.CloseElement();
		};
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p.Add(x => x.DisplayTemplate, template));

		Assert.Equal("REPORT", cut.Find("em").TextContent);
	}

	[Fact]
	public void TheField_IsNamed_AndCarriesOtherAttributes()
	{
		IRenderedComponent<MokaInlineEdit> cut = RenderEdit(p => p
			.Add(x => x.Label, "Title")
			.AddUnmatched("data-test", "x")
			.AddUnmatched("aria-label", "Ignored"));
		Open(cut);

		Assert.Equal("Title", Field(cut).GetAttribute("aria-label"));
		Assert.Equal("x", Field(cut).GetAttribute("data-test"));
	}

	[Fact]
	public void InAForm_AnErrorFromTheFormShows_AndEnterSaves()
	{
		Model model = new() { Title = "One" };
		EditContext context = new(model);
		ValidationMessageStore messages = new(context);
		messages.Add(() => model.Title, "Title is taken");
		IRenderedComponent<CascadingValue<EditContext>> cut = Render<CascadingValue<EditContext>>(p => p
			.Add(x => x.Value, context)
			.Add(x => x.IsFixed, true)
			.AddChildContent<MokaInlineEdit>(child => child
				.Add(x => x.Value, model.Title)
				.Add(x => x.ValueExpression, () => model.Title)
				.Add(x => x.ValueChanged, value => model.Title = value)));

		Assert.Equal("Title is taken", cut.Find(".moka-inline-edit__error").TextContent);
	}

	private sealed class Model
	{
		public string Title { get; set; } = "";
	}
}
