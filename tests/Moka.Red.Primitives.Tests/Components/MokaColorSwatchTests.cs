using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.ColorSwatch;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaColorSwatchTests : BunitContext
{
	private const string Css = "MokaColorSwatch.razor.css";

	private static readonly string[] Palette = ["#ef5350", "#42a5f5", "#66bb6a"];

	private static IReadOnlyList<IElement> Radios(IRenderedComponent<MokaColorSwatch> cut) =>
		cut.FindAll("input.moka-color-swatch-input");

	private static string[] Checked(IRenderedComponent<MokaColorSwatch> cut) =>
		[.. Radios(cut).Where(radio => radio.HasAttribute("checked")).Select(radio => radio.GetAttribute("aria-label")!)];

	private static string? FocusedId(JSRuntimeInvocation focus) =>
		focus.Arguments[0] is ElementReference element ? element.Id : null;

	// A single choice is one tab stop with the arrow keys moving the choice: native radios sharing a name give
	// exactly that, where a row of toggle buttons was one tab stop per color.
	[Fact]
	public void Swatches_AreOneRadioGroup()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.SelectedColor, "#42a5f5"));

		Assert.Equal("radiogroup", cut.Find(".moka-color-swatch").GetAttribute("role"));
		IReadOnlyList<IElement> radios = Radios(cut);
		Assert.Equal(3, radios.Count);
		Assert.All(radios, radio => Assert.Equal("radio", radio.GetAttribute("type")));
		string? name = radios[0].GetAttribute("name");
		Assert.False(string.IsNullOrEmpty(name));
		Assert.All(radios, radio => Assert.Equal(name, radio.GetAttribute("name")));
		Assert.Equal(["#42a5f5"], Checked(cut));
		Assert.Empty(cut.FindAll("button.moka-color-swatch-item"));
	}

	[Fact]
	public void TwoSwatches_NeverShareAGroupName()
	{
		IRenderedComponent<MokaColorSwatch> first = Render<MokaColorSwatch>(p => p.Add(x => x.Colors, Palette));
		IRenderedComponent<MokaColorSwatch> second = Render<MokaColorSwatch>(p => p.Add(x => x.Colors, Palette));

		Assert.NotEqual(Radios(first)[0].GetAttribute("name"), Radios(second)[0].GetAttribute("name"));
	}

	[Fact]
	public void ARadio_PicksItsColor_AndTheCheckMovesWithIt()
	{
		List<string?> changes = [];
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.SelectedColor, "#ef5350")
			.Add(x => x.SelectedColorChanged, color => changes.Add(color)));

		Radios(cut)[2].Change(true);

		Assert.Equal(["#66bb6a"], changes);
		Assert.Equal(["#66bb6a"], Checked(cut));
		Assert.Single(cut.FindAll(".moka-color-swatch-check"));
		Assert.Contains("moka-color-swatch-item--selected", cut.FindAll(".moka-color-swatch-item")[2].ClassList);
	}
	// The same color twice (a palette color also in a Recent group) used to show two chosen swatches. The one picked
	// stays checked, and a color set from outside checks the first.
	[Fact]
	public void AColorDrawnTwice_IsCheckedOnce_WhereItWasPicked()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, ["#EF5350", "#42a5f5", "#ef5350"])
			.Add(x => x.SelectedColor, "#ef5350"));

		Assert.Equal([true, false, false], Radios(cut).Select(radio => radio.HasAttribute("checked")));

		Radios(cut)[2].Change(true);

		Assert.Equal([false, false, true], Radios(cut).Select(radio => radio.HasAttribute("checked")));
		Assert.Single(cut.FindAll(".moka-color-swatch-item--selected"));
	}

	[Fact]
	public void AllowClear_AClickOnTheCheckedRadioClearsIt_AndOnlyThatRadioListens()
	{
		List<string?> changes = [];
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.SelectedColor, "#ef5350")
			.Add(x => x.AllowClear, true)
			.Add(x => x.SelectedColorChanged, color => changes.Add(color)));

		Assert.Throws<MissingEventHandlerException>(() => Radios(cut)[1].Click());

		Radios(cut)[0].Click();

		Assert.Equal([null], changes);
		Assert.Empty(Checked(cut));
		Assert.Empty(cut.FindAll(".moka-color-swatch-preview"));
	}

	// Space on a checked radio fires no click in Chromium, so a keyboard had no way to clear. Delete and Backspace do
	// nothing on a radio of their own, so they clear without fighting the browser.
	[Theory]
	[InlineData("Delete")]
	[InlineData("Backspace")]
	public async Task AllowClear_DeleteOrBackspaceOnTheCheckedRadioClearsIt(string key)
	{
		List<string?> changes = [];
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.SelectedColor, "#42a5f5")
			.Add(x => x.AllowClear, true)
			.Add(x => x.SelectedColorChanged, color => changes.Add(color)));

		await Radios(cut)[1].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
		Assert.Empty(changes);
		Assert.Throws<MissingEventHandlerException>(() => Radios(cut)[0].KeyDown(new KeyboardEventArgs { Key = key }));

		await Radios(cut)[1].KeyDownAsync(new KeyboardEventArgs { Key = key });

		Assert.Equal([null], changes);
		Assert.Empty(Checked(cut));
	}

	[Fact]
	public void WithoutAllowClear_NoRadioListensForClicks()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.SelectedColor, "#ef5350"));

		Assert.Throws<MissingEventHandlerException>(() => Radios(cut)[0].Click());
		Assert.Equal(["#ef5350"], Checked(cut));
	}

	// Disabled came from the base class and did nothing: every swatch could still be picked.
	[Fact]
	public void Disabled_DisablesEverySwatchAndTheAddButton()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.SelectedColor, "#ef5350")
			.Add(x => x.AllowCustom, true)
			.Add(x => x.Disabled, true));

		Assert.All(Radios(cut), radio => Assert.True(radio.HasAttribute("disabled")));
		Assert.True(cut.Find(".moka-color-swatch-add").HasAttribute("disabled"));
		Assert.Equal("true", cut.Find(".moka-color-swatch").GetAttribute("aria-disabled"));
		Assert.All(cut.FindAll(".moka-color-swatch-item"), item => Assert.Contains("moka-color-swatch-item--disabled", item.ClassList));
	}

	[Fact]
	public void Disabled_IgnoresAChangeThatGetsThroughAnyway()
	{
		List<string?> changes = [];
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.Disabled, true)
			.Add(x => x.SelectedColorChanged, color => changes.Add(color)));

		Radios(cut)[1].Change(true);

		Assert.Empty(changes);
		Assert.Empty(Checked(cut));
	}

	[Fact]
	public void ADisabledColor_IsADisabledRadio_AndKeepsItsLook()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.ColorDisabled, color => color == "#42a5f5"));

		Assert.Equal([false, true, false], Radios(cut).Select(radio => radio.HasAttribute("disabled")));
		Assert.Equal([false, true, false],
			cut.FindAll(".moka-color-swatch-item").Select(item => item.ClassList.Contains("moka-color-swatch-item--disabled")));
		Assert.False(cut.Find(".moka-color-swatch").HasAttribute("aria-disabled"));
	}

	// Size and SizeValue came from the base class and did nothing.
	[Theory]
	[InlineData(MokaSize.Xs, "16px")]
	[InlineData(MokaSize.Sm, "20px")]
	[InlineData(MokaSize.Md, "24px")]
	[InlineData(MokaSize.Lg, "32px")]
	public void Size_SetsTheSwatchSize(MokaSize size, string expected)
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.Size, size));

		Assert.Contains($"--moka-swatch-size: {expected}", cut.Find(".moka-color-swatch").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void SizeValue_SetsTheSwatchSize_AndSwatchSizeWins()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.SizeValue, "30"));

		Assert.Contains("--moka-swatch-size: 30px", cut.Find(".moka-color-swatch").GetAttribute("style"), StringComparison.Ordinal);

		cut.Render(p => p.Add(x => x.SwatchSize, "2rem"));

		Assert.Contains("--moka-swatch-size: 2rem", cut.Find(".moka-color-swatch").GetAttribute("style"), StringComparison.Ordinal);
	}

	// A bare number is invalid CSS, which left the swatches at the stylesheet's size.
	[Fact]
	public void SwatchSize_ABareNumberIsPixels()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.SwatchSize, "28"));

		Assert.Contains("--moka-swatch-size: 28px", cut.Find(".moka-color-swatch").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void Columns_BelowOne_CountsAsOne()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.Columns, 0));

		Assert.Contains("--moka-swatch-columns: 1", cut.Find(".moka-color-swatch").GetAttribute("style"), StringComparison.Ordinal);
	}

	// A fixed number of columns pushed the swatches out of a narrow container (MokaAppearancePanel turned the grid into
	// a flex row to stop it). Columns is now the most per row, and a narrow container wraps them.
	[Fact]
	public void TheGrid_WrapsInANarrowContainer_UpToColumnsPerRow()
	{
		IReadOnlyDictionary<string, string> grid = ScopedCss.Declarations(Css, ".moka-color-swatch-grid");

		Assert.Equal("repeat(auto-fill, var(--moka-swatch-size, 24px))", grid["grid-template-columns"]);
		Assert.Equal("calc(var(--moka-swatch-columns, 8) * (var(--moka-swatch-size, 24px) + var(--moka-spacing-xs)))", grid["max-width"]);
		Assert.Equal("var(--moka-spacing-xs)", grid["gap"]);
	}

	// White under difference blending turned grey on a grey swatch (#808080 drew a #7f7f7f check). Black or white,
	// whichever the color's CIE lightness says reads better, crossing over where the two have equal WCAG contrast.
	[Fact]
	public void TheCheck_IsBlackOrWhiteByTheColorsLightness()
	{
		IReadOnlyList<string> colors = ScopedCss.Values(Css, ".moka-color-swatch ::deep .moka-color-swatch-check", "color");
		Assert.Equal("white", colors[0]);
		Assert.Equal("lab(from var(--_swatch-color) clamp(0, (49.4 - l) * 1000, 100) 0 0)", colors[^1]);
		Assert.Equal("normal",
			ScopedCss.Values(Css, ".moka-color-swatch ::deep .moka-color-swatch-check", "mix-blend-mode")[^1]);
	}

	[Fact]
	public void ASwatch_GivesItsColorToTheCheck_ThroughAProperty()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.SelectedColor, "#ef5350"));

		Assert.Equal("--_swatch-color: #ef5350", cut.FindAll(".moka-color-swatch-item")[0].GetAttribute("style"));
		Assert.Equal("var(--_swatch-color, transparent)", ScopedCss.Declarations(Css, ".moka-color-swatch-item")["background-color"]);
	}

	// A white swatch on a white card, or a black one on the Matrix page, had no edge at all.
	[Fact]
	public void ASwatch_KeepsAFaintEdge_SoAColorLikeThePageStillShows()
	{
		Assert.Equal("inset 0 0 0 1px color-mix(in srgb, var(--moka-color-on-surface) 14%, transparent)",
			ScopedCss.Declarations(Css, ".moka-color-swatch-item")["box-shadow"]);
	}

	[Fact]
	public void KeyboardFocus_DrawsTheRingOnTheSwatch()
	{
		IReadOnlyDictionary<string, string> ring =
			ScopedCss.Declarations(Css, ".moka-color-swatch-item:has(.moka-color-swatch-input:focus-visible)");

		Assert.Equal("var(--moka-focus-width) solid var(--moka-focus-color)", ring["outline"]);
		Assert.Equal("var(--moka-focus-width)", ring["outline-offset"]);
	}

	// The + button was replaced by the input, so focus fell to the page, and back again when it closed.
	[Fact]
	public async Task CustomInput_TakesFocus_AndGivesItBackToTheAddButton()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.AllowCustom, true));

		await cut.Find(".moka-color-swatch-add").ClickAsync(new MouseEventArgs());

		string? input = cut.Find(".moka-color-swatch-custom input[type='color']").GetAttribute("blazor:elementReference");
		Assert.False(string.IsNullOrEmpty(input));
		Assert.Equal(input, FocusedId(JSInterop.VerifyFocusAsyncInvoke()));

		await cut.Find(".moka-color-swatch-custom-cancel").ClickAsync(new MouseEventArgs());

		string? back = FocusedId(JSInterop.VerifyFocusAsyncInvoke(2)[1]);
		Assert.False(string.IsNullOrEmpty(back));
		Assert.NotEqual(input, back);
		Assert.Single(cut.FindAll(".moka-color-swatch-add"));
	}

	// The color input takes only #rrggbb. Anything else showed black, and Use this color then picked the old value.
	[Theory]
	[InlineData("#EF5350", "#ef5350")]
	[InlineData("#abc", "#aabbcc")]
	[InlineData("#ef5350cc", "#ef5350")]
	[InlineData("rgb(10 20 30)", "#000000")]
	[InlineData("var(--moka-color-primary)", "#000000")]
	public async Task CustomInput_StartsFromTheChosenColor_AsTheInputTakesIt(string chosen, string expected)
	{
		List<string?> changes = [];
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.SelectedColor, chosen)
			.Add(x => x.AllowCustom, true)
			.Add(x => x.SelectedColorChanged, color => changes.Add(color)));

		await cut.Find(".moka-color-swatch-add").ClickAsync(new MouseEventArgs());

		Assert.Equal(expected, cut.Find(".moka-color-swatch-custom input[type='color']").GetAttribute("value"));

		await cut.Find(".moka-color-swatch-custom-ok").ClickAsync(new MouseEventArgs());

		Assert.Equal([expected], changes);
	}

	// A custom pick had no swatch, so nothing in the group showed what was chosen.
	[Fact]
	public async Task ACustomColor_GetsASwatchOfItsOwn_Checked()
	{
		List<string?> changes = [];
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.AllowCustom, true)
			.Add(x => x.SelectedColorChanged, color => changes.Add(color)));

		await cut.Find(".moka-color-swatch-add").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-color-swatch-custom input[type='color']").InputAsync(new ChangeEventArgs { Value = "#123456" });
		await cut.Find(".moka-color-swatch-custom-ok").ClickAsync(new MouseEventArgs());

		Assert.Equal(["#123456"], changes);
		Assert.Equal(4, Radios(cut).Count);
		Assert.Equal(["#123456"], Checked(cut));
		Assert.Contains("moka-color-swatch-item--custom", cut.FindAll(".moka-color-swatch-item")[3].ClassList);

		await Radios(cut)[0].ChangeAsync(new ChangeEventArgs { Value = true });

		Assert.Equal(4, Radios(cut).Count);
		Assert.Equal(["#ef5350"], Checked(cut));
	}

	[Fact]
	public async Task ACustomColorAlreadyInThePalette_PicksThatSwatch()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.AllowCustom, true));

		await cut.Find(".moka-color-swatch-add").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-color-swatch-custom input[type='color']").InputAsync(new ChangeEventArgs { Value = "#42A5F5" });
		await cut.Find(".moka-color-swatch-custom-ok").ClickAsync(new MouseEventArgs());

		Assert.Equal(3, Radios(cut).Count);
		Assert.Equal(["#42a5f5"], Checked(cut));
	}

	[Fact]
	public void AChosenColorOutsideThePalette_GetsACustomSwatch_WithAllowCustom()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.SelectedColor, "#123456")
			.Add(x => x.AllowCustom, true));

		Assert.Equal(["#123456"], Checked(cut));
	}

	[Fact]
	public void AChosenColorOutsideThePalette_WithoutAllowCustom_ChecksNothing()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.SelectedColor, "#123456"));

		Assert.Equal(3, Radios(cut).Count);
		Assert.Empty(Checked(cut));
		Assert.Equal("#123456", cut.Find(".moka-color-swatch-preview-text").TextContent);
	}

	[Fact]
	public async Task Escape_InTheCustomRow_Cancels()
	{
		List<string?> changes = [];
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, Palette)
			.Add(x => x.AllowCustom, true)
			.Add(x => x.SelectedColorChanged, color => changes.Add(color)));

		await cut.Find(".moka-color-swatch-add").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-color-swatch-custom").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(cut.FindAll(".moka-color-swatch-custom"));
		Assert.Single(cut.FindAll(".moka-color-swatch-add"));
		Assert.Empty(changes);
	}
}
