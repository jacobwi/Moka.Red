using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.SignaturePad;

namespace Moka.Red.Forms.Tests.Components;

// A Value from the parent was kept as state but never drawn, so a stored signature showed as an
// empty pad, and a parent clearing Value left the old drawing on the canvas. The drawing itself is
// JS; these tests cover what the component asks the module to do, and when it must not ask.
public class MokaSignaturePadTests : BunitContext
{
	private const string ModulePath = "./_content/Moka.Red.Forms/SignaturePad/MokaSignaturePad.razor.js";
	private const string Stored = "data:image/png;base64,U1RPUkVE";
	private const string Drawn = "data:image/png;base64,RFJBV04=";

	private readonly BunitJSModuleInterop _module;

	public MokaSignaturePadTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
		_module.Setup<bool>("showSignature", _ => true).SetResult(true);
	}

	[Fact]
	public void DrawsAValueFromTheParent_OnTheCanvas()
	{
		IRenderedComponent<MokaSignaturePad> cut = Render<MokaSignaturePad>(p => p
			.Add(x => x.Value, Stored)
			.Add(x => x.BackgroundColor, "#fafafa"));

		JSRuntimeInvocation show = _module.VerifyInvoke("showSignature");
		ElementReference canvas = Assert.IsType<ElementReference>(show.Arguments[0]);
		Assert.Equal(cut.Find("canvas").GetAttribute("blazor:elementReference"), canvas.Id);
		Assert.Equal(Stored, show.Arguments[1]);
		Assert.Equal("#fafafa", show.Arguments[2]);

		// The pad can still be drawn on.
		_module.VerifyInvoke("initCanvasDraw");
		Assert.Empty(cut.FindAll(".moka-signature-pad__placeholder"));
	}

	// Showing a stored signature is the main use of a read-only pad, which never attaches drawing.
	[Fact]
	public void AReadOnlyPad_StillDrawsTheValue()
	{
		Render<MokaSignaturePad>(p => p
			.Add(x => x.Value, Stored)
			.Add(x => x.ReadOnly, true));

		Assert.Equal(Stored, _module.VerifyInvoke("showSignature").Arguments[1]);
		_module.VerifyNotInvoke("initCanvasDraw");
	}

	// Drawing was attached only on the first render, so a pad that started read-only or disabled
	// could never be drawn on. Attaching repaints the background, so the signature comes back.
	[Fact]
	public void APadThatBecomesEditable_AttachesDrawing_AndRedrawsItsSignature()
	{
		IRenderedComponent<MokaSignaturePad> cut = Render<MokaSignaturePad>(p => p
			.Add(x => x.Value, Stored)
			.Add(x => x.ReadOnly, true));
		_module.VerifyNotInvoke("initCanvasDraw");

		cut.Render(p => p.Add(x => x.ReadOnly, false));

		_module.VerifyInvoke("initCanvasDraw");
		IReadOnlyList<JSRuntimeInvocation> shows = _module.VerifyInvoke("showSignature", 2);
		Assert.Equal(Stored, shows[1].Arguments[1]);
	}

	// Height was documented and never applied, so every pad took its buffer's 3:1 shape.
	[Fact]
	public void Height_SizesTheCanvas_AndTheBufferIsFittedBeforeDrawing()
	{
		IRenderedComponent<MokaSignaturePad> cut = Render<MokaSignaturePad>(p => p.Add(x => x.Height, "320px"));

		Assert.Contains("height: 320px", cut.Find("canvas").GetAttribute("style"), StringComparison.Ordinal);
		List<string> calls = _module.Invocations.Select(i => i.Identifier).ToList();
		int fit = calls.IndexOf("fitCanvas");
		Assert.InRange(fit, 0, calls.IndexOf("initCanvasDraw") - 1);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void ClearsTheCanvas_WhenTheParentEmptiesTheValue(string? empty)
	{
		IRenderedComponent<MokaSignaturePad> cut = Render<MokaSignaturePad>(p => p.Add(x => x.Value, Stored));

		cut.Render(p => p.Add(x => x.Value, empty));

		IReadOnlyList<JSRuntimeInvocation> shows = _module.VerifyInvoke("showSignature", 2);
		Assert.Equal(empty, shows[1].Arguments[1]);
		Assert.Single(cut.FindAll(".moka-signature-pad__placeholder"));
	}

	// A bound parent passes back what the pad reported. Drawing that again would wipe the strokes
	// the undo button works through, on every stroke.
	[Fact]
	public async Task AValueThePadReported_IsNotDrawnAgain()
	{
		string? value = Stored;
		IRenderedComponent<MokaSignaturePad> cut = Render<MokaSignaturePad>(p => p
			.Add(x => x.Value, value)
			.Add(x => x.ValueChanged, v => value = v));

		await cut.InvokeAsync(() => cut.Instance.OnSignatureChanged(Drawn));
		cut.Render(p => p.Add(x => x.Value, value));

		Assert.Equal(Drawn, value);
		Assert.Equal(Stored, Assert.Single(_module.Invocations["showSignature"]).Arguments[1]);
	}

	// null and "" are both an empty pad, so going from one to the other has nothing to clear.
	[Fact]
	public void AnEmptyPad_ToldToBeEmpty_DrawsNothing()
	{
		IRenderedComponent<MokaSignaturePad> cut = Render<MokaSignaturePad>(p => p.Add(x => x.Value, (string?)null));

		cut.Render(p => p.Add(x => x.Value, ""));
		cut.Render(p => p.Add(x => x.Value, Stored));

		Assert.Equal(Stored, Assert.Single(_module.Invocations["showSignature"]).Arguments[1]);
	}

	// The module repaints the stored signature under the strokes that are left. Once the last one is
	// gone the canvas shows the stored value again, so that is the value, not null.
	[Fact]
	public async Task UndoingEveryStroke_OverAStoredValue_ReportsTheStoredValue()
	{
		List<string?> changes = [];
		IRenderedComponent<MokaSignaturePad> cut = Render<MokaSignaturePad>(p => p
			.Add(x => x.Value, Stored)
			.Add(x => x.ValueChanged, v => changes.Add(v)));

		await cut.InvokeAsync(() => cut.Instance.OnSignatureChanged(Drawn));
		await cut.FindAll("button").Single(b => b.TextContent.Trim() == "Undo").ClickAsync(new MouseEventArgs());
		await cut.InvokeAsync(() => cut.Instance.OnSignatureChanged(null));

		JSRuntimeInvocation undo = _module.VerifyInvoke("undoSignature");
		Assert.Equal(["#000000", 2, "#ffffff"], undo.Arguments.Skip(1));
		Assert.Equal([Drawn, Stored], changes);
		Assert.Empty(cut.FindAll(".moka-signature-pad__placeholder"));
	}

	[Fact]
	public async Task Clear_EmptiesThePad_AndTellsTheParent()
	{
		List<string?> changes = [];
		IRenderedComponent<MokaSignaturePad> cut = Render<MokaSignaturePad>(p => p
			.Add(x => x.Value, Stored)
			.Add(x => x.ValueChanged, v => changes.Add(v)));

		await cut.FindAll("button").Single(b => b.TextContent.Trim() == "Clear").ClickAsync(new MouseEventArgs());

		_module.VerifyInvoke("clearSignature");
		Assert.Equal([null], changes);
		Assert.Single(cut.FindAll(".moka-signature-pad__placeholder"));

		// With nothing stored any more, undoing past the last stroke reports an empty pad.
		await cut.InvokeAsync(() => cut.Instance.OnSignatureChanged(null));
		Assert.Equal([null, null], changes);
	}

	[Fact]
	public async Task DetachesDrawing_WhenDisposed()
	{
		IRenderedComponent<MokaSignaturePad> cut = Render<MokaSignaturePad>();
		string? canvasId = cut.Find("canvas").GetAttribute("blazor:elementReference");

		await cut.Instance.DisposeAsync();

		ElementReference canvas = Assert.IsType<ElementReference>(_module.VerifyInvoke("removeCanvasDraw").Arguments[0]);
		Assert.Equal(canvasId, canvas.Id);
	}
}
