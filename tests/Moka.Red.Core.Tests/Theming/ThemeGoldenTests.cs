using System.Text;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

/// <summary>
///     What every built-in theme, a spread of style choices and the accent methods write, captured before the theming
///     engine moved onto the shared color library and the token table. A theme has to write these files byte for byte,
///     with the root classes its provider gives it. Set <c>MOKA_RECORD_GOLDENS=1</c> to write the files again after a
///     change that is meant to show.
/// </summary>
public sealed class ThemeGoldenTests : BunitContext
{
	// MokaThemeProvider writes its token styles as text or as markup, depending on the renderer.
	public ThemeGoldenTests() => SetRendererInfo(new RendererInfo("Server", true));

	private static readonly Dictionary<string, Func<MokaTheme>> Cases = new(StringComparer.Ordinal)
	{
		["red"] = () => MokaTheme.Default,
		["red-light"] = () => MokaTheme.DefaultLight,
		["gate"] = () => MokaTheme.Gate,
		["gate-light"] = () => MokaTheme.GateLight,
		["crisp"] = () => MokaTheme.Crisp,
		["crisp-light"] = () => MokaTheme.CrispLight,
		["matrix"] = () => MokaTheme.Matrix,
		["matrix-light"] = () => MokaTheme.MatrixLight,
		["ember"] = () => MokaTheme.Ember,
		["ember-light"] = () => MokaTheme.EmberLight,
		["dark"] = () => MokaTheme.Dark,
		["light"] = () => MokaTheme.Light,

		["style-compact-small"] = () => MokaTheme.Red
			.WithDensity(MokaDensity.Compact)
			.WithTextSize(MokaTextSize.Small)
			.WithCorners(MokaCorners.Medium)
			.WithMainButton(MokaMainButton.Tinted)
			.WithLetterCase(MokaLetterCase.Uppercase)
			.WithTitleFont(MokaTitleFont.JetBrainsMono)
			.WithSelection(MokaSelection.Glow)
			.WithCurrentPlace(MokaCurrentPlace.Solid),
		["style-principles"] = () => MokaTheme.GateLight
			.WithMainButton(MokaMainButton.Outline)
			.WithLetterCase(MokaLetterCase.Controls)
			.WithSelection(MokaSelection.Flat)
			.WithCurrentPlace(MokaCurrentPlace.SolidInset)
			.WithElevation(MokaElevation.Glow)
			.WithTextButtons(MokaTextButtonColor.Neutral)
			.WithNumberFont(MokaNumberFont.Mono)
			.WithTitleSize(MokaTitleSize.Compact)
			.WithCallouts(MokaCalloutLook.Edge)
			.WithSwitches(MokaSwitchLook.Tinted)
			.WithDescriptions(false),
		["style-line-muted"] = () => MokaTheme.Gate
			.WithElevation(MokaElevation.Line)
			.WithLabelTone(MokaLabelTone.Muted)
			.WithChartPalette(MokaChartPalette.Reference),
		["style-pastel-accent-first"] = () => MokaTheme.RedLight
			.WithLabelTone(MokaLabelTone.Pastel)
			.WithChartPalette(MokaChartPalette.AccentFirst),
		["style-components"] = () => MokaTheme.Red
			.WithComponentStyle(MokaComponentStyle.Soft)
			.WithTooltips(MokaTooltipLook.Glass)
			.WithTooltipArrows()
			.WithTooltipMotion(MokaTooltipMotion.Slide)
			.WithTooltipSize(MokaTooltipSize.Roomy)
			.WithButtons(MokaButtonLook.Glow)
			.WithDisabledButtons(MokaDisabledButtonLook.Outline)
			.WithPopups(MokaPopupLook.Contrast),
		["style-terminal-light"] = () => MokaTheme.CrispLight
			.WithComponentStyle(MokaComponentStyle.Terminal)
			.WithTooltips(MokaTooltipLook.Contrast)
			.WithTooltipMotion(MokaTooltipMotion.Instant)
			.WithTooltipSize(MokaTooltipSize.Compact)
			.WithDisabledButtons(MokaDisabledButtonLook.Muted),
		["style-matrix-muted-contrast"] = () => MokaTheme.Matrix
			.WithChartPalette(MokaChartPalette.Muted)
			.WithHighContrast(),
		["density-scale-red"] = () => MokaTheme.Red.WithDensity(0.75).WithFontScale(1.1),
		["density-scale-dark"] = () => MokaTheme.Dark.WithDensity(1.15).WithFontScale(0.9),

		["contrast-gate-light"] = () => MokaTheme.GateLight.WithHighContrast(),
		["contrast-red"] = () => MokaTheme.Red.WithHighContrast(),
		["contrast-red-light-off"] = () => MokaTheme.RedLight.WithHighContrast().WithHighContrast(false),
		["surfaces-red-bluegrey"] = () => MokaTheme.Red.WithSurfaces(MokaSurfaces.BlueGrey),
		["surfaces-gate-light-nearblack"] = () => MokaTheme.GateLight.WithSurfaces(MokaSurfaces.NearBlack),
		["dark-red-to-light"] = () => MokaTheme.Red.WithDark(false),
		["dark-gate-light-to-dark"] = () => MokaTheme.GateLight.WithDark(true),
		["dark-light-to-dark"] = () => MokaTheme.Light.WithDark(true),

		["accent-gate-green"] = () => MokaTheme.Gate.WithAccent("#00e676"),
		["accent-gate-light-purple"] = () => MokaTheme.GateLight.WithAccent("#7e57c2"),
		["accent-red-violet"] = () => MokaTheme.Red.WithAccent(MokaAccent.Violet),
		["accent-red-light-cyan"] = () => MokaTheme.RedLight.WithAccent(MokaAccent.Cyan),
		["accent-gate-preset-hex"] = () => MokaTheme.Gate.WithAccent("#6E92FF"),
		["accent-gate-grey"] = () => MokaTheme.Gate.WithAccent("#888"),
		["accent-red-hue-accent-first"] = () => MokaTheme.Red
			.WithAccent(MokaAccent.FromHue(140, 0.6))
			.WithChartPalette(MokaChartPalette.AccentFirst),
		["accent-matrix-orange-accent-first"] = () => MokaTheme.Matrix
			.WithAccent("#ff9800")
			.WithChartPalette(MokaChartPalette.AccentFirst),
		["accent-gate-light-amber-accent-first"] = () => MokaTheme.GateLight
			.WithAccent("#ffb547")
			.WithChartPalette(MokaChartPalette.AccentFirst),
		["accent-dark-blue"] = () => MokaTheme.Dark.WithAccent("#42a5f5"),
		["accent-light-purple"] = () => MokaTheme.Light.WithAccent("#7e57c2"),
		["accent-dark-black"] = () => MokaTheme.Dark.WithAccent("#000"),
		["accent-light-pink"] = () => MokaTheme.Light.WithAccent(MokaAccent.Pink),
		["accent-dark-then-light"] = () => MokaTheme.Dark.WithAccent("#42a5f5").WithDark(false),
		["accent-gate-then-light"] = () => MokaTheme.Gate.WithAccent("#3cc8e6").WithDark(false),
		["accent-contrast-off"] = () => MokaTheme.Red.WithHighContrast().WithAccent("#26a69a").WithHighContrast(false)
	};

	public static TheoryData<string> Names => [.. Cases.Keys];

	[Theory]
	[MemberData(nameof(Names))]
	public void EveryTheme_WritesWhatItWrote(string name)
	{
		MokaTheme theme = Cases[name]();
		string tokens = theme.ToCssVariables();

		IRenderedComponent<MokaThemeProvider> outer = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, theme)
			.AddChildContent<MokaThemeProvider>(inner => inner
				.Add(x => x.Theme, theme)
				.AddChildContent("<span>Nested</span>")));

		IReadOnlyList<IElement> roots = outer.FindAll(".moka-root");
		string rootStyle = outer.FindAll("style")[0].TextContent;
		string pageStyle = rootStyle[(":root { ".Length + tokens.Length)..^" }".Length].Trim();

		Assert.StartsWith($":root {{ {tokens}", rootStyle, StringComparison.Ordinal);
		Assert.Equal(tokens, roots[0].GetAttribute("style"));

		var text = new StringBuilder();
		text.Append("class: ").Append(roots[0].GetAttribute("class")).Append('\n');
		text.Append("nested-class: ").Append(roots[1].GetAttribute("class")).Append('\n');
		text.Append("page: ").Append(pageStyle).Append('\n');
		text.Append("root-declares: ").Append(Count(roots[0].GetAttribute("style"))).Append('\n');
		text.Append("nested-root-declares: ").Append(Count(roots[1].GetAttribute("style"))).Append('\n');
		text.Append('\n');
		foreach (string declaration in tokens.Split("; ", StringSplitOptions.RemoveEmptyEntries))
		{
			text.Append(declaration.TrimEnd(';')).Append('\n');
		}

		Golden(name, text.ToString());
	}

	private static void Golden(string name, string text) => GoldenFiles.Check(name, text);

	private static int Count(string? style) => style?.Split("; ", StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
}
