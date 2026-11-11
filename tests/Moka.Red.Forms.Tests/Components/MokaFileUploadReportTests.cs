using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.FileUpload;

namespace Moka.Red.Forms.Tests.Components;

// MokaGate's 0.4.1 gaps 25 and 26. After a refused file OnFilesSelected handed over the list from before, so an app
// read a file it had already read unless it compared files by reference, and nothing let the app empty the upload once
// it had used its file.
public sealed class MokaFileUploadReportTests : BunitContext
{
	public MokaFileUploadReportTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static void Upload(IRenderedComponent<MokaFileUpload> cut, params InputFileContent[] files) =>
		cut.FindComponent<InputFile>().UploadFiles(files);

	private static InputFileContent File(string name, string content = "x") =>
		InputFileContent.CreateFromText(content, name, contentType: "application/octet-stream");

	private static string[] Names(IEnumerable<IBrowserFile> files) => [.. files.Select(file => file.Name)];

	private static string[] Listed(IRenderedComponent<MokaFileUpload> cut) =>
		[.. cut.FindAll(".moka-fileupload-filename").Select(name => name.TextContent)];

	[Fact]
	public void ARefusedFile_RaisesNothing_AndLeavesTheSlotsFile()
	{
		List<string[]> selected = [];
		List<string[]> added = [];
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, MokaFileUploadLook.Slot)
			.Add(x => x.Accept, ".json")
			.Add(x => x.OnFilesSelected, files => selected.Add(Names(files)))
			.Add(x => x.OnFilesAdded, files => added.Add(Names(files))));
		Upload(cut, File("backup.json"));

		Upload(cut, File("photo.png"));

		Assert.Equal([["backup.json"]], selected);
		Assert.Equal([["backup.json"]], added);
		Assert.Equal("backup.json", cut.Find(".moka-fileupload-slot .moka-fileupload-name").TextContent);
		Assert.Equal("photo.png is not an accepted file type", cut.Find(".moka-fileupload-error").TextContent);
	}

	[Fact]
	public void ATooLargeFile_RaisesNothing()
	{
		int raised = 0;
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.MaxFileSize, 3)
			.Add(x => x.OnFilesSelected, _ => raised++)
			.Add(x => x.OnFilesAdded, _ => raised++));

		Upload(cut, File("big.txt", "too long"));

		Assert.Equal(0, raised);
		Assert.Single(cut.FindAll(".moka-fileupload-error"));
	}

	[Fact]
	public void FilesPastTheLimit_RaiseNothing_WhenNoneWereAdded()
	{
		int raised = 0;
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.MaxFiles, 1));
		Upload(cut, File("one.txt"));
		cut.Render(p => p
			.Add(x => x.OnFilesSelected, _ => raised++)
			.Add(x => x.OnFilesAdded, _ => raised++));

		Upload(cut, File("two.txt"));

		Assert.Equal(0, raised);
		Assert.Equal(["one.txt"], Listed(cut));
	}

	[Fact]
	public void OnFilesAdded_CarriesOnlyTheNewFiles_AndOnFilesSelectedTheWholeList()
	{
		List<string[]> selected = [];
		List<string[]> added = [];
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.Accept, ".txt")
			.Add(x => x.OnFilesSelected, files => selected.Add(Names(files)))
			.Add(x => x.OnFilesAdded, files => added.Add(Names(files))));

		Upload(cut, File("a.txt"));
		Upload(cut, File("b.txt"), File("c.png"), File("d.txt"));

		Assert.Equal([["a.txt"], ["b.txt", "d.txt"]], added);
		Assert.Equal([["a.txt"], ["a.txt", "b.txt", "d.txt"]], selected);
	}

	// The binding first, as InputBase raises ValueChanged first.
	[Fact]
	public void TheBindingComesFirst_ThenTheAddedFiles_ThenTheList()
	{
		List<string> order = [];
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.OnFilesSelected, _ => order.Add("selected"))
			.Add(x => x.FilesChanged, _ => order.Add("bound"))
			.Add(x => x.OnFilesAdded, _ => order.Add("added")));

		Upload(cut, File("a.txt"));

		Assert.Equal(["bound", "added", "selected"], order);
	}

	// It was the upload's own list behind a read-only wrapper, so a list an app kept changed under it.
	[Fact]
	public async Task TheListHandedOver_StaysAsItWas()
	{
		IReadOnlyList<IBrowserFile>? first = null;
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.OnFilesSelected, files => first ??= files));
		Upload(cut, File("a.txt"));

		Upload(cut, File("b.txt"));
		await cut.Find(".moka-fileupload-remove").ClickAsync(new MouseEventArgs());

		Assert.Equal(["a.txt"], Names(first!));
		Assert.Equal(["b.txt"], Listed(cut));
	}

	[Theory]
	[InlineData(MokaFileUploadLook.Standard)]
	[InlineData(MokaFileUploadLook.Slot)]
	[InlineData(MokaFileUploadLook.Field)]
	public void ANewFileInAOneFileLook_RemovesTheOldOne_First(MokaFileUploadLook look)
	{
		List<string> events = [];
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, look)
			.Add(x => x.OnFileRemoved, file => events.Add($"removed {file.Name}"))
			.Add(x => x.OnFilesAdded, files => events.Add($"added {string.Join(",", Names(files))}"))
			.Add(x => x.OnFilesSelected, files => events.Add($"list {string.Join(",", Names(files))}")));
		Upload(cut, File("old.txt"));
		events.Clear();

		Upload(cut, File("new.txt"));

		Assert.Equal(["removed old.txt", "added new.txt", "list new.txt"], events);
	}

	[Fact]
	public void ARefusedFile_InAOneFileLook_RemovesNothing()
	{
		List<string> removed = [];
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Accept, ".txt")
			.Add(x => x.OnFileRemoved, file => removed.Add(file.Name)));
		Upload(cut, File("kept.txt"));

		Upload(cut, File("photo.png"));

		Assert.Empty(removed);
		Assert.Equal(["kept.txt"], Listed(cut));
	}

	[Fact]
	public async Task ARemove_ReportsTheListLeft()
	{
		IReadOnlyList<IBrowserFile>? left = null;
		IReadOnlyList<IBrowserFile>? bound = null;
		int added = 0;
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.OnFilesSelected, files => left = files)
			.Add(x => x.FilesChanged, files => bound = files)
			.Add(x => x.OnFilesAdded, _ => added++));
		Upload(cut, File("a.txt"));

		await cut.Find(".moka-fileupload-remove").ClickAsync(new MouseEventArgs());

		Assert.Empty(left!);
		Assert.Empty(bound!);
		Assert.Equal(1, added);
	}

	[Fact]
	public void FilesChanged_CarriesTheSameListAsOnFilesSelected()
	{
		IReadOnlyList<IBrowserFile>? selected = null;
		IReadOnlyList<IBrowserFile>? bound = null;
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.OnFilesSelected, files => selected = files)
			.Add(x => x.FilesChanged, files => bound = files));

		Upload(cut, File("a.txt"), File("b.txt"));

		Assert.Same(selected, bound);
		Assert.Equal(["a.txt", "b.txt"], Names(bound!));
	}

	[Fact]
	public void BoundFiles_AnEmptyListFromTheApp_EmptiesTheUpload()
	{
		IRenderedComponent<BoundUpload> host = Render<BoundUpload>(p => p.Add(x => x.Look, MokaFileUploadLook.Slot));
		IRenderedComponent<MokaFileUpload> cut = host.FindComponent<MokaFileUpload>();
		Upload(cut, File("backup.json"));
		Assert.Equal(["backup.json"], Names(host.Instance.Files));

		host.InvokeAsync(host.Instance.Empty);

		Assert.Contains("moka-fileupload-slot--empty", cut.Find(".moka-fileupload-slot").ClassName, StringComparison.Ordinal);
		Assert.Empty(host.Instance.Removed);
		Assert.Equal(1, host.Instance.Selected);
	}

	[Fact]
	public void BoundFiles_ThePickAfterEmptying_IsAddedAgain()
	{
		IRenderedComponent<BoundUpload> host = Render<BoundUpload>();
		IRenderedComponent<MokaFileUpload> cut = host.FindComponent<MokaFileUpload>();
		Upload(cut, File("a.txt"));
		host.InvokeAsync(host.Instance.Empty);

		Upload(cut, File("a.txt"));

		Assert.Equal(["a.txt"], Listed(cut));
		Assert.Equal(["a.txt"], Names(host.Instance.Files));
	}

	[Fact]
	public void BoundFiles_ClearTheLastPicksErrors()
	{
		IRenderedComponent<BoundUpload> host = Render<BoundUpload>(p => p.Add(x => x.Accept, ".txt"));
		IRenderedComponent<MokaFileUpload> cut = host.FindComponent<MokaFileUpload>();
		Upload(cut, File("a.txt"));
		Upload(cut, File("b.png"));
		Assert.Single(cut.FindAll(".moka-fileupload-error"));

		host.InvokeAsync(host.Instance.Empty);

		Assert.Empty(cut.FindAll(".moka-fileupload-error"));
		Assert.Empty(Listed(cut));
	}

	// An app that reads the file and then empties the upload, all in OnFilesAdded, mustn't be handed the file back
	// through FilesChanged.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void BoundFiles_EmptiedInOnFilesAdded_StayEmpty(bool afterAwait)
	{
		IRenderedComponent<BoundUpload> host = Render<BoundUpload>(p => p
			.Add(x => x.EmptyWhenAdded, true)
			.Add(x => x.EmptyAfterAwait, afterAwait));
		IRenderedComponent<MokaFileUpload> cut = host.FindComponent<MokaFileUpload>();

		Upload(cut, File("import.csv"));

		cut.WaitForAssertion(() => Assert.Empty(Listed(cut)));
		Assert.Empty(host.Instance.Files);
		Assert.Equal(["import.csv"], host.Instance.Read);
	}

	// A parent that renders again before it has taken the new list, as one does in the middle of a callback, passes
	// its old list, which mustn't put the files from before back.
	[Fact]
	public void TheSameListAgain_ChangesNothing()
	{
		IReadOnlyList<IBrowserFile> before = [];
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.Files, before));
		Upload(cut, File("a.txt"));

		cut.Render(p => p.Add(x => x.Files, before));

		Assert.Equal(["a.txt"], Listed(cut));
	}

	[Fact]
	public void FilesFromTheApp_ShowWithoutRaisingAnything()
	{
		IReadOnlyList<IBrowserFile>? kept = null;
		IRenderedComponent<MokaFileUpload> picker = Render<MokaFileUpload>(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.OnFilesSelected, files => kept = files));
		Upload(picker, File("a.txt"), File("b.txt"));
		int raised = 0;

		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.Files, kept)
			.Add(x => x.OnFilesSelected, _ => raised++)
			.Add(x => x.FilesChanged, _ => raised++)
			.Add(x => x.OnFileRemoved, _ => raised++));

		Assert.Equal(["a.txt", "b.txt"], Listed(cut));
		Assert.Equal(0, raised);
	}

	[Fact]
	public void AOneFileLook_KeepsTheFirstOfTheAppsFiles()
	{
		IReadOnlyList<IBrowserFile>? kept = null;
		IRenderedComponent<MokaFileUpload> picker = Render<MokaFileUpload>(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.OnFilesSelected, files => kept = files));
		Upload(picker, File("a.txt"), File("b.txt"));

		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.Files, kept));

		Assert.Equal(["a.txt"], Listed(cut));
	}

	private sealed class BoundUpload : ComponentBase
	{
		[Parameter]
		public MokaFileUploadLook Look { get; set; }

		[Parameter]
		public string? Accept { get; set; }

		[Parameter]
		public bool EmptyWhenAdded { get; set; }

		[Parameter]
		public bool EmptyAfterAwait { get; set; }

		public IReadOnlyList<IBrowserFile> Files { get; private set; } = [];

		public List<string> Removed { get; } = [];

		public List<string> Read { get; } = [];

		public int Selected { get; private set; }

		public void Empty()
		{
			Files = [];
			StateHasChanged();
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaFileUpload>(0);
			builder.AddComponentParameter(1, nameof(MokaFileUpload.Look), Look);
			builder.AddComponentParameter(2, nameof(MokaFileUpload.Accept), Accept);
			builder.AddComponentParameter(3, nameof(MokaFileUpload.Files), Files);
			builder.AddComponentParameter(4, nameof(MokaFileUpload.FilesChanged),
				EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, files => Files = files));
			builder.AddComponentParameter(5, nameof(MokaFileUpload.OnFilesAdded),
				EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, AddedAsync));
			builder.AddComponentParameter(6, nameof(MokaFileUpload.OnFileRemoved),
				EventCallback.Factory.Create<IBrowserFile>(this, file => Removed.Add(file.Name)));
			builder.AddComponentParameter(7, nameof(MokaFileUpload.OnFilesSelected),
				EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, () => Selected++));
			builder.CloseComponent();
		}

		private async Task AddedAsync(IReadOnlyList<IBrowserFile> files)
		{
			Read.AddRange(files.Select(file => file.Name));
			if (!EmptyWhenAdded)
			{
				return;
			}

			if (EmptyAfterAwait)
			{
				await Task.Yield();
			}

			Files = [];
		}
	}
}
