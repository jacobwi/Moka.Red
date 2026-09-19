using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.FileUpload;

namespace Moka.Red.Forms.Tests.Components;

// The drop zone cancelled every drop and nothing read the files, so a dropped file did nothing. The
// file input now covers the zone and takes the drop, so a drop arrives the way a pick does, and the
// rules the dialog applies (Accept, one file without Multiple) are checked in code, since a drop
// skips the dialog.
public class MokaFileUploadDropTests : BunitContext
{
	public MokaFileUploadDropTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static bool Prevents(IElement element, string eventName) =>
		element.Attributes.Any(attribute => string.Equals(attribute.Name, $"blazor:{eventName}:preventDefault",
			StringComparison.OrdinalIgnoreCase));

	private static string[] ListedFiles(IRenderedComponent<MokaFileUpload> cut) =>
		cut.FindAll(".moka-fileupload-filename").Select(name => name.TextContent).ToArray();

	private static string[] Errors(IRenderedComponent<MokaFileUpload> cut) =>
		cut.FindAll(".moka-fileupload-error").Select(error => error.TextContent).ToArray();

	private static bool IsLit(IRenderedComponent<MokaFileUpload> cut) =>
		cut.Find(".moka-fileupload-dropzone").ClassList.Contains("moka-fileupload-dropzone--dragging");

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void DragDrop_TheZoneLetsTheDropReachTheFileInput(bool dragDrop)
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.DragDrop, dragDrop));
		IElement zone = cut.Find(".moka-fileupload-dropzone");

		// Without DragDrop the zone still swallows a drop, so the file does not open in the tab.
		Assert.Equal(!dragDrop, Prevents(zone, "ondrop"));
		Assert.Equal(!dragDrop, Prevents(zone, "ondragover"));
	}

	// A disabled upload's input takes no drop, so the file fell through to the page and opened in the tab.
	[Fact]
	public void Disabled_TheZoneSwallowsTheDrop()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.DragDrop, true)
			.Add(x => x.Disabled, true));
		IElement zone = cut.Find(".moka-fileupload-dropzone");

		Assert.True(Prevents(zone, "ondrop"));
		Assert.True(Prevents(zone, "ondragover"));
	}

	[Fact]
	public void Accept_AFileWithAnotherExtensionIsRejected()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Accept, ".pdf, .DOCX")
			.Add(x => x.Multiple, true));

		cut.FindComponent<InputFile>().UploadFiles(
			InputFileContent.CreateFromText("a", "report.PDF"),
			InputFileContent.CreateFromText("b", "notes.txt"),
			InputFileContent.CreateFromText("c", "letter.docx"));

		Assert.Equal(["report.PDF", "letter.docx"], ListedFiles(cut));
		Assert.Equal(["notes.txt is not an accepted file type"], Errors(cut));
	}

	[Fact]
	public void Accept_MimeTypesAndWildcards()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Accept, "image/*,application/pdf")
			.Add(x => x.Multiple, true));

		cut.FindComponent<InputFile>().UploadFiles(
			InputFileContent.CreateFromText("a", "photo.png", contentType: "image/png"),
			InputFileContent.CreateFromText("b", "report.pdf", contentType: "application/pdf"),
			InputFileContent.CreateFromText("c", "notes.txt", contentType: "text/plain"));

		Assert.Equal(["photo.png", "report.pdf"], ListedFiles(cut));
		Assert.Equal(["notes.txt is not an accepted file type"], Errors(cut));
	}

	// No Accept, "*/*" and an Accept with no valid token rule out no file. The browser ignores a token
	// that is neither an extension nor a MIME type, and the check does the same.
	[Theory]
	[InlineData(null)]
	[InlineData("pdf")]
	[InlineData("*/*")]
	public void Accept_ThatRulesOutNothingTakesEveryFile(string? accept)
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Accept, accept)
			.Add(x => x.Multiple, true));

		cut.FindComponent<InputFile>().UploadFiles(
			InputFileContent.CreateFromText("a", "notes.txt", contentType: "text/plain"),
			InputFileContent.CreateFromText("b", "photo.png", contentType: "image/png"));

		Assert.Equal(["notes.txt", "photo.png"], ListedFiles(cut));
	}

	[Fact]
	public void MaxFileSize_AppliesToEveryFile()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.MaxFileSize, 3)
			.Add(x => x.Multiple, true));

		cut.FindComponent<InputFile>().UploadFiles(
			InputFileContent.CreateFromText("hello", "big.txt"),
			InputFileContent.CreateFromText("hi", "small.txt"));

		Assert.Equal(["small.txt"], ListedFiles(cut));
		Assert.Equal(["big.txt exceeds maximum size (3 B)"], Errors(cut));
	}

	// Without Multiple the loop replaced the file for each one dropped, so the last of them won.
	[Fact]
	public void Single_ADropOfSeveralFilesKeepsTheFirst()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>();

		cut.FindComponent<InputFile>().UploadFiles(
			InputFileContent.CreateFromText("a", "first.txt"),
			InputFileContent.CreateFromText("b", "second.txt"));

		Assert.Equal(["first.txt"], ListedFiles(cut));
	}

	[Fact]
	public void Single_ANewFileReplacesTheListedOne()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>();
		IRenderedComponent<InputFile> input = cut.FindComponent<InputFile>();

		input.UploadFiles(InputFileContent.CreateFromText("a", "first.txt"));
		input.UploadFiles(InputFileContent.CreateFromText("b", "second.txt"));

		Assert.Equal(["second.txt"], ListedFiles(cut));
	}

	// More files than MaxFiles at once made GetMultipleFiles throw, so the whole pick was lost without
	// a message, and separate picks grew the list past MaxFiles.
	[Fact]
	public void MaxFiles_CapsTheWholeList()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.MaxFiles, 2));
		IRenderedComponent<InputFile> input = cut.FindComponent<InputFile>();

		input.UploadFiles(
			InputFileContent.CreateFromText("a", "one.txt"),
			InputFileContent.CreateFromText("b", "two.txt"),
			InputFileContent.CreateFromText("c", "three.txt"));

		Assert.Equal(["one.txt", "two.txt"], ListedFiles(cut));
		Assert.Equal(["1 file was not added: the limit is 2"], Errors(cut));

		input.UploadFiles(
			InputFileContent.CreateFromText("d", "four.txt"),
			InputFileContent.CreateFromText("e", "five.txt"));

		Assert.Equal(["one.txt", "two.txt"], ListedFiles(cut));
		Assert.Equal(["2 files were not added: the limit is 2"], Errors(cut));
	}

	// Crossing from the zone's border onto the input fires dragenter on the input before dragleave on
	// the zone, and a drop fires no dragleave at all. The zone went dark while the file was still over
	// it, and stayed lit after a drop.
	[Fact]
	public void Dragging_TheHighlightLastsUntilTheFileLeavesOrDrops()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>();

		cut.Find(".moka-fileupload-dropzone").DragEnter(new DragEventArgs());
		cut.Find("input[type=file]").DragEnter(new DragEventArgs());
		cut.Find(".moka-fileupload-dropzone").DragLeave(new DragEventArgs());
		Assert.True(IsLit(cut));

		cut.Find("input[type=file]").Drop(new DragEventArgs());
		Assert.False(IsLit(cut));
	}

	[Fact]
	public void Dragging_TheHighlightEndsWhenTheFileLeaves()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>();

		cut.Find("input[type=file]").DragEnter(new DragEventArgs());
		Assert.True(IsLit(cut));

		cut.Find("input[type=file]").DragLeave(new DragEventArgs());
		Assert.False(IsLit(cut));
	}

	// Without DragDrop the zone takes no drops, but it still lit up for one.
	[Fact]
	public void DragDropOff_TheZoneDoesNotLightUp()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.DragDrop, false));

		cut.Find(".moka-fileupload-dropzone").DragEnter(new DragEventArgs());

		Assert.False(IsLit(cut));
	}
}
