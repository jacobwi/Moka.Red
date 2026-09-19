using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Tests.Utilities;

// Links took any href, and a javascript: URL runs script when the link is followed.
public class UrlValuesTests
{
	[Theory]
	[InlineData("javascript:alert(1)")]
	[InlineData("JavaScript:alert(1)")]
	[InlineData("JAVASCRIPT:alert(1)")]
	[InlineData("vbscript:msgbox(1)")]
	[InlineData("data:text/html,<script>alert(1)</script>")]
	[InlineData("DATA:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==")]
	// Browsers skip leading spaces and control characters, and ignore tabs and line breaks anywhere.
	[InlineData("  javascript:alert(1)")]
	[InlineData("javascript:alert(1)")]
	[InlineData("java\tscript:alert(1)")]
	[InlineData("java\nscript:alert(1)")]
	[InlineData("java\r\nscript:alert(1)")]
	[InlineData("\tj\na\rv\ta\ns\rc\tr\ni\rp\tt\n:alert(1)")]
	[InlineData("javascript\t:alert(1)")]
	public void HasBlockedScheme_CatchesScriptAndDataUrls(string href)
	{
		Assert.True(UrlValues.HasBlockedScheme(href));
		Assert.Null(UrlValues.SafeHref(href));
	}

	[Theory]
	[InlineData("")]
	[InlineData("/orders/10442")]
	[InlineData("orders/10442")]
	[InlineData("../settings")]
	[InlineData("#section-2")]
	[InlineData("?page=2")]
	[InlineData("//example.com/a")]
	[InlineData("https://example.com/a?b=c#d")]
	[InlineData("http://localhost:5000/")]
	[InlineData("mailto:team@example.com")]
	[InlineData("tel:+15555550100")]
	[InlineData("ftp://example.com/file")]
	[InlineData("custom-app+v2.x:open")]
	// Not a scheme a browser reads: a space, a non-ASCII letter or a character entity is inside it.
	[InlineData("java script:alert(1)")]
	[InlineData(" javascript:alert(1)")]
	[InlineData("&#106;avascript:alert(1)")]
	[InlineData("javascripts:alert(1)")]
	[InlineData("/javascript:alert(1)")]
	[InlineData("x-javascript:alert(1)")]
	public void HasBlockedScheme_LeavesEverythingElse(string href)
	{
		Assert.False(UrlValues.HasBlockedScheme(href));
		Assert.Same(href, UrlValues.SafeHref(href));
	}

	[Fact]
	public void NullHasNoScheme()
	{
		Assert.False(UrlValues.HasBlockedScheme(null));
		Assert.Null(UrlValues.SafeHref(null));
	}
}
