using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Moka.Red.Forms.DropOverlay;

namespace Moka.Red.Forms.Tests.Components;

public class MokaDropOverlayTests : BunitContext
{
	private const string ModulePath = "./_content/Moka.Red.Forms/DropOverlay/MokaDropOverlay.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaDropOverlayTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
		_module.SetupVoid("bindDropOverlay", _ => true).SetVoidResult();
		_module.SetupVoid("unbindDropOverlay", _ => true).SetVoidResult();
	}

	private static void Drop(IRenderedComponent<MokaDropOverlay> cut, params InputFileContent[] files) =>
		cut.FindComponent<InputFile>().UploadFiles(files);

	[Fact]
	public void OnThePage_TheWindowListenersAreBound_OnceAndForTheRoot()
	{
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>();
		cut.Render(p => p.Add(x => x.Title, "Drop here"));

		JSRuntimeInvocation bind = Assert.Single(_module.Invocations["bindDropOverlay"]);
		Assert.IsType<Microsoft.AspNetCore.Components.ElementReference>(bind.Arguments[0]);
	}

	// Hidden until a drag of files reaches the window: the module sets data-moka-active, which Blazor never renders.
	[Fact]
	public void TheLayer_StartsHidden_WithTheInputCoveringIt()
	{
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p.Add(x => x.Accept, ".conf,image/*"));

		IElement root = cut.Find(".moka-drop-overlay");
		Assert.False(root.HasAttribute("data-moka-active"));
		IElement input = cut.Find("input[type=file]");
		Assert.Equal(".conf,image/*", input.GetAttribute("accept"));
		Assert.True(input.HasAttribute("multiple"));
		Assert.Equal("-1", input.GetAttribute("tabindex"));
		Assert.Equal("true", cut.Find(".moka-drop-overlay__panel").GetAttribute("aria-hidden"));
	}

	[Fact]
	public void Disabled_UnbindsTheListeners_AndTakesNoDrop()
	{
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>();

		cut.Render(p => p.Add(x => x.Disabled, true));

		Assert.Single(_module.Invocations["unbindDropOverlay"]);
		Assert.Empty(cut.FindAll("input[type=file]"));

		cut.Render(p => p.Add(x => x.Disabled, false));
		Assert.Equal(2, _module.Invocations["bindDropOverlay"].Count);
	}

	[Fact]
	public async Task Disposing_UnbindsTheListeners()
	{
		Render<MokaDropOverlay>();

		await DisposeComponentsAsync();

		Assert.Single(_module.Invocations["unbindDropOverlay"]);
	}

	[Fact]
	public void ADrop_HandsOverTheFilesThatPass_AndNamesTheRest()
	{
		IReadOnlyList<IBrowserFile>? accepted = null;
		IReadOnlyList<MokaRejectedFile>? rejected = null;
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p
			.Add(x => x.Accept, ".conf, .ZIP")
			.Add(x => x.MaxFileSize, 4)
			.Add(x => x.OnFiles, files => accepted = files)
			.Add(x => x.OnRejected, files => rejected = files));

		Drop(cut,
			InputFileContent.CreateFromText("abc", "home.conf"),
			InputFileContent.CreateFromText("abc", "notes.txt"),
			InputFileContent.CreateFromText("abcdefgh", "big.conf"),
			InputFileContent.CreateFromText("ab", "all.zip"));

		Assert.Equal(["home.conf", "all.zip"], accepted!.Select(f => f.Name));
		Assert.Equal([("notes.txt", MokaFileRejection.Type), ("big.conf", MokaFileRejection.Size)],
			rejected!.Select(r => (r.File.Name, r.Reason)));
	}

	[Fact]
	public void MaxFiles_TurnsAwayTheFilesPastIt()
	{
		IReadOnlyList<IBrowserFile>? accepted = null;
		IReadOnlyList<MokaRejectedFile>? rejected = null;
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p
			.Add(x => x.MaxFiles, 2)
			.Add(x => x.OnFiles, files => accepted = files)
			.Add(x => x.OnRejected, files => rejected = files));

		Drop(cut,
			InputFileContent.CreateFromText("a", "1.txt"),
			InputFileContent.CreateFromText("b", "2.txt"),
			InputFileContent.CreateFromText("c", "3.txt"));

		Assert.Equal(2, accepted!.Count);
		MokaRejectedFile extra = Assert.Single(rejected!);
		Assert.Equal(("3.txt", MokaFileRejection.Count), (extra.File.Name, extra.Reason));
	}

	[Fact]
	public void WithoutMultiple_TheFirstFileIsTaken()
	{
		IReadOnlyList<IBrowserFile>? accepted = null;
		IReadOnlyList<MokaRejectedFile>? rejected = null;
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p
			.Add(x => x.Multiple, false)
			.Add(x => x.OnFiles, files => accepted = files)
			.Add(x => x.OnRejected, files => rejected = files));

		Drop(cut,
			InputFileContent.CreateFromText("a", "1.txt"),
			InputFileContent.CreateFromText("b", "2.txt"));

		Assert.Equal("1.txt", Assert.Single(accepted!).Name);
		Assert.Equal(MokaFileRejection.Count, Assert.Single(rejected!).Reason);
	}

	[Fact]
	public void ADropWhereNothingPasses_RaisesNoOnFiles()
	{
		bool raised = false;
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p
			.Add(x => x.Accept, "image/*")
			.Add(x => x.OnFiles, _ => raised = true));

		Drop(cut, InputFileContent.CreateFromText("a", "notes.txt", contentType: "text/plain"));

		Assert.False(raised);
	}

	// Chrome sends no change for a drop of the files an input already holds, so each drop gets a fresh input.
	[Fact]
	public void AfterADrop_TheInputIsReplaced()
	{
		int drops = 0;
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p.Add(x => x.OnFiles, _ => drops++));
		InputFile first = cut.FindComponent<InputFile>().Instance;

		Drop(cut, InputFileContent.CreateFromText("a", "1.txt"));

		Assert.Equal(1, drops);
		Assert.NotSame(first, cut.FindComponent<InputFile>().Instance);
	}

	[Fact]
	public void TheTitleAndHint_ShowInThePanel_OrTheContentReplacesThem()
	{
		IRenderedComponent<MokaDropOverlay> cut = Render<MokaDropOverlay>(p => p
			.Add(x => x.Title, "Drop to import")
			.Add(x => x.Hint, ".conf · .zip"));

		Assert.Equal("Drop to import", cut.Find(".moka-drop-overlay__title").TextContent);
		Assert.Equal(".conf · .zip", cut.Find(".moka-drop-overlay__hint").TextContent);
		Assert.Equal("Drop to import", cut.Find("input[type=file]").GetAttribute("aria-label"));

		cut.Render(p => p.Add(x => x.ChildContent, "<b class=\"own\">Mine</b>"));
		Assert.Empty(cut.FindAll(".moka-drop-overlay__title"));
		Assert.Equal("Mine", cut.Find(".own").TextContent);
	}
}
