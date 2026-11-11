using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.SlashMenu;
using Moka.Red.Icons;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

// MokaSlashMenuItem.Glyph: an emoji where the icon goes, for the colon typeahead and any app's own list.
public sealed class MokaSlashMenuGlyphTests : BunitContext
{
	private static readonly IReadOnlyList<MokaSlashMenuItem> Items =
	[
		new() { Title = ":tada:", Description = "party popper", Glyph = "\U0001F389", Icon = MokaIcons.Action.Add },
		new() { Title = "Heading", Icon = MokaIcons.Content.Heading }
	];

	public MokaSlashMenuGlyphTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void OnTheOriginalLook_TheGlyphTakesTheIconsPlace()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));
		IReadOnlyList<IElement> rows = cut.FindAll("[role=option]");

		IElement glyph = rows[0].QuerySelector(".moka-slash-menu-glyph")!;
		Assert.Equal("\U0001F389", glyph.TextContent);
		Assert.Equal("true", glyph.GetAttribute("aria-hidden"));
		Assert.Empty(rows[0].QuerySelectorAll("svg"));
		Assert.Null(rows[1].QuerySelector(".moka-slash-menu-glyph"));
		Assert.NotNull(rows[1].QuerySelector("svg"));
	}

	[Fact]
	public void OnTheNewDesign_TheGlyphSitsInTheTile()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.AddCascadingValue(MokaTheme.Red)
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));
		IReadOnlyList<IElement> rows = cut.FindAll("[role=option]");

		IElement tile = rows[0].QuerySelector(".moka-slash-menu-tile")!;
		Assert.Equal("true", tile.GetAttribute("aria-hidden"));
		Assert.Equal("\U0001F389", tile.QuerySelector(".moka-slash-menu-glyph")!.TextContent);
		Assert.Empty(tile.QuerySelectorAll("svg"));
		Assert.NotNull(rows[1].QuerySelector(".moka-slash-menu-tile svg"));
	}

	[Fact]
	public void TheGlyph_IsNotMatchedByTheQuery()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.Query, "\U0001F389"));

		Assert.Empty(cut.FindAll("[role=option]"));
	}

	[Fact]
	public void TheGlyph_DrawsInMokaFlagsFontFirst()
	{
		Assert.StartsWith("'Twemoji Country Flags'", ScopedCss.Declarations("MokaSlashMenu.razor.css", ".moka-slash-menu-glyph")["font-family"],
			StringComparison.Ordinal);
	}
}
