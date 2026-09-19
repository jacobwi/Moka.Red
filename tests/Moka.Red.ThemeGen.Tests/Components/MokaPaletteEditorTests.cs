using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.ThemeGen.Editors;

namespace Moka.Red.ThemeGen.Tests.Components;

// The palette editor cut every '#' value to 7 characters for the native color input, so a hex value
// shorter than #rrggbb other than #rgb ("#12345", "#abcd") threw during the render that followed the
// edit, and any other text went into the palette unchecked.
public class MokaPaletteEditorTests : BunitContext
{
	[Theory]
	[InlineData("#12345")]
	[InlineData("#abcde")]
	[InlineData("#1234567")]
	[InlineData("#ggg")]
	[InlineData("12345")]
	[InlineData("red")]
	[InlineData("")]
	public async Task RejectsTextThatIsNotAHexColor_AndKeepsThePreviousColor(string typed)
	{
		MokaTheme? reported = null;
		IRenderedComponent<MokaThemeEditor> cut = Render<MokaThemeEditor>(p => p
			.Add(x => x.Theme, MokaTheme.Light)
			.Add(x => x.ThemeChanged, EventCallback.Factory.Create<MokaTheme>(this, t => reported = t)));

		await PrimaryHexInput(cut).ChangeAsync(new ChangeEventArgs { Value = typed });

		Assert.Null(reported);
		Assert.Equal(MokaPalette.Light.Primary, PrimaryHexInput(cut).GetAttribute("value"));
	}

	[Theory]
	[InlineData("#abc", "#abc", "#aabbcc")]
	[InlineData("#abcd", "#abcd", "#aabbcc")]
	[InlineData("#a1b2c3", "#a1b2c3", "#a1b2c3")]
	[InlineData("#a1b2c3d4", "#a1b2c3d4", "#a1b2c3")]
	[InlineData(" #A1B2C3 ", "#A1B2C3", "#a1b2c3")]
	public async Task AcceptsTheFourHexForms(string typed, string stored, string pickerValue)
	{
		MokaTheme? reported = null;
		IRenderedComponent<MokaThemeEditor> cut = Render<MokaThemeEditor>(p => p
			.Add(x => x.Theme, MokaTheme.Light)
			.Add(x => x.ThemeChanged, EventCallback.Factory.Create<MokaTheme>(this, t => reported = t)));

		await PrimaryHexInput(cut).ChangeAsync(new ChangeEventArgs { Value = typed });

		Assert.NotNull(reported);
		Assert.Equal(stored, reported.Palette.Primary);
		Assert.Equal(pickerValue, cut.FindAll(".moka-palette-editor__color-input")[0].GetAttribute("value"));
	}

	[Theory]
	[InlineData("#12345", "#000000")]
	[InlineData("#abcd", "#aabbcc")]
	[InlineData("rgba(239, 83, 80, 0.12)", "#ef5350")]
	[InlineData("hsl(0 0% 0%)", "#000000")]
	public void RendersAPaletteColorThePickerCannotShow_WithoutThrowing(string color, string pickerValue)
	{
		// A palette from code or an import can hold any string.
		IRenderedComponent<MokaPaletteEditor> cut = Render<MokaPaletteEditor>(p => p
			.Add(x => x.Palette, MokaPalette.Light with { Primary = color }));

		Assert.Equal(pickerValue, cut.FindAll(".moka-palette-editor__color-input")[0].GetAttribute("value"));
		Assert.Equal(color, PrimaryHexInput(cut).GetAttribute("value"));
	}

	// The first row is Primary.
	private static IElement PrimaryHexInput<TComponent>(IRenderedComponent<TComponent> cut)
		where TComponent : IComponent =>
		cut.FindAll(".moka-palette-editor__hex-input")[0];
}
