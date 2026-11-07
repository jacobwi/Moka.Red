using AngleSharp.Dom;
using Bunit;
using Moka.Red.Forms.CodeEditor;

namespace Moka.Red.Forms.Tests.Components;

public class MokaCodeEditorTests : BunitContext
{
	private const string ModulePath = "./_content/Moka.Red.Forms/CodeEditor/MokaCodeEditor.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaCodeEditorTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
		_module.SetupVoid("bindEditor", _ => true).SetVoidResult();
		_module.SetupVoid("unbindEditor", _ => true).SetVoidResult();
		_module.SetupVoid("selectRange", _ => true).SetVoidResult();
	}

	// "[Peer]" a heading, then key = value on the next line.
	private static IEnumerable<MokaCodeSpan> Ini(string text)
	{
		int offset = 0;
		foreach (string line in text.Split('\n'))
		{
			if (line.StartsWith('['))
			{
				yield return new MokaCodeSpan(offset, line.Length, MokaCodeTokenKind.Heading);
			}
			else if (line.IndexOf('=', StringComparison.Ordinal) is var equals and > 0)
			{
				yield return new MokaCodeSpan(offset, equals, MokaCodeTokenKind.Key);
				yield return new MokaCodeSpan(offset + equals, 1, MokaCodeTokenKind.Punctuation);
			}

			offset += line.Length + 1;
		}
	}

	[Fact]
	public void TheHighlighter_ColorsTheCopyBehindTheText()
	{
		IRenderedComponent<MokaCodeEditor> cut = Render<MokaCodeEditor>(p => p
			.Add(x => x.Value, "[Peer]\nPort=51820")
			.Add(x => x.Highlighter, Ini));

		IElement copy = cut.Find(".moka-code-editor__highlight");
		Assert.Equal("[Peer]", copy.QuerySelector(".moka-code-editor__token--heading")!.TextContent);
		Assert.Equal("Port", copy.QuerySelector(".moka-code-editor__token--key")!.TextContent);
		Assert.Equal("[Peer]\nPort=51820\n ", copy.TextContent);
		Assert.Equal("true", copy.GetAttribute("aria-hidden"));
		Assert.Equal("[Peer]\nPort=51820", cut.Find("textarea").GetAttribute("value"));
	}

	// A highlighter is the app's code, so the editor copes with spans out of order, overlapping or past the end.
	[Fact]
	public void UntidySpans_AreSortedClippedAndCut()
	{
		IRenderedComponent<MokaCodeEditor> cut = Render<MokaCodeEditor>(p => p
			.Add(x => x.Value, "abcdef")
			.Add(x => x.Highlighter, _ =>
			[
				new MokaCodeSpan(4, 10, MokaCodeTokenKind.Number),
				new MokaCodeSpan(0, 3, MokaCodeTokenKind.Keyword),
				new MokaCodeSpan(2, 3, MokaCodeTokenKind.Comment),
				new MokaCodeSpan(1, 0, MokaCodeTokenKind.Key)
			]));

		IElement copy = cut.Find(".moka-code-editor__highlight");
		Assert.Equal("abcdef\n ", copy.TextContent);
		Assert.Equal(["abc", "de", "f"], copy.QuerySelectorAll("span").Select(s => s.TextContent));
	}

	[Fact]
	public void LineNumbers_FollowTheText()
	{
		IRenderedComponent<MokaCodeEditor> cut = Render<MokaCodeEditor>(p => p.Add(x => x.Value, "a\nb\nc"));

		Assert.Equal(["1", "2", "3"], cut.FindAll(".moka-code-editor__line-number").Select(n => n.TextContent));

		cut.Find("textarea").Input("a\nb");
		Assert.Equal(2, cut.FindAll(".moka-code-editor__line-number").Count);

		cut.Render(p => p.Add(x => x.ShowLineNumbers, false));
		Assert.Empty(cut.FindAll(".moka-code-editor__gutter"));
	}

	[Fact]
	public void Diagnose_RunsOnTheValue_AndListsTheProblemsErrorsFirst()
	{
		IRenderedComponent<MokaCodeEditor> cut = Render<MokaCodeEditor>(p => p
			.Add(x => x.Value, "[Peer]\nPort=x\nKey")
			.Add(x => x.Diagnose, _ =>
			[
				new MokaCodeDiagnostic(MokaCodeSeverity.Warning, "Key has no value", 3),
				new MokaCodeDiagnostic(MokaCodeSeverity.Error, "Port isn't a number", 2, 6, 1, "WG001")
			]));

		IReadOnlyList<IElement> problems = cut.FindAll(".moka-code-editor__problem");
		Assert.Equal(2, problems.Count);
		Assert.Contains("moka-code-editor__problem--error", problems[0].ClassList);
		Assert.Equal("Ln 2, Col 6", problems[0].QuerySelector(".moka-code-editor__where")!.TextContent);
		Assert.Equal("WG001", problems[0].QuerySelector(".moka-code-editor__code")!.TextContent);
		Assert.Equal(["1 error", "1 warning"], cut.FindAll(".moka-code-editor__count").Select(c => c.TextContent));
		Assert.Contains("moka-code-editor__line-number--error", cut.FindAll(".moka-code-editor__line-number")[1].ClassList);
		Assert.Equal("x", cut.Find(".moka-code-editor__mark--error").TextContent);
		Assert.True(cut.Instance.HasErrors);

		IElement textarea = cut.Find("textarea");
		Assert.Equal("true", textarea.GetAttribute("aria-invalid"));
		Assert.Equal(cut.Find(".moka-code-editor__problems").Id, textarea.GetAttribute("aria-describedby"));
	}

	[Fact]
	public void AProblem_JumpsToItsText()
	{
		IRenderedComponent<MokaCodeEditor> cut = Render<MokaCodeEditor>(p => p
			.Add(x => x.Value, "[Peer]\n  Port = x  ")
			.Add(x => x.Diagnostics, [new MokaCodeDiagnostic(MokaCodeSeverity.Error, "Bad line", 2)]));

		cut.Find(".moka-code-editor__problem").Click();

		JSRuntimeInvocation select = Assert.Single(_module.Invocations["selectRange"]);
		Assert.Equal(7 + 2, select.Arguments[1]);
		Assert.Equal(7 + 10, select.Arguments[2]);
	}

	// Problems the app found describe the text they were found in, so editing hides them until the app passes new ones.
	[Fact]
	public void TheAppsProblems_HideOnceTheTextIsEdited()
	{
		IReadOnlyList<MokaCodeDiagnostic> found = [new MokaCodeDiagnostic(MokaCodeSeverity.Error, "Refused")];
		IRenderedComponent<MokaCodeEditor> cut = Render<MokaCodeEditor>(p => p
			.Add(x => x.Value, "a")
			.Add(x => x.Diagnostics, found));
		Assert.Single(cut.FindAll(".moka-code-editor__problem"));

		cut.Find("textarea").Input("ab");
		Assert.Empty(cut.FindAll(".moka-code-editor__problem"));

		cut.Render(p => p.Add(x => x.Diagnostics, [new MokaCodeDiagnostic(MokaCodeSeverity.Warning, "Again")]));
		Assert.Single(cut.FindAll(".moka-code-editor__problem"));
	}

	[Fact]
	public void Typing_ReportsTheValue_AndChecksAfterAPause()
	{
		string? value = null;
		IReadOnlyList<MokaCodeDiagnostic>? diagnosed = null;
		// No Value: a fixed one would hand the empty text back after each key, as a one-way binding does.
		IRenderedComponent<MokaCodeEditor> cut = Render<MokaCodeEditor>(p => p
			.Add(x => x.ValueChanged, text => value = text)
			.Add(x => x.DiagnoseDelay, 10)
			.Add(x => x.Diagnose, text => text.Contains('!', StringComparison.Ordinal)
				? [new MokaCodeDiagnostic(MokaCodeSeverity.Error, "No shouting", 1, 1, 1)]
				: [])
			.Add(x => x.OnDiagnosed, problems => diagnosed = problems));

		cut.Find("textarea").Input("!");

		Assert.Equal("!", value);
		cut.WaitForAssertion(() => Assert.Single(diagnosed!), TimeSpan.FromSeconds(5));
		Assert.Single(cut.FindAll(".moka-code-editor__problem"));
	}

	[Fact]
	public void ShowProblems_Off_KeepsTheMarksButNotTheList()
	{
		IRenderedComponent<MokaCodeEditor> cut = Render<MokaCodeEditor>(p => p
			.Add(x => x.Value, "abc")
			.Add(x => x.ShowProblems, false)
			.Add(x => x.Diagnostics, [new MokaCodeDiagnostic(MokaCodeSeverity.Warning, "Hm", 1, 2, 1)]));

		Assert.Empty(cut.FindAll(".moka-code-editor__problems"));
		Assert.Equal("b", cut.Find(".moka-code-editor__mark--warning").TextContent);
		Assert.Null(cut.Find("textarea").GetAttribute("aria-describedby"));
	}

	[Fact]
	public void TheWordsCanBeReplaced()
	{
		IRenderedComponent<MokaCodeEditor> cut = Render<MokaCodeEditor>(p => p
			.Add(x => x.Value, "a\nb")
			.Add(x => x.ProblemsLabel, "Probleme")
			.Add(x => x.CountText, (_, count) => $"{count} Fehler")
			.Add(x => x.LocationText, d => $"Z. {d.Line}")
			.Add(x => x.Diagnostics, [new MokaCodeDiagnostic(MokaCodeSeverity.Error, "x", 2)]));

		Assert.Equal("Probleme", cut.Find(".moka-code-editor__problems-title").TextContent);
		Assert.Equal("1 Fehler", cut.Find(".moka-code-editor__count").TextContent);
		Assert.Equal("Z. 2", cut.Find(".moka-code-editor__where").TextContent);
	}

	[Fact]
	public void OnFirstRender_TheModuleBindsTheScrolling()
	{
		IRenderedComponent<MokaCodeEditor> cut = Render<MokaCodeEditor>(p => p.Add(x => x.Value, "a"));
		cut.Render(p => p.Add(x => x.Value, "b"));

		JSRuntimeInvocation bind = Assert.Single(_module.Invocations["bindEditor"]);
		Assert.Equal(4, bind.Arguments.Count);
	}

	[Fact]
	public void Rows_SetTheHeight_AndIdNamesTheTextarea()
	{
		IRenderedComponent<MokaCodeEditor> cut = Render<MokaCodeEditor>(p => p
			.Add(x => x.Rows, 20)
			.Add(x => x.Id, "config")
			.Add(x => x.Label, "Config"));

		Assert.Contains("--_code-rows: 20", cut.Find(".moka-code-editor__frame").GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal("config", cut.Find("textarea").Id);
		Assert.Equal("config", cut.Find("label").GetAttribute("for"));
	}

	private readonly Model _model = new();

	private sealed class Model
	{
		public string Config { get; set; } = "[Interface]";
	}

	// A static page posts the form, so the textarea carries the field's name and its value.
	[Fact]
	public void BoundToAField_TheTextareaIsNamedForAPost()
	{
		IRenderedComponent<MokaCodeEditor> cut = Render<MokaCodeEditor>(p => p
			.Add(x => x.Value, _model.Config)
			.Add(x => x.ValueExpression, () => _model.Config));

		IElement textarea = cut.Find("textarea");
		Assert.Equal("_model.Config", textarea.GetAttribute("name"));
		Assert.Equal("[Interface]", textarea.GetAttribute("value"));
	}
}
