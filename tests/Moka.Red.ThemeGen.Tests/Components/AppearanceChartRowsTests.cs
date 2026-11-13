using System.Numerics;
using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.ThemeGen.Appearance;

namespace Moka.Red.ThemeGen.Tests.Components;

// The chart rows: the look, the colors, and texture and print in one row.
public sealed partial class MokaAppearancePanelTests
{
	private static readonly string[] ChartGroups = ["Chart look", "Chart colors", "Texture and print"];

	public static TheoryData<string, MokaChartTexture, MokaChartPrint> TextureOptions => new()
	{
		{ "Color only", MokaChartTexture.None, MokaChartPrint.AsShown },
		{ "Hatched in print", MokaChartTexture.None, MokaChartPrint.Hatched },
		{ "Ink in print", MokaChartTexture.None, MokaChartPrint.Ink },
		{ "Always hatched", MokaChartTexture.Hatched, MokaChartPrint.Hatched }
	};

	[Fact]
	public void TheChartRows_AreLeftOutOfDefaultAndStyle()
	{
		Assert.Equal(MokaAppearanceRows.None, MokaAppearanceRows.Default & MokaAppearanceRows.ChartsGroup);
		Assert.Equal(MokaAppearanceRows.None, MokaAppearanceRows.Style & MokaAppearanceRows.ChartsGroup);
		Assert.Equal(MokaAppearanceRows.ChartsGroup, MokaAppearanceRows.All & MokaAppearanceRows.ChartsGroup);
		Assert.Equal(3, BitOperations.PopCount((ulong)MokaAppearanceRows.ChartsGroup));
	}

	[Fact]
	public void OnlyChartRows_NeedNoHeading()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.ChartsGroup));

		Assert.Equal(ChartGroups, GroupNames(cut));
		Assert.Empty(cut.FindAll(".moka-appearance-panel__heading"));
	}

	[Fact]
	public void TheAppsOwnRows_GoUnderTheChartRows_WhenTheyShow()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.Toasts | MokaAppearanceRows.ChartsGroup)
			.AddChildContent("<div class=\"own-row\"></div>"));

		IElement own = cut.Find(".own-row");
		Assert.Contains("Charts", own.Closest(".moka-appearance-panel__group")!.QuerySelector(".moka-appearance-panel__heading")!.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void AnUnsetChartChoice_ShowsTodaysCharts()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.ChartsGroup));

		Assert.Equal("Standard", CheckedOption(cut, "Chart look"));
		Assert.Equal("Theme roles", CheckedOption(cut, "Chart colors"));
		Assert.Equal("Color only", CheckedOption(cut, "Texture and print"));
	}

	[Fact]
	public void PickingALookAndColors_RaisesThoseChoicesAlone()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.ChartsGroup)
			.Add(x => x.ValueChanged, v => raised = v));

		Pick(cut, "Chart look", "Quiet");
		Assert.Equal(new MokaAppearance { Charts = MokaChartLook.Quiet }, raised);

		Pick(cut, "Chart colors", "Accent first");
		Assert.Equal(new MokaAppearance { Charts = MokaChartLook.Quiet, ChartPalette = MokaChartPalette.AccentFirst }, raised);
		Assert.Equal("Quiet", CheckedOption(cut, "Chart look"));
		Assert.Equal("Accent first", CheckedOption(cut, "Chart colors"));
	}

	[Theory]
	[MemberData(nameof(TextureOptions))]
	public void PickingATextureOption_SetsTextureAndPrintTogether(string option, MokaChartTexture texture, MokaChartPrint print)
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.ChartTexture)
			.Add(x => x.Value, new MokaAppearance { ChartTexture = MokaChartTexture.Hatched, ChartPrint = MokaChartPrint.Ink })
			.Add(x => x.ValueChanged, v => raised = v));

		Pick(cut, "Texture and print", option);

		Assert.Equal(new MokaAppearance { ChartTexture = texture, ChartPrint = print }, raised);
		Assert.Equal(option, CheckedOption(cut, "Texture and print"));
	}

	[Fact]
	public void AMixTheRowDoesntOffer_LeavesNoOptionChecked() =>
		Assert.Null(CheckedOption(RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.ChartTexture)
			.Add(x => x.Value, new MokaAppearance { ChartTexture = MokaChartTexture.Hatched, ChartPrint = MokaChartPrint.Ink })), "Texture and print"));

	[Fact]
	public void TheChartRows_HideOnTheOriginalDesign_AndSayWhy()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.Palette | MokaAppearanceRows.ChartsGroup)
			.Add(x => x.Value, new MokaAppearance { Palette = "original", Charts = MokaChartLook.Bold }));

		Assert.Equal(["Palette"], GroupNames(cut));
		Assert.Contains("keeps its own style", cut.Find("p.moka-appearance-panel__said").TextContent, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll(".moka-appearance-panel__charts"));
	}

	[Fact]
	public void EachLookPicture_CarriesItsLooksClassFromMokaCss()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.Charts));
		IReadOnlyList<IElement> cards = cut.FindAll("[role='radiogroup'][aria-label='Chart look'] label");
		MokaChartLook[] looks = Enum.GetValues<MokaChartLook>();

		Assert.Equal(looks.Length, cards.Count);
		for (int i = 0; i < looks.Length; i++)
		{
			string look = MokaEnumHelpers.ToCssClass(looks[i]);
			Assert.NotNull(cards[i].QuerySelector($".moka-radio-item__art .moka-appearance-panel__charts--{look}.moka-chart-look-{look}"));
			Assert.Equal(2, cards[i].QuerySelectorAll(".moka-appearance-panel__chart-key").Length);
			Assert.Equal(4, cards[i].QuerySelectorAll(".moka-appearance-panel__chart-column").Length);
		}
	}

	[Fact]
	public void ThePanelsStylesheet_DrawsEveryNewChartLookFromOneBase()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", PanelCss));

		Assert.DoesNotContain(".moka-appearance-panel__charts--standard", css, StringComparison.Ordinal);
		Assert.Contains(".moka-appearance-panel__charts--modern", css, StringComparison.Ordinal);
		foreach (string look in new[] { "dense", "soft", "bold" })
		{
			Assert.Contains($".moka-appearance-panel__charts--{look}", css, StringComparison.Ordinal);
		}
	}

	[Theory]
	[InlineData("Theme roles", "var(--moka-color-primary)")]
	[InlineData("Reference", "#4083F3")]
	[InlineData("Accent first", "#EF5350")]
	[InlineData("Muted", "#5D8BD0")]
	public void EachColorsPicture_IsEightSwatches_UnderAProviderSetToThatPalette(string option, string first)
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.ChartPalette));
		IElement card = cut.FindAll("[role='radiogroup'][aria-label='Chart colors'] label").Single(label => TextOf(label) == option);

		Assert.Contains($"--moka-chart-1: {first};", card.QuerySelector(".moka-root")!.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal(Enumerable.Range(1, 8).Select(slot => $"moka-chart-c{slot}"),
			card.QuerySelectorAll(".moka-appearance-panel__swatches > span").Select(swatch => swatch.ClassName));
	}

	[Fact]
	public void TheColorsPictures_FollowTheAccentPicked()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.ChartPalette)
			.Add(x => x.Value, new MokaAppearance { Accent = "violet" }));
		IElement card = cut.FindAll("[role='radiogroup'][aria-label='Chart colors'] label").Single(label => TextOf(label) == "Accent first");

		Assert.Contains($"--moka-chart-1: {MokaAccent.Violet.Dark.Color};", card.QuerySelector(".moka-root")!.GetAttribute("style"), StringComparison.OrdinalIgnoreCase);
	}

	[Theory]
	[InlineData("Color only", false, false, false)]
	[InlineData("Hatched in print", false, true, false)]
	[InlineData("Ink in print", false, false, true)]
	[InlineData("Always hatched", true, true, false)]
	public void EachTexturePicture_HatchesTheScreenAndPageItSays(string option, bool screen, bool page, bool ink)
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.ChartTexture));
		IElement card = cut.FindAll("[role='radiogroup'][aria-label='Texture and print'] label").Single(label => TextOf(label) == option);

		Assert.Equal(screen, card.QuerySelector(".moka-appearance-panel__screen")!.ClassList.Contains("moka-chart-texture"));
		Assert.Equal(page, card.QuerySelector(".moka-appearance-panel__page")!.ClassList.Contains("moka-chart-texture"));
		Assert.Equal(ink, card.QuerySelector(".moka-appearance-panel__page")!.ClassList.Contains("moka-appearance-panel__page--ink"));
		Assert.Contains("moka-gate", card.QuerySelector(".moka-root")!.ClassList);
		Assert.Equal(6, card.QuerySelectorAll(".moka-chart-hatch").Length);
	}

	[Fact]
	public void TheTextureRow_IsAskedAboutWithItsChoices()
	{
		List<MokaAppearanceOption> asked = [];
		RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.ChartsGroup)
			.Add(x => x.OptionState, option =>
			{
				asked.Add(option);
				return MokaAppearanceOptionState.Available;
			}));

		Assert.Contains(new MokaAppearanceOption(MokaAppearanceRows.Charts, MokaChartLook.Dense), asked);
		Assert.Contains(new MokaAppearanceOption(MokaAppearanceRows.ChartPalette, MokaChartPalette.Muted), asked);
		Assert.Contains(new MokaAppearanceOption(MokaAppearanceRows.ChartTexture, MokaChartTextureChoice.InkInPrint), asked);
		Assert.Equal(MokaChartTextureChoice.All, asked.Where(option => option.Row == MokaAppearanceRows.ChartTexture).Select(option => (MokaChartTextureChoice)option.Value).Distinct());
	}

	[Fact]
	public void AHiddenTextureOption_LosesItsCard_AndADisabledOneCantBePicked()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.ChartsGroup)
			.Add(x => x.OptionState, option => option.Value switch
			{
				MokaChartTextureChoice { Print: MokaChartPrint.Ink } => MokaAppearanceOptionState.Hidden,
				MokaChartLook.Bold => MokaAppearanceOptionState.Disabled,
				_ => MokaAppearanceOptionState.Available
			}));

		Assert.Equal(["Color only", "Hatched in print", "Always hatched"], OptionsIn(cut, "Texture and print"));
		Assert.Equal(["Bold"], DisabledIn(cut, "Chart look"));
	}

	[Fact]
	public void Compact_DrawsTheChartRowsAsSegments()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Compact, true).Add(x => x.Rows, MokaAppearanceRows.ChartsGroup));

		Assert.Empty(cut.FindAll(".moka-appearance-panel__charts, .moka-appearance-panel__swatches, .moka-appearance-panel__textures"));
		Assert.Empty(cut.FindAll(".moka-root"));
		Assert.Equal(["Standard", "Quiet", "Dense", "Soft", "Bold"], OptionsIn(cut, "Chart look"));
		Assert.Equal("Color only", CheckedOption(cut, "Texture and print"));
	}

	[Fact]
	public void Reset_PutsTheChartChoicesBack()
	{
		MokaAppearance? raised = null;
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.ChartsGroup)
			.Add(x => x.Value, new MokaAppearance { Charts = MokaChartLook.Soft, ChartPrint = MokaChartPrint.Ink })
			.Add(x => x.ValueChanged, v => raised = v));

		ResetButtons(cut).Single().Click();

		Assert.Equal(new MokaAppearance(), raised);
	}

	[Fact]
	public void ToTheme_AppliesTheChartChoices()
	{
		MokaAppearance value = new()
		{
			Charts = MokaChartLook.Bold,
			ChartPalette = MokaChartPalette.Reference,
			ChartTexture = MokaChartTexture.Hatched,
			ChartPrint = MokaChartPrint.Ink
		};

		Assert.Equal(MokaTheme.Red.WithCharts(MokaChartLook.Bold).WithChartPalette(MokaChartPalette.Reference)
			.WithChartTexture(MokaChartTexture.Hatched).WithChartPrint(MokaChartPrint.Ink), value.ToTheme(isDark: true));
		Assert.Equal(MokaTheme.Dark, (value with { Palette = MokaAppearancePalette.Original.Id }).ToTheme(isDark: true));
	}

	[Fact]
	public void TheChartChoices_RoundTripThroughJson()
	{
		MokaAppearance value = new()
		{
			Charts = MokaChartLook.Dense,
			ChartPalette = MokaChartPalette.AccentFirst,
			ChartTexture = MokaChartTexture.Hatched,
			ChartPrint = MokaChartPrint.Hatched
		};

		Assert.Equal(value, JsonSerializer.Deserialize<MokaAppearance>(JsonSerializer.Serialize(value)));
	}
}
