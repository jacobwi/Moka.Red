using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml.Linq;
using Moka.Red.Core.Utilities;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Utilities;

// The checks MokaBarcode, MokaQRCode and MokaIdenticon (SvgValues), MokaWatermark (WatermarkTile) and
// MokaResponsiveStyleBuilder each kept their own copy of, in one public place.
public class CssValuesTests
{
	[Theory]
	[InlineData("#fff")]
	[InlineData("#FFFA")]
	[InlineData("#1a1a22")]
	[InlineData("#1a1a2280")]
	[InlineData("red")]
	[InlineData("currentColor")]
	[InlineData("transparent")]
	[InlineData(" rgb(26 26 34) ")]
	[InlineData("rgba(0, 0, 0, 0.5)")]
	[InlineData("hsl(120deg 50% 50% / 20%)")]
	[InlineData("oklch(62.8% 0.25 29.2)")]
	[InlineData("var(--moka-color-primary)")]
	[InlineData("var(--moka-color-primary, rgb(239 83 80))")]
	[InlineData("color-mix(in srgb, red 40%, white)")]
	public void IsColor_AcceptsCssColors(string color) => Assert.True(CssValues.IsColor(color));

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("#ff")]
	[InlineData("#gggggg")]
	[InlineData("red' onload='x")]
	[InlineData("red;fill:blue")]
	[InlineData("url(#gradient)")]
	[InlineData("rgb(0 0 0) red")]
	[InlineData("rgb(0 0 0")]
	[InlineData("rgb(0 0 0))")]
	[InlineData("rgb (0 0 0)")]
	[InlineData("expression(alert(1))")]
	[InlineData("var(--x, url(x))")]
	[InlineData("rgb(0 0 0 \"x\")")]
	[InlineData("red&blue")]
	public void IsColor_RejectsEverythingElse(string? color) => Assert.False(CssValues.IsColor(color));

	[Theory]
	[InlineData("12px")]
	[InlineData("0.75rem")]
	[InlineData(".8em")]
	[InlineData("10")]
	[InlineData("80%")]
	[InlineData(" 14PX ")]
	public void IsLength_AcceptsCssLengths(string length) => Assert.True(CssValues.IsLength(length));

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("-12px")]
	[InlineData("12 px")]
	[InlineData("12px'")]
	[InlineData("calc(1px + 2px)")]
	[InlineData("12furlongs")]
	public void IsLength_RejectsEverythingElse(string? length) => Assert.False(CssValues.IsLength(length));

	[Theory]
	[InlineData("4 4")]
	[InlineData("4")]
	[InlineData("6, 3")]
	[InlineData("6,3,1")]
	[InlineData(" 2px 1px ")]
	[InlineData("5% 2.5%")]
	[InlineData("1\t2")]
	public void IsLengthList_AcceptsDashArrays(string list) => Assert.True(CssValues.IsLengthList(list));

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("4,,4")]
	[InlineData("4 4,")]
	[InlineData(",4 4")]
	[InlineData("-4 4")]
	[InlineData("4 4' onload='x")]
	[InlineData("4;4")]
	[InlineData("none")]
	[InlineData("4\n4")]
	public void IsLengthList_RejectsEverythingElse(string? list) => Assert.False(CssValues.IsLengthList(list));

	[Theory]
	[InlineData("")]
	[InlineData("1fr 2fr")]
	[InlineData("repeat(auto-fill, minmax(12rem, 1fr))")]
	[InlineData("url(\"data:image/png,abc\")")]
	[InlineData("'Inter', sans-serif")]
	public void IsSafe_AcceptsValuesThatStayInTheirDeclaration(string value) => Assert.True(CssValues.IsSafe(value));

	[Theory]
	[InlineData(null)]
	[InlineData("1px; color: red")]
	[InlineData("1px} body{display:none")]
	[InlineData("1px{")]
	[InlineData("1px</style><script>")]
	[InlineData("a > b")]
	[InlineData("red\\")]
	public void IsSafe_RejectsValuesThatCouldEndTheirDeclaration(string? value) =>
		Assert.False(CssValues.IsSafe(value));

	[Theory]
	[InlineData("")]
	[InlineData("red")]
	[InlineData("12px")]
	[InlineData("var(--moka-color-primary)")]
	[InlineData("var(--x, rgb(0 0 0 / 50%))")]
	[InlineData("calc(100% - 2rem)")]
	[InlineData("repeat(auto-fill, minmax(12rem, 1fr))")]
	[InlineData("[full-start] 1fr [full-end]")]
	[InlineData("'Inter', \"Segoe UI\", sans-serif")]
	[InlineData("url(\"data:image/png;base64,AAAA\")")]
	[InlineData("url('a;b')")]
	[InlineData("url(data:image/png;base64,AAAA)")]
	[InlineData("url(a{b})")]
	[InlineData("url( a.png )")]
	[InlineData("url(a\\)b.png)")]
	[InlineData("URL(x;y)")]
	[InlineData("u\\72l(a;b)")]
	[InlineData("url(\"a\" ; x)")]
	[InlineData("\"{\" \"}\" \";\"")]
	[InlineData("\"a\\\"b\"")]
	[InlineData("\"line\\\ncontinued\"")]
	[InlineData("a\\;b")]
	[InlineData("a\\\\")]
	[InlineData("1px /* a comment; with } in it */ solid")]
	[InlineData("16/9")]
	[InlineData("linear-gradient(to right, red 1px, transparent 1px)")]
	[InlineData("a) b")]
	[InlineData("(a] b)")]
	[InlineData("\\3b url(a(b)c;x)")]
	public void IsSelfContained_AcceptsValuesThatStayInTheirDeclaration(string value) =>
		Assert.True(CssValues.IsSelfContained(value));

	[Theory]
	[InlineData(null)]
	[InlineData("red; color: blue")]
	[InlineData("red;")]
	[InlineData("1px} body{display:none")]
	[InlineData("{")]
	[InlineData("a}")]
	[InlineData("calc({)")]
	[InlineData("\"unclosed")]
	[InlineData("'unclosed")]
	[InlineData("\"a\n; color: red\"")]
	[InlineData("\"a\r; color: red\"")]
	[InlineData("rgb(0 0 0")]
	[InlineData("var(--x")]
	[InlineData("[a")]
	[InlineData("([)]")]
	[InlineData("url(abc")]
	[InlineData("url(a;b")]
	[InlineData("red /* never closed")]
	[InlineData("red\\")]
	[InlineData("red\\\\\\")]
	[InlineData("\"a\\\"")]
	// A url( token ends at its first ')' whatever comes before it, so the ';' after it is outside.
	[InlineData("url(a(b)c;color:red;x)")]
	[InlineData("u\\72l(a(b)c;color:red;x)")]
	[InlineData("\\75 rl(a(b)c;color:red;x)")]
	// A unit or a hash owns the name before the '(', which then opens a plain block, and the quote
	// inside it starts a string that never closes.
	[InlineData("1url(a\"b)")]
	[InlineData("#url(a\"b)")]
	public void IsSelfContained_RejectsValuesThatCouldEndTheirDeclaration(string? value) =>
		Assert.False(CssValues.IsSelfContained(value));

	[Fact]
	public void IsSelfContained_RefusesNestingDeeperThanRealCss()
	{
		string deep = new string('[', 65) + ";" + new string(']', 65);
		string fine = new string('[', 64) + ";" + new string(']', 64);

		Assert.False(CssValues.IsSelfContained(deep));
		Assert.True(CssValues.IsSelfContained(fine));
	}

	// Url() quotes and escapes everything a URL can hold, so its output always stays in one declaration.
	[Fact]
	[SuppressMessage("Security", "CA5394:Do not use insecure randomness",
		Justification = "A fixed seed makes the inputs repeatable. Nothing here needs to be unpredictable.")]
	public void WhatUrlWrites_IsSelfContained()
	{
		const string alphabet = "a/.;:?#&=%()[]{}\"'\\ \t\n\r\f*<>,";
		var random = new Random(19092026);
		for (int n = 0; n < 20_000; n++)
		{
			var sb = new StringBuilder();
			int length = random.Next(0, 16);
			for (int j = 0; j < length; j++)
			{
				sb.Append(alphabet[random.Next(alphabet.Length)]);
			}

			Assert.True(CssValues.IsSelfContained(CssValues.Url(sb.ToString())), CssValues.Url(sb.ToString()));
		}
	}

	[Fact]
	public void EscapeXml_EscapesMarkupCharacters() =>
		Assert.Equal("a&apos;b&quot;c&lt;d&gt;e&amp;f", CssValues.EscapeXml("a'b\"c<d>e&f"));

	[Fact]
	public void EscapeXml_DropsCharactersXmlDoesNotAllow() =>
		Assert.Equal("ab\U0001F600c", CssValues.EscapeXml("a" + (char)1 + "b\U0001F600\uD800c"));

	[Fact]
	public void ColorOrDefault_FallsBackForAnythingElse() =>
		Assert.Equal("#000000", CssValues.ColorOrDefault("x' onload='y", "#000000"));

	[Fact]
	public void ColorOrDefault_TrimsAColor() =>
		Assert.Equal("rgb(26 26 34)", CssValues.ColorOrDefault("  rgb(26 26 34)\n", "#000000"));

	[Theory]
	[InlineData(" 0.8rem ", "0.8rem")]
	[InlineData("12px\" onload=\"x", "12px")]
	[InlineData(null, "12px")]
	public void LengthOrDefault_KeepsALengthAndReplacesAnythingElse(string? value, string expected) =>
		Assert.Equal(expected, CssValues.LengthOrDefault(value, "12px"));

	[Theory]
	[InlineData("48px", 48)]
	[InlineData("48", 48)]
	[InlineData(" 14PX ", 14)]
	[InlineData("12pt", 16)]
	[InlineData("1pc", 16)]
	[InlineData("1in", 96)]
	[InlineData("2.54cm", 96)]
	[InlineData("25.4mm", 96)]
	[InlineData("101.6q", 96)]
	[InlineData("3em", 48)]
	[InlineData("2rem", 32)]
	[InlineData(".5px", 0.5)]
	[InlineData("0", 0)]
	public void TryParsePixels_ConvertsAbsoluteLengths(string value, double expected)
	{
		Assert.True(CssValues.TryParsePixels(value, out double pixels));
		Assert.Equal(expected, pixels, 6);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("-1px")]
	[InlineData("50%")]
	[InlineData("10vw")]
	[InlineData("2ex")]
	[InlineData("12 px")]
	[InlineData("1e5")]
	[InlineData("calc(1px + 1px)")]
	[InlineData("48px' onload='x")]
	public void TryParsePixels_RefusesEverythingElse(string? value)
	{
		Assert.False(CssValues.TryParsePixels(value, out double pixels));
		Assert.Equal(0, pixels);
	}

	[Fact]
	public void TryParsePixels_RefusesANumberTooBigForADouble() =>
		Assert.False(CssValues.TryParsePixels(new string('9', 400) + "px", out _));

	[Theory]
	[InlineData("hero.png", "url(\"hero.png\")")]
	[InlineData("https://example.com/a b.png?x=1&y=2", "url(\"https://example.com/a b.png?x=1&y=2\")")]
	[InlineData("x.png'); color: red", "url(\"x.png'); color: red\")")]
	[InlineData("x.png\"); color: red", "url(\"x.png\\\"); color: red\")")]
	[InlineData("x\\y.png", "url(\"x\\\\y.png\")")]
	[InlineData("x.png\n); color: red", "url(\"x.png\\a ); color: red\")")]
	public void Url_QuotesAndEscapes(string address, string expected) =>
		Assert.Equal(expected, CssValues.Url(address));

	// Whatever the URL holds, the declaration it goes into stays one declaration.
	[Theory]
	[InlineData("x.png'); background-color: red; content: url('y")]
	[InlineData("x.png\"); background-color: red; content: url(\"y")]
	[InlineData("x.png\\\"); background-color: red")]
	[InlineData("x.png\n); background-color: red")]
	[InlineData("x.png\r\f); background-color: red")]
	public void Url_CannotAddDeclarations(string address) =>
		Assert.Equal(["background-image"], CssDeclarations.PropertyNames($"background-image: {CssValues.Url(address)}"));

	[Fact]
	public void SvgDataUrl_PercentEncodesTheWholeDocument()
	{
		const string svg = "<svg xmlns='http://www.w3.org/2000/svg'><text>a \"b\" (c); #d</text></svg>";

		string url = CssValues.SvgDataUrl(svg);

		const string prefix = "url(\"data:image/svg+xml,";
		Assert.StartsWith(prefix, url, StringComparison.Ordinal);
		Assert.EndsWith("\")", url, StringComparison.Ordinal);
		string payload = url[prefix.Length..^2];
		Assert.DoesNotContain(payload, c => c is '"' or '\'' or '(' or ')' or ';' or '#' or ' ' or '<' or '\\');
		Assert.Equal(svg, Uri.UnescapeDataString(payload));
	}

	[Fact]
	public void TheEncoders_RefuseNull()
	{
		Assert.Throws<ArgumentNullException>(() => CssValues.Url(null!));
		Assert.Throws<ArgumentNullException>(() => CssValues.SvgDataUrl(null!));
		Assert.Throws<ArgumentNullException>(() => CssValues.EscapeXml(null!));
		Assert.Throws<ArgumentNullException>(() => CssValues.ColorOrDefault("red", null!));
		Assert.Throws<ArgumentNullException>(() => CssValues.LengthOrDefault("1px", null!));
	}

	// The class documents that an accepted color or length needs no escaping for an XML attribute or a
	// declaration. Random strings over an alphabet full of markup characters check that it holds.
	[Fact]
	[SuppressMessage("Security", "CA5394:Do not use insecure randomness",
		Justification = "A fixed seed makes the inputs repeatable. Nothing here needs to be unpredictable.")]
	public void WhatTheChecksAccept_HoldsNoMarkupOrDeclarationCharacters()
	{
		const string alphabet = "#abcdefrgbvarlhs0123456789.,%/+-_ ()'\"<>&;{}\\\n";
		var random = new Random(20260919);
		int accepted = 0;
		for (int i = 0; i < 200_000; i++)
		{
			var sb = new StringBuilder();
			int length = random.Next(1, 14);
			for (int j = 0; j < length; j++)
			{
				sb.Append(alphabet[random.Next(alphabet.Length)]);
			}

			string value = sb.ToString();
			if (!CssValues.IsColor(value) && !CssValues.IsLength(value) && !CssValues.IsLengthList(value))
			{
				continue;
			}

			accepted++;
			Assert.DoesNotContain(value.Trim(), c => c is '\'' or '"' or '<' or '>' or '&' or ';' or '{' or '}' or '\\');
			XElement element = XElement.Parse($"<x a='{value.Trim()}'/>");
			Assert.Equal(value.Trim(), element.Attribute("a")!.Value);
		}

		Assert.True(accepted > 100, $"Only {accepted} random values were accepted, too few to say anything.");
	}
}
