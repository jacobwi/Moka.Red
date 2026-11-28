using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Theming;
using Moka.Red.Primitives.Button;

namespace Moka.Red.Primitives.Tests.Components;

// A button couldn't be a toggle: an app passed aria-pressed itself and no style showed it. Pressed makes it one, opt-in,
// with a look per Quiet button and a fill per other variant, through the theme or the button.
public sealed partial class MokaButtonLookTests
{
	private static readonly string[] PressedQuietLooks = ["raised", "accent-icon", "ring", "bar"];
	private static readonly string[] PressedTones = ["error", "warning", "success", "info"];

	[Fact]
	public void WithoutPressed_TheButtonIsAsItWas()
	{
		IElement button = Render<MokaButton>(p => p.AddChildContent("Save")).Find("button");

		Assert.False(button.HasAttribute("aria-pressed"));
		Assert.False(button.HasAttribute("data-moka-pressed-look"));
		Assert.False(button.HasAttribute("data-moka-pressed-fill"));
		Assert.DoesNotContain("moka-btn--pressed", button.ClassList);
	}

	[Theory]
	[InlineData(true, "true")]
	[InlineData(false, "false")]
	public void Pressed_SaysWhetherTheToggleIsOn_AndDrawsItWhileOn(bool pressed, string state)
	{
		IElement button = Render<MokaButton>(p => p.Add(x => x.Pressed, pressed).Add(x => x.Quiet, true)).Find("button");

		Assert.Equal(state, button.GetAttribute("aria-pressed"));
		Assert.Equal(pressed, button.ClassList.Contains("moka-btn--pressed"));
	}

	// Apps already pass aria-pressed with looks of their own, so the attribute alone changes nothing.
	[Fact]
	public void AnAppsOwnAriaPressed_PassesThroughUndrawn()
	{
		IElement button = Render<MokaButton>(p => p
			.AddUnmatched("aria-pressed", "true")
			.AddChildContent("Star")).Find("button");

		Assert.Equal("true", button.GetAttribute("aria-pressed"));
		Assert.DoesNotContain("moka-btn--pressed", button.ClassList);
	}

	[Fact]
	public void AClick_RaisesPressedChangedWithTheOpposite_BeforeOnClick()
	{
		List<string> heard = [];
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Pressed, false)
			.Add(x => x.PressedChanged, (bool on) => heard.Add($"pressed {on}"))
			.Add(x => x.OnClick, (MouseEventArgs _) => heard.Add("click"))
			.AddChildContent("Pin"));

		cut.Find("button").Click();

		Assert.Equal(["pressed True", "click"], heard);
	}

	// The parent owns the state: a parent that doesn't take the change keeps the toggle as it was.
	[Fact]
	public void TheButton_ShowsThePressedItsParentPasses()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Pressed, false)
			.Add(x => x.PressedChanged, (bool _) => { })
			.AddChildContent("Pin"));

		cut.Find("button").Click();
		Assert.Equal("false", cut.Find("button").GetAttribute("aria-pressed"));

		cut.Render(p => p.Add(x => x.Pressed, true));
		Assert.Equal("true", cut.Find("button").GetAttribute("aria-pressed"));
	}

	[Fact]
	public void ADisabledToggle_RaisesNothing()
	{
		bool heard = false;
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Pressed, true)
			.Add(x => x.Disabled, true)
			.Add(x => x.PressedChanged, (bool _) => heard = true)
			.AddChildContent("Pin"));

		cut.Find("button").Click();

		Assert.False(heard);
		Assert.Equal("true", cut.Find("button").GetAttribute("aria-pressed"));
	}

	[Fact]
	public void ALink_IsNeverAToggle()
	{
		IElement link = Render<MokaButton>(p => p
			.Add(x => x.Href, "/settings")
			.Add(x => x.Pressed, true)
			.AddChildContent("Settings")).Find("a");

		Assert.False(link.HasAttribute("aria-pressed"));
		Assert.DoesNotContain("moka-btn--pressed", link.ClassList);
	}

	[Theory]
	[InlineData(MokaPressedButtonLook.Tint, "tint")]
	[InlineData(MokaPressedButtonLook.Raised, "raised")]
	[InlineData(MokaPressedButtonLook.AccentIcon, "accent-icon")]
	[InlineData(MokaPressedButtonLook.Ring, "ring")]
	[InlineData(MokaPressedButtonLook.Bar, "bar")]
	public void PressedLook_IsWrittenOnTheButton(MokaPressedButtonLook look, string name) =>
		Assert.Equal(name, Render<MokaButton>(p => p.Add(x => x.Quiet, true).Add(x => x.Pressed, true).Add(x => x.PressedLook, look))
			.Find("button").GetAttribute("data-moka-pressed-look"));

	[Theory]
	[InlineData(MokaPressedButtonFill.Tinted, "tinted")]
	[InlineData(MokaPressedButtonFill.Solid, "solid")]
	public void PressedFill_IsWrittenOnTheButton(MokaPressedButtonFill fill, string name) =>
		Assert.Equal(name, Render<MokaButton>(p => p.Add(x => x.Pressed, true).Add(x => x.PressedFill, fill).AddChildContent("Pin"))
			.Find("button").GetAttribute("data-moka-pressed-fill"));

	[Fact]
	public void TheThemesPressedChoices_AreClassesOnTheOutermostRootOnly()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Red.WithPressedButtons(MokaPressedButtonLook.Ring).WithPressedButtonFill(MokaPressedButtonFill.Solid))
			.AddChildContent<MokaThemeProvider>(inner => inner
				.Add(x => x.Theme, MokaTheme.Red.WithPressedButtons(MokaPressedButtonLook.Bar))
				.AddChildContent<MokaButton>(button => button.Add(x => x.Pressed, true).AddChildContent("Pin"))));

		IReadOnlyList<IElement> roots = cut.FindAll(".moka-root");
		Assert.Contains("moka-pressed-buttons-ring", roots[0].ClassList);
		Assert.Contains("moka-pressed-button-fill-solid", roots[0].ClassList);
		Assert.DoesNotContain(roots[1].ClassList, c => c.StartsWith("moka-pressed-", StringComparison.Ordinal));
	}

	// Tint and Tinted are the shared rules every pressed button gets, so the defaults write nothing and name no rule.
	[Fact]
	public void TheDefaults_WriteNoClassAndHaveNoRulesOfTheirOwn()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p.Add(x => x.Theme, MokaTheme.Red));

		Assert.DoesNotContain(cut.Find(".moka-root").ClassList, c => c.StartsWith("moka-pressed-", StringComparison.Ordinal));
		Assert.DoesNotContain(PressedChoiceSelectors(), s => s.Contains("tint", StringComparison.Ordinal));
		Assert.Equal(MokaPressedButtonLook.Tint, MokaTheme.Red.PressedButtons);
		Assert.Equal(MokaPressedButtonFill.Tinted, MokaTheme.Red.PressedButtonFill);
	}

	[Fact]
	public void TheOriginalDesign_DrawsTheDefaults()
	{
		Assert.Same(MokaTheme.Dark, MokaTheme.Dark.WithPressedButtons(MokaPressedButtonLook.Raised));
		Assert.Same(MokaTheme.Light, MokaTheme.Light.WithPressedButtonFill(MokaPressedButtonFill.Solid));

		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Dark with { PressedButtons = MokaPressedButtonLook.Raised }));

		Assert.DoesNotContain(cut.Find(".moka-root").ClassList, c => c.StartsWith("moka-pressed-", StringComparison.Ordinal));
		Assert.All(PressedChoiceSelectors(), s => Assert.StartsWith(".moka-gate", s, StringComparison.Ordinal));
		Assert.Equal("var(--_btn-press-bg)", Declarations(".moka-btn--quiet.moka-btn--pressed")["background-color"]);
		Assert.Equal("var(--_btn-press)", Declarations(".moka-btn--outlined.moka-btn--pressed")["border-color"]);
		Assert.Equal("brightness(0.86)", Declarations(".moka-btn--filled.moka-btn--pressed")["filter"]);
	}

	[Fact]
	public void EveryColor_HasItsOwnPressedTone()
	{
		foreach (string tone in PressedTones)
		{
			Dictionary<string, string> declarations = Declarations($".moka-btn--pressed.moka-btn--{tone}");

			Assert.Equal($"var(--moka-color-{tone})", declarations["--_btn-press"]);
			Assert.Equal($"var(--moka-color-{tone}-bg)", declarations["--_btn-press-bg"]);
			Assert.Equal($"var(--moka-color-{tone}-ink)", declarations["--_btn-press-ink"]);
			Assert.Equal($"var(--moka-color-on-{tone})", declarations["--_btn-press-on"]);
		}
	}

	// Forced colors drop fills and shadows; a transparent outline is drawn there in the system color.
	[Fact]
	public void APressedButton_KeepsARingUnderForcedColors() =>
		Assert.Equal("var(--moka-border-width) solid transparent", Declarations(".moka-btn--pressed")["outline"]);

	[Fact]
	public void EveryPressedChoice_HasTheThemesSelectorAndTheButtonsOwnInEveryRule()
	{
		IEnumerable<(string Theme, string Own)> choices = PressedQuietLooks
			.Select(look => ($"moka-pressed-buttons-{look}", $"[data-moka-pressed-look=\"{look}\"]"))
			.Append(("moka-pressed-button-fill-solid", "[data-moka-pressed-fill=\"solid\"]"));

		foreach ((string theme, string own) in choices)
		{
			List<List<string>> rules = [.. Rules().Select(r => r.Selectors)
				.Where(s => s.Any(x => x.Contains(theme, StringComparison.Ordinal) || x.Contains(own, StringComparison.Ordinal)))];

			Assert.True(rules.Count > 0, $"No rules for {theme}.");
			Assert.All(rules, selectors => Assert.Equal(
				selectors.Count(x => x.Contains(theme, StringComparison.Ordinal)),
				selectors.Count(x => x.Contains(own, StringComparison.Ordinal))));
		}
	}

	[Fact]
	public void TheThemesPressedSelectors_SkipAButtonWithAChoiceOfItsOwn()
	{
		List<string> look = [.. PressedChoiceSelectors().Where(s => s.Contains("moka-pressed-buttons-", StringComparison.Ordinal))];
		List<string> fill = [.. PressedChoiceSelectors().Where(s => s.Contains("moka-pressed-button-fill-", StringComparison.Ordinal))];

		Assert.NotEmpty(look);
		Assert.NotEmpty(fill);
		Assert.All(look, s => Assert.Contains(":not([data-moka-pressed-look])", s, StringComparison.Ordinal));
		Assert.All(fill, s => Assert.Contains(":not([data-moka-pressed-fill])", s, StringComparison.Ordinal));
	}

	// After every button look's rule, so a pressed button takes the pressed colors under any look, and before the
	// disabled looks, so a disabled toggle reads as unavailable first.
	[Fact]
	public void ThePressedRules_ComeAfterTheButtonLooks_AndBeforeTheDisabledLooks()
	{
		List<List<string>> rules = [.. Rules().Select(r => r.Selectors)];
		int lastLook = rules.FindLastIndex(s => s.Any(x => x.Contains("data-moka-look", StringComparison.Ordinal)));
		int firstGatePressed = rules.FindIndex(s => s.Any(x => x.StartsWith(".moka-gate", StringComparison.Ordinal) && x.Contains("moka-btn--pressed", StringComparison.Ordinal)));
		int lastPressed = rules.FindLastIndex(s => s.Any(x => x.Contains("moka-btn--pressed", StringComparison.Ordinal)));
		int firstDisabled = rules.FindIndex(s => s.Any(x => x.Contains("disabled-look", StringComparison.Ordinal)));

		Assert.True(firstGatePressed > lastLook, $"A pressed rule ({firstGatePressed}) comes before a look rule ({lastLook}).");
		Assert.True(lastPressed < firstDisabled, $"A pressed rule ({lastPressed}) comes after a disabled look rule ({firstDisabled}).");
	}

	[Fact]
	public void Solid_LeavesADisabledToggleToTheDisabledLooks()
	{
		List<string> resting = [.. PressedChoiceSelectors()
			.Where(s => s.Contains("solid", StringComparison.Ordinal) && !s.Contains(":hover", StringComparison.Ordinal))];

		Assert.NotEmpty(resting);
		Assert.All(resting, s => Assert.Contains(":not(.moka-btn--disabled)", s, StringComparison.Ordinal));
	}

	[Fact]
	public void Bar_DrawsABarInTheToneUnderTheIcon()
	{
		Dictionary<string, string> bar = Declarations(".moka-gate .moka-btn.moka-btn--pressed[data-moka-pressed-look=\"bar\"].moka-btn--quiet::after");

		Assert.Equal("\"\"", bar["content"]);
		Assert.Equal("absolute", bar["position"]);
		Assert.Equal("var(--_btn-press)", bar["background-color"]);
	}

	private static List<string> PressedChoiceSelectors() =>
		[.. Selectors().Where(s => s.Contains("data-moka-pressed-", StringComparison.Ordinal) || s.Contains(".moka-pressed-", StringComparison.Ordinal))];
}
