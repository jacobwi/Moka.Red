using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.FileBrowser;

namespace Moka.Red.Data.Tests.Components;

public class MokaFileBrowserInputTests : MokaFileBrowserKit
{
	[Fact]
	public void AClick_SelectsOnlyThatRow()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		Click(cut, "a.txt");
		Click(cut, "b.txt");

		Assert.Equal(["b.txt"], SelectedNames(cut));
	}

	[Fact]
	public void ControlClick_Toggles()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		Click(cut, "a.txt", ctrl: true);
		Click(cut, "c.txt", ctrl: true);
		Assert.Equal(["a.txt", "c.txt"], SelectedNames(cut));

		Click(cut, "a.txt", ctrl: true);
		Assert.Equal(["c.txt"], SelectedNames(cut));
	}

	[Fact]
	public void CommandClick_Toggles()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		Row(cut, "a.txt").Click(new MouseEventArgs { MetaKey = true, Detail = 1 });
		Row(cut, "b.txt").Click(new MouseEventArgs { MetaKey = true, Detail = 1 });

		Assert.Equal(2, SelectedNames(cut).Length);
	}

	[Fact]
	public void ShiftClick_SelectsTheRangeFromTheAnchor()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		Click(cut, "a.txt");
		Click(cut, "c.txt", shift: true);
		Assert.Equal(["a.txt", "b.txt", "c.txt"], SelectedNames(cut));

		Click(cut, "b.txt", shift: true);
		Assert.Equal(["a.txt", "b.txt"], SelectedNames(cut));
	}

	[Fact]
	public void ClickingTheEmptySpace_ClearsTheSelection()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();
		Click(cut, "a.txt");

		cut.Find("[role=listbox]").Click();

		Assert.Empty(SelectedNames(cut));
	}

	[Fact]
	public void SelectedChanged_CarriesTheKeys()
	{
		IReadOnlySet<string>? selected = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.SelectedChanged, value => selected = value));

		Click(cut, "a.txt", ctrl: true);
		Click(cut, "b.txt", ctrl: true);

		Assert.True(selected!.SetEquals(["/a.txt", "/b.txt"]));
	}

	[Fact]
	public void TheSelectedParameter_SelectsThoseRows_AndDropsKeysNotThere()
	{
		IReadOnlySet<string>? reported = null;
		HashSet<string> wanted = ["/b.txt", "/gone.txt"];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Selected, wanted)
			.Add(x => x.SelectedChanged, value => reported = value));

		Assert.Equal(["b.txt"], SelectedNames(cut));
		Assert.True(reported!.SetEquals(["/b.txt"]));
	}

	[Fact]
	public async Task Arrows_MoveTheCursorAndTheSelection()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		int line = await Key(cut, "ArrowDown");
		Assert.Equal(0, line);
		Assert.Equal(["Docs"], SelectedNames(cut));

		line = await Key(cut, "ArrowDown");
		Assert.Equal(1, line);
		Assert.Equal(["a.txt"], SelectedNames(cut));
		Assert.Equal(cut.Find("[role=option][aria-selected=true]").Id, cut.Find("[role=listbox]").GetAttribute("aria-activedescendant"));

		await Key(cut, "ArrowUp");
		Assert.Equal(["Docs"], SelectedNames(cut));
	}

	[Fact]
	public async Task ShiftArrows_ExtendFromTheAnchor()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();
		Click(cut, "a.txt");

		await Key(cut, "ArrowDown", shift: true);
		await Key(cut, "ArrowDown", shift: true);
		Assert.Equal(["a.txt", "b.txt", "c.txt"], SelectedNames(cut));

		await Key(cut, "ArrowUp", shift: true);
		Assert.Equal(["a.txt", "b.txt"], SelectedNames(cut));
	}

	[Fact]
	public async Task ControlArrows_MoveOnlyTheCursor_AndSpaceSelectsThere()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();
		Click(cut, "Docs");

		await Key(cut, "ArrowDown", ctrl: true);
		await Key(cut, "ArrowDown", ctrl: true);
		Assert.Equal(["Docs"], SelectedNames(cut));

		await Key(cut, " ");
		Assert.Equal(["Docs", "b.txt"], SelectedNames(cut));
	}

	[Fact]
	public async Task HomeEndAndPage_Clamp()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		await Key(cut, "End");
		Assert.Equal(["c.txt"], SelectedNames(cut));

		await Key(cut, "Home");
		Assert.Equal(["Docs"], SelectedNames(cut));

		await Key(cut, "PageDown", pageRows: 2);
		Assert.Equal(["b.txt"], SelectedNames(cut));

		await Key(cut, "PageDown", pageRows: 50);
		Assert.Equal(["c.txt"], SelectedNames(cut));
	}

	[Fact]
	public async Task InTiles_UpAndDownMoveByARow_AndLeftAndRightByOne()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.View, new MokaFileView(MokaFileViewMode.Tiles)));
		await cut.InvokeAsync(() => cut.Instance.HandleColumns(2));
		Click(cut, "Docs");

		int line = await Key(cut, "ArrowDown");
		Assert.Equal(1, line);
		Assert.Equal(["b.txt"], SelectedNames(cut));

		await Key(cut, "ArrowLeft");
		Assert.Equal(["a.txt"], SelectedNames(cut));

		await Key(cut, "ArrowRight");
		Assert.Equal(["b.txt"], SelectedNames(cut));
	}

	[Fact]
	public async Task ControlA_SelectsAll_AndEscapeClears()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		await Key(cut, "a", ctrl: true);
		Assert.Equal(4, SelectedNames(cut).Length);

		await Key(cut, "Escape");
		Assert.Empty(SelectedNames(cut));
	}

	[Fact]
	public async Task TypeAhead_FindsTheNextName_AndARepeatedLetterCycles()
	{
		Tree["/"] = [new("/1", "alpha"), new("/2", "avocado"), new("/3", "beta")];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		await cut.InvokeAsync(() => cut.Instance.HandleTypeAhead("be"));
		Assert.Equal(["beta"], SelectedNames(cut));

		await cut.InvokeAsync(() => cut.Instance.HandleTypeAhead("a"));
		Assert.Equal(["alpha"], SelectedNames(cut));

		await cut.InvokeAsync(() => cut.Instance.HandleTypeAhead("aa"));
		Assert.Equal(["avocado"], SelectedNames(cut));
	}

	[Fact]
	public async Task Enter_OpensAFolder_AndBackspaceGoesUp()
	{
		string? folder = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.FolderChanged, value => folder = value));
		Click(cut, "Docs");

		await Key(cut, "Enter");
		Assert.Equal("/docs", folder);
		Assert.Equal(["Sub", "x.md"], Names(cut));
		Assert.Empty(SelectedNames(cut));

		cut.Render(p => p.Add(x => x.Crumbs, CrumbsFor("/docs")));
		await Key(cut, "Backspace");
		Assert.Equal("/", folder);
	}

	[Fact]
	public void DoubleClick_OpensAFile()
	{
		FileEntry? opened = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.OnOpenFile, entry => opened = entry));

		Row(cut, "a.txt").DoubleClick();

		Assert.Equal("a.txt", opened!.Name);
	}

	[Fact]
	public async Task BackAndForward_WalkTheHistory_ByButtonAndByAltArrows()
	{
		List<string> folders = [];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.FolderChanged, folders.Add));
		Assert.True(Button(cut, "Back").HasAttribute("disabled"));

		DoubleClickOn(Row(cut, "Docs"));
		Assert.False(Button(cut, "Back").HasAttribute("disabled"));

		ClickOn(Button(cut, "Back"));
		Assert.Equal(4, Names(cut).Length);
		Assert.False(Button(cut, "Forward").HasAttribute("disabled"));

		await Key(cut, "ArrowRight", alt: true);
		Assert.Equal(["Sub", "x.md"], Names(cut));

		await Key(cut, "ArrowLeft", alt: true);
		Assert.Equal(["/docs", "/", "/docs", "/"], folders);
	}

	[Fact]
	public void AFolderPickedFromOutside_JoinsTheHistory()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		cut.Render(p => p.Add(x => x.Folder, "/docs"));

		Assert.False(Button(cut, "Back").HasAttribute("disabled"));
		Assert.Equal(["Sub", "x.md"], Names(cut));
	}

	[Fact]
	public void TheParentsEcho_DoesNotLoadTwice()
	{
		int loads = 0;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Load, (folder, token) =>
		{
			loads++;
			return LoadTree(folder, token);
		}), ownLoad: true);
		Row(cut, "Docs").DoubleClick();
		Assert.Equal(2, loads);

		cut.Render(p => p.Add(x => x.Folder, "/docs"));

		Assert.Equal(2, loads);
	}

	[Fact]
	public void UpAndCrumbs_Navigate()
	{
		string? folder = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.FolderChanged, value => folder = value), "/docs/sub");
		Assert.False(Button(cut, "Up one folder").HasAttribute("disabled"));

		cut.Find(".moka-file-browser__crumb").Click();

		Assert.Equal("/", folder);
		Assert.Equal("/", cut.Find(".moka-file-browser__crumb").GetAttribute("data-moka-file-target"));
	}

	[Fact]
	public void TheFolderAtTheTop_HasNowhereToGoUp()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		Assert.True(Button(cut, "Up one folder").HasAttribute("disabled"));
	}

	[Fact]
	public void Rows_CarryWhatTheDragScriptReads()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Move, (_, _) => Task.CompletedTask));

		IElement folder = Row(cut, "Docs");
		Assert.Equal("true", folder.GetAttribute("draggable"));
		Assert.Equal("/docs", folder.GetAttribute("data-moka-key"));
		Assert.Equal("/docs", folder.GetAttribute("data-moka-file-target"));
		Assert.False(Row(cut, "a.txt").HasAttribute("data-moka-file-target"));
	}

	[Fact]
	public void WithoutMove_RowsDontDrag()
	{
		Assert.False(Row(RenderBrowser(), "a.txt").HasAttribute("draggable"));
	}

	[Fact]
	public async Task TheScript_IsBoundOnce_AndUnboundOnDispose()
	{
		RenderBrowser();

		Module.VerifyInvoke("bind");
		await DisposeComponentsAsync();
		Module.VerifyInvoke("unbind");
	}
}
