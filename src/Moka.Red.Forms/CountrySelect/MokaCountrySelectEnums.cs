namespace Moka.Red.Forms.CountrySelect;

/// <summary>What a <see cref="MokaCountrySelect" /> looks like.</summary>
public enum MokaCountrySelectShape
{
	/// <summary>A field with a list that opens under it, a search box at its top, and the flag kept in the field.</summary>
	Dropdown,

	/// <summary>
	///     Only the flag and a chevron, for a toolbar or a card header where a field is too wide. The country's name is
	///     its tooltip and its accessible name, so it shows no label. One country at a time.
	/// </summary>
	FlagButton,

	/// <summary>
	///     Tiles with the flag and the code, always open, with the arrow keys moving in both directions, for a list
	///     people scan by eye. Needs an interactive render to pick.
	/// </summary>
	Grid,

	/// <summary>
	///     The flag and the dialing code with a chevron and no border, for the start of a field. What
	///     <c>MokaPhoneInput</c> draws when it has a <c>Country</c>. One country at a time.
	/// </summary>
	Prefix
}

/// <summary>What each row of a <see cref="MokaCountrySelect" />'s list shows beside the flag.</summary>
public enum MokaCountryRowContent
{
	/// <summary>The name only.</summary>
	Name,

	/// <summary>The name, and the two-letter code at the end in a quiet mono.</summary>
	NameAndCode,

	/// <summary>The name, and the dialing code at the end.</summary>
	NameAndDialingCode,

	/// <summary>The name, and the country's own name under it where it differs: Sverige, 日本, Deutschland.</summary>
	OwnName
}

/// <summary>The order of a <see cref="MokaCountrySelect" />'s list.</summary>
public enum MokaCountryOrder
{
	/// <summary>By name, A to Z, with accents set aside, so Åland Islands sorts under A.</summary>
	Alphabetical,

	/// <summary>
	///     The browser's country and the app's <c>Suggested</c> under a Suggested heading, then everyone else under All
	///     countries, without repeating them. With no suggestion to show, the same as <see cref="Alphabetical" />.
	/// </summary>
	SuggestedFirst,

	/// <summary>Under headings for Africa, Americas, Antarctica, Asia, Europe and Oceania, A to Z in each.</summary>
	ByRegion,

	/// <summary>The suggestions first, as <see cref="SuggestedFirst" />, then everyone else under their continents.</summary>
	SuggestedThenByRegion
}
