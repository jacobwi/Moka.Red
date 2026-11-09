using AngleSharp.Dom;
using Bunit;
using Moka.Red.Data.FileBrowser;

namespace Moka.Red.Data.Tests.Components;

// Rows are keyed so a refresh that moves one keeps its state. The state that shows it best is text typed in a rename.
public class MokaFileBrowserRowKeyTests : MokaFileBrowserKit
{
	private const int Many = 300;

	private static MokaFileView Tiles => new() { Mode = MokaFileViewMode.Tiles };

	private static FileEntry[] Files(int count, string prefix = "f") =>
		[.. Enumerable.Range(0, count).Select(i => new FileEntry($"/{prefix}{i:0000}", $"{prefix}{i:0000}.txt", Size: i))];

	private async Task<(IRenderedComponent<MokaFileBrowser<FileEntry>> Cut, Func<string?> Saved)> StartRename(string name, MokaFileView? view = null)
	{
		string? saved = null;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p =>
		{
			p.Add(x => x.Rename, (_, newName) =>
			{
				saved = newName;
				return Task.CompletedTask;
			});
			if (view is not null)
			{
				p.Add(x => x.View, view);
			}
		});

		await cut.InvokeAsync(() => cut.Instance.RevealAsync($"/{name}"));
		await Key(cut, "F2");
		Type(cut.Find("[role=option] input"), "typed.txt");
		return (cut, () => saved);
	}

	private async Task InsertAbove(IRenderedComponent<MokaFileBrowser<FileEntry>> cut, int count)
	{
		Tree["/"] = [.. Files(count, "a"), .. Tree["/"]];
		await cut.InvokeAsync(cut.Instance.ReloadAsync);
	}

	private static void AssertSavesTyped(IRenderedComponent<MokaFileBrowser<FileEntry>> cut, Func<string?> saved, string key)
	{
		IElement input = cut.Find("[role=option] input");
		Assert.Equal(key, input.Closest("[role=option]")!.GetAttribute("data-moka-key"));

		Press(input, "Enter");

		cut.WaitForAssertion(() => Assert.Equal("typed.txt", saved()));
	}

	private static string FirstKeyOnTheLineOfTheField(IRenderedComponent<MokaFileBrowser<FileEntry>> cut) =>
		cut.Find("[role=option] input").Closest("[data-moka-line]")!.QuerySelector("[role=option]")!.GetAttribute("data-moka-key")!;

	[Fact]
	public async Task ListRows_RowsInsertedAbove_KeepWhatWasTypedInARename()
	{
		Tree["/"] = Files(6);
		(IRenderedComponent<MokaFileBrowser<FileEntry>> cut, Func<string?> saved) = await StartRename("f0003");

		await InsertAbove(cut, 2);

		AssertSavesTyped(cut, saved, "/f0003");
	}

	[Fact]
	public async Task Tiles_RowsInsertedAbove_KeepWhatWasTypedEvenOnAnotherLine()
	{
		Tree["/"] = Files(10);
		(IRenderedComponent<MokaFileBrowser<FileEntry>> cut, Func<string?> saved) = await StartRename("f0001", Tiles);
		string lineBefore = FirstKeyOnTheLineOfTheField(cut);

		await InsertAbove(cut, 5);

		Assert.NotEqual(lineBefore, FirstKeyOnTheLineOfTheField(cut));
		AssertSavesTyped(cut, saved, "/f0001");
	}

	[Fact]
	public async Task VirtualizedRows_RowsInsertedAbove_KeepWhatWasTypedInARename()
	{
		Tree["/"] = Files(Many);
		(IRenderedComponent<MokaFileBrowser<FileEntry>> cut, Func<string?> saved) = await StartRename("f0002");

		await InsertAbove(cut, 1);

		AssertSavesTyped(cut, saved, "/f0002");
	}

	[Fact]
	public async Task VirtualizedTiles_RowsInsertedAbove_KeepWhatWasTypedInARename()
	{
		Tree["/"] = Files(Many * 4);
		(IRenderedComponent<MokaFileBrowser<FileEntry>> cut, Func<string?> saved) = await StartRename("f0001", Tiles);

		await InsertAbove(cut, 5);

		AssertSavesTyped(cut, saved, "/f0001");
	}

	[Fact]
	public void TheListbox_StillOwnsItsOptionsDirectly()
	{
		Tree["/"] = Files(3);
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		Assert.All(cut.FindAll("[role=option]"), option => Assert.Equal("listbox", option.ParentElement!.GetAttribute("role")));
	}

	[Fact]
	public async Task ItemsSharingAKey_DontThrowOnLaterRenders()
	{
		Tree["/"] = [new("/same", "first"), new("/same", "second"), new("/other", "other")];
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser();

		await cut.InvokeAsync(cut.Instance.ReloadAsync);
		Click(cut, "other");

		Assert.Equal(["first", "other"], Names(cut));
	}
}
