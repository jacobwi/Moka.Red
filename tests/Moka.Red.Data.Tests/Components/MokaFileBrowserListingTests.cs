using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Data.FileBrowser;
using Moka.Red.Forms.SearchInput;

namespace Moka.Red.Data.Tests.Components;

public class MokaFileBrowserListingTests : MokaFileBrowserKit
{
	[Fact]
	public void FoldersComeFirst_ThenNamesInOrder()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		Assert.Equal(["Docs", "a.txt", "b.txt", "c.txt"], Names(cut));
	}

	[Fact]
	public void Names_SortNumbersByValue()
	{
		Tree["/"] = [new("/10", "log10"), new("/2", "log2"), new("/1", "log1")];

		Assert.Equal(["log1", "log2", "log10"], Names(RenderBrowser()));
	}

	[Fact]
	public void TheStatusBar_CountsItemsAndTheSelection()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		Assert.Contains("4 items", cut.Find(".moka-file-browser__status").TextContent, StringComparison.Ordinal);

		Click(cut, "b.txt");

		string status = cut.Find(".moka-file-browser__status").TextContent;
		Assert.Contains("1 selected", status, StringComparison.Ordinal);
		Assert.Contains("30 B", status, StringComparison.Ordinal);
	}

	[Fact]
	public void TheList_IsAMultiSelectListboxWithOneTabStop()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		IElement list = cut.Find("[role=listbox]");
		Assert.Equal("0", list.GetAttribute("tabindex"));
		Assert.Equal("true", list.GetAttribute("aria-multiselectable"));
		Assert.Equal("4", cut.Find("[role=option]").GetAttribute("aria-setsize"));
		Assert.Empty(cut.FindAll("[role=option][tabindex]"));
	}

	[Fact]
	public async Task WhileLoading_ThePlaceholderShows_ThenTheRows()
	{
		TaskCompletionSource<IReadOnlyList<FileEntry>> answer = new();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Load, (_, _) => answer.Task), ownLoad: true);

		Assert.Single(cut.FindAll(".moka-file-browser__loading"));
		Assert.Equal("true", cut.Find(".moka-file-browser").GetAttribute("aria-busy"));

		await cut.InvokeAsync(() => answer.SetResult(Tree["/"]));

		Assert.Empty(cut.FindAll(".moka-file-browser__loading"));
		Assert.Equal(4, cut.FindAll("[role=option]").Count);
	}

	[Fact]
	public void AnEmptyFolder_SaysSo_AndMentionsDropsOnlyWhenItTakesThem()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(folder: "/empty");
		Assert.Contains("This folder is empty", cut.Find(".moka-empty-state").TextContent, StringComparison.Ordinal);
		Assert.DoesNotContain("Drop files", cut.Find(".moka-empty-state").TextContent, StringComparison.Ordinal);

		IRenderedComponent<MokaFileBrowser<FileEntry>> uploads = RenderBrowser(
			p => p.Add(x => x.Upload, (_, _) => Task.CompletedTask), "/empty");
		Assert.Contains("Drop files", uploads.Find(".moka-empty-state").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task AFilter_NarrowsTheList_AndNoMatchesOffersToClearIt()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();
		MokaSearchInput search = cut.FindComponent<MokaSearchInput>().Instance;

		await cut.InvokeAsync(() => search.OnSearch.InvokeAsync("B."));
		Assert.Equal(["b.txt"], Names(cut));
		Assert.Contains("1 of 4 items", cut.Find(".moka-file-browser__status").TextContent, StringComparison.Ordinal);

		await cut.InvokeAsync(() => search.OnSearch.InvokeAsync("zzz"));
		Assert.Contains("zzz", cut.Find(".moka-empty-state").TextContent, StringComparison.Ordinal);

		ClickOn(cut.Find(".moka-empty-state button"));
		Assert.Equal(4, Names(cut).Length);
	}

	[Fact]
	public async Task AFilter_DropsWhatItHidesFromTheSelection()
	{
		IReadOnlySet<string>? selected = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.SelectedChanged, value => selected = value));
		Click(cut, "a.txt");
		MokaSearchInput search = cut.FindComponent<MokaSearchInput>().Instance;

		await cut.InvokeAsync(() => search.OnSearch.InvokeAsync("b"));

		Assert.Empty(selected!);
	}

	[Fact]
	public void AFailedLoad_ShowsWhy_AndTriesAgain()
	{
		int attempts = 0;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Load, (folder, token) =>
		{
			if (++attempts == 1)
			{
				throw new InvalidOperationException("Disk is gone");
			}

			return LoadTree(folder, token);
		}), ownLoad: true);

		Assert.Contains("Disk is gone", cut.Find(".moka-callout").TextContent, StringComparison.Ordinal);

		cut.Find(".moka-callout button").Click();

		Assert.Equal(4, cut.FindAll("[role=option]").Count);
		Assert.Empty(cut.FindAll(".moka-callout"));
	}

	[Fact]
	public async Task ProblemFragment_ReplacesTheError_AndCanRetry()
	{
		bool locked = true;
		MokaFileProblem? shown = null;
		RenderFragment<MokaFileProblem> fragment = problem => builder =>
		{
			shown = problem;
			builder.OpenElement(0, "p");
			builder.AddAttribute(1, "class", "locked");
			builder.AddContent(2, problem.Error.Message);
			builder.CloseElement();
		};
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Problem, fragment)
			.Add(x => x.Load, (folder, token) => locked ? throw new UnauthorizedAccessException("Locked") : LoadTree(folder, token)), ownLoad: true);

		Assert.Equal("Locked", cut.Find(".locked").TextContent);
		Assert.Empty(cut.FindAll(".moka-callout"));

		locked = false;
		await cut.InvokeAsync(shown!.Retry);

		Assert.Equal(4, cut.FindAll("[role=option]").Count);
	}

	[Fact]
	public async Task ALateAnswer_FromAFolderLeftBehind_IsIgnored_AndCancelled()
	{
		TaskCompletionSource<IReadOnlyList<FileEntry>> first = new();
		TaskCompletionSource<IReadOnlyList<FileEntry>> second = new();
		List<CancellationToken> tokens = [];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Load, (folder, token) =>
		{
			tokens.Add(token);
			return folder == "/" ? first.Task : second.Task;
		}), ownLoad: true);

		cut.Render(p => p.Add(x => x.Folder, "/docs"));
		Assert.True(tokens[0].IsCancellationRequested);

		await cut.InvokeAsync(() => second.SetResult(Tree["/docs"]));
		await cut.InvokeAsync(() => first.SetResult(Tree["/"]));

		Assert.Equal(["Sub", "x.md"], Names(cut));
	}

	[Fact]
	public async Task ALateFailure_FromAFolderLeftBehind_IsIgnored()
	{
		TaskCompletionSource<IReadOnlyList<FileEntry>> first = new();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Load, (folder, token) =>
			folder == "/" ? first.Task : LoadTree(folder, token)), ownLoad: true);

		cut.Render(p => p.Add(x => x.Folder, "/docs"));
		await cut.InvokeAsync(() => first.SetException(new InvalidOperationException("Too late")));

		Assert.Empty(cut.FindAll(".moka-callout"));
		Assert.Equal(["Sub", "x.md"], Names(cut));
	}

	[Fact]
	public async Task Disposing_CancelsTheLoad()
	{
		CancellationToken captured = default;
		TaskCompletionSource<IReadOnlyList<FileEntry>> answer = new();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Load, (_, token) =>
		{
			captured = token;
			return answer.Task;
		}), ownLoad: true);

		await cut.Instance.DisposeAsync();

		Assert.True(captured.IsCancellationRequested);
	}

	[Fact]
	public void DescribeRunsOncePerItem_AcrossRenders()
	{
		int described = 0;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Describe, entry =>
		{
			described++;
			return Describe(entry);
		}), ownDescribe: true);

		Click(cut, "a.txt");
		Click(cut, "b.txt");

		Assert.Equal(4, described);
	}

	[Fact]
	public void RepeatedKeys_KeepTheFirstItem()
	{
		Tree["/"] = [new("/same", "first"), new("/same", "second")];

		Assert.Equal(["first"], Names(RenderBrowser()));
	}

	[Fact]
	public void ALongList_RendersOnlyWhatIsInView_AndCountsAllOfIt()
	{
		Tree["/"] = [.. Enumerable.Range(0, 500).Select(i => new FileEntry($"/{i}", $"file{i}"))];

		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		IReadOnlyList<IElement> options = cut.FindAll("[role=option]");
		Assert.True(options.Count < 200, $"{options.Count} rows were rendered.");
		Assert.Equal("500", options[0].GetAttribute("aria-setsize"));
	}

	[Fact]
	public void HeadingsSort_AndClickedAgainReverse_WithFoldersStayingFirst()
	{
		MokaFileView? view = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.ViewChanged, value => view = value));
		IElement SizeHeading() => cut.FindAll("button.moka-file-browser__heading").Single(b => b.TextContent.Contains("Size", StringComparison.Ordinal));

		SizeHeading().Click();
		Assert.Equal(["Docs", "a.txt", "c.txt", "b.txt"], Names(cut));
		Assert.Equal(MokaFileSort.Size, view!.SortBy);

		SizeHeading().Click();
		Assert.Equal(["Docs", "b.txt", "c.txt", "a.txt"], Names(cut));
		Assert.True(view.Descending);
	}

	[Fact]
	public void TheViewParameter_SortsAndSwitchesToTiles()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.View, new MokaFileView(MokaFileViewMode.Tiles, MokaFileSort.Size, Descending: true)));

		Assert.NotEmpty(cut.FindAll(".moka-file-browser__tile"));
		Assert.Empty(cut.FindAll(".moka-file-browser__row"));
		Assert.Equal(["Docs", "b.txt", "c.txt", "a.txt"], Names(cut));
	}

	[Fact]
	public async Task Tiles_FollowTheColumnsThatFit()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.View, new MokaFileView(MokaFileViewMode.Tiles)));

		await cut.InvokeAsync(() => cut.Instance.HandleColumns(2));

		Assert.Equal(2, cut.FindAll(".moka-file-browser__tile-row").Count);
		Assert.Contains("--_fb-columns: 2", cut.Find("[role=listbox]").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void TheToggle_RaisesTheChangedView()
	{
		MokaFileView? view = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.ViewChanged, value => view = value));

		Button(cut, "Tiles").Click();

		Assert.Equal(MokaFileViewMode.Tiles, view!.Mode);
		Assert.Equal("true", Button(cut, "Tiles").GetAttribute("aria-pressed"));
		Module.VerifyInvoke("measure");
	}

	[Fact]
	public void CustomColumns_ReplaceTheBuiltInOnes()
	{
		IReadOnlyList<MokaFileColumn<FileEntry>> columns =
		[
			new("kind", "Kind")
			{
				Text = entry => entry.Folder ? "Folder" : "File",
				Compare = (a, b) => string.CompareOrdinal(b.Name, a.Name),
				Fit = MokaFileColumnFit.Always
			}
		];

		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Columns, columns));

		Assert.Equal("Folder", cut.Find(".moka-file-browser__cell").TextContent);
		Assert.DoesNotContain(cut.FindAll("[role=radio], .moka-segment"), s => s.TextContent.Contains("Size", StringComparison.Ordinal));
		cut.FindAll("button.moka-file-browser__heading").Single(b => b.TextContent.Contains("Kind", StringComparison.Ordinal)).Click();
		Assert.Equal(["Docs", "c.txt", "b.txt", "a.txt"], Names(cut));
	}

	[Fact]
	public void TheSlots_Render()
	{
		RenderFragment Mark(string text) => builder => builder.AddContent(0, text);
		RenderFragment<IReadOnlyList<FileEntry>> details = selected => builder =>
			builder.AddContent(0, $"Details of {selected.Count}");

		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Side, Mark("Side pane"))
			.Add(x => x.ToolbarEnd, Mark("Toolbar end"))
			.Add(x => x.StatusEnd, Mark("Status end"))
			.Add(x => x.Details, details));

		Assert.Contains("Side pane", cut.Find(".moka-file-browser__side").TextContent, StringComparison.Ordinal);
		Assert.Contains("Toolbar end", cut.Markup, StringComparison.Ordinal);
		Assert.Contains("Status end", cut.Find(".moka-file-browser__status").TextContent, StringComparison.Ordinal);
		Assert.Contains("Details of 0", cut.Find(".moka-file-browser__details").TextContent, StringComparison.Ordinal);

		Click(cut, "a.txt");
		Assert.Contains("Details of 1", cut.Find(".moka-file-browser__details").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void EveryWord_ComesFromTheTextRecord()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Text, new MokaFileBrowserText
			{
				Back = "Zurück",
				ItemCount = count => $"{count} Dateien",
				SortName = "Benennung"
			}));

		Assert.NotNull(Button(cut, "Zurück"));
		Assert.Contains("4 Dateien", cut.Find(".moka-file-browser__status").TextContent, StringComparison.Ordinal);
		Assert.Contains("Benennung", cut.Find(".moka-file-browser__head").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void MarginPaddingAndRounding_LandOnTheRoot()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.MarginValue, "7px")
			.Add(x => x.PaddingValue, "5px")
			.Add(x => x.RoundedValue, "3px")
			.Add(x => x.Height, "24rem")
			.Add(x => x.Class, "mine"));

		IElement root = cut.Find(".moka-file-browser");
		Assert.Equal("margin: 7px; padding: 5px; border-radius: 3px; height: 24rem", root.GetAttribute("style")!.TrimEnd(';', ' '));
		Assert.EndsWith("mine", root.ClassName, StringComparison.Ordinal);
	}
}
