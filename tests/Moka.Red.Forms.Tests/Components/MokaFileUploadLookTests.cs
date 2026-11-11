using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.FileUpload;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// The looks BC picked from the upload board (U1 to U3). Every one is a parameter whose default keeps the upload exactly
// as it was, so the first tests here hold the default markup still.
public class MokaFileUploadLookTests : BunitContext
{
	private const string ModulePath = "./_content/Moka.Red.Forms/FileUpload/MokaFileUpload.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaFileUploadLookTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
		_module.SetupVoid("revokePreviews", _ => true).SetVoidResult();
		_module.SetupVoid("bindDragCheck", _ => true).SetVoidResult();
		_module.SetupVoid("unbindDragCheck", _ => true).SetVoidResult();
		_module.SetupVoid("bindChips", _ => true).SetVoidResult();
	}

	private static void Upload(IRenderedComponent<MokaFileUpload> cut, params InputFileContent[] files) =>
		cut.FindComponent<InputFile>().UploadFiles(files);

	private static InputFileContent File(string name, string content = "x", string? type = null) =>
		InputFileContent.CreateFromText(content, name, contentType: type ?? "application/octet-stream");

	private static IElement Root(IRenderedComponent<MokaFileUpload> cut) => cut.Find(".moka-fileupload");

	private int ModuleCalls(string identifier) => _module.Invocations[identifier].Count;

	[Fact]
	public void TheDefaults_KeepTheStandardZoneAndList_WithNoNewAttributesOrScript()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.Multiple, true));
		Upload(cut, File("report.pdf"), File("photo.png"));

		Assert.DoesNotContain(Root(cut).ClassList, c => c.StartsWith("moka-fileupload--", StringComparison.Ordinal));
		IElement zone = cut.Find(".moka-fileupload-dropzone");
		Assert.Equal("moka-fileupload-dropzone", zone.ClassName);
		Assert.False(zone.HasAttribute("data-moka-drag-look"));
		Assert.False(zone.HasAttribute("data-moka-room"));
		Assert.Empty(cut.FindAll(".moka-fileupload-dragface, .moka-fileupload-check"));
		Assert.Equal(["report.pdf", "photo.png"], cut.FindAll(".moka-fileupload-filename").Select(n => n.TextContent));
		Assert.Empty(cut.FindAll("[role=status]"));
		Assert.Empty(cut.FindAll("img"));
		Assert.Empty(_module.Invocations);
	}

	[Theory]
	[InlineData(MokaFileUploadLook.CardList, "moka-fileupload--cards", ".moka-fileupload-dropzone.moka-fileupload-dropzone--row")]
	[InlineData(MokaFileUploadLook.Field, "moka-fileupload--field", ".moka-fileupload-field")]
	[InlineData(MokaFileUploadLook.Picture, "moka-fileupload--picture", ".moka-fileupload-picture")]
	[InlineData(MokaFileUploadLook.Gallery, "moka-fileupload--gallery", ".moka-fileupload-gallery")]
	[InlineData(MokaFileUploadLook.Quiet, "moka-fileupload--quiet", ".moka-fileupload-quiet")]
	[InlineData(MokaFileUploadLook.Terminal, "moka-fileupload--terminal", ".moka-fileupload-term")]
	[InlineData(MokaFileUploadLook.Slot, "moka-fileupload--slot", ".moka-fileupload-slot")]
	public void EachLook_MarksTheRoot_AndHoldsTheFileInputInItsTarget(MokaFileUploadLook look, string rootClass, string target)
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, look)
			.Add(x => x.Id, "files")
			.Add(x => x.Class, "mine"));

		IElement root = Root(cut);
		Assert.Contains(rootClass, root.ClassList);
		Assert.Equal("mine", root.ClassList[^1]);
		IElement input = cut.Find("input[type=file]");
		Assert.Equal("files", input.Id);
		Assert.True(input.ParentElement!.Matches(target));
		Assert.Empty(cut.FindAll(".moka-fileupload-dropzone:not(.moka-fileupload-dropzone--row)"));
	}

	[Theory]
	[InlineData(MokaFileUploadLook.Picture)]
	[InlineData(MokaFileUploadLook.Slot)]
	public void ThePictureAndSlotLooks_TakeOneFile_EvenWithMultiple(MokaFileUploadLook look)
	{
		IReadOnlyList<IBrowserFile>? picked = null;
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, look)
			.Add(x => x.Accept, ".pdf,.png")
			.Add(x => x.Multiple, true)
			.Add(x => x.OnFilesSelected, files => picked = files));

		Assert.False(cut.Find("input[type=file]").HasAttribute("multiple"));
		Upload(cut, File("a.pdf"), File("b.pdf"));
		Upload(cut, File("c.png"));

		Assert.Equal("c.png", Assert.Single(picked!).Name);
	}

	[Theory]
	[InlineData(MokaFileUploadLook.Picture, null, "image/*")]
	[InlineData(MokaFileUploadLook.Gallery, null, "image/*")]
	[InlineData(MokaFileUploadLook.Gallery, ".png", ".png")]
	[InlineData(MokaFileUploadLook.CardList, null, null)]
	public void ThePictureLooks_TakeImagesUnlessAcceptSaysOtherwise(MokaFileUploadLook look, string? accept, string? expected)
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, look)
			.Add(x => x.Accept, accept));

		Assert.Equal(expected, cut.Find("input[type=file]").GetAttribute("accept"));
	}

	[Fact]
	public void CardList_DrawsACardPerFile_UnderASlimZone()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, MokaFileUploadLook.CardList)
			.Add(x => x.Multiple, true));
		Upload(cut, File("Q3-plan.pdf", "12345"), File("budget.xlsx"));

		Assert.Equal("Up to 10 MB each", cut.Find(".moka-fileupload-dropzone--row .moka-fileupload-meta").TextContent);
		IElement[] cards = [.. cut.FindAll(".moka-fileupload-card")];
		Assert.Equal(2, cards.Length);
		Assert.Equal("Q3-plan.pdf", cards[0].QuerySelector(".moka-fileupload-name")!.TextContent);
		Assert.Contains("moka-fileupload-tone--error", cards[0].QuerySelector(".moka-fileupload-type")!.ClassList);
		Assert.Contains("moka-fileupload-tone--success", cards[1].QuerySelector(".moka-fileupload-type")!.ClassList);
		Assert.Equal("5 B", cards[0].QuerySelector(".moka-fileupload-meta")!.TextContent.Trim());
		Assert.Equal("Remove Q3-plan.pdf", cards[0].QuerySelector("button")!.GetAttribute("aria-label"));
		Assert.Empty(cut.FindAll(".moka-fileupload-bar"));
	}

	[Fact]
	public void CardList_HidesTheCards_WithoutShowFileList()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, MokaFileUploadLook.CardList)
			.Add(x => x.ShowFileList, false));
		Upload(cut, File("a.pdf"));

		Assert.Empty(cut.FindAll(".moka-fileupload-card"));
	}

	[Fact]
	public void Field_ShowsChipsAndTheTotal_AndBindsTheChipFitterOnce()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, MokaFileUploadLook.Field)
			.Add(x => x.Multiple, true));

		Assert.Equal("No file chosen", cut.Find(".moka-fileupload-placeholder").TextContent);
		Assert.Empty(cut.FindAll(".moka-fileupload-chiprow"));
		Assert.Equal(0, ModuleCalls("bindChips"));

		Upload(cut, File("a.pdf", "1234"), File("b.png", "123456"));

		Assert.Empty(cut.FindAll(".moka-fileupload-placeholder"));
		Assert.Equal(["a.pdf", "b.png"], cut.FindAll(".moka-fileupload-chip-name").Select(n => n.TextContent));
		Assert.Equal("2 files, 10 B", cut.Find(".moka-fileupload-total").TextContent);
		Assert.Equal("Browse", cut.Find("label.moka-fileupload-field-browse").TextContent);
		Assert.Equal(1, ModuleCalls("bindChips"));

		Upload(cut, File("c.txt"));
		Assert.Equal(1, ModuleCalls("bindChips"));
	}

	[Fact]
	public async Task Field_ARemoveButtonInAChip_RemovesItsFile_WithoutOpeningAPick()
	{
		List<string> removed = [];
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, MokaFileUploadLook.Field)
			.Add(x => x.Multiple, true)
			.Add(x => x.OnFileRemoved, file => removed.Add(file.Name)));
		Upload(cut, File("a.pdf"), File("b.pdf"));

		IElement remove = cut.FindAll(".moka-fileupload-chip button")[0];
		Assert.Contains(remove.Attributes, a => string.Equals(a.Name, "blazor:onclick:stopPropagation", StringComparison.OrdinalIgnoreCase));
		await remove.ClickAsync(new MouseEventArgs());

		Assert.Equal(["a.pdf"], removed);
		Assert.Equal(["b.pdf"], cut.FindAll(".moka-fileupload-chip-name").Select(n => n.TextContent));
	}

	[Fact]
	public void Picture_ShowsTheAppsPicture_InACircleByDefault()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, MokaFileUploadLook.Picture)
			.Add(x => x.PictureSource, "/me.png"));

		IElement frame = cut.Find(".moka-fileupload-picture");
		Assert.DoesNotContain("moka-fileupload-picture--square", frame.ClassList);
		Assert.Equal("/me.png", frame.QuerySelector("img")!.GetAttribute("src"));
		Assert.Equal("true", cut.Find(".moka-fileupload-picture-over").GetAttribute("aria-hidden"));
		Assert.Single(cut.FindAll(".moka-fileupload-picture-badge"));
		Assert.Equal("Replace", cut.Find("label.moka-fileupload-action").TextContent);
		Assert.Empty(cut.FindAll("button.moka-fileupload-action"));
	}

	[Fact]
	public void Picture_DropsAnAddressThatIsNoPicture()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, MokaFileUploadLook.Picture)
			.Add(x => x.PictureSource, "javascript:alert(1)"));

		Assert.Empty(cut.FindAll("img"));
		Assert.Contains("moka-fileupload-picture--empty", cut.Find(".moka-fileupload-picture").ClassList);
		Assert.Equal("Add picture", cut.Find(".moka-fileupload-picture-add span").TextContent);
	}

	[Fact]
	public async Task Picture_RemoveTakesAwayThePickedFile_ThenAsksTheAppForItsOwn()
	{
		int appRemoved = 0;
		List<string> removed = [];
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, MokaFileUploadLook.Picture)
			.Add(x => x.PictureShape, MokaPictureShape.RoundedSquare)
			.Add(x => x.PictureSource, "/logo.png")
			.Add(x => x.OnPictureRemoved, () => appRemoved++)
			.Add(x => x.OnFileRemoved, file => removed.Add(file.Name)));

		Assert.Contains("moka-fileupload-picture--square", cut.Find(".moka-fileupload-picture").ClassList);
		Assert.Empty(cut.FindAll(".moka-fileupload-picture-badge"));
		Upload(cut, File("new.png", "x", "image/png"));
		Assert.Equal("new.png", cut.Find(".moka-fileupload-picture-side .moka-fileupload-name").TextContent);

		await cut.Find("button.moka-fileupload-action").ClickAsync(new MouseEventArgs());
		Assert.Equal(["new.png"], removed);
		Assert.Equal(0, appRemoved);

		await cut.Find("button.moka-fileupload-action").ClickAsync(new MouseEventArgs());
		Assert.Equal(1, appRemoved);
	}

	[Fact]
	public void Gallery_MarksTheCover_AndOffersAnAddTileUntilItIsFull()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, MokaFileUploadLook.Gallery)
			.Add(x => x.Multiple, true)
			.Add(x => x.MaxFiles, 3));
		Upload(cut, File("a.png", "x", "image/png"), File("b.png", "x", "image/png"));

		IElement[] thumbs = [.. cut.FindAll("figure.moka-fileupload-thumb")];
		Assert.Equal(2, thumbs.Length);
		Assert.Equal("Cover", thumbs[0].QuerySelector(".moka-fileupload-cover")!.TextContent);
		Assert.Null(thumbs[1].QuerySelector(".moka-fileupload-cover"));
		Assert.Equal("a.png", thumbs[0].QuerySelector("figcaption")!.TextContent);
		Assert.Equal("2 of 3", cut.Find(".moka-fileupload-total").TextContent);
		Assert.Single(cut.FindAll("label.moka-fileupload-add"));

		Upload(cut, File("c.png", "x", "image/png"));
		Assert.Empty(cut.FindAll("label.moka-fileupload-add"));
	}

	// A file dropped on a thumbnail before the input comes to the top must not open in the tab.
	[Fact]
	public void Gallery_AThumbnailCancelsADropThatMissesTheInput()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, MokaFileUploadLook.Gallery)
			.Add(x => x.Multiple, true));
		Upload(cut, File("a.png", "x", "image/png"));

		IElement thumb = cut.Find("figure.moka-fileupload-thumb");
		Assert.Contains(thumb.Attributes, a => string.Equals(a.Name, "blazor:ondrop:preventDefault", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(thumb.Attributes, a => string.Equals(a.Name, "blazor:ondragover:preventDefault", StringComparison.OrdinalIgnoreCase));
	}

	[Theory]
	[InlineData(MokaFileUploadLook.Field, ".moka-fileupload-field")]
	[InlineData(MokaFileUploadLook.Gallery, ".moka-fileupload-gallery")]
	[InlineData(MokaFileUploadLook.Slot, ".moka-fileupload-slot")]
	[InlineData(MokaFileUploadLook.Picture, ".moka-fileupload-picture")]
	public void EveryTarget_LightsUpWhileAFileIsOverIt(MokaFileUploadLook look, string target)
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.Look, look));
		string dragging = $"{target[1..]}--dragging";

		cut.Find("input[type=file]").DragEnter(new DragEventArgs());
		Assert.Contains(dragging, cut.Find(target).ClassList);

		cut.Find("input[type=file]").Drop(new DragEventArgs());
		Assert.DoesNotContain(dragging, cut.Find(target).ClassList);
	}

	[Fact]
	public void Quiet_ListsItsOwnSmallChips_OrTheListLookAsked()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, MokaFileUploadLook.Quiet)
			.Add(x => x.Multiple, true));
		Upload(cut, File("hinge.jpg", "x", "image/jpeg"));

		Assert.Equal("Drop files here or", cut.Find(".moka-fileupload-quiet > span:not(.moka-fileupload-quiet-icon)").TextContent);
		Assert.Single(cut.FindAll(".moka-fileupload-qfile"));

		cut.Render(p => p.Add(x => x.ListLook, MokaFileUploadListLook.Rows));
		Assert.Empty(cut.FindAll(".moka-fileupload-qfile"));
		Assert.Single(cut.FindAll(".moka-fileupload-rows .moka-fileupload-row"));
	}

	[Fact]
	public void Terminal_LogsEveryFile_AndWhatTheLastPickTurnedAway()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, MokaFileUploadLook.Terminal)
			.Add(x => x.Accept, ".pdf,.png")
			.Add(x => x.Multiple, true));

		Assert.Equal("# or press Enter to browse. pdf png, max 10M", cut.Find(".moka-fileupload-term-dim").TextContent);
		Upload(cut, File("Q3-plan.pdf"), File("photos.zip"));

		IElement[] rows = [.. cut.FindAll(".moka-fileupload-term-row")];
		Assert.Equal(2, rows.Length);
		Assert.Equal("Q3-plan.pdf", rows[0].QuerySelector(".moka-fileupload-term-name")!.TextContent);
		Assert.Contains("moka-fileupload-term-row--refused", rows[1].ClassList);
		Assert.Equal("wrong type", rows[1].QuerySelectorAll(".moka-fileupload-term-err")[1].TextContent);
		Assert.Equal("1 added, 1 refused", cut.Find(".moka-fileupload-term-sum").TextContent);
		Assert.Empty(cut.FindAll(".moka-fileupload-errors"));
	}

	[Fact]
	public async Task Slot_IsEmptyWithItsLimits_ThenFilledWithReplaceAndRemove()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, MokaFileUploadLook.Slot)
			.Add(x => x.Accept, ".pdf"));

		Assert.Contains("moka-fileupload-slot--empty", cut.Find(".moka-fileupload-slot").ClassList);
		Assert.Equal("No file yet", cut.Find(".moka-fileupload-name").TextContent);
		Assert.Equal("One PDF, 10 MB max", cut.Find(".moka-fileupload-meta").TextContent);
		Assert.Equal("Choose file", cut.Find("label.moka-fileupload-action").TextContent);

		Upload(cut, File("contract.pdf", "123"));
		Assert.DoesNotContain("moka-fileupload-slot--empty", cut.Find(".moka-fileupload-slot").ClassList);
		Assert.Equal("contract.pdf", cut.Find(".moka-fileupload-name").TextContent);
		Assert.Equal("3 B, PDF", cut.Find(".moka-fileupload-meta").TextContent);
		Assert.Equal("Replace", cut.Find("label.moka-fileupload-action").TextContent);

		await cut.Find(".moka-fileupload-slot-tools button").ClickAsync(new MouseEventArgs());
		Assert.Contains("moka-fileupload-slot--empty", cut.Find(".moka-fileupload-slot").ClassList);
	}

	[Theory]
	[InlineData(MokaFileUploadDragLook.Glow, "glow")]
	[InlineData(MokaFileUploadDragLook.Tint, "tint")]
	[InlineData(MokaFileUploadDragLook.Marching, "marching")]
	[InlineData(MokaFileUploadDragLook.Lift, "lift")]
	public void DragLook_IsWrittenOnTheZone_OfTheStandardAndCardListLooks(MokaFileUploadDragLook dragLook, string name)
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.DragLook, dragLook));
		Assert.Equal(name, cut.Find(".moka-fileupload-dropzone").GetAttribute("data-moka-drag-look"));

		cut.Render(p => p.Add(x => x.Look, MokaFileUploadLook.CardList));
		Assert.Equal(name, cut.Find(".moka-fileupload-dropzone").GetAttribute("data-moka-drag-look"));

		cut.Render(p => p.Add(x => x.Look, MokaFileUploadLook.Quiet));
		Assert.Empty(cut.FindAll("[data-moka-drag-look]"));
	}

	[Theory]
	[InlineData(MokaFileUploadDragLook.Standard, null)]
	[InlineData(MokaFileUploadDragLook.Glow, null)]
	[InlineData(MokaFileUploadDragLook.Marching, null)]
	[InlineData(MokaFileUploadDragLook.Tint, "Drop to add it")]
	[InlineData(MokaFileUploadDragLook.Lift, "Drop to upload")]
	public void TintAndLift_HaveWordsForWhileAFileIsOver(MokaFileUploadDragLook dragLook, string? words)
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.DragLook, dragLook));

		IElement? face = cut.FindAll(".moka-fileupload-dragface").SingleOrDefault();
		Assert.Equal(words, face?.QuerySelector(".moka-fileupload-dragface-text")!.TextContent);
		Assert.True(face is null || face.GetAttribute("aria-hidden") == "true");
	}

	[Fact]
	public void DragLook_KeepsTheDragStateWorking()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.DragLook, MokaFileUploadDragLook.Lift));

		cut.Find("input[type=file]").DragEnter(new DragEventArgs());
		Assert.Contains("moka-fileupload-dropzone--dragging", cut.Find(".moka-fileupload-dropzone").ClassList);

		cut.Find("input[type=file]").DragLeave(new DragEventArgs());
		Assert.DoesNotContain("moka-fileupload-dropzone--dragging", cut.Find(".moka-fileupload-dropzone").ClassList);
	}

	[Fact]
	public void CheckWhileDragging_BindsTheCheckOnce_AndDrawsAFaceForEveryAnswer()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.CheckWhileDragging, true)
			.Add(x => x.Accept, "application/pdf,image/png,image/jpeg")
			.Add(x => x.Multiple, true)
			.Add(x => x.MaxFiles, 3));
		cut.Render(p => p.Add(x => x.Label, "Attachments"));

		Assert.Equal(1, ModuleCalls("bindDragCheck"));
		IElement zone = cut.Find(".moka-fileupload-dropzone");
		Assert.Equal("3", zone.GetAttribute("data-moka-room"));
		IElement check = cut.Find(".moka-fileupload-check");
		Assert.Equal("true", check.GetAttribute("aria-hidden"));

		IElement count = cut.Find(".moka-fileupload-check-face--ok [data-moka-check-count]");
		Assert.Equal("1 file", count.GetAttribute("data-one"));
		Assert.Equal("{0} files", count.GetAttribute("data-many"));
		Assert.Equal(", ready to drop", cut.Find(".moka-fileupload-check-face--ok .moka-fileupload-check-title").TextContent);
		Assert.Equal(" files can't be added", cut.Find(".moka-fileupload-check-face--type .moka-fileupload-check-title").TextContent);
		Assert.Equal("Only PDF, PNG or JPG", cut.Find(".moka-fileupload-check-face--type .moka-fileupload-check-why").TextContent);
		Assert.Equal("The limit is 3", cut.Find(".moka-fileupload-check-face--full .moka-fileupload-check-why").TextContent);
		Assert.Equal("Only 3 more fit", cut.Find(".moka-fileupload-check-face--many .moka-fileupload-check-why").TextContent);

		Upload(cut, File("a.pdf", "x", "application/pdf"));
		Assert.Equal("2", cut.Find(".moka-fileupload-dropzone").GetAttribute("data-moka-room"));
	}

	[Fact]
	public void CheckWhileDragging_ComesOffWhenDisabled()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.CheckWhileDragging, true));

		cut.Render(p => p.Add(x => x.Disabled, true));

		Assert.Equal(1, ModuleCalls("bindDragCheck"));
		Assert.Equal(1, ModuleCalls("unbindDragCheck"));
	}

	[Fact]
	public void CheckWhileDragging_InTheOtherLooks_BindsWithoutTheFaces()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.CheckWhileDragging, true)
			.Add(x => x.Look, MokaFileUploadLook.Slot));

		Assert.Equal(1, ModuleCalls("bindDragCheck"));
		Assert.Equal("1", cut.Find(".moka-fileupload-slot").GetAttribute("data-moka-room"));
		Assert.Empty(cut.FindAll(".moka-fileupload-check"));
	}

	[Theory]
	[InlineData(MokaFileUploadListLook.Rows, ".moka-fileupload-rows .moka-fileupload-row")]
	[InlineData(MokaFileUploadListLook.Progress, ".moka-fileupload-rows .moka-fileupload-row")]
	[InlineData(MokaFileUploadListLook.Chips, ".moka-fileupload-chips .moka-fileupload-fchip")]
	[InlineData(MokaFileUploadListLook.Thumbnails, ".moka-fileupload-strip .moka-fileupload-st")]
	public void ListLook_ReplacesTheStandardList(MokaFileUploadListLook listLook, string item)
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.ListLook, listLook)
			.Add(x => x.Multiple, true));
		Upload(cut, File("a.pdf"), File("b.png", "x", "image/png"));

		Assert.Empty(cut.FindAll(".moka-fileupload-list"));
		Assert.Equal(2, cut.FindAll(item).Count);
		Assert.Equal(["Remove a.pdf", "Remove b.png"],
			cut.FindAll("button.moka-fileupload-x").Select(b => b.GetAttribute("aria-label")));
	}

	[Fact]
	public void ListLook_Rows_SaysTheSizeAndType()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.ListLook, MokaFileUploadListLook.Rows));
		Upload(cut, File("Q3-plan.pdf", "1234"));

		Assert.Equal("4 B, PDF", cut.Find(".moka-fileupload-row .moka-fileupload-meta").TextContent);
	}

	[Theory]
	[InlineData(MokaFileUploadLook.CardList)]
	[InlineData(MokaFileUploadLook.Field)]
	[InlineData(MokaFileUploadLook.Gallery)]
	[InlineData(MokaFileUploadLook.Terminal)]
	public void ListLook_IsIgnoredByTheLooksThatDrawTheirOwnFiles(MokaFileUploadLook look)
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, look)
			.Add(x => x.ListLook, MokaFileUploadListLook.Chips)
			.Add(x => x.Multiple, true));
		Upload(cut, File("a.png", "x", "image/png"));

		Assert.Empty(cut.FindAll(".moka-fileupload-chips"));
	}

	[Fact]
	public async Task ListLook_Chips_ClearAllRemovesEveryFile()
	{
		List<string> removed = [];
		IReadOnlyList<IBrowserFile>? left = null;
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.ListLook, MokaFileUploadListLook.Chips)
			.Add(x => x.Multiple, true)
			.Add(x => x.OnFileRemoved, file => removed.Add(file.Name))
			.Add(x => x.OnFilesSelected, files => left = files));
		Upload(cut, File("a.pdf", "12"), File("b.pdf", "345"));

		Assert.Equal("2 files, 5 B", cut.Find(".moka-fileupload-total-row .moka-fileupload-meta").TextContent);
		await cut.Find(".moka-fileupload-textbtn").ClickAsync(new MouseEventArgs());

		Assert.Equal(["a.pdf", "b.pdf"], removed);
		Assert.Empty(left!);
		Assert.Empty(cut.FindAll(".moka-fileupload-fchip"));
	}

	[Fact]
	public void Status_DrawsEachFilesProgress()
	{
		Dictionary<string, MokaUploadStatus> states = new()
		{
			["done.pdf"] = new MokaUploadStatus(MokaUploadState.Done),
			["going.png"] = new MokaUploadStatus(MokaUploadState.Uploading, 60),
			["broken.xlsx"] = new MokaUploadStatus(MokaUploadState.Failed, 30, "Connection lost"),
			["later.zip"] = new MokaUploadStatus(MokaUploadState.Waiting)
		};
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.ListLook, MokaFileUploadListLook.Progress)
			.Add(x => x.Multiple, true)
			.Add(x => x.Status, file => states.GetValueOrDefault(file.Name)));
		Upload(cut, File("done.pdf"), File("going.png", new string('x', 1000)), File("broken.xlsx"), File("later.zip"), File("new.txt"));

		IElement[] rows = [.. cut.FindAll(".moka-fileupload-row")];
		Assert.Contains("Done", rows[0].QuerySelector(".moka-fileupload-state")!.TextContent, StringComparison.Ordinal);
		Assert.Contains("moka-fileupload-bar--done", rows[0].QuerySelector(".moka-fileupload-bar")!.ClassList);
		Assert.Equal("60%", rows[1].QuerySelector(".moka-fileupload-pct")!.TextContent);
		Assert.Equal("width: 60%", rows[1].QuerySelector(".moka-fileupload-bar > span")!.GetAttribute("style"));
		Assert.Equal("Cancel going.png", rows[1].QuerySelector("button.moka-fileupload-x")!.GetAttribute("aria-label"));
		Assert.Equal("Connection lost", rows[2].QuerySelector(".moka-fileupload-meta")!.TextContent);
		Assert.Contains("moka-fileupload-row--failed", rows[2].ClassList);
		Assert.Empty(rows[2].QuerySelectorAll(".moka-fileupload-retry"));
		Assert.Equal("1 B, waiting", rows[3].QuerySelector(".moka-fileupload-meta")!.TextContent);
		Assert.Null(rows[4].QuerySelector(".moka-fileupload-bar"));
	}

	[Fact]
	public async Task OnRetry_ShowsRetryOnFailedFiles_AndRaisesItWithTheFile()
	{
		List<string> retried = [];
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.ListLook, MokaFileUploadListLook.Progress)
			.Add(x => x.Multiple, true)
			.Add(x => x.Status, file => file.Name == "bad.pdf" ? new MokaUploadStatus(MokaUploadState.Failed) : null)
			.Add(x => x.OnRetry, file => retried.Add(file.Name)));
		Upload(cut, File("bad.pdf"), File("good.pdf"));

		IElement retry = cut.Find(".moka-fileupload-retry");
		Assert.Equal("Retry bad.pdf", retry.GetAttribute("aria-label"));
		Assert.Contains("Retry", retry.TextContent, StringComparison.Ordinal);
		Assert.Single(cut.FindAll(".moka-fileupload-retry"));

		await retry.ClickAsync(new MouseEventArgs());
		Assert.Equal(["bad.pdf"], retried);
	}

	[Fact]
	public void Status_IsReadOutWhenAnUploadFinishesOrFails()
	{
		Dictionary<string, MokaUploadStatus> states = new();
		Func<IBrowserFile, MokaUploadStatus?> status = file => states.GetValueOrDefault(file.Name);
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.Status, status));
		Upload(cut, File("a.pdf"), File("b.pdf"));
		Assert.Equal(string.Empty, cut.Find("[role=status]").TextContent);

		states["a.pdf"] = new MokaUploadStatus(MokaUploadState.Uploading, 50);
		cut.Render(p => p.Add(x => x.Status, status));
		Assert.Equal(string.Empty, cut.Find("[role=status]").TextContent);

		states["a.pdf"] = new MokaUploadStatus(MokaUploadState.Done);
		states["b.pdf"] = new MokaUploadStatus(MokaUploadState.Failed, 10, "Disk full");
		cut.Render(p => p.Add(x => x.Status, status));
		Assert.Equal("a.pdf uploaded. b.pdf failed: Disk full", cut.Find("[role=status]").TextContent);
	}

	[Fact]
	public void PreviewSource_GivesTheAppsAddress_AndDropsOneThatIsNoPicture()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, MokaFileUploadLook.Gallery)
			.Add(x => x.Multiple, true)
			.Add(x => x.PreviewSource, file => file.Name == "a.png" ? "https://cdn.example/a.png" : "javascript:alert(1)"));
		Upload(cut, File("a.png", "x", "image/png"), File("b.png", "x", "image/png"));

		Assert.Equal(["https://cdn.example/a.png"], cut.FindAll("img").Select(i => i.GetAttribute("src")));
		Assert.Equal(0, ModuleCalls("createPreviews"));
	}

	[Fact]
	public async Task Previews_AskTheBrowserForImagesOnly_AndLetTheAddressGoWithTheFile()
	{
		_module.Setup<string?[]>("createPreviews", _ => true).SetResult(["blob:http://localhost/1"]);
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.ListLook, MokaFileUploadListLook.Thumbnails)
			.Add(x => x.Multiple, true)
			.Add(x => x.Previews, true));

		Upload(cut, File("notes.txt"), File("photo.png", "x", "image/png"));

		JSRuntimeInvocation create = Assert.Single(_module.Invocations["createPreviews"]);
		Assert.IsType<ElementReference>(create.Arguments[0]);
		Assert.Equal([1], (int[])create.Arguments[1]!);
		cut.WaitForAssertion(() => Assert.Equal("blob:http://localhost/1", cut.Find("img").GetAttribute("src")));

		await cut.FindAll("button.moka-fileupload-x")[1].ClickAsync(new MouseEventArgs());

		JSRuntimeInvocation revoke = Assert.Single(_module.Invocations["revokePreviews"]);
		Assert.Equal(["blob:http://localhost/1"], (string[])revoke.Arguments[0]!);
	}

	[Fact]
	public async Task Previews_LetEveryAddressGo_WhenTheUploadGoes()
	{
		_module.Setup<string?[]>("createPreviews", _ => true).SetResult(["blob:http://localhost/2"]);
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.ListLook, MokaFileUploadListLook.Thumbnails)
			.Add(x => x.Previews, true));
		Upload(cut, File("photo.png", "x", "image/png"));
		cut.WaitForAssertion(() => Assert.Single(cut.FindAll("img")));

		await DisposeComponentsAsync();

		JSRuntimeInvocation revoke = Assert.Single(_module.Invocations["revokePreviews"]);
		Assert.Equal(["blob:http://localhost/2"], (string[])revoke.Arguments[0]!);
	}

	[Fact]
	public void Previews_Off_NeverAskTheBrowser()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p.Add(x => x.Look, MokaFileUploadLook.Gallery));
		Upload(cut, File("photo.png", "x", "image/png"));

		Assert.Equal(0, ModuleCalls("createPreviews"));
		Assert.Empty(cut.FindAll("img"));
	}

	[Fact]
	public void Text_ReplacesTheWords()
	{
		MokaFileUploadText words = new()
		{
			DragHere = "Släpp filer här eller",
			Browse = "Bläddra",
			Remove = name => $"Ta bort {name}",
			NotAccepted = name => $"{name} går inte"
		};
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Text, words)
			.Add(x => x.Accept, ".pdf"));
		Upload(cut, File("a.pdf"));
		Upload(cut, File("b.txt"));

		Assert.Equal("Släpp filer här eller", cut.Find(".moka-fileupload-text").TextContent);
		Assert.Equal("Bläddra", cut.Find(".moka-fileupload-browse").TextContent);
		Assert.Equal("Ta bort a.pdf", cut.Find(".moka-fileupload-remove").GetAttribute("aria-label"));
		Assert.Equal("b.txt går inte", cut.Find(".moka-fileupload-error").TextContent);
	}

	[Theory]
	[InlineData(MokaFileUploadLook.Field)]
	[InlineData(MokaFileUploadLook.Gallery)]
	[InlineData(MokaFileUploadLook.Slot)]
	public void Disabled_DisablesTheInputAndTheButtonsInEveryLook(MokaFileUploadLook look)
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, look)
			.Add(x => x.Multiple, true));
		Upload(cut, File("a.png", "x", "image/png"));

		cut.Render(p => p.Add(x => x.Disabled, true));

		Assert.True(cut.Find("input[type=file]").HasAttribute("disabled"));
		Assert.All(cut.FindAll("button"), b => Assert.True(b.HasAttribute("disabled")));
		Assert.Contains(cut.FindAll("[class]"), e => e.ClassList.Any(c => c.EndsWith("--disabled", StringComparison.Ordinal)));
	}

	[Fact]
	public void APicker_OpensFromEveryLooksTarget()
	{
		FakeFilePicker picker = new() { Files = [new PickedFile("a.pdf")] };
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.Look, MokaFileUploadLook.Slot)
			.Add(x => x.FilePicker, picker));

		IElement slot = cut.Find(".moka-fileupload-slot");
		Assert.Contains(slot.Attributes, a => string.Equals(a.Name, "blazor:onclick:preventDefault", StringComparison.OrdinalIgnoreCase));
		cut.Find("input[type=file]").Click();

		Assert.Equal([new Moka.Red.Core.Files.MokaFilePickRequest(false, null)], picker.Requests);
		Assert.Equal("a.pdf", cut.Find(".moka-fileupload-name").TextContent);
	}
}
