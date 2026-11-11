namespace Moka.Red.Primitives.Emoji;

/// <summary>
///     How an emoji panel looks and behaves, in one record so every component that holds a panel (MokaEmojiPanel,
///     MokaEmojiPicker, the emoji buttons of MokaTextField, MokaTextArea and MokaChat, MokaReactions and
///     MokaEmojiTypeahead) takes the same choices. It also carries the two things a panel changes, the skin tone and the
///     frequently used list: the component hands back a new record through its <c>OptionsChanged</c> callback and the
///     app stores it however it likes.
/// </summary>
public sealed record MokaEmojiOptions
{
	/// <summary>The defaults: a popover grid with group tabs, search, skin tones, frequently used and a preview strip.</summary>
	public static MokaEmojiOptions Default { get; } = new();

	/// <summary>How the panel is laid out and shown. Default <see cref="MokaEmojiShape.Popover" />.</summary>
	public MokaEmojiShape Shape { get; init; } = MokaEmojiShape.Popover;

	/// <summary>How people move between groups. Default <see cref="MokaEmojiNavigation.Tabs" />.</summary>
	public MokaEmojiNavigation Navigation { get; init; } = MokaEmojiNavigation.Tabs;

	/// <summary>Whether the panel has a search box. Default true.</summary>
	public bool ShowSearch { get; init; } = true;

	/// <summary>
	///     Whether search also matches keywords and shortcodes. Whole matches rank first (a shortcode, then a name, then
	///     a keyword), then what starts with the text, so "lol" finds 😂 and "yes" finds 👍 and ✅. False matches names
	///     only. Default true.
	/// </summary>
	public bool SearchKeywords { get; init; } = true;

	/// <summary>Whether a button beside the search box picks the skin tone. Default true.</summary>
	public bool SkinTones { get; init; } = true;

	/// <summary>The skin tone applied to every emoji that takes one, ZWJ sequences included. Default none.</summary>
	public MokaSkinTone SkinTone { get; init; } = MokaSkinTone.Default;

	/// <summary>Whether the first group holds the emoji picked most, from <see cref="Recent" />. Default true.</summary>
	public bool ShowFrequent { get; init; } = true;

	/// <summary>
	///     What was picked, how often and when, which ranks the Frequently used group by count and then by how recent.
	///     The panel adds every pick and hands the new list back; the app stores it. Null starts empty.
	/// </summary>
	public IReadOnlyList<MokaEmojiUse>? Recent { get; init; }

	/// <summary>How many emoji the Frequently used group shows. Default 16.</summary>
	public int FrequentCount { get; init; } = 16;

	/// <summary>Whether a strip at the bottom shows the lit emoji large with its name and shortcode. Default true.</summary>
	public bool ShowPreview { get; init; } = true;

	/// <summary>
	///     Whether a row under the grid shows what the keys do. The arrows, Page Up and Page Down, Home, End and Enter
	///     work either way. Default false.
	/// </summary>
	public bool ShowKeyHints { get; init; }

	/// <summary>
	///     The quick picks of <see cref="MokaEmojiShape.Strip" />. Null is eight everyday ones: 😀 🍔 ☕ 🎉 🏠 ✈️ 😴 🤔.
	/// </summary>
	public IReadOnlyList<string>? Quick { get; init; }

	/// <summary>
	///     The newest emoji version to show, such as 15.1. Null finds out what this system draws on first open and hides
	///     the emoji it would show as boxes.
	/// </summary>
	public double? MaxVersion { get; init; }

	/// <summary>How many columns the grid has. Null fits as many as the width takes.</summary>
	public int? Columns { get; init; }

	/// <summary>
	///     The address of an app's own data file in the same format, for names and keywords in another language. Null
	///     loads the English file that ships with Moka.Red.Primitives.
	/// </summary>
	public string? DataSource { get; init; }

	/// <summary>The words the panel shows. Null is English.</summary>
	public MokaEmojiText? Text { get; init; }

	internal static IReadOnlyList<string> DefaultQuick { get; } =
		["\U0001F600", "\U0001F354", "\u2615", "\U0001F389", "\U0001F3E0", "\u2708\uFE0F", "\U0001F634", "\U0001F914"];
}
