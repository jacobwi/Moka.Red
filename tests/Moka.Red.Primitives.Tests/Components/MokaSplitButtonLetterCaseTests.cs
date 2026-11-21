using Moka.Red.Core.Theming;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// A button resets letter spacing and case, so under an uppercase letter case on the new design a split button's label
// stayed as written while a MokaButton's turned uppercase.
public sealed class MokaSplitButtonLetterCaseTests
{
	private const string Css = "MokaSplitButton.razor.css";

	[Theory]
	[InlineData(".moka-gate .moka-split-btn__primary")]
	[InlineData(".moka-gate .moka-split-btn__toggle")]
	public void OnTheNewDesign_TheHalvesTakeTheSplitButtonsCase(string half)
	{
		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations(Css, half);

		Assert.Equal("inherit", rule["letter-spacing"]);
		Assert.Equal("inherit", rule["text-transform"]);
	}

	[Fact]
	public void OnTheNewDesign_TheSplitButtonReadsTheControlTokens()
	{
		IReadOnlyDictionary<string, string> root = ScopedCss.Declarations(Css, ".moka-gate .moka-split-btn");

		Assert.Equal("var(--moka-control-tracking)", root["letter-spacing"]);
		Assert.Equal("var(--moka-control-case)", root["text-transform"]);
	}

	// What the halves inherit at the default letter case is what a button gets: no case change and no extra spacing.
	[Fact]
	public void TheDefaultLetterCase_LeavesTheLabelAsWritten()
	{
		MokaTypography typography = MokaTheme.Default.Typography;

		Assert.Equal("none", typography.ControlCase);
		Assert.Equal("0", typography.ControlTracking);
	}

	[Theory]
	[InlineData(MokaLetterCase.Uppercase)]
	[InlineData(MokaLetterCase.Controls)]
	public void AnUppercaseLetterCase_ReachesControls(MokaLetterCase letterCase)
	{
		MokaTypography typography = MokaTheme.Default.WithLetterCase(letterCase).Typography;

		Assert.Equal("uppercase", typography.ControlCase);
		Assert.Equal("0.08em", typography.ControlTracking);
	}
}
