namespace Moka.Red.Feedback.CommandPalette;

/// <summary>How <see cref="MokaCommandPalette" /> matches what is typed against its commands.</summary>
public enum MokaCommandMatching
{
	/// <summary>
	///     A command matches when its title, description, keywords or group holds the typed text, in any case. Results
	///     keep the order the commands were registered in.
	/// </summary>
	Contains,

	/// <summary>
	///     A command matches when the typed letters appear in its title, description, keywords or group in order, with
	///     anything between them, so "opst" finds "Open settings". Results are ranked: letters next to each other, letters
	///     that start a word and a match in the title count for more. Equal matches keep registration order.
	/// </summary>
	Fuzzy
}
