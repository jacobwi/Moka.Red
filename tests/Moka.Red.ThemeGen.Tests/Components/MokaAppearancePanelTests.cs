using System.Reflection;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.ThemeGen.Appearance;

namespace Moka.Red.ThemeGen.Tests.Components;

public sealed partial class MokaAppearancePanelTests : BunitContext
{
	private static readonly string[] DefaultGroups = ["Color mode", "Palette", "Density", "Text size"];

	private static readonly string[] StyleGroups =
		["Density", "Text size", "Corners", "Main button", "Letter case", "Title font", "Selected items", "Current page", "Status bar", "Descriptions"];

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
		{ MokaAppearanceRows.Descriptions, "Descriptions", "Hidden", new MokaAppearance { Descriptions = false } }
	};

	[Fact]
	public void ByDefault_ShowsThe5UsualRows()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel();

		Assert.Equal(DefaultGroups, GroupNames(cut));
		Assert.Single(cut.FindAll("[role='group'][aria-label='Accent']"));
	}

	[Fact]
	public void TheRowsFlags_PickWhatShows()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.Palette | MokaAppearanceRows.StatusBar));

		Assert.Equal(["Palette", "Status bar"], GroupNames(cut));
		Assert.Empty(cut.FindAll("[role='group'][aria-label='Accent']"));
	}

	[Fact]
	public void All_ShowsEveryRow()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.All));

		Assert.Equal(["Color mode", "Palette", .. StyleGroups], GroupNames(cut));
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

		Swatch(cut, "Violet").Click();

		Assert.Equal(MokaAccent.Violet.Id, raised!.Accent);
		Assert.Equal("true", Swatch(cut, "Violet").GetAttribute("aria-pressed"));
		cut.FindAll("button").Single(b => b.TextContent.Contains("Use the palette's own", StringComparison.Ordinal)).Click();
		Assert.Null(raised.Accent);
		Assert.Equal("false", Swatch(cut, "Violet").GetAttribute("aria-pressed"));
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

		Assert.Contains($"background-color: {MokaAccent.Blue.For(mode == MokaColorMode.Dark).Color}", Swatch(cut, "Blue").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void InSystemMode_TheSchemeIsWhatThePageShows()
	{
		IRenderedComponent<MokaAppearancePanel> cut = Render<MokaAppearancePanel>(p => p
			.AddCascadingValue(MokaTheme.Light)
			.Add(x => x.Value, new MokaAppearance { Mode = MokaColorMode.System }));

		Assert.Contains($"background-color: {MokaAccent.Blue.Light.Color}", Swatch(cut, "Blue").GetAttribute("style"), StringComparison.Ordinal);
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

		Assert.Equal(["Color mode", "Palette"], GroupNames(cut));
		Assert.Single(cut.FindAll("[role='group'][aria-label='Accent']"));
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

		Assert.Empty(cut.FindAll("[role='group'][aria-label='Accent']"));
	}

	[Fact]
	public void AnAppsOwnAccents_ReplaceTheBuiltInOnes()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Accents, [MokaAccent.Pink with { Id = "brand", Name = "Brand pink" }])
			.Add(x => x.ValueChanged, v => raised = v));

		Swatch(cut, "Brand pink").Click();

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

		string shown = string.Join(' ', states.Select(state => VisibleText(RenderPanel(p => p
			.Add(x => x.Value, state)
			.Add(x => x.Rows, MokaAppearanceRows.All)
			.Add(x => x.Palettes, palettes)
			.Add(x => x.Accents, accents)
			.Add(x => x.Text, text)))));

		Assert.All(words, word => Assert.Contains(word, shown, StringComparison.Ordinal));
		Assert.Equal("", Numbered().Replace(shown, "").Trim());
	}

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
		cut.Find($"button.moka-color-swatch-item[aria-label='{name}']");

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
