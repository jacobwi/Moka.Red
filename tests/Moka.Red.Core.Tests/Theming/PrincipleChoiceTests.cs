using System.Text.RegularExpressions;
using Moka.Red.Core.Theming;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Theming;

// The looks picked from a design audit: style choices on the new design that start at today's look, write their
// tokens, do nothing on the original design, and come together in the Ember preset.
public partial class PrincipleChoiceTests
{
	private const string MokaCss = "moka.css";

	public static TheoryData<string> NewDesignPresets => ["Red", "RedLight", "Gate", "GateLight", "Crisp", "CrispLight", "Matrix", "MatrixLight"];

	private static readonly string[] FloatingShadows =
		["--moka-shadow-3", "--moka-shadow-4", "--moka-shadow-popup", "--moka-shadow-popup-lg", "--moka-shadow-modal"];

	[Theory]
	[MemberData(nameof(NewDesignPresets))]
	public void EveryPresetButEmber_StartsAtTodaysLook(string name)
	{
		MokaTheme theme = Preset(name);

		Assert.Equal(MokaElevation.Shadow, theme.Elevation);
		Assert.Equal(MokaTextButtonColor.Accent, theme.TextButtons);
		Assert.Equal(MokaCalloutLook.Standard, theme.Callouts);
		Assert.Equal(MokaSwitchLook.Standard, theme.Switches);
		Assert.Equal(MokaSettingsHeaderLook.Standard, theme.SettingsHeaders);
		Assert.Equal(MokaCurrentPlace.Ring, theme.CurrentPlace);
		Assert.Equal("var(--moka-font-family-display)", theme.Typography.FontFamilyNumber);
		Assert.Equal("var(--moka-font-family)", theme.Typography.FontFamilyFigure);
		Assert.Equal("var(--moka-heading-tracking)", theme.Typography.NumberTracking);
	}

	[Theory]
	[MemberData(nameof(NewDesignPresets))]
	public void TodaysChoices_WriteTodaysValues(string name)
	{
		MokaTheme theme = Preset(name);
		Dictionary<string, string> tokens = Tokens(theme.ToCssVariables());

		Assert.Equal("var(--moka-color-primary-light)", tokens["--moka-button-text-color"]);
		Assert.Equal("var(--moka-color-on-surface)", tokens["--moka-button-text-hover-color"]);
		Assert.Equal("100%", tokens["--moka-callout-fill"]);
		Assert.Equal("0px", tokens["--moka-callout-edge"]);
		Assert.Equal("var(--moka-radius-lg)", tokens["--moka-callout-edge-radius"]);
		Assert.Equal("currentColor", tokens["--moka-switch-on-track"]);
		Assert.Equal("none", tokens["--moka-switch-on-ring"]);
		Assert.Equal("var(--moka-color-knob)", tokens["--moka-switch-on-knob"]);
		Assert.Equal("var(--moka-shadow-knob)", tokens["--moka-switch-on-knob-shadow"]);
		Assert.Equal("var(--moka-font-family-display)", tokens["--moka-font-family-number"]);
		Assert.Equal("var(--moka-font-family)", tokens["--moka-font-family-figure"]);
		Assert.Equal("var(--moka-heading-tracking)", tokens["--moka-number-tracking"]);
		Assert.Equal("var(--moka-font-size-display)", tokens["--moka-font-size-heading"]);
	}

	// Each choice at its own default changes nothing, so a stored value that names it is the same theme.
	[Theory]
	[MemberData(nameof(NewDesignPresets))]
	public void PickingTodaysOption_IsTheSameTheme(string name)
	{
		MokaTheme theme = Preset(name);

		MokaTheme same = theme
			.WithElevation(MokaElevation.Shadow)
			.WithTextButtons(MokaTextButtonColor.Accent)
			.WithNumberFont(MokaNumberFont.Title)
			.WithTitleSize(MokaTitleSize.Display)
			.WithCallouts(MokaCalloutLook.Standard)
			.WithSwitches(MokaSwitchLook.Standard)
			.WithSettingsHeaders(MokaSettingsHeaderLook.Standard);

		Assert.Equal(theme, same);
		Assert.Equal(theme.ToCssVariables(), same.ToCssVariables());
	}

	[Fact]
	public void NoChoice_MovesTheOriginalDesign()
	{
		foreach (MokaTheme original in new[] { MokaTheme.Dark, MokaTheme.Light })
		{
			Assert.Same(original, original.WithElevation(MokaElevation.Glow));
			Assert.Same(original, original.WithTextButtons(MokaTextButtonColor.Neutral));
			Assert.Same(original, original.WithNumberFont(MokaNumberFont.Mono));
			Assert.Same(original, original.WithTitleSize(MokaTitleSize.Compact));
			Assert.Same(original, original.WithCallouts(MokaCalloutLook.Edge));
			Assert.Same(original, original.WithSwitches(MokaSwitchLook.Tinted));
			Assert.Same(original, original.WithSettingsHeaders(MokaSettingsHeaderLook.Tinted));
			Assert.Same(original, original.WithCurrentPlace(MokaCurrentPlace.SolidInset));
		}
	}

	// Set past the With methods, as a theme read from JSON could be, the original design still writes today's values.
	[Fact]
	public void TheOriginalDesign_IgnoresTheChoicesEvenWhenSet()
	{
		MokaTheme set = MokaTheme.Dark with
		{
			Elevation = MokaElevation.Line,
			TextButtons = MokaTextButtonColor.Neutral,
			Callouts = MokaCalloutLook.Edge,
			Switches = MokaSwitchLook.Tinted,
			CurrentPlace = MokaCurrentPlace.SolidInset
		};

		Assert.Equal(MokaTheme.Dark.ToCssVariables(), set.ToCssVariables());
	}

	[Theory]
	[InlineData("--moka-font-family-number", "var(--moka-font-family-display)")]
	[InlineData("--moka-font-family-figure", "var(--moka-font-family)")]
	[InlineData("--moka-number-tracking", "var(--moka-heading-tracking)")]
	[InlineData("--moka-button-text-color", "var(--moka-color-primary-light)")]
	[InlineData("--moka-button-text-hover-color", "var(--moka-color-on-surface)")]
	[InlineData("--moka-callout-fill", "100%")]
	[InlineData("--moka-callout-edge", "0px")]
	[InlineData("--moka-callout-edge-radius", "var(--moka-radius-lg)")]
	[InlineData("--moka-switch-on-track", "currentColor")]
	[InlineData("--moka-switch-on-ring", "none")]
	[InlineData("--moka-switch-on-knob", "var(--moka-color-knob)")]
	[InlineData("--moka-switch-on-knob-shadow", "var(--moka-shadow-knob)")]
	public void NewTokens_StartFromTodaysValues_OnTheOriginalDesignAndInMokaCss(string token, string value)
	{
		Assert.Equal(value, Tokens(MokaTheme.Dark.ToCssVariables())[token]);
		Assert.Equal(value, Tokens(MokaTheme.Light.ToCssVariables())[token]);
		Assert.Equal(value, ScopedCss.Declarations(MokaCss, ":root")[token]);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Glow_RingsEveryFloatingPanelInTheAccent_AndLeavesCardsAlone(bool dark)
	{
		MokaTheme theme = (dark ? MokaTheme.Red : MokaTheme.RedLight).WithElevation(MokaElevation.Glow);
		Dictionary<string, string> tokens = Tokens(theme.ToCssVariables());
		Dictionary<string, string> today = Tokens((dark ? MokaTheme.Red : MokaTheme.RedLight).ToCssVariables());
		string glow = dark ? "var(--moka-color-primary-glow-md)" : "var(--moka-color-primary-glow)";

		foreach (string shadow in FloatingShadows)
		{
			Assert.StartsWith("0 0 0 1px var(--moka-color-primary-ring), 0 0 ", tokens[shadow], StringComparison.Ordinal);
			Assert.EndsWith(glow, tokens[shadow], StringComparison.Ordinal);
			Assert.DoesNotContain("rgba", tokens[shadow], StringComparison.Ordinal);
		}

		Assert.Equal("0 0 0 1px var(--moka-color-primary-ring), 0 0 24px " + glow, tokens["--moka-shadow-popup"]);
		Assert.Equal(today["--moka-shadow-1"], tokens["--moka-shadow-1"]);
		Assert.Equal(today["--moka-shadow-2"], tokens["--moka-shadow-2"]);
		Assert.Equal(today["--moka-shadow-subtle"], tokens["--moka-shadow-subtle"]);
		Assert.Equal(today["--moka-shadow-knob"], tokens["--moka-shadow-knob"]);
	}

	[Fact]
	public void Line_DrawsNoShadowPastAFloatingPanelsBorder()
	{
		Dictionary<string, string> tokens = Tokens(MokaTheme.Gate.WithElevation(MokaElevation.Line).ToCssVariables());

		Assert.All(FloatingShadows, shadow => Assert.Equal("none", tokens[shadow]));
	}

	[Fact]
	public void Shadow_IsTodaysDropShadow()
	{
		Dictionary<string, string> tokens = Tokens(MokaTheme.Red.ToCssVariables());

		Assert.Equal("0 18px 40px rgba(0, 0, 0, 0.45)", tokens["--moka-shadow-popup"]);
		Assert.Equal("0 3px 6px 2px rgba(0, 0, 0, 0.3)", tokens["--moka-shadow-modal"]);
	}

	[Fact]
	public void NeutralTextButtons_AreQuietUntilHovered()
	{
		Dictionary<string, string> tokens = Tokens(MokaTheme.Red.WithTextButtons(MokaTextButtonColor.Neutral).ToCssVariables());

		Assert.Equal("var(--moka-color-on-surface-2)", tokens["--moka-button-text-color"]);
		Assert.Equal("var(--moka-color-primary-light)", tokens["--moka-button-text-hover-color"]);
	}

	[Fact]
	public void MonoNumbers_LeaveTheTitleFontAlone()
	{
		MokaTheme theme = MokaTheme.Red.WithTitleFont(MokaTitleFont.Inter).WithNumberFont(MokaNumberFont.Mono);
		Dictionary<string, string> tokens = Tokens(theme.ToCssVariables());

		Assert.Equal("var(--moka-font-family-mono)", tokens["--moka-font-family-number"]);
		Assert.Equal("var(--moka-font-family-mono)", tokens["--moka-font-family-figure"]);
		Assert.Equal("0", tokens["--moka-number-tracking"]);
		Assert.Equal("var(--moka-font-family)", tokens["--moka-font-family-display"]);
		Assert.Equal(theme, theme.WithNumberFont(MokaNumberFont.Mono));
		Assert.Equal(MokaTheme.Red.WithTitleFont(MokaTitleFont.Inter), theme.WithNumberFont(MokaNumberFont.Title));
	}

	[Fact]
	public void ACompactTitle_Is17Pixels_AndFollowsTheFontScale()
	{
		Assert.Equal("1.0625rem", Tokens(MokaTheme.Red.WithTitleSize(MokaTitleSize.Compact).ToCssVariables())["--moka-font-size-heading"]);
		Assert.Equal("1.3281rem",
			Tokens(MokaTheme.Red.WithTitleSize(MokaTitleSize.Compact).WithFontScale(1.25).ToCssVariables())["--moka-font-size-heading"]);
	}

	// The text size swaps the scale steps, the title size the heading's own, so either can change without the other.
	[Fact]
	public void TitleSizeAndTextSize_AreApart()
	{
		MokaTheme small = MokaTheme.Red.WithTitleSize(MokaTitleSize.Compact).WithTextSize(MokaTextSize.Small);

		Assert.Equal("1.0625rem", small.Typography.FontSizeHeading);
		Assert.Equal(small, small.WithTitleSize(MokaTitleSize.Compact));
		Assert.Equal("var(--moka-font-size-display)", small.WithTitleSize(MokaTitleSize.Display).Typography.FontSizeHeading);
	}

	[Fact]
	public void EdgeCallouts_ShowNoneOfTheFill_AndAThreePixelEdge()
	{
		Dictionary<string, string> tokens = Tokens(MokaTheme.RedLight.WithCallouts(MokaCalloutLook.Edge).ToCssVariables());

		Assert.Equal("0%", tokens["--moka-callout-fill"]);
		Assert.Equal("3px", tokens["--moka-callout-edge"]);
		Assert.Equal("var(--moka-radius-sm)", tokens["--moka-callout-edge-radius"]);
	}

	[Fact]
	public void TintedSwitches_MixTheSwitchsOwnColor()
	{
		Dictionary<string, string> tokens = Tokens(MokaTheme.Red.WithSwitches(MokaSwitchLook.Tinted).ToCssVariables());

		Assert.Equal("color-mix(in srgb, currentColor 18%, transparent)", tokens["--moka-switch-on-track"]);
		Assert.Equal("inset 0 0 0 1px color-mix(in srgb, currentColor 45%, transparent)", tokens["--moka-switch-on-ring"]);
		Assert.Equal("currentColor", tokens["--moka-switch-on-knob"]);
		Assert.Equal("0 0 8px color-mix(in srgb, currentColor 45%, transparent)", tokens["--moka-switch-on-knob-shadow"]);
	}

	// Inset keeps everything Solid draws but the glow, which stays inside the row.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void SolidInset_IsSolid_WithItsGlowInside(bool dark)
	{
		MokaTheme baseTheme = dark ? MokaTheme.Red : MokaTheme.RedLight;
		Dictionary<string, string> solid = Tokens(baseTheme.WithCurrentPlace(MokaCurrentPlace.Solid).ToCssVariables());
		Dictionary<string, string> inset = Tokens(baseTheme.WithCurrentPlace(MokaCurrentPlace.SolidInset).ToCssVariables());

		foreach (string token in new[] { "--moka-current-bg", "--moka-current-color", "--moka-current-icon", "--moka-current-edge", "--moka-current-pill-ring" })
		{
			Assert.Equal(solid[token], inset[token]);
		}

		Assert.StartsWith("inset ", inset["--moka-current-shadow"], StringComparison.Ordinal);
		Assert.Equal("0 0 24px color-mix(in srgb, var(--moka-color-primary) 25%, transparent)", solid["--moka-current-shadow"]);
	}

	[Fact]
	public void Ember_IsMatrixWithEveryPick()
	{
		MokaTheme picked = MokaTheme.Matrix
			.WithElevation(MokaElevation.Glow)
			.WithTextButtons(MokaTextButtonColor.Neutral)
			.WithNumberFont(MokaNumberFont.Mono)
			.WithCallouts(MokaCalloutLook.Edge)
			.WithCorners(MokaCorners.Tight)
			.WithTitleFont(MokaTitleFont.Inter)
			.WithTitleSize(MokaTitleSize.Compact)
			.WithCurrentPlace(MokaCurrentPlace.SolidInset)
			.WithSwitches(MokaSwitchLook.Tinted)
			.WithSettingsHeaders(MokaSettingsHeaderLook.Tinted);

		Assert.Equal(picked, MokaTheme.Ember);
		Assert.Equal(MokaTheme.Matrix.Palette, MokaTheme.Ember.Palette);
		Assert.Equal(MokaLook.Gate, MokaTheme.Ember.Look);
	}

	[Fact]
	public void EmberLight_IsEmberOnTheLightScheme()
	{
		Assert.Equal(MokaTheme.EmberLight, MokaTheme.Ember.WithDark(false));
		Assert.Equal(MokaTheme.Ember, MokaTheme.EmberLight.WithDark(true));
		Assert.Equal(MokaTheme.MatrixLight.Palette, MokaTheme.EmberLight.Palette);
	}

	[Fact]
	public void EveryEmberChoice_CanStillBeChanged()
	{
		MokaTheme back = MokaTheme.Ember
			.WithElevation(MokaElevation.Shadow)
			.WithTextButtons(MokaTextButtonColor.Accent)
			.WithNumberFont(MokaNumberFont.Title)
			.WithCallouts(MokaCalloutLook.Standard)
			.WithCorners(MokaCorners.Soft)
			.WithTitleFont(MokaTitleFont.SpaceGrotesk)
			.WithTitleSize(MokaTitleSize.Display)
			.WithCurrentPlace(MokaCurrentPlace.Ring)
			.WithSwitches(MokaSwitchLook.Standard)
			.WithSettingsHeaders(MokaSettingsHeaderLook.Standard);

		Assert.Equal(MokaTheme.Matrix, back);
	}

	[Fact]
	public void TheChoices_LeaveTheColorsAlone()
	{
		MokaTheme changed = MokaTheme.Gate
			.WithElevation(MokaElevation.Line)
			.WithTextButtons(MokaTextButtonColor.Neutral)
			.WithNumberFont(MokaNumberFont.Mono)
			.WithTitleSize(MokaTitleSize.Compact)
			.WithCallouts(MokaCalloutLook.Edge)
			.WithSwitches(MokaSwitchLook.Tinted)
			.WithSettingsHeaders(MokaSettingsHeaderLook.Tinted);

		Assert.Equal(MokaTheme.Gate.Palette, changed.Palette);
		Assert.Equal(MokaTheme.Gate.Spacing, changed.Spacing);
	}

	private static MokaTheme Preset(string name) => name switch
	{
		"Red" => MokaTheme.Red,
		"RedLight" => MokaTheme.RedLight,
		"Gate" => MokaTheme.Gate,
		"GateLight" => MokaTheme.GateLight,
		"Crisp" => MokaTheme.Crisp,
		"CrispLight" => MokaTheme.CrispLight,
		"Matrix" => MokaTheme.Matrix,
		_ => MokaTheme.MatrixLight
	};

	private static Dictionary<string, string> Tokens(string css) =>
		Declaration().Matches(css).ToDictionary(m => m.Groups["name"].Value, m => m.Groups["value"].Value.Trim(), StringComparer.Ordinal);

	[GeneratedRegex(@"(?<name>--[\w-]+):\s*(?<value>[^;]*);")]
	private static partial Regex Declaration();
}
