using System.Numerics;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.ThemeGen.Appearance;
using Moka.Red.Tests.Shared;

namespace Moka.Red.ThemeGen.Tests.Components;

// The component look rows: toasts, toast stack, notifications, popups, lists, dialogs, buttons, the four tooltip rows,
// and the component style row that sets six of them at once.
public sealed partial class MokaAppearancePanelTests
{
	private const string PanelCss = "MokaAppearancePanel.razor.css";

	private const MokaAppearanceRows LookRows = MokaAppearanceRows.ComponentStyle | MokaAppearanceRows.Components;

	[Fact]
	public void TheComponentRows_AreLeftOutOfDefaultAndStyle()
	{
		Assert.Equal(MokaAppearanceRows.None, MokaAppearanceRows.Default & LookRows);
		Assert.Equal(MokaAppearanceRows.None, MokaAppearanceRows.Style & LookRows);
		Assert.Equal(LookRows, MokaAppearanceRows.All & LookRows);
	}

	[Fact]
	public void Components_IsTheElevenLookRows() =>
		Assert.Equal(11, BitOperations.PopCount((uint)MokaAppearanceRows.Components));

	[Fact]
	public void TheComponentRows_HaveAHeadingOfTheirOwn()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.All));

		Assert.Equal(["Colors", "Style", "Components", "Charts"], cut.FindAll(".moka-appearance-panel__heading").Select(heading => heading.TextContent));
	}

	[Fact]
	public void OnlyComponentRows_NeedNoHeading()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, LookRows));

		Assert.Equal(ComponentGroups, GroupNames(cut));
		Assert.Empty(cut.FindAll(".moka-appearance-panel__heading"));
	}

	[Fact]
	public void PickingAComponentStyle_SetsTheSixLooksItCovers()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, LookRows)
			.Add(x => x.ValueChanged, v => raised = v));

		Pick(cut, "Component style", "Soft");

		Assert.Equal(new MokaAppearance
		{
			Toasts = MokaToastLook.IconTile,
			Notifications = MokaNotificationLook.Inbox,
			Popups = MokaPopupLook.Elevated,
			Lists = MokaListLook.Check,
			Dialogs = MokaDialogLook.IconLead,
			Buttons = MokaButtonLook.Soft
		}, raised);
		Assert.Equal("Soft", CheckedOption(cut, "Component style"));
		Assert.Equal("Icon tile", CheckedOption(cut, "Toasts"));
		Assert.Equal("Elevated", CheckedOption(cut, "Popups"));
		Assert.Equal("List", CheckedOption(cut, "Toast stack"));
	}

	[Fact]
	public void PickingAComponentStyle_LeavesTheToastStackAlone()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, LookRows)
			.Add(x => x.Value, new MokaAppearance { ToastStacking = MokaToastStacking.Pile })
			.Add(x => x.ValueChanged, v => raised = v));

		Pick(cut, "Component style", "Terminal");

		Assert.Equal(MokaToastStacking.Pile, raised!.ToastStacking);
		Assert.Equal(MokaButtonLook.Glow, raised.Buttons);
	}

	[Fact]
	public void FineTuningOneLook_LeavesNoComponentStyleCurrent()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, LookRows)
			.Add(x => x.Value, new MokaAppearance().WithComponentStyle(MokaComponentStyle.Terminal))
			.Add(x => x.ValueChanged, v => raised = v));
		Assert.Equal("Terminal", CheckedOption(cut, "Component style"));

		Pick(cut, "Toasts", "Pill");

		Assert.Equal(MokaToastLook.Pill, raised!.Toasts);
		Assert.Equal(MokaPopupLook.Terminal, raised.Popups);
		Assert.Null(CheckedOption(cut, "Component style"));
	}

	[Fact]
	public void TheComponentRows_HideOnTheOriginalDesign_AndSayWhy()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.Palette | LookRows)
			.Add(x => x.Value, new MokaAppearance { Palette = "original", Toasts = MokaToastLook.Pill }));

		Assert.Equal(["Palette"], GroupNames(cut));
		Assert.Contains("keeps its own style", cut.Find("p.moka-appearance-panel__said").TextContent, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll(".moka-appearance-panel__look"));
	}

	[Fact]
	public void EachComponentRow_DrawsEveryLookAsItsOwnPicture()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, LookRows));

		AssertPictures<MokaComponentStyle>(cut, "Component style", "pack");
		AssertPictures<MokaToastLook>(cut, "Toasts", "toasts");
		AssertPictures<MokaToastStacking>(cut, "Toast stack", "stacking");
		AssertPictures<MokaNotificationLook>(cut, "Notifications", "notifications");
		AssertPictures<MokaPopupLook>(cut, "Popups", "popups");
		AssertPictures<MokaListLook>(cut, "Lists", "lists");
		AssertPictures<MokaDialogLook>(cut, "Dialogs", "dialogs");
		AssertPictures<MokaButtonLook>(cut, "Buttons", "buttons");
		AssertPictures<MokaTooltipLook>(cut, "Tooltips", "tooltips");
	}

	[Fact]
	public void TheTooltipPictures_CarryTheRealTooltipClasses()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.Tooltips));

		IReadOnlyList<IElement> tips = cut.FindAll("[aria-label='Tooltips'] .moka-appearance-panel__tip");

		Assert.Equal(
			Enum.GetValues<MokaTooltipLook>().Select(look => $"moka-tooltips-{MokaEnumHelpers.ToCssClass(look)}"),
			tips.Select(tip => tip.ClassList.Single(name => name.StartsWith("moka-tooltips-", StringComparison.Ordinal))));
	}

	[Fact]
	public void TheTooltipPicture_ReadsEachLookVariable_WithTheStandardValueBehindIt()
	{
		IReadOnlyDictionary<string, string> tip = ScopedCss.Declarations(PanelCss, ".moka-appearance-panel__tip");

		Assert.Equal("var(--moka-tooltip-look-bg, var(--moka-color-surface-variant))", tip["background"]);
		Assert.Equal("var(--moka-tooltip-look-color, var(--moka-color-on-surface))", tip["color"]);
		Assert.Contains("var(--moka-tooltip-look-border, var(--moka-color-outline-strong))", tip["border"], StringComparison.Ordinal);
		Assert.Contains("var(--moka-tooltip-look-radius, var(--moka-radius-md))", tip["border-radius"], StringComparison.Ordinal);
		Assert.Equal("var(--moka-tooltip-look-shadow, var(--moka-shadow-popup))", tip["box-shadow"]);
		Assert.Equal("var(--moka-tooltip-look-blur, none)", tip["backdrop-filter"]);
	}

	// Arrows, motion and size are plain segments: a picture can't show a motion, and the other two read clearly in words.
	[Fact]
	public void TheOtherTooltipRows_AreSegments()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.Components));

		Assert.Equal(["None", "Shown"], OptionsIn(cut, "Tooltip arrows"));
		Assert.Equal(["Fade", "Slide", "Grow", "Instant"], OptionsIn(cut, "Tooltip motion"));
		Assert.Equal(["Compact", "Standard", "Roomy"], OptionsIn(cut, "Tooltip size"));
		Assert.Equal("None", CheckedOption(cut, "Tooltip arrows"));
		Assert.Equal("Fade", CheckedOption(cut, "Tooltip motion"));
		Assert.Equal("Standard", CheckedOption(cut, "Tooltip size"));
		Assert.Empty(cut.FindAll("[aria-label='Tooltip motion'] .moka-appearance-panel__look"));
	}

	[Fact]
	public void TheTooltipChoices_ReachTheTheme()
	{
		MokaTheme theme = new MokaAppearance
		{
			Palette = "gate",
			Tooltips = MokaTooltipLook.Terminal,
			TooltipArrows = true,
			TooltipMotion = MokaTooltipMotion.Grow,
			TooltipSize = MokaTooltipSize.Compact
		}.ToTheme(isDark: true);

		Assert.Equal(MokaTooltipLook.Terminal, theme.Tooltips);
		Assert.True(theme.TooltipArrows);
		Assert.Equal(MokaTooltipMotion.Grow, theme.TooltipMotion);
		Assert.Equal(MokaTooltipSize.Compact, theme.TooltipSize);
	}

	// A component style sets six looks and leaves the tooltips as they were.
	[Fact]
	public void AComponentStyle_LeavesTheTooltipsAlone()
	{
		MokaAppearance value = new MokaAppearance { Tooltips = MokaTooltipLook.Flat, TooltipArrows = true }.WithComponentStyle(MokaComponentStyle.Terminal);

		Assert.Equal(MokaTooltipLook.Flat, value.Tooltips);
		Assert.True(value.TooltipArrows);
	}

	[Fact]
	public void ThePopupPictures_CarryTheRealPopupClasses()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.Popups));

		IReadOnlyList<IElement> menus = cut.FindAll("[aria-label='Popups'] .moka-appearance-panel__menu");

		Assert.Equal(
			Enum.GetValues<MokaPopupLook>().Select(look => $"moka-popups-{MokaEnumHelpers.ToCssClass(look)}"),
			menus.Select(menu => menu.ClassList.Single(name => name.StartsWith("moka-popups-", StringComparison.Ordinal))));
	}

	[Theory]
	[InlineData("Standard", "moka-appearance-panel__toasts--standard", "moka-appearance-panel__buttons--standard")]
	[InlineData("Soft", "moka-appearance-panel__toasts--icon-tile", "moka-appearance-panel__buttons--soft")]
	[InlineData("Terminal", "moka-appearance-panel__toasts--terminal", "moka-appearance-panel__buttons--glow")]
	public void AComponentStylePicture_DrawsTheToastAndButtonsItPicks(string style, string toasts, string buttons)
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.ComponentStyle));

		IElement card = cut.FindAll("[aria-label='Component style'] label").Single(label => TextOf(label) == style);

		Assert.NotNull(card.QuerySelector($".{toasts} .moka-appearance-panel__toast"));
		Assert.NotNull(card.QuerySelector($".{buttons} .moka-appearance-panel__button--primary"));
	}

	[Fact]
	public void Compact_DrawsTheComponentRowsAsSegments()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Compact, true)
			.Add(x => x.Rows, LookRows)
			.Add(x => x.ValueChanged, v => raised = v));

		Assert.Empty(cut.FindAll(".moka-appearance-panel__look"));
		Assert.Equal(ComponentGroups, GroupNames(cut));
		Assert.Equal("Standard", CheckedOption(cut, "Buttons"));

		Pick(cut, "Buttons", "Raised");

		Assert.Equal(MokaButtonLook.Raised, raised!.Buttons);
	}

	[Fact]
	public void AComponentRow_RespectsOptionState()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, LookRows)
			.Add(x => x.OptionState, option => option.Value switch
			{
				MokaToastLook.Terminal => MokaAppearanceOptionState.Hidden,
				MokaPopupLook.Glass => MokaAppearanceOptionState.Disabled,
				MokaComponentStyle.Soft => MokaAppearanceOptionState.Hidden,
				_ => MokaAppearanceOptionState.Available
			}));

		Assert.DoesNotContain("Terminal", OptionsIn(cut, "Toasts"));
		Assert.Equal(["Glass"], DisabledIn(cut, "Popups"));
		Assert.Equal(["Standard", "Terminal"], OptionsIn(cut, "Component style"));
	}

	[Fact]
	public void TheQuestion_NamesAComponentLookByItsRowAndChoice()
	{
		List<MokaAppearanceOption> asked = [];
		RenderPanel(p => p
			.Add(x => x.Rows, LookRows)
			.Add(x => x.OptionState, option =>
			{
				asked.Add(option);
				return MokaAppearanceOptionState.Available;
			}));

		Assert.Contains(new MokaAppearanceOption(MokaAppearanceRows.ComponentStyle, MokaComponentStyle.Soft), asked);
		Assert.Contains(new MokaAppearanceOption(MokaAppearanceRows.ToastStacking, MokaToastStacking.Pile), asked);
		Assert.Contains(new MokaAppearanceOption(MokaAppearanceRows.Buttons, MokaButtonLook.Gradient), asked);
	}

	[Fact]
	public void TheAppsOwnRows_ComeAfterTheComponentRows()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.Density | MokaAppearanceRows.Toasts)
			.AddChildContent<MokaAppearanceRow>(row => row.Add(x => x.Label, "Sidebar")));

		IReadOnlyList<IElement> groups = cut.FindAll(".moka-appearance-panel__group");
		Assert.Equal(2, groups.Count);
		Assert.Null(groups[0].QuerySelector("[aria-label='Toasts']"));
		Assert.NotNull(groups[1].QuerySelector("[aria-label='Toasts']"));
		Assert.Equal("Sidebar", groups[1].QuerySelector(".moka-appearance-panel__rows .moka-micro-label")!.TextContent);
		Assert.DoesNotContain("Sidebar", groups[0].TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void ThePopupPicture_ReadsEachPopupVariable_WithTheStandardValueBehindIt()
	{
		IReadOnlyDictionary<string, string> menu = ScopedCss.Declarations(PanelCss, ".moka-appearance-panel__menu");

		Assert.Contains("var(--moka-popup-bg, var(--moka-color-surface))", menu["background"], StringComparison.Ordinal);
		Assert.Contains("var(--moka-popup-color, ", menu["color"], StringComparison.Ordinal);
		Assert.Contains("var(--moka-popup-border, ", menu["border"], StringComparison.Ordinal);
		Assert.Contains("var(--moka-popup-radius, ", menu["border-radius"], StringComparison.Ordinal);
		Assert.Contains("var(--moka-popup-shadow, ", menu["box-shadow"], StringComparison.Ordinal);
		Assert.Equal("var(--moka-popup-blur, none)", menu["backdrop-filter"]);

		IReadOnlyDictionary<string, string> lit = ScopedCss.Declarations(PanelCss, ".moka-appearance-panel__item--lit");
		Assert.Contains("var(--moka-popup-row-hover, ", lit["background"], StringComparison.Ordinal);
		Assert.Contains("var(--moka-popup-row-hover-shadow, none)", lit["box-shadow"], StringComparison.Ordinal);
	}

	// A picture that drew two looks the same would ask the person to choose between two identical cards.
	[Fact]
	public void EveryLookButStandard_HasRulesOfItsOwn()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", PanelCss));

		AssertRulesForEachLook<MokaToastLook>(css, "toasts");
		AssertRulesForEachLook<MokaToastStacking>(css, "stacking");
		AssertRulesForEachLook<MokaNotificationLook>(css, "notifications");
		AssertRulesForEachLook<MokaListLook>(css, "lists");
		AssertRulesForEachLook<MokaDialogLook>(css, "dialogs");
		AssertRulesForEachLook<MokaButtonLook>(css, "buttons");
	}

	[Fact]
	public void ThePanelsStylesheet_ReadsTokensForEveryColor() =>
		Assert.DoesNotMatch(LiteralColor(), File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", PanelCss)));

	private static void AssertPictures<TLook>(IRenderedComponent<MokaAppearancePanel> cut, string group, string row) where TLook : struct, Enum
	{
		IReadOnlyList<IElement> cards = cut.FindAll($"[role='radiogroup'][aria-label='{group}'] label");
		TLook[] looks = Enum.GetValues<TLook>();

		Assert.Equal(looks.Length, cards.Count);
		for (int i = 0; i < looks.Length; i++)
		{
			Assert.NotNull(cards[i].QuerySelector($".moka-radio-item__art .moka-appearance-panel__{row}--{MokaEnumHelpers.ToCssClass(looks[i])}"));
		}
	}

	// The first value of each look is the plain drawing every other look starts from.
	private static void AssertRulesForEachLook<TLook>(string css, string row) where TLook : struct, Enum
	{
		TLook[] looks = Enum.GetValues<TLook>();
		Assert.DoesNotContain($".moka-appearance-panel__{row}--{MokaEnumHelpers.ToCssClass(looks[0])}", css, StringComparison.Ordinal);
		foreach (TLook look in looks.Skip(1))
		{
			Assert.Contains($".moka-appearance-panel__{row}--{MokaEnumHelpers.ToCssClass(look)}", css, StringComparison.Ordinal);
		}
	}

	[GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b|rgba?\(")]
	private static partial Regex LiteralColor();
}
