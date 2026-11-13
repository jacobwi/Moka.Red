using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Icons;
using Moka.Red.Primitives.Fab;
using Moka.Red.Primitives.SplitButton;
using Moka.Red.Primitives.Tree;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// The split button, the floating action button and a tree item that loads its children had no busy state. Busy
// controls stay focusable, say so with aria-busy, and draw MokaIcons.Status.Spinner turning in an icon's place.
public sealed class LoadingStateTests : BunitContext
{
	private const string SplitModule = "./_content/Moka.Red.Primitives/SplitButton/MokaSplitButton.razor.js";
	private const string TreeModule = "./_content/Moka.Red.Primitives/Tree/MokaTree.razor.js";

	public LoadingStateTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule(SplitModule);
		JSInterop.SetupModule(TreeModule).SetupVoid("bindTree", _ => true).SetVoidResult();
	}

	[Fact]
	public void SplitButton_Loading_KeepsBothHalvesFocusable_AndSaysTheMainActionIsBusy()
	{
		IRenderedComponent<MokaSplitButton> cut = RenderSplit(loading: true);
		IElement primary = cut.Find(".moka-split-btn__primary");
		IElement toggle = cut.Find(".moka-split-btn__toggle");

		Assert.False(primary.HasAttribute("disabled"));
		Assert.False(toggle.HasAttribute("disabled"));
		Assert.Equal("true", primary.GetAttribute("aria-disabled"));
		Assert.Equal("true", toggle.GetAttribute("aria-disabled"));
		Assert.Equal("true", primary.GetAttribute("aria-busy"));
		Assert.Equal("Loading", primary.QuerySelector("[role=status]")!.TextContent);
		Assert.Contains("moka-spin", primary.QuerySelector(".moka-split-btn__spinner svg")!.ClassList);
		Assert.Equal("Save", primary.QuerySelector(".moka-split-btn__content")!.TextContent);
	}

	[Fact]
	public async Task SplitButton_Loading_RunsNothing_AndKeepsTheMenuShut()
	{
		int clicks = 0;
		IRenderedComponent<MokaSplitButton> cut = RenderSplit(loading: false, onClick: () => clicks++);
		await cut.Find(".moka-split-btn__toggle").ClickAsync(new MouseEventArgs());
		Assert.NotEmpty(cut.FindAll("[role=menu]"));

		cut.Render(p => p.Add(x => x.Loading, true));
		Assert.Empty(cut.FindAll("[role=menu]"));
		Assert.Equal("false", cut.Find(".moka-split-btn__toggle").GetAttribute("aria-expanded"));

		await cut.Find(".moka-split-btn__primary").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-split-btn__toggle").ClickAsync(new MouseEventArgs());
		Assert.Equal(0, clicks);
		Assert.Empty(cut.FindAll("[role=menu]"));

		cut.Render(p => p.Add(x => x.Loading, false));
		await cut.Find(".moka-split-btn__primary").ClickAsync(new MouseEventArgs());
		Assert.Equal(1, clicks);
		Assert.Empty(cut.FindAll("[role=status]"));
	}

	[Fact]
	public void SplitButton_NotLoading_HasNoBusyState()
	{
		IRenderedComponent<MokaSplitButton> cut = RenderSplit(loading: false);

		Assert.Empty(cut.FindAll("[aria-busy], [aria-disabled], [role=status], .moka-split-btn__spinner"));
		Assert.DoesNotContain("moka-split-btn--loading", cut.Find(".moka-split-btn").ClassList);
	}

	[Fact]
	public void SplitButton_Disabled_StaysNativelyDisabled()
	{
		IRenderedComponent<MokaSplitButton> cut = Render<MokaSplitButton>(p => p
			.Add(x => x.Disabled, true)
			.Add(x => x.Loading, true)
			.AddChildContent("Save"));

		Assert.True(cut.Find(".moka-split-btn__primary").HasAttribute("disabled"));
		Assert.False(cut.Find(".moka-split-btn__primary").HasAttribute("aria-disabled"));
	}

	[Fact]
	public void SplitButton_Busy_HalvesLetThePointerThrough_AndTheLabelKeepsItsPlace()
	{
		const string css = "MokaSplitButton.razor.css";

		Assert.Equal("progress", ScopedCss.Declarations(css, ".moka-split-btn--loading")["cursor"]);
		Assert.Equal("none", ScopedCss.Declarations(css, ".moka-split-btn--loading .moka-split-btn__toggle")["pointer-events"]);
		Assert.Equal("0", ScopedCss.Declarations(css, ".moka-split-btn--loading .moka-split-btn__content")["opacity"]);
		Assert.Equal("absolute", ScopedCss.Declarations(css, ".moka-split-btn__spinner")["position"]);
	}

	[Fact]
	public async Task Fab_Loading_ShowsTheSpinnerInTheIconsPlace_AndIgnoresTheClick()
	{
		int clicks = 0;
		IRenderedComponent<MokaFloatingActionButton> cut = Render<MokaFloatingActionButton>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Add)
			.Add(x => x.Label, "New item")
			.Add(x => x.Extended, true)
			.Add(x => x.Loading, true)
			.Add(x => x.OnClick, _ => clicks++));
		IElement button = cut.Find("button");

		Assert.False(button.HasAttribute("disabled"));
		Assert.Equal("true", button.GetAttribute("aria-disabled"));
		Assert.Equal("true", button.GetAttribute("aria-busy"));
		Assert.Equal("New item", button.GetAttribute("aria-label"));
		Assert.Equal(MokaIcons.Status.Spinner.SvgPath, button.QuerySelector(".moka-fab-icon path")!.GetAttribute("d"));
		Assert.Contains("moka-spin", button.QuerySelector(".moka-fab-icon")!.ClassList);
		Assert.Equal("New item", button.QuerySelector(".moka-fab-label")!.TextContent);
		Assert.Equal("Loading", button.QuerySelector("[role=status]")!.TextContent);

		await button.ClickAsync(new MouseEventArgs());
		Assert.Equal(0, clicks);

		cut.Render(p => p.Add(x => x.Loading, false).Add(x => x.LoadingText, "Adding"));
		Assert.Equal(MokaIcons.Action.Add.SvgPath, cut.Find(".moka-fab-icon path").GetAttribute("d"));
		Assert.Empty(cut.FindAll("[role=status], [aria-busy]"));
		await cut.Find("button").ClickAsync(new MouseEventArgs());
		Assert.Equal(1, clicks);
	}

	[Fact]
	public void Fab_LoadingText_IsWhatIsAnnounced()
	{
		IRenderedComponent<MokaFloatingActionButton> cut = Render<MokaFloatingActionButton>(p => p
			.Add(x => x.Icon, MokaIcons.Action.Add)
			.Add(x => x.Loading, true)
			.Add(x => x.LoadingText, "Adding"));

		Assert.Equal("Adding", cut.Find("[role=status]").TextContent);
		Assert.Equal("auto", ScopedCss.Declarations("MokaFloatingActionButton.razor.css", ".moka-fab--loading")["pointer-events"]);
	}

	[Fact]
	public void TreeItem_Loading_ReplacesTheArrowWithASpinner()
	{
		IRenderedComponent<MokaTree> cut = RenderFolder(loading: true, expanded: false);
		IElement toggle = cut.Find(".moka-tree-item__toggle");

		Assert.Equal(MokaIcons.Status.Spinner.SvgPath, toggle.QuerySelector("path")!.GetAttribute("d"));
		Assert.Contains("moka-spin", toggle.QuerySelector("svg")!.ClassList);
		Assert.Equal("false", cut.Find("[role=treeitem]").GetAttribute("aria-expanded"));
		Assert.Empty(cut.FindAll("[role=group]"));
	}

	[Fact]
	public void TreeItem_LoadingWhileExpanded_ShowsABusyGroupWithALoadingRow_ThatIsNotAnItem()
	{
		IRenderedComponent<MokaTree> cut = RenderFolder(loading: true, expanded: true);
		IElement group = cut.Find("[role=group]");

		Assert.Equal("true", group.GetAttribute("aria-busy"));
		IElement row = group.QuerySelector(".moka-tree-item__loading")!;
		Assert.Equal("Fetching", row.TextContent.Trim());
		Assert.Null(row.GetAttribute("role"));
		Assert.Null(row.GetAttribute("tabindex"));
		Assert.Single(cut.FindAll("[role=treeitem]"));
		Assert.Equal("padding-left: 20px", row.GetAttribute("style"));
	}

	[Fact]
	public void TreeItem_LoadedChildren_ReplaceTheLoadingRow()
	{
		IRenderedComponent<MokaTree> cut = RenderFolder(loading: true, expanded: true);

		cut.Render(p => p.AddChildContent<MokaTreeItem>(item => item
			.Add(x => x.Text, "src")
			.Add(x => x.Expanded, true)
			.Add(x => x.Loading, false)
			.AddChildContent<MokaTreeItem>(child => child.Add(x => x.Text, "app.cs"))));

		Assert.Empty(cut.FindAll(".moka-tree-item__loading"));
		Assert.False(cut.Find("[role=group]").HasAttribute("aria-busy"));
		Assert.Equal(2, cut.FindAll("[role=treeitem]").Count);
		Assert.Equal(MokaIcons.Navigation.ChevronRight.SvgPath, cut.Find(".moka-tree-item__toggle path").GetAttribute("d"));
	}

	// A loading item promises children, so with HasChildren unset it draws the arrow (a spinner) even before any arrive.
	[Fact]
	public void TreeItem_Loading_CountsAsHavingChildren_UnlessHasChildrenSaysNo()
	{
		IRenderedComponent<MokaTree> loading = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item.Add(x => x.Text, "src").Add(x => x.Loading, true)));
		IRenderedComponent<MokaTree> leaf = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item.Add(x => x.Text, "src").Add(x => x.Loading, true).Add(x => x.HasChildren, false)));

		Assert.Equal("false", loading.Find("[role=treeitem]").GetAttribute("aria-expanded"));
		Assert.NotNull(loading.Find(".moka-tree-item__toggle"));
		Assert.False(leaf.Find("[role=treeitem]").HasAttribute("aria-expanded"));
		Assert.Contains("moka-spin", leaf.Find(".moka-tree-item__toggle-spacer--loading svg").ClassList);
	}

	[Fact]
	public async Task TreeItem_Loading_StillExpandsAndCollapses()
	{
		bool? expanded = null;
		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "src")
				.Add(x => x.Loading, true)
				.Add(x => x.ExpandedChanged, value => expanded = value)));

		await cut.Find(".moka-tree-item__toggle").ClickAsync(new MouseEventArgs());

		Assert.True(expanded);
		Assert.Equal("true", cut.Find("[role=treeitem]").GetAttribute("aria-expanded"));
		Assert.NotNull(cut.Find("[role=group] .moka-tree-item__loading"));
	}

	[Fact]
	public void TreeItem_NotLoading_KeepsItsMarkup()
	{
		IRenderedComponent<MokaTree> cut = Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item.Add(x => x.Text, "README.md")));

		Assert.Empty(cut.FindAll(".moka-tree-item__toggle, .moka-tree-item__loading, [aria-busy], .moka-spin"));
		Assert.NotNull(cut.Find(".moka-tree-item__toggle-spacer"));
	}

	private IRenderedComponent<MokaSplitButton> RenderSplit(bool loading, Action? onClick = null) =>
		Render<MokaSplitButton>(p => p
			.Add(x => x.Loading, loading)
			.Add(x => x.OnClick, _ => onClick?.Invoke())
			.Add(x => x.DropdownContent, (RenderFragment)Items)
			.AddChildContent("Save"));

	private IRenderedComponent<MokaTree> RenderFolder(bool loading, bool expanded) =>
		Render<MokaTree>(p => p
			.AddChildContent<MokaTreeItem>(item => item
				.Add(x => x.Text, "src")
				.Add(x => x.HasChildren, true)
				.Add(x => x.Expanded, expanded)
				.Add(x => x.Loading, loading)
				.Add(x => x.LoadingText, "Fetching")));

	private static void Items(RenderTreeBuilder b)
	{
		b.OpenElement(0, "div");
		b.AddAttribute(1, "role", "menuitem");
		b.AddContent(2, "Save as");
		b.CloseElement();
	}
}
