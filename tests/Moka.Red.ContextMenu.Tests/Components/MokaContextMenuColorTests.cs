using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Icons;
using Moka.Red.Tests.Shared;
using static Moka.Red.ContextMenu.Tests.MenuDom;

namespace Moka.Red.ContextMenu.Tests.Components;

// A "Color > Red" menu had no way to show the color: the lit row recolors the icon, so even an icon lost it.
public class MokaContextMenuColorTests : BunitContext
{
	private const string Css = "MokaContextMenu.razor.css";

	public MokaContextMenuColorTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IElement RenderMenu(params MokaContextMenuItem[] items) =>
		Render<MokaContextMenu>(p => p
			.Add(x => x.Items, items)
			.Add(x => x.Visible, true)).Find("[role=menu]");

	// What a row draws before its name: the check, the icon, a swatch or an empty slot.
	private static IElement[] Slots(IElement row) =>
		[.. row.Children.TakeWhile(child => !child.ClassList.Contains("moka-ctx-text"))];

	[Fact]
	public void ALabel_IsASwatchInTheIconsPlace()
	{
		IElement row = Row(RenderMenu(new MokaContextMenuItem { Text = "Red", Label = MokaLabelColor.Red }), "Red");

		IElement slot = Assert.Single(Slots(row));
		Assert.True(slot.ClassList.Contains("moka-ctx-swatch"));
		Assert.True(slot.ClassList.Contains("moka-icon-slot"));
		Assert.Equal("--moka-icon-slot: 14px", slot.GetAttribute("style"));
		Assert.Equal("true", slot.GetAttribute("aria-hidden"));
		Assert.Equal("background-color: var(--moka-label-red)", slot.QuerySelector(".moka-ctx-swatch__dot")!.GetAttribute("style"));
	}

	[Fact]
	public void ColorValue_WinsOverTheLabel()
	{
		IElement row = Row(RenderMenu(new MokaContextMenuItem { Text = "Brand", Label = MokaLabelColor.Red, ColorValue = " #ff8800 " }), "Brand");

		Assert.Equal("background-color: #ff8800", row.QuerySelector(".moka-ctx-swatch__dot")!.GetAttribute("style"));
	}

	[Fact]
	public void ATokenWorksAsAColorValue()
	{
		IElement row = Row(RenderMenu(new MokaContextMenuItem { Text = "Accent", ColorValue = "var(--moka-color-primary)" }), "Accent");

		Assert.Equal("background-color: var(--moka-color-primary)", row.QuerySelector(".moka-ctx-swatch__dot")!.GetAttribute("style"));
	}

	[Theory]
	[InlineData("red; position: fixed")]
	[InlineData("url(x)")]
	[InlineData("12px")]
	public void AColorValueThatIsNotAColor_IsIgnored(string value)
	{
		IElement menu = RenderMenu(
			new MokaContextMenuItem { Text = "Bad", ColorValue = value },
			new MokaContextMenuItem { Text = "Labelled", ColorValue = value, Label = MokaLabelColor.Teal });

		Assert.True(Assert.Single(Slots(Row(menu, "Bad"))).ClassList.Contains("moka-ctx-icon-placeholder"));
		Assert.Equal("background-color: var(--moka-label-teal)",
			Row(menu, "Labelled").QuerySelector(".moka-ctx-swatch__dot")!.GetAttribute("style"));
	}

	[Fact]
	public void ALabelThatIsNoColor_DrawsNone()
	{
		IElement row = Row(RenderMenu(new MokaContextMenuItem { Text = "Odd", Label = (MokaLabelColor)999 }), "Odd");

		Assert.True(Assert.Single(Slots(row)).ClassList.Contains("moka-ctx-icon-placeholder"));
	}

	[Fact]
	public void AnItemWithAnIcon_DrawsTheIconInItsColor()
	{
		IElement row = Row(RenderMenu(new MokaContextMenuItem { Text = "Work", Icon = MokaIcons.Content.Tag, Label = MokaLabelColor.Teal }), "Work");

		IElement icon = Assert.Single(Slots(row));
		Assert.True(icon.ClassList.Contains("moka-ctx-icon"));
		Assert.True(icon.ClassList.Contains("moka-ctx-icon--colored"));
		Assert.EndsWith("color: var(--moka-label-teal)", icon.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Empty(row.QuerySelectorAll(".moka-ctx-swatch"));
	}

	[Fact]
	public void ACheckedColor_ShowsTheCheckBesideIt_AndEveryRowKeepsBothColumns()
	{
		IElement menu = RenderMenu(
			new MokaContextMenuItem { Text = "Red", Label = MokaLabelColor.Red, Checked = true },
			new MokaContextMenuItem { Text = "Green", Label = MokaLabelColor.Green },
			new MokaContextMenuItem { Text = "Tagged", Icon = MokaIcons.Content.Tag },
			new MokaContextMenuItem { Text = "No color", DividerBefore = true });

		IElement[] red = Slots(Row(menu, "Red"));
		Assert.Equal(2, red.Length);
		Assert.True(red[0].ClassList.Contains("moka-ctx-check"));
		Assert.True(red[0].ClassList.Contains("moka-ctx-check--column"));
		Assert.True(red[0].ClassList.Contains("moka-ctx-icon-placeholder"));
		Assert.True(red[1].ClassList.Contains("moka-ctx-swatch"));

		IElement[] green = Slots(Row(menu, "Green"));
		Assert.Equal(["moka-ctx-icon-placeholder", "moka-ctx-swatch"], green.Select(SlotKind));

		Assert.Equal(["moka-ctx-icon-placeholder", "moka-ctx-icon"], Slots(Row(menu, "Tagged")).Select(SlotKind));
		Assert.Equal(["moka-ctx-icon-placeholder", "moka-ctx-icon-placeholder"], Slots(Row(menu, "No color")).Select(SlotKind));
	}

	[Fact]
	public void ColorsWithNothingChecked_KeepOneColumn()
	{
		IElement menu = RenderMenu(
			new MokaContextMenuItem { Text = "Red", Label = MokaLabelColor.Red },
			new MokaContextMenuItem { Text = "No color" });

		Assert.Single(Slots(Row(menu, "Red")));
		Assert.Single(Slots(Row(menu, "No color")));
	}

	// Menus without colors draw what they always did: the check in the icon's place.
	[Fact]
	public void AMenuWithoutColors_PutsTheCheckInTheIconsPlace()
	{
		IElement menu = RenderMenu(
			new MokaContextMenuItem { Text = "Wrap lines", Checked = true, Icon = MokaIcons.Content.Tag },
			new MokaContextMenuItem { Text = "Show hidden" });

		IElement check = Assert.Single(Slots(Row(menu, "Wrap lines")));
		Assert.Equal("moka-ctx-check", check.ClassName);
		Assert.Equal("moka-ctx-icon-placeholder", Assert.Single(Slots(Row(menu, "Show hidden"))).ClassName);
	}

	[Fact]
	public void TheSwatch_IsAToken_RingedRoundDot_ThatForcedColorsLeaveAlone()
	{
		IReadOnlyDictionary<string, string> dot = ScopedCss.Declarations(Css, ".moka-ctx-swatch__dot");

		Assert.Equal("0.75em", dot["width"]);
		Assert.Equal("0.75em", dot["height"]);
		Assert.Equal("var(--moka-radius-full)", dot["border-radius"]);
		Assert.Equal("inset 0 0 0 var(--moka-border-width) color-mix(in srgb, var(--moka-color-on-surface) 14%, transparent)", dot["box-shadow"]);
		Assert.Equal("none", dot["forced-color-adjust"]);
	}

	[Fact]
	public void AColoredIcon_IsNotDimmedLikeTheOthers() =>
		Assert.Equal("1", ScopedCss.Declarations(Css, ".moka-ctx-item ::deep .moka-ctx-icon--colored")["opacity"]);

	private static readonly string[] SlotKinds = ["moka-ctx-check", "moka-ctx-swatch", "moka-ctx-icon", "moka-ctx-icon-placeholder"];

	private static string SlotKind(IElement slot) => SlotKinds.First(slot.ClassList.Contains);
}
