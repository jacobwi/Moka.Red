using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.FileUpload;

namespace Moka.Red.Forms.Tests.Components;

public class MokaFileUploadTests : BunitContext
{
	public MokaFileUploadTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// Disabled only dimmed the drop zone and blocked the mouse. The hidden file input kept its tab stop,
	// so the keyboard still opened the file dialog, and the remove buttons still removed files.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Disabled_DisablesTheFileInput(bool disabled)
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.Disabled, disabled));

		Assert.Equal(disabled, cut.Find("input[type=file]").HasAttribute("disabled"));
	}

	[Fact]
	public async Task Disabled_TheRemoveButtonsAreDisabled_AndRemoveNothing()
	{
		var removed = new List<string>();
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.OnFileRemoved, (IBrowserFile file) => removed.Add(file.Name)));
		cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "notes.txt"));

		cut.Render(p => p.Add(x => x.Disabled, true));

		IElement remove = cut.Find(".moka-fileupload-remove");
		Assert.True(remove.HasAttribute("disabled"));

		// A click that was already on its way when the upload was disabled.
		await remove.ClickAsync(new MouseEventArgs());

		Assert.Empty(removed);
		Assert.Single(cut.FindAll(".moka-fileupload-file"));
	}

	[Fact]
	public async Task Enabled_TheRemoveButtonsWork()
	{
		var removed = new List<string>();
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.OnFileRemoved, (IBrowserFile file) => removed.Add(file.Name)));
		cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "notes.txt"));

		IElement remove = cut.Find(".moka-fileupload-remove");
		Assert.False(remove.HasAttribute("disabled"));
		await remove.ClickAsync(new MouseEventArgs());

		Assert.Equal(["notes.txt"], removed);
		Assert.Empty(cut.FindAll(".moka-fileupload-file"));
	}

	// Files that were already on their way when the upload was disabled were still added.
	[Fact]
	public void Disabled_FilesThatArriveAnywayAreNotAdded()
	{
		var reported = 0;
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Disabled, true)
			.Add(x => x.OnFilesSelected, (IReadOnlyList<IBrowserFile> _) => reported++));

		cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "notes.txt"));

		Assert.Empty(cut.FindAll(".moka-fileupload-file"));
		Assert.Equal(0, reported);
	}

	// The remove buttons held only an icon, so a screen reader announced each one as "button".
	[Fact]
	public void RemoveButtons_AreNamedAfterTheirFile()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.Multiple, true));

		cut.FindComponent<InputFile>().UploadFiles(
			InputFileContent.CreateFromText("a", "report.pdf"),
			InputFileContent.CreateFromText("b", "notes.txt"));

		Assert.Equal(["Remove report.pdf", "Remove notes.txt"],
			cut.FindAll(".moka-fileupload-remove").Select(button => button.GetAttribute("aria-label")));
	}
}
