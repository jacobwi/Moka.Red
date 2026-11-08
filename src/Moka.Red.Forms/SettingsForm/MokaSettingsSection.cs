using Moka.Red.Core.Icons;

namespace Moka.Red.Forms.SettingsForm;

/// <summary>One row of the side list of a <see cref="MokaSettingsLayout" />.</summary>
/// <param name="Id">What <see cref="MokaSettingsLayout.Active" /> names it by.</param>
/// <param name="Label">The row's text.</param>
/// <param name="Group">The name of the group it's listed under. Sections of a group stay together, in order of first appearance.</param>
/// <param name="Href">
///     Its address. Without one the row only switches <see cref="MokaSettingsLayout.Active" />, and leaving unsaved edits
///     that way isn't asked about, because no navigation happens.
/// </param>
public sealed record MokaSettingsSection(string Id, string Label, string? Group = null, string? Href = null)
{
	/// <summary>
	///     An icon before the label, in the row and beside the section's name in search results. A row without one leaves
	///     no gap, so give every row of a group an icon or none.
	/// </summary>
	public MokaIconDefinition? Icon { get; init; }
}
