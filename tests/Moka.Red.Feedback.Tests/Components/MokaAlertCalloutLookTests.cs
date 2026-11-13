using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

// MokaTheme.WithCallouts reaches filled alerts through the callout tokens: at 100% they are today's tint, line and text,
// at 0% an edge in the severity's color over a faint tint of it. Outlined alerts keep their outline.
public class MokaAlertCalloutLookTests
{
	private const string Css = "MokaAlert.razor.css";

	[Fact]
	public void AFilledAlert_MixesItsSeverityByTheCalloutTokens()
	{
		IReadOnlyDictionary<string, string> alert = ScopedCss.Declarations(Css, ".moka-gate .moka-alert:not(.moka-alert--outlined)");

		Assert.Equal("color-mix(in srgb, var(--_alert-bg) var(--moka-callout-fill), color-mix(in srgb, var(--_alert-tone) 6%, transparent))",
			alert["background"]);
		Assert.Equal("color-mix(in srgb, var(--_alert-line) var(--moka-callout-fill), transparent)", alert["border-color"]);
		Assert.Equal("color-mix(in srgb, var(--_alert-line) var(--moka-callout-fill), var(--_alert-tone))", alert["border-left-color"]);
		Assert.Equal("color-mix(in srgb, var(--_alert-text) var(--moka-callout-fill), var(--moka-color-on-surface))", alert["color"]);
		Assert.Equal("max(var(--moka-border-width), var(--moka-callout-edge))", alert["border-left-width"]);
		Assert.Equal("var(--moka-callout-edge-radius)", alert["border-bottom-left-radius"]);
	}

	[Theory]
	[InlineData("info")]
	[InlineData("success")]
	[InlineData("warning")]
	[InlineData("error")]
	public void EachSeverity_NamesItsShades(string severity)
	{
		IReadOnlyDictionary<string, string> shades = ScopedCss.Declarations(Css, $".moka-gate .moka-alert--{severity}");

		Assert.Equal($"var(--moka-color-{severity})", shades["--_alert-tone"]);
		Assert.Equal($"var(--moka-color-{severity}-bg)", shades["--_alert-bg"]);
		Assert.Equal($"var(--moka-color-{severity}-line)", shades["--_alert-line"]);
		Assert.Equal($"var(--moka-color-{severity}-text)", shades["--_alert-text"]);
		Assert.Equal("transparent", ScopedCss.Declarations(Css, $".moka-gate .moka-alert--outlined.moka-alert--{severity}")["background"]);
	}

	[Fact]
	public void TheAccentAlert_IsTheAccentsTint()
	{
		IReadOnlyDictionary<string, string> shades = ScopedCss.Declarations(Css, ".moka-gate .moka-alert--accent");

		Assert.Equal("var(--moka-color-primary)", shades["--_alert-tone"]);
		Assert.Equal("var(--moka-color-primary-bg)", shades["--_alert-bg"]);
		Assert.Equal("var(--moka-color-primary-ring)", shades["--_alert-line"]);
		Assert.Equal("var(--moka-color-primary-text)", shades["--_alert-text"]);
	}
}
