namespace Moka.Red.Primitives.Flag;

/// <summary>A country <see cref="MokaCountries.Search" /> found, with what matched.</summary>
/// <param name="Country">The country.</param>
/// <param name="Kind">What the search text matched.</param>
/// <param name="Matched">
///     The text that matched when it wasn't the shown name: the code, the dialing code, the other name or the own
///     name. Null for <see cref="MokaCountryMatchKind.Name" />.
/// </param>
public sealed record MokaCountryMatch(MokaCountry Country, MokaCountryMatchKind Kind, string? Matched);

/// <summary>What a search text matched in a <see cref="MokaCountry" />.</summary>
public enum MokaCountryMatchKind
{
	/// <summary>The name shown for it.</summary>
	Name,

	/// <summary>Its two-letter code, typed in full.</summary>
	Code,

	/// <summary>One of its dialing codes, from the start.</summary>
	DialingCode,

	/// <summary>One of its other names, or its English name when another name is shown.</summary>
	OtherName,

	/// <summary>Its own name.</summary>
	NativeName
}
