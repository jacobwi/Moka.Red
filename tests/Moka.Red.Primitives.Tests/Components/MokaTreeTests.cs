using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Icons;
using Moka.Red.Primitives.Tree;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// The arrow keys and the roving tab stop live in MokaTree.razor.js. These tests cover what the
// components render for it and for screen readers.
public class MokaTreeTests : BunitContext
{
	private const string TreeModule = "./_content/Moka.Red.Primitives/Tree/MokaTree.razor.js";

	[Fact]
	public void Items_ReportTheirStateAsStrings()
	{
		SetupTreeModule();

		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.Add(x => x.Selectable, true)
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "src")
				.Add(x => x.Expanded, true)
				.Add(x => x.Selected, true)
				.AddChildContent<MokaTreeItem>(child => child.Add(x => x.Text, "app.cs")))
			.AddChildContent<MokaTreeItem>(item => item.Add(x => x.Text, "README.md")));

		IReadOnlyList<IElement> items = cut.FindAll("[role=treeitem]");
		Assert.Equal(3, items.Count);
		Assert.Equal("true", items[0].GetAttribute("aria-expanded"));
		Assert.Equal("true", items[0].GetAttribute("aria-selected"));
		Assert.False(items[1].HasAttribute("aria-expanded"));
		Assert.Equal("false", items[1].GetAttribute("aria-selected"));
		Assert.All(items, item => Assert.Equal("-1", item.GetAttribute("tabindex")));
		Assert.Equal("true", cut.Find("[role=tree]").GetAttribute("aria-multiselectable"));
	}

	[Fact]
	public void ACollapsedParent_ReportsFalse()
	{
		SetupTreeModule();

		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "src")
				.AddChildContent<MokaTreeItem>(child => child.Add(x => x.Text, "app.cs"))));

		IElement parent = Assert.Single(cut.FindAll("[role=treeitem]"));
		Assert.Equal("false", parent.GetAttribute("aria-expanded"));
		Assert.False(parent.HasAttribute("aria-selected"));
		Assert.False(cut.Find("[role=tree]").HasAttribute("aria-multiselectable"));
	}

	[Fact]
	public void ToggleButtons_StayOutOfTheTabOrder()
	{
		SetupTreeModule();

		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "src")
				.AddChildContent<MokaTreeItem>(child => child.Add(x => x.Text, "app.cs"))));

		IElement toggle = cut.Find(".moka-tree-item__toggle");
		Assert.Equal("-1", toggle.GetAttribute("tabindex"));
		Assert.Equal("true", toggle.GetAttribute("aria-hidden"));
	}

	[Fact]
	public void TheTree_BindsItsKeyboardHandlingOnce()
	{
		BunitJSModuleInterop module = SetupTreeModule();

		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item.Add(x => x.Text, "README.md")));

		ElementReference root = Assert.IsType<ElementReference>(module.VerifyInvoke("bindTree").Arguments[0]);
		Assert.Equal(root.Id, cut.Find("[role=tree]").GetAttribute("blazor:elementReference"));
	}

	// A disabled item's subtree ignored the mouse (pointer-events on the whole item), but its
	// children took Enter and Space, since only the item itself checked Disabled.
	[Fact]
	public async Task ItemsUnderADisabledItem_AreDisabledToo()
	{
		SetupTreeModule();
		bool childSelected = false;

		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.Add(x => x.Selectable, true)
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "Archive")
				.Add(x => x.Disabled, true)
				.Add(x => x.Expanded, true)
				.AddChildContent<MokaTreeItem>(child => child
					.Add(x => x.Text, "2025.zip")
					.Add(x => x.SelectedChanged, selected => childSelected = selected))));

		IReadOnlyList<IElement> items = cut.FindAll("[role=treeitem]");
		Assert.All(items, item => Assert.Equal("true", item.GetAttribute("aria-disabled")));

		await items[1].QuerySelector(".moka-tree-item__row")!.ClickAsync(new MouseEventArgs());

		Assert.False(childSelected);
		Assert.Equal("false", cut.FindAll("[role=treeitem]")[1].GetAttribute("aria-selected"));
	}

	// A folder whose children load on expand has none to render yet. HasChildren draws its arrow anyway, so the click
	// that loads them can happen. Without it the arrow came and went with the child content.
	[Fact]
	public void HasChildrenTrue_DrawsTheArrow_AndRaisesExpandedChanged_WithNoChildContent()
	{
		SetupTreeModule();
		bool? expanded = null;

		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "Library")
				.Add(x => x.HasChildren, true)
				.Add(x => x.ExpandedChanged, open => expanded = open)));

		Assert.Equal("false", cut.Find("[role=treeitem]").GetAttribute("aria-expanded"));

		cut.Find(".moka-tree-item__toggle").Click();

		Assert.True(expanded);
		Assert.Equal("true", cut.Find("[role=treeitem]").GetAttribute("aria-expanded"));
		Assert.Empty(cut.FindAll("[role=group]"));
	}

	[Fact]
	public void HasChildrenFalse_DrawsNoArrowAndNoChildren_EvenWithChildContent()
	{
		SetupTreeModule();

		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "Library")
				.Add(x => x.HasChildren, false)
				.Add(x => x.Expanded, true)
				.AddChildContent<MokaTreeItem>(child => child.Add(x => x.Text, "a.cs"))));

		IElement item = Assert.Single(cut.FindAll("[role=treeitem]"));
		Assert.False(item.HasAttribute("aria-expanded"));
		Assert.Empty(cut.FindAll(".moka-tree-item__toggle"));
	}

	[Fact]
	public void HasChildrenUnset_DrawsTheArrowOnlyForChildContent()
	{
		SetupTreeModule();

		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "src")
				.AddChildContent<MokaTreeItem>(child => child.Add(x => x.Text, "a.cs")))
			.AddChildContent<MokaTreeItem>(item => item.Add(x => x.Text, "README.md")));

		IReadOnlyList<IElement> items = cut.FindAll("[role=treeitem]");
		Assert.Equal("false", items[0].GetAttribute("aria-expanded"));
		Assert.False(items[1].HasAttribute("aria-expanded"));
		Assert.Single(cut.FindAll(".moka-tree-item__toggle"));
	}

	[Fact]
	public void IconColor_ColorsTheIcon_AndIgnoresWhatIsntAColor()
	{
		SetupTreeModule();

		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item.Add(x => x.Icon, MokaIcons.File.Folder).Add(x => x.IconColor, "#42a5f5"))
			.AddChildContent<MokaTreeItem>(item => item.Add(x => x.Icon, MokaIcons.File.Folder).Add(x => x.IconColor, "red; position: fixed")));

		IReadOnlyList<IElement> icons = cut.FindAll("svg.moka-tree-item__icon");
		Assert.Contains("color: #42a5f5", icons[0].GetAttribute("style"), StringComparison.Ordinal);
		Assert.DoesNotContain("position: fixed", icons[1].GetAttribute("style"), StringComparison.Ordinal);
	}

	// A click on plain content falls through to the row, by CSS. A click on a control stops at the slot.
	[Fact]
	public void EndContent_SitsInTheRow_AndAControlInItDoesntSelectTheRow()
	{
		SetupTreeModule();
		bool? selected = null;
		bool clicked = false;

		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.Add(x => x.Selectable, true)
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "Photos")
				.Add(x => x.SelectedChanged, on => selected = on)
				.Add(x => x.EndContent, (RenderFragment)(builder =>
				{
					builder.OpenElement(0, "button");
					builder.AddAttribute(1, "class", "tag");
					builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, () => clicked = true));
					builder.CloseElement();
				}))));

		IElement end = cut.Find(".moka-tree-item__row > .moka-tree-item__end");
		end.QuerySelector(".tag")!.Click();

		Assert.True(clicked);
		Assert.Null(selected);
		Assert.Equal("none", ScopedCss.Declarations("MokaTreeItem.razor.css", ".moka-tree-item__end")["pointer-events"]);
	}

	[Fact]
	public void WithoutDragParameters_TheRowHasNoDragHandlers()
	{
		SetupTreeModule();

		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item.Add(x => x.Text, "Photos")));

		IElement row = cut.Find(".moka-tree-item__row");
		Assert.False(row.HasAttribute("draggable"));
		Assert.Throws<MissingEventHandlerException>(() => row.Drop());
		Assert.Throws<MissingEventHandlerException>(() => row.DragStart());
	}

	[Fact]
	public void ADraggableRow_RaisesItsDragEvents()
	{
		SetupTreeModule();
		List<string> raised = [];

		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "Photos")
				.Add(x => x.Draggable, true)
				.Add(x => x.OnDragStart, _ => raised.Add("start"))
				.Add(x => x.OnDragEnd, _ => raised.Add("end"))));

		IElement row = cut.Find(".moka-tree-item__row");
		Assert.Equal("true", row.GetAttribute("draggable"));
		row.DragStart();
		row.DragEnd();

		Assert.Equal(["start", "end"], raised);
	}

	[Fact]
	public void ARowWithOnDrop_MarksItselfUnderADrag_CountingItsOwnParts()
	{
		SetupTreeModule();
		int drops = 0;

		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "Photos")
				.Add(x => x.OnDrop, _ => drops++)));

		cut.Find(".moka-tree-item__row").DragEnter();
		cut.Find(".moka-tree-item__row").DragEnter();
		cut.Find(".moka-tree-item__row").DragLeave();
		Assert.Contains("moka-tree-item__row--drop-target", cut.Find(".moka-tree-item__row").ClassList);

		cut.Find(".moka-tree-item__row").Drop();

		Assert.Equal(1, drops);
		Assert.DoesNotContain("moka-tree-item__row--drop-target", cut.Find(".moka-tree-item__row").ClassList);
	}

	[Fact]
	public void ADisabledRow_NeitherDragsNorTakesDrops()
	{
		SetupTreeModule();

		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Disabled, true)
				.Add(x => x.Draggable, true)
				.Add(x => x.OnDrop, _ => { })));

		IElement row = cut.Find(".moka-tree-item__row");
		Assert.False(row.HasAttribute("draggable"));
		Assert.Throws<MissingEventHandlerException>(() => row.Drop());
	}

	private BunitJSModuleInterop SetupTreeModule()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TreeModule);
		module.SetupVoid("bindTree", _ => true).SetVoidResult();
		return module;
	}
}
