using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Data.FileBrowser;

namespace Moka.Red.Data.Tests.Components;

// The views beyond List and Tiles, and grouping. The keys arrive through the script as in the kit.
public class MokaFileBrowserViewTests : MokaFileBrowserKit
{
	private static readonly MokaFileViewMode[] Every =
	[
		MokaFileViewMode.List, MokaFileViewMode.Tiles, MokaFileViewMode.Compact, MokaFileViewMode.Columns, MokaFileViewMode.Gallery,
		MokaFileViewMode.Cards
	];

	private IRenderedComponent<MokaFileBrowser<FileEntry>> RenderView(MokaFileViewMode mode,
		Action<ComponentParameterCollectionBuilder<MokaFileBrowser<FileEntry>>>? extra = null, string folder = "/") =>
		RenderBrowser(p =>
		{
			p.Add(x => x.View, new MokaFileView(mode));
			extra?.Invoke(p);
		}, folder);

	private static string[] Labels(IRenderedComponent<MokaFileBrowser<FileEntry>> cut) =>
		[.. cut.FindAll(".moka-file-browser__modes button").Select(button => button.GetAttribute("aria-label")!)];

	[Fact]
	public void TheSwitch_OffersListAndTiles_ByDefault()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		Assert.Equal(["List", "Tiles"], Labels(cut));
		Assert.Empty(cut.FindAll(".moka-file-browser__preview-control"));
		Assert.Empty(cut.FindAll("[aria-label='Group by']"));
		Assert.Equal("moka-file-browser__list moka-thin-scrollbar", cut.Find("[role=listbox]").GetAttribute("class"));
		Assert.All(cut.FindAll("[role=option]"), option => Assert.False(option.HasAttribute("data-moka-line")));
	}

	[Fact]
	public void Views_ListsWhatTheSwitchOffers_InItsOrder()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Views, Every));

		Assert.Equal(["List", "Tiles", "Compact list", "Columns", "Gallery", "Cards"], Labels(cut));
		Assert.All(cut.FindAll(".moka-file-browser__modes button"), button => Assert.NotNull(button.Closest(".moka-tooltip-trigger")));
	}

	[Theory]
	[InlineData("Compact list", MokaFileViewMode.Compact)]
	[InlineData("Columns", MokaFileViewMode.Columns)]
	[InlineData("Gallery", MokaFileViewMode.Gallery)]
	[InlineData("Cards", MokaFileViewMode.Cards)]
	public void PressingAView_ShowsItAndRaisesIt(string label, MokaFileViewMode mode)
	{
		MokaFileView? raised = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Views, Every)
			.Add(x => x.ViewChanged, view => raised = view));

		Button(cut, label).Click();

		Assert.Equal(mode, raised!.Mode);
		Assert.Equal("true", Button(cut, label).GetAttribute("aria-pressed"));
		Assert.Equal("false", Button(cut, "List").GetAttribute("aria-pressed"));
	}

	[Fact]
	public void AViewSetByTheApp_ShowsEvenWhenNotOffered()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(MokaFileViewMode.Cards);

		Assert.Equal(["List", "Tiles"], Labels(cut));
		Assert.Equal(4, cut.FindAll(".moka-file-browser__card").Count);
	}

	[Fact]
	public async Task Compact_DrawsShortLinesOfIconAndName()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(MokaFileViewMode.Compact);
		await cut.InvokeAsync(() => cut.Instance.HandleColumns(3));

		Assert.Contains("moka-file-browser__list--compact", cut.Find("[role=listbox]").ClassList);
		IReadOnlyList<IElement> lines = cut.FindAll(".moka-file-browser__grid-row");
		Assert.Equal(["0", "1"], lines.Select(line => line.GetAttribute("data-moka-line")));
		Assert.Equal(3, lines[0].QuerySelectorAll("[role=option].moka-file-browser__compact").Length);
		Assert.Empty(cut.FindAll(".moka-file-browser__cell"));
		Assert.Equal(["Docs", "a.txt", "b.txt", "c.txt"], Names(cut));
	}

	[Fact]
	public async Task Compact_ArrowsMoveAcrossAndDownItsLines()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(MokaFileViewMode.Compact);
		await cut.InvokeAsync(() => cut.Instance.HandleColumns(2));
		Click(cut, "Docs");

		Assert.Equal(1, await Key(cut, "ArrowDown"));
		Assert.Equal(["b.txt"], SelectedNames(cut));
		await Key(cut, "ArrowRight");
		Assert.Equal(["c.txt"], SelectedNames(cut));
		Assert.Equal(0, await Key(cut, "ArrowUp"));
		Assert.Equal(["a.txt"], SelectedNames(cut));
	}

	[Fact]
	public void Cards_ShowTheTypeAndSize_UnderTheName()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(MokaFileViewMode.Cards);

		IElement card = Row(cut, "a.txt");
		Assert.Contains("moka-file-browser__card", card.ClassList);
		Assert.Equal("TXT document · 10 B", card.QuerySelector(".moka-file-browser__card-detail")!.TextContent);
		Assert.Equal("Folder", Row(cut, "Docs").QuerySelector(".moka-file-browser__card-detail")!.TextContent);
	}

	[Fact]
	public async Task Gallery_PutsFoldersInChipsFirst_AndFilesUnderThem()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(MokaFileViewMode.Gallery);
		await cut.InvokeAsync(() => cut.Instance.HandleColumns(4));

		IReadOnlyList<IElement> lines = cut.FindAll(".moka-file-browser__grid-row");
		Assert.Contains("moka-file-browser__grid-row--chips", lines[0].ClassList);
		Assert.Single(lines[0].QuerySelectorAll(".moka-file-browser__chip"));
		Assert.Equal(3, lines[1].QuerySelectorAll(".moka-file-browser__shot").Length);
		Assert.Contains("--_fb-chips: 2", cut.Find("[role=listbox]").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void Gallery_KeepsFoldersFirst_WhateverTheView()
	{
		Tree["/"] = [new FileEntry("/a", "a.txt", Size: 1), new FileEntry("/z", "Zed", true)];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.View, new MokaFileView(MokaFileViewMode.Gallery, FoldersFirst: false)));

		Assert.Equal(["Zed", "a.txt"], Names(cut));
	}

	[Fact]
	public void Gallery_ShowsTheAppsThumbnails_AndTypeIconsForTheRest()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(MokaFileViewMode.Gallery, p => p
			.Add(x => x.Thumbnail, entry => entry.Name switch
			{
				"a.txt" => "pictures/a.png",
				"b.txt" => "javascript:alert(1)",
				_ => null
			}));

		Assert.Equal("pictures/a.png", Row(cut, "a.txt").QuerySelector(".moka-file-browser__thumb img")!.GetAttribute("src"));
		Assert.Null(Row(cut, "b.txt").QuerySelector("img"));
		Assert.Equal("TXT", Row(cut, "c.txt").QuerySelector(".moka-file-browser__thumb-tag")!.TextContent);
		Assert.Null(Row(cut, "Docs").QuerySelector(".moka-file-browser__thumb"));
	}

	[Fact]
	public async Task Gallery_ArrowsFindTheItemAboveAndBelow_AcrossChipsAndPictures()
	{
		Tree["/"] =
		[
			new FileEntry("/f1", "F1", true), new FileEntry("/f2", "F2", true), new FileEntry("/f3", "F3", true),
			new FileEntry("/a", "a.txt", Size: 1), new FileEntry("/b", "b.txt", Size: 1), new FileEntry("/c", "c.txt", Size: 1),
			new FileEntry("/d", "d.txt", Size: 1)
		];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(MokaFileViewMode.Gallery);
		await cut.InvokeAsync(() => cut.Instance.HandleColumns(4));
		Click(cut, "F2");

		Assert.Equal(1, await Key(cut, "ArrowDown"));
		Assert.Equal(["F3"], SelectedNames(cut));
		Assert.Equal(2, await Key(cut, "ArrowDown"));
		Assert.Equal(["a.txt"], SelectedNames(cut));
		await Key(cut, "ArrowRight");
		await Key(cut, "ArrowRight");
		await Key(cut, "ArrowRight");
		Assert.Equal(["d.txt"], SelectedNames(cut));
		Assert.Equal(1, await Key(cut, "ArrowUp"));
		Assert.Equal(["F3"], SelectedNames(cut));
		await Key(cut, "ArrowUp");
		Assert.Equal(["F1"], SelectedNames(cut));
		await Key(cut, "ArrowRight");
		Assert.Equal(["F2"], SelectedNames(cut));
		Assert.Equal(1, await Key(cut, "ArrowDown"));
		Assert.Equal(2, await Key(cut, "ArrowDown"));
		Assert.Equal(["a.txt"], SelectedNames(cut));
	}

	[Fact]
	public void Columns_ShowTheFolderAbove_WithTheOneOpenMarked()
	{
		Tree["/"] = [.. Tree["/"], new FileEntry("/pics", "Pics", true)];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(MokaFileViewMode.Columns, folder: "/docs");

		cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".moka-file-browser__up-row")));
		IElement miller = cut.Find(".moka-file-browser__miller");
		Assert.NotNull(miller.QuerySelector(":scope > [role=listbox].moka-file-browser__list--columns"));
		IElement open = cut.Find(".moka-file-browser__up-row--open");
		Assert.Equal("Docs", open.TextContent.Trim());
		Assert.Equal("true", cut.Find(".moka-file-browser__miller-up").GetAttribute("aria-hidden"));
		Assert.Equal(["Sub", "x.md"], Names(cut));
	}

	[Fact]
	public void Columns_AtTheTop_HasNoFolderAbove()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(MokaFileViewMode.Columns);

		Assert.Contains("moka-file-browser__miller--top", cut.Find(".moka-file-browser__miller").ClassList);
		Assert.Empty(cut.FindAll(".moka-file-browser__miller-up"));
	}

	[Fact]
	public void Columns_AFolderAbove_OpensWhenClicked()
	{
		Tree["/"] = [.. Tree["/"], new FileEntry("/pics", "Pics", true)];
		Tree["/pics"] = [new FileEntry("/pics/p.png", "p.png", Size: 5)];
		string? folder = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(MokaFileViewMode.Columns,
			p => p.Add(x => x.FolderChanged, value => folder = value), "/docs");
		cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".moka-file-browser__up-row")));

		cut.FindAll(".moka-file-browser__up-row").Single(row => row.TextContent.Trim() == "Pics").Click();

		Assert.Equal("/pics", folder);
		Assert.DoesNotContain(cut.FindAll(".moka-file-browser__up-row--folder"), row => row.TextContent.Trim() == "a.txt");
	}

	[Fact]
	public async Task Columns_LeftGoesUp_WithTheFolderLeftSelected_AndRightOpens()
	{
		string? folder = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(MokaFileViewMode.Columns,
			p => p.Add(x => x.FolderChanged, value => folder = value), "/docs");
		Click(cut, "Sub");

		await Key(cut, "ArrowLeft");

		Assert.Equal("/", folder);
		Assert.Equal(["Docs"], SelectedNames(cut));

		await Key(cut, "ArrowRight");
		Assert.Equal("/docs", folder);
	}

	[Fact]
	public void Columns_TheLastColumn_ShowsWhatASelectedFolderHolds()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(MokaFileViewMode.Columns);

		Click(cut, "Docs");

		cut.WaitForAssertion(() => Assert.Equal(["Sub", "x.md"],
			cut.FindAll(".moka-file-browser__miller-peek .moka-file-browser__peek-row").Select(row => row.TextContent.Trim())));

		Click(cut, "a.txt");
		Assert.Equal("a.txt", cut.Find(".moka-file-browser__miller-peek .moka-file-browser__peek-name").TextContent);
		Assert.Empty(cut.FindAll(".moka-file-browser__preview-control"));
	}

	[Fact]
	public void GroupByKind_PutsHeadingsWithCountAndSize_OverTheirRows()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.GroupBy, MokaFileGroupBy.Kind));

		IReadOnlyList<IElement> heads = cut.FindAll(".moka-file-browser__group-head");
		Assert.Equal(["Folders", "Documents"], heads.Select(head => head.QuerySelector(".moka-file-browser__group-name")!.TextContent));
		Assert.Equal(["1", "3"], heads.Select(head => head.QuerySelector(".moka-file-browser__group-count")!.TextContent));
		Assert.Null(heads[0].QuerySelector(".moka-file-browser__group-size"));
		Assert.Equal("60 B", heads[1].QuerySelector(".moka-file-browser__group-size")!.TextContent);
		Assert.All(heads, head => Assert.Equal("true", head.GetAttribute("aria-expanded")));
		IElement group = heads[1].ParentElement!;
		Assert.Equal("group", group.GetAttribute("role"));
		Assert.Equal(heads[1].Id, group.GetAttribute("aria-labelledby"));
		Assert.Equal(3, group.QuerySelectorAll("[role=option]").Length);
	}

	[Fact]
	public void Grouping_SortsInsideEachGroup()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.GroupBy, MokaFileGroupBy.Kind)
			.Add(x => x.View, new MokaFileView(SortBy: MokaFileSort.Size, Descending: true)));

		Assert.Equal(["Docs", "b.txt", "c.txt", "a.txt"], Names(cut));
	}

	[Fact]
	public async Task AHeading_FoldsItsGroup_AndTheKeysSkipIt()
	{
		IReadOnlySet<string>? selected = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.GroupBy, MokaFileGroupBy.Kind)
			.Add(x => x.SelectedChanged, value => selected = value));
		Click(cut, "Docs");

		ClickOn(cut.FindAll(".moka-file-browser__group-head")[0]);

		Assert.Equal("false", cut.FindAll(".moka-file-browser__group-head")[0].GetAttribute("aria-expanded"));
		Assert.Equal(["a.txt", "b.txt", "c.txt"], Names(cut));
		Assert.Empty(selected!);
		await Key(cut, "Home");
		Assert.Equal(["a.txt"], SelectedNames(cut));

		ClickOn(cut.FindAll(".moka-file-browser__group-head")[0]);
		Assert.Equal(["Docs", "a.txt", "b.txt", "c.txt"], Names(cut));
	}

	[Fact]
	public void AHeadingClick_DoesntClearTheSelection()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.GroupBy, MokaFileGroupBy.Kind));
		Click(cut, "a.txt");

		ClickOn(cut.FindAll(".moka-file-browser__group-head")[0]);

		Assert.Equal(["a.txt"], SelectedNames(cut));
	}

	[Fact]
	public void GroupByDate_UsesTheLocalDate_OfTheRegisteredClock()
	{
		DateTimeOffset now = new(2026, 11, 10, 15, 0, 0, TimeSpan.Zero);
		Services.AddSingleton<TimeProvider>(new FixedClock(now));
		Tree["/"] =
		[
			new FileEntry("/t", "today.txt", Size: 1, Modified: now.AddHours(-2)),
			new FileEntry("/y", "yesterday.txt", Size: 1, Modified: now.AddDays(-1)),
			new FileEntry("/w", "week.txt", Size: 1, Modified: now.AddDays(-5)),
			new FileEntry("/m", "month.txt", Size: 1, Modified: now.AddDays(-20)),
			new FileEntry("/o", "old.txt", Size: 1, Modified: now.AddDays(-90)),
			new FileEntry("/n", "none.txt", Size: 1)
		];

		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.GroupBy, MokaFileGroupBy.Date));

		Assert.Equal(["Today", "Yesterday", "Last 7 days", "Last 30 days", "Older", "No date"],
			cut.FindAll(".moka-file-browser__group-name").Select(name => name.TextContent));
		Assert.Equal(["today.txt", "yesterday.txt", "week.txt", "month.txt", "old.txt", "none.txt"], Names(cut));
	}

	[Fact]
	public void TheGroupByChoice_ShowsOnlyWhileGrouped_AndRaisesTheChange()
	{
		MokaFileGroupBy? raised = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();
		Assert.Empty(cut.FindAll("[role=radiogroup][aria-label='Group by']"));

		cut.Render(p => p
			.Add(x => x.GroupBy, MokaFileGroupBy.Kind)
			.Add(x => x.GroupByChanged, value => raised = value));
		IElement choice = cut.Find("[role=radiogroup][aria-label='Group by']");
		choice.QuerySelectorAll("input[type=radio]")[1].Change("date");

		Assert.Equal(MokaFileGroupBy.Date, raised);
		Assert.Equal(["No date"], cut.FindAll(".moka-file-browser__group-name").Select(name => name.TextContent));
	}

	[Fact]
	public void TheGroupByChoice_GoesThroughToolbarControl()
	{
		List<MokaFileControls> asked = [];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.GroupBy, MokaFileGroupBy.Kind)
			.Add(x => x.ToolbarControl, context =>
			{
				asked.Add(context.Which);
				return context.Default;
			}));

		Assert.Contains(MokaFileControls.GroupBy, asked);
		cut.Render(p => p.Add(x => x.Controls, MokaFileControls.All & ~MokaFileControls.GroupBy));
		Assert.Empty(cut.FindAll("[role=radiogroup][aria-label='Group by']"));
	}

	[Fact]
	public void Grouping_WinsOverVirtualizing()
	{
		Tree["/"] = [.. Enumerable.Range(0, 250).Select(i => new FileEntry($"/f{i:000}", $"f{i:000}.txt", Size: i))];

		IRenderedComponent<MokaFileBrowser<FileEntry>> plain = RenderBrowser();
		Assert.True(plain.FindAll("[role=option]").Count < 250);

		IRenderedComponent<MokaFileBrowser<FileEntry>> grouped = RenderBrowser(p => p.Add(x => x.GroupBy, MokaFileGroupBy.Kind));
		Assert.Equal(250, grouped.FindAll("[role=option]").Count);
		Assert.All(grouped.FindAll("[role=option]"), option => Assert.NotNull(option.GetAttribute("data-moka-line")));
	}

	[Fact]
	public async Task CompactLines_VirtualizeFrom200()
	{
		Tree["/"] = [.. Enumerable.Range(0, 250).Select(i => new FileEntry($"/f{i:000}", $"f{i:000}.txt", Size: i))];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(MokaFileViewMode.Compact);

		await cut.InvokeAsync(() => cut.Instance.HandleColumns(1));

		Assert.True(cut.FindAll("[role=option]").Count < 250);
		Assert.Equal(249, await Key(cut, "End"));
	}

	[Fact]
	public async Task GroupedTiles_ArrowsCrossTheHeadings()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.GroupBy, MokaFileGroupBy.Kind)
			.Add(x => x.View, new MokaFileView(MokaFileViewMode.Tiles)));
		await cut.InvokeAsync(() => cut.Instance.HandleColumns(2));
		Click(cut, "Docs");

		Assert.Equal(1, await Key(cut, "ArrowDown"));
		Assert.Equal(["a.txt"], SelectedNames(cut));
		Assert.Equal(2, await Key(cut, "ArrowDown"));
		Assert.Equal(["c.txt"], SelectedNames(cut));
		Assert.Equal(3, cut.FindAll(".moka-file-browser__tile-row").Count);
	}

	[Theory]
	[InlineData(MokaFileViewMode.Gallery)]
	[InlineData(MokaFileViewMode.Columns)]
	public void GalleryAndColumns_DontGroup(MokaFileViewMode mode)
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(mode, p => p.Add(x => x.GroupBy, MokaFileGroupBy.Kind));

		Assert.Empty(cut.FindAll(".moka-file-browser__group-head"));
		Assert.Empty(cut.FindAll("[role=radiogroup][aria-label='Group by']"));
	}

	[Fact]
	public void SwitchingViews_WhileGrouped_RedrawsEveryItem()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.GroupBy, MokaFileGroupBy.Kind)
			.Add(x => x.Views, Every)
			.Add(x => x.View, new MokaFileView(MokaFileViewMode.Compact)));

		Button(cut, "List").Click();

		Assert.Equal(4, cut.FindAll("[role=option].moka-file-browser__row").Count);
		Assert.Empty(cut.FindAll(".moka-file-browser__grid-row"));
		Assert.Empty(cut.FindAll(".moka-file-browser__compact"));
	}

	[Fact]
	public async Task BoxSelect_CountsCompactLinesByTheirColumns()
	{
		BunitJSModuleInterop box = BoxModule();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(MokaFileViewMode.Compact, p => p.Add(x => x.BoxSelect, true));

		await cut.InvokeAsync(() => cut.Instance.HandleColumns(3));

		cut.WaitForAssertion(() => Assert.Equal(3, Geometry(box.Invocations["bindBoxSelect"][^1])!["columns"]));
	}

	[Theory]
	[InlineData(MokaFileViewMode.Gallery, null)]
	[InlineData(MokaFileViewMode.List, MokaFileGroupBy.Kind)]
	public void BoxSelect_TakesOnlyWhatIsDrawn_WhereLinesDiffer(MokaFileViewMode mode, MokaFileGroupBy? groupBy)
	{
		BunitJSModuleInterop box = BoxModule();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(mode, p => p
			.Add(x => x.BoxSelect, true)
			.Add(x => x.GroupBy, groupBy ?? MokaFileGroupBy.None));

		Assert.Null(Geometry(box.Invocations["bindBoxSelect"][^1]));
		Assert.Single(cut.FindAll("[data-moka-list] > .moka-box-select"));
	}

	[Fact]
	public void BoxSelect_FindsTheListInsideTheColumns()
	{
		BunitJSModuleInterop box = BoxModule();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderView(MokaFileViewMode.Columns, p => p.Add(x => x.BoxSelect, true));

		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(box.Invocations["bindBoxSelect"][^1].Arguments[2]);
		Assert.EndsWith("> .moka-file-browser__miller > [data-moka-list]", (string?)options["area"], StringComparison.Ordinal);
		Assert.NotNull(cut.Find(".moka-file-browser__miller > [data-moka-list] > .moka-box-select"));
	}

	private BunitJSModuleInterop BoxModule()
	{
		BunitJSModuleInterop box = JSInterop.SetupModule("./_content/Moka.Red.Core/moka-box-select.js");
		box.SetupVoid("bindBoxSelect", _ => true).SetVoidResult();
		box.SetupVoid("unbindBoxSelect", _ => true).SetVoidResult();
		return box;
	}

	private static Dictionary<string, object?>? Geometry(JSRuntimeInvocation bind) =>
		Assert.IsType<Dictionary<string, object?>>(bind.Arguments[2]).GetValueOrDefault("geometry") as Dictionary<string, object?>;

	private sealed class FixedClock(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;

		public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
	}
}
