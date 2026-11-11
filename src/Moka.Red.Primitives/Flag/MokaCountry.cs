namespace Moka.Red.Primitives.Flag;

/// <summary>
///     A country or territory from <see cref="MokaCountries" />: its code, its names, how to phone it and where it is.
/// </summary>
/// <param name="Code">The ISO 3166-1 alpha-2 code in upper case, such as "SE". Kosovo is "XK".</param>
/// <param name="Name">The English name, as CLDR writes it, such as "Côte d’Ivoire" or "St. Lucia".</param>
/// <param name="NativeName">
///     The name in the country's own language, such as "Sverige". Several, separated by commas, where a country has
///     more than one ("Schweiz, Suisse, Svizzera"). The English name where that is the country's own.
/// </param>
/// <param name="DialingCodes">
///     The international dialing codes with their plus, such as "+46", the main one first. Empty for a place with no
///     telephones of its own, such as Bouvet Island. Countries of the North American plan all have "+1".
/// </param>
/// <param name="Region">The continent, by the UN's M49 regions.</param>
/// <param name="OtherNames">Other names people search by, such as "Holland" or "UK".</param>
public sealed record MokaCountry(
	string Code,
	string Name,
	string NativeName,
	IReadOnlyList<string> DialingCodes,
	MokaWorldRegion Region,
	IReadOnlyList<string> OtherNames)
{
	/// <summary>The main dialing code, such as "+46", or null when the place has none.</summary>
	public string? DialingCode => DialingCodes.Count > 0 ? DialingCodes[0] : null;

	/// <summary>Whether <see cref="NativeName" /> says something <see cref="Name" /> doesn't.</summary>
	public bool HasOwnName => !string.Equals(NativeName, Name, StringComparison.Ordinal);
}
