using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Utilities;
using Moka.Red.Data.FileBrowser;
using Moka.Red.Feedback.JobProgress;
using Moka.Red.Forms.SearchInput;

namespace Moka.Red.Data.Tests.Components;

public partial class MokaFileBrowserOptionTests : MokaFileBrowserKit
{
	private static RenderFragment Mark(string text) => builder => builder.AddContent(0, text);

	private static RenderFragment PressMe(string id, Func<Task> run) => builder =>
	{
		builder.OpenElement(0, "button");
		builder.AddAttribute(1, "id", id);
		builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(new object(), run));
		builder.CloseElement();
	};

	private static string Status(IRenderedComponent<MokaFileBrowser<FileEntry>> cut) =>
		cut.Find(".moka-file-browser__status").TextContent;

	private static void Toolbar(IRenderedComponent<MokaFileBrowser<FileEntry>> cut, string text) =>
		cut.FindAll(".moka-file-browser__bar button").Single(b => b.TextContent.Contains(text, StringComparison.Ordinal)).Click();

	[Theory]
	[InlineData("x")]
	[InlineData("d")]
	[InlineData("c")]
	[InlineData("a")]
	[InlineData("v")]
	public async Task ControlShiftAndALetter_IsNotTheShortcut(string letter)
	{
		int copies = 0;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Move, (_, _) => Task.CompletedTask)
			.Add(x => x.Copy, (_, _) =>
			{
				copies++;
				return Task.CompletedTask;
			}));
		Click(cut, "a.txt");

		await Key(cut, letter, ctrl: true, shift: true);

		Assert.Equal(0, copies);
		Assert.Equal(["a.txt"], SelectedNames(cut));
		Assert.DoesNotContain(" to ", Status(cut), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("Delete")]
	[InlineData("Enter")]
	[InlineData("F2")]
	[InlineData("Backspace")]
	[InlineData("Escape")]
	[InlineData("F5")]
	public async Task ShiftWithAKeyThatDoesntExtend_IsNotTheShortcut(string key)
	{
		int loads = 0;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Load, (folder, token) =>
			{
				loads++;
				return LoadTree(folder, token);
			})
			.Add(x => x.ConfirmDelete, _ => throw new InvalidOperationException("Asked to delete"))
			.Add(x => x.Delete, _ => Task.CompletedTask)
			.Add(x => x.Rename, (_, _) => Task.CompletedTask), ownLoad: true);
		Click(cut, "Docs");

		await Key(cut, key, shift: true);

		Assert.Equal(1, loads);
		Assert.Equal(["Docs"], SelectedNames(cut));
		Assert.Empty(cut.FindAll(".moka-alert"));
		Assert.Empty(cut.FindAll("[role=option] input"));
	}

	[Fact]
	public async Task AltWithAnotherModifier_IsNotHistory()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();
		DoubleClickOn(Row(cut, "Docs"));

		await Key(cut, "ArrowLeft", ctrl: true, alt: true);
		await Key(cut, "ArrowLeft", shift: true, alt: true);

		Assert.Equal(["Sub", "x.md"], Names(cut));
	}

	[Fact]
	public void TheScriptsKeyTable_MatchesTheEnum()
	{
		Match table = KeyTable().Match(File.ReadAllText(ScriptPath()));
		Dictionary<string, int> script = KeyEntry().Matches(table.Groups[1].Value)
			.ToDictionary(entry => entry.Groups[1].Value, entry => int.Parse(entry.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture));

		MokaFileKeys[] flags = [.. Enum.GetValues<MokaFileKeys>().Where(flag => flag is not MokaFileKeys.None and not MokaFileKeys.All)];
		Assert.Equal(flags.Length, script.Count);
		foreach (MokaFileKeys flag in flags)
		{
			Assert.Equal((int)flag, script[flag.ToString()]);
		}
	}

	[Fact]
	public void TheScript_LeavesCopyAloneWhileTextIsSelected()
	{
		string script = File.ReadAllText(ScriptPath());

		Assert.Contains("flag === KEY.Copy && hasTextSelected()", script, StringComparison.Ordinal);
		Assert.Contains("if (e.shiftKey) return 0;", script, StringComparison.Ordinal);
	}

	[Fact]
	public void FoldersFirst_CanBeTurnedOff()
	{
		Tree["/"] = [new("/z", "Zed", true), new("/a", "a.txt", Size: 1), new("/m", "m.txt", Size: 2)];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.View, new MokaFileView(FoldersFirst: false)));

		Assert.Equal(["a.txt", "m.txt", "Zed"], Names(cut));

		cut.Render(p => p.Add(x => x.View, new MokaFileView(Descending: true, FoldersFirst: false)));
		Assert.Equal(["Zed", "m.txt", "a.txt"], Names(cut));

		cut.Render(p => p.Add(x => x.View, new MokaFileView()));
		Assert.Equal(["Zed", "a.txt", "m.txt"], Names(cut));
	}

	[Fact]
	public void NameCell_DrawsMoreBesideTheName()
	{
		RenderFragment<MokaFileNameContext<FileEntry>> cell = context => builder =>
		{
			builder.AddContent(0, context.Name);
			builder.OpenElement(1, "span");
			builder.AddAttribute(2, "class", "target");
			builder.AddContent(3, $"to {context.Item.Key}");
			builder.CloseElement();
		};

		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.NameCell, cell));

		Assert.Equal("to /a.txt", Row(cut, "a.txt").QuerySelector(".target")!.TextContent);
	}

	[Fact]
	public async Task NameCell_GivesWayToTheRenameField()
	{
		RenderFragment<MokaFileNameContext<FileEntry>> cell = _ => Mark("Drawn");
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Rename, (_, _) => Task.CompletedTask)
			.Add(x => x.NameCell, cell));
		await cut.InvokeAsync(() => cut.Instance.RevealAsync("/a.txt"));

		await Key(cut, "F2");

		Assert.Single(cut.FindAll("[role=option] input"));
		Assert.Equal(3, cut.FindAll("[role=option]").Count(option => option.TextContent.Contains("Drawn", StringComparison.Ordinal)));
	}

	[Fact]
	public void ToolbarStart_ComesBeforeBack()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.ToolbarStart, Mark("Home")));

		string bar = cut.Find(".moka-file-browser__bar").TextContent;
		Assert.StartsWith("Home", bar.TrimStart(), StringComparison.Ordinal);
		Assert.True(
			cut.Find(".moka-file-browser__bar").InnerHtml.IndexOf("Home", StringComparison.Ordinal) <
			cut.Find(".moka-file-browser__bar").InnerHtml.IndexOf("aria-label=\"Back\"", StringComparison.Ordinal));
	}

	[Theory]
	[InlineData(MokaFileControls.Back, "Back")]
	[InlineData(MokaFileControls.Forward, "Forward")]
	[InlineData(MokaFileControls.Up, "Up one folder")]
	[InlineData(MokaFileControls.Refresh, "Refresh")]
	[InlineData(MokaFileControls.ViewMode, "Tiles")]
	public void Controls_HideEachButton(MokaFileControls control, string label)
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Controls, MokaFileControls.All & ~control));

		Assert.Empty(cut.FindAll($"button[aria-label='{label}']"));
		Assert.Single(RenderBrowser().FindAll($"button[aria-label='{label}']"));
	}

	[Fact]
	public void Controls_HideThePathTheFilterAndTheSort()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Controls, MokaFileControls.All & ~(MokaFileControls.Path | MokaFileControls.Filter | MokaFileControls.Sort)));

		Assert.Empty(cut.FindAll(".moka-breadcrumb"));
		Assert.Empty(cut.FindAll(".moka-file-browser__filter"));
		Assert.Empty(cut.FindComponents<MokaSearchInput>());
		Assert.DoesNotContain(cut.FindAll(".moka-file-browser__bar .moka-segment"), s => s.TextContent.Contains("Date", StringComparison.Ordinal));
	}

	[Fact]
	public void Controls_HideNewFolderAndUpload_ButTheActionsStayInTheMenu()
	{
		FakeMenuService menu = UseMenuService();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.CreateFolder, (_, _) => Task.CompletedTask)
			.Add(x => x.Upload, (_, _) => Task.CompletedTask)
			.Add(x => x.Controls, MokaFileControls.All & ~(MokaFileControls.NewFolder | MokaFileControls.Upload)));

		Assert.DoesNotContain("New folder", cut.Find(".moka-file-browser").TextContent, StringComparison.Ordinal);

		cut.Find(".moka-file-browser__main").ContextMenu(new MouseEventArgs { Button = 2, Detail = 1 });
		Assert.NotNull(menu.Named("New folder"));
		Assert.NotNull(menu.Named("Upload"));
	}

	[Fact]
	public void WithNothingToDraw_TheSecondRowGoes()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Controls, MokaFileControls.All & ~(MokaFileControls.Sort | MokaFileControls.ViewMode)));

		Assert.Single(cut.FindAll(".moka-file-browser__bar"));
		Assert.Equal(2, RenderBrowser().FindAll(".moka-file-browser__bar").Count);
	}

	[Fact]
	public void IconOnlyActions_AreNamedIconsThatStillAct()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.IconOnlyActions, true)
			.Add(x => x.CreateFolder, (_, _) => Task.CompletedTask)
			.Add(x => x.Upload, (_, _) => Task.CompletedTask));

		Assert.DoesNotContain(cut.FindAll(".moka-file-browser__bar button"), b => b.TextContent.Contains("New folder", StringComparison.Ordinal));

		Button(cut, "Upload").Click();
		Module.VerifyInvoke("pickFiles");

		Button(cut, "New folder").Click();
		Assert.Single(cut.FindAll(".moka-file-browser__new-row input"));
	}

	[Fact]
	public void ToolbarControl_ReplacesThePath_AndKeepsTheRestWithDefault()
	{
		List<MokaFileControls> asked = [];
		RenderFragment<MokaFileControlContext> control = context =>
		{
			asked.Add(context.Which);
			return context.Which == MokaFileControls.Path ? Mark("Typed path") : context.Default;
		};

		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.ToolbarControl, control));

		Assert.Equal("Typed path", cut.Find(".moka-file-browser__path").TextContent);
		Assert.Empty(cut.FindAll(".moka-breadcrumb"));
		Assert.Single(cut.FindAll("button[aria-label='Back']"));
		Assert.Contains(MokaFileControls.Filter, asked);
		Assert.Contains(MokaFileControls.Sort, asked);
		Assert.DoesNotContain(MokaFileControls.NewFolder, asked);
	}

	[Fact]
	public void ToolbarControl_IsToldWhatTheControlWouldDo()
	{
		string? folder = null;
		RenderFragment<MokaFileControlContext> control = context =>
			context.Which == MokaFileControls.Up ? PressMe("my-up", context.Run!) : context.Default;

		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ToolbarControl, control)
			.Add(x => x.FolderChanged, value => folder = value), "/docs/sub");

		cut.Find("#my-up").Click();

		Assert.Equal("/docs", folder);
	}

	[Fact]
	public void ToolbarControl_DoesNotDrawWhatControlsHides()
	{
		RenderFragment<MokaFileControlContext> control = context => Mark($"[{context.Which}]");

		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ToolbarControl, control)
			.Add(x => x.Controls, MokaFileControls.Back));

		Assert.Contains("[Back]", cut.Markup, StringComparison.Ordinal);
		Assert.DoesNotContain("[Forward]", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void TheFilterParameter_FillsTheFilter()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Filter, "b."));

		Assert.Equal(["b.txt"], Names(cut));
		Assert.Contains("1 of 4 items", Status(cut), StringComparison.Ordinal);

		cut.Render(p => p.Add(x => x.Filter, ""));
		Assert.Equal(4, Names(cut).Length);
	}

	[Fact]
	public async Task FilterChanged_FollowsTheBox_AndAnotherFolderEmptiesIt()
	{
		List<string> filters = [];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.FilterChanged, filters.Add));
		MokaSearchInput search = cut.FindComponent<MokaSearchInput>().Instance;

		await cut.InvokeAsync(() => search.OnSearch.InvokeAsync("d"));
		DoubleClickOn(Row(cut, "Docs"));

		Assert.Equal(["d", ""], filters);
	}

	[Fact]
	public async Task RevealAsync_SelectsTheRow_AndScrollsToIt()
	{
		IReadOnlySet<string>? selected = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.SelectedChanged, value => selected = value));

		await cut.InvokeAsync(() => cut.Instance.RevealAsync("/c.txt"));

		Assert.Equal(["c.txt"], SelectedNames(cut));
		Assert.True(selected!.SetEquals(["/c.txt"]));
		Assert.Equal(3, Module.VerifyInvoke("revealLine").Arguments[1]);
	}

	[Fact]
	public async Task RevealAsync_WhileLoading_WaitsForTheFolder()
	{
		TaskCompletionSource<IReadOnlyList<FileEntry>> answer = new();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Load, (_, _) => answer.Task), ownLoad: true);

		await cut.InvokeAsync(() => cut.Instance.RevealAsync("/b.txt"));
		Assert.Empty(cut.FindAll("[role=option]"));

		await cut.InvokeAsync(() => answer.SetResult(Tree["/"]));

		Assert.Equal(["b.txt"], SelectedNames(cut));
		Module.VerifyInvoke("revealLine");
	}

	[Fact]
	public async Task RevealAsync_OfAnUnknownKey_DoesNothing()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();
		Click(cut, "a.txt");

		await cut.InvokeAsync(() => cut.Instance.RevealAsync("/gone"));

		Assert.Equal(["a.txt"], SelectedNames(cut));
		Module.VerifyNotInvoke("revealLine");
	}

	[Fact]
	public async Task RevealAsync_ClearsAFilterHidingTheRow_ButNotAnItemFilter()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.ItemFilter, entry => entry.Name != "c.txt"));
		MokaSearchInput search = cut.FindComponent<MokaSearchInput>().Instance;
		await cut.InvokeAsync(() => search.OnSearch.InvokeAsync("b."));

		await cut.InvokeAsync(() => cut.Instance.RevealAsync("/a.txt"));
		Assert.Equal(["a.txt"], SelectedNames(cut));
		Assert.Equal(3, Names(cut).Length);

		await cut.InvokeAsync(() => cut.Instance.RevealAsync("/c.txt"));
		Assert.Equal(["a.txt"], SelectedNames(cut));
	}

	[Fact]
	public async Task NavigateAsync_ToAFolderStillLoading_WaitsToSelect()
	{
		TaskCompletionSource<IReadOnlyList<FileEntry>> answer = new();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Load, (_, _) => answer.Task), ownLoad: true);

		await cut.InvokeAsync(() => cut.Instance.NavigateAsync("/", select: "/b.txt"));
		await cut.InvokeAsync(() => answer.SetResult(Tree["/"]));

		Assert.Equal(["b.txt"], SelectedNames(cut));
	}

	[Fact]
	public async Task NavigateAsync_GoesThere_AndSelectsAfterItArrives()
	{
		string? folder = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.FolderChanged, value => folder = value));

		await cut.InvokeAsync(() => cut.Instance.NavigateAsync("/docs", select: "/docs/x.md"));

		Assert.Equal("/docs", folder);
		Assert.Equal(["x.md"], SelectedNames(cut));
		Assert.False(Button(cut, "Back").HasAttribute("disabled"));
	}

	[Fact]
	public async Task NavigateAsync_ToTheFolderOnScreen_OnlyReveals()
	{
		int loads = 0;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Load, (folder, token) =>
		{
			loads++;
			return LoadTree(folder, token);
		}), ownLoad: true);

		await cut.InvokeAsync(() => cut.Instance.NavigateAsync("/", select: "/b.txt"));

		Assert.Equal(1, loads);
		Assert.Equal(["b.txt"], SelectedNames(cut));
	}

	[Fact]
	public void ReselectOnReturn_SelectsTheFolderLeft_GoingUpOrBack()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.ReselectOnReturn, true));

		DoubleClickOn(Row(cut, "Docs"));
		cut.Render(p => p.Add(x => x.Crumbs, CrumbsFor("/docs")));
		ClickOn(Button(cut, "Up one folder"));
		Assert.Equal(["Docs"], SelectedNames(cut));

		DoubleClickOn(Row(cut, "Docs"));
		ClickOn(Button(cut, "Back"));
		Assert.Equal(["Docs"], SelectedNames(cut));
	}

	[Fact]
	public void WithoutReselectOnReturn_NothingIsSelected()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		DoubleClickOn(Row(cut, "Docs"));
		ClickOn(Button(cut, "Back"));

		Assert.Empty(SelectedNames(cut));
	}

	[Theory]
	[InlineData(MokaFileKeys.SelectAll, "a", true)]
	[InlineData(MokaFileKeys.Move, "ArrowDown", false)]
	[InlineData(MokaFileKeys.Move, "ArrowDown", true)]
	[InlineData(MokaFileKeys.Toggle, " ", false)]
	public async Task AKeyLeftOut_DoesNothing(MokaFileKeys left, string key, bool ctrl)
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Keys, MokaFileKeys.All & ~left));
		Click(cut, "a.txt");

		int line = await Key(cut, key, ctrl: ctrl);

		Assert.Equal(-1, line);
		Assert.Equal(["a.txt"], SelectedNames(cut));
		Assert.Equal(cut.Find("[role=option][aria-selected=true]").Id, cut.Find("[role=listbox]").GetAttribute("aria-activedescendant"));
	}

	[Fact]
	public async Task TypeAhead_CanBeLeftOut()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Keys, MokaFileKeys.All & ~MokaFileKeys.TypeAhead));

		await cut.InvokeAsync(() => cut.Instance.HandleTypeAhead("b"));

		Assert.Empty(SelectedNames(cut));
	}

	[Fact]
	public void TheScript_IsToldTheKeys_OnlyWhenSomeAreLeftOut()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Keys, MokaFileKeys.Copy | MokaFileKeys.Cut));

		Assert.Equal("768", cut.Find(".moka-file-browser").GetAttribute("data-moka-file-keys"));
		Assert.False(RenderBrowser().Find(".moka-file-browser").HasAttribute("data-moka-file-keys"));
	}

	[Fact]
	public void TheMenu_DoesntAdvertiseAKeyLeftOut()
	{
		FakeMenuService menu = UseMenuService();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Keys, MokaFileKeys.All & ~MokaFileKeys.Rename)
			.Add(x => x.Rename, (_, _) => Task.CompletedTask)
			.Add(x => x.Delete, _ => Task.CompletedTask));

		Row(cut, "a.txt").ContextMenu(new MouseEventArgs { Button = 2, Detail = 1 });

		Assert.Null(menu.Named("Rename").Shortcut);
		Assert.Equal("Del", menu.Named("Delete").Shortcut);
	}

	[Fact]
	public async Task FailureActions_RenderInTheAlert_AndCanRetry()
	{
		int attempts = 0;
		MokaFileFailure? shown = null;
		RenderFragment<MokaFileFailure> actions = failure =>
		{
			shown = failure;
			return PressMe("retry", failure.Retry!);
		};
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.FailureActions, actions)
			.Add(x => x.ConfirmDelete, _ => Task.FromResult(true))
			.Add(x => x.Delete, _ => ++attempts == 1 ? throw new InvalidOperationException("Denied") : Task.CompletedTask));
		Click(cut, "a.txt");
		await Key(cut, "Delete");

		Assert.Equal("Denied", shown!.Error.Message);
		Assert.Single(cut.FindAll(".moka-alert-fix #retry"));

		ClickOn(cut.Find("#retry"));

		Assert.Equal(2, attempts);
		Assert.Empty(cut.FindAll(".moka-alert"));
	}

	[Fact]
	public async Task WithoutFailureActions_TheAlertHasNoActionArea()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ConfirmDelete, _ => Task.FromResult(true))
			.Add(x => x.Delete, _ => throw new InvalidOperationException("Denied")));
		Click(cut, "a.txt");

		await Key(cut, "Delete");

		Assert.Empty(cut.FindAll(".moka-alert-fix"));
	}

	[Fact]
	public async Task ACutPasted_WithFailureReported_StaysHeld()
	{
		MokaFileBrowser<FileEntry>? browser = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Move, (_, _) => browser!.ReportFailureAsync()));
		browser = cut.Instance;
		Click(cut, "a.txt");
		await Key(cut, "x", ctrl: true);

		await Key(cut, "v", ctrl: true);

		Assert.Contains("1 to move", Status(cut), StringComparison.Ordinal);
		Assert.True(browser.Held!.IsCut);
		Assert.Empty(cut.FindAll(".moka-alert"));
	}

	[Fact]
	public async Task ACutPasted_WithoutAComplaint_IsLetGo()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Move, (_, _) => Task.CompletedTask));
		Click(cut, "a.txt");
		await Key(cut, "x", ctrl: true);

		await Key(cut, "v", ctrl: true);

		Assert.Null(cut.Instance.Held);
	}

	[Fact]
	public async Task ACutPasted_ThatThrows_StaysHeld_AndRetryPastesAgain()
	{
		int attempts = 0;
		MokaFileFailure? shown = null;
		RenderFragment<MokaFileFailure> actions = failure =>
		{
			shown = failure;
			return Mark("");
		};
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.FailureActions, actions)
			.Add(x => x.Move, (_, _) => ++attempts == 1 ? throw new InvalidOperationException("Denied") : Task.CompletedTask));
		Click(cut, "a.txt");
		await Key(cut, "x", ctrl: true);
		await Key(cut, "v", ctrl: true);
		Assert.NotNull(cut.Instance.Held);

		await cut.InvokeAsync(shown!.Retry!);

		Assert.Equal(2, attempts);
		Assert.Null(cut.Instance.Held);
	}

	[Fact]
	public async Task ReportFailureAsync_WithAnError_ShowsItInTheAlert_ButNoRetryOutsideAnAction()
	{
		MokaFileFailure? shown = null;
		RenderFragment<MokaFileFailure> actions = failure =>
		{
			shown = failure;
			return Mark("");
		};
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.FailureActions, actions));

		await cut.InvokeAsync(() => cut.Instance.ReportFailureAsync(new InvalidOperationException("From the app")));

		Assert.Contains("From the app", cut.Find(".moka-alert").TextContent, StringComparison.Ordinal);
		Assert.Null(shown!.Retry);
	}

	[Fact]
	public async Task ReportFailureAsync_WithAnErrorFromAnAction_KeepsTheAlertThroughTheReload()
	{
		MokaFileBrowser<FileEntry>? browser = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Move, (_, _) => browser!.ReportFailureAsync(new InvalidOperationException("Half of it moved"))));
		browser = cut.Instance;
		Click(cut, "a.txt");
		await Key(cut, "x", ctrl: true);

		await Key(cut, "v", ctrl: true);

		Assert.Contains("Half of it moved", cut.Find(".moka-alert").TextContent, StringComparison.Ordinal);
		Assert.NotNull(browser.Held);
	}

	[Fact]
	public void ByDefault_AFailedNavigation_ReplacesTheList()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Load, (folder, token) =>
			folder == "/docs" ? throw new InvalidOperationException("Denied") : LoadTree(folder, token)), ownLoad: true);

		DoubleClickOn(Row(cut, "Docs"));

		Assert.Contains("Denied", cut.Find(".moka-callout").TextContent, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll("[role=option]"));
	}

	[Fact]
	public void KeepListOnLoadFailure_KeepsTheList_AndTheFolder_AndTheHistory()
	{
		List<string> folders = [];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.KeepListOnLoadFailure, true)
			.Add(x => x.FolderChanged, folders.Add)
			.Add(x => x.Load, (folder, token) => folder == "/docs" ? throw new InvalidOperationException("Denied") : LoadTree(folder, token)), ownLoad: true);
		Click(cut, "a.txt");

		DoubleClickOn(Row(cut, "Docs"));

		Assert.Contains("Denied", cut.Find(".moka-alert").TextContent, StringComparison.Ordinal);
		Assert.Contains("didn't open", cut.Find(".moka-alert").TextContent, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll(".moka-callout"));
		Assert.Equal(4, Names(cut).Length);
		Assert.Equal(["a.txt"], SelectedNames(cut));
		Assert.Empty(folders);
		Assert.True(Button(cut, "Back").HasAttribute("disabled"));
	}

	[Fact]
	public async Task KeepListOnLoadFailure_ShowsTheOldListUntilTheNewOneArrives()
	{
		TaskCompletionSource<IReadOnlyList<FileEntry>> docs = new();
		string? folder = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.KeepListOnLoadFailure, true)
			.Add(x => x.FolderChanged, value => folder = value)
			.Add(x => x.Load, (key, token) => key == "/docs" ? docs.Task : LoadTree(key, token)), ownLoad: true);

		DoubleClickOn(Row(cut, "Docs"));
		Assert.Equal(4, Names(cut).Length);
		Assert.True(cut.Instance.IsBusy);
		Assert.Null(folder);

		await cut.InvokeAsync(() => docs.SetResult(Tree["/docs"]));

		Assert.Equal(["Sub", "x.md"], Names(cut));
		Assert.Equal("/docs", folder);
		Assert.False(Button(cut, "Back").HasAttribute("disabled"));
	}

	[Fact]
	public void KeepListOnLoadFailure_RetryThroughTheAlertGoesThere()
	{
		bool denied = true;
		MokaFileFailure? shown = null;
		RenderFragment<MokaFileFailure> actions = failure =>
		{
			shown = failure;
			return PressMe("again", failure.Retry!);
		};
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.KeepListOnLoadFailure, true)
			.Add(x => x.FailureActions, actions)
			.Add(x => x.Load, (folder, token) => folder == "/docs" && denied ? throw new InvalidOperationException("Denied") : LoadTree(folder, token)), ownLoad: true);
		DoubleClickOn(Row(cut, "Docs"));
		Assert.NotNull(shown);

		denied = false;
		cut.Find("#again").Click();

		Assert.Equal(["Sub", "x.md"], Names(cut));
		Assert.Empty(cut.FindAll(".moka-alert"));
	}

	[Fact]
	public void KeepListOnLoadFailure_BackThatFails_StaysPut()
	{
		bool denied = false;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.KeepListOnLoadFailure, true)
			.Add(x => x.Load, (folder, token) => denied ? throw new InvalidOperationException("Denied") : LoadTree(folder, token)), ownLoad: true);
		DoubleClickOn(Row(cut, "Docs"));

		denied = true;
		ClickOn(Button(cut, "Back"));

		Assert.Equal(["Sub", "x.md"], Names(cut));
		Assert.False(Button(cut, "Back").HasAttribute("disabled"));
		Assert.True(Button(cut, "Forward").HasAttribute("disabled"));
	}

	[Fact]
	public void KeepListOnLoadFailure_AFolderFromOutsideThatFails_TellsTheParentWhereItStayed()
	{
		string? told = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.KeepListOnLoadFailure, true)
			.Add(x => x.FolderChanged, value => told = value)
			.Add(x => x.Load, (folder, token) => folder == "/docs" ? throw new InvalidOperationException("Denied") : LoadTree(folder, token)), ownLoad: true);

		cut.Render(p => p.Add(x => x.Folder, "/docs"));

		Assert.Equal("/", told);
		Assert.Equal(4, Names(cut).Length);
		Assert.Single(cut.FindAll(".moka-alert"));
	}

	[Fact]
	public async Task KeepListOnLoadFailure_AReloadThatFails_KeepsTheList()
	{
		bool broken = false;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.KeepListOnLoadFailure, true)
			.Add(x => x.Load, (folder, token) => broken ? throw new InvalidOperationException("Gone") : LoadTree(folder, token)), ownLoad: true);

		broken = true;
		await cut.InvokeAsync(() => cut.Instance.ReloadAsync());

		Assert.Equal(4, Names(cut).Length);
		Assert.Contains("Gone", cut.Find(".moka-alert").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void KeepListOnLoadFailure_WithNothingToKeep_ShowsTheProblem()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.KeepListOnLoadFailure, true)
			.Add(x => x.Load, (_, _) => throw new InvalidOperationException("Denied")), ownLoad: true);

		Assert.Single(cut.FindAll(".moka-callout"));
	}

	[Fact]
	public async Task Held_SaysWhatAndHow_AndOnHeldChangedFollows()
	{
		List<MokaFileHeld<FileEntry>?> heard = [];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Move, (_, _) => Task.CompletedTask)
			.Add(x => x.Copy, (_, _) => Task.CompletedTask)
			.Add(x => x.OnHeldChanged, value => heard.Add(value)));
		Assert.Null(cut.Instance.Held);
		Click(cut, "a.txt");

		await Key(cut, "x", ctrl: true);
		Assert.True(cut.Instance.Held!.IsCut);
		Assert.Equal(["a.txt"], cut.Instance.Held.Items.Select(item => item.Name));

		await Key(cut, "c", ctrl: true);
		Assert.False(cut.Instance.Held!.IsCut);

		await cut.InvokeAsync(() => cut.Instance.ClearHeldAsync());
		Assert.Null(cut.Instance.Held);

		Assert.Equal(3, heard.Count);
		Assert.True(heard[0]!.IsCut);
		Assert.False(heard[1]!.IsCut);
		Assert.Null(heard[2]);
	}

	[Fact]
	public async Task PasteAsync_GoesIntoTheFolderNamed()
	{
		string? target = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Move, (_, folder) =>
		{
			target = folder;
			return Task.CompletedTask;
		}));
		Click(cut, "a.txt");
		await Key(cut, "x", ctrl: true);

		await cut.InvokeAsync(() => cut.Instance.PasteAsync("/docs"));

		Assert.Equal("/docs", target);
		Assert.Null(cut.Instance.Held);
	}

	[Fact]
	public async Task PasteAsync_DoesntPutAFolderInsideItself()
	{
		IReadOnlyList<FileEntry>? moved = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Move, (items, _) =>
		{
			moved = items;
			return Task.CompletedTask;
		}));
		Click(cut, "Docs");
		await Key(cut, "x", ctrl: true);

		await cut.InvokeAsync(() => cut.Instance.PasteAsync("/docs"));

		Assert.Null(moved);
		Assert.NotNull(cut.Instance.Held);
	}

	[Fact]
	public async Task OnCopyUnavailable_IsRaisedWhenCopyIsntOffered()
	{
		int told = 0;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Move, (_, _) => Task.CompletedTask)
			.Add(x => x.OnCopyUnavailable, () => told++));

		await Key(cut, "c", ctrl: true);
		Assert.Equal(0, told);

		Click(cut, "a.txt");
		await Key(cut, "c", ctrl: true);
		Assert.Equal(1, told);
		Assert.Null(cut.Instance.Held);
	}

	[Fact]
	public async Task WithEscapeDropsHeldLeftOut_EscapeKeepsWhatIsCut()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Keys, MokaFileKeys.All & ~MokaFileKeys.EscapeDropsHeld)
			.Add(x => x.Move, (_, _) => Task.CompletedTask));
		Click(cut, "a.txt");
		await Key(cut, "x", ctrl: true);

		await Key(cut, "Escape");

		Assert.NotNull(cut.Instance.Held);
		Assert.Empty(SelectedNames(cut));
	}

	[Fact]
	public void Empty_ReplacesTheEmptyState_AndSaysWhy()
	{
		MokaFileEmptyContext? told = null;
		Tree["/"] = [new FileEntry("/.hidden", ".hidden")];
		RenderFragment<MokaFileEmptyContext> empty = context =>
		{
			told = context;
			return Mark("Only hidden items");
		};

		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Empty, empty)
			.Add(x => x.ItemFilter, entry => !entry.Name.StartsWith('.')));

		Assert.Contains("Only hidden items", cut.Find(".moka-file-browser__state").TextContent, StringComparison.Ordinal);
		Assert.Equal(1, told!.Hidden);
		Assert.Equal("", told.Filter);
		Assert.Empty(cut.FindAll(".moka-empty-state"));
	}

	[Fact]
	public async Task Empty_CanFallBackAndClearTheFilter()
	{
		MokaFileEmptyContext? told = null;
		RenderFragment<MokaFileEmptyContext> empty = context =>
		{
			told = context;
			return context.Default;
		};
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Empty, empty));
		MokaSearchInput search = cut.FindComponent<MokaSearchInput>().Instance;

		await cut.InvokeAsync(() => search.OnSearch.InvokeAsync("zzz"));
		Assert.Equal("zzz", told!.Filter);
		Assert.Single(cut.FindAll(".moka-empty-state"));

		await cut.InvokeAsync(told.ClearFilter);
		Assert.Equal(4, Names(cut).Length);
	}

	[Fact]
	public void ItemFilter_HidesItems_WithoutReadingTheFolderAgain_AndCountsOnlyTheRest()
	{
		bool showHidden = false;
		int loads = 0;
		Tree["/"] = [new("/.env", ".env"), new("/a.txt", "a.txt")];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ItemFilter, entry => showHidden || !entry.Name.StartsWith('.'))
			.Add(x => x.Load, (folder, token) =>
			{
				loads++;
				return LoadTree(folder, token);
			}), ownLoad: true);

		Assert.Equal(["a.txt"], Names(cut));
		Assert.Contains("1 item", Status(cut), StringComparison.Ordinal);

		showHidden = true;
		cut.Render();

		Assert.Equal([".env", "a.txt"], Names(cut));
		Assert.Contains("2 items", Status(cut), StringComparison.Ordinal);
		Assert.Equal(1, loads);
	}

	[Fact]
	public void ItemFilter_DropsWhatItHidesFromTheSelection()
	{
		IReadOnlySet<string>? selected = null;
		bool hide = false;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ItemFilter, entry => !hide || entry.Name != "a.txt")
			.Add(x => x.SelectedChanged, value => selected = value));
		Click(cut, "a.txt");

		hide = true;
		cut.Render();

		Assert.Empty(selected!);
		Assert.Equal(3, Names(cut).Length);
	}

	[Fact]
	public void ItemFilter_Removed_ShowsEverythingAgain()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.ItemFilter, entry => entry.Name == "a.txt"));
		Assert.Single(Names(cut));

		cut.Render(p => p.Add(x => x.ItemFilter, (Func<FileEntry, bool>?)null));

		Assert.Equal(4, Names(cut).Length);
	}

	[Fact]
	public void FitWidths_ReachTheScript_OnlyWhenSet()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.FitWidths, new MokaFileFitWidths(20.5, 40)));

		IElement root = cut.Find(".moka-file-browser");
		Assert.Contains("moka-file-browser--own-fit", root.ClassName, StringComparison.Ordinal);
		Assert.Equal("20.5", root.GetAttribute("data-moka-fit-medium"));
		Assert.Equal("40", root.GetAttribute("data-moka-fit-wide"));

		IElement plain = RenderBrowser().Find(".moka-file-browser");
		Assert.DoesNotContain("own-fit", plain.ClassName, StringComparison.Ordinal);
		Assert.False(plain.HasAttribute("data-moka-fit-medium"));
	}

	[Fact]
	public async Task IsBusy_FollowsTheLoad_AndOnBusyChangedSaysSo()
	{
		TaskCompletionSource<IReadOnlyList<FileEntry>> answer = new();
		List<bool> heard = [];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Load, (_, _) => answer.Task)
			.Add(x => x.OnBusyChanged, value => heard.Add(value)), ownLoad: true);

		Assert.True(cut.Instance.IsBusy);

		await cut.InvokeAsync(() => answer.SetResult(Tree["/"]));

		Assert.False(cut.Instance.IsBusy);
		Assert.Equal([true, false], heard);
	}

	[Fact]
	public void ByteUnits_ApplyToTheColumnAndTheStatusBar()
	{
		Tree["/"] = [new FileEntry("/a", "a", Size: 2048)];

		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.ByteUnits, MokaByteUnits.Binary));

		Assert.Contains("2 KiB", cut.Find(".moka-file-browser__cell").TextContent, StringComparison.Ordinal);
		Assert.Contains("2 KiB", Status(cut), StringComparison.Ordinal);
		Assert.Contains("2 KB", Status(RenderBrowser()), StringComparison.Ordinal);
	}

	[Fact]
	public void ByteFormatter_WinsOverByteUnits()
	{
		Tree["/"] = [new FileEntry("/a", "a", Size: 2048)];

		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ByteUnits, MokaByteUnits.Binary)
			.Add(x => x.ByteFormatter, bytes => $"{MokaByteFormat.Format(bytes)}!"));

		Assert.Contains("2 KB!", Status(cut), StringComparison.Ordinal);
	}

	[Fact]
	public async Task OnOpenFiles_TakesEnterOnSeveralFiles()
	{
		IReadOnlyList<FileEntry>? opened = null;
		FileEntry? single = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.OnOpenFile, entry => single = entry)
			.Add(x => x.OnOpenFiles, entries => opened = entries));
		Click(cut, "a.txt", ctrl: true);
		Click(cut, "b.txt", ctrl: true);

		await Key(cut, "Enter");

		Assert.Equal(["a.txt", "b.txt"], opened!.Select(e => e.Name));
		Assert.Null(single);
	}

	[Fact]
	public async Task OnOpenFiles_LeavesAFolderInTheSelectionAlone()
	{
		IReadOnlyList<FileEntry>? opened = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.OnOpenFiles, entries => opened = entries));
		Click(cut, "a.txt", ctrl: true);
		Click(cut, "Docs", ctrl: true);

		await Key(cut, "Enter");

		Assert.Null(opened);
		Assert.Equal(["Sub", "x.md"], Names(cut));
	}

	[Fact]
	public async Task WithoutOnOpenFiles_EnterOpensTheRowUnderTheCursor()
	{
		FileEntry? opened = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.OnOpenFile, entry => opened = entry));
		Click(cut, "a.txt", ctrl: true);
		Click(cut, "b.txt", ctrl: true);

		await Key(cut, "Enter");

		Assert.Equal("b.txt", opened!.Name);
	}

	[Fact]
	public async Task AfterADelete_TheCursorStaysInPlace_AndFocusIsGivenBack()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ConfirmDelete, _ => Task.FromResult(true))
			.Add(x => x.Delete, entries =>
			{
				Tree["/"] = [.. Tree["/"].Where(entry => !entries.Contains(entry))];
				return Task.CompletedTask;
			}));
		Click(cut, "a.txt");

		await Key(cut, "Delete");

		Assert.Equal(["Docs", "b.txt", "c.txt"], Names(cut));
		Assert.Empty(SelectedNames(cut));
		Assert.Equal(Row(cut, "b.txt").Id, cut.Find("[role=listbox]").GetAttribute("aria-activedescendant"));
		Module.VerifyInvoke("focusListIfLost");

		await Key(cut, "ArrowDown");
		Assert.Equal(["c.txt"], SelectedNames(cut));
	}

	[Fact]
	public async Task AfterDeletingTheLastRow_TheCursorMovesUpOne()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ConfirmDelete, _ => Task.FromResult(true))
			.Add(x => x.Delete, entries =>
			{
				Tree["/"] = [.. Tree["/"].Where(entry => !entries.Contains(entry))];
				return Task.CompletedTask;
			}));
		Click(cut, "c.txt");

		await Key(cut, "Delete");

		Assert.Equal(Row(cut, "b.txt").Id, cut.Find("[role=listbox]").GetAttribute("aria-activedescendant"));
	}

	[Fact]
	public void DragText_ReachesTheRows()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Move, (_, _) => Task.CompletedTask)
			.Add(x => x.DragText, entry => entry.Key));

		IElement row = Row(cut, "a.txt");
		Assert.Equal("/a.txt", row.GetAttribute("data-moka-drag-text"));
		Assert.False(row.HasAttribute("data-moka-drag-only"));
		Assert.False(Row(RenderBrowser(p => p.Add(x => x.Move, (_, _) => Task.CompletedTask)), "a.txt").HasAttribute("data-moka-drag-text"));
	}

	[Fact]
	public void DragText_MakesRowsDraggable_WithoutMove()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.DragText, entry => entry.Key));

		IElement row = Row(cut, "a.txt");
		Assert.Equal("true", row.GetAttribute("draggable"));
		Assert.True(row.HasAttribute("data-moka-drag-only"));
	}

	private static string ScriptPath()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "Moka.Red.slnx")))
			{
				return Path.Combine(folder.FullName, "src", "Moka.Red.Data", "FileBrowser", "MokaFileBrowser.razor.js");
			}
		}

		throw new InvalidOperationException("The repository root wasn't found above the test output.");
	}

	[GeneratedRegex(@"const KEY = \{(.*?)\};", RegexOptions.Singleline)]
	private static partial Regex KeyTable();

	[GeneratedRegex(@"(\w+):\s*(\d+)")]
	private static partial Regex KeyEntry();
}
