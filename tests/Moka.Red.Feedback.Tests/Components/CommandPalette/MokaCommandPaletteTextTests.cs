using Bunit;
using Moka.Red.Feedback.CommandPalette;

namespace Moka.Red.Feedback.Tests.Components.CommandPalette;

public class MokaCommandPaletteTextTests : CommandPaletteTestBase
{
	[Fact]
	public async Task TheFooterHints_SayWhatTextSays()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[Command("a", "A")],
			p => p.Add(x => x.Text, new MokaCommandPaletteText { Navigate = "Bewegen", Select = "Wählen", Close = "Schließen" }));

		Assert.Equal(
			["Bewegen", "Wählen", "Schließen"],
			cut.FindAll(".moka-command-palette__footer > span").Select(hint => hint.TextContent.Split(' ', 2)[1].Trim()));
	}

	[Fact]
	public async Task TheFooter_IsUnchangedByDefault()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Command("a", "A")]);

		cut.Find(".moka-command-palette__footer").MarkupMatches(
			"""
			<div class="moka-command-palette__footer">
				<span><kbd class="moka-kbd">↑↓</kbd> Navigate</span>
				<span><kbd class="moka-kbd">⏎</kbd> Select</span>
				<span><kbd class="moka-kbd">Esc</kbd> Close</span>
			</div>
			""");
	}

	[Fact]
	public async Task OpeningThePalette_WatchesTheHighlight()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Command("a", "A")]);

		cut.WaitForAssertion(() => Assert.Single(PaletteModule.Invocations["watchHighlight"]));
	}
}
