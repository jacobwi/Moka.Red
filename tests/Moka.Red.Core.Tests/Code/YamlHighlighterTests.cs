#pragma warning disable CA5394 // Seeded Random keeps the fuzz test repeatable; nothing here is security related
using System.Text;
using Moka.Red.Core.Code;

namespace Moka.Red.Core.Tests.Code;

public sealed class YamlHighlighterTests
{
	private const string Code = """
		%YAML 1.2
		---
		# settings
		name: Moka Red
		version: 0.9.1
		count: 42
		ratio: -1.5e7
		mask: 0x1F
		mode: 0o17
		top: .inf
		odd: .NaN
		enabled: true
		missing: ~
		empty: null
		quoted: "say \"hi\"\n"
		single: 'it''s'
		"quoted key": text
		url: http://example.com:8080/a
		tight:value
		hash: a#b # trailing note
		defaults: &defaults
		  adapter: postgres
		dev:
		  <<: *defaults
		  tags: !!set {alpha, beta}
		list:
		  - one
		  - [two, 64, False]
		  - {kk: vv, "jj": 77}
		script: |
		  dotnet build

		  dotnet test # still the script
		folded: >-
		  first
		  second
		after: done
		custom: !local thing
		? complex
		: answer
		...
		""";

	private static readonly IReadOnlyList<MokaCodeSpan> Spans = CodeSpanAssert.Highlight(MokaCodeHighlighters.Yaml, Code);

	[Theory]
	[InlineData("%YAML 1.2", MokaCodeTokenKind.Directive, 0)]
	[InlineData("---", MokaCodeTokenKind.Punctuation, 0)]
	[InlineData("# settings", MokaCodeTokenKind.Comment, 0)]
	[InlineData("name", MokaCodeTokenKind.Key, 0)]
	[InlineData("Moka Red", MokaCodeTokenKind.Value, 0)]
	[InlineData("0.9.1", MokaCodeTokenKind.Value, 0)]
	[InlineData("42", MokaCodeTokenKind.Number, 0)]
	[InlineData("-1.5e7", MokaCodeTokenKind.Number, 0)]
	[InlineData("0x1F", MokaCodeTokenKind.Number, 0)]
	[InlineData("0o17", MokaCodeTokenKind.Number, 0)]
	[InlineData(".inf", MokaCodeTokenKind.Number, 0)]
	[InlineData(".NaN", MokaCodeTokenKind.Number, 0)]
	[InlineData("true", MokaCodeTokenKind.Keyword, 0)]
	[InlineData("~", MokaCodeTokenKind.Keyword, 0)]
	[InlineData("null", MokaCodeTokenKind.Keyword, 0)]
	[InlineData("\"say \\\"hi\\\"\\n\"", MokaCodeTokenKind.String, 0)]
	[InlineData("'it''s'", MokaCodeTokenKind.String, 0)]
	[InlineData("\"quoted key\"", MokaCodeTokenKind.Key, 0)]
	[InlineData("http://example.com:8080/a", MokaCodeTokenKind.Value, 0)]
	[InlineData("tight:value", MokaCodeTokenKind.Value, 0)]
	[InlineData("a#b", MokaCodeTokenKind.Value, 0)]
	[InlineData("# trailing note", MokaCodeTokenKind.Comment, 0)]
	[InlineData("&defaults", MokaCodeTokenKind.Directive, 0)]
	[InlineData("adapter", MokaCodeTokenKind.Key, 0)]
	[InlineData("<<", MokaCodeTokenKind.Key, 0)]
	[InlineData("*defaults", MokaCodeTokenKind.Directive, 0)]
	[InlineData("!!set", MokaCodeTokenKind.Type, 0)]
	[InlineData("{", MokaCodeTokenKind.Punctuation, 0)]
	[InlineData("alpha", MokaCodeTokenKind.Value, 0)]
	[InlineData(",", MokaCodeTokenKind.Punctuation, 0)]
	[InlineData("}", MokaCodeTokenKind.Punctuation, 0)]
	[InlineData("one", MokaCodeTokenKind.Value, 0)]
	[InlineData("[", MokaCodeTokenKind.Punctuation, 0)]
	[InlineData("two", MokaCodeTokenKind.Value, 0)]
	[InlineData("64", MokaCodeTokenKind.Number, 0)]
	[InlineData("False", MokaCodeTokenKind.Keyword, 0)]
	[InlineData("kk", MokaCodeTokenKind.Key, 0)]
	[InlineData("vv", MokaCodeTokenKind.Value, 0)]
	[InlineData("\"jj\"", MokaCodeTokenKind.Key, 0)]
	[InlineData("77", MokaCodeTokenKind.Number, 0)]
	[InlineData("|", MokaCodeTokenKind.Punctuation, 0)]
	[InlineData("dotnet build", MokaCodeTokenKind.String, 0)]
	[InlineData("dotnet test # still the script", MokaCodeTokenKind.String, 0)]
	[InlineData(">-", MokaCodeTokenKind.Punctuation, 0)]
	[InlineData("second", MokaCodeTokenKind.String, 0)]
	[InlineData("after", MokaCodeTokenKind.Key, 0)]
	[InlineData("done", MokaCodeTokenKind.Value, 0)]
	[InlineData("!local", MokaCodeTokenKind.Type, 0)]
	[InlineData("thing", MokaCodeTokenKind.Value, 0)]
	[InlineData("?", MokaCodeTokenKind.Punctuation, 0)]
	[InlineData("complex", MokaCodeTokenKind.Key, 0)]
	[InlineData("answer", MokaCodeTokenKind.Value, 0)]
	[InlineData("...", MokaCodeTokenKind.Punctuation, 0)]
	public void EachPart_HasItsKind(string fragment, MokaCodeTokenKind kind, int occurrence) =>
		Assert.Equal(kind, CodeSpanAssert.KindOf(Code, Spans, fragment, occurrence));

	[Fact]
	public void ASequenceDash_IsPunctuation_AndADashInAValueIsNot()
	{
		const string yaml = "- one\n- -2\n- a - b\n";
		IReadOnlyList<MokaCodeSpan> spans = CodeSpanAssert.Highlight(MokaCodeHighlighters.Yaml, yaml);

		Assert.Equal(MokaCodeTokenKind.Punctuation, CodeSpanAssert.KindOf(yaml, spans, "-", 0));
		Assert.Equal(MokaCodeTokenKind.Number, CodeSpanAssert.KindOf(yaml, spans, "-2"));
		Assert.Equal(MokaCodeTokenKind.Value, CodeSpanAssert.KindOf(yaml, spans, "a - b"));
	}

	// The lines after the header that are indented past its key are one string, the blank lines among them included.
	[Theory]
	[InlineData("\n")]
	[InlineData("\r\n")]
	public void ABlockScalar_IsOneString_UpToTheFirstLineThatIsntIndentedPastItsKey(string newline)
	{
		ArgumentNullException.ThrowIfNull(newline);
		string yaml = string.Join(newline, "steps:", "  - run: |", "      dotnet build", "", "        --no-restore", "    name: build", "");
		IReadOnlyList<MokaCodeSpan> spans = CodeSpanAssert.Highlight(MokaCodeHighlighters.Yaml, yaml);

		int start = yaml.IndexOf("dotnet", StringComparison.Ordinal);
		int end = yaml.IndexOf("--no-restore", StringComparison.Ordinal) + "--no-restore".Length;
		MokaCodeSpan body = Assert.Single(spans, span => span.Kind == MokaCodeTokenKind.String);
		Assert.Equal(start, body.Start);
		Assert.Equal(end, body.Start + body.Length - (newline.Length - 1));
		Assert.Equal(MokaCodeTokenKind.Key, CodeSpanAssert.KindOf(yaml, spans, "name"));
		Assert.Equal(MokaCodeTokenKind.Value, CodeSpanAssert.KindOf(yaml, spans, "build", 1));
	}

	[Fact]
	public void ABlockScalarHeader_TakesItsIndicatorsAndAComment()
	{
		const string yaml = "a: |2- # kept\n    two spaces in\nb: >+\n\nc: 1\n";
		IReadOnlyList<MokaCodeSpan> spans = CodeSpanAssert.Highlight(MokaCodeHighlighters.Yaml, yaml);

		Assert.Equal(MokaCodeTokenKind.Punctuation, CodeSpanAssert.KindOf(yaml, spans, "|2-"));
		Assert.Equal(MokaCodeTokenKind.Comment, CodeSpanAssert.KindOf(yaml, spans, "# kept"));
		Assert.Equal(MokaCodeTokenKind.String, CodeSpanAssert.KindOf(yaml, spans, "  two spaces in"));
		Assert.Equal(MokaCodeTokenKind.Punctuation, CodeSpanAssert.KindOf(yaml, spans, ">+"));
		Assert.Equal(MokaCodeTokenKind.Key, CodeSpanAssert.KindOf(yaml, spans, "c", 1));
	}

	[Fact]
	public void ABlockScalarWithNothingIndented_IsEmpty()
	{
		const string yaml = "a: |\nb: text\n";
		IReadOnlyList<MokaCodeSpan> spans = CodeSpanAssert.Highlight(MokaCodeHighlighters.Yaml, yaml);

		Assert.DoesNotContain(spans, span => span.Kind == MokaCodeTokenKind.String);
		Assert.Equal(MokaCodeTokenKind.Key, CodeSpanAssert.KindOf(yaml, spans, "b"));
	}

	[Fact]
	public void ATopLevelBlockScalar_RunsToTheNextDocument()
	{
		const string yaml = "--- |\nline one\nline two\n---\nkey: 1\n";
		IReadOnlyList<MokaCodeSpan> spans = CodeSpanAssert.Highlight(MokaCodeHighlighters.Yaml, yaml);

		Assert.Equal(MokaCodeTokenKind.String, CodeSpanAssert.KindOf(yaml, spans, "line one\nline two"));
		Assert.Equal(MokaCodeTokenKind.Punctuation, CodeSpanAssert.KindOf(yaml, spans, "---", 1));
		Assert.Equal(MokaCodeTokenKind.Key, CodeSpanAssert.KindOf(yaml, spans, "key"));
	}

	[Fact]
	public void AQuotedScalar_RunsOverLines_AndOneLeftOpenStopsAtTheNextDocument()
	{
		const string yaml = "a: \"one\n  two\"\nb: 'open\nstill open\n---\nc: 1\n";
		IReadOnlyList<MokaCodeSpan> spans = CodeSpanAssert.Highlight(MokaCodeHighlighters.Yaml, yaml);

		Assert.Equal(MokaCodeTokenKind.String, CodeSpanAssert.KindOf(yaml, spans, "\"one\n  two\""));
		Assert.Equal(MokaCodeTokenKind.Key, CodeSpanAssert.KindOf(yaml, spans, "b"));
		Assert.Equal(MokaCodeTokenKind.String, CodeSpanAssert.KindOf(yaml, spans, "'open\nstill open"));
		Assert.Equal(MokaCodeTokenKind.Punctuation, CodeSpanAssert.KindOf(yaml, spans, "---"));
		Assert.Equal(MokaCodeTokenKind.Key, CodeSpanAssert.KindOf(yaml, spans, "c"));
	}

	[Fact]
	public void AFlowCollection_RunsOverLines()
	{
		const string yaml = "matrix: {\n  os: [linux, windows],\n  \"node\": 22\n}\nnext: [aa,bb]\n";
		IReadOnlyList<MokaCodeSpan> spans = CodeSpanAssert.Highlight(MokaCodeHighlighters.Yaml, yaml);

		Assert.Equal(MokaCodeTokenKind.Key, CodeSpanAssert.KindOf(yaml, spans, "os"));
		Assert.Equal(MokaCodeTokenKind.Value, CodeSpanAssert.KindOf(yaml, spans, "linux"));
		Assert.Equal(MokaCodeTokenKind.Key, CodeSpanAssert.KindOf(yaml, spans, "\"node\""));
		Assert.Equal(MokaCodeTokenKind.Number, CodeSpanAssert.KindOf(yaml, spans, "22"));
		Assert.Equal(MokaCodeTokenKind.Key, CodeSpanAssert.KindOf(yaml, spans, "next"));
		Assert.Equal(MokaCodeTokenKind.Value, CodeSpanAssert.KindOf(yaml, spans, "aa"));
		Assert.Equal(MokaCodeTokenKind.Punctuation, CodeSpanAssert.KindOf(yaml, spans, ",", 2));
		Assert.Equal(MokaCodeTokenKind.Value, CodeSpanAssert.KindOf(yaml, spans, "bb"));
	}

	[Theory]
	[InlineData("1", MokaCodeTokenKind.Number)]
	[InlineData("+12", MokaCodeTokenKind.Number)]
	[InlineData("1.", MokaCodeTokenKind.Number)]
	[InlineData(".5", MokaCodeTokenKind.Number)]
	[InlineData("1e3", MokaCodeTokenKind.Number)]
	[InlineData("-.INF", MokaCodeTokenKind.Number)]
	[InlineData("0xFF", MokaCodeTokenKind.Number)]
	[InlineData("0o8", MokaCodeTokenKind.Value)]
	[InlineData("0x", MokaCodeTokenKind.Value)]
	[InlineData("1e", MokaCodeTokenKind.Value)]
	[InlineData(".", MokaCodeTokenKind.Value)]
	[InlineData("2024-01-01", MokaCodeTokenKind.Value)]
	[InlineData("TRUE", MokaCodeTokenKind.Keyword)]
	[InlineData("Null", MokaCodeTokenKind.Keyword)]
	[InlineData("yes", MokaCodeTokenKind.Value)]
	[InlineData("nULL", MokaCodeTokenKind.Value)]
	public void APlainValue_FollowsTheCoreSchema(string value, MokaCodeTokenKind kind)
	{
		string yaml = $"key: {value}\n";

		Assert.Equal(kind, CodeSpanAssert.KindOf(yaml, CodeSpanAssert.Highlight(MokaCodeHighlighters.Yaml, yaml), value));
	}

	// Text built from YAML's own punctuation in every order never throws, and the spans stay in order inside it.
	[Fact]
	public void RandomText_NeverThrows_AndItsSpansFitTheText()
	{
		const string alphabet = "ab9 :-?#|>'\"\\[]{},!&*%.~\n\r\t+eE<";
		Random random = new(20261119);
		StringBuilder text = new();
		for (int run = 0; run < 5000; run++)
		{
			text.Clear();
			int length = random.Next(0, 160);
			for (int i = 0; i < length; i++)
			{
				text.Append(alphabet[random.Next(alphabet.Length)]);
			}

			string code = text.ToString();
			List<MokaCodeSpan> spans = [.. MokaCodeHighlighters.Yaml(code)];
			CodeSpanAssert.Valid(code, spans, code);
			Assert.Equal(spans, MokaCodeHighlighters.Normalize(code, spans));
		}
	}
}
