using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Theming;

// On the new design a medium button was 38 px, a toggle group 36 and a segmented control 44 beside 40 px fields, so a
// row of mixed controls didn't line up. Today's heights stay the default; WithControlHeights(Matched) gives buttons,
// toggle groups and segmented controls a field's height at every size, from one set of tokens the fields already used.
public class ControlHeightTests : BunitContext
{
	[Fact]
	public void ControlHeights_AreStandard_UntilAnAppSaysOtherwise()
	{
		Assert.Equal(MokaControlHeights.Standard, MokaTheme.Red.ControlHeights);
		Assert.Equal(MokaControlHeights.Matched, MokaTheme.Red.WithControlHeights(MokaControlHeights.Matched).ControlHeights);
	}

	[Fact]
	public void Matched_IsAClassOnEveryRootThatChoseIt_SoANestedProviderCanSayOtherwise()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Red.WithControlHeights(MokaControlHeights.Matched))
			.AddChildContent<MokaThemeProvider>(standard => standard
				.Add(x => x.Theme, MokaTheme.Red)
				.AddChildContent<MokaThemeProvider>(matched => matched
					.Add(x => x.Theme, MokaTheme.Gate.WithControlHeights(MokaControlHeights.Matched)))));

		IReadOnlyList<IElement> roots = cut.FindAll(".moka-root");
		Assert.Contains("moka-control-heights-matched", roots[0].ClassList);
		Assert.DoesNotContain("moka-control-heights-matched", roots[1].ClassList);
		Assert.Contains("moka-control-heights-matched", roots[2].ClassList);
	}

	[Fact]
	public void TheOriginalDesign_NeverGetsTheClass()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Dark with { ControlHeights = MokaControlHeights.Matched }));

		Assert.DoesNotContain("moka-control-heights-matched", cut.Find(".moka-root").ClassList);
	}

	[Theory]
	[InlineData("--moka-control-height-xs", "calc(var(--moka-control-height) - var(--moka-spacing-xl))")]
	[InlineData("--moka-control-height-sm", "calc(var(--moka-control-height) - var(--moka-spacing-md))")]
	[InlineData("--moka-control-height-lg", "calc(var(--moka-control-height) + var(--moka-spacing-md))")]
	public void EachSize_IsAStepFromTheMediumHeight(string token, string value)
	{
		Assert.Equal(value, ScopedCss.Declarations("moka.css", ":root")[token]);
		Assert.Equal(value, MokaTheme.Red.ToTokenMap()[token]);
	}

	// Every new-design root resets the variables and the class sets them, so the nearest provider decides. Components
	// read them with their own height as the fallback.
	[Theory]
	[InlineData("--moka-matched-height-xs", "var(--moka-control-height-xs)")]
	[InlineData("--moka-matched-height-sm", "var(--moka-control-height-sm)")]
	[InlineData("--moka-matched-height", "var(--moka-control-height)")]
	[InlineData("--moka-matched-height-lg", "var(--moka-control-height-lg)")]
	public void TheClass_SetsTheMatchedHeights_AndEveryRootResetsThem(string variable, string value)
	{
		Assert.Equal("initial", ScopedCss.Declarations("moka.css", ":where(.moka-root.moka-gate)")[variable]);
		Assert.Equal(value, ScopedCss.Declarations("moka.css", ".moka-control-heights-matched")[variable]);
	}
}
