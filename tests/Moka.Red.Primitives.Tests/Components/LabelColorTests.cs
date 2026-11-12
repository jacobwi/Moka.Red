using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Icons;
using Moka.Red.Primitives.Chip;
using Moka.Red.Primitives.ColorSwatch;
using Moka.Red.Primitives.List;
using Moka.Red.Primitives.Tag;
using Moka.Red.Primitives.Tree;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// A Label draws a color someone gave a thing from the theme's label tokens, and leaves everything as it was without one.
public class LabelColorTests : BunitContext
{
	private const string Emoji = "<span class=\"app-emoji\">\U0001F4DA</span>";

	public LabelColorTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<MokaVariant> Variants => [.. Enum.GetValues<MokaVariant>()];

	[Fact]
	public void Tag_WithALabel_DrawsItsTintInkAndDot_InPlaceOfTheColor()
	{
		IRenderedComponent<MokaTag> cut = Render<MokaTag>(p => p
			.Add(x => x.Text, "Design")
			.Add(x => x.Label, MokaLabelColor.Teal)
			.Add(x => x.Color, MokaColor.Error));

		IElement tag = cut.Find(".moka-tag");
		Assert.Equal("moka-tag moka-tag--soft moka-tag--label moka-tag--sm", tag.ClassName);
		Assert.Equal(
			"--_tag-label: var(--moka-label-teal); --_tag-label-bg: var(--moka-label-teal-bg); --_tag-label-ink: var(--moka-label-teal-ink)",
			tag.GetAttribute("style"));
		IElement dot = tag.FirstElementChild!;
		Assert.Contains("moka-tag-dot", dot.ClassList);
		Assert.Equal("true", dot.GetAttribute("aria-hidden"));
	}

	[Theory]
	[MemberData(nameof(Variants))]
	public void Tag_KeepsItsVariant_WithALabel(MokaVariant variant)
	{
		IRenderedComponent<MokaTag> cut = Render<MokaTag>(p => p
			.Add(x => x.Text, "Design")
			.Add(x => x.Label, MokaLabelColor.Amber)
			.Add(x => x.Variant, variant));

		Assert.Contains($"moka-tag--{MokaEnumHelpers.ToCssClass(variant)}", cut.Find(".moka-tag").ClassList);
	}

	[Fact]
	public void Tag_WithAnIcon_ShowsTheIconRatherThanADot()
	{
		IRenderedComponent<MokaTag> cut = Render<MokaTag>(p => p
			.Add(x => x.Text, "Design")
			.Add(x => x.Label, MokaLabelColor.Teal)
			.Add(x => x.Icon, MokaIcons.Toggle.Star));

		Assert.Single(cut.FindAll("svg.moka-tag-icon"));
		Assert.Empty(cut.FindAll(".moka-tag-dot"));
	}

	[Fact]
	public void Tag_New_WinsOverALabel()
	{
		IRenderedComponent<MokaTag> cut = Render<MokaTag>(p => p
			.Add(x => x.Text, "New")
			.Add(x => x.New, true)
			.Add(x => x.Label, MokaLabelColor.Teal));

		Assert.DoesNotContain("moka-tag--label", cut.Find(".moka-tag").ClassList);
		Assert.Null(cut.Find(".moka-tag").GetAttribute("style"));
		Assert.Empty(cut.FindAll(".moka-tag-dot"));
	}

	[Fact]
	public void Tag_LabelRules_ReadTheLabelsProperties()
	{
		Assert.Equal("var(--_tag-label-ink)", ScopedCss.Declarations("MokaTag.razor.css", ".moka-tag--label")["color"]);
		Assert.Equal("var(--_tag-label-bg)", ScopedCss.Declarations("MokaTag.razor.css", ".moka-tag--label.moka-tag--soft")["background-color"]);
		Assert.Equal("var(--_tag-label-bg)", ScopedCss.Declarations("MokaTag.razor.css", ".moka-tag--label.moka-tag--filled")["background-color"]);
		Assert.Equal("var(--_tag-label)", ScopedCss.Declarations("MokaTag.razor.css", ".moka-tag--label.moka-tag--outlined")["border-color"]);
		Assert.Equal("var(--_tag-label)", ScopedCss.Declarations("MokaTag.razor.css", ".moka-tag-dot")["background-color"]);
	}

	[Fact]
	public void Chip_WithALabel_DrawsItsTintInkAndDot_InPlaceOfTheColor()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Research")
			.Add(x => x.Label, MokaLabelColor.Purple)
			.Add(x => x.Color, MokaColor.Primary));

		IElement chip = cut.Find(".moka-chip");
		Assert.Contains("moka-chip--label", chip.ClassList);
		Assert.DoesNotContain("moka-chip--primary", chip.ClassList);
		Assert.Contains("moka-chip--soft", chip.ClassList);
		Assert.Equal(
			"--_chip-label: var(--moka-label-purple); --_chip-label-bg: var(--moka-label-purple-bg); --_chip-label-ink: var(--moka-label-purple-ink)",
			chip.GetAttribute("style"));
		Assert.Single(cut.FindAll(".moka-chip__dot[aria-hidden='true']"));
	}

	[Fact]
	public void Chip_ByDefault_KeepsItsColorClassAndNoStyle()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p.Add(x => x.Text, "Research"));

		IElement chip = cut.Find(".moka-chip");
		Assert.Equal("moka-chip moka-chip--soft moka-chip--md moka-chip--surface moka-chip--static", chip.ClassName);
		Assert.Null(chip.GetAttribute("style"));
		Assert.Empty(cut.FindAll(".moka-chip__dot"));
	}

	[Fact]
	public void Chip_Selected_ShowsTheCheckInPlaceOfTheDot()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Research")
			.Add(x => x.Label, MokaLabelColor.Purple)
			.Add(x => x.Selected, true));

		Assert.Single(cut.FindAll(".moka-chip__check"));
		Assert.Empty(cut.FindAll(".moka-chip__dot"));
	}

	[Fact]
	public void Chip_WithAnAvatarOrIcon_HasNoDot()
	{
		IRenderedComponent<MokaChip> avatar = Render<MokaChip>(p => p
			.Add(x => x.Text, "Ada")
			.Add(x => x.Label, MokaLabelColor.Pink)
			.Add(x => x.Avatar, "/ada.png"));
		IRenderedComponent<MokaChip> icon = Render<MokaChip>(p => p
			.Add(x => x.Text, "Star")
			.Add(x => x.Label, MokaLabelColor.Pink)
			.Add(x => x.Icon, MokaIcons.Toggle.Star));

		Assert.Empty(avatar.FindAll(".moka-chip__dot"));
		Assert.Empty(icon.FindAll(".moka-chip__dot"));
	}

	[Fact]
	public void Chip_LabelRules_KeepTheLabelsColorsOnEveryDesign()
	{
		IReadOnlyDictionary<string, string> label = ScopedCss.Declarations("MokaChip.razor.css", ".moka-chip.moka-chip--label");
		Assert.Equal("var(--_chip-label)", label["--_chip-color"]);
		Assert.Equal("var(--_chip-label-ink)", label["color"]);
		Assert.Equal("var(--_chip-label-bg)", ScopedCss.Declarations("MokaChip.razor.css", ".moka-chip--label.moka-chip--soft")["background-color"]);

		IReadOnlyDictionary<string, string> gate = ScopedCss.Declarations("MokaChip.razor.css", ".moka-gate .moka-chip--label.moka-chip--soft");
		Assert.Equal("var(--_chip-label-bg)", gate["background-color"]);
		Assert.Equal("var(--_chip-label-ink)", gate["color"]);
		Assert.Equal("var(--_chip-label)",
			ScopedCss.Declarations("MokaChip.razor.css", ".moka-gate .moka-chip.moka-chip--label.moka-chip--selected")["border-color"]);
	}

	[Fact]
	public void ListItem_WithALabel_ColorsItsIcon()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Photos")
				.Add(x => x.Icon, MokaIcons.File.Folder)
				.Add(x => x.Label, MokaLabelColor.Blue)));

		Assert.Contains("color: var(--moka-label-blue)", cut.Find("svg.moka-list-item__icon").GetAttribute("style"), StringComparison.Ordinal);
		IElement item = cut.Find(".moka-list-item");
		Assert.Contains("moka-list-item--label-icon", item.ClassList);
		Assert.DoesNotContain("moka-list-item--label-tinted", item.ClassList);
		Assert.Empty(cut.FindAll(".moka-list-item__dot"));
	}

	[Fact]
	public void ListItem_WithALabelAndNoIcon_ShowsADotBeforeTheName()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Reading list")
				.Add(x => x.Label, MokaLabelColor.Indigo)));

		IElement dot = cut.Find(".moka-list-item__dot");
		Assert.Equal("background-color: var(--moka-label-indigo)", dot.GetAttribute("style"));
		Assert.Equal("true", dot.GetAttribute("aria-hidden"));
		Assert.Contains("moka-list-item__content", dot.NextElementSibling!.ClassList);
		Assert.DoesNotContain("moka-list-item--label-icon", cut.Find(".moka-list-item").ClassList);
	}

	[Fact]
	public void ListItem_Dot_KeepsTheIconsOwnColor()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Photos")
				.Add(x => x.Icon, MokaIcons.File.Folder)
				.Add(x => x.Label, MokaLabelColor.Blue)
				.Add(x => x.LabelMark, MokaLabelMark.Dot)));

		Assert.DoesNotContain("--moka-label", cut.Find("svg.moka-list-item__icon").GetAttribute("style") ?? "", StringComparison.Ordinal);
		Assert.DoesNotContain("moka-list-item--label-icon", cut.Find(".moka-list-item").ClassList);
		Assert.Single(cut.FindAll(".moka-list-item__dot"));
	}

	[Fact]
	public void ListItem_TintedIcon_MarksTheItemForTheSoftFill()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Photos")
				.Add(x => x.Icon, MokaIcons.File.Folder)
				.Add(x => x.Label, MokaLabelColor.Blue)
				.Add(x => x.LabelMark, MokaLabelMark.TintedIcon)));

		IElement item = cut.Find(".moka-list-item");
		Assert.Contains("moka-list-item--label-icon", item.ClassList);
		Assert.Contains("moka-list-item--label-tinted", item.ClassList);
	}

	[Fact]
	public void ListItem_IconColor_WinsOverTheLabel()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Photos")
				.Add(x => x.Icon, MokaIcons.File.Folder)
				.Add(x => x.IconColor, "#123456")
				.Add(x => x.Label, MokaLabelColor.Blue)));

		string style = cut.Find("svg.moka-list-item__icon").GetAttribute("style")!;
		Assert.Contains("color: #123456", style, StringComparison.Ordinal);
		Assert.DoesNotContain("--moka-label", style, StringComparison.Ordinal);
	}

	[Fact]
	public void ListItem_Label_ReachesTheIconSlot()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Library")
				.Add(x => x.IconContent, Emoji)
				.Add(x => x.Label, MokaLabelColor.Green)));

		Assert.Contains("color: var(--moka-label-green)", cut.Find(".moka-icon-slot").GetAttribute("style"), StringComparison.Ordinal);
		Assert.Empty(cut.FindAll(".moka-list-item__dot"));
	}

	[Fact]
	public void ListItem_WithoutALabel_IsAsItWas()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Photos")
				.Add(x => x.Icon, MokaIcons.File.Folder)));

		Assert.Equal("moka-list-item", cut.Find(".moka-list-item").ClassName);
		Assert.False(cut.Find("svg.moka-list-item__icon").GetAttribute("style")!.Contains("color:", StringComparison.Ordinal));
		Assert.Empty(cut.FindAll(".moka-list-item__dot"));
	}

	[Fact]
	public void ListItem_LabelRules_ShowTheIconFully_AndTintOnlyOutlineIcons()
	{
		Assert.Equal("1", ScopedCss.Declarations("MokaList.razor.css", ".moka-list ::deep .moka-list-item--label-icon > .moka-list-item__icon")["opacity"]);
		Assert.Equal("color-mix(in srgb, currentColor 30%, transparent)",
			ScopedCss.Declarations("MokaList.razor.css",
				".moka-list ::deep .moka-list-item--label-tinted > .moka-list-item__icon:not([fill=\"currentColor\"])")["fill"]);
	}

	[Fact]
	public void TreeItem_WithALabel_ColorsItsIcon_AndNotItsChildren()
	{
		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "Taxes 2026")
				.Add(x => x.Icon, MokaIcons.File.Folder)
				.Add(x => x.Label, MokaLabelColor.Amber)
				.Add(x => x.LabelMark, MokaLabelMark.TintedIcon)
				.Add(x => x.Expanded, true)
				.AddChildContent<MokaTreeItem>(child => child
					.Add(x => x.Text, "Receipts")
					.Add(x => x.Icon, MokaIcons.File.Folder))));

		IReadOnlyList<IElement> rows = cut.FindAll(".moka-tree-item__row");
		Assert.Contains("moka-tree-item__row--label-icon", rows[0].ClassList);
		Assert.Contains("moka-tree-item__row--label-tinted", rows[0].ClassList);
		Assert.DoesNotContain("moka-tree-item__row--label-icon", rows[1].ClassList);

		IReadOnlyList<IElement> icons = cut.FindAll("svg.moka-tree-item__icon");
		Assert.Contains("color: var(--moka-label-amber)", icons[0].GetAttribute("style"), StringComparison.Ordinal);
		Assert.DoesNotContain("--moka-label", icons[1].GetAttribute("style") ?? "", StringComparison.Ordinal);
	}

	[Fact]
	public void TreeItem_WithALabelAndNoIcon_ShowsADotBeforeTheName()
	{
		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "Wedding")
				.Add(x => x.Label, MokaLabelColor.Pink)));

		IElement dot = cut.Find(".moka-tree-item__dot");
		Assert.Equal("background-color: var(--moka-label-pink)", dot.GetAttribute("style"));
		Assert.Contains("moka-tree-item__text", dot.NextElementSibling!.ClassList);
	}

	[Fact]
	public void TreeItem_IconColor_WinsOverTheLabel()
	{
		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "Photos")
				.Add(x => x.Icon, MokaIcons.File.Folder)
				.Add(x => x.IconColor, "var(--moka-color-success)")
				.Add(x => x.Label, MokaLabelColor.Blue)));

		Assert.Contains("color: var(--moka-color-success)", cut.Find("svg.moka-tree-item__icon").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void TreeItem_WithoutALabel_IsAsItWas()
	{
		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "Photos")
				.Add(x => x.Icon, MokaIcons.File.Folder)));

		Assert.Equal("moka-tree-item__row", cut.Find(".moka-tree-item__row").ClassName);
		Assert.Empty(cut.FindAll(".moka-tree-item__dot"));
	}

	[Fact]
	public void TreeItem_LabelRules_ShowTheIconFully()
	{
		Assert.Equal("1", ScopedCss.Declarations("MokaTreeItem.razor.css", ".moka-tree-item__row--label-icon ::deep .moka-tree-item__icon")["opacity"]);
		Assert.Equal("var(--moka-radius-full)", ScopedCss.Declarations("MokaTreeItem.razor.css", ".moka-tree-item__dot")["border-radius"]);
	}

	[Fact]
	public void Swatch_OffersLabelColors_ByName()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p.Add(x => x.Labels, MokaLabelColors.Standard));

		IReadOnlyList<IElement> swatches = cut.FindAll(".moka-color-swatch-input");
		Assert.Equal(13, swatches.Count);
		Assert.Equal("Red", swatches[0].GetAttribute("aria-label"));
		Assert.Equal("Slate", swatches[^1].GetAttribute("aria-label"));
		Assert.Equal("--_swatch-color: var(--moka-label-red)", cut.FindAll(".moka-color-swatch-item")[0].GetAttribute("style"));
		Assert.Empty(cut.FindAll(".moka-color-swatch-add"));
	}

	[Fact]
	public void Swatch_PicksALabel_AndReportsItsName()
	{
		MokaLabelColor? picked = null;
		string? color = null;
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Labels, MokaLabelColors.All)
			.Add(x => x.SelectedLabelChanged, label => picked = label)
			.Add(x => x.SelectedColorChanged, value => color = value));

		cut.Find("[aria-label='Teal']").Change(true);

		Assert.Equal(MokaLabelColor.Teal, picked);
		Assert.Null(color);
		Assert.True(cut.Find("[aria-label='Teal']").HasAttribute("checked"));
		Assert.Equal("Teal", cut.Find(".moka-color-swatch-preview-text").TextContent);
	}

	[Fact]
	public void Swatch_ShowsTheSelectedLabel_AndClearsIt()
	{
		MokaLabelColor? picked = MokaLabelColor.Blue;
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Labels, MokaLabelColors.Distinct)
			.Add(x => x.SelectedLabel, MokaLabelColor.Blue)
			.Add(x => x.AllowClear, true)
			.Add(x => x.SelectedLabelChanged, label => picked = label));

		Assert.True(cut.Find("[aria-label='Blue']").HasAttribute("checked"));

		cut.Find("[aria-label='Blue']").Click();

		Assert.Null(picked);
		Assert.Empty(cut.FindAll(".moka-color-swatch-preview"));
	}

	[Fact]
	public void Swatch_ANewSelectedLabelFromTheParent_ReplacesThePick()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Labels, MokaLabelColors.All)
			.Add(x => x.SelectedLabel, MokaLabelColor.Red));

		cut.Render(p => p.Add(x => x.SelectedLabel, MokaLabelColor.Lime));

		Assert.True(cut.Find("[aria-label='Lime']").HasAttribute("checked"));
		Assert.False(cut.Find("[aria-label='Red']").HasAttribute("checked"));
	}

	[Fact]
	public void Swatch_NamesLabelsInTheAppsWords_AndDisablesSome()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Labels, MokaLabelColors.Distinct)
			.Add(x => x.LabelName, label => label == MokaLabelColor.Red ? "Rouge" : null)
			.Add(x => x.LabelDisabled, label => label == MokaLabelColor.Slate));

		Assert.Single(cut.FindAll("[aria-label='Rouge']"));
		Assert.Single(cut.FindAll("[aria-label='Orange']"));
		Assert.True(cut.Find("[aria-label='Slate']").HasAttribute("disabled"));
		Assert.False(cut.Find("[aria-label='Rouge']").HasAttribute("disabled"));
	}

	[Fact]
	public void Swatch_WithoutLabels_StillTakesColors()
	{
		string? picked = null;
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, ["#111111", "#222222"])
			.Add(x => x.SelectedColorChanged, value => picked = value));

		cut.Find("[aria-label='#222222']").Change(true);

		Assert.Equal("#222222", picked);
	}
}
