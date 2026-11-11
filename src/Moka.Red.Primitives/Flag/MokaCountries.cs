using System.Collections.Frozen;
using System.Collections.ObjectModel;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Flag;

/// <summary>
///     Every country <see cref="MokaFlag" /> draws a flag for: the 249 ISO 3166-1 countries and territories plus Kosovo
///     (XK), with English and own names, dialing codes and continents. The table is built into the assembly, so names
///     are there while prerendering and under WebAssembly's invariant globalization, where <c>RegionInfo</c> has none,
///     and nothing is fetched. Left out of MokaFlag's regions: the European Union and the United Nations, the
///     exceptional reservations AC, CP, DG, EA, IC and TA (parts of other countries, whose codes are listed under them),
///     and England, Scotland and Wales.
/// </summary>
public static partial class MokaCountries
{
	// Where several places share a dialing code, the one it belongs to first, as libphonenumber orders them, so "+1"
	// lists the United States before Jamaica and "44" the United Kingdom before Jersey.
	private static readonly FrozenSet<string> MainForSharedCode = new[]
	{
		"US", "RU", "GB", "NO", "AU", "RE", "SH", "FI", "IT", "FK", "GP", "CW", "NF", "MA", "NZ"
	}.ToFrozenSet(StringComparer.Ordinal);

	private static readonly Folded[] Table = Parse();

	private static readonly ReadOnlyCollection<MokaCountry> Countries =
		Array.AsReadOnly(Table.Select(static f => f.Country).ToArray());

	private static readonly FrozenDictionary<string, Folded> Index =
		Table.ToFrozenDictionary(static f => f.Country.Code, StringComparer.OrdinalIgnoreCase);

	/// <summary>Every country, sorted by English name with accents set aside, so Åland Islands sorts under A.</summary>
	public static IReadOnlyList<MokaCountry> All => Countries;

	/// <summary>The country with a code, in any case and with spaces around it, or null when there's none.</summary>
	/// <param name="code">A two-letter code such as "SE" or "se".</param>
	/// <returns>The country, or null.</returns>
	public static MokaCountry? Find(string? code) =>
		!string.IsNullOrWhiteSpace(code) && Index.TryGetValue(code.Trim(), out Folded? found) ? found.Country : null;

	/// <summary>The English name of a continent, such as "Americas".</summary>
	/// <param name="region">The continent.</param>
	/// <returns>Its name.</returns>
	public static string RegionName(MokaWorldRegion region) => region switch
	{
		MokaWorldRegion.Africa => "Africa",
		MokaWorldRegion.Americas => "Americas",
		MokaWorldRegion.Antarctica => "Antarctica",
		MokaWorldRegion.Asia => "Asia",
		MokaWorldRegion.Europe => "Europe",
		MokaWorldRegion.Oceania => "Oceania",
		_ => region.ToString()
	};

	/// <summary>
	///     Finds countries by what someone typed, best first. Matches the shown name, the two-letter code, a dialing code
	///     (with or without its plus, "44" or "+44", and a whole number such as "+46701234567"), other names ("Holland",
	///     "UK") and the own name, with case, accents and apostrophes set aside (<see cref="MokaTextFold" />). An exact
	///     code beats an exact other name, which beats a name that starts with the text, then a word that does, then the
	///     rest; ties sort by name.
	/// </summary>
	/// <param name="text">What was typed. Blank finds nothing.</param>
	/// <param name="countries">Where to look. Null looks in <see cref="All" />.</param>
	/// <param name="name">
	///     The name shown for each country, for an app that shows its own (in another language, say). The English name
	///     still matches, reported as an other name. Null shows <see cref="MokaCountry.Name" />.
	/// </param>
	/// <returns>The matches, best first.</returns>
	public static IReadOnlyList<MokaCountryMatch> Search(string? text, IEnumerable<MokaCountry>? countries = null,
		Func<MokaCountry, string>? name = null)
	{
		string query = MokaTextFold.Fold(text).Trim();
		if (query.Length == 0)
		{
			return [];
		}

		string digits = DialDigits(query);
		bool dialing = digits.Length > 0 && query.All(static c => char.IsAsciiDigit(c) || c is '+' or ' ' or '(' or ')' or '-' or '.');

		List<(MokaCountryMatch Match, int Score, string Shown)> hits = [];
		foreach (MokaCountry country in countries ?? Countries)
		{
			Folded folded = Index.TryGetValue(country.Code, out Folded? known) && ReferenceEquals(known.Country, country)
				? known
				: new Folded(country);
			string shown = name is null ? folded.Name : MokaTextFold.Fold(name(country));
			(int score, MokaCountryMatchKind kind, string? matched) = dialing
				? ByDialing(country, digits)
				: ByText(folded, shown, name is not null, query);
			if (score > 0)
			{
				hits.Add((new MokaCountryMatch(country, kind, matched), score, shown));
			}
		}

		hits.Sort(static (a, b) => a.Score != b.Score ? b.Score.CompareTo(a.Score) : string.CompareOrdinal(a.Shown, b.Shown));
		return [.. hits.Select(static hit => hit.Match)];
	}

	private static (int Score, MokaCountryMatchKind Kind, string? Matched) ByDialing(MokaCountry country, string digits)
	{
		(int Score, MokaCountryMatchKind Kind, string? Matched) best = (0, MokaCountryMatchKind.DialingCode, null);
		foreach (string code in country.DialingCodes)
		{
			string codeDigits = DialDigits(code);
			int score = 0;
			if (codeDigits.StartsWith(digits, StringComparison.Ordinal))
			{
				score = 900 - 10 * (codeDigits.Length - digits.Length);
			}
			else if (digits.StartsWith(codeDigits, StringComparison.Ordinal))
			{
				// A whole number typed or pasted: the longer the code it starts with, the surer the match.
				score = 600 + 10 * codeDigits.Length;
			}

			if (score > best.Score)
			{
				best = (score, MokaCountryMatchKind.DialingCode, code);
			}
		}

		if (best.Score > 0 && MainForSharedCode.Contains(country.Code))
		{
			best.Score++;
		}

		return best;
	}

	private static (int Score, MokaCountryMatchKind Kind, string? Matched) ByText(Folded folded, string shown,
		bool ownNames, string query)
	{
		MokaCountry country = folded.Country;
		if (query.Length == 2 && string.Equals(country.Code, query, StringComparison.OrdinalIgnoreCase))
		{
			return (1000, MokaCountryMatchKind.Code, country.Code);
		}

		int exact = Array.IndexOf(folded.Others, query);
		if (exact >= 0)
		{
			return (950, MokaCountryMatchKind.OtherName, country.OtherNames[exact]);
		}

		if (shown.StartsWith(query, StringComparison.Ordinal))
		{
			return (800, MokaCountryMatchKind.Name, null);
		}

		if (StartsAWord(shown, query))
		{
			return (700, MokaCountryMatchKind.Name, null);
		}

		for (int i = 0; i < folded.Others.Length; i++)
		{
			if (StartsAWord(folded.Others[i], query))
			{
				return (600, MokaCountryMatchKind.OtherName, country.OtherNames[i]);
			}
		}

		if (ownNames && StartsAWord(folded.Name, query))
		{
			return (580, MokaCountryMatchKind.OtherName, country.Name);
		}

		if (country.HasOwnName && folded.Native.Contains(query, StringComparison.Ordinal))
		{
			return (500, MokaCountryMatchKind.NativeName, country.NativeName);
		}

		if (shown.Contains(query, StringComparison.Ordinal))
		{
			return (400, MokaCountryMatchKind.Name, null);
		}

		if (ownNames && folded.Name.Contains(query, StringComparison.Ordinal))
		{
			return (350, MokaCountryMatchKind.OtherName, country.Name);
		}

		for (int i = 0; i < folded.Others.Length; i++)
		{
			if (folded.Others[i].Contains(query, StringComparison.Ordinal))
			{
				return (300, MokaCountryMatchKind.OtherName, country.OtherNames[i]);
			}
		}

		return (0, MokaCountryMatchKind.Name, null);
	}

	private static bool StartsAWord(string text, string query)
	{
		for (int at = text.IndexOf(query, StringComparison.Ordinal); at >= 0;
		     at = text.IndexOf(query, at + 1, StringComparison.Ordinal))
		{
			if (at == 0 || !char.IsLetterOrDigit(text[at - 1]))
			{
				return true;
			}
		}

		return false;
	}

	private static string DialDigits(string text) => string.Concat(text.Where(char.IsAsciiDigit));

	private static Folded[] Parse()
	{
		List<Folded> parsed = [];
		foreach (string line in Data.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			string[] fields = line.Split('|');
			MokaCountry country = new(
				fields[0],
				fields[1],
				fields[2],
				fields[3].Length == 0 ? [] : fields[3].Split(' '),
				RegionOf(fields[4]),
				fields[5].Length == 0 ? [] : fields[5].Split(';'));
			parsed.Add(new Folded(country));
		}

		parsed.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));
		return [.. parsed];
	}

	private static MokaWorldRegion RegionOf(string letter) => letter switch
	{
		"F" => MokaWorldRegion.Africa,
		"M" => MokaWorldRegion.Americas,
		"N" => MokaWorldRegion.Antarctica,
		"S" => MokaWorldRegion.Asia,
		"E" => MokaWorldRegion.Europe,
		"O" => MokaWorldRegion.Oceania,
		_ => throw new InvalidOperationException($"Unknown region '{letter}' in the country table.")
	};

	private sealed class Folded(MokaCountry country)
	{
		public MokaCountry Country { get; } = country;

		public string Name { get; } = MokaTextFold.Fold(country.Name);

		public string Native { get; } = MokaTextFold.Fold(country.NativeName);

		public string[] Others { get; } = [.. country.OtherNames.Select(MokaTextFold.Fold)];
	}
}
