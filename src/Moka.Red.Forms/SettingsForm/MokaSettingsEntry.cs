namespace Moka.Red.Forms.SettingsForm;

/// <summary>One setting someone might look for, for the search box and an app's command palette.</summary>
/// <param name="Label">The setting's name.</param>
/// <param name="Keywords">Other words someone might type for it.</param>
/// <param name="Anchor">The id of the element it scrolls to, such as a <see cref="MokaSettingsGroup" />'s.</param>
/// <param name="Advanced">Whether going to it turns on "Show advanced" first.</param>
public sealed record MokaSettingsEntry(string Label, string Keywords, string Anchor, bool Advanced = false)
{
	/// <summary>The address of the page it's on, like <c>settings/general</c>. Null means the page showing.</summary>
	public string? Route { get; init; }

	/// <summary>The section it's in, shown beside it in the results and matched by the search.</summary>
	public string? Section { get; init; }

	/// <summary>The address that goes to it. Without a <see cref="Route" /> this is only the fragment.</summary>
	public string Href => $"{Route}#{Uri.EscapeDataString(Anchor)}";
}
