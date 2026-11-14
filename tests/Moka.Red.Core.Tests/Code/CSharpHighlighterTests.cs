using Moka.Red.Core.Code;

namespace Moka.Red.Core.Tests.Code;

public sealed class CSharpHighlighterTests
{
	private const string Code = """"
		// A greeter.
		#nullable enable
		[Parameter, EditorRequired]
		[assembly: InternalsVisibleTo("Tests")]
		public sealed class Greeter : ComponentBase, IDisposable
		{
			private readonly List<string> _names = new();
			public Task<int> CountAsync(Foo foo) => Task.FromResult(42);
			string a = "x \" y", b = @"c "" d", c = $"Hi {name.ToUpper()} {price:N2}!", d = $$"""{{total}} {y}""";
			char e = '\'';
			double f = 1.5e3, g = 0x1F;
			var item = new Dictionary<string, Item>();
			bool h = x is Shape shape && y < z;
			object o = typeof(Widget);
			/* a block
			   comment */
			Run<Widget>(value);
			return IsOpen ? "open" : "shut";
		}
		"""";

	private static readonly IReadOnlyList<MokaCodeSpan> Spans = CodeSpanAssert.Highlight(MokaCodeHighlighters.CSharp, Code);

	[Theory]
	[InlineData("// A greeter.", MokaCodeTokenKind.Comment, 0)]
	[InlineData("#nullable enable", MokaCodeTokenKind.Directive, 0)]
	[InlineData("Parameter", MokaCodeTokenKind.Type, 0)]
	[InlineData("EditorRequired", MokaCodeTokenKind.Type, 0)]
	[InlineData("assembly", MokaCodeTokenKind.Keyword, 0)]
	[InlineData("InternalsVisibleTo", MokaCodeTokenKind.Type, 0)]
	[InlineData("\"Tests\"", MokaCodeTokenKind.String, 0)]
	[InlineData("public", MokaCodeTokenKind.Keyword, 0)]
	[InlineData("class", MokaCodeTokenKind.Keyword, 0)]
	[InlineData("Greeter", MokaCodeTokenKind.Type, 0)]
	[InlineData("ComponentBase", MokaCodeTokenKind.Type, 0)]
	[InlineData("IDisposable", MokaCodeTokenKind.Type, 0)]
	[InlineData("List", MokaCodeTokenKind.Type, 0)]
	[InlineData("string", MokaCodeTokenKind.Keyword, 0)]
	[InlineData("Task", MokaCodeTokenKind.Type, 0)]
	[InlineData("CountAsync", MokaCodeTokenKind.Plain, 0)]
	[InlineData("Foo", MokaCodeTokenKind.Type, 0)]
	[InlineData("FromResult", MokaCodeTokenKind.Plain, 0)]
	[InlineData("42", MokaCodeTokenKind.Number, 0)]
	[InlineData("\"x \\\" y\"", MokaCodeTokenKind.String, 0)]
	[InlineData("@\"c \"\" d\"", MokaCodeTokenKind.String, 0)]
	[InlineData("$\"Hi ", MokaCodeTokenKind.String, 0)]
	[InlineData("name", MokaCodeTokenKind.Plain, 0)]
	[InlineData(":N2", MokaCodeTokenKind.String, 0)]
	[InlineData("total", MokaCodeTokenKind.Plain, 0)]
	[InlineData(" {y}\"\"\"", MokaCodeTokenKind.String, 0)]
	[InlineData("'\\''", MokaCodeTokenKind.String, 0)]
	[InlineData("1.5e3", MokaCodeTokenKind.Number, 0)]
	[InlineData("0x1F", MokaCodeTokenKind.Number, 0)]
	[InlineData("var", MokaCodeTokenKind.Keyword, 0)]
	[InlineData("Dictionary", MokaCodeTokenKind.Type, 0)]
	[InlineData("Item", MokaCodeTokenKind.Type, 0)]
	[InlineData("Shape", MokaCodeTokenKind.Type, 0)]
	[InlineData("<", MokaCodeTokenKind.Punctuation, 3)]
	[InlineData("Widget", MokaCodeTokenKind.Type, 0)]
	[InlineData("/* a block", MokaCodeTokenKind.Comment, 0)]
	[InlineData("comment */", MokaCodeTokenKind.Comment, 0)]
	[InlineData("Run", MokaCodeTokenKind.Plain, 0)]
	[InlineData("Widget", MokaCodeTokenKind.Type, 1)]
	[InlineData("IsOpen", MokaCodeTokenKind.Plain, 0)]
	[InlineData("return", MokaCodeTokenKind.Keyword, 0)]
	public void EachPart_HasItsKind(string fragment, MokaCodeTokenKind kind, int occurrence) =>
		Assert.Equal(kind, CodeSpanAssert.KindOf(Code, Spans, fragment, occurrence));

	// The braces of a hole are punctuation, and a raw string's braces count as its dollars say.
	[Fact]
	public void InterpolationHoles_AreCSharpBetweenTheirBraces()
	{
		const string code = "var s = $\"a{x + 1}b\";";
		IReadOnlyList<MokaCodeSpan> spans = CodeSpanAssert.Highlight(MokaCodeHighlighters.CSharp, code);

		Assert.Equal(MokaCodeTokenKind.String, CodeSpanAssert.KindOf(code, spans, "$\"a"));
		Assert.Equal(MokaCodeTokenKind.Punctuation, CodeSpanAssert.KindOf(code, spans, "{"));
		Assert.Equal(MokaCodeTokenKind.Number, CodeSpanAssert.KindOf(code, spans, "1"));
		Assert.Equal(MokaCodeTokenKind.String, CodeSpanAssert.KindOf(code, spans, "b\""));
	}

	[Fact]
	public void AStringLeftOpen_EndsWithItsLine()
	{
		const string code = "var s = \"never closed\nint x = 1;";
		IReadOnlyList<MokaCodeSpan> spans = CodeSpanAssert.Highlight(MokaCodeHighlighters.CSharp, code);

		Assert.Equal(MokaCodeTokenKind.String, CodeSpanAssert.KindOf(code, spans, "\"never closed"));
		Assert.Equal(MokaCodeTokenKind.Keyword, CodeSpanAssert.KindOf(code, spans, "int"));
	}

	[Fact]
	public void ARawString_EndsAtAsManyQuotesAsOpenedIt()
	{
		const string code = "var s = \"\"\"\"a \"\"\" b\"\"\"\"; int x;";
		IReadOnlyList<MokaCodeSpan> spans = CodeSpanAssert.Highlight(MokaCodeHighlighters.CSharp, code);

		Assert.Equal(MokaCodeTokenKind.String, CodeSpanAssert.KindOf(code, spans, "\"\"\"\"a \"\"\" b\"\"\"\""));
		Assert.Equal(MokaCodeTokenKind.Keyword, CodeSpanAssert.KindOf(code, spans, "int"));
	}
}
