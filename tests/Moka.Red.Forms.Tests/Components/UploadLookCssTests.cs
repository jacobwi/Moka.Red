using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// What the upload and drop layer looks do only in CSS, read from their stylesheets since bUnit applies none.
public class UploadLookCssTests
{
	private const string Upload = "MokaFileUpload.razor.css";
	private const string Overlay = "MokaDropOverlay.razor.css";

	private const string Marching = ".moka-fileupload-dropzone.moka-fileupload-dropzone--dragging[data-moka-drag-look=\"marching\"]";
	private const string Lift = ".moka-fileupload-dropzone.moka-fileupload-dropzone--dragging[data-moka-drag-look=\"lift\"]";

	[Fact]
	public void Marching_MovesTheBorder_AndStandsStillForLessMotion()
	{
		Assert.StartsWith("moka-fileupload-march", ScopedCss.Values(Upload, Marching, "animation")[0], StringComparison.Ordinal);
		Assert.Equal("none", ScopedCss.Values(Upload, Marching, "animation")[^1]);
		Assert.Equal("transparent", ScopedCss.Declarations(Upload, Marching)["border-color"]);
	}

	[Fact]
	public void Lift_RaisesTheZone_ButNotForLessMotion()
	{
		Assert.Equal("translateY(calc(-1 * var(--moka-spacing-xs)))", ScopedCss.Values(Upload, Lift, "transform")[0]);
		Assert.Equal("none", ScopedCss.Values(Upload, Lift, "transform")[^1]);
		Assert.Equal("var(--moka-shadow-popup)", ScopedCss.Declarations(Upload, Lift)["box-shadow"]);
	}

	[Fact]
	public void Glow_GoesSolidWithARing()
	{
		IReadOnlyDictionary<string, string> glow = ScopedCss.Declarations(Upload,
			".moka-fileupload-dropzone.moka-fileupload-dropzone--dragging[data-moka-drag-look=\"glow\"]");

		Assert.Equal("solid", glow["border-style"]);
		Assert.Contains("var(--moka-color-primary-ring)", glow["box-shadow"], StringComparison.Ordinal);
	}

	// The new design's dragging tint would otherwise outrank every drag look and the check's colors.
	[Fact]
	public void TheGateDraggingTint_LeavesTheDragLooksAndTheCheckAlone()
	{
		IReadOnlyDictionary<string, string> tint = ScopedCss.Declarations(Upload,
			".moka-gate .moka-fileupload-dropzone.moka-fileupload-dropzone--dragging:not([data-moka-drag-look]):not([data-moka-drag-check])");

		Assert.Equal("var(--moka-color-primary-bg)", tint["background"]);
		Assert.Empty(ScopedCss.Declarations(Upload, ".moka-gate .moka-fileupload-dropzone.moka-fileupload-dropzone--dragging"));
	}

	[Fact]
	public void TheCheckFaces_ShowOnlyForTheirAnswer()
	{
		Assert.Equal("none", ScopedCss.Declarations(Upload, ".moka-fileupload-check-face")["display"]);
		Assert.Equal("contents", ScopedCss.Declarations(Upload, ".moka-fileupload-dropzone[data-moka-drag-check] > .moka-fileupload-check")["display"]);
		foreach (string answer in new[] { "ok", "type", "full", "many" })
		{
			Assert.Equal("flex", ScopedCss.Declarations(Upload,
				$".moka-fileupload-dropzone[data-moka-drag-check=\"{answer}\"] .moka-fileupload-check-face--{answer}")["display"]);
		}

		Assert.Equal("var(--moka-color-success-bg)", ScopedCss.Declarations(Upload, ".moka-fileupload-slot[data-moka-drag-check=\"ok\"]")["background"]);
		Assert.Equal("var(--moka-color-error-bg)",
			ScopedCss.Declarations(Upload, ".moka-fileupload-slot[data-moka-drag-check]:not([data-moka-drag-check=\"ok\"])")["background"]);
	}

	[Theory]
	[InlineData("field")]
	[InlineData("picture")]
	[InlineData("gallery")]
	[InlineData("quiet")]
	[InlineData("term")]
	[InlineData("slot")]
	public void EveryTarget_IsCoveredByTheInput_WhichComesToTheTopWhileAFileIsOver(string target)
	{
		IReadOnlyDictionary<string, string> input = ScopedCss.Declarations(Upload, $".moka-fileupload-{target} ::deep .moka-fileupload-native");
		Assert.Equal("absolute", input["position"]);
		Assert.Equal("0", input["opacity"]);
		Assert.Equal("1", input["z-index"]);

		Assert.Equal("3", ScopedCss.Declarations(Upload, $".moka-fileupload-{target}--dragging ::deep .moka-fileupload-native")["z-index"]);
		Assert.Equal("none", ScopedCss.Declarations(Upload, $".moka-fileupload-{target}--disabled")["pointer-events"]);
	}

	[Fact]
	public void ThumbnailRemoveButtons_ShowOnHoverOrFocus()
	{
		Assert.Equal("0", ScopedCss.Declarations(Upload, ".moka-fileupload-thumb-tools")["opacity"]);
		Assert.Equal("1", ScopedCss.Declarations(Upload, ".moka-fileupload-thumb:focus-within .moka-fileupload-thumb-tools")["opacity"]);
		Assert.Equal("1", ScopedCss.Declarations(Upload, ".moka-fileupload-st:hover .moka-fileupload-thumb-tools")["opacity"]);
	}

	[Fact]
	public void ThePictureFrame_ShowsReplaceOnHoverOrKeyboardFocus()
	{
		Assert.Equal("0", ScopedCss.Declarations(Upload, ".moka-fileupload-picture-over")["opacity"]);
		Assert.Equal("1", ScopedCss.Declarations(Upload,
			".moka-fileupload-picture:has(> .moka-fileupload-native:focus-visible) .moka-fileupload-picture-over")["opacity"]);
		Assert.Equal("var(--moka-radius-full)", ScopedCss.Declarations(Upload, ".moka-fileupload-picture")["border-radius"]);
		Assert.Equal("var(--moka-radius-lg)", ScopedCss.Declarations(Upload, ".moka-fileupload-picture--square")["border-radius"]);
	}

	[Fact]
	public void TheFieldsChips_LetClicksThroughToTheInput_ButNotTheirRemoveButtons()
	{
		Assert.Equal("none", ScopedCss.Declarations(Upload, ".moka-fileupload-chiprow")["pointer-events"]);
		Assert.Equal("auto", ScopedCss.Declarations(Upload, ".moka-fileupload-chip .moka-fileupload-x")["pointer-events"]);
		Assert.Equal("none", ScopedCss.Declarations(Upload, ".moka-fileupload-more:empty")["display"]);
	}

	[Fact]
	public void TheTerminalCaret_StandsStillForLessMotion()
	{
		Assert.Equal("none", ScopedCss.Values(Upload, ".moka-fileupload-term-caret", "animation")[^1]);
	}

	[Fact]
	public void TheLayerLooks_HideAsMuchOfThePageAsTheBoardSays()
	{
		Assert.Equal("var(--moka-color-backdrop)", ScopedCss.Declarations(Overlay, ".moka-drop-overlay.moka-drop-overlay--card")["background"]);
		Assert.Equal("transparent", ScopedCss.Declarations(Overlay, ".moka-drop-overlay.moka-drop-overlay--frame")["background"]);
		Assert.Equal("transparent", ScopedCss.Declarations(Overlay, ".moka-drop-overlay.moka-drop-overlay--corner")["background"]);
		Assert.Equal("end", ScopedCss.Declarations(Overlay, ".moka-drop-overlay.moka-drop-overlay--corner")["place-items"]);
		Assert.Contains("var(--moka-color-primary)", ScopedCss.Declarations(Overlay, ".moka-drop-overlay.moka-drop-overlay--wash")["background"],
			StringComparison.Ordinal);
		Assert.Equal("end center", ScopedCss.Declarations(Overlay, ".moka-drop-overlay.moka-drop-overlay--banner")["place-items"]);
	}

	[Fact]
	public void TheCountReplacesTheTitle_OnceTheBrowserSaysIt()
	{
		Assert.Equal("none", ScopedCss.Declarations(Overlay, ".moka-drop-overlay[data-moka-count] .moka-drop-overlay__headline-title")["display"]);
		Assert.Equal("none", ScopedCss.Declarations(Overlay, ".moka-drop-overlay:not([data-moka-count]) .moka-drop-overlay__headline-count")["display"]);
	}

	[Fact]
	public void TheCardsPulse_StandsStillForLessMotion()
	{
		Assert.Equal("none", ScopedCss.Values(Overlay, ".moka-drop-overlay__mark--pulse::after", "animation")[^1]);
	}
}
