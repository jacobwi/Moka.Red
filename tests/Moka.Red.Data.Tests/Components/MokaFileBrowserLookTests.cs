using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Data.FileBrowser;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Primitives.Tree;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Components;

// Row styles and the extras an app turns on: preview pane, size bars, type colors, full status bar, path field, tree.
public class MokaFileBrowserLookTests : MokaFileBrowserKit
{
	private const string Css = "MokaFileBrowser.razor.css";

	private readonly BunitJSModuleInterop _popup;

	public MokaFileBrowserLookTests()
	{
		_popup = TopLayerPopup.Setup(JSInterop);
		Module.SetupVoid("openPath", _ => true).SetVoidResult();
		Module.SetupVoid("focusPathEdit", _ => true).SetVoidResult();
	}

	private static string Status(IRenderedComponent<MokaFileBrowser<FileEntry>> cut) =>
		cut.Find(".moka-file-browser__status").TextContent;

	[Fact]
	public void TheDefaults_AddNoneOfTheNewParts()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		Assert.DoesNotContain(cut.Find("section").ClassList, name => name.StartsWith("moka-file-browser--rows", StringComparison.Ordinal));
		Assert.Empty(cut.FindAll(".moka-file-browser__preview, .moka-file-browser__tag, .moka-file-browser__cell--bar, .moka-file-browser__size-bar"));
		Assert.Empty(cut.FindAll(".moka-file-browser__path-well, [role=tree], .moka-file-browser__side"));
		Assert.All(cut.FindAll("[role=option]"), option => Assert.DoesNotContain("moka-file-browser__item--even", option.ClassList));
		Assert.Contains("4 items", Status(cut), StringComparison.Ordinal);
		Assert.Contains("moka-file-browser__crumbs", cut.Find(".moka-file-browser__path > div").ClassList);
	}

	[Theory]
	[InlineData(MokaFileRowStyle.Zebra, "moka-file-browser--rows-zebra")]
	[InlineData(MokaFileRowStyle.Airy, "moka-file-browser--rows-airy")]
	[InlineData(MokaFileRowStyle.Outlined, "moka-file-browser--rows-outlined")]
	[InlineData(MokaFileRowStyle.Terminal, "moka-file-browser--rows-terminal")]
	[InlineData(MokaFileRowStyle.Solid, "moka-file-browser--rows-solid")]
	public void ARowStyle_IsAClassOnTheRoot(MokaFileRowStyle style, string expected)
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.RowStyle, style));

		Assert.Contains(expected, cut.Find("section").ClassList);
		Assert.Contains("moka-file-browser__list--rows", cut.Find("[role=listbox]").ClassList);
	}

	[Fact]
	public void Zebra_MarksEveryOtherRow_ByItsPlace()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.RowStyle, MokaFileRowStyle.Zebra));

		Assert.Equal([false, true, false, true],
			cut.FindAll("[role=option]").Select(option => option.ClassList.Contains("moka-file-browser__item--even")));
	}

	[Fact]
	public void Solid_LetsTheSelectedIconTakeTheTextsColor()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.RowStyle, MokaFileRowStyle.Solid));

		Click(cut, "Docs");
		Assert.DoesNotContain("moka-icon--warning", Row(cut, "Docs").QuerySelector(".moka-icon")!.ClassList);

		Click(cut, "a.txt");
		Assert.Contains("moka-icon--warning", Row(cut, "Docs").QuerySelector(".moka-icon")!.ClassList);
	}

	[Fact]
	public void Terminal_LaysTheListOutLikeLs()
	{
		Tree["/"] = [new FileEntry("/docs", "Docs", true), new FileEntry("/big", "big.bin", Size: 1_400_000), new FileEntry("/a", "a.txt", Size: 10)];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.RowStyle, MokaFileRowStyle.Terminal));

		Assert.Empty(cut.FindAll(".moka-file-browser__head"));
		Assert.Contains("ls -lh Home", cut.Find(".moka-file-browser__term-top").TextContent, StringComparison.Ordinal);
		Assert.Contains("moka-file-browser__list--terminal", cut.Find("[role=listbox]").ClassList);
		Assert.Equal(["-", "10", "1.4M"], cut.FindAll(".moka-file-browser__term-size").Select(size => size.TextContent));
		Assert.Contains("moka-file-browser__term-name--folder", Row(cut, "Docs").QuerySelector(".moka-file-browser__term-name")!.ClassList);
		Assert.Equal("/", Row(cut, "Docs").QuerySelector(".moka-file-browser__term-slash")!.TextContent);
	}

	[Fact]
	public void Terminal_PutsTheAppsColumnsFirst()
	{
		MokaFileColumn<FileEntry>[] columns =
		[
			new(MokaFileSort.Size),
			new("perm", "Mode") { Text = entry => entry.Folder ? "drwxr-xr-x" : "-rw-r--r--" }
		];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.RowStyle, MokaFileRowStyle.Terminal)
			.Add(x => x.Columns, columns));

		IElement row = Row(cut, "a.txt");
		Assert.Equal("-rw-r--r--", row.QuerySelector(".moka-file-browser__term-cell")!.TextContent);
		Assert.Null(row.QuerySelector(".moka-file-browser__term-date"));
	}

	[Fact]
	public void Terminal_TurnsTilesIntoPlainLs()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.RowStyle, MokaFileRowStyle.Terminal)
			.Add(x => x.View, new MokaFileView(MokaFileViewMode.Tiles)));

		IElement list = cut.Find("[role=listbox]");
		Assert.Contains("moka-file-browser__list--ls", list.ClassList);
		Assert.DoesNotContain("moka-file-browser__list--tiles", list.ClassList);
		Assert.Equal(4, cut.FindAll(".moka-file-browser__ls").Count);
		Assert.Empty(cut.FindAll(".moka-file-browser__tile"));
	}

	[Fact]
	public void ThePreviewPane_ShowsTheFolder_WhileNothingIsSelected()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.PreviewPane, true));

		IElement pane = cut.Find("aside.moka-file-browser__preview");
		Assert.Equal("Preview", pane.GetAttribute("aria-label"));
		Assert.Equal("Home", pane.QuerySelector(".moka-file-browser__peek-name")!.TextContent);
		Assert.Equal("4 items, 60 B", pane.QuerySelector(".moka-file-browser__peek-kind")!.TextContent);
	}

	[Fact]
	public void ThePreviewPane_DescribesOneItem_WithTheAppsPicture()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.PreviewPane, true)
			.Add(x => x.Thumbnail, entry => entry.Name == "a.txt" ? "/thumbs/a.png" : null));

		Click(cut, "a.txt");

		IElement pane = cut.Find(".moka-file-browser__preview");
		Assert.Equal("/thumbs/a.png", pane.QuerySelector("img")!.GetAttribute("src"));
		Assert.Equal("TXT document", pane.QuerySelector(".moka-file-browser__peek-kind")!.TextContent);
		Assert.Equal(["Size", "10 B"], pane.QuerySelectorAll("dt, dd").Select(fact => fact.TextContent));
	}

	[Fact]
	public void ThePreviewPane_CountsWhatAFolderHolds_ReadThroughLoad()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.PreviewPane, true));

		Click(cut, "Docs");

		cut.WaitForAssertion(() => Assert.Equal(["Items", "2"],
			cut.Find(".moka-file-browser__preview").QuerySelectorAll("dt, dd").Select(fact => fact.TextContent)));
	}

	[Fact]
	public void ThePreviewPane_AddsUpSeveralItems()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.PreviewPane, true));

		Click(cut, "a.txt");
		Click(cut, "b.txt", ctrl: true);

		IElement pane = cut.Find(".moka-file-browser__preview");
		Assert.Equal("2 selected", pane.QuerySelector(".moka-file-browser__peek-name")!.TextContent);
		Assert.Equal("40 B in all", pane.QuerySelector(".moka-file-browser__peek-kind")!.TextContent);
	}

	[Fact]
	public void ThePreviewTemplate_ReplacesTheBrowsersOwn()
	{
		RenderFragment<FileEntry> preview = entry => builder => builder.AddMarkupContent(0, $"<p class=\"mine\">{entry.Name}</p>");
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.PreviewPane, true)
			.Add(x => x.Preview, preview));

		Click(cut, "c.txt");

		Assert.Equal("c.txt", cut.Find(".moka-file-browser__preview .mine").TextContent);
		Assert.Empty(cut.FindAll(".moka-file-browser__preview .moka-file-browser__peek"));
	}

	[Fact]
	public void ThePreviewButton_HidesThePane_AndSaysSo()
	{
		bool? open = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.PreviewPane, true)
			.Add(x => x.PreviewOpenChanged, value => open = value));
		IElement toggle = cut.Find(".moka-file-browser__preview-toggle button");
		Assert.Equal("Preview", toggle.GetAttribute("aria-label"));
		Assert.Equal("true", toggle.GetAttribute("aria-pressed"));

		toggle.Click();

		Assert.False(open);
		Assert.Empty(cut.FindAll("aside.moka-file-browser__preview"));
		Assert.Equal("false", cut.Find(".moka-file-browser__preview-toggle button").GetAttribute("aria-pressed"));
		Assert.NotNull(cut.Find(".moka-file-browser__preview-toggle button").Closest(".moka-tooltip-trigger"));
	}

	[Fact]
	public void PreviewOpen_StartsThePaneHidden()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.PreviewPane, true)
			.Add(x => x.PreviewOpen, false));

		Assert.Empty(cut.FindAll("aside.moka-file-browser__preview"));
	}

	[Fact]
	public void ThePreviewButton_GoesThroughToolbarControl_WithARun()
	{
		MokaFileControlContext? asked = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.PreviewPane, true)
			.Add(x => x.ToolbarControl, context =>
			{
				if (context.Which == MokaFileControls.Preview)
				{
					asked = context;
				}

				return context.Default;
			}));

		Assert.NotNull(asked?.Run);
		cut.InvokeAsync(asked!.Run!);
		Assert.Empty(cut.FindAll("aside.moka-file-browser__preview"));
	}

	[Fact]
	public void SizeBars_MeasureEachFile_AgainstTheLargest()
	{
		CultureInfo before = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
		try
		{
			IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.SizeBars, true));

			IElement a = Row(cut, "a.txt").QuerySelector(".moka-file-browser__cell--bar")!;
			Assert.Contains("--_fb-share: 0.333", a.GetAttribute("style"), StringComparison.Ordinal);
			Assert.Contains("--_fb-share: 1", Row(cut, "b.txt").QuerySelector(".moka-file-browser__cell--bar")!.GetAttribute("style"), StringComparison.Ordinal);
			Assert.Null(Row(cut, "Docs").QuerySelector(".moka-file-browser__cell--bar"));
		}
		finally
		{
			CultureInfo.CurrentCulture = before;
		}
	}

	[Fact]
	public void SizeBars_DrawAThinBar_InTiles()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.SizeBars, true)
			.Add(x => x.View, new MokaFileView(MokaFileViewMode.Tiles)));

		Assert.Equal(3, cut.FindAll(".moka-file-browser__tile .moka-file-browser__size-bar").Count);
		Assert.Equal("true", cut.Find(".moka-file-browser__size-bar").GetAttribute("aria-hidden"));
	}

	[Fact]
	public void TypeColors_TagEachFile_InItsTone()
	{
		Tree["/"] =
		[
			new FileEntry("/docs", "Docs", true), new FileEntry("/p", "photo.png", Size: 1), new FileEntry("/r", "report.pdf", Size: 1),
			new FileEntry("/l", "build.log", Size: 1), new FileEntry("/n", "notes.md", Size: 1)
		];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.TypeColors, true));

		Assert.Null(Row(cut, "Docs").QuerySelector(".moka-file-browser__tag"));
		AssertTag(Row(cut, "photo.png"), "PNG", "success");
		AssertTag(Row(cut, "report.pdf"), "PDF", "error");
		AssertTag(Row(cut, "build.log"), "LOG", "neutral");
		AssertTag(Row(cut, "notes.md"), "MD", "info");
		Assert.Contains("moka-icon--success", Row(cut, "photo.png").QuerySelector(".moka-icon")!.ClassList);
	}

	[Fact]
	public void TypeColors_FollowTheAppsOwnMapping()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.TypeColors, true)
			.Add(x => x.FileType, entry => entry.Name == "b.txt" ? new MokaFileType(MokaFileCategory.Code, "MINE", MokaColor.Warning) : null));

		AssertTag(Row(cut, "b.txt"), "MINE", "warning");
		AssertTag(Row(cut, "a.txt"), "TXT", "info");
	}

	[Fact]
	public void FileType_AlsoDecidesTheKindHeadings()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.GroupBy, MokaFileGroupBy.Kind)
			.Add(x => x.FileType, entry => entry.Name == "b.txt" ? new MokaFileType(MokaFileCategory.Code) : null));

		Assert.Equal(["Folders", "Documents", "Code"], cut.FindAll(".moka-file-browser__group-name").Select(name => name.TextContent));
	}

	private static void AssertTag(IElement row, string text, string tone)
	{
		IElement tag = row.QuerySelector(".moka-file-browser__tag")!;
		Assert.Equal(text, tag.TextContent);
		Assert.Contains($"moka-file-browser__tag--{tone}", tag.ClassList);
	}

	[Fact]
	public void TheFullStatus_CountsFoldersAndFilesApart_WithTheFolderSize()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.FullStatus, true));

		string status = Status(cut);
		Assert.Contains("1 folder, 3 files", status, StringComparison.Ordinal);
		Assert.Contains("60 B", status, StringComparison.Ordinal);
		Assert.Contains("Nothing selected", status, StringComparison.Ordinal);
	}

	[Fact]
	public void TheFullStatus_GivesTheSelectionsShareOfTheFolder()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.FullStatus, true));

		Click(cut, "b.txt");

		string status = Status(cut);
		Assert.Contains("b.txt", status, StringComparison.Ordinal);
		Assert.Contains("30 B", status, StringComparison.Ordinal);
		Assert.Contains("50% of this folder", status, StringComparison.Ordinal);
		Assert.Contains("width: 50%", cut.Find(".moka-file-browser__meter-fill").GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal("true", cut.Find(".moka-file-browser__meter").GetAttribute("aria-hidden"));
	}

	[Fact]
	public void TheFullStatus_CountsWhatASelectedFolderHolds()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.FullStatus, true));

		Click(cut, "Docs");

		cut.WaitForAssertion(() => Assert.Contains("2 items", Status(cut), StringComparison.Ordinal));
	}

	[Fact]
	public void ThePathWell_HasANamedButtonInATooltip()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.PathField, true));

		IElement edit = cut.Find(".moka-file-browser__path-well .moka-file-browser__path-edit");
		Assert.Equal("Type a path", edit.GetAttribute("aria-label"));
		Assert.False(edit.HasAttribute("title"));
		Assert.Contains(cut.FindComponents<MokaTooltip>(), tooltip => tooltip.Instance.Text == "Type a path");
		Assert.NotEmpty(cut.FindAll(".moka-file-browser__path-well .moka-file-browser__crumbs"));
	}

	[Fact]
	public void ThePathField_OpensWithThePath_AndOffersTheFoldersInside()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.PathField, true), "/docs");

		ClickOn(cut.Find(".moka-file-browser__path-edit"));

		IElement input = cut.Find("input[data-moka-file-path]");
		Assert.Equal("Home/docs/", input.GetAttribute("value"));
		Assert.Equal("combobox", input.GetAttribute("role"));
		Assert.Equal("true", input.GetAttribute("aria-expanded"));
		cut.WaitForAssertion(() => Assert.Equal(["Docs/Sub/"], cut.FindAll(".moka-file-browser__path-option").Select(option => option.TextContent.Trim())));
		IElement panel = cut.Find(".moka-file-browser__path-suggest");
		Assert.Equal(input.GetAttribute("aria-controls"), panel.Id);
		Assert.Equal("manual", panel.GetAttribute("popover"));
		Module.VerifyInvoke("openPath");
		IReadOnlyDictionary<string, object?> options = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(_popup.Invocations["open"][^1].Arguments[4]);
		Assert.Equal("bottom-start", options["placement"]);
	}

	[Fact]
	public async Task ThePathField_ArrowsLightAFolder_AndEnterGoesThere()
	{
		string? folder = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.PathField, true)
			.Add(x => x.FolderChanged, value => folder = value), "/docs");
		ClickOn(cut.Find(".moka-file-browser__path-edit"));
		cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".moka-file-browser__path-option")));

		await cut.InvokeAsync(() => cut.Instance.HandlePathKey("ArrowDown", "Home/docs/"));

		IElement option = cut.Find(".moka-file-browser__path-option");
		Assert.Equal("true", option.GetAttribute("aria-selected"));
		Assert.Equal(option.Id, cut.Find("input[data-moka-file-path]").GetAttribute("aria-activedescendant"));

		await cut.InvokeAsync(() => cut.Instance.HandlePathKey("Enter", "Home/docs/"));

		Assert.Equal("/docs/sub", folder);
		Assert.Empty(cut.FindAll("input[data-moka-file-path]"));
		_popup.VerifyInvoke("close");
	}

	[Fact]
	public async Task ThePathField_GoesToATypedPath_FolderByFolder()
	{
		string? folder = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.PathField, true)
			.Add(x => x.FolderChanged, value => folder = value));
		ClickOn(cut.Find(".moka-file-browser__path-edit"));
		Type(cut.Find("input[data-moka-file-path]"), "home/DOCS/");
		cut.WaitForAssertion(() => Assert.Equal(["Docs/Sub/"], cut.FindAll(".moka-file-browser__path-option").Select(o => o.TextContent.Trim())));

		await cut.InvokeAsync(() => cut.Instance.HandlePathKey("Enter", "home/DOCS/sub"));

		Assert.Equal("/docs/sub", folder);
	}

	[Fact]
	public async Task ThePathField_SaysSo_WhenNoFolderHasTheName()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.PathField, true));
		ClickOn(cut.Find(".moka-file-browser__path-edit"));
		Type(cut.Find("input[data-moka-file-path]"), "Home/nowhere");

		await cut.InvokeAsync(() => cut.Instance.HandlePathKey("Enter", "Home/nowhere"));

		Assert.Equal("true", cut.Find("input[data-moka-file-path]").GetAttribute("aria-invalid"));
		Assert.Equal("No folder by that name", cut.Find(".moka-file-browser__path-none").TextContent);
	}

	[Fact]
	public async Task ThePathField_TabTakesTheLitFolder()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.PathField, true));
		ClickOn(cut.Find(".moka-file-browser__path-edit"));
		await cut.InvokeAsync(() => cut.Instance.HandlePathKey("ArrowDown", "Home/"));

		await cut.InvokeAsync(() => cut.Instance.HandlePathKey("Tab", "Home/"));

		Assert.Equal("Home/Docs/", cut.Find("input[data-moka-file-path]").GetAttribute("value"));
	}

	[Fact]
	public async Task EscapeAndLeaving_PutTheCrumbsBack()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.PathField, true));
		ClickOn(cut.Find(".moka-file-browser__path-edit"));

		await cut.InvokeAsync(() => cut.Instance.HandlePathKey("Escape", "Home/"));

		Assert.NotEmpty(cut.FindAll(".moka-file-browser__path-well"));
		Module.VerifyInvoke("focusPathEdit");

		ClickOn(cut.Find(".moka-file-browser__path-edit"));
		await cut.InvokeAsync(cut.Instance.HandlePathLeft);
		Assert.Empty(cut.FindAll("input[data-moka-file-path]"));
	}

	[Fact]
	public void TheFolderTree_IsBuiltFromThePath_AndTheFoldersRead()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.FolderTree, true), "/docs");

		cut.WaitForAssertion(() => Assert.Equal(["Home", "Docs", "Sub"],
			cut.FindComponents<MokaTreeItem>().Select(item => item.Instance.Text)));
		IElement side = cut.Find("aside.moka-file-browser__side");
		Assert.Equal("Folders", side.GetAttribute("aria-label"));
		Assert.Equal("Folders", side.QuerySelector("[role=tree]")!.GetAttribute("aria-label"));
		IElement docs = cut.Find("[role=treeitem][data-moka-file-target='/docs']");
		Assert.Equal("true", docs.GetAttribute("aria-selected"));
		Assert.Equal("Folders", Button(cut, "Folders").GetAttribute("aria-label"));
	}

	[Fact]
	public async Task PickingAFolderInTheTree_GoesThere()
	{
		string? folder = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.FolderTree, true)
			.Add(x => x.FolderChanged, value => folder = value), "/docs");
		cut.WaitForAssertion(() => Assert.Equal(3, cut.FindComponents<MokaTreeItem>().Count));

		IRenderedComponent<MokaTreeItem> home = cut.FindComponents<MokaTreeItem>().Single(item => item.Instance.Text == "Home");
		await cut.InvokeAsync(() => home.Instance.SelectedChanged.InvokeAsync(true));

		Assert.Equal("/", folder);
		Assert.Equal(["Docs", "a.txt", "b.txt", "c.txt"], Names(cut));
	}

	[Fact]
	public async Task OpeningABranch_ReadsItsFolders()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.FolderTree, true));
		cut.WaitForAssertion(() => Assert.Equal(2, cut.FindComponents<MokaTreeItem>().Count));

		IRenderedComponent<MokaTreeItem> docs = cut.FindComponents<MokaTreeItem>().Single(item => item.Instance.Text == "Docs");
		await cut.InvokeAsync(() => docs.Instance.ExpandedChanged.InvokeAsync(true));

		cut.WaitForAssertion(() => Assert.Contains("Sub", cut.FindComponents<MokaTreeItem>().Select(item => item.Instance.Text)));
	}

	[Fact]
	public void TheTree_AndTheAppsSidePane_ShareTheSide()
	{
		RenderFragment side = builder => builder.AddMarkupContent(0, "<p class=\"places\">Places</p>");
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.FolderTree, true)
			.Add(x => x.Side, side));

		IElement aside = cut.Find("aside.moka-file-browser__side");
		Assert.NotNull(aside.QuerySelector(":scope > [role=tree]"));
		Assert.NotNull(aside.QuerySelector(":scope > .moka-file-browser__side-more > .places"));
		Assert.Equal("Places", aside.GetAttribute("aria-label"));
	}

	[Fact]
	public void TheStylesheet_HidesTheFolderAbove_AndThePreview_WhenNarrow()
	{
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-file-browser__miller-up")["display"]);
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-file-browser__preview")["display"]);
		Assert.Equal("inline-flex", ScopedCss.Declarations(Css, ".moka-file-browser__preview-open")["display"]);
	}

	[Fact]
	public void TheStylesheet_DrawsEachRowStyle_FromTokens()
	{
		Assert.Equal("var(--moka-color-primary)",
			ScopedCss.Declarations(Css, ".moka-file-browser--rows-solid [role=option][aria-selected=true]")["background"]);
		Assert.Contains("var(--moka-color-primary)",
			ScopedCss.Declarations(Css, ".moka-file-browser--rows-outlined [role=option][aria-selected=true]")["box-shadow"], StringComparison.Ordinal);
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-file-browser--rows-airy [role=option][aria-selected=true]")["box-shadow"]);
		Assert.Contains("var(--_fb-share, 0)", ScopedCss.Declarations(Css, ".moka-file-browser__cell--bar")["background"], StringComparison.Ordinal);
		Assert.Equal("10.5", ScopedCss.Declarations(Css, ".moka-file-browser__list--compact")["--_fb-tile-width"]);
	}

	[Fact]
	public void TheScript_TakesThePathFieldsKeys()
	{
		string script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaFileBrowser.razor.js"));

		Assert.Contains("e.target.matches('[data-moka-file-path]')", script, StringComparison.Ordinal);
		Assert.Contains("'HandlePathKey'", script, StringComparison.Ordinal);
		Assert.Contains("'HandlePathLeft'", script, StringComparison.Ordinal);
		Assert.Contains("[data-moka-line=\"${index}\"]", script, StringComparison.Ordinal);
	}
}
