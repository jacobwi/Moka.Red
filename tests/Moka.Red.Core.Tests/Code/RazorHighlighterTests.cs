using Moka.Red.Core.Code;

namespace Moka.Red.Core.Tests.Code;

public sealed class RazorHighlighterTests
{
	private const string Code = """
		@page "/counter"
		@using Moka.Red.Core
		@inject IJSRuntime JS
		@inherits LayoutComponentBase
		@* a comment *@
		<PageTitle>Counter</PageTitle>
		<h1 class="title @extra">Count: @currentCount</h1>
		<button @onclick="IncrementCount" disabled="@(currentCount > 3)">Click &amp; go</button>
		<MokaButton OnClick="Save" Icon="@MokaIcons.Action.Save" />
		@if (currentCount > 0)
		{
			<p>Positive</p>
		}
		else
		{
			<text>Zero</text>
		}
		@foreach (var item in Items) { <li @key="item.Id">@item.Name</li> }
		<a href="mailto:me@example.com">mail</a>
		@code {
			private int currentCount;
			private RenderFragment Row => @<tr><td>@currentCount</td></tr>;
			private void IncrementCount() => currentCount++;
		}
		<footer>after</footer>
		""";

	private static readonly IReadOnlyList<MokaCodeSpan> Spans = CodeSpanAssert.Highlight(MokaCodeHighlighters.Razor, Code);

	[Theory]
	[InlineData("@page", MokaCodeTokenKind.Directive, 0)]
	[InlineData("\"/counter\"", MokaCodeTokenKind.String, 0)]
	[InlineData("@using", MokaCodeTokenKind.Directive, 0)]
	[InlineData("Moka", MokaCodeTokenKind.Plain, 0)]
	[InlineData("@inject", MokaCodeTokenKind.Directive, 0)]
	[InlineData("IJSRuntime", MokaCodeTokenKind.Type, 0)]
	[InlineData("LayoutComponentBase", MokaCodeTokenKind.Type, 0)]
	[InlineData("@* a comment *@", MokaCodeTokenKind.Comment, 0)]
	[InlineData("PageTitle", MokaCodeTokenKind.Tag, 0)]
	[InlineData("Counter", MokaCodeTokenKind.Plain, 0)]
	[InlineData("h1", MokaCodeTokenKind.Tag, 0)]
	[InlineData("class", MokaCodeTokenKind.Attribute, 0)]
	[InlineData("\"title ", MokaCodeTokenKind.String, 0)]
	[InlineData("extra", MokaCodeTokenKind.Plain, 0)]
	[InlineData("Count: ", MokaCodeTokenKind.Plain, 0)]
	[InlineData("@onclick", MokaCodeTokenKind.Directive, 0)]
	[InlineData("IncrementCount", MokaCodeTokenKind.Plain, 0)]
	[InlineData("disabled", MokaCodeTokenKind.Attribute, 0)]
	[InlineData("3", MokaCodeTokenKind.Number, 0)]
	[InlineData("&amp;", MokaCodeTokenKind.Constant, 0)]
	[InlineData("MokaButton", MokaCodeTokenKind.Tag, 0)]
	[InlineData("OnClick", MokaCodeTokenKind.Attribute, 0)]
	[InlineData("\"Save\"", MokaCodeTokenKind.String, 0)]
	[InlineData("if", MokaCodeTokenKind.Keyword, 0)]
	[InlineData("Positive", MokaCodeTokenKind.Plain, 0)]
	[InlineData("else", MokaCodeTokenKind.Keyword, 0)]
	[InlineData("text", MokaCodeTokenKind.Directive, 0)]
	[InlineData("foreach", MokaCodeTokenKind.Keyword, 0)]
	[InlineData("var", MokaCodeTokenKind.Keyword, 0)]
	[InlineData("@key", MokaCodeTokenKind.Directive, 0)]
	[InlineData("\"mailto:me@example.com\"", MokaCodeTokenKind.String, 0)]
	[InlineData("@code", MokaCodeTokenKind.Directive, 0)]
	[InlineData("private", MokaCodeTokenKind.Keyword, 0)]
	[InlineData("RenderFragment", MokaCodeTokenKind.Type, 0)]
	[InlineData("td", MokaCodeTokenKind.Tag, 0)]
	[InlineData("void", MokaCodeTokenKind.Keyword, 0)]
	[InlineData("footer", MokaCodeTokenKind.Tag, 0)]
	[InlineData("after", MokaCodeTokenKind.Plain, 0)]
	public void EachPart_HasItsKind(string fragment, MokaCodeTokenKind kind, int occurrence) =>
		Assert.Equal(kind, CodeSpanAssert.KindOf(Code, Spans, fragment, occurrence));

	// The @ that moves into C# is a transition, colored as a directive.
	[Fact]
	public void TheTransitions_AreDirectives()
	{
		int transition = Code.IndexOf("@currentCount<", StringComparison.Ordinal);

		Assert.Contains(Spans, s => s.Start == transition && s.Length == 1 && s.Kind == MokaCodeTokenKind.Directive);
		Assert.Contains(Spans, s => s.Start == Code.IndexOf("@if", StringComparison.Ordinal) && s.Kind == MokaCodeTokenKind.Directive);
	}

	// "in" inside the foreach header is a keyword; the same letters in the markup after it are text.
	[Fact]
	public void ForeachHeader_IsCSharp()
	{
		int header = Code.IndexOf("item in Items", StringComparison.Ordinal);

		Assert.Equal(MokaCodeTokenKind.Keyword, CodeSpanAssert.KindOf(Code[header..], [.. Spans.Select(s => s with { Start = s.Start - header })], "in"));
	}

	[Fact]
	public void AComparisonInACodeBlock_IsNotMarkup()
	{
		const string code = "@{ var ok = count < limit; }<p>done</p>";
		IReadOnlyList<MokaCodeSpan> spans = CodeSpanAssert.Highlight(MokaCodeHighlighters.Razor, code);

		Assert.Equal(MokaCodeTokenKind.Punctuation, CodeSpanAssert.KindOf(code, spans, "<"));
		Assert.Equal(MokaCodeTokenKind.Plain, CodeSpanAssert.KindOf(code, spans, "limit"));
		Assert.Equal(MokaCodeTokenKind.Tag, CodeSpanAssert.KindOf(code, spans, "p"));
	}

	[Fact]
	public void AStyleBlock_IsCss()
	{
		const string code = "<style>.card { color: red; }</style><p>x</p>";
		IReadOnlyList<MokaCodeSpan> spans = CodeSpanAssert.Highlight(MokaCodeHighlighters.Razor, code);

		Assert.Equal(MokaCodeTokenKind.Type, CodeSpanAssert.KindOf(code, spans, ".card"));
		Assert.Equal(MokaCodeTokenKind.Key, CodeSpanAssert.KindOf(code, spans, "color"));
		Assert.Equal(MokaCodeTokenKind.Tag, CodeSpanAssert.KindOf(code, spans, "p"));
	}
}
