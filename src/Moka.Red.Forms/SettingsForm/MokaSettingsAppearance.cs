namespace Moka.Red.Forms.SettingsForm;

/// <summary>How a <see cref="MokaSettingsForm" /> is drawn.</summary>
public enum MokaSettingsAppearance
{
	/// <summary>One box: the title, then each group as a heading over its fields.</summary>
	Plain,

	/// <summary>
	///     The header and each group in a card of their own, the groups two to a row when the form is wide enough
	///     (46rem of its own width) and one when it isn't.
	/// </summary>
	Cards
}
