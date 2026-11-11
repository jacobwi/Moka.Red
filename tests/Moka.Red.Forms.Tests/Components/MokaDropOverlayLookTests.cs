using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Moka.Red.Forms.DropOverlay;

namespace Moka.Red.Forms.Tests.Components;

// The drop layer's looks from the upload board (U4). The default keeps today's panel and markup.
public class MokaDropOverlayLookTests : BunitContext
{
	private const string ModulePath = "./_content/Moka.Red.Forms/DropOverlay/MokaDropOverlay.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaDropOverlayLookTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
		_module.SetupVoid("bindDropOverlay", _ => true).SetVoidResult();
		_module.SetupVoid("unbindDropOverlay", _ => true).SetVoidResult();
	}

	[Fact]
	public void TheDefault_KeepsTodaysPanel_WithNoCountWords()
	{
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p
			.Add(x => x.Destination, "Q3 plans")
			.Add(x => x.Hint, "PDF only"));

		IElement root = cut.Find(".moka-drop-overlay");
		Assert.Equal("moka-drop-overlay", root.ClassName);
		Assert.False(root.HasAttribute("data-moka-count-one"));
		Assert.False(root.HasAttribute("data-moka-count-many"));
		Assert.Equal("Drop files here", cut.Find(".moka-drop-overlay__panel .moka-drop-overlay__title").TextContent);
		Assert.Equal("PDF only", cut.Find(".moka-drop-overlay__hint").TextContent);
		Assert.Empty(cut.FindAll(".moka-drop-overlay__box, [data-moka-drop-count]"));
		Assert.DoesNotContain("Q3 plans", cut.Markup, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(MokaDropOverlayLook.Card, "moka-drop-overlay--card", "moka-drop-overlay__box--card")]
	[InlineData(MokaDropOverlayLook.Frame, "moka-drop-overlay--frame", "moka-drop-overlay__box--pill")]
	[InlineData(MokaDropOverlayLook.Corner, "moka-drop-overlay--corner", "moka-drop-overlay__box--corner")]
	[InlineData(MokaDropOverlayLook.Wash, "moka-drop-overlay--wash", "moka-drop-overlay__box--wash")]
	[InlineData(MokaDropOverlayLook.Banner, "moka-drop-overlay--banner", "moka-drop-overlay__box--banner")]
	public void EachLook_DrawsItsOwnBox_WithTheTitleAndASlotForTheCount(MokaDropOverlayLook look, string rootClass, string boxClass)
	{
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p
			.Add(x => x.Look, look)
			.Add(x => x.Class, "mine"));

		IElement root = cut.Find(".moka-drop-overlay");
		Assert.Equal(["moka-drop-overlay", rootClass, "mine"], root.ClassList);
		Assert.Equal("Drop 1 file", root.GetAttribute("data-moka-count-one"));
		Assert.Equal("Drop {0} files", root.GetAttribute("data-moka-count-many"));
		Assert.Empty(cut.FindAll(".moka-drop-overlay__panel"));
		IElement box = cut.Find(".moka-drop-overlay__box");
		Assert.Contains(boxClass, box.ClassList);
		Assert.Equal("true", box.GetAttribute("aria-hidden"));
		Assert.Equal("Drop files here", box.QuerySelector(".moka-drop-overlay__headline-title")!.TextContent);
		Assert.Equal(string.Empty, box.QuerySelector("[data-moka-drop-count]")!.TextContent);
		Assert.Single(cut.FindAll("input[type=file]"));
	}

	[Fact]
	public void TheCountWords_FollowTheirParameters()
	{
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p
			.Add(x => x.Look, MokaDropOverlayLook.Wash)
			.Add(x => x.OneFileTitle, "Släpp 1 fil")
			.Add(x => x.CountTitle, "Släpp {0} filer"));

		IElement root = cut.Find(".moka-drop-overlay");
		Assert.Equal("Släpp 1 fil", root.GetAttribute("data-moka-count-one"));
		Assert.Equal("Släpp {0} filer", root.GetAttribute("data-moka-count-many"));
	}

	[Fact]
	public void Card_SaysWhereTheFilesGo_AndWhatItTakes()
	{
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p
			.Add(x => x.Look, MokaDropOverlayLook.Card)
			.Add(x => x.Accept, ".pdf, image/png, image/*")
			.Add(x => x.MaxFileSize, 5 * 1024 * 1024)
			.Add(x => x.Destination, "Q3 plans"));

		IElement sub = cut.Find(".moka-drop-overlay__sub");
		Assert.Equal("to Q3 plans", sub.TextContent);
		Assert.Equal("Q3 plans", sub.QuerySelector("b")!.TextContent);
		Assert.Equal(["PDF", "PNG", "image/*", "5 MB each"], cut.FindAll(".moka-drop-overlay__types > span").Select(s => s.TextContent));
	}

	[Fact]
	public void DestinationText_PutsTheNameWhereItSays()
	{
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p
			.Add(x => x.Look, MokaDropOverlayLook.Frame)
			.Add(x => x.Destination, "Q3")
			.Add(x => x.DestinationText, "into {0} now"));

		Assert.Equal("into Q3 now", cut.Find(".moka-drop-overlay__sub").TextContent);
	}

	[Fact]
	public void Banner_NamesTheFolderWithItsIcon()
	{
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p
			.Add(x => x.Look, MokaDropOverlayLook.Banner)
			.Add(x => x.Destination, "Q3 plans"));

		IElement name = cut.Find(".moka-drop-overlay__sub b");
		Assert.Equal("Q3 plans", name.TextContent.Trim());
		Assert.NotNull(name.QuerySelector("svg"));
	}

	[Theory]
	[InlineData(null, "Anywhere works")]
	[InlineData("Q3 plans", "to Q3 plans")]
	public void Corner_SaysWhereTheFilesGo_ElseTheHint(string? destination, string note)
	{
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p
			.Add(x => x.Look, MokaDropOverlayLook.Corner)
			.Add(x => x.Hint, "Anywhere works")
			.Add(x => x.Destination, destination));

		Assert.Equal(note, cut.Find(".moka-drop-overlay__note").TextContent);
	}

	[Fact]
	public void ChildContent_ReplacesWhatALookDraws()
	{
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p
			.Add(x => x.Look, MokaDropOverlayLook.Card)
			.Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<em>Mine</em>"))));

		Assert.Equal("Mine", cut.Find(".moka-drop-overlay__box em").TextContent);
		Assert.Empty(cut.FindAll(".moka-drop-overlay__headline"));
	}

	[Fact]
	public void ALook_KeepsTheListenersAndTheDrops()
	{
		IReadOnlyList<IBrowserFile>? dropped = null;
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p
			.Add(x => x.Look, MokaDropOverlayLook.Frame)
			.Add(x => x.OnFiles, files => dropped = files));

		Assert.Single(_module.Invocations["bindDropOverlay"]);
		cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("a", "a.txt"));
		Assert.Equal("a.txt", Assert.Single(dropped!).Name);

		cut.Render(p => p.Add(x => x.Disabled, true));
		Assert.Single(_module.Invocations["unbindDropOverlay"]);
	}

	[Fact]
	public void Padding_AndRounding_GoOnTheLooksBox()
	{
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p
			.Add(x => x.Look, MokaDropOverlayLook.Card)
			.Add(x => x.PaddingValue, "2rem")
			.Add(x => x.RoundedValue, "4px"));

		Assert.Equal("padding: 2rem; border-radius: 4px", cut.Find(".moka-drop-overlay__box").GetAttribute("style"));
	}
}
