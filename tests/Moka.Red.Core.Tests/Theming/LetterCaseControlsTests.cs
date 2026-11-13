using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

// MokaLetterCase.Controls: uppercase buttons and card titles, with page and dialog titles as written. The other two
// letter cases must write exactly what they wrote before the card title tokens existed.
public class LetterCaseControlsTests
{
	public static TheoryData<string> Presets => ["Red", "Gate", "Crisp", "Matrix", "Ember"];

	[Theory]
	[InlineData("--moka-control-case: uppercase;")]
	[InlineData("--moka-control-tracking: 0.08em;")]
	[InlineData("--moka-card-title-case: uppercase;")]
	[InlineData("--moka-card-title-tracking: 0.08em;")]
	[InlineData("--moka-chart-title-tracking: var(--moka-card-title-tracking);")]
	[InlineData("--moka-title-case: none;")]
	[InlineData("--moka-title-tracking: 0;")]
	[InlineData("--moka-heading-tracking: -0.02em;")]
	public void Controls_UppercasesControlsAndCardTitlesOnly(string expected) =>
		Assert.Contains(expected, MokaTheme.Red.WithLetterCase(MokaLetterCase.Controls).ToCssVariables(), StringComparison.Ordinal);

	[Fact]
	public void Controls_DrawsCardTitlesAtTheUppercaseSize() =>
		Assert.Equal(
			MokaTheme.Red.WithLetterCase(MokaLetterCase.Uppercase).Typography.FontSizeCardTitle,
			MokaTheme.Red.WithLetterCase(MokaLetterCase.Controls).Typography.FontSizeCardTitle);

	[Fact]
	public void Controls_KeepsBigNumbersTight()
	{
		string vars = MokaTheme.Red.WithLetterCase(MokaLetterCase.Controls).ToCssVariables();

		Assert.Contains("--moka-number-tracking: var(--moka-heading-tracking);", vars, StringComparison.Ordinal);
		Assert.Contains("--moka-heading-tracking: -0.02em;", vars, StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(Presets))]
	public void TheOtherCases_PointTheCardTokensAtTheSharedOnes(string preset)
	{
		foreach (MokaLetterCase letterCase in new[] { MokaLetterCase.Sentence, MokaLetterCase.Uppercase })
		{
			string vars = Preset(preset).WithLetterCase(letterCase).ToCssVariables();

			Assert.Contains("--moka-card-title-case: var(--moka-title-case);", vars, StringComparison.Ordinal);
			Assert.Contains("--moka-card-title-tracking: var(--moka-title-tracking);", vars, StringComparison.Ordinal);
			Assert.Contains("--moka-chart-title-tracking: var(--moka-heading-tracking);", vars, StringComparison.Ordinal);
		}
	}

	[Theory]
	[MemberData(nameof(Presets))]
	public void LeavingControls_PutsEverythingBack(string preset)
	{
		MokaTheme theme = Preset(preset);
		MokaTheme controls = theme.WithLetterCase(MokaLetterCase.Controls);

		Assert.Equal(theme.WithLetterCase(MokaLetterCase.Sentence), controls.WithLetterCase(MokaLetterCase.Sentence));
		Assert.Equal(theme.WithLetterCase(MokaLetterCase.Uppercase), controls.WithLetterCase(MokaLetterCase.Uppercase));
	}

	[Fact]
	public void ThePresets_StayInSentenceCase() =>
		Assert.Equal(MokaTheme.Red, MokaTheme.Red.WithLetterCase(MokaLetterCase.Sentence));

	[Fact]
	public void TheOriginalDesign_IgnoresIt()
	{
		Assert.Same(MokaTheme.Dark, MokaTheme.Dark.WithLetterCase(MokaLetterCase.Controls));
		Assert.Same(MokaTheme.Light, MokaTheme.Light.WithLetterCase(MokaLetterCase.Controls));
		Assert.Contains("--moka-card-title-case: var(--moka-title-case);", MokaTheme.Dark.ToCssVariables(), StringComparison.Ordinal);
		Assert.Contains("--moka-chart-title-tracking: var(--moka-heading-tracking);", MokaTheme.Dark.ToCssVariables(), StringComparison.Ordinal);
	}

	private static MokaTheme Preset(string name) => name switch
	{
		"Gate" => MokaTheme.Gate,
		"Crisp" => MokaTheme.Crisp,
		"Matrix" => MokaTheme.Matrix,
		"Ember" => MokaTheme.Ember,
		_ => MokaTheme.Red
	};
}
