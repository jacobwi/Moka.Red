using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// The looks picked from MokaArk's design audit reach these components through tokens, which hold today's values unless
// an app picks otherwise, so each rule here reads the token where it used to name the value.
public class PrincipleLookRulesTests
{
	[Fact]
	public void PrimaryTextButtons_ReadTheTextButtonTokens()
	{
		const string Css = "MokaButton.razor.css";

		Assert.Equal("var(--moka-button-text-color)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-btn--text.moka-btn--primary")["color"]);
		Assert.Equal("var(--moka-button-text-hover-color)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-btn--text.moka-btn--primary:hover:not(.moka-btn--disabled)")["color"]);
		Assert.Equal("var(--moka-color-primary-light)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-btn--text.moka-btn--primary:focus-visible")["color"]);
	}

	// Only the primary color moves: a text button given another color keeps it.
	[Theory]
	[InlineData(".moka-gate .moka-btn--text.moka-btn--secondary", "var(--moka-color-on-surface-variant)")]
	[InlineData(".moka-gate .moka-btn--text.moka-btn--error", "var(--moka-color-error-soft)")]
	[InlineData(".moka-gate .moka-btn--text.moka-btn--info", "var(--moka-color-info-ink)")]
	public void OtherTextButtonColors_KeepTheirs(string selector, string color) =>
		Assert.Equal(color, ScopedCss.Declarations("MokaButton.razor.css", selector)["color"]);

	[Fact]
	public void PrimaryTextSplitButtons_ReadTheTextButtonColor()
	{
		const string Css = "MokaSplitButton.razor.css";

		Assert.Equal("var(--moka-button-text-color)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-split-btn--text.moka-split-btn--primary")["color"]);
		Assert.Equal("var(--moka-color-primary-light)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-split-btn--text.moka-split-btn--primary .moka-split-btn__primary:hover:not(:disabled)")["color"]);
		Assert.Equal("var(--moka-color-primary-light)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-split-btn--text.moka-split-btn--primary .moka-split-btn__toggle:focus-visible")["color"]);
	}

	[Fact]
	public void BigNumbers_ReadTheNumberFont()
	{
		IReadOnlyDictionary<string, string> stat = ScopedCss.Declarations("MokaStat.razor.css", ".moka-gate .moka-stat__value");

		Assert.Equal("var(--moka-font-family-number)", stat["font-family"]);
		Assert.Equal("var(--moka-number-tracking)", stat["letter-spacing"]);
		Assert.Equal("tabular-nums", stat["font-variant-numeric"]);
		Assert.Equal("var(--moka-font-family-figure)",
			ScopedCss.Declarations("MokaStat.razor.css", ".moka-stat.moka-stat--tile .moka-stat__value")["font-family"]);
		Assert.Equal("var(--moka-font-family-mono)",
			ScopedCss.Declarations("MokaStat.razor.css", ".moka-stat.moka-stat--mono .moka-stat__value")["font-family"]);

		foreach (string gauge in new[]
		         {
			         ".moka-gauge.moka-gauge--ring > .moka-gauge-center > .moka-gauge-value",
			         ".moka-gauge.moka-gauge--dial > .moka-gauge-center > .moka-gauge-value"
		         })
		{
			IReadOnlyDictionary<string, string> value = ScopedCss.Declarations("MokaGauge.razor.css", gauge);
			Assert.Equal("var(--moka-font-family-number)", value["font-family"]);
			Assert.Equal("var(--moka-number-tracking)", value["letter-spacing"]);
		}
	}

	// At 100% the mixes give the type's own tint, line and text exactly, which is today's callout.
	[Fact]
	public void Callouts_MixTheirTypeByTheCalloutTokens()
	{
		IReadOnlyDictionary<string, string> callout = ScopedCss.Declarations("MokaCallout.razor.css", ".moka-gate .moka-callout");

		Assert.Equal("color-mix(in srgb, var(--_callout-bg) var(--moka-callout-fill), color-mix(in srgb, var(--_callout-tone) 6%, transparent))",
			callout["background-color"]);
		Assert.Equal("color-mix(in srgb, var(--_callout-line) var(--moka-callout-fill), transparent)", callout["border-color"]);
		Assert.Equal("color-mix(in srgb, var(--_callout-line) var(--moka-callout-fill), var(--_callout-tone))", callout["border-left-color"]);
		Assert.Equal("color-mix(in srgb, var(--_callout-text) var(--moka-callout-fill), var(--moka-color-on-surface))", callout["color"]);
		Assert.Equal("max(var(--moka-border-width), var(--moka-callout-edge))", callout["border-left-width"]);
		Assert.Equal("var(--moka-callout-edge-radius)", callout["border-top-left-radius"]);
	}

	[Theory]
	[InlineData("note", "info")]
	[InlineData("tip", "success")]
	[InlineData("warning", "warning")]
	[InlineData("caution", "error")]
	public void EachCalloutType_NamesItsMeaningsShades(string type, string meaning)
	{
		IReadOnlyDictionary<string, string> shades = ScopedCss.Declarations("MokaCallout.razor.css", $".moka-gate .moka-callout--{type}");

		Assert.Equal($"var(--moka-color-{meaning})", shades["--_callout-tone"]);
		Assert.Equal($"var(--moka-color-{meaning}-bg)", shades["--_callout-bg"]);
		Assert.Equal($"var(--moka-color-{meaning}-line)", shades["--_callout-line"]);
		Assert.Equal($"var(--moka-color-{meaning}-text)", shades["--_callout-text"]);
	}

	[Fact]
	public void TheImportantCallout_IsTheAccent()
	{
		IReadOnlyDictionary<string, string> shades = ScopedCss.Declarations("MokaCallout.razor.css", ".moka-gate .moka-callout--important");

		Assert.Equal("var(--moka-color-primary)", shades["--_callout-tone"]);
		Assert.Equal("var(--moka-color-primary-bg)", shades["--_callout-bg"]);
		Assert.Equal("var(--moka-color-primary-ring)", shades["--_callout-line"]);
		Assert.Equal("var(--moka-color-primary-text)", shades["--_callout-text"]);
	}
}
