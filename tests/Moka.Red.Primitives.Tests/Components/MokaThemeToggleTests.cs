using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;
using Moka.Red.Primitives.Utility;
using Moka.Red.Tests.Shared;
using Moka.Red.ThemeGen.Appearance;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaThemeToggleTests : BunitContext
{
	[Fact]
	public async Task Click_FlipsTheMode_AndReportsIt()
	{
		bool? reported = null;
		IRenderedComponent<MokaThemeToggle> cut = Render<MokaThemeToggle>(p => p
			.Add(x => x.IsDark, false)
			.Add(x => x.IsDarkChanged, EventCallback.Factory.Create<bool>(this, v => reported = v)));

		await cut.Find("button").ClickAsync(new MouseEventArgs());

		Assert.True(reported);
		Assert.Contains("moka-theme-toggle--dark", cut.Find("button").ClassList);
	}

	// The inherited Disabled parameter used to do nothing: the button stayed enabled and switched.
	[Fact]
	public async Task Disabled_DisablesTheButton_AndIgnoresClicks()
	{
		bool? reported = null;
		IRenderedComponent<MokaThemeToggle> cut = Render<MokaThemeToggle>(p => p
			.Add(x => x.IsDark, false)
			.Add(x => x.Disabled, true)
			.Add(x => x.IsDarkChanged, EventCallback.Factory.Create<bool>(this, v => reported = v)));

		IElement button = cut.Find("button");
		await button.ClickAsync(new MouseEventArgs());

		Assert.True(button.HasAttribute("disabled"));
		Assert.Null(reported);
		Assert.DoesNotContain("moka-theme-toggle--dark", cut.Find("button").ClassList);
		Assert.Equal("Switch to dark mode", cut.Find("button").GetAttribute("aria-label"));
	}

	private IRenderedComponent<MokaThemeToggle> RenderToggle(MokaThemeToggleLook look, bool system, Action<ComponentParameterCollectionBuilder<MokaThemeToggle>>? extra = null) =>
		Render<MokaThemeToggle>(p =>
		{
			p.Add(x => x.Look, look);
			p.Add(x => x.ShowSystem, system);
			extra?.Invoke(p);
		});

	private static void PickRadio(IRenderedComponent<MokaThemeToggle> cut, string name) =>
		cut.Find($"input[type=radio][aria-label='{name}']").Change(true);

	[Fact]
	public void WithNothingNew_TheMarkupIsTheRoundButton()
	{
		IRenderedComponent<MokaThemeToggle> cut = Render<MokaThemeToggle>(p => p.Add(x => x.IsDark, true));

		IElement button = cut.Find("button");
		Assert.Equal("moka-theme-toggle moka-theme-toggle--dark", button.ClassName);
		Assert.Equal("button", button.GetAttribute("type"));
		Assert.False(button.HasAttribute("role"));
		Assert.Empty(cut.FindAll("input"));
		Assert.Contains("18px", button.QuerySelector("svg")!.OuterHtml, StringComparison.Ordinal);
	}

	[Fact]
	public void TheDefaultLook_IsTheIcon() =>
		Assert.Equal(MokaThemeToggleLook.Icon, Render<MokaThemeToggle>().Instance.Look);

	[Fact]
	public void TheColorMode_LivesInCore_UnderItsOldNamespace()
	{
		Assert.Equal(typeof(MokaTheme).Assembly, typeof(MokaColorMode).Assembly);
		Assert.Equal("Moka.Red.ThemeGen.Appearance", typeof(MokaColorMode).Namespace);
	}

	[Fact]
	public async Task TheIcon_WithThreeStates_CyclesAndShowsTheCurrentOne()
	{
		List<MokaColorMode> modes = [];
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(MokaThemeToggleLook.Icon, true, p => p.Add(x => x.ModeChanged, modes.Add));
		IconTooltip.AssertNamed(cut.Find("button"), "Switch to dark mode", MokaTooltipPosition.Bottom);

		await cut.Find("button").ClickAsync(new MouseEventArgs());
		IconTooltip.AssertNamed(cut.Find("button"), "Follow this device", MokaTooltipPosition.Bottom);
		await cut.Find("button").ClickAsync(new MouseEventArgs());
		IconTooltip.AssertNamed(cut.Find("button"), "Switch to light mode", MokaTooltipPosition.Bottom);
		Assert.Contains(MokaIcons.Content.Monitor.SvgPath, cut.Find("button").InnerHtml, StringComparison.Ordinal);
		await cut.Find("button").ClickAsync(new MouseEventArgs());

		Assert.Equal([MokaColorMode.Dark, MokaColorMode.System, MokaColorMode.Light], modes);
	}

	[Fact]
	public async Task System_DoesNotReportADarkOrLightChange()
	{
		List<bool> dark = [];
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(MokaThemeToggleLook.Icon, true, p => p.Add(x => x.IsDarkChanged, dark.Add));

		await cut.Find("button").ClickAsync(new MouseEventArgs());
		await cut.Find("button").ClickAsync(new MouseEventArgs());

		Assert.Equal([true], dark);
	}

	[Fact]
	public void TheSwitch_WithTwoStates_IsASwitch()
	{
		List<bool> dark = [];
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(MokaThemeToggleLook.Switch, false, p => p.Add(x => x.IsDarkChanged, dark.Add));

		IElement button = cut.Find("button[role=switch]");
		Assert.Equal("false", button.GetAttribute("aria-checked"));
		IconTooltip.AssertNamed(button, "Dark mode", MokaTooltipPosition.Bottom);
		Assert.Contains("moka-theme-toggle-switch--at-0", button.ClassList);

		button.Click();

		Assert.Equal([true], dark);
		IElement after = cut.Find("button[role=switch]");
		Assert.Equal("true", after.GetAttribute("aria-checked"));
		Assert.Contains("moka-theme-toggle-switch--at-1", after.ClassList);
		Assert.DoesNotContain("moka-theme-toggle", after.ClassList);
	}

	[Fact]
	public void TheSwitch_WithThreeStates_IsARadioGroup()
	{
		MokaColorMode? picked = null;
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(MokaThemeToggleLook.Switch, true, p => p.Add(x => x.ModeChanged, m => picked = m));

		IElement group = cut.Find("[role=radiogroup]");
		Assert.Equal("Color mode", group.GetAttribute("aria-label"));
		Assert.Contains("moka-theme-toggle-switch--three", group.ClassList);
		Assert.Empty(cut.FindAll("[role=switch]"));
		IReadOnlyList<IElement> radios = cut.FindAll("input[type=radio]");
		Assert.Equal(3, radios.Count);
		Assert.Single(radios.Select(radio => radio.GetAttribute("name")).Distinct());
		IconTooltip.AssertNamed(radios[2], "System", MokaTooltipPosition.Bottom);

		PickRadio(cut, "System");

		Assert.Equal(MokaColorMode.System, picked);
		Assert.Contains("moka-theme-toggle-switch--at-2", cut.Find("[role=radiogroup]").ClassList);
		Assert.True(cut.Find("input[aria-label='System']").HasAttribute("checked"));
	}

	[Theory]
	[InlineData(false, 2)]
	[InlineData(true, 3)]
	public void TheSegmentedLook_IsRadiosNamedAndInTooltips(bool system, int count)
	{
		MokaColorMode? picked = null;
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(MokaThemeToggleLook.Segmented, system, p => p.Add(x => x.ModeChanged, m => picked = m));

		Assert.Equal("Color mode", cut.Find(".moka-segmented[role=radiogroup]").GetAttribute("aria-label"));
		IReadOnlyList<IElement> radios = cut.FindAll("input[type=radio]");
		Assert.Equal(count, radios.Count);
		Assert.All(radios, radio => Assert.NotNull(radio.Closest(".moka-tooltip-trigger")));
		IconTooltip.AssertNamed(radios[1], "Dark", MokaTooltipPosition.Bottom);
		Assert.Empty(cut.FindAll(".moka-segment__text"));

		PickRadio(cut, "Dark");

		Assert.Equal(MokaColorMode.Dark, picked);
	}

	[Theory]
	[InlineData(false, 2)]
	[InlineData(true, 3)]
	public void IconsAndWords_ShowBoth(bool system, int count)
	{
		string[] all = ["Light", "Dark", "System"];
		string[] words = all[..count];
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(MokaThemeToggleLook.IconsAndWords, system);

		Assert.Equal(words, cut.FindAll(".moka-segment__text").Select(word => word.TextContent));
		Assert.Equal(words.Length, cut.FindAll(".moka-segment__icon").Count);
	}

	[Fact]
	public async Task TheChip_ShowsTheCurrentState_AndMovesOn()
	{
		List<MokaColorMode> modes = [];
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(MokaThemeToggleLook.Chip, true, p => p
			.Add(x => x.Mode, MokaColorMode.Dark)
			.Add(x => x.ModeChanged, modes.Add));

		IElement chip = cut.Find("button.moka-theme-toggle-chip");
		Assert.Equal("Dark", chip.QuerySelector(".moka-theme-toggle-chip__word")!.TextContent);
		Assert.Equal("Dark. Follow this device", chip.GetAttribute("aria-label"));

		await chip.ClickAsync(new MouseEventArgs());

		Assert.Equal([MokaColorMode.System], modes);
		Assert.Equal("System", cut.Find(".moka-theme-toggle-chip__word").TextContent);
	}

	[Fact]
	public async Task TheChip_WithTwoStates_Flips()
	{
		bool? dark = null;
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(MokaThemeToggleLook.Chip, false, p => p.Add(x => x.IsDarkChanged, value => dark = value));

		await cut.Find("button").ClickAsync(new MouseEventArgs());

		Assert.True(dark);
		Assert.Equal("Dark", cut.Find(".moka-theme-toggle-chip__word").TextContent);
	}

	[Theory]
	[InlineData(MokaThemeToggleLook.Icon, MokaSize.Lg, "moka-theme-toggle--lg")]
	[InlineData(MokaThemeToggleLook.Icon, MokaSize.Sm, "moka-theme-toggle--sm")]
	[InlineData(MokaThemeToggleLook.Switch, MokaSize.Xs, "moka-theme-toggle-switch--xs")]
	[InlineData(MokaThemeToggleLook.Chip, MokaSize.Lg, "moka-theme-toggle-chip--lg")]
	[InlineData(MokaThemeToggleLook.Segmented, MokaSize.Lg, "moka-segmented--lg")]
	[InlineData(MokaThemeToggleLook.IconsAndWords, MokaSize.Sm, "moka-segmented--sm")]
	public void Size_ReachesEveryLook(MokaThemeToggleLook look, MokaSize size, string expected)
	{
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(look, false, p => p.Add(x => x.Size, size));

		Assert.Contains(expected, cut.Nodes.OfType<IElement>().First().QuerySelectorAll("*").Append(cut.Nodes.OfType<IElement>().First()).SelectMany(e => e.ClassList));
	}

	[Theory]
	[InlineData(MokaThemeToggleLook.Switch, false)]
	[InlineData(MokaThemeToggleLook.Chip, false)]
	[InlineData(MokaThemeToggleLook.Chip, true)]
	public async Task Disabled_StopsAButtonLook(MokaThemeToggleLook look, bool system)
	{
		bool raised = false;
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(look, system, p => p
			.Add(x => x.Disabled, true)
			.Add(x => x.ModeChanged, _ => raised = true));

		await cut.Find("button").ClickAsync(new MouseEventArgs());

		Assert.True(cut.Find("button").HasAttribute("disabled"));
		Assert.False(raised);
	}

	[Theory]
	[InlineData(MokaThemeToggleLook.Switch)]
	[InlineData(MokaThemeToggleLook.Segmented)]
	[InlineData(MokaThemeToggleLook.IconsAndWords)]
	public void Disabled_DisablesEveryRadio(MokaThemeToggleLook look)
	{
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(look, true, p => p.Add(x => x.Disabled, true));

		Assert.All(cut.FindAll("input[type=radio]"), radio => Assert.True(radio.HasAttribute("disabled")));
	}

	[Fact]
	public void EveryWord_CanBeReplaced()
	{
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(MokaThemeToggleLook.IconsAndWords, true, p => p
			.Add(x => x.LightText, "Ljust")
			.Add(x => x.DarkText, "Mörkt")
			.Add(x => x.SystemText, "Enhet")
			.Add(x => x.GroupLabel, "Färgläge"));

		Assert.Equal(["Ljust", "Mörkt", "Enhet"], cut.FindAll(".moka-segment__text").Select(word => word.TextContent));
		Assert.Equal("Färgläge", cut.Find("[role=radiogroup]").GetAttribute("aria-label"));
	}

	[Fact]
	public async Task SystemModeLabel_NamesTheStepToSystem()
	{
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(MokaThemeToggleLook.Icon, true, p => p
			.Add(x => x.Mode, MokaColorMode.Dark)
			.Add(x => x.SystemModeLabel, "Följ enheten"));

		IconTooltip.AssertNamed(cut.Find("button"), "Följ enheten", MokaTooltipPosition.Bottom);
		await cut.Find("button").ClickAsync(new MouseEventArgs());
		IconTooltip.AssertNamed(cut.Find("button"), "Switch to light mode", MokaTooltipPosition.Bottom);
	}

	[Fact]
	public void SwitchLabel_NamesTheSwitch()
	{
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(MokaThemeToggleLook.Switch, false, p => p.Add(x => x.SwitchLabel, "Mörkt läge"));

		IconTooltip.AssertNamed(cut.Find("[role=switch]"), "Mörkt läge", MokaTooltipPosition.Bottom);
	}

	[Fact]
	public void Mode_WinsOverIsDark()
	{
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(MokaThemeToggleLook.Switch, false, p => p
			.Add(x => x.Mode, MokaColorMode.Light)
			.Add(x => x.IsDark, true));

		Assert.Equal("false", cut.Find("[role=switch]").GetAttribute("aria-checked"));
	}

	[Fact]
	public async Task Mode_KeepsTheClick_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(MokaThemeToggleLook.Chip, true, p => p.Add(x => x.Mode, MokaColorMode.Light));
		await cut.Find("button").ClickAsync(new MouseEventArgs());

		cut.Render(p => p.Add(x => x.Mode, MokaColorMode.Light));

		Assert.Equal("Dark", cut.Find(".moka-theme-toggle-chip__word").TextContent);
	}

	[Fact]
	public void Mode_FollowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(MokaThemeToggleLook.Chip, true, p => p.Add(x => x.Mode, MokaColorMode.Light));

		cut.Render(p => p.Add(x => x.Mode, MokaColorMode.System));

		Assert.Equal("System", cut.Find(".moka-theme-toggle-chip__word").TextContent);
	}

	[Fact]
	public void System_WithoutAThirdState_ShowsWhatThePageIsShowing()
	{
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(MokaThemeToggleLook.Switch, false, p => p
			.Add(x => x.Mode, MokaColorMode.System)
			.Add(x => x.IsDark, true));

		Assert.Equal("true", cut.Find("[role=switch]").GetAttribute("aria-checked"));
	}

	[Fact]
	public async Task System_StaysPicked_WhenTheDeviceChangesScheme()
	{
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(MokaThemeToggleLook.Chip, true, p => p.Add(x => x.IsDark, true));
		await cut.Find("button").ClickAsync(new MouseEventArgs());
		Assert.Equal("System", cut.Find(".moka-theme-toggle-chip__word").TextContent);

		cut.Render(p => p.Add(x => x.IsDark, false));

		Assert.Equal("System", cut.Find(".moka-theme-toggle-chip__word").TextContent);
	}

	[Theory]
	[InlineData(MokaThemeToggleLook.Switch, true)]
	[InlineData(MokaThemeToggleLook.Switch, false)]
	[InlineData(MokaThemeToggleLook.Segmented, false)]
	[InlineData(MokaThemeToggleLook.Chip, false)]
	public void ClassIdStyleAndAttributes_LandOnTheRoot(MokaThemeToggleLook look, bool system)
	{
		IRenderedComponent<MokaThemeToggle> cut = RenderToggle(look, system, p => p
			.Add(x => x.Class, "mine")
			.Add(x => x.Id, "theme")
			.Add(x => x.Style, "color: red")
			.AddUnmatched("data-test", "1"));

		IElement root = cut.Find("#theme");
		Assert.Contains("mine", root.ClassList);
		Assert.Equal("1", root.GetAttribute("data-test"));
		Assert.Contains("color: red", root.GetAttribute("style"), StringComparison.Ordinal);
	}
}
