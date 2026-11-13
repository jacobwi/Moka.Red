using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Primitives.Button;

namespace Moka.Red.Primitives.Tests.Components;

// A design audit: a disabled solid button on the new design is a dim fill with dark text and reads as broken. The
// faded look stays the default; Muted and Outline draw it in neutral colors, through the theme or the button.
public sealed partial class MokaButtonLookTests
{
	private static readonly string[] DisabledLooks = ["muted", "outline"];

	[Theory]
	[InlineData(MokaDisabledButtonLook.Faded, "faded")]
	[InlineData(MokaDisabledButtonLook.Muted, "muted")]
	[InlineData(MokaDisabledButtonLook.Outline, "outline")]
	public void DisabledLook_IsWrittenOnTheButtonAndTheLink(MokaDisabledButtonLook look, string name)
	{
		IRenderedComponent<MokaButton> button = Render<MokaButton>(p => p
			.Add(x => x.DisabledLook, look)
			.Add(x => x.Disabled, true)
			.AddChildContent("Save"));
		IRenderedComponent<MokaButton> link = Render<MokaButton>(p => p
			.Add(x => x.DisabledLook, look)
			.Add(x => x.Href, "/settings")
			.AddChildContent("Settings"));

		Assert.Equal(name, button.Find("button").GetAttribute("data-moka-disabled-look"));
		Assert.Equal(name, link.Find("a").GetAttribute("data-moka-disabled-look"));
	}

	[Fact]
	public void NoDisabledLook_WritesNoAttribute()
	{
		IElement button = Render<MokaButton>(p => p.Add(x => x.Disabled, true).AddChildContent("Save")).Find("button");

		Assert.False(button.HasAttribute("data-moka-disabled-look"));
	}

	[Fact]
	public void TheThemesDisabledLook_IsAClassOnTheOutermostRootOnly()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Red.WithDisabledButtons(MokaDisabledButtonLook.Muted))
			.AddChildContent<MokaThemeProvider>(inner => inner
				.Add(x => x.Theme, MokaTheme.Red.WithDisabledButtons(MokaDisabledButtonLook.Outline))
				.AddChildContent<MokaButton>(button => button.Add(x => x.Disabled, true).AddChildContent("Save"))));

		IReadOnlyList<IElement> roots = cut.FindAll(".moka-root");
		Assert.Contains("moka-disabled-buttons-muted", roots[0].ClassList);
		Assert.DoesNotContain(roots[1].ClassList, c => c.StartsWith("moka-disabled-buttons-", StringComparison.Ordinal));
		Assert.False(cut.Find("button").HasAttribute("data-moka-disabled-look"));
	}

	// Faded is how disabled buttons have always looked, so the default writes nothing and no rule names it.
	[Fact]
	public void Faded_WritesNoClassAndHasNoRules()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p.Add(x => x.Theme, MokaTheme.Red));

		Assert.DoesNotContain(cut.Find(".moka-root").ClassList, c => c.StartsWith("moka-disabled-buttons-", StringComparison.Ordinal));
		Assert.DoesNotContain("faded", File.ReadAllText(CssPath(ButtonCss)), StringComparison.Ordinal);
		Assert.Equal(MokaDisabledButtonLook.Faded, MokaTheme.Red.DisabledButtons);
	}

	[Fact]
	public void TheOriginalDesign_TakesNoDisabledLook()
	{
		MokaTheme dark = MokaTheme.Dark;
		Assert.Same(dark, dark.WithDisabledButtons(MokaDisabledButtonLook.Muted));

		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Dark with { DisabledButtons = MokaDisabledButtonLook.Muted }));

		Assert.DoesNotContain(cut.Find(".moka-root").ClassList, c => c.StartsWith("moka-disabled-buttons-", StringComparison.Ordinal));
		Assert.All(DisabledSelectors(), s => Assert.StartsWith(".moka-gate", s, StringComparison.Ordinal));
	}

	[Fact]
	public void EveryDisabledLookSelector_SkipsABusyButton_AndOnlyMatchesADisabledOne()
	{
		List<string> selectors = DisabledSelectors();

		Assert.NotEmpty(selectors);
		Assert.All(selectors, s =>
		{
			Assert.Contains(":not(.moka-btn--loading)", s, StringComparison.Ordinal);
			Assert.Contains(".moka-btn.moka-btn--disabled", s, StringComparison.Ordinal);
		});
	}

	[Fact]
	public void TheThemesDisabledSelectors_SkipAButtonWithADisabledLookOfItsOwn()
	{
		List<string> theme = [.. DisabledSelectors().Where(s => s.Contains("moka-disabled-buttons-", StringComparison.Ordinal))];

		Assert.NotEmpty(theme);
		Assert.All(theme, s => Assert.Contains(":not([data-moka-disabled-look])", s, StringComparison.Ordinal));
	}

	[Fact]
	public void EveryDisabledLook_HasTheThemesSelectorAndTheButtonsOwnInEveryRule()
	{
		foreach (string look in DisabledLooks)
		{
			List<List<string>> rules = [.. Rules().Select(r => r.Selectors)
				.Where(s => s.Any(x => NamesTheme(x, look) || NamesOwn(x, look)))];

			Assert.True(rules.Count > 0, $"No rules for the {look} disabled look.");
			Assert.All(rules, selectors => Assert.Equal(
				selectors.Count(x => NamesTheme(x, look)),
				selectors.Count(x => NamesOwn(x, look))));
		}
	}

	// Last in the file, so they win over every button look's rule they tie with.
	[Fact]
	public void TheDisabledLookRules_ComeAfterEveryButtonLooksRule()
	{
		List<List<string>> rules = [.. Rules().Select(r => r.Selectors)];
		int lastLook = rules.FindLastIndex(s => s.Any(x => x.Contains("data-moka-look", StringComparison.Ordinal)));
		int firstDisabled = rules.FindIndex(s => s.Any(x => x.Contains("disabled-look", StringComparison.Ordinal)));

		Assert.True(firstDisabled > lastLook, $"A disabled look rule ({firstDisabled}) comes before a look rule ({lastLook}).");
	}

	[Fact]
	public void Muted_IsANeutralFillWithMutedText_AtFullStrength()
	{
		const string all = ".moka-gate .moka-btn.moka-btn--disabled:is([data-moka-disabled-look=\"muted\"], [data-moka-disabled-look=\"outline\"]):not(.moka-btn--loading):is(.moka-btn--filled, .moka-btn--soft, .moka-btn--outlined, .moka-btn--text)";
		const string filled = ".moka-gate .moka-btn.moka-btn--disabled[data-moka-disabled-look=\"muted\"]:not(.moka-btn--loading):is(.moka-btn--filled, .moka-btn--soft)";
		Dictionary<string, string> every = Declarations(all);

		Assert.Equal("1", every["opacity"]);
		Assert.Equal("var(--moka-color-on-surface-tertiary)", every["color"]);
		Assert.Equal("none", every["background-image"]);
		Assert.Equal("none", every["box-shadow"]);
		Assert.Equal("var(--moka-color-neutral-bg)", Declarations(filled)["background-color"]);
	}

	[Fact]
	public void Outline_IsANeutralOutlineWithNoFill()
	{
		const string outline = ".moka-gate .moka-btn.moka-btn--disabled[data-moka-disabled-look=\"outline\"]:not(.moka-btn--loading):is(.moka-btn--filled, .moka-btn--soft, .moka-btn--outlined)";

		Assert.Equal("var(--moka-color-outline-strong)", Declarations(outline)["border-color"]);
		Assert.DoesNotContain(Rules().Where(r => r.Selectors.Any(s => NamesOwn(s, "outline")))
			.SelectMany(r => r.Declarations), d => d is { Property: "background-color", Value: not "transparent" });
	}

	private static bool NamesTheme(string selector, string look) =>
		selector.StartsWith(".moka-gate", StringComparison.Ordinal) && selector.Contains($"moka-disabled-buttons-{look}", StringComparison.Ordinal);

	private static bool NamesOwn(string selector, string look) =>
		selector.Contains($"[data-moka-disabled-look=\"{look}\"]", StringComparison.Ordinal);

	private static List<string> DisabledSelectors() =>
		[.. Selectors().Where(s => s.Contains("disabled-look", StringComparison.Ordinal) || s.Contains("moka-disabled-buttons-", StringComparison.Ordinal))];
}
