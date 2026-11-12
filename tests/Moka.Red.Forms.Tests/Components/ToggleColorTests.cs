using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Forms.Checkbox;
using Moka.Red.Forms.Switch;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// Up to 0.4.2 MokaSwitch and MokaCheckbox ignored Color: the checked track, the box and the focus ring were always
// the primary color. The switch's checkbox also had no switch role, so screen readers said "checked" for "on".
public class ToggleColorTests : BunitContext
{
	private const string SwitchCss = "MokaSwitch.razor.css";
	private const string CheckboxCss = "MokaCheckbox.razor.css";

	// Color, class, the switch's track, the checkbox's box and its check.
	public static TheoryData<MokaColor, string, string, string, string> Tones => new()
	{
		{ MokaColor.Primary, "primary", "var(--moka-color-primary)", "var(--moka-color-primary)", "var(--moka-color-on-primary)" },
		{ MokaColor.Secondary, "secondary", "var(--moka-color-secondary)", "var(--moka-color-secondary)", "var(--moka-color-on-secondary)" },
		{ MokaColor.Error, "error", "var(--moka-color-error)", "var(--moka-color-error)", "var(--moka-color-on-error)" },
		{ MokaColor.Warning, "warning", "var(--moka-color-warning)", "var(--moka-color-warning)", "var(--moka-color-on-warning)" },
		{ MokaColor.Success, "success", "var(--moka-color-success)", "var(--moka-color-success)", "var(--moka-color-on-success)" },
		{ MokaColor.Info, "info", "var(--moka-color-info)", "var(--moka-color-info)", "var(--moka-color-on-info)" },
		{ MokaColor.Surface, "surface", "var(--moka-color-on-surface-variant)", "var(--moka-color-on-surface)", "var(--moka-color-surface)" }
	};

	// ARIA in HTML allows role="switch" on input type=checkbox and forbids aria-checked there: the checked state is
	// what the browser exposes as the switch's on or off.
	[Fact]
	public void Switch_IsACheckboxWithTheSwitchRole()
	{
		IRenderedComponent<MokaSwitch> cut = Render<MokaSwitch>(p => p.Add(x => x.Value, true));

		IElement input = cut.Find("input");
		Assert.Equal("checkbox", input.GetAttribute("type"));
		Assert.Equal("switch", input.GetAttribute("role"));
		Assert.False(input.HasAttribute("aria-checked"));
		Assert.True(input.HasAttribute("checked"));
	}

	[Fact]
	public void Switch_AnAppsRoleStillWins()
	{
		IRenderedComponent<MokaSwitch> cut = Render<MokaSwitch>(p => p.AddUnmatched("role", "menuitemcheckbox"));

		Assert.Equal("menuitemcheckbox", cut.Find("input").GetAttribute("role"));
	}

	[Fact]
	public void Switch_WithoutColor_WritesNoColorClass()
	{
		IRenderedComponent<MokaSwitch> cut = Render<MokaSwitch>();

		Assert.Equal("moka-switch", cut.Find("label").ClassName);
	}

	[Fact]
	public void Checkbox_WithoutColor_WritesNoColorClass()
	{
		IRenderedComponent<MokaCheckbox> cut = Render<MokaCheckbox>();

		Assert.Equal("moka-checkbox", cut.Find("label").ClassName);
	}

	[Theory]
	[MemberData(nameof(Tones))]
	public void Switch_ColorSetsTheTone(MokaColor color, string name, string track, string box, string check)
	{
		_ = (box, check);
		IRenderedComponent<MokaSwitch> cut = Render<MokaSwitch>(p => p.Add(x => x.Color, color));

		Assert.Contains($"moka-switch--{name}", cut.Find("label").ClassList);
		Assert.Equal(track, ScopedCss.Declarations(SwitchCss, $".moka-switch--{name}")["--_switch-tone"]);
	}

	[Theory]
	[MemberData(nameof(Tones))]
	public void Checkbox_ColorSetsTheToneAndTheCheck(MokaColor color, string name, string track, string box, string check)
	{
		_ = track;
		IRenderedComponent<MokaCheckbox> cut = Render<MokaCheckbox>(p => p.Add(x => x.Color, color));

		Assert.Contains($"moka-checkbox--{name}", cut.Find("label").ClassList);
		IReadOnlyDictionary<string, string> declared = ScopedCss.Declarations(CheckboxCss, $".moka-checkbox--{name}");
		Assert.Equal(box, declared["--_checkbox-tone"]);
		Assert.Equal(check, declared["--_checkbox-ink"]);
	}

	[Fact]
	public void DisplayOnlyCheckbox_TakesTheColorToo()
	{
		IRenderedComponent<MokaCheckbox> cut = Render<MokaCheckbox>(p => p
			.Add(x => x.DisplayOnly, true)
			.Add(x => x.Value, true)
			.Add(x => x.Color, MokaColor.Success));

		Assert.Contains("moka-checkbox--success", cut.Find(".moka-checkbox").ClassList);
	}

	// The fallback is what every theme drew before, so a switch or checkbox without a Color looks the same.
	[Fact]
	public void Switch_TrackAndFocusRing_ReadTheTone()
	{
		Assert.Equal("var(--_switch-tone, var(--moka-color-primary))",
			ScopedCss.Declarations(SwitchCss, ".moka-switch-input:checked + .moka-switch-track")["background"]);
		Assert.Equal("2px solid var(--_switch-tone, var(--moka-color-primary))",
			ScopedCss.Declarations(SwitchCss, ".moka-switch-input:focus-visible + .moka-switch-track")["outline"]);
	}

	[Theory]
	[InlineData(".moka-checkbox-input:checked + .moka-checkbox-box")]
	[InlineData(".moka-checkbox--indeterminate .moka-checkbox-input + .moka-checkbox-box")]
	[InlineData(".moka-checkbox--display-only.moka-checkbox--checked .moka-checkbox-box")]
	public void Checkbox_FilledBox_ReadsTheTone(string selector)
	{
		IReadOnlyDictionary<string, string> box = ScopedCss.Declarations(CheckboxCss, selector);

		Assert.Equal("var(--_checkbox-tone, var(--moka-color-primary))", box["background"]);
		Assert.Equal("var(--_checkbox-tone, var(--moka-color-primary))", box["border-color"]);
		Assert.Equal("var(--_checkbox-ink, var(--moka-color-on-primary))", box["color"]);
	}

	[Fact]
	public void Checkbox_FocusRingAndGateHover_ReadTheTone()
	{
		Assert.Equal("2px solid var(--_checkbox-tone, var(--moka-color-primary))",
			ScopedCss.Declarations(CheckboxCss, ".moka-checkbox-input:focus-visible + .moka-checkbox-box")["outline"]);
		Assert.Equal("var(--_checkbox-tone, var(--moka-color-primary))",
			ScopedCss.Declarations(CheckboxCss,
				".moka-gate .moka-checkbox:hover:not(.moka-checkbox--disabled) .moka-checkbox-input:not(:checked) + .moka-checkbox-box")
				["border-color"]);
	}
}
