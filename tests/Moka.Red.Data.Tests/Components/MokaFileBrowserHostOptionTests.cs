using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Data.FileBrowser;

namespace Moka.Red.Data.Tests.Components;

public class MokaFileBrowserHostOptionTests : MokaFileBrowserKit
{
	private static readonly IReadOnlyList<MokaFileColumn<FileEntry>> OwnColumns =
	[
		new("owner", "Owner") { Text = entry => entry.Name, Fit = MokaFileColumnFit.Medium },
		new("hash", "Hash") { Text = entry => entry.Key, Mono = true },
		new("left", "Left") { Text = entry => entry.Key, Mono = true, Align = MokaTextAlign.Left },
		new("right", "Right") { Text = entry => entry.Key, Align = MokaTextAlign.Right },
		new("middle", "Middle") { Text = entry => entry.Key, Align = MokaTextAlign.Center }
	];

	private static string RootAttribute(IRenderedComponent<MokaFileBrowser<FileEntry>> cut, string name) =>
		cut.Find("[data-moka-file-browser]").GetAttribute(name) ?? "";

	private static IElement Heading(IRenderedComponent<MokaFileBrowser<FileEntry>> cut, string title) =>
		cut.FindAll(".moka-file-browser__heading").Single(heading => heading.TextContent.Contains(title, StringComparison.Ordinal));

	private static IElement Edge(IRenderedComponent<MokaFileBrowser<FileEntry>> cut, string column) =>
		cut.Find($"[data-moka-column='{column}']");

	private static string MainStyle(IRenderedComponent<MokaFileBrowser<FileEntry>> cut) =>
		cut.Find(".moka-file-browser__main").GetAttribute("style") ?? "";

	private static string[] CellStyles(IRenderedComponent<MokaFileBrowser<FileEntry>> cut, string name) =>
		[.. Row(cut, name).QuerySelectorAll(".moka-file-browser__cell").Select(cell => cell.GetAttribute("style") ?? "")];

	private static string ButtonLabeledBack(IRenderedComponent<MokaFileBrowser<FileEntry>> cut) =>
		Button(cut, "Back").HasAttribute("disabled") ? "disabled" : "enabled";

	[Fact]
	public void TheHeadRow_LeavesRoomForTheScrollbar_ThroughTheVariableTheScriptSets()
	{
		string script = File.ReadAllText(ScriptPath());
		string styles = File.ReadAllText(Path.ChangeExtension(ScriptPath(), ".css"));

		Assert.Contains("padding-inline: var(--moka-spacing-md) calc(var(--moka-spacing-md) + var(--moka-file-browser-gutter, 0px))", styles, StringComparison.Ordinal);
		Assert.Contains("head.style.setProperty('--moka-file-browser-gutter'", script, StringComparison.Ordinal);
		Assert.Contains("head.style.removeProperty('--moka-file-browser-gutter')", script, StringComparison.Ordinal);
		Assert.Contains("entry.borderBoxSize[0].inlineSize - entry.contentBoxSize[0].inlineSize", script, StringComparison.Ordinal);
	}

	[Fact]
	public void ByDefault_TheHeadRowHasNoStyleOfItsOwn_ForTheScriptToKeep() =>
		Assert.Null(RenderBrowser().Find(".moka-file-browser__head").GetAttribute("style"));

	[Fact]
	public void ByDefault_ColumnsHaveNoEdges_AndTheirWidthsAreLiteral()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		Assert.Empty(cut.FindAll("[data-moka-column]"));
		Assert.Empty(cut.FindAll(".moka-file-browser__heading-wrap"));
		Assert.Equal("flex-basis: 6rem", Heading(cut, "Size").GetAttribute("style"));
		Assert.Equal(["flex-basis: 6rem", "flex-basis: 9rem"], CellStyles(cut, "a.txt"));
		Assert.Empty(MainStyle(cut));
	}

	[Fact]
	public void ResizableColumns_GivesEachColumnAnEdgeNamedForItsTitle()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.ResizableColumns, true));

		IElement edge = Edge(cut, MokaFileSort.Size);
		Assert.Equal("separator", edge.GetAttribute("role"));
		Assert.Equal("0", edge.GetAttribute("tabindex"));
		Assert.Equal("vertical", edge.GetAttribute("aria-orientation"));
		Assert.Equal("Resize Size", edge.GetAttribute("aria-label"));
		Assert.Equal("--_fb-col-0", edge.GetAttribute("data-moka-column-var"));
		Assert.Equal("50", edge.GetAttribute("data-moka-column-min"));
		Assert.Equal(2, cut.FindAll("[data-moka-column]").Count);
		Assert.Empty(cut.FindAll(".moka-file-browser__heading [data-moka-column]"));
	}

	[Fact]
	public void AResizableColumn_ReadsItsVariable_WithItsOwnWidthBehindIt()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.ResizableColumns, true));

		Assert.Equal(["flex-basis: var(--_fb-col-0, 6rem)", "flex-basis: var(--_fb-col-1, 9rem)"], CellStyles(cut, "a.txt"));
		Assert.Equal("flex-basis: var(--_fb-col-0, 6rem)", Edge(cut, MokaFileSort.Size).ParentElement!.GetAttribute("style"));
	}

	[Fact]
	public void TheNameColumn_HasNoEdge_SoItKeepsTakingWhatIsLeft()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.ResizableColumns, true));

		Assert.Empty(cut.FindAll(".moka-file-browser__name-cell [data-moka-column]"));
		Assert.Null(cut.Find(".moka-file-browser__head .moka-file-browser__name-cell").GetAttribute("style"));
	}

	[Fact]
	public async Task AResizedColumn_WritesItsVariable_AndTellsTheApp()
	{
		IReadOnlyDictionary<string, double>? told = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ResizableColumns, true)
			.Add(x => x.ColumnWidthsChanged, widths => told = widths));

		await cut.InvokeAsync(() => cut.Instance.HandleColumnResized(MokaFileSort.Modified, 200.5));

		Assert.Equal("--_fb-col-1: 200.5px", MainStyle(cut));
		Assert.Equal(200.5, told![MokaFileSort.Modified]);
	}

	[Fact]
	public async Task AResizedColumn_StopsAtTheMinimum()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ResizableColumns, true)
			.Add(x => x.MinColumnWidth, 90d));

		await cut.InvokeAsync(() => cut.Instance.HandleColumnResized(MokaFileSort.Size, 10));

		Assert.Equal("--_fb-col-0: 90px", MainStyle(cut));
		Assert.Equal("90", Edge(cut, MokaFileSort.Size).GetAttribute("data-moka-column-min"));
	}

	[Fact]
	public void WidthsHandedBack_AreApplied_ThroughTheColumnsIds()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ResizableColumns, true)
			.Add(x => x.ColumnWidths, new Dictionary<string, double> { [MokaFileSort.Modified] = 150, ["gone"] = 300 }));

		Assert.Equal("--_fb-col-1: 150px", MainStyle(cut));
	}

	[Fact]
	public void WidthsHandedBack_DoNothing_WithoutResizing()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ColumnWidths, new Dictionary<string, double> { [MokaFileSort.Size] = 150 }));

		Assert.Empty(MainStyle(cut));
		Assert.Equal("flex-basis: 6rem", Heading(cut, "Size").GetAttribute("style"));
	}

	[Fact]
	public async Task AWidthTheUserGave_SurvivesAParentRenderWithoutAnyBinding()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.ResizableColumns, true));
		await cut.InvokeAsync(() => cut.Instance.HandleColumnResized(MokaFileSort.Size, 140));

		cut.Render(p => p.Add(x => x.Height, "20rem"));

		Assert.Equal("--_fb-col-0: 140px", MainStyle(cut));
	}

	[Fact]
	public async Task ANewSetOfWidths_ReplacesWhatWasDragged()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.ResizableColumns, true));
		await cut.InvokeAsync(() => cut.Instance.HandleColumnResized(MokaFileSort.Size, 140));

		cut.Render(p => p.Add(x => x.ColumnWidths, new Dictionary<string, double>()));

		Assert.Empty(MainStyle(cut));
	}

	[Fact]
	public async Task ADoubleClickOnTheEdge_GivesTheColumnItsOwnWidthBack()
	{
		IReadOnlyDictionary<string, double>? told = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ResizableColumns, true)
			.Add(x => x.ColumnWidthsChanged, widths => told = widths));
		await cut.InvokeAsync(() => cut.Instance.HandleColumnResized(MokaFileSort.Size, 140));

		await cut.InvokeAsync(() => cut.Instance.HandleColumnReset(MokaFileSort.Size));

		Assert.Empty(MainStyle(cut));
		Assert.Empty(told!);
	}

	[Fact]
	public async Task AnUnknownColumn_IsIgnored()
	{
		int told = 0;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ResizableColumns, true)
			.Add(x => x.ColumnWidthsChanged, () => told++));

		await cut.InvokeAsync(() => cut.Instance.HandleColumnResized("nothing", 140));

		Assert.Equal(0, told);
		Assert.Empty(MainStyle(cut));
	}

	[Fact]
	public void TheEdgeWord_ComesFromTheTextRecord()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ResizableColumns, true)
			.Add(x => x.Text, new MokaFileBrowserText { ResizeColumn = title => $"Breite von {title}" }));

		Assert.Equal("Breite von Size", Edge(cut, MokaFileSort.Size).GetAttribute("aria-label"));
	}

	[Fact]
	public void AWrappedHeading_CarriesTheColumnsFitClass_SoItFoldsWithTheColumn()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ResizableColumns, true)
			.Add(x => x.Columns, OwnColumns));

		Assert.Contains("moka-file-browser__cell--medium", Edge(cut, "owner").ParentElement!.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void AMonoColumn_StillLinesUpOnTheEnd_WhenAlignIsLeftOut()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Columns, OwnColumns));

		IElement hash = Heading(cut, "Hash");
		Assert.Contains("moka-file-browser__heading--number", hash.ClassName, StringComparison.Ordinal);
		Assert.DoesNotContain("--align-", hash.ClassName, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("Left", "--align-left", true)]
	[InlineData("Right", "--align-right", false)]
	[InlineData("Middle", "--align-center", false)]
	public void Align_LinesUpTheHeadingAndCells_ApartFromTheFont(string title, string alignClass, bool mono)
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Columns, OwnColumns));
		int column = OwnColumns.ToList().FindIndex(c => c.Title == title);

		string headingClass = Heading(cut, title).ClassName!;
		string cellClass = Row(cut, "a.txt").QuerySelectorAll(".moka-file-browser__cell")[column].ClassName!;

		Assert.Contains(alignClass, headingClass, StringComparison.Ordinal);
		Assert.Contains(alignClass, cellClass, StringComparison.Ordinal);
		Assert.Equal(mono, cellClass.Contains("moka-file-browser__cell--number", StringComparison.Ordinal));
	}

	[Fact]
	public void Justify_ReadsAsLeft()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Columns,
			(IReadOnlyList<MokaFileColumn<FileEntry>>) [new("text", "Text") { Text = entry => entry.Key, Align = MokaTextAlign.Justify }]));

		Assert.Contains("--align-left", Heading(cut, "Text").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ResetHistory_ForgetsWhereTheBrowserHasBeen()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();
		await cut.InvokeAsync(() => cut.Instance.NavigateAsync("/docs"));
		Assert.Equal("enabled", ButtonLabeledBack(cut));

		await cut.InvokeAsync(cut.Instance.ResetHistoryAsync);

		Assert.Equal("disabled", ButtonLabeledBack(cut));
		Assert.Equal(["Sub", "x.md"], Names(cut));
	}

	[Fact]
	public async Task ResetHistory_DropsForwardToo_AndTheNextFolderCanGoBackToThisOne()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();
		await cut.InvokeAsync(() => cut.Instance.NavigateAsync("/docs"));
		await cut.InvokeAsync(() => cut.Instance.NavigateAsync("/docs/sub"));
		await Key(cut, "ArrowLeft", alt: true);
		Assert.False(Button(cut, "Forward").HasAttribute("disabled"));

		await cut.InvokeAsync(cut.Instance.ResetHistoryAsync);
		Assert.True(Button(cut, "Forward").HasAttribute("disabled"));

		await cut.InvokeAsync(() => cut.Instance.NavigateAsync("/empty"));
		await Key(cut, "ArrowLeft", alt: true);
		Assert.Equal(["Sub", "x.md"], Names(cut));
	}

	[Fact]
	public async Task ResetHistory_WhileAKeptListWaitsForTheNextFolder_StartsFromThatFolder()
	{
		TaskCompletionSource<IReadOnlyList<FileEntry>> arriving = new();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.KeepListOnLoadFailure, true)
			.Add(x => x.Load, (folder, token) => folder == "/docs" ? arriving.Task : LoadTree(folder, token)), ownLoad: true);

		Task going = cut.InvokeAsync(() => cut.Instance.NavigateAsync("/docs"));
		await cut.InvokeAsync(cut.Instance.ResetHistoryAsync);
		arriving.SetResult(Tree["/docs"]);
		await going;

		Assert.Equal(["Sub", "x.md"], Names(cut));
		Assert.Equal("disabled", ButtonLabeledBack(cut));
	}

	private IRenderedComponent<MokaFileBrowser<FileEntry>> RenderKeeping(
		Action<ComponentParameterCollectionBuilder<MokaFileBrowser<FileEntry>>>? extra = null) =>
		RenderBrowser(p =>
		{
			p.Add(x => x.KeepListOnLoadFailure, true);
			p.Add(x => x.Load, (folder, token) => folder == "/docs" ? throw new IOException("Denied") : LoadTree(folder, token));
			extra?.Invoke(p);
		}, ownLoad: true);

	[Fact]
	public async Task TheFailure_NamesTheFolderThatDidntOpen()
	{
		MokaFileFailure? told = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderKeeping(p => p.Add(x => x.OnFailureChanged, failure => told = failure));

		await cut.InvokeAsync(() => cut.Instance.NavigateAsync("/docs"));

		Assert.Equal("/docs", told!.Folder);
		Assert.Equal("This folder didn't open", told.Title);
		Assert.Contains("This folder didn't open", cut.Find(".moka-alert").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task FailureTitle_WordsTheAlert_AndCanNameTheFolder()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderKeeping(p => p
			.Add(x => x.FailureTitle, failure => $"Can't open {failure.Folder}"));

		await cut.InvokeAsync(() => cut.Instance.NavigateAsync("/docs"));

		string alert = cut.Find(".moka-alert").TextContent;
		Assert.Contains("Can't open /docs", alert, StringComparison.Ordinal);
		Assert.DoesNotContain("This folder didn't open", alert, StringComparison.Ordinal);
	}

	[Fact]
	public async Task FailureTitle_ReturningNull_KeepsTheTitle()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderKeeping(p => p.Add(x => x.FailureTitle, _ => null));

		await cut.InvokeAsync(() => cut.Instance.NavigateAsync("/docs"));

		Assert.Contains("This folder didn't open", cut.Find(".moka-alert").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task AFailedAction_NamesNoFolder_AndItsTitleIsTheActionsOwn()
	{
		MokaFileFailure? told = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Delete, _ => throw new InvalidOperationException("Locked"))
			.Add(x => x.ConfirmDelete, _ => Task.FromResult(true))
			.Add(x => x.OnFailureChanged, failure => told = failure));
		Click(cut, "a.txt");

		await Key(cut, "Delete");

		Assert.Null(told!.Folder);
		Assert.Equal("That didn't work", told.Title);
	}

	[Fact]
	public async Task OnFailureChanged_IsRaisedOnceWhenItAppears_AndWithNullWhenTheAlertCloses()
	{
		List<MokaFileFailure?> told = [];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderKeeping(p => p.Add(x => x.OnFailureChanged, failure => told.Add(failure)));
		await cut.InvokeAsync(() => cut.Instance.NavigateAsync("/docs"));
		Assert.Single(told);

		ClickOn(cut.Find(".moka-alert button"));

		Assert.Equal(2, told.Count);
		Assert.NotNull(told[0]);
		Assert.Null(told[1]);
	}

	[Fact]
	public async Task OnFailureChanged_IsRaisedWhenAFollowingFolderClearsTheAlert()
	{
		List<MokaFileFailure?> told = [];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderKeeping(p => p.Add(x => x.OnFailureChanged, failure => told.Add(failure)));
		await cut.InvokeAsync(() => cut.Instance.NavigateAsync("/docs"));

		await cut.InvokeAsync(() => cut.Instance.NavigateAsync("/empty"));

		Assert.Null(told[^1]);
		Assert.Empty(cut.FindAll(".moka-alert"));
	}

	[Fact]
	public void KeyScope_IsListByDefault_AndWritesNothing() =>
		Assert.Empty(RootAttribute(RenderBrowser(), "data-moka-file-key-scope"));

	[Fact]
	public void KeyScope_Browser_TellsTheScript() =>
		Assert.Equal("browser", RootAttribute(RenderBrowser(p => p.Add(x => x.KeyScope, MokaFileKeyScope.Browser)), "data-moka-file-key-scope"));

	[Fact]
	public void TheScript_LeavesEnterAndSpaceToAFocusedControl()
	{
		string script = File.ReadAllText(ScriptPath());

		Assert.Contains("ACTIVATE_KEYS.has(e.key) && e.target !== list && e.target !== root", script, StringComparison.Ordinal);
		Assert.Contains("if (isEditable(e.target)) return;", script, StringComparison.Ordinal);
	}

	[Fact]
	public async Task HoldAsync_ACut_ShowsDimmed_AndPastesThroughMove()
	{
		string? movedTo = null;
		IReadOnlyList<FileEntry>? moved = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Move, (items, folder) =>
		{
			moved = items;
			movedTo = folder;
			return Task.CompletedTask;
		}));

		await cut.InvokeAsync(() => cut.Instance.HoldAsync([Tree["/"][1]], cut: true));

		Assert.Contains("moka-file-browser__row--held", Row(cut, "a.txt").ClassName, StringComparison.Ordinal);
		Assert.True(cut.Instance.Held!.IsCut);

		await cut.InvokeAsync(() => cut.Instance.PasteAsync("/docs"));

		Assert.Equal("/docs", movedTo);
		Assert.Equal("a.txt", Assert.Single(moved!).Name);
		Assert.Null(cut.Instance.Held);
	}

	[Fact]
	public async Task HoldAsync_ACopy_StaysHeld_AndCanHoldWhatIsntInThisFolder()
	{
		int copies = 0;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Copy, (_, _) =>
		{
			copies++;
			return Task.CompletedTask;
		}));

		await cut.InvokeAsync(() => cut.Instance.HoldAsync([Tree["/docs"][1]], cut: false));
		await cut.InvokeAsync(() => cut.Instance.PasteAsync());

		Assert.Equal(1, copies);
		Assert.False(cut.Instance.Held!.IsCut);
	}

	[Fact]
	public async Task HoldAsync_RaisesOnHeldChanged()
	{
		MokaFileHeld<FileEntry>? told = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Move, (_, _) => Task.CompletedTask)
			.Add(x => x.OnHeldChanged, held => told = held));

		await cut.InvokeAsync(() => cut.Instance.HoldAsync([Tree["/"][1]], cut: true));

		Assert.Equal("a.txt", Assert.Single(told!.Items).Name);
	}

	[Fact]
	public async Task HoldAsync_WithoutTheAction_OrWithNothing_HoldsNothing()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Copy, (_, _) => Task.CompletedTask));

		await cut.InvokeAsync(() => cut.Instance.HoldAsync([Tree["/"][1]], cut: true));
		Assert.Null(cut.Instance.Held);

		await cut.InvokeAsync(() => cut.Instance.HoldAsync([], cut: false));
		Assert.Null(cut.Instance.Held);
	}

	private IRenderedComponent<MokaFileBrowser<FileEntry>> RenderWithConfirm(Func<IReadOnlyList<FileEntry>, string, Task<bool>> confirm, Action<string> moved) =>
		RenderBrowser(p => p
			.Add(x => x.ConfirmDrop, confirm)
			.Add(x => x.Move, (_, folder) =>
			{
				moved(folder);
				return Task.CompletedTask;
			}));

	[Fact]
	public async Task ConfirmDrop_IsAskedOnADrag_AndYesMoves()
	{
		List<string> moved = [];
		string? askedFor = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderWithConfirm((_, folder) =>
		{
			askedFor = folder;
			return Task.FromResult(true);
		}, moved.Add);

		await cut.InvokeAsync(() => cut.Instance.HandleDrop("/a.txt", "/docs"));

		Assert.Equal("/docs", askedFor);
		Assert.Equal(["/docs"], moved);
	}

	[Fact]
	public async Task ConfirmDrop_No_LeavesItWhereItIs()
	{
		List<string> moved = [];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderWithConfirm((_, _) => Task.FromResult(false), moved.Add);

		await cut.InvokeAsync(() => cut.Instance.HandleDrop("/a.txt", "/docs"));

		Assert.Empty(moved);
	}

	[Fact]
	public async Task ConfirmDrop_IsNotAskedOnAPaste()
	{
		List<string> moved = [];
		int asked = 0;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderWithConfirm((_, _) =>
		{
			asked++;
			return Task.FromResult(true);
		}, moved.Add);
		Click(cut, "a.txt");

		await Key(cut, "x", ctrl: true);
		await cut.InvokeAsync(() => cut.Instance.PasteAsync("/docs"));

		Assert.Equal(0, asked);
		Assert.Equal(["/docs"], moved);
	}

	[Fact]
	public async Task ConfirmDrop_ThatThrows_ShowsAndMovesNothing()
	{
		List<string> moved = [];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderWithConfirm((_, _) => throw new InvalidOperationException("Asked badly"), moved.Add);

		await cut.InvokeAsync(() => cut.Instance.HandleDrop("/a.txt", "/docs"));

		Assert.Empty(moved);
		Assert.Contains("Asked badly", cut.Find(".moka-alert").TextContent, StringComparison.Ordinal);
	}

	private static string ScriptPath()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			string candidate = Path.Combine(folder.FullName, "src", "Moka.Red.Data", "FileBrowser", "MokaFileBrowser.razor.js");
			if (File.Exists(candidate))
			{
				return candidate;
			}
		}

		throw new FileNotFoundException("The browser's script wasn't found.");
	}
}
