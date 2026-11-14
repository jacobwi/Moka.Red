using Moka.Red.Core.Colors;
using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

public class ContrastReportTests
{
	public static TheoryData<string> Presets => [.. PresetThemes.Keys];

	private static Dictionary<string, MokaTheme> PresetThemes { get; } = new(StringComparer.Ordinal)
	{
		["red"] = MokaTheme.Red, ["red-light"] = MokaTheme.RedLight, ["gate"] = MokaTheme.Gate, ["gate-light"] = MokaTheme.GateLight,
		["crisp"] = MokaTheme.Crisp, ["crisp-light"] = MokaTheme.CrispLight, ["matrix"] = MokaTheme.Matrix,
		["matrix-light"] = MokaTheme.MatrixLight, ["ember"] = MokaTheme.Ember, ["ember-light"] = MokaTheme.EmberLight,
		["dark"] = MokaTheme.Dark, ["light"] = MokaTheme.Light
	};

	// What every preset's text and lines measure today, failures included, so a change that moves them shows.
	[Theory]
	[MemberData(nameof(Presets))]
	public void EveryPresetsReport_IsWhatItWas(string name)
	{
		MokaContrastReport report = PresetThemes[name].CheckContrast();

		Assert.Empty(report.Unresolved);
		GoldenFiles.Check($"report-{name}", report.ToString());
	}

	[Fact]
	public void TheReport_ListsBodyTextOnEverySurface()
	{
		MokaContrastReport report = MokaTheme.Red.CheckContrast();

		Assert.Contains(report.Checks, check => check is { Foreground: "--moka-color-on-surface", Background: "--moka-color-surface-variant" });
		MokaContrastCheck body = report.Checks.First(check => check is { Foreground: "--moka-color-on-surface", Background: "--moka-color-surface" });
		Assert.Equal(MokaContrastRequirement.Text, body.Requirement);
		Assert.Equal(4.5, body.RequiredRatio);
		Assert.True(body.Passes);
		Assert.True(body.Apca < -90);
	}

	[Fact]
	public void APin_ShowsInTheReport()
	{
		MokaContrastReport report = MokaTheme.Red.WithToken("--moka-color-on-surface", "#303036").CheckContrast();

		Assert.Contains(report.Failures, check => check is { Foreground: "--moka-color-on-surface", Background: "--moka-color-surface" });
		Assert.False(report.Passes);
	}

	[Theory]
	[InlineData("--moka-color-primary", "#EF5350")]
	[InlineData("var(--moka-color-primary)", "#EF5350")]
	[InlineData("var(--moka-missing, #123456)", "#123456")]
	[InlineData("color-mix(in srgb, #ff0000 50%, #0000ff)", "#800080")]
	[InlineData("color-mix(in srgb, #ff0000, #0000ff 25%)", "#bf0040")]
	[InlineData("color-mix(in srgb, var(--moka-color-primary) 100%, black)", "#ef5350")]
	[InlineData("color-mix(in oklab, white 50%, black)", "#636363")]
	public void TryResolveColor_WorksValuesOutLikeABrowser(string value, string hex)
	{
		Assert.True(MokaTheme.Red.TryResolveColor(value, out MokaSrgb color));
		Assert.Equal(hex, color.ToHex(), ignoreCase: true);
	}

	[Fact]
	public void TryResolveColor_FadesAMixWhosePercentagesFallShort()
	{
		Assert.True(MokaTheme.Red.TryResolveColor("color-mix(in srgb, #ff0000 20%, #0000ff 20%)", out MokaSrgb color));

		Assert.Equal(0.4, color.Alpha, 6);
	}

	[Theory]
	[InlineData("currentColor")]
	[InlineData("var(--moka-missing)")]
	[InlineData("color-mix(in hsl, red, blue)")]
	[InlineData("--moka-switch-on-track")]
	public void TryResolveColor_RefusesWhatHasNoSingleAnswer(string value) =>
		Assert.False(MokaTheme.Red.TryResolveColor(value, out _));
}
