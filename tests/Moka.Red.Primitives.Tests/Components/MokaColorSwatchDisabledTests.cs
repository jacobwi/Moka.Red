using AngleSharp.Dom;
using Bunit;
using Moka.Red.Primitives.ColorSwatch;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaColorSwatchDisabledTests : BunitContext
{
	private static readonly string[] Colors = ["#111111", "#222222"];

	[Fact]
	public void WithoutColorDisabled_EverySwatchCanBePicked()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p.Add(x => x.Colors, Colors));

		Assert.All(cut.FindAll(".moka-color-swatch-item"), swatch => Assert.False(swatch.HasAttribute("disabled")));
	}

	[Fact]
	public void ADisabledColor_IsDrawnAndNamed_ButCantBePicked()
	{
		string? picked = null;
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Colors)
			.Add(x => x.ColorDisabled, color => color == "#222222")
			.Add(x => x.SelectedColorChanged, color => picked = color));

		IReadOnlyList<IElement> swatches = cut.FindAll(".moka-color-swatch-item");
		Assert.False(swatches[0].HasAttribute("disabled"));
		Assert.True(swatches[1].HasAttribute("disabled"));
		Assert.Equal("#222222", swatches[1].GetAttribute("aria-label"));

		swatches[0].Click();

		Assert.Equal("#111111", picked);
	}

	[Fact]
	public void TheChosenColor_CanBeDisabled_AndStaysChosen()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Colors)
			.Add(x => x.SelectedColor, "#111111")
			.Add(x => x.AllowClear, true)
			.Add(x => x.ColorDisabled, _ => true));

		IElement chosen = cut.FindAll(".moka-color-swatch-item")[0];
		Assert.True(chosen.HasAttribute("disabled"));
		Assert.Equal("true", chosen.GetAttribute("aria-pressed"));
	}
}
