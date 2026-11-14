using Bunit;
using Moka.Red.Core.Code;
using Moka.Red.Primitives.CodeBlock;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaCodeBlockHighlighterTests : BunitContext
{
	private const string Stylesheet = "MokaCodeBlock.razor.css";

	private IRenderedComponent<MokaCodeBlock> RenderBlock(string code, Func<string, IEnumerable<MokaCodeSpan>>? highlighter) =>
		Render<MokaCodeBlock>(p => p
			.Add(x => x.Code, code)
			.Add(x => x.Highlighter, highlighter));

	private static string Lines(IRenderedComponent<MokaCodeBlock> cut) =>
		string.Join('\n', cut.FindAll(".moka-code-block__line").Select(line => line.TextContent));

	[Fact]
	public void AHighlighter_ColorsTheCode_AndTheLinesStillHoldItsText()
	{
		const string code = "var count = 1; // one\nreturn \"done\";";
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock(code, MokaCodeHighlighters.CSharp);

		Assert.Contains("moka-code-block--highlighted", cut.Find(".moka-code-block").ClassList);
		Assert.Equal("var", cut.Find(".moka-code-block__token--keyword").TextContent);
		Assert.Equal("1", cut.Find(".moka-code-block__token--number").TextContent);
		Assert.Equal("// one", cut.Find(".moka-code-block__token--comment").TextContent);
		Assert.Equal("\"done\"", cut.Find(".moka-code-block__token--string").TextContent);
		Assert.Equal(code, Lines(cut));
		Assert.Equal(".moka-code-block__line", cut.Find(".moka-code-block__copy").GetAttribute("data-moka-copy-from"));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("csharp")]
	public void WindowsLineEndings_LeaveNoCarriageReturnOnAnyLine(string? language)
	{
		const string code = "var a = 1;\r\nvar b = 2;\r\nreturn a;";
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock(code, MokaCodeHighlighters.For(language));

		Assert.Equal(["var a = 1;", "var b = 2;", "return a;"], cut.FindAll(".moka-code-block__line").Select(line => line.TextContent));
		Assert.DoesNotContain('\r', cut.Markup);
	}

	[Fact]
	public void ASpanAcrossLines_IsCutAtEachBreak()
	{
		const string code = "a /* one\ntwo */ b";
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock(code, _ => [new MokaCodeSpan(2, 13, MokaCodeTokenKind.Comment)]);

		Assert.Equal(["/* one", "two */"], cut.FindAll(".moka-code-block__token--comment").Select(t => t.TextContent));
		Assert.All(cut.FindAll(".moka-code-block__token"), token => Assert.Equal("moka-code-block__line", token.ParentElement!.ClassName));
		Assert.Equal(code, Lines(cut));
	}

	// A highlighter is the app's code, so the block copes with spans out of order, overlapping or past the end.
	[Fact]
	public void UntidySpans_AreSortedClippedAndCut()
	{
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock("abcdef", _ =>
		[
			new MokaCodeSpan(4, 10, MokaCodeTokenKind.Number),
			new MokaCodeSpan(0, 3, MokaCodeTokenKind.Keyword),
			new MokaCodeSpan(2, 3, MokaCodeTokenKind.Comment),
			new MokaCodeSpan(1, 0, MokaCodeTokenKind.Key)
		]);

		Assert.Equal(["abc", "de", "f"], cut.FindAll(".moka-code-block__token").Select(t => t.TextContent));
		Assert.Equal("abcdef", Lines(cut));
	}

	[Fact]
	public void EveryKind_HasItsOwnClass_AndPlainDrawsAsText()
	{
		MokaCodeTokenKind[] kinds = Enum.GetValues<MokaCodeTokenKind>();
		string code = new('x', kinds.Length);
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock(code, _ => kinds.Select((kind, i) => new MokaCodeSpan(i, 1, kind)));

		string[] expected = ["heading", "keyword", "key", "value", "literal", "number", "constant", "comment", "punctuation",
			"tag", "attribute", "string", "type", "directive"];
		Assert.Equal(
			expected.Select(kind => $"moka-code-block__token moka-code-block__token--{kind}"),
			cut.FindAll(".moka-code-block__token").Select(t => t.ClassName));
	}

	[Fact]
	public async Task TheHighlighter_RunsOnlyWhenTheCodeOrTheHighlighterChanges()
	{
		int runs = 0;
		Func<string, IEnumerable<MokaCodeSpan>> counting = code =>
		{
			runs++;
			return MokaCodeHighlighters.CSharp(code);
		};
		IRenderedComponent<MokaCodeBlock> cut = RenderBlock("int a;", counting);
		Assert.Equal(1, runs);

		await cut.Find(".moka-code-block__copy").CopiedAsync();
		cut.Render(p => p.Add(x => x.Code, "int a;").Add(x => x.Highlighter, counting));
		Assert.Equal(1, runs);

		cut.Render(p => p.Add(x => x.Code, "int b;"));
		Assert.Equal(2, runs);

		cut.Render(p => p.Add(x => x.Highlighter, MokaCodeHighlighters.CSharp));
		Assert.Equal(2, runs);
		Assert.Equal("int", cut.Find(".moka-code-block__token--keyword").TextContent);

		cut.Render(p => p.Add(x => x.Highlighter, null));
		Assert.Empty(cut.FindAll(".moka-code-block__token"));
		Assert.DoesNotContain("moka-code-block--highlighted", cut.Find(".moka-code-block").ClassList);
		Assert.Equal("int b;", Lines(cut));
	}

	// Language is only a label: it never colors the code by itself.
	[Fact]
	public void ALanguageAlone_ColorsNothing()
	{
		IRenderedComponent<MokaCodeBlock> cut = Render<MokaCodeBlock>(p => p
			.Add(x => x.Code, "var a = 1;")
			.Add(x => x.Language, "csharp"));

		Assert.Empty(cut.FindAll(".moka-code-block__token"));
	}

	// Each kind reads its --moka-code-* token, with an existing color to fall back on until the theme sets the token.
	[Theory]
	[InlineData("tag", "--moka-code-tag")]
	[InlineData("attribute", "--moka-code-attribute")]
	[InlineData("key", "--moka-code-attribute")]
	[InlineData("string", "--moka-code-string")]
	[InlineData("literal", "--moka-code-string")]
	[InlineData("keyword", "--moka-code-keyword")]
	[InlineData("heading", "--moka-code-keyword")]
	[InlineData("type", "--moka-code-type")]
	[InlineData("number", "--moka-code-number")]
	[InlineData("constant", "--moka-code-number")]
	[InlineData("comment", "--moka-code-comment")]
	[InlineData("punctuation", "--moka-code-punctuation")]
	[InlineData("directive", "--moka-code-directive")]
	[InlineData("value", "--moka-code-text")]
	public void EachKind_ReadsItsCodeToken(string kind, string token)
	{
		string color = ScopedCss.Declarations(Stylesheet, $".moka-code-block__line ::deep .moka-code-block__token--{kind}")["color"];

		Assert.StartsWith($"var({token}, ", color, StringComparison.Ordinal);
		Assert.Contains("--moka-color-", color, StringComparison.Ordinal);
	}

	[Fact]
	public void TheKindsList_CoversEveryKindButPlain()
	{
		int covered = typeof(MokaCodeBlockHighlighterTests).GetMethod(nameof(EachKind_ReadsItsCodeToken))!
			.GetCustomAttributes(typeof(InlineDataAttribute), false).Length;

		Assert.Equal(Enum.GetValues<MokaCodeTokenKind>().Length - 1, covered);
	}

	[Fact]
	public void HighlightedCode_TakesTheCodeTextColor()
	{
		string color = ScopedCss.Declarations(Stylesheet, ".moka-code-block--highlighted .moka-code-block__code")["color"];

		Assert.Equal("var(--moka-code-text, var(--moka-color-on-surface))", color);
	}
}
