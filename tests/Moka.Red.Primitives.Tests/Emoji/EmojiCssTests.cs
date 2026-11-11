using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Emoji;

// The panel's grid is drawn by its script, so its rules reach the cells with ::deep, and much of what the components
// do lives only in CSS. bUnit applies none, so these read the stylesheets as text.
public sealed class EmojiCssTests
{
	private const string Panel = "MokaEmojiPanel.razor.css";
	private const string Reactions = "MokaReactions.razor.css";
	private const string FlagFontFirst = "'Twemoji Country Flags'";

	[Fact]
	public void TheEmojiFont_StartsWithMokaFlagsFont_SoFlagsDrawOnWindows()
	{
		Assert.StartsWith(FlagFontFirst, ScopedCss.Declarations(Panel, ".moka-emoji-panel")["--_emoji"], StringComparison.Ordinal);
		Assert.StartsWith(FlagFontFirst, ScopedCss.Declarations(Reactions, ".moka-reactions")["--_emoji"], StringComparison.Ordinal);
		Assert.Equal("var(--_emoji)", ScopedCss.Declarations(Panel, ".moka-emoji-panel__scroll ::deep .moka-emoji-panel__cell")["font-family"]);
		Assert.Equal("var(--_emoji)", ScopedCss.Declarations(Panel, ".moka-emoji-panel__quick")["font-family"]);
	}

	// The columns share out what's left of the width and the scrollbar's room is kept on both edges, so the grid sits
	// with the same space on its left and right (it had 6 px on the left and about 27 on the right).
	[Fact]
	public void TheGrid_HasTheSameSpaceOnBothSides()
	{
		IReadOnlyDictionary<string, string> scroll = ScopedCss.Declarations(Panel, ".moka-emoji-panel__scroll");
		Assert.Equal("stable both-edges", scroll["scrollbar-gutter"]);
		Assert.Equal("center", ScopedCss.Declarations(Panel, ".moka-emoji-panel__scroll ::deep .moka-emoji-panel__row")["justify-items"]);
	}

	[Fact]
	public void TheScriptsCells_AreReachedThroughDeep()
	{
		Assert.Equal("repeat(var(--_cols, 8), minmax(var(--_cell), 1fr))",
			ScopedCss.Declarations(Panel, ".moka-emoji-panel__scroll ::deep .moka-emoji-panel__row")["grid-template-columns"]);
		Assert.Equal("sticky", ScopedCss.Declarations(Panel, ".moka-emoji-panel__scroll ::deep .moka-emoji-panel__head")["position"]);
		Assert.Equal("var(--_bg)", ScopedCss.Declarations(Panel, ".moka-emoji-panel__scroll ::deep .moka-emoji-panel__head")["background"]);
		Assert.Equal("inset 0 0 0 2px var(--moka-color-primary)",
			ScopedCss.Declarations(Panel, ".moka-emoji-panel__scroll[data-moka-keys] ::deep .is-lit")["box-shadow"]);
	}

	[Fact]
	public void APopup_ReadsThePopupLook_OnTheNewDesign()
	{
		IReadOnlyDictionary<string, string> popup = ScopedCss.Declarations(Panel, ".moka-gate .moka-emoji-panel--popup .moka-emoji-panel__body");

		Assert.Equal("var(--moka-popup-bg, var(--moka-color-surface))", popup["--_bg"]);
		Assert.Equal("var(--moka-popup-shadow, var(--moka-shadow-popup))", popup["box-shadow"]);
		Assert.Equal("var(--moka-popup-radius, var(--moka-radius-popup))", popup["border-radius"]);
	}

	[Fact]
	public void TheBackdrops_CoverTheWindow()
	{
		IReadOnlyDictionary<string, string> backdrop = ScopedCss.Declarations(Panel, ".moka-emoji-panel__backdrop");

		Assert.Equal("fixed", backdrop["position"]);
		Assert.Equal("0", backdrop["inset"]);
	}

	[Fact]
	public void ThePanel_FitsAPhone()
	{
		Assert.Equal("min(21.25rem, 100%)", ScopedCss.Declarations(Panel, ".moka-emoji-panel")["width"]);
		Assert.Equal("calc(100vw - 1rem)", ScopedCss.Declarations(Panel, ".moka-emoji-panel--popup")["max-width"]);
	}

	[Fact]
	public void TheReactionBar_IsHiddenUntilHoverOrFocus_AndCostsNoMotionWhenAskedNot()
	{
		IReadOnlyDictionary<string, string> bar = ScopedCss.Declarations(Reactions, ".moka-reactions__bar");
		Assert.Equal("0", bar["opacity"]);
		Assert.Equal("none", bar["pointer-events"]);
		Assert.Equal("absolute", bar["position"]);
		Assert.Null(bar.GetValueOrDefault("transform"));

		IReadOnlyDictionary<string, string> shown = ScopedCss.Declarations(Reactions, ".moka-reactions--message:focus-within > .moka-reactions__bar");
		Assert.Equal("1", shown["opacity"]);
		Assert.Equal("auto", shown["pointer-events"]);

		Assert.Equal("none", ScopedCss.Values(Reactions, ".moka-reactions__bar", "transition")[^1]);
	}

	[Fact]
	public void FocusRings_UseTheToken()
	{
		Assert.Equal("var(--moka-focus-ring)", ScopedCss.Declarations(Panel, ".moka-emoji-panel__tab:focus-visible")["box-shadow"]);
		Assert.Equal("var(--moka-focus-ring)", ScopedCss.Declarations(Reactions, ".moka-reactions__chip:focus-visible")["box-shadow"]);
	}
}
