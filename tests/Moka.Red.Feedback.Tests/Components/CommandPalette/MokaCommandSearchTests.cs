using Moka.Red.Feedback.CommandPalette;

namespace Moka.Red.Feedback.Tests.Components.CommandPalette;

public class MokaCommandSearchTests
{
	private static int Score(string text, string typed) => MokaCommandSearch.Score(text, MokaCommandSearch.FuzzyQuery(typed));

	[Fact]
	public void LettersOutOfOrder_DontMatch() =>
		Assert.Null(MokaCommandSearch.Fuzzy(new MokaCommand { Id = "a", Title = "Settings" }, MokaCommandSearch.FuzzyQuery("tes")));

	[Fact]
	public void LettersInARow_BeatTheSameLettersApart() =>
		Assert.True(Score("Save file", "sav") > Score("Show a view", "sav"));

	[Fact]
	public void ALetterStartingAWord_BeatsOneInsideAWord() =>
		Assert.True(Score("Open tab", "t") > Score("Settings", "t"));

	[Fact]
	public void ACapitalInsideAName_StartsAWord() =>
		Assert.True(Score("openTab", "t") > Score("opentab", "t"));

	[Fact]
	public void TheBestPlacementWins_NotTheFirst() =>
		Assert.True(Score("a b abc", "abc") > Score("a b axbxc", "abc"));

	[Fact]
	public void CaseDoesntMatter() =>
		Assert.Equal(Score("Save", "sa"), Score("save", "SA"));

	[Fact]
	public void ATitleMatchCountsDouble()
	{
		var titled = new MokaCommand { Id = "a", Title = "Theme" };
		var described = new MokaCommand { Id = "b", Title = "Open", Description = "Theme" };
		string query = MokaCommandSearch.FuzzyQuery("theme");

		Assert.Equal(MokaCommandSearch.Fuzzy(described, query) * 2, MokaCommandSearch.Fuzzy(titled, query));
	}

	[Fact]
	public void LongTextIsScored()
	{
		string text = new string('x', 500) + " Save";

		Assert.True(Score(text, "save") > 0);
	}
}
