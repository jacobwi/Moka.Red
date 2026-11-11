using AngleSharp.Dom;
using Bunit;
using Moka.Red.Icons;
using Moka.Red.Primitives.List;
using Moka.Red.Primitives.Stat;
using Moka.Red.Primitives.Tree;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// IconContent takes the icon's place in the icon's own box, so a row of mixed items lines up.
public class IconSlotTests : BunitContext
{
	private const string Emoji = "<span class=\"app-emoji\">\U0001F4DA</span>";

	public IconSlotTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void TreeItem_DrawsTheContentInTheIconsBox_InPlaceOfTheIcon()
	{
		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "Library")
				.Add(x => x.Icon, MokaIcons.File.Folder)
				.Add(x => x.IconContent, Emoji)));

		IElement slot = cut.Find(".moka-tree-item__icon");
		Assert.Contains("moka-icon-slot", slot.ClassList);
		Assert.Equal("true", slot.GetAttribute("aria-hidden"));
		Assert.Contains("--moka-icon-slot: 14px", slot.GetAttribute("style"), StringComparison.Ordinal);
		Assert.NotNull(slot.QuerySelector(".app-emoji"));
		Assert.Empty(cut.FindAll("svg.moka-tree-item__icon"));
	}

	[Fact]
	public void TreeItem_IconColor_ReachesTheSlot()
	{
		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "Library")
				.Add(x => x.IconColor, "var(--moka-color-success)")
				.Add(x => x.IconContent, Emoji)));

		Assert.Contains("color: var(--moka-color-success)", cut.Find(".moka-icon-slot").GetAttribute("style"),
			StringComparison.Ordinal);
	}

	[Fact]
	public void TreeItem_WithoutContent_KeepsItsIcon()
	{
		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "Library")
				.Add(x => x.Icon, MokaIcons.File.Folder)));

		Assert.Single(cut.FindAll("svg.moka-tree-item__icon"));
		Assert.Empty(cut.FindAll(".moka-icon-slot"));
	}

	[Fact]
	public void ListItem_DrawsTheContentInTheIconsBox_InPlaceOfTheIcon()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Reading list")
				.Add(x => x.Icon, MokaIcons.File.Folder)
				.Add(x => x.IconContent, Emoji)));

		IElement slot = cut.Find(".moka-list-item__icon");
		Assert.Contains("moka-icon-slot", slot.ClassList);
		Assert.Equal("true", slot.GetAttribute("aria-hidden"));
		Assert.Contains("--moka-icon-slot: 16px", slot.GetAttribute("style"), StringComparison.Ordinal);
		Assert.NotNull(slot.QuerySelector(".app-emoji"));
		Assert.Empty(cut.FindAll("svg.moka-list-item__icon"));
	}

	[Fact]
	public void ListItem_LinkRow_DrawsTheSlotToo()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.AddChildContent<MokaListItem>(item => item
				.Add(x => x.Text, "Reading list")
				.Add(x => x.Href, "/library/reading")
				.Add(x => x.IconContent, Emoji)));

		Assert.NotNull(cut.Find("a").QuerySelector(".moka-icon-slot .app-emoji"));
	}

	[Fact]
	public void Stat_DrawsTheContentInTheIconsBox_InPlaceOfTheIcon()
	{
		IRenderedComponent<MokaStat> cut = Render<MokaStat>(p => p
			.Add(x => x.Value, "42")
			.Add(x => x.Icon, MokaIcons.File.Folder)
			.Add(x => x.IconContent, Emoji));

		IElement slot = cut.Find(".moka-stat__icon .moka-icon-slot");
		Assert.Contains("--moka-icon-slot: 24px", slot.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal("true", slot.GetAttribute("aria-hidden"));
		Assert.Empty(cut.FindAll(".moka-stat__icon svg"));
	}

	// The slot is global CSS, because what goes in it is the app's markup, out of the components' scope.
	[Fact]
	public void TheSlot_SizesItselfAndItsContentFromOneVariable()
	{
		IReadOnlyDictionary<string, string> slot = ScopedCss.Declarations("moka.css", ".moka-icon-slot");
		Assert.Equal("var(--moka-icon-slot, 1em)", slot["width"]);
		Assert.Equal("var(--moka-icon-slot, 1em)", slot["height"]);
		Assert.Equal("var(--moka-icon-slot, 1em)", slot["font-size"]);
		Assert.Equal("none", slot["flex"]);

		Assert.Equal("100%", ScopedCss.Declarations("moka.css", ".moka-icon-slot > *")["max-width"]);
	}
}
