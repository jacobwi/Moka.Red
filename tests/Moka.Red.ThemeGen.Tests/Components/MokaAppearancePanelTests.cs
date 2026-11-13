using System.Reflection;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Primitives.Utility;
using Moka.Red.ThemeGen.Appearance;

namespace Moka.Red.ThemeGen.Tests.Components;

public sealed partial class MokaAppearancePanelTests : BunitContext
{
	private static readonly string[] DefaultGroups = ["Color mode", "Palette", "Accent", "Status bar", "Density", "Text size"];

	private static readonly string[] StyleGroups =
	[
		"Status bar", "Density", "Text size", "Corners", "Depth", "Main button", "Text buttons", "Letter case", "Title font", "Page titles", "Numbers",
		"Selected items", "Current page", "Descriptions", "Label colors"
	];

	private static readonly string[] ComponentGroups =
	[
		"Component style", "Toasts", "Toast stack", "Notifications", "Popups", "Lists", "Dialogs", "Buttons", "Tooltips", "Tooltip arrows", "Tooltip motion",
		"Tooltip size", "Disabled buttons", "Callouts", "Switches", "Settings headers"
	];

	public MokaAppearancePanelTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<MokaAppearanceRows, string, string, MokaAppearance> Picks => new()
	{
		{ MokaAppearanceRows.Density, "Density", "Compact", new MokaAppearance { Density = MokaDensity.Compact } },
		{ MokaAppearanceRows.TextSize, "Text size", "Small", new MokaAppearance { TextSize = MokaTextSize.Small } },
		{ MokaAppearanceRows.Corners, "Corners", "Tight", new MokaAppearance { Corners = MokaCorners.Tight } },
		{ MokaAppearanceRows.MainButton, "Main button", "Outline", new MokaAppearance { MainButton = MokaMainButton.Outline } },
		{ MokaAppearanceRows.LetterCase, "Letter case", "Uppercase", new MokaAppearance { LetterCase = MokaLetterCase.Uppercase } },
		{ MokaAppearanceRows.TitleFont, "Title font", "JetBrains Mono", new MokaAppearance { TitleFont = MokaTitleFont.JetBrainsMono } },
		{ MokaAppearanceRows.Selection, "Selected items", "Glow", new MokaAppearance { Selection = MokaSelection.Glow } },
		{ MokaAppearanceRows.CurrentPlace, "Current page", "Solid", new MokaAppearance { CurrentPlace = MokaCurrentPlace.Solid } },
		{ MokaAppearanceRows.StatusBar, "Status bar", "Terminal", new MokaAppearance { StatusBar = MokaStatusBarLook.Terminal } },
		{ MokaAppearanceRows.Descriptions, "Descriptions", "Hidden", new MokaAppearance { Descriptions = false } },
		{ MokaAppearanceRows.LabelTone, "Label colors", "Pastel", new MokaAppearance { LabelTone = MokaLabelTone.Pastel } },
		{ MokaAppearanceRows.Toasts, "Toasts", "Tone edge", new MokaAppearance { Toasts = MokaToastLook.ToneEdge } },
		{ MokaAppearanceRows.ToastStacking, "Toast stack", "Pile", new MokaAppearance { ToastStacking = MokaToastStacking.Pile } },
		{ MokaAppearanceRows.Notifications, "Notifications", "Grouped", new MokaAppearance { Notifications = MokaNotificationLook.Grouped } },
		{ MokaAppearanceRows.Popups, "Popups", "Accent tint", new MokaAppearance { Popups = MokaPopupLook.AccentTint } },
		{ MokaAppearanceRows.Lists, "Lists", "Attached", new MokaAppearance { Lists = MokaListLook.Attached } },
		{ MokaAppearanceRows.Dialogs, "Dialogs", "Icon lead", new MokaAppearance { Dialogs = MokaDialogLook.IconLead } },
		{ MokaAppearanceRows.Buttons, "Buttons", "Ghost", new MokaAppearance { Buttons = MokaButtonLook.Ghost } },
		{ MokaAppearanceRows.DisabledButtons, "Disabled buttons", "Muted", new MokaAppearance { DisabledButtons = MokaDisabledButtonLook.Muted } },
		{ MokaAppearanceRows.Tooltips, "Tooltips", "Glass", new MokaAppearance { Tooltips = MokaTooltipLook.Glass } },
		{ MokaAppearanceRows.TooltipArrows, "Tooltip arrows", "Shown", new MokaAppearance { TooltipArrows = true } },
		{ MokaAppearanceRows.TooltipMotion, "Tooltip motion", "Slide", new MokaAppearance { TooltipMotion = MokaTooltipMotion.Slide } },
		{ MokaAppearanceRows.TooltipSize, "Tooltip size", "Roomy", new MokaAppearance { TooltipSize = MokaTooltipSize.Roomy } },
		{ MokaAppearanceRows.Elevation, "Depth", "Glow", new MokaAppearance { Elevation = MokaElevation.Glow } },
		{ MokaAppearanceRows.TextButtons, "Text buttons", "Neutral", new MokaAppearance { TextButtons = MokaTextButtonColor.Neutral } },
		{ MokaAppearanceRows.TitleSize, "Page titles", "Compact", new MokaAppearance { TitleSize = MokaTitleSize.Compact } },
		{ MokaAppearanceRows.NumberFont, "Numbers", "Mono", new MokaAppearance { NumberFont = MokaNumberFont.Mono } },
		{ MokaAppearanceRows.CurrentPlace, "Current page", "Solid inset", new MokaAppearance { CurrentPlace = MokaCurrentPlace.SolidInset } },
		{ MokaAppearanceRows.Callouts, "Callouts", "Edge", new MokaAppearance { Callouts = MokaCalloutLook.Edge } },
		{ MokaAppearanceRows.Switches, "Switches", "Tinted", new MokaAppearance { Switches = MokaSwitchLook.Tinted } },
		{ MokaAppearanceRows.SettingsHeaders, "Settings headers", "Tinted", new MokaAppearance { SettingsHeaders = MokaSettingsHeaderLook.Tinted } }
	};

	[Fact]
	public void ByDefault_ShowsThe5UsualRows()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel();

		Assert.Equal(DefaultGroups, GroupNames(cut));
		Assert.Single(cut.FindAll("[role='radiogroup'][aria-label='Accent']"));
	}

	[Fact]
	public void TheRowsFlags_PickWhatShows()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.Palette | MokaAppearanceRows.StatusBar));

		Assert.Equal(["Palette", "Status bar"], GroupNames(cut));
		Assert.Empty(cut.FindAll("[role='radiogroup'][aria-label='Accent']"));
	}

	[Fact]
	public void All_ShowsEveryRow()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.All));

		Assert.Equal(["Color mode", "Palette", "Accent", .. StyleGroups, .. ComponentGroups, .. ChartGroups], GroupNames(cut));
	}

	[Theory]
	[InlineData("Light", MokaColorMode.Light)]
	[InlineData("System", MokaColorMode.System)]
	public void PickingAMode_RaisesTheNewValue(string card, MokaColorMode expected)
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.ValueChanged, v => raised = v));

		Pick(cut, "Color mode", card);

		Assert.Equal(new MokaAppearance { Mode = expected }, raised);
	}

	[Fact]
	public void PickingAPalette_RaisesItsId()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.ValueChanged, v => raised = v));

		Pick(cut, "Palette", "Gate");

		Assert.Equal(new MokaAppearance { Palette = "gate" }, raised);
	}

	[Theory]
	[MemberData(nameof(Picks))]
	public void PickingAStyleOption_RaisesThatChoiceAlone(MokaAppearanceRows row, string group, string option, MokaAppearance expected)
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, row)
			.Add(x => x.ValueChanged, v => raised = v));

		Pick(cut, group, option);

		Assert.Equal(expected, raised);
		Assert.Equal(option, CheckedOption(cut, group));
	}

	[Fact]
	public void Picks_AddUpOnOneValue()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.ValueChanged, v => raised = v));

		Pick(cut, "Density", "Compact");
		Pick(cut, "Palette", "Matrix");

		Assert.Equal(new MokaAppearance { Density = MokaDensity.Compact, Palette = "matrix" }, raised);
	}

	[Fact]
	public void PickingAnAccent_RaisesItsId_AndOffersThePalettesOwn()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.ValueChanged, v => raised = v));
		Assert.Contains("The palette's own accent", cut.Markup, StringComparison.Ordinal);
		Assert.DoesNotContain("Use the palette's own", cut.Markup, StringComparison.Ordinal);

		Swatch(cut, "Violet").Change(true);

		Assert.Equal(MokaAccent.Violet.Id, raised!.Accent);
		Assert.True(Swatch(cut, "Violet").HasAttribute("checked"));
		cut.FindAll("button").Single(b => b.TextContent.Contains("Use the palette's own", StringComparison.Ordinal)).Click();
		Assert.Null(raised.Accent);
		Assert.False(Swatch(cut, "Violet").HasAttribute("checked"));
	}

	[Fact]
	public void ClickingTheChosenAccentAgain_GoesBackToThePalettesOwn()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Value, new MokaAppearance { Accent = "cyan" })
			.Add(x => x.ValueChanged, v => raised = v));

		Swatch(cut, "Cyan").Click();

		Assert.Null(raised!.Accent);
	}

	[Theory]
	[InlineData(MokaColorMode.Dark)]
	[InlineData(MokaColorMode.Light)]
	public void Accents_AreDrawnInTheShadeOfTheScheme(MokaColorMode mode)
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Value, new MokaAppearance { Mode = mode }));

		Assert.Contains($"--_swatch-color: {MokaAccent.Blue.For(mode == MokaColorMode.Dark).Color}", SwatchItem(cut, "Blue").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void InSystemMode_TheSchemeIsWhatThePageShows()
	{
		IRenderedComponent<MokaAppearancePanel> cut = Render<MokaAppearancePanel>(p => p
			.AddCascadingValue(MokaTheme.Light)
			.Add(x => x.Value, new MokaAppearance { Mode = MokaColorMode.System }));

		Assert.Contains($"--_swatch-color: {MokaAccent.Blue.Light.Color}", SwatchItem(cut, "Blue").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void APrimaryAccentChoice_ShowsOnEveryPreview()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Value, new MokaAppearance { Accent = "cyan" }));

		IReadOnlyList<IElement> previews = cut.FindAll(".moka-root");
		Assert.NotEmpty(previews);
		Assert.All(previews, preview => Assert.Matches(new Regex("--moka-color-primary: (#3CC8E6|#00748C)", RegexOptions.IgnoreCase), preview.GetAttribute("style")!));
	}

	[Fact]
	public void TheModeCards_ShowEachForm()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Value, new MokaAppearance { Palette = "gate" }));

		IReadOnlyList<IElement> modeCards = cut.FindAll("[aria-label='Color mode'] label");

		Assert.DoesNotContain("moka-dark", modeCards[0].QuerySelector(".moka-root")!.ClassName!, StringComparison.Ordinal);
		Assert.Contains("moka-dark", modeCards[1].QuerySelector(".moka-root")!.ClassName!, StringComparison.Ordinal);
		string[] system = [.. modeCards[2].QuerySelectorAll(".moka-root").Select(root => root.ClassName!)];
		Assert.Equal(2, system.Length);
		Assert.DoesNotContain("moka-dark", system[0], StringComparison.Ordinal);
		Assert.Contains("moka-dark", system[1], StringComparison.Ordinal);
		Assert.Contains(MokaTheme.Gate.Palette.Background, modeCards[1].QuerySelector(".moka-root")!.GetAttribute("style")!, StringComparison.Ordinal);
	}

	[Fact]
	public void ThePreviews_AreNestedProviders_UnderAnOuterOne()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
			.Add(x => x.ChildContent, (RenderFragment)(b =>
			{
				b.OpenComponent<MokaAppearancePanel>(0);
				b.CloseComponent();
			})));

		Assert.Single(cut.FindAll("link[href*='moka.css']"));
		Assert.NotEmpty(cut.FindAll(".moka-root--nested"));
		Assert.Empty(cut.FindAll(".moka-radio-item__art style"));
	}

	[Fact]
	public void ThePreviews_AreHiddenFromScreenReaders()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel();

		Assert.All(cut.FindAll(".moka-root"), root => Assert.NotNull(root.Closest("[aria-hidden='true']")));
	}

	[Fact]
	public void ANonce_ReachesThePreviews()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Nonce, "abc123"));

		Assert.NotEmpty(cut.FindAll(".moka-root[data-moka-theme]"));
		Assert.Empty(cut.FindAll(".moka-root[style]"));
	}

	[Theory]
	[InlineData("Density", "Comfortable")]
	[InlineData("Text size", "Normal")]
	[InlineData("Corners", "Soft")]
	[InlineData("Main button", "Solid")]
	[InlineData("Letter case", "Sentence")]
	[InlineData("Title font", "Space Grotesk")]
	[InlineData("Selected items", "Ring")]
	[InlineData("Current page", "Ring")]
	[InlineData("Status bar", "Standard")]
	[InlineData("Descriptions", "Shown")]
	[InlineData("Label colors", "Vivid")]
	[InlineData("Component style", "Standard")]
	[InlineData("Toasts", "Standard")]
	[InlineData("Toast stack", "List")]
	[InlineData("Notifications", "Standard")]
	[InlineData("Popups", "Standard")]
	[InlineData("Lists", "Standard")]
	[InlineData("Dialogs", "Standard")]
	[InlineData("Buttons", "Standard")]
	public void AnUnsetChoice_ShowsWhatTheDefaultPaletteHas(string group, string expected)
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.All));

		Assert.Equal(expected, CheckedOption(cut, group));
	}

	[Theory]
	[InlineData("Density", "Compact")]
	[InlineData("Text size", "Small")]
	[InlineData("Corners", "Tight")]
	[InlineData("Title font", "Inter")]
	[InlineData("Selected items", "Flat")]
	public void AnUnsetChoice_ShowsWhatThisPaletteHas(string group, string expected)
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.All)
			.Add(x => x.Palettes, [MokaAppearancePalette.Crisp, MokaAppearancePalette.Red]));

		Assert.Equal(expected, CheckedOption(cut, group));
	}

	[Fact]
	public void TheOriginalDesign_HidesTheStyleRows_AndSaysWhy()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.All)
			.Add(x => x.Value, new MokaAppearance { Palette = "original" }));

		Assert.Equal(["Color mode", "Palette", "Accent"], GroupNames(cut));
		Assert.Single(cut.FindAll("[role='radiogroup'][aria-label='Accent']"));
		Assert.Contains("keeps its own style", cut.Find(".moka-appearance-panel__said + *, p.moka-appearance-panel__said").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void WithNoStyleRowsAsked_TheOriginalDesignSaysNothing()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.ColorMode | MokaAppearanceRows.Palette)
			.Add(x => x.Value, new MokaAppearance { Palette = "original" }));

		Assert.Empty(cut.FindAll("p.moka-appearance-panel__said"));
	}

	[Fact]
	public void PickingTheOriginalDesign_KeepsTheStyleChoicesForLater()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Value, new MokaAppearance { Density = MokaDensity.Compact })
			.Add(x => x.ValueChanged, v => raised = v));

		Pick(cut, "Palette", "Original");
		Assert.DoesNotContain("Density", GroupNames(cut));
		Assert.Equal(MokaDensity.Compact, raised!.Density);

		Pick(cut, "Palette", "Red");
		Assert.Equal("Compact", CheckedOption(cut, "Density"));
	}

	[Fact]
	public void AnAppsOwnPalettes_ReplaceTheBuiltInOnes()
	{
		MokaAppearancePalette[] brand =
		[
			new("sea", "Sea", MokaTheme.Gate, MokaTheme.GateLight) { Hint = "Deep blue" },
			new("sand", "Sand", MokaTheme.Red, MokaTheme.RedLight)
		];

		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Palettes, brand));

		string[] names = [.. cut.FindAll("[aria-label='Palette'] .moka-radio-item__name").Select(n => n.TextContent.Trim())];
		Assert.Equal(["Sea", "Sand"], names);
		Assert.Equal("Deep blue", cut.Find("[aria-label='Palette'] .moka-radio-item__hint").TextContent);
		Assert.Equal("Sea", CheckedOption(cut, "Palette"));
	}

	[Fact]
	public void OnePalette_LeavesNothingToPick() =>
		Assert.DoesNotContain("Palette", GroupNames(RenderPanel(p => p.Add(x => x.Palettes, [MokaAppearancePalette.Gate]))));

	[Fact]
	public void NoAccents_LeavesNothingToPick()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Accents, []));

		Assert.Empty(cut.FindAll("[role='radiogroup'][aria-label='Accent']"));
	}

	[Fact]
	public void AnAppsOwnAccents_ReplaceTheBuiltInOnes()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Accents, [MokaAccent.Pink with { Id = "brand", Name = "Brand pink" }])
			.Add(x => x.ValueChanged, v => raised = v));

		Swatch(cut, "Brand pink").Change(true);

		Assert.Equal("brand", raised!.Accent);
		Assert.Single(cut.FindAll(".moka-color-swatch-item"));
	}

	[Fact]
	public void TheAppsOwnRows_ComeAfterTheBuiltInOnes()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.AddChildContent<MokaAppearanceRow>(row => row.Add(x => x.Label, "Sidebar").Add(x => x.Hint, "Narrow or wide")));

		IReadOnlyList<IElement> rows = cut.FindAll(".moka-appearance-panel__rows > *");
		Assert.Equal(["DENSITY", "TEXT SIZE", "SIDEBAR"], rows.Select(r => r.QuerySelector(".moka-micro-label")!.TextContent.ToUpperInvariant()));
		Assert.Equal("Narrow or wide", rows[^1].QuerySelector(".moka-appearance-row__hint")!.TextContent);
	}

	[Fact]
	public void TheAppsOwnRows_ShowWithNoBuiltInOnes()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.None)
			.AddChildContent("<span class='mine'></span>"));

		Assert.Single(cut.FindAll(".moka-appearance-panel__rows .mine"));
	}

	[Fact]
	public void NothingAtAll_DrawsNoEmptyRowsBox() =>
		Assert.Empty(RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.None)).FindAll(".moka-appearance-panel__rows"));

	[Fact]
	public void ANewValueFromTheParent_ReplacesThePicks()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel();
		Pick(cut, "Density", "Compact");

		cut.Render(p => p.Add(x => x.Value, new MokaAppearance { Mode = MokaColorMode.Light }));

		Assert.Equal("Light", CheckedOption(cut, "Color mode"));
		Assert.Equal("Comfortable", CheckedOption(cut, "Density"));
	}

	[Fact]
	public void AParentRenderWithTheOldValue_DoesNotUndoAPick()
	{
		MokaAppearance original = new();
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Value, original));
		Pick(cut, "Density", "Compact");

		cut.Render(p => p.Add(x => x.Value, original));

		Assert.Equal("Compact", CheckedOption(cut, "Density"));
	}

	[Fact]
	public void Disabled_MakesThePanelInert()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Disabled, true));

		IElement root = cut.Find(".moka-appearance-panel");
		Assert.True(root.HasAttribute("inert"));
		Assert.Contains("moka-appearance-panel--disabled", root.ClassName, StringComparison.Ordinal);
		Assert.All(cut.FindAll("input[type=radio]"), radio => Assert.True(radio.HasAttribute("disabled")));
	}

	[Fact]
	public void TheCards_TakeTheirWidthFromThePanelsStylesheet()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel();

		Assert.All(cut.FindAll(".moka-radiogroup--cards"), group =>
			Assert.Contains("var(--_appearance-card)", group.GetAttribute("style"), StringComparison.Ordinal));
	}

	[Fact]
	public void Size_ReachesTheSegments()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Size, MokaSize.Lg));

		Assert.All(cut.FindAll(".moka-segmented"), segmented => Assert.Contains("moka-segmented--lg", segmented.ClassName, StringComparison.Ordinal));
	}

	[Fact]
	public void TheRoot_TakesClassStyleIdAndAttributes()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Class, "mine")
			.Add(x => x.Style, "color: red")
			.Add(x => x.Id, "look")
			.AddUnmatched("data-test", "1"));

		IElement root = cut.Find(".moka-appearance-panel");
		Assert.EndsWith("mine", root.ClassName, StringComparison.Ordinal);
		Assert.Equal("color: red", root.GetAttribute("style"));
		Assert.Equal("look", root.Id);
		Assert.Equal("1", root.GetAttribute("data-test"));
	}

	[Fact]
	public void TheRoot_TakesMarginPaddingAndRounding()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.MarginValue, "1px")
			.Add(x => x.PaddingValue, "2px")
			.Add(x => x.RoundedValue, "3px"));

		string style = cut.Find(".moka-appearance-panel").GetAttribute("style")!;
		Assert.Contains("margin: 1px", style, StringComparison.Ordinal);
		Assert.Contains("padding: 2px", style, StringComparison.Ordinal);
		Assert.Contains("border-radius: 3px", style, StringComparison.Ordinal);
	}

	[Fact]
	public void TheRow_TakesClassStyleIdAndAttributes()
	{
		IRenderedComponent<MokaAppearanceRow> cut = Render<MokaAppearanceRow>(p => p
			.Add(x => x.Label, "Sidebar")
			.Add(x => x.Class, "mine")
			.Add(x => x.Style, "color: red")
			.Add(x => x.Id, "row")
			.AddUnmatched("data-test", "1"));

		IElement root = cut.Find(".moka-appearance-row");
		Assert.EndsWith("mine", root.ClassName, StringComparison.Ordinal);
		Assert.Equal("color: red", root.GetAttribute("style"));
		Assert.Equal("row", root.Id);
		Assert.Equal("1", root.GetAttribute("data-test"));
	}

	[Fact]
	public void EveryWord_ComesFromTheTextRecord()
	{
		MokaAppearanceText text = NumberedText(out IReadOnlyList<string> words);
		MokaAppearancePalette[] palettes =
		[
			new("one", "PX", MokaTheme.Red, MokaTheme.RedLight),
			new("two", "PY", MokaTheme.Gate, MokaTheme.GateLight),
			new("three", "PZ", MokaTheme.Dark, MokaTheme.Light)
		];
		MokaAccent[] accents = [MokaAccent.Blue with { Id = "x", Name = "AX" }];

		MokaAppearance[] states = [new(), new() { Accent = "x" }, new() { Palette = "three" }];
		MokaColorMode[] modes = Enum.GetValues<MokaColorMode>();

		string shown = string.Join(' ', states.Select(state => VisibleText(RenderPanel(p => p
			.Add(x => x.Value, state)
			.Add(x => x.Rows, MokaAppearanceRows.All)
			.Add(x => x.Palettes, palettes)
			.Add(x => x.Accents, accents)
			.Add(x => x.Text, text))))
			.Concat(modes.Select(mode => VisibleText(RenderPanel(p => p
				.Add(x => x.Value, new MokaAppearance { Mode = mode })
				.Add(x => x.ModeLook, MokaThemeToggleLook.Icon)
				.Add(x => x.Palettes, palettes)
				.Add(x => x.Accents, accents)
				.Add(x => x.Text, text))))));

		Assert.All(words, word => Assert.Contains(word, shown, StringComparison.Ordinal));
		Assert.Equal("", Numbered().Replace(shown, "").Trim());
	}

	[Fact]
	public void WithNoOptionState_EveryOptionIsOffered()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.All));

		Assert.Equal(["Light", "Dark", "System"], OptionsIn(cut, "Color mode"));
		Assert.Equal(["Red", "Gate", "Matrix", "Ember", "Original"], OptionsIn(cut, "Palette"));
		Assert.Equal(MokaAccent.Presets.Count, cut.FindAll(".moka-color-swatch-item").Count);
		Assert.Empty(cut.FindAll("input[type=radio][disabled]"));
		Assert.Empty(cut.FindAll("input.moka-color-swatch-input[disabled]"));
	}

	[Fact]
	public void TheQuestion_NamesEachOptionByItsRowAndChoice()
	{
		List<MokaAppearanceOption> asked = [];
		RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.All)
			.Add(x => x.OptionState, option =>
			{
				asked.Add(option);
				return MokaAppearanceOptionState.Available;
			}));

		Assert.Contains(new MokaAppearanceOption(MokaAppearanceRows.ColorMode, MokaColorMode.System), asked);
		Assert.Contains(new MokaAppearanceOption(MokaAppearanceRows.Palette, "gate"), asked);
		Assert.Contains(new MokaAppearanceOption(MokaAppearanceRows.Accent, "blue"), asked);
		Assert.Contains(new MokaAppearanceOption(MokaAppearanceRows.Density, MokaDensity.Compact), asked);
		Assert.Contains(new MokaAppearanceOption(MokaAppearanceRows.StatusBar, MokaStatusBarLook.Terminal), asked);
		Assert.Contains(new MokaAppearanceOption(MokaAppearanceRows.Descriptions, false), asked);
	}

	[Fact]
	public void AHiddenMode_LosesItsCard()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.OptionState, option => Hide(option, MokaColorMode.System)));

		Assert.Equal(["Light", "Dark"], OptionsIn(cut, "Color mode"));
	}

	[Fact]
	public void ADisabledMode_StaysOnScreen_AndCantBePicked()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.OptionState, option => Disable(option, MokaColorMode.System)));

		Assert.Equal(["Light", "Dark", "System"], OptionsIn(cut, "Color mode"));
		Assert.Equal(["System"], DisabledIn(cut, "Color mode"));
	}

	[Fact]
	public void TheStoredChoice_KeepsAHiddenOption_UntilAnotherIsPicked()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Value, new MokaAppearance { Mode = MokaColorMode.System })
			.Add(x => x.ValueChanged, v => raised = v)
			.Add(x => x.OptionState, option => Hide(option, MokaColorMode.System)));

		Assert.Null(CheckedOption(cut, "Color mode"));
		Assert.Null(raised);

		Pick(cut, "Color mode", "Light");

		Assert.Equal(MokaColorMode.Light, raised!.Mode);
	}

	[Fact]
	public void AHiddenPalette_LosesItsCard()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.OptionState, option => Hide(option, "original")));

		Assert.Equal(["Red", "Gate", "Matrix", "Ember"], OptionsIn(cut, "Palette"));
	}

	[Fact]
	public void ADisabledPalette_StaysOnScreen_AndCantBePicked()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.OptionState, option => Disable(option, "matrix")));

		Assert.Equal(["Matrix"], DisabledIn(cut, "Palette"));
	}

	[Fact]
	public void AHiddenStyleOption_LosesItsSegment()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.Corners)
			.Add(x => x.OptionState, option => Hide(option, MokaCorners.Tight)));

		Assert.Equal(["Soft", "Medium"], OptionsIn(cut, "Corners"));
	}

	[Fact]
	public void ADisabledStyleOption_StaysOnScreen_AndCantBePicked()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.Corners)
			.Add(x => x.OptionState, option => Disable(option, MokaCorners.Tight)));

		Assert.Equal(["Soft", "Medium", "Tight"], OptionsIn(cut, "Corners"));
		Assert.Equal(["Tight"], DisabledIn(cut, "Corners"));
	}

	[Fact]
	public void TheDescriptionsRow_IsAskedAboutWithBooleans()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.Descriptions)
			.Add(x => x.OptionState, option => option is { Value: false } ? MokaAppearanceOptionState.Disabled : MokaAppearanceOptionState.Available));

		Assert.Equal(["Hidden"], DisabledIn(cut, "Descriptions"));
	}

	[Fact]
	public void ALoneOption_ThatIsAlreadyPicked_LeavesNothingToChoose()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.OptionState, option => Hide(option, MokaDensity.Compact)));

		Assert.DoesNotContain("Density", GroupNames(cut));
	}

	[Fact]
	public void ALoneOption_ThatIsNotPicked_IsStillAChoice()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.OptionState, option => Hide(option, MokaDensity.Comfortable)));

		Assert.Equal(["Compact"], OptionsIn(cut, "Density"));
	}

	[Fact]
	public void EveryOptionHidden_HidesTheRow()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.OptionState, option =>
			option.Row == MokaAppearanceRows.ColorMode ? MokaAppearanceOptionState.Hidden : MokaAppearanceOptionState.Available));

		Assert.DoesNotContain("Color mode", GroupNames(cut));
	}

	[Fact]
	public void OnePaletteLeft_ThatIsAlreadyPicked_LeavesNothingToChoose()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.OptionState, option =>
			option is { Row: MokaAppearanceRows.Palette, Value: not "red" } ? MokaAppearanceOptionState.Hidden : MokaAppearanceOptionState.Available));

		Assert.DoesNotContain("Palette", GroupNames(cut));
	}

	[Fact]
	public void AHiddenAccent_LosesItsSwatch()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.OptionState, option => Hide(option, "cyan")));

		Assert.Empty(cut.FindAll("input.moka-color-swatch-input[aria-label='Cyan']"));
		Assert.Equal(MokaAccent.Presets.Count - 1, cut.FindAll(".moka-color-swatch-item").Count);
	}

	[Fact]
	public void EveryAccentHidden_HidesTheRow()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.OptionState, option =>
			option.Row == MokaAppearanceRows.Accent ? MokaAppearanceOptionState.Hidden : MokaAppearanceOptionState.Available));

		Assert.Empty(cut.FindAll("[role='radiogroup'][aria-label='Accent']"));
	}

	[Fact]
	public void ADisabledAccent_StaysOnScreen_AndCantBePicked()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.OptionState, option => Disable(option, "cyan")));

		Assert.True(Swatch(cut, "Cyan").HasAttribute("disabled"));
		Assert.False(Swatch(cut, "Violet").HasAttribute("disabled"));
	}

	[Fact]
	public void AnyColor_CanBeOfferedAsAnAccent()
	{
		MokaAccent brand = MokaAccent.FromColor("#12AB34") with { Id = "brand", Name = "Brand" };
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Accents, [.. MokaAccent.Presets, brand])
			.Add(x => x.ValueChanged, v => raised = v));

		Swatch(cut, "Brand").Change(true);

		Assert.Equal("brand", raised!.Accent);
		Assert.Equal(brand.Dark.Color, raised.ToTheme(isDark: true, accents: [brand]).Palette.Primary);
	}

	[Fact]
	public void EveryDensity_IsOffered() =>
		Assert.Equal(Enum.GetNames<MokaDensity>(), OptionsIn(RenderPanel(), "Density"));

	[Fact]
	public void TheColorMode_IsForwardedFromThisPackage() =>
		Assert.Contains(typeof(MokaColorMode), typeof(MokaAppearance).Assembly.GetForwardedTypes());

	[Fact]
	public void TheStatusBarRow_IsPictureCards_OfRealBars()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel();

		IReadOnlyList<IElement> cards = cut.FindAll("[aria-label='Status bar'] label");
		Assert.Equal(Enum.GetValues<MokaStatusBarLook>().Length, cards.Count);
		Assert.All(cards, card => Assert.NotNull(card.QuerySelector(".moka-radio-item__art .moka-statusbar--inline")));
		Assert.Contains("moka-statusbar--terminal", cards.Single(card => TextOf(card) == "Terminal").QuerySelector("footer")!.ClassList);
		Assert.Contains("moka-statusbar--floating", cards.Single(card => TextOf(card) == "Floating").QuerySelector("footer")!.ClassList);
	}

	[Fact]
	public void TheStatusBarRow_ShowsTheStoredChoice_AndAPickRaisesIt()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Value, new MokaAppearance { StatusBar = MokaStatusBarLook.Chips })
			.Add(x => x.ValueChanged, v => raised = v));
		Assert.Equal("Chips", CheckedOption(cut, "Status bar"));

		Pick(cut, "Status bar", "Segments");

		Assert.Equal(MokaStatusBarLook.Segments, raised!.StatusBar);
	}

	[Fact]
	public void TheStatusBarRow_FollowsItsFlag() =>
		Assert.DoesNotContain("Status bar", GroupNames(RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.Default & ~MokaAppearanceRows.StatusBar))));

	[Fact]
	public void TheStatusBarRow_RespectsOptionState()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.OptionState, option => option.Value switch
		{
			MokaStatusBarLook.Terminal => MokaAppearanceOptionState.Hidden,
			MokaStatusBarLook.Accent => MokaAppearanceOptionState.Disabled,
			_ => MokaAppearanceOptionState.Available
		}));

		Assert.DoesNotContain("Terminal", OptionsIn(cut, "Status bar"));
		Assert.Equal(["Accent"], DisabledIn(cut, "Status bar"));
		Assert.Empty(cut.FindAll(".moka-statusbar--terminal"));
	}

	[Fact]
	public void TheStatusBarRow_GoesWithTheStyleRows_OnTheOriginalDesign() =>
		Assert.Empty(RenderPanel(p => p.Add(x => x.Value, new MokaAppearance { Palette = "original" })).FindAll(".moka-statusbar"));

	[Theory]
	[InlineData(MokaAppearanceRows.Density, "Density", 2, ".moka-appearance-panel__density-row")]
	[InlineData(MokaAppearanceRows.Corners, "Corners", 3, ".moka-appearance-panel__corner")]
	[InlineData(MokaAppearanceRows.MainButton, "Main button", 3, ".moka-appearance-panel__main-button")]
	[InlineData(MokaAppearanceRows.LabelTone, "Label colors", 3, ".moka-appearance-panel__label")]
	public void AStyleRowAWordCantShow_HasPictures(MokaAppearanceRows row, string group, int options, string picture)
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, row));

		IReadOnlyList<IElement> cards = cut.FindAll($"[aria-label='{group}'] label");
		Assert.Equal(options, cards.Count);
		Assert.All(cards, card => Assert.NotNull(card.QuerySelector(picture)));
	}

	[Fact]
	public void EachPicture_IsDrawnWithItsOwnOption()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.Corners));

		string[] styles = [.. cut.FindAll("[aria-label='Corners'] .moka-root").Select(root => root.GetAttribute("style")!)];

		Assert.Equal(3, styles.Distinct().Count());
	}

	[Fact]
	public void TheLabelColorPictures_AreTagsDrawnInEachTone()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.LabelTone));

		string[] styles = [.. cut.FindAll("[aria-label='Label colors'] .moka-root").Select(root => root.GetAttribute("style")!)];

		Assert.Equal(3, styles.Length);
		Assert.Contains("--moka-label-teal: #14B8A6;", styles[0], StringComparison.Ordinal);
		Assert.Contains("--moka-label-teal: #46B5A8;", styles[1], StringComparison.Ordinal);
		Assert.Contains("--moka-label-teal: #7ED8CE;", styles[2], StringComparison.Ordinal);
		Assert.Contains("--_label-bg: var(--moka-label-teal-bg)", cut.Find(".moka-appearance-panel__label").GetAttribute("style"),
			StringComparison.Ordinal);
	}

	[Fact]
	public void TheLabelColorRow_GoesWithTheStyleRows_OnTheOriginalDesign() =>
		Assert.Empty(RenderPanel(p => p
				.Add(x => x.Rows, MokaAppearanceRows.LabelTone)
				.Add(x => x.Value, new MokaAppearance { Palette = "original" }))
			.FindAll(".moka-appearance-panel__label"));

	[Fact]
	public void APictures_ThemeInstance_SurvivesARender()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.Corners));
		MokaTheme[] before = [.. cut.FindComponents<MokaThemeProvider>().Select(provider => provider.Instance.Theme)];

		cut.Render();

		Assert.Equal(before, cut.FindComponents<MokaThemeProvider>().Select(provider => provider.Instance.Theme), ReferenceEqualityComparer.Instance);
	}

	[Fact]
	public void APicturesTheme_FollowsTheValue()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.Corners));
		string before = cut.Find("[aria-label='Corners'] .moka-root").GetAttribute("style")!;

		cut.Render(p => p.Add(x => x.Value, new MokaAppearance { Accent = "cyan" }));

		Assert.NotEqual(before, cut.Find("[aria-label='Corners'] .moka-root").GetAttribute("style"));
	}

	[Fact]
	public void TheGroups_HaveQuietHeadings()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel();

		Assert.Equal(["Colors", "Style"], cut.FindAll(".moka-appearance-panel__heading").Select(heading => heading.TextContent));
	}

	[Fact]
	public void OneGroup_NeedsNoHeading() =>
		Assert.Empty(RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.Density | MokaAppearanceRows.Corners)).FindAll(".moka-appearance-panel__heading"));

	[Fact]
	public void ModeLook_DrawsTheColorModeAsAToggle()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.ModeLook, MokaThemeToggleLook.IconsAndWords)
			.Add(x => x.ValueChanged, v => raised = v));

		Assert.Equal(["Light", "Dark", "System"], cut.FindAll("[aria-label='Color mode'] .moka-segment__text").Select(word => word.TextContent));
		Assert.Empty(cut.FindAll("[aria-label='Color mode'] .moka-radio-item__art"));

		cut.Find("[aria-label='Color mode'] input[type=radio]").Change(true);

		Assert.Equal(MokaColorMode.Light, raised!.Mode);
	}

	[Theory]
	[InlineData(MokaThemeToggleLook.Icon, "button.moka-theme-toggle")]
	[InlineData(MokaThemeToggleLook.Chip, "button.moka-theme-toggle-chip")]
	[InlineData(MokaThemeToggleLook.Switch, ".moka-theme-toggle-switch[role=radiogroup]")]
	[InlineData(MokaThemeToggleLook.Segmented, ".moka-theme-toggle-segmented")]
	public void ModeLook_TakesEveryLook(MokaThemeToggleLook look, string selector) =>
		Assert.Single(RenderPanel(p => p.Add(x => x.ModeLook, look)).FindAll(selector));

	[Fact]
	public void AHiddenSystem_MakesTheToggleTwoStates()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.ModeLook, MokaThemeToggleLook.Switch)
			.Add(x => x.OptionState, option => Hide(option, MokaColorMode.System)));

		Assert.Single(cut.FindAll("[role=switch]"));
	}

	[Theory]
	[InlineData(MokaColorMode.Light, MokaAppearanceOptionState.Hidden)]
	[InlineData(MokaColorMode.Dark, MokaAppearanceOptionState.Hidden)]
	[InlineData(MokaColorMode.System, MokaAppearanceOptionState.Disabled)]
	public void AModeAToggleCantShow_KeepsTheCards(MokaColorMode mode, MokaAppearanceOptionState state)
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.ModeLook, MokaThemeToggleLook.Chip)
			.Add(x => x.OptionState, option => Equals(option.Value, mode) ? state : MokaAppearanceOptionState.Available));

		Assert.Empty(cut.FindAll(".moka-theme-toggle-chip"));
		Assert.NotEmpty(cut.FindAll("[aria-label='Color mode'] .moka-radio-item__art"));
	}

	[Fact]
	public void Compact_HasNoHeadingsOrPictures_AndAToggleForTheMode()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Compact, true).Add(x => x.Rows, MokaAppearanceRows.All));

		Assert.Contains("moka-appearance-panel--compact", cut.Find(".moka-appearance-panel").ClassList);
		Assert.Empty(cut.FindAll(".moka-appearance-panel__heading"));
		Assert.Empty(cut.FindAll(".moka-appearance-panel__art"));
		Assert.Empty(cut.FindAll(".moka-statusbar"));
		Assert.Single(cut.FindAll(".moka-theme-toggle-segmented"));
		Assert.Equal("Standard", CheckedOption(cut, "Status bar"));
	}

	[Fact]
	public void Compact_StillPicks()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Compact, true).Add(x => x.ValueChanged, v => raised = v));

		Pick(cut, "Status bar", "Accent");

		Assert.Equal(MokaStatusBarLook.Accent, raised!.StatusBar);
	}

	[Fact]
	public void ModeLook_WinsOverCompactsChoice() =>
		Assert.Single(RenderPanel(p => p.Add(x => x.Compact, true).Add(x => x.ModeLook, MokaThemeToggleLook.Chip)).FindAll(".moka-theme-toggle-chip"));

	private static IReadOnlyList<IElement> ResetButtons(IRenderedComponent<MokaAppearancePanel> cut) => cut.FindAll(".moka-appearance-panel__foot button");

	[Fact]
	public void Reset_ShowsOnlyOnceSomethingChanged()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel();
		Assert.Empty(ResetButtons(cut));

		Pick(cut, "Density", "Compact");

		Assert.Equal("Back to defaults", ResetButtons(cut)[0].TextContent.Trim());
	}

	[Fact]
	public void Reset_PutsEveryChoiceBack()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Value, new MokaAppearance { Density = MokaDensity.Compact, Palette = "gate" })
			.Add(x => x.ValueChanged, v => raised = v));

		ResetButtons(cut)[0].Click();

		Assert.Equal(new MokaAppearance(), raised);
		Assert.Empty(ResetButtons(cut));
		Assert.Equal("Comfortable", CheckedOption(cut, "Density"));
	}

	[Fact]
	public void Reset_GoesBackToTheAppsDefaults()
	{
		MokaAppearance defaults = new() { Mode = MokaColorMode.System };
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Defaults, defaults)
			.Add(x => x.ValueChanged, v => raised = v));

		ResetButtons(cut)[0].Click();

		Assert.Equal(defaults, raised);
		Assert.Empty(ResetButtons(cut));
	}

	[Fact]
	public void ShowReset_TurnsItOff() =>
		Assert.Empty(ResetButtons(RenderPanel(p => p.Add(x => x.ShowReset, false).Add(x => x.Value, new MokaAppearance { Density = MokaDensity.Compact }))));

	private static MokaAppearanceOptionState Hide(MokaAppearanceOption option, object value) =>
		Equals(option.Value, value) ? MokaAppearanceOptionState.Hidden : MokaAppearanceOptionState.Available;

	private static MokaAppearanceOptionState Disable(MokaAppearanceOption option, object value) =>
		Equals(option.Value, value) ? MokaAppearanceOptionState.Disabled : MokaAppearanceOptionState.Available;

	private static string[] OptionsIn(IRenderedComponent<MokaAppearancePanel> cut, string group) =>
		[.. cut.FindAll($"[role='radiogroup'][aria-label='{group}'] label").Select(TextOf)];

	private static string[] DisabledIn(IRenderedComponent<MokaAppearancePanel> cut, string group) =>
		[.. cut.FindAll($"[role='radiogroup'][aria-label='{group}'] label")
			.Where(label => label.QuerySelector("input[type=radio]")!.HasAttribute("disabled"))
			.Select(TextOf)];

	private IRenderedComponent<MokaAppearancePanel> RenderPanel(Action<ComponentParameterCollectionBuilder<MokaAppearancePanel>>? parameters = null) =>
		Render<MokaAppearancePanel>(p => parameters?.Invoke(p));

	private static string VisibleText(IRenderedComponent<MokaAppearancePanel> cut)
	{
		IElement root = cut.Find(".moka-appearance-panel");
		foreach (IElement style in root.QuerySelectorAll("style").ToArray())
		{
			style.Remove();
		}

		return root.TextContent;
	}

	private static string[] GroupNames(IRenderedComponent<MokaAppearancePanel> cut) =>
		[.. cut.FindAll("[role='radiogroup']").Select(group => group.GetAttribute("aria-label")!)];

	private static void Pick(IRenderedComponent<MokaAppearancePanel> cut, string group, string option) =>
		cut.FindAll($"[role='radiogroup'][aria-label='{group}'] label")
			.Single(label => TextOf(label) == option)
			.QuerySelector("input[type=radio]")!
			.Change(true);

	private static string? CheckedOption(IRenderedComponent<MokaAppearancePanel> cut, string group)
	{
		IElement? selected = cut.FindAll($"[role='radiogroup'][aria-label='{group}'] label")
			.SingleOrDefault(label => label.QuerySelector("input[type=radio]")!.HasAttribute("checked"));
		return selected is null ? null : TextOf(selected);
	}

	private static string TextOf(IElement label) =>
		(label.QuerySelector(".moka-radio-item__name") ?? label.QuerySelector(".moka-segment__text"))!.TextContent.Trim();

	private static IElement Swatch(IRenderedComponent<MokaAppearancePanel> cut, string name) =>
		cut.Find($"input.moka-color-swatch-input[aria-label='{name}']");

	private static IElement SwatchItem(IRenderedComponent<MokaAppearancePanel> cut, string name) =>
		Swatch(cut, name).ParentElement!;

	private static MokaAppearanceText NumberedText(out IReadOnlyList<string> words)
	{
		MokaAppearanceText text = new();
		List<string> used = [];
		foreach (PropertyInfo property in typeof(MokaAppearanceText).GetProperties().Where(p => p.PropertyType == typeof(string)))
		{
			string word = $"W{used.Count}W";
			used.Add(word);
			property.SetValue(text, word);
		}

		words = used;
		return text;
	}

	[GeneratedRegex(@"W\d+W|P[XYZ]|AX")]
	private static partial Regex Numbered();
}
