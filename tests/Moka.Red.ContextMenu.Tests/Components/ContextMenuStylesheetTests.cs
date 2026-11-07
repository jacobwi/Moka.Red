using Moka.Red.Tests.Shared;

namespace Moka.Red.ContextMenu.Tests.Components;

// bUnit applies no CSS, so the rules that draw the highlight are read as text.
public class ContextMenuStylesheetTests
{
	private const string Menu = "MokaContextMenu.razor.css";
	private const string Radial = "MokaRadialMenu.razor.css";

	private const string GateHovered = ".moka-gate .moka-ctx-menu[data-moka-pointer] .moka-ctx-item:hover:not(.moka-ctx-item--disabled)";
	private const string GateFocused = ".moka-gate .moka-ctx-menu:not([data-moka-pointer]) .moka-ctx-item--focused:not(.moka-ctx-item--disabled)";

	[Theory]
	[InlineData(GateHovered)]
	[InlineData(GateFocused)]
	public void OnTheNewDesign_TheLitRow_IsTheAccentRow(string selector)
	{
		IReadOnlyDictionary<string, string> row = ScopedCss.Declarations(Menu, selector);

		Assert.Equal("var(--moka-color-primary-row)", row["background"]);
		Assert.Equal("var(--moka-selected-glow)", row["box-shadow"]);
	}

	[Theory]
	[InlineData(GateHovered + " ::deep .moka-ctx-icon")]
	[InlineData(GateFocused + " ::deep .moka-ctx-icon")]
	[InlineData(GateHovered + " .moka-ctx-submenu-arrow")]
	[InlineData(GateFocused + " .moka-ctx-submenu-arrow")]
	public void OnTheNewDesign_TheLitRowsIconAndArrow_TakeTheAccent(string selector) =>
		Assert.Equal("var(--moka-color-primary-light)", ScopedCss.Declarations(Menu, selector)["color"]);

	[Fact]
	public void TheOriginalDesign_KeepsItsTwoMixes_OneHighlightAtATime()
	{
		Assert.Equal("color-mix(in srgb, var(--moka-color-primary) 8%, transparent)",
			ScopedCss.Declarations(Menu, ".moka-ctx-menu[data-moka-pointer] .moka-ctx-item:hover:not(.moka-ctx-item--disabled)")["background"]);
		Assert.Equal("color-mix(in srgb, var(--moka-color-primary) 12%, transparent)",
			ScopedCss.Declarations(Menu, ".moka-ctx-menu:not([data-moka-pointer]) .moka-ctx-item--focused:not(.moka-ctx-item--disabled)")["background"]);
	}

	// A hover rule that ignores data-moka-pointer lights a second row beside the keyboard's.
	[Theory]
	[InlineData(".moka-ctx-item:hover:not(.moka-ctx-item--disabled)")]
	[InlineData(".moka-ctx-item--focused:not(.moka-ctx-item--disabled)")]
	[InlineData(".moka-gate .moka-ctx-item:hover:not(.moka-ctx-item--disabled)")]
	[InlineData(".moka-gate .moka-ctx-item--focused:not(.moka-ctx-item--disabled)")]
	public void NoRowRule_IgnoresWhoLeads(string selector) => Assert.Empty(ScopedCss.Declarations(Menu, selector));

	[Fact]
	public void TheCheckMark_StaysTheAccent() =>
		Assert.Equal("var(--moka-color-primary)", ScopedCss.Declarations(Menu, ".moka-ctx-check")["color"]);

	// BC's pick A: the lit button is the accent, though on Red a normal and a dangerous one then look alike.
	[Fact]
	public void TheLitRadialButton_IsTheAccent()
	{
		IReadOnlyDictionary<string, string> lit = ScopedCss.Declarations(Radial,
			".moka-radial-menu:not([data-moka-pointer]) > .moka-radial-menu__slot--active:not(.moka-radial-menu__slot--disabled) > .moka-radial-menu__button");

		Assert.Equal("var(--moka-color-primary)", lit["--_lit"]);
		Assert.Equal("var(--moka-color-on-primary)", lit["--_lit-ink"]);
		Assert.Equal("var(--_lit)", lit["background"]);
	}

	[Theory]
	[InlineData("danger", "error")]
	[InlineData("good", "success")]
	public void ALitDangerOrGoodButton_KeepsItsTone(string tone, string color)
	{
		IReadOnlyDictionary<string, string> lit = ScopedCss.Declarations(Radial,
			$".moka-radial-menu[data-moka-pointer] > .moka-radial-menu__slot--{tone}[data-moka-pointed]:not(.moka-radial-menu__slot--disabled) > .moka-radial-menu__button");

		Assert.Equal($"var(--moka-color-{color})", lit["--_lit"]);
		Assert.Equal($"var(--moka-color-on-{color})", lit["--_lit-ink"]);
	}

	[Theory]
	[InlineData(".moka-radial-menu__track")]
	[InlineData(".moka-radial-menu__button")]
	[InlineData(".moka-radial-menu__hub")]
	public void ReducedMotion_DropsTheOpening(string selector) =>
		Assert.Equal("none", ScopedCss.Values(Radial, selector, "animation")[^1]);

	[Fact]
	public void TheButtons_OpenOneAfterAnother()
	{
		string animation = ScopedCss.Values(Radial, ".moka-radial-menu__button", "animation")[0];

		Assert.Contains("var(--moka-radial-delay)", animation, StringComparison.Ordinal);
		Assert.Contains("cubic-bezier(0.2, 1.4, 0.4, 1)", animation, StringComparison.Ordinal);
	}
}
