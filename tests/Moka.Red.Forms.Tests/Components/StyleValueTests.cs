using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.ColorInput;
using Moka.Red.Forms.ColorPicker;

namespace Moka.Red.Forms.Tests.Components;

// The colour inputs wrote their colours straight into style attributes, so a value such as
// "red; background-image: url(x)" added a declaration of its own. A colour now reaches a style only
// as a hex colour, through StyleBuilder.
public class StyleValueTests : BunitContext
{
	private const string Injected = "red; background-image: url(x)";

	private static readonly string[] Presets = [Injected, "#00ff00"];

	public StyleValueTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ColorInput_ShowsOnlyAHexColourInItsSwatch()
	{
		IRenderedComponent<MokaColorInput> cut = Render<MokaColorInput>(p => p.Add(x => x.Value, Injected));

		Assert.Equal("background-color: transparent", cut.Find(".moka-color-input__swatch").GetAttribute("style"));

		cut.Render(p => p.Add(x => x.Value, "#ef5350"));

		Assert.Equal("background-color: #ef5350", cut.Find(".moka-color-input__swatch").GetAttribute("style"));
	}

	[Fact]
	public void ColorPicker_ShowsOnlyAHexColourInItsPreview()
	{
		IRenderedComponent<MokaColorPicker> cut = Render<MokaColorPicker>(p => p.Add(x => x.Value, Injected));

		Assert.False(cut.Find(".moka-colorpicker-swatch-preview").HasAttribute("style"));

		cut.Render(p => p.Add(x => x.Value, "#ef5350"));

		Assert.Equal("background: #ef5350", cut.Find(".moka-colorpicker-swatch-preview").GetAttribute("style"));
	}

	[Fact]
	public async Task ColorPicker_ShowsOnlyHexPresets()
	{
		IRenderedComponent<MokaColorPicker> cut = Render<MokaColorPicker>(p => p.Add(x => x.Presets, Presets));
		await cut.Find("input[aria-haspopup]").ClickAsync(new MouseEventArgs());

		IReadOnlyList<IElement> presets = cut.FindAll(".moka-colorpicker-presets .moka-colorpicker-preset");
		Assert.False(presets[0].HasAttribute("style"));
		Assert.Equal("background: #00ff00", presets[1].GetAttribute("style"));
	}

	[Fact]
	public async Task ColorPicker_ShowsOnlyHexRecentColours()
	{
		IRenderedComponent<MokaColorPicker> cut = Render<MokaColorPicker>(p => p.Add(x => x.Value, Injected));

		// Closing the picker adds its value to the recent colours.
		await cut.Find("input[aria-haspopup]").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-colorpicker-backdrop").ClickAsync(new MouseEventArgs());
		await cut.Find("input[aria-haspopup]").ClickAsync(new MouseEventArgs());

		Assert.False(cut.Find(".moka-colorpicker-recent .moka-colorpicker-preset").HasAttribute("style"));
	}
}
