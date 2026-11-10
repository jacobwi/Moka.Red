using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Files;
using Moka.Red.Data.FileBrowser;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Components;

// The Upload control clicked the browser's hidden file input from script. With an app's own picker it asks the picker
// instead, and the picked files go to Upload as the browser's do.
public class MokaFileBrowserPickerTests : MokaFileBrowserKit
{
	private readonly List<(string[] Names, string Folder)> _uploads = [];

	private Task Uploaded(IReadOnlyList<IBrowserFile> files, string folder)
	{
		_uploads.Add(([.. files.Select(file => file.Name)], folder));
		return Task.CompletedTask;
	}

	private FakeFilePicker Register(params IBrowserFile[] files)
	{
		FakeFilePicker picker = new() { Files = files };
		Services.AddSingleton<IMokaFilePicker>(picker);
		return picker;
	}

	private IRenderedComponent<MokaFileBrowser<FileEntry>> RenderUploading(
		Action<ComponentParameterCollectionBuilder<MokaFileBrowser<FileEntry>>>? extra = null, string folder = "/") =>
		RenderBrowser(p =>
		{
			p.Add(x => x.Upload, Uploaded);
			extra?.Invoke(p);
		}, folder);

	private static IElement UploadButton(IRenderedComponent<MokaFileBrowser<FileEntry>> cut) =>
		cut.FindAll(".moka-file-browser__bar button").Single(b => b.TextContent.Contains("Upload", StringComparison.Ordinal));

	private static bool PreventsClicks(IElement element) =>
		element.Attributes.Any(a => string.Equals(a.Name, "blazor:onclick:preventDefault", StringComparison.OrdinalIgnoreCase));

	[Fact]
	public void WithoutAPicker_TheButtonStillClicksTheBrowsersInput()
	{
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderUploading();

		UploadButton(cut).Click();

		Module.VerifyInvoke("pickFiles");
		Assert.False(PreventsClicks(cut.Find("[data-moka-file-drop]")));
	}

	[Fact]
	public void ARegisteredPicker_IsAsked_AndItsFilesGoToUpload()
	{
		int loads = 0;
		FakeFilePicker picker = Register(new PickedFile("a.txt"), new PickedFile("b.txt"));
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderBrowser(p => p
			.Add(x => x.Upload, Uploaded)
			.Add(x => x.Load, (folder, token) =>
			{
				loads++;
				return LoadTree(folder, token);
			}), "/docs", ownLoad: true);

		UploadButton(cut).Click();

		Assert.Empty(Module.Invocations["pickFiles"]);
		Assert.Equal([new MokaFilePickRequest(true)], picker.Requests);
		(string[] names, string folder) = Assert.Single(_uploads);
		Assert.Equal(["a.txt", "b.txt"], names);
		Assert.Equal("/docs", folder);
		Assert.Equal(2, loads);
	}

	[Fact]
	public async Task TheMenusUpload_AsksThePickerToo()
	{
		FakeMenuService menu = UseMenuService();
		FakeFilePicker picker = Register(new PickedFile("a.txt"));
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderUploading();

		await cut.Find(".moka-file-browser__main").ContextMenuAsync(new MouseEventArgs { Button = 2, Detail = 1 });
		await cut.InvokeAsync(menu.Named("Upload").OnClick!);

		Assert.Single(picker.Requests);
		Assert.Single(_uploads);
		Assert.Empty(Module.Invocations["pickFiles"]);
	}

	[Fact]
	public void ACancelledPick_UploadsNothing()
	{
		FakeFilePicker picker = Register();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderUploading();

		UploadButton(cut).Click();
		picker.Failure = new OperationCanceledException();
		UploadButton(cut).Click();

		Assert.Equal(2, picker.Requests.Count);
		Assert.Empty(_uploads);
		Assert.Empty(cut.FindAll(".moka-alert"));
	}

	// The browser shows what an action throws in its alert, so a picker that fails shows there too, with a retry.
	[Fact]
	public async Task AFailingPicker_ShowsInTheAlert_AndRetryAsksAgain()
	{
		MokaFileFailure? told = null;
		FakeFilePicker picker = Register(new PickedFile("a.txt"));
		picker.Failure = new InvalidOperationException("No window to show the dialog over.");
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderUploading(p => p.Add(x => x.OnFailureChanged, failure => told = failure));

		await UploadButton(cut).ClickAsync(new MouseEventArgs());

		Assert.Contains("No window to show the dialog over.", cut.Find(".moka-alert").TextContent, StringComparison.Ordinal);
		Assert.Equal("That didn't work", told!.Title);
		Assert.Empty(_uploads);

		picker.Failure = null;
		await cut.InvokeAsync(told.Retry!);

		Assert.Equal(2, picker.Requests.Count);
		Assert.Single(_uploads);
		Assert.Empty(cut.FindAll(".moka-alert"));
	}

	[Fact]
	public async Task WhileThePickerIsOpen_AnotherClickDoesNothing()
	{
		FakeFilePicker picker = Register();
		picker.StaysOpen = true;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderUploading();

		Task first = UploadButton(cut).ClickAsync(new MouseEventArgs());
		await UploadButton(cut).ClickAsync(new MouseEventArgs());
		Assert.Single(picker.Requests);

		picker.Close(new PickedFile("a.txt"));
		await first;
		Assert.Single(_uploads);

		picker.StaysOpen = false;
		await UploadButton(cut).ClickAsync(new MouseEventArgs());
		Assert.Equal(2, picker.Requests.Count);
	}

	[Fact]
	public void TheParameter_WinsOverTheRegisteredPicker()
	{
		FakeFilePicker registered = Register(new PickedFile("registered.txt"));
		FakeFilePicker own = new() { Files = [new PickedFile("own.txt")] };
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderUploading(p => p.Add(x => x.FilePicker, own));

		UploadButton(cut).Click();

		Assert.Empty(registered.Requests);
		Assert.Equal(["own.txt"], Assert.Single(_uploads).Names);
	}

	[Fact]
	public void Drops_StillArriveThroughTheInput()
	{
		FakeFilePicker picker = Register();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderUploading();

		cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "dropped.txt"));

		Assert.Equal(["dropped.txt"], Assert.Single(_uploads).Names);
		Assert.Empty(picker.Requests);
	}

	// The drop layer stays up for a moment after a drag that ends off the page, and a click on its input then would open
	// the browser's dialog.
	[Fact]
	public void WithAPicker_TheDropLayerCancelsClicks()
	{
		Register();
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderUploading();

		Assert.True(PreventsClicks(cut.Find("[data-moka-file-drop]")));
	}

	[Fact]
	public async Task Disposing_CancelsAnOpenPick_AndItsFilesAreIgnored()
	{
		FakeFilePicker picker = Register();
		picker.StaysOpen = true;
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut = RenderUploading();
		Task click = UploadButton(cut).ClickAsync(new MouseEventArgs());

		await DisposeComponentsAsync();
		Assert.True(picker.LastToken.IsCancellationRequested);

		picker.Close(new PickedFile("late.txt"));
		await click;
		Assert.Empty(_uploads);
	}
}
