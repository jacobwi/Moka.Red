using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.ContextMenu;
using Moka.Red.Data.FileBrowser;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Forms.InlineEdit;

namespace Moka.Red.Data.Tests.Components;

public class MokaFileBrowserActionTests : MokaFileBrowserKit
{
	private static MouseEventArgs RightClick => new() { Button = 2, Detail = 1, ClientX = 40, ClientY = 50 };

	[Fact]
	public void ActionsLeftNull_DontAppear()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		Assert.DoesNotContain("New folder", cut.Markup, StringComparison.Ordinal);
		Assert.DoesNotContain("Upload", cut.Markup, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll("input[type=file]"));
	}

	[Fact]
	public void ActionsGiven_Appear()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.CreateFolder, (_, _) => Task.CompletedTask)
			.Add(x => x.Upload, (_, _) => Task.CompletedTask));

		Assert.Contains("New folder", cut.Markup, StringComparison.Ordinal);
		Assert.Single(cut.FindAll("input[type=file]"));
	}

	[Fact]
	public async Task F2_OpensTheInlineEditor_AndSavingRenamesAndReloads()
	{
		(string Key, string Name)? renamed = null;
		int loads = 0;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Load, (folder, token) =>
			{
				loads++;
				return LoadTree(folder, token);
			})
			.Add(x => x.Rename, (entry, name) =>
			{
				renamed = (entry.Key, name);
				Tree["/"] = [.. Tree["/"].Select(e => e == entry ? e with { Name = name } : e)];
				return Task.CompletedTask;
			}), ownLoad: true);
		Click(cut, "a.txt");

		await Key(cut, "F2");
		IElement field = cut.Find("[role=option] input");
		Type(field, "renamed.txt");
		Press(field, "Enter");

		Assert.Equal(("/a.txt", "renamed.txt"), renamed);
		Assert.Equal(2, loads);
		Assert.Contains("renamed.txt", Names(cut));
		Assert.Equal(["renamed.txt"], SelectedNames(cut));
		Assert.Empty(cut.FindAll("[role=option] input"));
		Module.VerifyInvoke("focusListIfLost");
	}

	[Fact]
	public async Task AFailedRename_ShowsInTheField_AndKeepsItOpen()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Rename, (_, _) => throw new InvalidOperationException("Name is taken")));
		Click(cut, "a.txt");

		await Key(cut, "F2");
		IElement field = cut.Find("[role=option] input");
		Type(field, "b.txt");
		Press(field, "Enter");

		Assert.Equal("Name is taken", cut.Find(".moka-inline-edit__error").TextContent);
		Assert.Single(cut.FindAll("[role=option] input"));
	}

	[Fact]
	public async Task ValidateName_BlocksARename()
	{
		bool renamed = false;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ValidateName, name => name.Contains('/', StringComparison.Ordinal) ? "No slashes" : null)
			.Add(x => x.Rename, (_, _) =>
			{
				renamed = true;
				return Task.CompletedTask;
			}));
		Click(cut, "a.txt");

		await Key(cut, "F2");
		IElement field = cut.Find("[role=option] input");
		Type(field, "x/y");
		Press(field, "Enter");

		Assert.False(renamed);
		Assert.Equal("No slashes", cut.Find(".moka-inline-edit__error").TextContent);
	}

	[Fact]
	public async Task WhileRenaming_ListKeysDoNothing()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Rename, (_, _) => Task.CompletedTask));
		Click(cut, "a.txt");
		await Key(cut, "F2");

		await Key(cut, "ArrowDown");

		Assert.Single(cut.FindAll("[role=option][aria-selected=true] input"));
	}

	[Fact]
	public async Task EscapeInTheEditor_CancelsWithoutRenaming()
	{
		bool renamed = false;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Rename, (_, _) =>
		{
			renamed = true;
			return Task.CompletedTask;
		}));
		Click(cut, "a.txt");
		await Key(cut, "F2");

		Press(cut.Find("[role=option] input"), "Escape");

		Assert.False(renamed);
		Assert.Empty(cut.FindAll("[role=option] input"));
	}

	[Fact]
	public async Task RenamingAFile_SelectsItsStem()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Rename, (_, _) => Task.CompletedTask));
		Click(cut, "a.txt");

		await Key(cut, "F2");

		Assert.Equal(MokaInlineEditSelection.Stem, cut.FindComponent<MokaInlineEdit>().Instance.Selection);
	}

	[Fact]
	public void NewFolder_OpensAFieldWithAFreeName_AndEnterMakesIt()
	{
		Tree["/"] = [.. Tree["/"], new FileEntry("/nf", "New folder", true)];
		(string Folder, string Name)? made = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.CreateFolder, (folder, name) =>
		{
			made = (folder, name);
			Tree["/"] = [.. Tree["/"], new FileEntry($"/{name}", name, true)];
			return Task.CompletedTask;
		}));

		cut.FindAll(".moka-file-browser__bar button").Single(b => b.TextContent.Contains("New folder", StringComparison.Ordinal)).Click();
		IElement field = cut.Find(".moka-file-browser__new-row input");
		Assert.Equal("New folder 2", field.GetAttribute("value"));

		field.KeyDown(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal(("/", "New folder 2"), made);
		Assert.Empty(cut.FindAll(".moka-file-browser__new-row"));
		Assert.Equal(["New folder 2"], SelectedNames(cut));
	}

	[Fact]
	public void NewFolder_WorksInAnEmptyFolder()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.CreateFolder, (_, _) => Task.CompletedTask), "/empty");

		cut.FindAll(".moka-file-browser__bar button").Single(b => b.TextContent.Contains("New folder", StringComparison.Ordinal)).Click();

		Assert.Single(cut.FindAll(".moka-file-browser__new-row input"));
		Assert.Empty(cut.FindAll(".moka-empty-state"));
	}

	[Fact]
	public async Task Delete_AsksThroughTheOwnDialog_AndDeletesOnConfirm()
	{
		IReadOnlyList<FileEntry>? deleted = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Delete, entries =>
		{
			deleted = entries;
			return Task.CompletedTask;
		}));
		Click(cut, "a.txt");

		await Key(cut, "Delete");

		Assert.Null(deleted);
		IRenderedComponent<MokaDialog> dialog = cut.FindComponent<MokaDialog>();
		Assert.True(dialog.Instance.Open);
		Assert.Contains("a.txt", dialog.Markup, StringComparison.Ordinal);

		ClickOn(dialog.FindAll("button").Single(b => b.TextContent == "Delete"));

		Assert.Equal(["a.txt"], deleted!.Select(e => e.Name));
		Assert.Empty(cut.FindComponents<MokaDialog>());
	}

	[Fact]
	public async Task CancellingTheDialog_DeletesNothing()
	{
		bool deleted = false;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Delete, _ =>
		{
			deleted = true;
			return Task.CompletedTask;
		}));
		Click(cut, "a.txt");
		await Key(cut, "Delete");

		ClickOn(cut.FindComponent<MokaDialog>().FindAll("button").Single(b => b.TextContent == "Cancel"));

		Assert.False(deleted);
		Assert.Empty(cut.FindComponents<MokaDialog>());
	}

	[Fact]
	public async Task ConfirmDelete_ReplacesTheDialog()
	{
		bool answer = false;
		int deletes = 0;
		int loads = 0;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Load, (folder, token) =>
			{
				loads++;
				return LoadTree(folder, token);
			})
			.Add(x => x.ConfirmDelete, _ => Task.FromResult(answer))
			.Add(x => x.Delete, _ =>
			{
				deletes++;
				return Task.CompletedTask;
			}), ownLoad: true);
		Click(cut, "a.txt");

		await Key(cut, "Delete");
		Assert.Equal(0, deletes);
		Assert.Empty(cut.FindComponents<MokaDialog>());

		answer = true;
		await Key(cut, "Delete");
		Assert.Equal(1, deletes);
		Assert.True(loads >= 2);
	}

	[Fact]
	public async Task AThrownDelete_ShowsAnAlert_AndKeepsTheRows()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ConfirmDelete, _ => Task.FromResult(true))
			.Add(x => x.Delete, _ => throw new InvalidOperationException("Read-only disk")));
		Click(cut, "a.txt");

		await Key(cut, "Delete");

		Assert.Contains("Read-only disk", cut.Find(".moka-alert").TextContent, StringComparison.Ordinal);
		Assert.Equal(4, Names(cut).Length);
	}

	[Fact]
	public async Task DroppingOnAFolder_MovesTheSelection()
	{
		(IReadOnlyList<FileEntry> Items, string Target)? moved = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Move, (items, target) =>
		{
			moved = (items, target);
			return Task.CompletedTask;
		}));
		Click(cut, "a.txt", ctrl: true);
		Click(cut, "b.txt", ctrl: true);

		await cut.InvokeAsync(() => cut.Instance.HandleDrop("/a.txt", "/docs"));

		Assert.Equal("/docs", moved!.Value.Target);
		Assert.Equal(["a.txt", "b.txt"], moved.Value.Items.Select(e => e.Name));
	}

	[Fact]
	public async Task DroppingARowOutsideTheSelection_MovesOnlyIt()
	{
		IReadOnlyList<FileEntry>? moved = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Move, (items, _) =>
		{
			moved = items;
			return Task.CompletedTask;
		}));
		Click(cut, "a.txt");

		await cut.InvokeAsync(() => cut.Instance.HandleDrop("/c.txt", "/docs"));

		Assert.Equal(["c.txt"], moved!.Select(e => e.Name));
	}

	[Fact]
	public async Task AFolder_CantBeDroppedOnItself()
	{
		bool moved = false;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Move, (_, _) =>
		{
			moved = true;
			return Task.CompletedTask;
		}));

		await cut.InvokeAsync(() => cut.Instance.HandleDrop("/docs", "/docs"));

		Assert.False(moved);
	}

	[Fact]
	public async Task CutThenPaste_MovesIntoTheFolderOnScreen()
	{
		(IReadOnlyList<FileEntry> Items, string Target)? moved = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Move, (items, target) =>
		{
			moved = (items, target);
			return Task.CompletedTask;
		}));
		Click(cut, "a.txt");

		await Key(cut, "x", ctrl: true);
		Assert.Contains("1 to move", cut.Find(".moka-file-browser__status").TextContent, StringComparison.Ordinal);
		Assert.Contains("moka-file-browser__row--held", Row(cut, "a.txt").ClassName, StringComparison.Ordinal);

		DoubleClickOn(Row(cut, "Docs"));
		await Key(cut, "v", ctrl: true);

		Assert.Equal("/docs", moved!.Value.Target);
		Assert.Equal(["a.txt"], moved.Value.Items.Select(e => e.Name));
		Assert.DoesNotContain("to move", cut.Find(".moka-file-browser__status").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task EscapeLetsGoOfCutItems()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Move, (_, _) => Task.CompletedTask));
		Click(cut, "a.txt");
		await Key(cut, "x", ctrl: true);

		await Key(cut, "Escape");

		Assert.DoesNotContain("to move", cut.Find(".moka-file-browser__status").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task CopyAndDuplicate_GoThroughCopy()
	{
		List<string> targets = [];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Copy, (_, target) =>
		{
			targets.Add(target);
			return Task.CompletedTask;
		}));
		Click(cut, "a.txt");

		await Key(cut, "d", ctrl: true);
		await Key(cut, "c", ctrl: true);
		Assert.Contains("1 to copy", cut.Find(".moka-file-browser__status").TextContent, StringComparison.Ordinal);
		await Key(cut, "v", ctrl: true);

		Assert.Equal(["/", "/"], targets);
		Assert.Contains("1 to copy", cut.Find(".moka-file-browser__status").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task WithoutCopy_TheKeysDoNothing()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();
		Click(cut, "a.txt");

		await Key(cut, "c", ctrl: true);
		await Key(cut, "x", ctrl: true);

		Assert.DoesNotContain("to c", cut.Find(".moka-file-browser__status").TextContent, StringComparison.Ordinal);
		Assert.DoesNotContain("to m", cut.Find(".moka-file-browser__status").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void Upload_GetsTheFilesAndTheFolder_AndReloads()
	{
		(int Count, string Folder)? uploaded = null;
		int loads = 0;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Load, (folder, token) =>
			{
				loads++;
				return LoadTree(folder, token);
			})
			.Add(x => x.Upload, (files, folder) =>
			{
				uploaded = (files.Count, folder);
				return Task.CompletedTask;
			}), ownLoad: true);

		cut.FindComponent<InputFile>().UploadFiles(
			InputFileContent.CreateFromText("hello", "a.txt"),
			InputFileContent.CreateFromText("world", "b.txt"));

		Assert.Equal((2, "/"), uploaded);
		Assert.Equal(2, loads);
	}

	[Fact]
	public void TheUploadButton_OpensThePicker()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Upload, (_, _) => Task.CompletedTask));

		cut.FindAll(".moka-file-browser__bar button").Single(b => b.TextContent.Contains("Upload", StringComparison.Ordinal)).Click();

		Module.VerifyInvoke("pickFiles");
	}

	[Fact]
	public void ARightClick_SelectsTheRowFirst_AndOpensItsMenu()
	{
		FakeMenuService menu = UseMenuService();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Rename, (_, _) => Task.CompletedTask)
			.Add(x => x.Delete, _ => Task.CompletedTask));
		Click(cut, "a.txt");

		Row(cut, "b.txt").ContextMenu(RightClick);

		Assert.Equal(["b.txt"], SelectedNames(cut));
		Assert.True(menu.Visible);
		Assert.NotNull(menu.Named("Rename"));
		Assert.NotNull(menu.Named("Delete"));
	}

	[Fact]
	public void ARightClickOnASelectedRow_KeepsTheSelection()
	{
		FakeMenuService menu = UseMenuService();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Rename, (_, _) => Task.CompletedTask));
		Click(cut, "a.txt", ctrl: true);
		Click(cut, "b.txt", ctrl: true);

		Row(cut, "b.txt").ContextMenu(RightClick);

		Assert.Equal(2, SelectedNames(cut).Length);
		Assert.True(menu.Named("Rename").Disabled);
	}

	[Fact]
	public async Task TheMenusRename_OpensTheEditor()
	{
		FakeMenuService menu = UseMenuService();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Rename, (_, _) => Task.CompletedTask));
		ContextMenuOn(Row(cut, "a.txt"), RightClick);

		await cut.InvokeAsync(() => menu.Named("Rename").OnClickSync!());

		Assert.Single(cut.FindAll("[role=option] input"));
	}

	[Fact]
	public void ItemMenu_GetsTheItemsAndTheStandardMenu()
	{
		FakeMenuService menu = UseMenuService();
		MokaFileMenuContext<FileEntry>? asked = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Delete, _ => Task.CompletedTask)
			.Add(x => x.ItemMenu, context =>
			{
				asked = context;
				return [new MokaContextMenuItem { Text = "Share" }, .. context.Standard];
			}));

		Row(cut, "a.txt").ContextMenu(RightClick);

		Assert.Equal(["a.txt"], asked!.Items.Select(e => e.Name));
		Assert.Equal("Share", menu.Items[0].Text);
		Assert.NotNull(menu.Named("Delete"));
	}

	[Fact]
	public void TheBackground_HasItsOwnMenu()
	{
		FakeMenuService menu = UseMenuService();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.CreateFolder, (_, _) => Task.CompletedTask));

		cut.Find(".moka-file-browser__main").ContextMenu(RightClick);

		Assert.NotNull(menu.Named("New folder"));
		Assert.NotNull(menu.Named("Refresh"));
	}

	[Fact]
	public void BackgroundMenu_CanAddItems()
	{
		FakeMenuService menu = UseMenuService();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.BackgroundMenu, context =>
			[new MokaContextMenuItem { Text = "Mine" }, .. context.Standard]));

		cut.Find(".moka-file-browser__main").ContextMenu(RightClick);

		Assert.Equal("Mine", menu.Items[0].Text);
	}

	[Fact]
	public void TheContextMenuKey_OnTheList_OpensTheMenuOfTheCursor()
	{
		FakeMenuService menu = UseMenuService();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Rename, (_, _) => Task.CompletedTask));
		Click(cut, "a.txt");

		cut.Find(".moka-file-browser__main").ContextMenu(new MouseEventArgs { Button = 0, Detail = 0 });

		Assert.NotNull(menu.Named("Rename"));
	}

	[Fact]
	public void WithoutAMenuHost_TheBrowserDrawsItsOwnMenu()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p.Add(x => x.Rename, (_, _) => Task.CompletedTask));

		Row(cut, "a.txt").ContextMenu(RightClick);

		MokaContextMenu menu = cut.FindComponent<MokaContextMenu>().Instance;
		Assert.True(menu.Visible);
		Assert.Contains(menu.Items, item => item.Text == "Rename");
	}

	[Fact]
	public async Task AnActionThatFails_LeavesAnAlertThatCanBeDismissed()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.ConfirmDelete, _ => Task.FromResult(true))
			.Add(x => x.Delete, _ => throw new InvalidOperationException("Nope")));
		Click(cut, "a.txt");
		await Key(cut, "Delete");

		ClickOn(cut.Find(".moka-alert button"));

		Assert.Empty(cut.FindAll(".moka-alert"));
	}
}
