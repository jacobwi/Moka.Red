using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Theming;

// The tooltip choices: on the theme, as classes on new-design roots that are absent at their defaults, and as the
// variables moka.css hangs off those classes.
public class TooltipChoiceTests : BunitContext
{
	private const string MokaCss = "moka.css";

	private static readonly string[] LookVariables =
	[
		"--moka-tooltip-look-bg", "--moka-tooltip-look-color", "--moka-tooltip-look-muted", "--moka-tooltip-look-border",
		"--moka-tooltip-look-radius", "--moka-tooltip-look-shadow", "--moka-tooltip-look-blur", "--moka-tooltip-look-font"
	];

	[Fact]
	public void EveryThemeStartsAtTodaysTooltips()
	{
		foreach (MokaTheme theme in new[] { MokaTheme.Default, MokaTheme.Red, MokaTheme.GateLight, MokaTheme.Crisp, MokaTheme.Dark, MokaTheme.Light })
		{
			Assert.Equal(MokaTooltipLook.Standard, theme.Tooltips);
			Assert.False(theme.TooltipArrows);
			Assert.Equal(MokaTooltipMotion.Fade, theme.TooltipMotion);
			Assert.Equal(MokaTooltipSize.Standard, theme.TooltipSize);
		}
	}

	[Fact]
	public void TheMethods_SetTheirChoice_AndLeaveTheColorsAlone()
	{
		MokaTheme theme = MokaTheme.Red
			.WithTooltips(MokaTooltipLook.Glass)
			.WithTooltipArrows(true)
			.WithTooltipMotion(MokaTooltipMotion.Slide)
			.WithTooltipSize(MokaTooltipSize.Roomy);

		Assert.Equal(MokaTooltipLook.Glass, theme.Tooltips);
		Assert.True(theme.TooltipArrows);
		Assert.Equal(MokaTooltipMotion.Slide, theme.TooltipMotion);
		Assert.Equal(MokaTooltipSize.Roomy, theme.TooltipSize);
		Assert.Equal(MokaTheme.Red.Palette, theme.Palette);
		Assert.Equal(MokaTheme.Red.ToCssVariables(), theme.ToCssVariables());
		Assert.False(theme.WithTooltipArrows(false).TooltipArrows);
	}

	[Fact]
	public void NoTooltipChoice_MovesTheOriginalDesign()
	{
		foreach (MokaTheme original in new[] { MokaTheme.Dark, MokaTheme.Light })
		{
			Assert.Same(original, original.WithTooltips(MokaTooltipLook.Terminal));
			Assert.Same(original, original.WithTooltipArrows(true));
			Assert.Same(original, original.WithTooltipMotion(MokaTooltipMotion.Grow));
			Assert.Same(original, original.WithTooltipSize(MokaTooltipSize.Compact));
		}
	}

	// Apps that already pick a component style keep today's tooltips.
	[Theory]
	[InlineData(MokaComponentStyle.Soft)]
	[InlineData(MokaComponentStyle.Terminal)]
	public void AComponentStyle_LeavesTheTooltipsAlone(MokaComponentStyle style)
	{
		MokaTheme theme = MokaTheme.Red.WithTooltips(MokaTooltipLook.Flat).WithComponentStyle(style);

		Assert.Equal(MokaTooltipLook.Flat, theme.Tooltips);
		Assert.Equal(MokaTooltipLook.Standard, MokaTheme.Red.WithComponentStyle(style).Tooltips);
	}

	[Fact]
	public void AtTheDefaults_TheRootCarriesNoTooltipClass()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p.Add(x => x.Theme, MokaTheme.Red));

		Assert.DoesNotContain(cut.Find(".moka-root").ClassList, c => c.StartsWith("moka-tooltips", StringComparison.Ordinal));
	}

	[Theory]
	[InlineData(MokaTooltipLook.Flat, "moka-tooltips-flat")]
	[InlineData(MokaTooltipLook.Elevated, "moka-tooltips-elevated")]
	[InlineData(MokaTooltipLook.Glass, "moka-tooltips-glass")]
	[InlineData(MokaTooltipLook.AccentTint, "moka-tooltips-accent-tint")]
	[InlineData(MokaTooltipLook.Contrast, "moka-tooltips-contrast")]
	[InlineData(MokaTooltipLook.Terminal, "moka-tooltips-terminal")]
	public void ALook_IsAClassOnTheRoot(MokaTooltipLook look, string expected)
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p.Add(x => x.Theme, MokaTheme.Red.WithTooltips(look)));

		Assert.Equal([expected], TooltipClasses(cut.Find(".moka-root")));
	}

	[Fact]
	public void ArrowsMotionAndSize_AreClassesOnTheRoot()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p.Add(x => x.Theme, MokaTheme.Red
			.WithTooltipArrows(true)
			.WithTooltipMotion(MokaTooltipMotion.Grow)
			.WithTooltipSize(MokaTooltipSize.Compact)));

		Assert.Equal(["moka-tooltips-arrow", "moka-tooltips-motion-grow", "moka-tooltips-size-compact"], TooltipClasses(cut.Find(".moka-root")));
	}

	[Theory]
	[InlineData(MokaTooltipMotion.Slide, "moka-tooltips-motion-slide")]
	[InlineData(MokaTooltipMotion.Instant, "moka-tooltips-motion-instant")]
	public void EachMotion_HasItsClass(MokaTooltipMotion motion, string expected)
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p.Add(x => x.Theme, MokaTheme.Red.WithTooltipMotion(motion)));

		Assert.Equal([expected], TooltipClasses(cut.Find(".moka-root")));
	}

	[Fact]
	public void TheRoomySize_HasItsClass()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p.Add(x => x.Theme, MokaTheme.Red.WithTooltipSize(MokaTooltipSize.Roomy)));

		Assert.Equal(["moka-tooltips-size-roomy"], TooltipClasses(cut.Find(".moka-root")));
	}

	// The classes only set variables, so every provider writes its own and the nearest wins; a provider at the defaults
	// writes none, and moka.css resets the variables on its root.
	[Fact]
	public void ANestedProviderAtTheDefaults_WritesNone()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Red.WithTooltips(MokaTooltipLook.Glass).WithTooltipArrows(true))
			.AddChildContent<MokaThemeProvider>(inner => inner
				.Add(x => x.Theme, MokaTheme.RedLight)
				.AddChildContent<MokaThemeProvider>(innermost => innermost.Add(x => x.Theme, MokaTheme.Red.WithTooltips(MokaTooltipLook.Contrast)))));

		IReadOnlyList<IElement> roots = cut.FindAll(".moka-root");

		Assert.Equal(["moka-tooltips-glass", "moka-tooltips-arrow"], TooltipClasses(roots[0]));
		Assert.Empty(TooltipClasses(roots[1]));
		Assert.Equal(["moka-tooltips-contrast"], TooltipClasses(roots[2]));
	}

	// Built by hand, a Matrix theme could hold a choice the With methods would refuse. The original design still writes
	// nothing.
	[Fact]
	public void TheOriginalDesign_WritesNoTooltipClass()
	{
		MokaTheme handMade = MokaTheme.Dark with
		{
			Tooltips = MokaTooltipLook.Glass,
			TooltipArrows = true,
			TooltipMotion = MokaTooltipMotion.Slide,
			TooltipSize = MokaTooltipSize.Roomy
		};
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p.Add(x => x.Theme, handMade));

		Assert.Empty(TooltipClasses(cut.Find(".moka-root")));
	}

	[Fact]
	public void EveryNewDesignRoot_ResetsTheTooltipVariables_AtNoSpecificity()
	{
		string css = Normalized();

		Assert.Contains(":where(.moka-root.moka-gate),\n.moka-tooltips-standard,", css, StringComparison.Ordinal);
		Assert.Contains(":where(.moka-root.moka-gate),\n.moka-tooltips-no-arrow {", css, StringComparison.Ordinal);
		Assert.Contains(":where(.moka-root.moka-gate),\n.moka-tooltips-motion-fade,", css, StringComparison.Ordinal);
		Assert.Contains(":where(.moka-root.moka-gate),\n.moka-tooltips-size-compact,", css, StringComparison.Ordinal);

		IReadOnlyDictionary<string, string> standard = ScopedCss.Declarations(MokaCss, ".moka-tooltips-standard");
		Assert.All(LookVariables, variable => Assert.Equal("initial", standard[variable]));
	}

	// A chart tooltip falls back to the popup look for any variable a tooltip look leaves unset, so every look but
	// Standard sets all of them.
	[Theory]
	[InlineData(".moka-tooltips-flat")]
	[InlineData(".moka-tooltips-elevated")]
	[InlineData(".moka-tooltips-glass")]
	[InlineData(".moka-tooltips-accent-tint")]
	[InlineData(".moka-tooltips-contrast")]
	[InlineData(".moka-tooltips-terminal")]
	public void EveryLookButStandard_SetsEveryVariable(string look)
	{
		IReadOnlyDictionary<string, string> rules = ScopedCss.Declarations(MokaCss, look);

		Assert.All(LookVariables, variable => Assert.NotEqual("initial", rules[variable]));
	}

	[Fact]
	public void TheLooks_CopyThePopupLooksCharacter()
	{
		Assert.Equal("none", ScopedCss.Declarations(MokaCss, ".moka-tooltips-flat")["--moka-tooltip-look-shadow"]);
		Assert.Equal("transparent", ScopedCss.Declarations(MokaCss, ".moka-tooltips-elevated")["--moka-tooltip-look-border"]);
		Assert.StartsWith("blur(", ScopedCss.Declarations(MokaCss, ".moka-tooltips-glass")["--moka-tooltip-look-blur"], StringComparison.Ordinal);
		Assert.Equal("var(--moka-color-primary-ring)", ScopedCss.Declarations(MokaCss, ".moka-tooltips-accent-tint")["--moka-tooltip-look-border"]);
		Assert.Equal("var(--moka-color-on-surface)", ScopedCss.Declarations(MokaCss, ".moka-tooltips-contrast")["--moka-tooltip-look-bg"]);
		Assert.Contains("--moka-color-lift", ScopedCss.Declarations(MokaCss, ".moka-dark.moka-tooltips-contrast")["--moka-tooltip-look-bg"], StringComparison.Ordinal);
		Assert.Contains("--moka-color-lift", ScopedCss.Declarations(MokaCss, ".moka-dark .moka-tooltips-contrast:not(.moka-root)")["--moka-tooltip-look-bg"], StringComparison.Ordinal);

		IReadOnlyDictionary<string, string> terminal = ScopedCss.Declarations(MokaCss, ".moka-tooltips-terminal");
		Assert.Equal("var(--moka-color-background)", terminal["--moka-tooltip-look-bg"]);
		Assert.Equal("var(--moka-font-family-mono)", terminal["--moka-tooltip-look-font"]);
		Assert.Contains("--moka-color-primary-glow", terminal["--moka-tooltip-look-shadow"], StringComparison.Ordinal);
	}

	[Fact]
	public void TheArrowClass_TurnsTheArrowOn_AndNoArrowTurnsItOff()
	{
		Assert.Equal("block", ScopedCss.Declarations(MokaCss, ".moka-tooltips-arrow")["--moka-tooltip-arrow-display"]);
		Assert.Equal("initial", ScopedCss.Declarations(MokaCss, ".moka-tooltips-no-arrow")["--moka-tooltip-arrow-display"]);
	}

	[Fact]
	public void TheMotions_SetWhereTheyStart()
	{
		IReadOnlyDictionary<string, string> fade = ScopedCss.Declarations(MokaCss, ".moka-tooltips-motion-fade");
		Assert.Equal("4px", fade["--moka-tooltip-motion-from"]);
		Assert.Equal("none", fade["--moka-tooltip-motion-scale"]);
		Assert.Equal("0.15s", fade["--moka-tooltip-motion-time"]);

		Assert.Equal("initial", fade["--moka-tooltip-motion-margin"]);

		Assert.Equal(["4px", "0px", "4px"], ScopedCss.Values(MokaCss, ".moka-tooltips-motion-slide", "--moka-tooltip-motion-from"));
		Assert.Equal(["initial", "margin", "initial"], ScopedCss.Values(MokaCss, ".moka-tooltips-motion-slide", "--moka-tooltip-motion-margin"));
		Assert.Equal(["none", "0.96", "none"], ScopedCss.Values(MokaCss, ".moka-tooltips-motion-grow", "--moka-tooltip-motion-scale"));
		Assert.Equal("0s", ScopedCss.Declarations(MokaCss, ".moka-tooltips-motion-instant")["--moka-tooltip-motion-time"]);
	}

	// The last values for Slide and Grow above come from here.
	[Fact]
	public void ReducedMotion_TurnsSlideAndGrowIntoAFade()
	{
		string css = Normalized();
		int media = css.IndexOf("@media (prefers-reduced-motion: reduce) {\n\t.moka-tooltips-motion-slide,", StringComparison.Ordinal);

		Assert.True(media > 0);
		Assert.Contains("--moka-tooltip-motion-from: 4px;", css[media..css.IndexOf("}\n}", media, StringComparison.Ordinal)], StringComparison.Ordinal);
	}

	[Fact]
	public void TheSizes_ReadTokens()
	{
		IReadOnlyDictionary<string, string> compact = ScopedCss.Declarations(MokaCss, ".moka-tooltips-size-compact");
		IReadOnlyDictionary<string, string> roomy = ScopedCss.Declarations(MokaCss, ".moka-tooltips-size-roomy");

		Assert.Equal("var(--moka-font-size-xs)", compact["--moka-tooltip-size-font"]);
		Assert.Equal("var(--moka-font-size-base)", roomy["--moka-tooltip-size-font"]);
		Assert.StartsWith("var(--moka-spacing-", compact["--moka-tooltip-size-padding"], StringComparison.Ordinal);
		Assert.Equal("initial", ScopedCss.Declarations(MokaCss, ".moka-tooltips-size-standard")["--moka-tooltip-size-font"]);
	}

	private static string[] TooltipClasses(IElement root) =>
		[.. root.ClassList.Where(c => c.StartsWith("moka-tooltips", StringComparison.Ordinal))];

	private static string Normalized() =>
		File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", MokaCss)).ReplaceLineEndings("\n");

	[Fact]
	public void TheClassNames_FollowTheEnumNames()
	{
		Assert.Equal("accent-tint", MokaEnumHelpers.ToCssClass(MokaTooltipLook.AccentTint));
		Assert.Equal("instant", MokaEnumHelpers.ToCssClass(MokaTooltipMotion.Instant));
		Assert.Equal("roomy", MokaEnumHelpers.ToCssClass(MokaTooltipSize.Roomy));
	}
}
