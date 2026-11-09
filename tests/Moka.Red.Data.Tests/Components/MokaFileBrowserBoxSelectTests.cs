using Bunit;
using Moka.Red.Data.FileBrowser;
using Moka.Red.Primitives.SelectionArea;

namespace Moka.Red.Data.Tests.Components;

public sealed class MokaFileBrowserBoxSelectTests : MokaFileBrowserKit
{
	private const string BoxModulePath = "./_content/Moka.Red.Core/moka-box-select.js";

	private readonly BunitJSModuleInterop _box;

	public MokaFileBrowserBoxSelectTests()
	{
		_box = JSInterop.SetupModule(BoxModulePath);
		_box.SetupVoid("bindBoxSelect", _ => true).SetVoidResult();
		_box.SetupVoid("unbindBoxSelect", _ => true).SetVoidResult();
	}

	private static Task Report(
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut,
		MokaBoxSelectPhase phase,
		MokaBoxSelectMode mode,
		string[] keys,
		int[]? range = null) =>
		cut.InvokeAsync(() => cut.Instance.HandleBoxSelect((int)phase, (int)mode, keys, range));

	private static Dictionary<string, object?> Geometry(JSRuntimeInvocation bind) =>
		Assert.IsType<Dictionary<string, object?>>(Assert.IsType<Dictionary<string, object?>>(bind.Arguments[2])["geometry"]);

	[Fact]
	public void Off_DrawsNoBox_AndTouchesNoBoxScript()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		Assert.Empty(cut.FindAll("[data-moka-box]"));
		Assert.Empty(_box.Invocations);
	}

	[Fact]
	public void On_DrawsTheBoxInTheList_AndDescribesTheRows()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.BoxSelect, true));

		AngleSharp.Dom.IElement box = cut.Find("[data-moka-list] > .moka-box-select");
		Assert.True(box.HasAttribute("hidden"));
		Assert.Equal("true", box.GetAttribute("aria-hidden"));

		JSRuntimeInvocation bind = _box.Invocations["bindBoxSelect"][^1];
		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(bind.Arguments[2]);
		Assert.Equal("[data-moka-key]", options["items"]);
		Assert.Equal("data-moka-key", options["key"]);
		Assert.Equal("HandleBoxSelect", options["method"]);
		Assert.EndsWith("> [data-moka-list]", (string?)options["area"], StringComparison.Ordinal);
		Dictionary<string, object?> geometry = Geometry(bind);
		Assert.Equal(4, geometry["count"]);
		Assert.Equal(1, geometry["columns"]);
		Assert.Equal("aria-posinset", geometry["index"]);
		Assert.Equal(1, geometry["base"]);
	}

	[Fact]
	public void TheTiles_AreDescribedByTheirColumns()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.BoxSelect, true)
			.Add(x => x.View, new MokaFileView(MokaFileViewMode.Tiles)));

		cut.InvokeAsync(() => cut.Instance.HandleColumns(3));

		cut.WaitForAssertion(() => Assert.Equal(3, Geometry(_box.Invocations["bindBoxSelect"][^1])["columns"]));
	}

	[Fact]
	public void ARenderThatChangesNothing_DoesNotBindAgain()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.BoxSelect, true));
		int binds = _box.Invocations["bindBoxSelect"].Count;

		cut.Render();

		Assert.Equal(binds, _box.Invocations["bindBoxSelect"].Count);
	}

	[Fact]
	public void TurningItOff_Unbinds()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.BoxSelect, true));

		cut.Render(p => p.Add(x => x.BoxSelect, false));

		Assert.Empty(cut.FindAll("[data-moka-box]"));
		Assert.Single(_box.Invocations["unbindBoxSelect"]);
	}

	[Fact]
	public async Task ABox_SelectsLive_AndReportsTheSelectionOnceReleased()
	{
		List<IReadOnlySet<string>> raised = [];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.BoxSelect, true)
			.Add(x => x.SelectedChanged, set => raised.Add(set)));

		await Report(cut, MokaBoxSelectPhase.Started, MokaBoxSelectMode.Replace, ["/a.txt"]);
		Assert.Equal(["a.txt"], SelectedNames(cut));

		await Report(cut, MokaBoxSelectPhase.Changed, MokaBoxSelectMode.Replace, ["/a.txt", "/b.txt"]);
		Assert.Equal(["a.txt", "b.txt"], SelectedNames(cut));
		Assert.Empty(raised);

		await Report(cut, MokaBoxSelectPhase.Finished, MokaBoxSelectMode.Replace, ["/a.txt", "/b.txt"]);
		IReadOnlySet<string> selected = Assert.Single(raised);
		Assert.Equal(["/a.txt", "/b.txt"], selected.Order(StringComparer.Ordinal));
		Assert.Contains("moka-file-browser__row--cursor", Row(cut, "a.txt").ClassList);
	}

	[Fact]
	public async Task Replace_DropsWhatWasSelected()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.BoxSelect, true));
		Click(cut, "c.txt");

		await Report(cut, MokaBoxSelectPhase.Started, MokaBoxSelectMode.Replace, ["/a.txt"]);

		Assert.Equal(["a.txt"], SelectedNames(cut));
	}

	[Fact]
	public async Task Add_KeepsWhatWasSelected()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.BoxSelect, true));
		Click(cut, "c.txt");

		await Report(cut, MokaBoxSelectPhase.Started, MokaBoxSelectMode.Add, ["/a.txt"]);
		await Report(cut, MokaBoxSelectPhase.Finished, MokaBoxSelectMode.Add, ["/a.txt", "/b.txt"]);

		Assert.Equal(["a.txt", "b.txt", "c.txt"], SelectedNames(cut));
	}

	[Fact]
	public async Task Toggle_FlipsWhatTheBoxCovers()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.BoxSelect, true));
		Click(cut, "a.txt");
		Click(cut, "c.txt", ctrl: true);

		await Report(cut, MokaBoxSelectPhase.Started, MokaBoxSelectMode.Toggle, []);
		await Report(cut, MokaBoxSelectPhase.Finished, MokaBoxSelectMode.Toggle, ["/a.txt", "/b.txt"]);

		Assert.Equal(["b.txt", "c.txt"], SelectedNames(cut));
	}

	[Fact]
	public async Task Cancelled_PutsTheSelectionBack_AndRaisesNothing()
	{
		List<IReadOnlySet<string>> raised = [];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.BoxSelect, true)
			.Add(x => x.SelectedChanged, set => raised.Add(set)));
		Click(cut, "c.txt");
		raised.Clear();

		await Report(cut, MokaBoxSelectPhase.Started, MokaBoxSelectMode.Replace, ["/a.txt"]);
		await Report(cut, MokaBoxSelectPhase.Changed, MokaBoxSelectMode.Replace, ["/a.txt", "/b.txt"]);
		await Report(cut, MokaBoxSelectPhase.Cancelled, MokaBoxSelectMode.Replace, []);

		Assert.Equal(["c.txt"], SelectedNames(cut));
		Assert.Empty(raised);
	}

	[Fact]
	public async Task TheRange_SelectsRowsThatArentDrawn()
	{
		Tree["/"] = [.. Enumerable.Range(0, 300).Select(i => new FileEntry($"/f{i:000}", $"f{i:000}.txt", Size: 1))];
		IReadOnlySet<string>? raised = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.BoxSelect, true)
			.Add(x => x.SelectedChanged, set => raised = set));
		Assert.True(cut.FindAll("[role=option]").Count < 300);

		await Report(cut, MokaBoxSelectPhase.Started, MokaBoxSelectMode.Replace, ["/f000"], [0, 0, 0, 0, 1, 300]);
		await Report(cut, MokaBoxSelectPhase.Finished, MokaBoxSelectMode.Replace, ["/f000", "/f001"], [0, 249, 0, 0, 1, 300]);

		Assert.NotNull(raised);
		Assert.Equal(250, raised.Count);
		Assert.Contains("/f249", raised);
		Assert.DoesNotContain("/f250", raised);
		Assert.Contains("250", Status(cut), StringComparison.Ordinal);
	}

	[Fact]
	public async Task ARangeMeasuredOnAnotherList_IsIgnored()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.BoxSelect, true));

		await Report(cut, MokaBoxSelectPhase.Finished, MokaBoxSelectMode.Replace, ["/b.txt"], [0, 3, 0, 0, 1, 99]);

		Assert.Equal(["b.txt"], SelectedNames(cut));
	}

	[Fact]
	public async Task TheTileRange_CoversTheColumnsBetween()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.BoxSelect, true)
			.Add(x => x.View, new MokaFileView(MokaFileViewMode.Tiles)));
		await cut.InvokeAsync(() => cut.Instance.HandleColumns(2));

		await Report(cut, MokaBoxSelectPhase.Finished, MokaBoxSelectMode.Replace, [], [0, 1, 1, 1, 2, 4]);

		Assert.Equal(["a.txt", "c.txt"], SelectedNames(cut));
	}

	[Fact]
	public async Task TurningItOffHalfway_PutsTheSelectionBack()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.BoxSelect, true));
		Click(cut, "c.txt");
		await Report(cut, MokaBoxSelectPhase.Started, MokaBoxSelectMode.Replace, ["/a.txt", "/b.txt"]);
		Assert.Equal(["a.txt", "b.txt"], SelectedNames(cut));

		cut.Render(p => p.Add(x => x.BoxSelect, false));
		await Report(cut, MokaBoxSelectPhase.Cancelled, MokaBoxSelectMode.Replace, []);

		cut.WaitForAssertion(() => Assert.Equal(["c.txt"], SelectedNames(cut)));
	}

	[Fact]
	public async Task AReportWhileOff_IsIgnored()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		await Report(cut, MokaBoxSelectPhase.Finished, MokaBoxSelectMode.Replace, ["/a.txt"]);

		Assert.Empty(SelectedNames(cut));
	}

	private static string Status(IRenderedComponent<MokaFileBrowser<FileEntry>> cut) =>
		cut.Find(".moka-file-browser__status").TextContent;
}
