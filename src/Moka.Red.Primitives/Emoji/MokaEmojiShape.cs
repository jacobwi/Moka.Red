namespace Moka.Red.Primitives.Emoji;

/// <summary>How an emoji panel is laid out, and how the components that hold one show it.</summary>
public enum MokaEmojiShape
{
	/// <summary>A grid with search and group tabs, in a popup opened from a button. The default.</summary>
	Popover,

	/// <summary>A row of quick picks with More, which opens the full grid in place under the row.</summary>
	Strip,

	/// <summary>
	///     Docked as wide as its host, with the groups along the bottom like a phone keyboard. It stays open for several
	///     picks; its button or Escape closes it.
	/// </summary>
	Composer,

	/// <summary>A list like a command palette: each row the emoji, its name and its shortcode, most used first.</summary>
	List
}
