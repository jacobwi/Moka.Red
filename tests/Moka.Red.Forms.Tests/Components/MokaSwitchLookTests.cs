using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// MokaTheme.WithSwitches draws a switch that is on through four tokens. The track carries the switch's tone as its color,
// so a token can mix it in through currentColor.
public class MokaSwitchLookTests
{
	private const string Css = "MokaSwitch.razor.css";
	private const string OnTrack = ".moka-gate .moka-switch-input:checked + .moka-switch-track";
	private const string OnThumb = ".moka-gate .moka-switch-input:checked + .moka-switch-track .moka-switch-thumb";

	[Fact]
	public void TheOnTrack_CarriesTheTone_AndReadsTheTokens()
	{
		IReadOnlyDictionary<string, string> track = ScopedCss.Declarations(Css, OnTrack);

		Assert.Equal("var(--_switch-tone, var(--moka-color-primary))", track["color"]);
		Assert.Equal("var(--moka-switch-on-track)", track["background"]);
		Assert.Equal("var(--moka-switch-on-ring)", track["box-shadow"]);
	}

	[Fact]
	public void TheOnThumb_ReadsTheTokens()
	{
		IReadOnlyDictionary<string, string> thumb = ScopedCss.Declarations(Css, OnThumb);

		Assert.Equal("var(--moka-switch-on-knob)", thumb["background"]);
		Assert.Equal("var(--moka-switch-on-knob-shadow)", thumb["box-shadow"]);
	}

	// At today's values the rules draw what the older ones did: a track in the tone, the knob token and its shadow.
	[Fact]
	public void TheOffSwitch_AndTheOriginalDesign_KeepTheirRules()
	{
		Assert.Equal("var(--_switch-tone, var(--moka-color-primary))",
			ScopedCss.Declarations(Css, ".moka-switch-input:checked + .moka-switch-track")["background"]);
		Assert.Equal("var(--moka-color-on-surface-quaternary)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-switch-input:not(:checked) + .moka-switch-track")["background"]);
		Assert.Equal("var(--moka-color-knob)", ScopedCss.Declarations(Css, ".moka-gate .moka-switch-thumb")["background"]);
	}
}
