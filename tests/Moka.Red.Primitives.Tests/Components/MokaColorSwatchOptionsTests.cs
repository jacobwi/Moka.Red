using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Primitives.ColorSwatch;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// Each option is off by default, so the markup without them is what it was.
public class MokaColorSwatchOptionsTests : BunitContext
{
	private static readonly string[] Palette = ["#ef5350", "#42a5f5", "#66bb6a"];

	private static IReadOnlyList<IElement> Radios(IRenderedComponent<MokaColorSwatch> cut) =>
		cut.FindAll("input.moka-color-swatch-input");

	private static string[] Checked(IRenderedComponent<MokaColorSwatch> cut) =>
		[.. Radios(cut).Where(radio => radio.HasAttribute("checked")).Select(radio => radio.GetAttribute("aria-label")!)];

	[Fact]
	public void AriaLabel_NamesTheGroup()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.AriaLabel, "Folder color"));

		Assert.Equal("Folder color", cut.Find("[role='radiogroup']").GetAttribute("aria-label"));
	}

	[Fact]
	public void WithoutAriaLabel_TheGroupCarriesNoEmptyName()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p.Add(x => x.Colors, Palette));

		Assert.False(cut.Find(".moka-color-swatch").HasAttribute("aria-label"));
	}

	[Fact]
	public void ShowNone_AddsANoColorSwatchFirst_CheckedWhileNothingIsChosen()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.ShowNone, true));

		IReadOnlyList<IElement> radios = Radios(cut);
		Assert.Equal(4, radios.Count);
		IconTooltip.AssertNamed(radios[0], "No color", MokaTooltipPosition.Bottom);
		Assert.Contains("moka-color-swatch-item--none", cut.FindAll(".moka-color-swatch-item")[0].ClassList);
		Assert.Equal(["No color"], Checked(cut));
		Assert.Empty(cut.FindAll(".moka-color-swatch-preview"));
	}

	[Fact]
	public void ShowNone_PickingItClearsTheColor()
	{
		List<string?> changes = [];
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.SelectedColor, "#42a5f5")
			.Add(x => x.ShowNone, true)
			.Add(x => x.SelectedColorChanged, color => changes.Add(color)));

		Assert.Equal(["#42a5f5"], Checked(cut));

		Radios(cut)[0].Change(true);

		Assert.Equal([null], changes);
		Assert.Equal(["No color"], Checked(cut));
	}

	[Fact]
	public void ShowNone_IsDisabledWithTheRest()
	{
		List<string?> changes = [];
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.SelectedColor, "#42a5f5")
			.Add(x => x.ShowNone, true)
			.Add(x => x.Disabled, true)
			.Add(x => x.SelectedColorChanged, color => changes.Add(color)));

		Assert.True(Radios(cut)[0].HasAttribute("disabled"));
		Assert.Contains("moka-color-swatch-item--disabled", cut.FindAll(".moka-color-swatch-item")[0].ClassList);

		Radios(cut)[0].Change(true);

		Assert.Empty(changes);
	}

	[Fact]
	public void ShowNone_WorksForLabels()
	{
		List<MokaLabelColor?> changes = [];
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Labels, MokaLabelColors.Distinct)
			.Add(x => x.SelectedLabel, MokaLabelColor.Teal)
			.Add(x => x.ShowNone, true)
			.Add(x => x.SelectedLabelChanged, label => changes.Add(label)));

		Radios(cut)[0].Change(true);

		Assert.Equal([null], changes);
		Assert.Equal(["No color"], Checked(cut));
	}

	[Fact]
	public void Text_GivesEveryWordTheAppsOwn()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.ShowNone, true)
			.Add(x => x.AllowCustom, true)
			.Add(x => x.Text, new MokaColorSwatchText
			{
				None = "Aucune",
				Custom = "Autre couleur",
				Confirm = "Choisir",
				Cancel = "Annuler"
			}));

		IconTooltip.AssertNamed(Radios(cut)[0], "Aucune", MokaTooltipPosition.Bottom);
		IElement add = cut.Find(".moka-color-swatch-add");
		IconTooltip.AssertNamed(add, "Autre couleur", MokaTooltipPosition.Bottom);
		add.Click();
		IconTooltip.AssertNamed(cut.Find(".moka-color-swatch-custom-ok"), "Choisir", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(cut.Find(".moka-color-swatch-custom-cancel"), "Annuler", MokaTooltipPosition.Bottom);
	}

	[Fact]
	public void ShowPreview_Off_LeavesOutTheChosenColorsLine()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.SelectedColor, "#ef5350")
			.Add(x => x.ShowPreview, false));

		Assert.Empty(cut.FindAll(".moka-color-swatch-preview"));
		Assert.Equal(["#ef5350"], Checked(cut));
	}

	[Fact]
	public void Groups_DrawAHeadedGridEach_InOneRadioGroup()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Groups, [new MokaSwatchGroup("Theme", ["#ef5350", "#42a5f5"]), new MokaSwatchGroup("Recent", ["#123456"])])
			.Add(x => x.SelectedColor, "#123456"));

		IReadOnlyList<IElement> groups = cut.FindAll(".moka-color-swatch-group");
		Assert.Equal(2, groups.Count);
		for (int i = 0; i < groups.Count; i++)
		{
			Assert.Equal("group", groups[i].GetAttribute("role"));
			IElement heading = groups[i].QuerySelector(".moka-color-swatch-heading")!;
			Assert.Equal(heading.Id, groups[i].GetAttribute("aria-labelledby"));
			Assert.False(string.IsNullOrEmpty(heading.Id));
		}

		Assert.Equal("Theme", groups[0].QuerySelector(".moka-color-swatch-heading")!.TextContent);
		Assert.Equal(2, groups[0].QuerySelectorAll("input.moka-color-swatch-input").Length);
		Assert.Single(groups[1].QuerySelectorAll("input.moka-color-swatch-input"));
		Assert.Single(Radios(cut).Select(radio => radio.GetAttribute("name")).Distinct());
		Assert.Equal(["#123456"], Checked(cut));
		Assert.NotEqual(groups[0].QuerySelector(".moka-color-swatch-heading")!.Id, groups[1].QuerySelector(".moka-color-swatch-heading")!.Id);
	}

	[Fact]
	public void Groups_PutNoneFirst_AndCustomColorsLast()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Groups, [new MokaSwatchGroup("Theme", ["#ef5350"]), new MokaSwatchGroup("Standard", ["#42a5f5"])])
			.Add(x => x.SelectedColor, "#abcdef")
			.Add(x => x.ShowNone, true)
			.Add(x => x.AllowCustom, true));

		IReadOnlyList<IElement> groups = cut.FindAll(".moka-color-swatch-group");
		Assert.NotNull(groups[0].QuerySelector(".moka-color-swatch-item--none"));
		Assert.NotNull(groups[1].QuerySelector(".moka-color-swatch-item--custom"));
		Assert.NotNull(groups[1].QuerySelector(".moka-color-swatch-add"));
		Assert.Equal(["#abcdef"], Checked(cut));
	}

	[Fact]
	public void Groups_AnUntitledGroupIsAPlainGrid()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Groups, [new MokaSwatchGroup("", ["#ef5350"]), new MokaSwatchGroup("Recent", ["#42a5f5"])]));

		IReadOnlyList<IElement> groups = cut.FindAll(".moka-color-swatch-group");
		Assert.False(groups[0].HasAttribute("role"));
		Assert.Null(groups[0].QuerySelector(".moka-color-swatch-heading"));
		Assert.Equal("group", groups[1].GetAttribute("role"));
	}

	[Fact]
	public void Labels_WinOverGroups()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Labels, MokaLabelColors.Distinct)
			.Add(x => x.Groups, [new MokaSwatchGroup("Theme", ["#ef5350"])]));

		Assert.Empty(cut.FindAll(".moka-color-swatch-group"));
		Assert.Equal(MokaLabelColors.Distinct.Count, Radios(cut).Count);
	}

	// Without the new options the markup keeps its shape: one grid of swatches and the line under it.
	[Fact]
	public void Defaults_DrawOneGrid_WithNoNoneOrHeadings()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.SelectedColor, "#ef5350"));

		Assert.Single(cut.FindAll(".moka-color-swatch-grid"));
		Assert.Empty(cut.FindAll(".moka-color-swatch-group"));
		Assert.Empty(cut.FindAll(".moka-color-swatch-item--none"));
		Assert.Equal("moka-color-swatch", cut.Find(".moka-color-swatch").ClassName);
		Assert.Equal("--moka-swatch-columns: 8; --moka-swatch-size: 24px", cut.Find(".moka-color-swatch").GetAttribute("style"));
		Assert.Single(cut.FindAll(".moka-color-swatch-preview"));
	}
}
