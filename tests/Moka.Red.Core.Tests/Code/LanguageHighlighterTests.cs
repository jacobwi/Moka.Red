using Moka.Red.Core.Code;

namespace Moka.Red.Core.Tests.Code;

public sealed class LanguageHighlighterTests
{
	private const string Html = """
		<!DOCTYPE html>
		<!-- a note -->
		<div id="main" hidden data-x='1'>Fish &amp; chips<br>
		<style>.card > a:hover { color: #fff; }</style>
		<script>if (a < b) { }</script>
		</div>
		""";

	private const string Xml = """
		<?xml version="1.0"?>
		<root xmlns:x="urn:x"><x:item a='1'><![CDATA[<raw>]]></x:item><!-- c --></root>
		""";

	private const string Css = """
		/* tokens */
		@import "base.css";
		@media (min-width: 720px) {
			.card > a:hover, #main {
				color: var(--moka-x, #fff);
				margin: 0 auto 1.5rem;
			}
		}
		.b::deep [data-on="1"] { --gap: 10px; background: url(data:image/png;base64,AA) !important }
		""";

	private const string Json = """
		{
			"name": "Moka",
			"count": 3,
			"ok": true,
			"none": null,
			"list": [1.5, -2e3] // a note
		}
		""";

	private const string Shell = """
		# install
		dotnet add package Moka.Red --version 0.6.0
		export PATH="$HOME/bin:$PATH"
		if [ -f x ]; then echo 'yes' | grep y; fi
		Get-ChildItem -Path $env:TEMP | Where-Object { $_.Length -gt 1kb }
		[string]$name = "World"
		cat <<EOF
		body $x
		EOF
		ls
		""";

	[Theory]
	[InlineData("<!DOCTYPE html>", MokaCodeTokenKind.Directive)]
	[InlineData("<!-- a note -->", MokaCodeTokenKind.Comment)]
	[InlineData("div", MokaCodeTokenKind.Tag)]
	[InlineData("id", MokaCodeTokenKind.Attribute)]
	[InlineData("\"main\"", MokaCodeTokenKind.String)]
	[InlineData("hidden", MokaCodeTokenKind.Attribute)]
	[InlineData("'1'", MokaCodeTokenKind.String)]
	[InlineData("Fish ", MokaCodeTokenKind.Plain)]
	[InlineData("&amp;", MokaCodeTokenKind.Constant)]
	[InlineData("br", MokaCodeTokenKind.Tag)]
	[InlineData(".card", MokaCodeTokenKind.Type)]
	[InlineData(":hover", MokaCodeTokenKind.Keyword)]
	[InlineData("#fff", MokaCodeTokenKind.Constant)]
	[InlineData("if (a < b) { }", MokaCodeTokenKind.Plain)]
	public void Html_EachPart_HasItsKind(string fragment, MokaCodeTokenKind kind) =>
		Assert.Equal(kind, KindOf(MokaCodeHighlighters.Html, Html, fragment));

	[Theory]
	[InlineData("<?xml version=\"1.0\"?>", MokaCodeTokenKind.Directive)]
	[InlineData("root", MokaCodeTokenKind.Tag)]
	[InlineData("xmlns:x", MokaCodeTokenKind.Attribute)]
	[InlineData("x:item", MokaCodeTokenKind.Tag)]
	[InlineData("<![CDATA[<raw>]]>", MokaCodeTokenKind.String)]
	[InlineData("<!-- c -->", MokaCodeTokenKind.Comment)]
	public void Xml_EachPart_HasItsKind(string fragment, MokaCodeTokenKind kind) =>
		Assert.Equal(kind, KindOf(MokaCodeHighlighters.Xml, Xml, fragment));

	[Theory]
	[InlineData("/* tokens */", MokaCodeTokenKind.Comment)]
	[InlineData("@import", MokaCodeTokenKind.Directive)]
	[InlineData("\"base.css\"", MokaCodeTokenKind.String)]
	[InlineData("@media", MokaCodeTokenKind.Directive)]
	[InlineData("min-width", MokaCodeTokenKind.Key)]
	[InlineData("720px", MokaCodeTokenKind.Number)]
	[InlineData(".card", MokaCodeTokenKind.Type)]
	[InlineData(":hover", MokaCodeTokenKind.Keyword)]
	[InlineData("#main", MokaCodeTokenKind.Type)]
	[InlineData("color", MokaCodeTokenKind.Key)]
	[InlineData("--moka-x", MokaCodeTokenKind.Key)]
	[InlineData("#fff", MokaCodeTokenKind.Constant)]
	[InlineData("1.5rem", MokaCodeTokenKind.Number)]
	[InlineData("auto", MokaCodeTokenKind.Plain)]
	[InlineData("::deep", MokaCodeTokenKind.Keyword)]
	[InlineData("data-on", MokaCodeTokenKind.Attribute)]
	[InlineData("--gap", MokaCodeTokenKind.Key)]
	[InlineData("10px", MokaCodeTokenKind.Number)]
	[InlineData("data:image/png;base64,AA", MokaCodeTokenKind.String)]
	[InlineData("!important", MokaCodeTokenKind.Keyword)]
	public void Css_EachPart_HasItsKind(string fragment, MokaCodeTokenKind kind) =>
		Assert.Equal(kind, KindOf(MokaCodeHighlighters.Css, Css, fragment));

	[Theory]
	[InlineData("\"name\"", MokaCodeTokenKind.Key)]
	[InlineData("\"Moka\"", MokaCodeTokenKind.String)]
	[InlineData("3", MokaCodeTokenKind.Number)]
	[InlineData("true", MokaCodeTokenKind.Keyword)]
	[InlineData("null", MokaCodeTokenKind.Keyword)]
	[InlineData("-2e3", MokaCodeTokenKind.Number)]
	[InlineData("// a note", MokaCodeTokenKind.Comment)]
	[InlineData("{", MokaCodeTokenKind.Punctuation)]
	public void Json_EachPart_HasItsKind(string fragment, MokaCodeTokenKind kind) =>
		Assert.Equal(kind, KindOf(MokaCodeHighlighters.Json, Json, fragment));

	[Theory]
	[InlineData("# install", MokaCodeTokenKind.Comment)]
	[InlineData("dotnet", MokaCodeTokenKind.Type)]
	[InlineData("--version", MokaCodeTokenKind.Attribute)]
	[InlineData("0.6.0", MokaCodeTokenKind.Number)]
	[InlineData("export", MokaCodeTokenKind.Keyword)]
	[InlineData("PATH", MokaCodeTokenKind.Key)]
	[InlineData("$HOME", MokaCodeTokenKind.Key)]
	[InlineData("/bin:", MokaCodeTokenKind.String)]
	[InlineData("then", MokaCodeTokenKind.Keyword)]
	[InlineData("-f", MokaCodeTokenKind.Attribute)]
	[InlineData("echo", MokaCodeTokenKind.Type)]
	[InlineData("'yes'", MokaCodeTokenKind.String)]
	[InlineData("grep", MokaCodeTokenKind.Type)]
	[InlineData("fi", MokaCodeTokenKind.Keyword)]
	[InlineData("Get-ChildItem", MokaCodeTokenKind.Type)]
	[InlineData("-Path", MokaCodeTokenKind.Attribute)]
	[InlineData("$env:TEMP", MokaCodeTokenKind.Key)]
	[InlineData("Where-Object", MokaCodeTokenKind.Type)]
	[InlineData("$_", MokaCodeTokenKind.Key)]
	[InlineData("-gt", MokaCodeTokenKind.Attribute)]
	[InlineData("[string]", MokaCodeTokenKind.Type)]
	[InlineData("\"World\"", MokaCodeTokenKind.String)]
	[InlineData("cat", MokaCodeTokenKind.Type)]
	[InlineData("body $x", MokaCodeTokenKind.String)]
	[InlineData("ls", MokaCodeTokenKind.Type)]
	public void Shell_EachPart_HasItsKind(string fragment, MokaCodeTokenKind kind) =>
		Assert.Equal(kind, KindOf(MokaCodeHighlighters.Shell, Shell, fragment));

	private static MokaCodeTokenKind KindOf(Func<string, IEnumerable<MokaCodeSpan>> highlighter, string code, string fragment) =>
		CodeSpanAssert.KindOf(code, CodeSpanAssert.Highlight(highlighter, code), fragment);
}
