using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.ThemeGen.Editors;

namespace Moka.Red.ThemeGen.Tests.Components;

// The editors' previews wrote the theme's values into hand-built style attributes. The spacing and
// typography fields take any text, and an imported theme can hold anything, so a semicolon in a value
// added declarations to the editor's own markup.
public class EditorPreviewStyleTests : BunitContext
{
	private const string Hostile = "8px; position: fixed; inset: 0; z-index: 9999";

	[Fact]
	public void PaletteEditor_DrawsASwatchThatIsNotAColorEmpty()
	{
		IRenderedComponent<MokaPaletteEditor> cut = Render<MokaPaletteEditor>(p => p
			.Add(x => x.Palette, MokaPalette.Light with { Primary = Hostile }));

		IReadOnlyList<IElement> swatches = cut.FindAll(".moka-palette-editor__swatch");
		Assert.Null(swatches[0].GetAttribute("style"));
		Assert.Equal($"background-color: {MokaPalette.Light.PrimaryLight}", swatches[1].GetAttribute("style"));
	}

	[Fact]
	public void SpacingEditor_KeepsAValueInItsDeclaration()
	{
		IRenderedComponent<MokaSpacingEditor> cut = Render<MokaSpacingEditor>(p => p
			.Add(x => x.Spacing, MokaSpacing.Default with { Xxs = Hostile, RadiusNone = Hostile }));

		Assert.Null(cut.FindAll(".moka-spacing-editor__bar")[0].GetAttribute("style"));
		Assert.Equal($"width: {MokaSpacing.Default.Xs}", cut.FindAll(".moka-spacing-editor__bar")[1].GetAttribute("style"));
		Assert.Null(cut.FindAll(".moka-spacing-editor__radius-preview")[0].GetAttribute("style"));
	}

	[Fact]
	public void TypographyEditor_KeepsAFontSizeInItsDeclaration()
	{
		IRenderedComponent<MokaTypographyEditor> cut = Render<MokaTypographyEditor>(p => p
			.Add(x => x.Typography, MokaTypography.Default with { FontSizeXs = Hostile }));

		IReadOnlyList<IElement> previews = cut.FindAll(".moka-typography-editor__preview");
		Assert.Null(previews[0].GetAttribute("style"));
		Assert.Equal($"font-size: {MokaTypography.Default.FontSizeSm}", previews[1].GetAttribute("style"));
	}
}
