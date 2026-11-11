using System.Text.RegularExpressions;
using Moka.Red.Primitives.Flag;

namespace Moka.Red.Primitives.Tests.Components;

/// <summary>
///     The country table is built into the assembly. These hold it to MokaFlag's regions and to the shape the country
///     select and the phone picker rely on, and check search the way people type.
/// </summary>
public sealed partial class MokaCountriesTests
{
	// MokaFlag draws these, and the table leaves them out on purpose: two organizations and the exceptional
	// reservations, whose places are listed under the countries they belong to.
	private static readonly string[] LeftOut = ["AC", "CP", "DG", "EA", "EU", "IC", "TA", "UN"];

	private static readonly string[] Nordic = ["SE", "NO", "DK", "FI", "IS"];

	[Fact]
	public void EveryRegionMokaFlagDraws_IsListed_ButTheOnesLeftOutOnPurpose()
	{
		List<string> drawn = [];
		for (char first = 'A'; first <= 'Z'; first++)
		{
			for (char second = 'A'; second <= 'Z'; second++)
			{
				string code = $"{first}{second}";
				if (MokaFlag.ToEmoji(code) is not null)
				{
					drawn.Add(code);
				}
			}
		}

		Assert.Equal(258, drawn.Count);
		string[] listed = [.. MokaCountries.All.Select(c => c.Code).Order(StringComparer.Ordinal)];
		Assert.Equal(drawn.Except(LeftOut).Order(StringComparer.Ordinal), listed);
	}

	[Fact]
	public void TheTable_HasTheIsoCountriesAndKosovo()
	{
		Assert.Equal(250, MokaCountries.All.Count);
		Assert.NotNull(MokaCountries.Find("XK"));
	}

	[Fact]
	public void Codes_AreUniqueUpperCasePairs_WithAFlag()
	{
		Assert.Equal(MokaCountries.All.Count, MokaCountries.All.Select(c => c.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count());
		Assert.All(MokaCountries.All, c =>
		{
			Assert.Matches(CodePattern(), c.Code);
			Assert.NotNull(MokaFlag.ToEmoji(c.Code));
		});
	}

	[Fact]
	public void Names_AreNeverEmpty_AndCarryNoSeparator()
	{
		Assert.All(MokaCountries.All, c =>
		{
			Assert.False(string.IsNullOrWhiteSpace(c.Name), c.Code);
			Assert.False(string.IsNullOrWhiteSpace(c.NativeName), c.Code);
			Assert.Equal(c.Name.Trim(), c.Name);
			Assert.DoesNotContain('|', c.Name);
			Assert.All(c.OtherNames, other => Assert.False(string.IsNullOrWhiteSpace(other), c.Code));
		});
		Assert.Equal(MokaCountries.All.Count, MokaCountries.All.Select(c => c.Name).Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void DialingCodes_AreAPlusAndOneToFourDigits()
	{
		Assert.All(MokaCountries.All, c => Assert.All(c.DialingCodes, dial => Assert.Matches(DialPattern(), dial)));

		string[] withoutTelephones = [.. MokaCountries.All.Where(c => c.DialingCodes.Count == 0).Select(c => c.Code).Order(StringComparer.Ordinal)];
		Assert.Equal(["BV", "HM"], withoutTelephones);
	}

	[Theory]
	[InlineData("SE", "+46")]
	[InlineData("US", "+1")]
	[InlineData("CA", "+1")]
	[InlineData("JM", "+1")]
	[InlineData("GB", "+44")]
	[InlineData("RU", "+7")]
	[InlineData("KZ", "+7")]
	[InlineData("XK", "+383")]
	[InlineData("AX", "+358")]
	[InlineData("SS", "+211")]
	[InlineData("VA", "+39")]
	[InlineData("IN", "+91")]
	public void DialingCode_IsTheMainOne(string code, string dial) =>
		Assert.Equal(dial, MokaCountries.Find(code)!.DialingCode);

	[Theory]
	[InlineData("SE", MokaWorldRegion.Europe)]
	[InlineData("RU", MokaWorldRegion.Europe)]
	[InlineData("TR", MokaWorldRegion.Asia)]
	[InlineData("JP", MokaWorldRegion.Asia)]
	[InlineData("EG", MokaWorldRegion.Africa)]
	[InlineData("EH", MokaWorldRegion.Africa)]
	[InlineData("BR", MokaWorldRegion.Americas)]
	[InlineData("GL", MokaWorldRegion.Americas)]
	[InlineData("AU", MokaWorldRegion.Oceania)]
	[InlineData("AQ", MokaWorldRegion.Antarctica)]
	public void Region_FollowsUnM49(string code, MokaWorldRegion region) =>
		Assert.Equal(region, MokaCountries.Find(code)!.Region);

	[Fact]
	public void All_SortsByName_WithAccentsSetAside()
	{
		List<string> codes = [.. MokaCountries.All.Select(c => c.Code)];
		Assert.Equal("AF", codes[0]);
		Assert.True(codes.IndexOf("AX") < codes.IndexOf("AL"), "Åland Islands sorts under A, before Albania.");
		Assert.True(codes.IndexOf("CI") < codes.IndexOf("HR"), "Côte d'Ivoire sorts under C, before Croatia.");
	}

	[Theory]
	[InlineData("se")]
	[InlineData(" SE ")]
	public void Find_IgnoresCaseAndSpaces(string code) => Assert.Equal("Sweden", MokaCountries.Find(code)!.Name);

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("ZZ")]
	[InlineData("EU")]
	public void Find_WithoutACountry_IsNull(string? code) => Assert.Null(MokaCountries.Find(code));

	[Fact]
	public void OwnNames_ComeFromEachCountrysLanguage()
	{
		Assert.Equal("Sverige", MokaCountries.Find("SE")!.NativeName);
		Assert.Equal("日本", MokaCountries.Find("JP")!.NativeName);
		Assert.Equal("Deutschland", MokaCountries.Find("DE")!.NativeName);
		Assert.Equal("Schweiz, Suisse, Svizzera", MokaCountries.Find("CH")!.NativeName);
		Assert.False(MokaCountries.Find("AU")!.HasOwnName);
	}

	[Fact]
	public void Search_ACode_FindsItsCountryFirst()
	{
		MokaCountryMatch first = MokaCountries.Search("se")[0];

		Assert.Equal("SE", first.Country.Code);
		Assert.Equal(MokaCountryMatchKind.Code, first.Kind);
		Assert.Equal("SE", first.Matched);
	}

	[Fact]
	public void Search_PlusOne_FindsTheNorthAmericanPlan_WithTheUnitedStatesFirst()
	{
		IReadOnlyList<MokaCountryMatch> matches = MokaCountries.Search("+1");

		Assert.Equal("US", matches[0].Country.Code);
		Assert.All(matches, m => Assert.Equal(MokaCountryMatchKind.DialingCode, m.Kind));
		Assert.Contains(matches, m => m.Country.Code == "CA");
		Assert.Contains(matches, m => m.Country.Code == "JM");
		Assert.DoesNotContain(matches, m => m.Country.Code == "SE");
	}

	[Theory]
	[InlineData("44")]
	[InlineData("+44")]
	[InlineData("+44 7400 123456")]
	public void Search_ADialingCode_FindsTheCountryItBelongsTo(string text) =>
		Assert.Equal("GB", MokaCountries.Search(text)[0].Country.Code);

	[Fact]
	public void Search_AWholeNumber_FindsItsCountry() =>
		Assert.Equal("SE", MokaCountries.Search("+46701234567")[0].Country.Code);

	[Theory]
	[InlineData("cote", "CI", MokaCountryMatchKind.Name)]
	[InlineData("COTE D'IVOIRE", "CI", MokaCountryMatchKind.Name)]
	[InlineData("aland", "AX", MokaCountryMatchKind.Name)]
	[InlineData("holland", "NL", MokaCountryMatchKind.OtherName)]
	[InlineData("uk", "GB", MokaCountryMatchKind.OtherName)]
	[InlineData("turkey", "TR", MokaCountryMatchKind.OtherName)]
	[InlineData("ivory", "CI", MokaCountryMatchKind.OtherName)]
	[InlineData("suomi", "FI", MokaCountryMatchKind.NativeName)]
	[InlineData("sverige", "SE", MokaCountryMatchKind.NativeName)]
	[InlineData("saint lucia", "LC", MokaCountryMatchKind.OtherName)]
	[InlineData("trinidad and", "TT", MokaCountryMatchKind.OtherName)]
	public void Search_FindsNamesOtherNamesAndOwnNames(string text, string code, MokaCountryMatchKind kind)
	{
		MokaCountryMatch first = MokaCountries.Search(text)[0];

		Assert.Equal(code, first.Country.Code);
		Assert.Equal(kind, first.Kind);
	}

	[Fact]
	public void Search_ANameMatch_SaysNothingMore()
	{
		MokaCountryMatch match = MokaCountries.Search("swed")[0];

		Assert.Equal(MokaCountryMatchKind.Name, match.Kind);
		Assert.Null(match.Matched);
	}

	[Fact]
	public void Search_AWordInTheName_ComesAfterANameThatStartsWithIt()
	{
		IReadOnlyList<MokaCountryMatch> matches = MokaCountries.Search("guinea");

		Assert.Equal("GN", matches[0].Country.Code);
		Assert.Contains(matches, m => m.Country.Code == "PG");
		Assert.Contains(matches, m => m.Country.Code == "GQ");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Search_Blank_FindsNothing(string? text) => Assert.Empty(MokaCountries.Search(text));

	[Fact]
	public void Search_LooksOnlyWhereItIsTold()
	{
		MokaCountry[] nordic = [.. Nordic.Select(code => MokaCountries.Find(code)!)];

		IReadOnlyList<MokaCountryMatch> matches = MokaCountries.Search("s", nordic);

		Assert.All(matches, m => Assert.Contains(m.Country, nordic));
	}

	[Fact]
	public void Search_WithShownNames_MatchesThemFirst_AndStillFindsTheEnglishName()
	{
		static string German(MokaCountry country) => country.Code switch
		{
			"SE" => "Schweden",
			"CH" => "Schweiz",
			_ => country.Name
		};

		Assert.Equal(["SE", "CH"], MokaCountries.Search("schwe", name: German).Take(2).Select(m => m.Country.Code));

		MokaCountryMatch english = MokaCountries.Search("sweden", name: German)[0];
		Assert.Equal("SE", english.Country.Code);
		Assert.Equal(MokaCountryMatchKind.OtherName, english.Kind);
		Assert.Equal("Sweden", english.Matched);
	}

	[Fact]
	public void RegionName_IsEnglish() => Assert.Equal("Americas", MokaCountries.RegionName(MokaWorldRegion.Americas));

	[GeneratedRegex("^[A-Z]{2}$")]
	private static partial Regex CodePattern();

	[GeneratedRegex(@"^\+[1-9]\d{0,3}$")]
	private static partial Regex DialPattern();
}
