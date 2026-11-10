using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Files;
using Moka.Red.Forms.FileUpload;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// WebView2's file dialog can end a Blazor Hybrid app on Windows, so an app can pick files itself. The zone then cancels
// the click that would open the browser's dialog and asks the app's picker, and the files it hands back go through the
// same checks as the browser's.
public class MokaFileUploadPickerTests : BunitContext
{
	public MokaFileUploadPickerTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static bool Has(IElement element, string attribute) =>
		element.Attributes.Any(a => string.Equals(a.Name, attribute, StringComparison.OrdinalIgnoreCase));

	private static string[] ListedFiles(IRenderedComponent<MokaFileUpload> cut) =>
		cut.FindAll(".moka-fileupload-filename").Select(name => name.TextContent).ToArray();

	private static string[] Errors(IRenderedComponent<MokaFileUpload> cut) =>
		cut.FindAll(".moka-fileupload-error").Select(error => error.TextContent).ToArray();

	private FakeFilePicker Register(params IBrowserFile[] files)
	{
		FakeFilePicker picker = new() { Files = files };
		Services.AddSingleton<IMokaFilePicker>(picker);
		return picker;
	}

	[Fact]
	public void WithoutAPicker_TheZoneTakesNoClick_AndNoScriptRuns()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.Multiple, true));
		IElement zone = cut.Find(".moka-fileupload-dropzone");

		Assert.False(Has(zone, "blazor:onclick"));
		Assert.False(Has(zone, "blazor:onclick:preventDefault"));
		Assert.All(JSInterop.Invocations,
			invocation => Assert.StartsWith("Blazor._internal.InputFile.", invocation.Identifier, StringComparison.Ordinal));
	}

	[Fact]
	public void ARegisteredPicker_OpensOnAClick_AndTheBrowsersDialogIsCancelled()
	{
		FakeFilePicker picker = Register(new PickedFile("report.pdf"));
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Accept, ".pdf")
			.Add(x => x.Multiple, true));
		IElement zone = cut.Find(".moka-fileupload-dropzone");
		Assert.True(Has(zone, "blazor:onclick:preventDefault"));

		cut.Find("input[type=file]").Click();

		Assert.Equal([new MokaFilePickRequest(true, ".pdf")], picker.Requests);
		Assert.Equal(["report.pdf"], ListedFiles(cut));
	}

	// The labels forward their click to the input in a browser, and the zone cancels that too.
	[Theory]
	[InlineData(false, ".moka-fileupload-browse")]
	[InlineData(true, ".moka-fileupload-browse-compact")]
	public void TheLabels_OpenThePicker(bool compact, string label)
	{
		FakeFilePicker picker = Register(new PickedFile("a.txt"));
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.Compact, compact));

		cut.Find(label).Click();

		Assert.Single(picker.Requests);
		Assert.Equal(["a.txt"], ListedFiles(cut));
	}

	[Fact]
	public void PickedFiles_GoThroughTheSameChecks()
	{
		List<string> reported = [];
		Register(
			new PickedFile("report.pdf", contentType: "application/pdf"),
			new PickedFile("notes.txt", contentType: "text/plain"),
			new PickedFile("big.pdf", "far too long", "application/pdf"),
			new PickedFile("two.pdf", contentType: "application/pdf"),
			new PickedFile("three.pdf", contentType: "application/pdf"));
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Accept, ".pdf")
			.Add(x => x.Multiple, true)
			.Add(x => x.MaxFiles, 2)
			.Add(x => x.MaxFileSize, 5)
			.Add(x => x.OnFilesSelected, (IReadOnlyList<IBrowserFile> files) => reported.AddRange(files.Select(f => f.Name))));

		cut.Find(".moka-fileupload-dropzone").Click();

		Assert.Equal(["report.pdf", "two.pdf"], ListedFiles(cut));
		Assert.Equal(
		[
			"notes.txt is not an accepted file type",
			"big.pdf exceeds maximum size (5 B)",
			"1 file was not added: the limit is 2"
		], Errors(cut));
		Assert.Equal(["report.pdf", "two.pdf"], reported);
	}

	[Fact]
	public void Single_KeepsTheFirstPickedFile_AndAskedForOne()
	{
		FakeFilePicker picker = Register(new PickedFile("first.txt"), new PickedFile("second.txt"));
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>();

		cut.Find(".moka-fileupload-dropzone").Click();

		Assert.False(Assert.Single(picker.Requests).Multiple);
		Assert.Equal(["first.txt"], ListedFiles(cut));
	}

	[Fact]
	public void ACancelledPick_ChangesNothing()
	{
		int reported = 0;
		FakeFilePicker picker = Register(new PickedFile("kept.txt"));
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.OnFilesSelected, (IReadOnlyList<IBrowserFile> _) => reported++));
		cut.Find(".moka-fileupload-dropzone").Click();

		picker.Files = [];
		cut.Find(".moka-fileupload-dropzone").Click();
		picker.Failure = new OperationCanceledException();
		cut.Find(".moka-fileupload-dropzone").Click();

		Assert.Equal(3, picker.Requests.Count);
		Assert.Equal(["kept.txt"], ListedFiles(cut));
		Assert.Empty(Errors(cut));
		Assert.Equal(1, reported);
	}

	// A picker that fails is the app's failure, so it reaches the error boundary like a failing OnFilesSelected.
	[Fact]
	public async Task AFailingPicker_IsNotSwallowed()
	{
		FakeFilePicker picker = Register();
		picker.Failure = new InvalidOperationException("No window to show the dialog over.");
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>();

		InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(
			() => cut.Find(".moka-fileupload-dropzone").ClickAsync(new MouseEventArgs()));

		Assert.Equal("No window to show the dialog over.", thrown.Message);
	}

	[Fact]
	public async Task WhileThePickerIsOpen_AnotherClickDoesNothing()
	{
		FakeFilePicker picker = Register();
		picker.StaysOpen = true;
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.Multiple, true));

		Task first = cut.Find(".moka-fileupload-dropzone").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-fileupload-dropzone").ClickAsync(new MouseEventArgs());
		Assert.Single(picker.Requests);

		picker.Close(new PickedFile("a.txt"));
		await first;
		Assert.Equal(["a.txt"], ListedFiles(cut));

		picker.StaysOpen = false;
		await cut.Find(".moka-fileupload-dropzone").ClickAsync(new MouseEventArgs());
		Assert.Equal(2, picker.Requests.Count);
	}

	[Fact]
	public void TheParameter_WinsOverTheRegisteredPicker()
	{
		FakeFilePicker registered = Register(new PickedFile("registered.txt"));
		FakeFilePicker own = new() { Files = [new PickedFile("own.txt")] };
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.FilePicker, own));

		cut.Find(".moka-fileupload-dropzone").Click();

		Assert.Empty(registered.Requests);
		Assert.Single(own.Requests);
		Assert.Equal(["own.txt"], ListedFiles(cut));
	}

	[Fact]
	public void TheParameter_WorksWithoutARegisteredPicker()
	{
		FakeFilePicker own = new() { Files = [new PickedFile("own.txt")] };
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.FilePicker, own));

		Assert.True(Has(cut.Find(".moka-fileupload-dropzone"), "blazor:onclick:preventDefault"));
		cut.Find(".moka-fileupload-dropzone").Click();

		Assert.Equal(["own.txt"], ListedFiles(cut));
	}

	[Fact]
	public void Drops_StillArriveThroughTheInput()
	{
		FakeFilePicker picker = Register();
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.Multiple, true));
		IElement zone = cut.Find(".moka-fileupload-dropzone");

		Assert.False(Has(zone, "blazor:ondrop:preventDefault"));
		cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "dropped.txt"));

		Assert.Equal(["dropped.txt"], ListedFiles(cut));
		Assert.Empty(picker.Requests);
	}

	[Fact]
	public void Disabled_DoesNotOpenThePicker()
	{
		FakeFilePicker picker = Register(new PickedFile("a.txt"));
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.Disabled, true));

		cut.Find(".moka-fileupload-dropzone").Click();

		Assert.Empty(picker.Requests);
		Assert.Empty(ListedFiles(cut));
	}

	[Fact]
	public async Task FilesPickedAfterTheUploadWasDisabled_AreNotAdded()
	{
		int reported = 0;
		FakeFilePicker picker = Register();
		picker.StaysOpen = true;
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.OnFilesSelected, (IReadOnlyList<IBrowserFile> _) => reported++));
		Task click = cut.Find(".moka-fileupload-dropzone").ClickAsync(new MouseEventArgs());

		cut.Render(p => p.Add(x => x.Disabled, true));
		picker.Close(new PickedFile("late.txt"));
		await click;

		Assert.Empty(ListedFiles(cut));
		Assert.Equal(0, reported);
	}

	[Fact]
	public async Task Disposing_CancelsAnOpenPick_AndItsFilesAreIgnored()
	{
		int reported = 0;
		FakeFilePicker picker = Register();
		picker.StaysOpen = true;
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.OnFilesSelected, (IReadOnlyList<IBrowserFile> _) => reported++));
		Task click = cut.Find(".moka-fileupload-dropzone").ClickAsync(new MouseEventArgs());
		Assert.False(picker.LastToken.IsCancellationRequested);

		await DisposeComponentsAsync();
		Assert.True(picker.LastToken.IsCancellationRequested);

		picker.Close(new PickedFile("late.txt"));
		await click;
		Assert.Equal(0, reported);
	}
}
