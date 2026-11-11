using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Tests.Utilities;

/// <summary>
///     The fold is the library's own table, so it gives the same answer under WebAssembly's invariant globalization,
///     where an accent-blind compare would need ICU.
/// </summary>
public class MokaTextFoldTests
{
	[Theory]
	[InlineData("Côte d’Ivoire", "cote divoire")]
	[InlineData("Côte d'Ivoire", "cote divoire")]
	[InlineData("Åland Islands", "aland islands")]
	[InlineData("Việt Nam", "viet nam")]
	[InlineData("São Tomé & Príncipe", "sao tome & principe")]
	[InlineData("Straße", "strasse")]
	[InlineData("Færøerne", "faeroerne")]
	[InlineData("ŁÓDŹ", "lodz")]
	[InlineData("Türkiye", "turkiye")]
	[InlineData("Oʻzbekiston", "ozbekiston")]
	[InlineData("Ísland", "island")]
	[InlineData("Curaçao", "curacao")]
	[InlineData("Œuvre", "oeuvre")]
	[InlineData("Þingvellir", "thingvellir")]
	[InlineData("Kıbrıs", "kibris")]
	public void Fold_SetsAccentsCaseAndApostrophesAside(string text, string folded) =>
		Assert.Equal(folded, MokaTextFold.Fold(text));

	[Fact]
	public void Fold_LeavesOtherScriptsAsTheyAre_InLowerCase()
	{
		Assert.Equal("日本", MokaTextFold.Fold("日本"));
		Assert.Equal("ελλάδα", MokaTextFold.Fold("ΕΛΛΆΔΑ"));
		Assert.Equal("россия", MokaTextFold.Fold("РОССИЯ"));
	}

	[Fact]
	public void Fold_OfNullOrEmpty_IsEmpty()
	{
		Assert.Equal(string.Empty, MokaTextFold.Fold(null));
		Assert.Equal(string.Empty, MokaTextFold.Fold(string.Empty));
	}

	[Fact]
	public void Fold_DropsCombiningMarks()
	{
		Assert.Equal("cote", MokaTextFold.Fold("Côte"));
	}

	[Fact]
	public void ContainsAndStartsWith_FoldBothSides()
	{
		Assert.True(MokaTextFold.Contains("Côte d’Ivoire", "COTE D'IV"));
		Assert.True(MokaTextFold.StartsWith("Åland Islands", "ala"));
		Assert.False(MokaTextFold.StartsWith("Åland Islands", "isl"));
		Assert.True(MokaTextFold.Contains("Anything", null));
	}
}
