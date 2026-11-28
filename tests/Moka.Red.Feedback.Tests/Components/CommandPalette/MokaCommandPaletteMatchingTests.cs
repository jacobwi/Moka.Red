using Bunit;
using Moka.Red.Feedback.CommandPalette;

namespace Moka.Red.Feedback.Tests.Components.CommandPalette;

public class MokaCommandPaletteMatchingTests : CommandPaletteTestBase
{
	[Fact]
	public async Task Contains_IsTheDefault_AndNeedsTheTextInOnePiece()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Command("open", "Open settings"), Command("close", "Close tab")]);

		await Type(cut, "opst");

		Assert.Empty(Titles(cut));
	}

	[Fact]
	public async Task Fuzzy_FindsTheLettersInOrder_WithGapsBetween()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[Command("open", "Open settings"), Command("close", "Close tab")],
			p => p.Add(x => x.Matching, MokaCommandMatching.Fuzzy));

		await Type(cut, "opst");

		Assert.Equal(["Open settings"], Titles(cut));
	}

	[Fact]
	public async Task Fuzzy_IgnoresSpacesInWhatIsTyped()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[Command("home", "Go to home")],
			p => p.Add(x => x.Matching, MokaCommandMatching.Fuzzy));

		await Type(cut, "go home");

		Assert.Equal(["Go to home"], Titles(cut));
	}

	[Fact]
	public async Task Fuzzy_PutsTheBetterMatchFirst()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[Command("sidebar", "Toggle sidebar"), Command("save", "Save")],
			p => p.Add(x => x.Matching, MokaCommandMatching.Fuzzy).Add(x => x.ShowGroups, false));

		await Type(cut, "sa");

		Assert.Equal(["Save", "Toggle sidebar"], Titles(cut));
		Assert.Equal("Save", Highlighted(cut));
	}

	[Fact]
	public async Task Fuzzy_RanksATitleMatchOverADescriptionMatch()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[
				new MokaCommand { Id = "open", Title = "Open", Description = "Theme settings", OnExecuteSync = () => { } },
				Command("theme", "Theme")
			],
			p => p.Add(x => x.Matching, MokaCommandMatching.Fuzzy));

		await Type(cut, "theme");

		Assert.Equal(["Theme", "Open"], Titles(cut));
	}

	[Fact]
	public async Task Filter_WinsOverMatching()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[Command("a", "Alpha"), Command("b", "Beta")],
			p => p
				.Add(x => x.Matching, MokaCommandMatching.Fuzzy)
				.Add(x => x.Filter, (command, _) => command.Id == "b" ? 1 : null));

		await Type(cut, "alpha");

		Assert.Equal(["Beta"], Titles(cut));
	}

	[Fact]
	public async Task Filter_PutsAHigherScoreFirst_AndKeepsRegistrationOrderForTies()
	{
		Dictionary<string, int> scores = new() { ["a"] = 1, ["b"] = 5, ["c"] = 1 };
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[Command("a", "A"), Command("b", "B"), Command("c", "C")],
			p => p.Add(x => x.Filter, (command, _) => scores[command.Id]).Add(x => x.ShowGroups, false));

		await Type(cut, "x");

		Assert.Equal(["B", "A", "C"], Titles(cut));
	}

	[Fact]
	public async Task Filter_IsNotAsked_WhileTheBoxIsEmpty()
	{
		int asked = 0;
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[Command("a", "A"), Command("b", "B")],
			p => p.Add(x => x.Filter, (_, _) =>
			{
				asked++;
				return null;
			}));

		Assert.Equal(["A", "B"], Titles(cut));
		Assert.Equal(0, asked);
	}

	[Fact]
	public async Task RankedResults_ListGroupsInTheOrderOfTheirBestResult()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[Command("weak", "Toggle sidebar", "Alpha"), Command("strong", "Save", "Zulu")],
			p => p.Add(x => x.Matching, MokaCommandMatching.Fuzzy));

		await Type(cut, "sa");

		Assert.Equal(["Zulu", "Alpha"], Headings(cut));
		Assert.Equal(["Save", "Toggle sidebar"], Titles(cut));
	}

	[Fact]
	public async Task RankedResults_KeepCommandsWithoutAGroupFirst()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[Command("weak", "Toggle sidebar"), Command("strong", "Save", "Files")],
			p => p.Add(x => x.Matching, MokaCommandMatching.Fuzzy));

		await Type(cut, "sa");

		Assert.Equal(["Toggle sidebar", "Save"], Titles(cut));
	}

	[Fact]
	public async Task Fuzzy_WithNothingTyped_ListsEverythingAsContainsDoes()
	{
		MokaCommand[] commands = [Command("home", "Home", "Go"), Command("save", "Save", "Edit")];
		IRenderedComponent<MokaCommandPalette> fuzzy = await Open(commands, p => p.Add(x => x.Matching, MokaCommandMatching.Fuzzy));

		Assert.Equal(["Edit", "Go"], Headings(fuzzy));
		Assert.Equal(["Save", "Home"], Titles(fuzzy));
	}

	[Fact]
	public async Task Fuzzy_KeepsMaxResults()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			Enumerable.Range(0, 10).Select(i => Command($"c{i}", $"Command {i}")),
			p => p.Add(x => x.Matching, MokaCommandMatching.Fuzzy).Add(x => x.MaxResults, 3));

		await Type(cut, "cmd");

		Assert.Equal(3, Titles(cut).Count);
	}
}
