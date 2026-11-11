using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// What the color looks do in CSS alone, read from the stylesheets since bUnit applies none.
public class MokaColorLookCssTests
{
	private const string Picker = "MokaColorPicker.razor.css";
	private const string Input = "MokaColorInput.razor.css";

	private static readonly string[] LookRoots = [".moka-color-input.moka-color-input--look", ".moka-gate .moka-color-input.moka-color-input--look"];

	// The script marks the panel only where the browser has an EyeDropper, so the buttons never show where it can't.
	[Fact]
	public void TheEyedropper_ShowsOnlyWhereTheScriptFoundOne()
	{
		Assert.Equal("none", ScopedCss.Declarations(Picker, ".moka-colorpicker-eye-btn")["display"]);
		Assert.Equal("none", ScopedCss.Declarations(Picker, ".moka-colorpicker-eye-note")["display"]);
		Assert.Equal("none", ScopedCss.Declarations(Picker, ".moka-colorpicker-hexrow ::deep .moka-colorpicker-eye-tip")["display"]);

		Assert.Equal("flex", ScopedCss.Declarations(Picker, "[data-moka-eyedropper] .moka-colorpicker-eye-btn")["display"]);
		Assert.Equal("block", ScopedCss.Declarations(Picker, "[data-moka-eyedropper] .moka-colorpicker-eye-note")["display"]);
		Assert.Equal("inline-flex",
			ScopedCss.Declarations(Picker, "[data-moka-eyedropper] .moka-colorpicker-hexrow ::deep .moka-colorpicker-eye-tip")["display"]);
	}

	// On the original design the alpha bar keeps today's look, its gradient alone; the new design lays it over a
	// checkerboard drawn from the theme's colors.
	[Fact]
	public void TheAlphaChecker_IsDrawnFromTokens_OnlyOnTheNewDesign()
	{
		Assert.Equal("var(--_cp-alpha-fill)", ScopedCss.Declarations(Picker, ".moka-colorpicker-alpha-slider--checker")["background"]);

		string gate = ScopedCss.Declarations(Picker, ".moka-gate .moka-colorpicker-alpha-slider--checker")["background"];
		Assert.StartsWith("var(--_cp-alpha-fill)", gate, StringComparison.Ordinal);
		Assert.Contains("conic-gradient(var(--moka-color-outline-strong) 0 25%, var(--moka-color-surface) 0 50%", gate, StringComparison.Ordinal);
		Assert.DoesNotContain("#", gate, StringComparison.Ordinal);
	}

	[Fact]
	public void TheThumbs_FollowThePanelsProperties()
	{
		IReadOnlyDictionary<string, string> square = ScopedCss.Declarations(Picker, ".moka-colorpicker-square > .moka-colorpicker-thumb");
		Assert.Equal("calc(var(--_cp-sat) * 100%)", square["left"]);
		Assert.Equal("calc((1 - var(--_cp-val)) * 100%)", square["top"]);

		Assert.Equal("calc(var(--_cp-hue) / 360 * 100%)",
			ScopedCss.Declarations(Picker, ".moka-colorpicker-track--hue > .moka-colorpicker-thumb")["left"]);
		Assert.Equal("calc(var(--_cp-alpha) * 100%)",
			ScopedCss.Declarations(Picker, ".moka-colorpicker-track--alpha > .moka-colorpicker-thumb")["left"]);
		Assert.Equal("rotate(calc(var(--_cp-hue) * 1deg))", ScopedCss.Declarations(Picker, ".moka-colorpicker-ring-arm")["transform"]);
	}

	// Each slider's track shows where it goes from the color the panel holds now.
	[Fact]
	public void TheSliderTracks_ShowTheirGradients()
	{
		Assert.Contains("var(--_cp-spectrum)", ScopedCss.Declarations(Picker, ".moka-colorpicker-range--h")["--_cp-track"], StringComparison.Ordinal);
		Assert.Equal("linear-gradient(to right, hsl(var(--_cp-hue) 0% var(--_cp-ll)), hsl(var(--_cp-hue) 100% var(--_cp-ll)))",
			ScopedCss.Declarations(Picker, ".moka-colorpicker-range--s")["--_cp-track"]);
		Assert.Equal("linear-gradient(to right, black, hsl(var(--_cp-hue) var(--_cp-sl) 50%), white)",
			ScopedCss.Declarations(Picker, ".moka-colorpicker-range--l")["--_cp-track"]);
	}

	// The presets and the panel's rows are MokaColorSwatch radio groups now. They keep the old sizes and rings: the
	// palette fills the panel in eight columns, 6 px apart, on rounded squares, with the ring's gap in the panel's color
	// and the softer focus glow the fields use.
	[Fact]
	public void ThePickersRows_KeepThePanelsSizes()
	{
		IReadOnlyDictionary<string, string> row = ScopedCss.Declarations(Picker, ".moka-colorpicker-sec ::deep .moka-colorpicker-swatches");
		Assert.Equal("var(--_cp-gap, var(--moka-color-surface))", row["--moka-swatch-ring-gap"]);
		Assert.Equal("var(--moka-color-primary-glow-md)", row["--moka-swatch-halo"]);

		IReadOnlyDictionary<string, string> grid =
			ScopedCss.Declarations(Picker, ".moka-colorpicker-sec ::deep .moka-colorpicker-swatches--grid .moka-color-swatch-grid");
		Assert.Equal("grid", grid["display"]);
		Assert.Equal("repeat(8, minmax(0, 1fr))", grid["grid-template-columns"]);
		Assert.Equal("var(--moka-spacing-sm)",
			ScopedCss.Declarations(Picker, ".moka-colorpicker-sec ::deep .moka-colorpicker-swatches .moka-color-swatch-grid")["gap"]);
		Assert.Equal("var(--moka-radius-md)",
			ScopedCss.Declarations(Picker, ".moka-colorpicker-sec ::deep .moka-colorpicker-swatches .moka-color-swatch-item")["border-radius"]);
		Assert.Equal("1", ScopedCss.Declarations(Picker,
			".moka-colorpicker-sec ::deep .moka-colorpicker-swatches--grid .moka-color-swatch-item")["aspect-ratio"]);
	}

	[Fact]
	public void TheInputsPalette_WrapsWithTheField_AndKeepsItsGlow()
	{
		Assert.Equal("var(--moka-color-primary-glow-md)",
			ScopedCss.Declarations(Input, ".moka-color-input ::deep .moka-color-input__palette")["--moka-swatch-halo"]);

		IReadOnlyDictionary<string, string> grid =
			ScopedCss.Declarations(Input, ".moka-color-input ::deep .moka-color-input__palette .moka-color-swatch-grid");
		Assert.Equal("none", grid["max-width"]);
		Assert.Equal("var(--moka-control-height)", grid["min-height"]);

		// The field dims as a whole, so the presets don't dim twice.
		Assert.Equal("1", ScopedCss.Declarations(Input,
			".moka-color-input ::deep .moka-color-input__palette .moka-color-swatch-item--disabled")["opacity"]);
	}

	// The Standard look's box rules reach every look's root through .moka-color-input, so the looks take them back.
	[Fact]
	public void ALooksRoot_DrawsNoBox_OnEitherDesign()
	{
		foreach (string selector in LookRoots)
		{
			IReadOnlyDictionary<string, string> root = ScopedCss.Declarations(Input, selector);
			Assert.Equal("0", root["border"]);
			Assert.Equal("none", root["background"]);
			Assert.Equal("0", root["padding"]);
		}

		Assert.Equal("none", ScopedCss.Declarations(Input, ".moka-color-input.moka-color-input--look:focus-within")["box-shadow"]);
	}

	[Fact]
	public void Terminal_IsUppercaseMono_AndItsSquareGlowsInTheColor()
	{
		Assert.Equal("uppercase", ScopedCss.Declarations(Input, ".moka-color-input__text--terminal")["text-transform"]);

		IReadOnlyDictionary<string, string> square = ScopedCss.Declarations(Input, ".moka-color-input__term-square");
		Assert.Equal("var(--_ci-color, transparent)", square["background"]);
		Assert.Contains("color-mix(in srgb, var(--_ci-color, transparent) 55%, transparent)", square["box-shadow"], StringComparison.Ordinal);
		Assert.Equal("var(--moka-color-error)", ScopedCss.Declarations(Input, ".moka-color-input--error .moka-color-input__term-tag")["color"]);
	}

	[Fact]
	public void TheInputsPopup_ReadsThePopupLook_OnTheNewDesign()
	{
		IReadOnlyDictionary<string, string> popup = ScopedCss.Declarations(Input, ".moka-gate .moka-color-input__popup");

		Assert.Equal("var(--moka-popup-bg, var(--moka-color-surface))", popup["background"]);
		Assert.Equal("var(--moka-popup-radius, var(--moka-radius-popup))", popup["border-radius"]);
		Assert.Equal("var(--moka-popup-shadow, var(--moka-shadow-popup))", popup["box-shadow"]);
		Assert.Equal(popup["backdrop-filter"], popup["-webkit-backdrop-filter"]);
	}
}
