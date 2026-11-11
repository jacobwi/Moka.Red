namespace Moka.Red.Primitives.Emoji;

/// <summary>
///     Every word an emoji panel shows or says to a screen reader, in English by default. Replace the ones a language
///     needs with <c>with</c>, and pass a data file in the same language through <see cref="MokaEmojiOptions.DataSource" />
///     for the emoji's own names and keywords.
/// </summary>
public sealed record MokaEmojiText
{
	/// <summary>The English words.</summary>
	public static MokaEmojiText Default { get; } = new();

	/// <summary>The name of the panel. Default "Choose an emoji".</summary>
	public string PanelLabel { get; init; } = "Choose an emoji";

	/// <summary>The name of the grid or list. Default "Emoji".</summary>
	public string GridLabel { get; init; } = "Emoji";

	/// <summary>The search box's placeholder. Default "Search emoji".</summary>
	public string SearchPlaceholder { get; init; } = "Search emoji";

	/// <summary>The search box's name. Default "Search emoji".</summary>
	public string SearchLabel { get; init; } = "Search emoji";

	/// <summary>The name of the row of group tabs. Default "Groups".</summary>
	public string GroupsLabel { get; init; } = "Groups";

	/// <summary>
	///     The nine group names, in Unicode's order: smileys, people, nature, food, travel, activities, objects, symbols,
	///     flags. Null names the tabs in English and the headings from the data file.
	/// </summary>
	public IReadOnlyList<string>? Groups { get; init; }

	/// <summary>The first group, the ones picked most. Default "Frequently used".</summary>
	public string Frequent { get; init; } = "Frequently used";

	/// <summary>
	///     The group shown with <see cref="MokaEmojiNavigation.SearchOnly" /> before anything is picked. Default
	///     "Popular".
	/// </summary>
	public string Common { get; init; } = "Popular";

	/// <summary>The note under a search-only panel. <c>{count}</c> is how many emoji there are. Default "Type to find any of the {count} emoji."</summary>
	public string SearchOnlyNote { get; init; } = "Type to find any of the {count} emoji.";

	/// <summary>The heading over one search result. Default "1 match".</summary>
	public string OneMatch { get; init; } = "1 match";

	/// <summary>The heading over several search results. <c>{count}</c> is how many. Default "{count} matches".</summary>
	public string Matches { get; init; } = "{count} matches";

	/// <summary>What an empty search says. <c>{query}</c> is what was typed. Default <c>No emoji match "{query}"</c>.</summary>
	public string NoMatch { get; init; } = "No emoji match \"{query}\"";

	/// <summary>The line under <see cref="NoMatch" />. Default "Try a name, a keyword or a :shortcode:".</summary>
	public string NoMatchHint { get; init; } = "Try a name, a keyword or a :shortcode:";

	/// <summary>What the grid says while the data file loads. Default "Loading emoji".</summary>
	public string Loading { get; init; } = "Loading emoji";

	/// <summary>What the grid says when the data file couldn't load. Default "The emoji couldn't be loaded."</summary>
	public string Failed { get; init; } = "The emoji couldn't be loaded.";

	/// <summary>The preview strip's text while nothing is lit. Default "Nothing lit".</summary>
	public string NothingLit { get; init; } = "Nothing lit";

	/// <summary>The skin tone button's tooltip and the tone list's name. Default "Skin tone".</summary>
	public string SkinTone { get; init; } = "Skin tone";

	/// <summary>
	///     The six tone names, from <see cref="MokaSkinTone.Default" /> to <see cref="MokaSkinTone.Dark" />. Null is
	///     Default, Light, Medium light, Medium, Medium dark, Dark.
	/// </summary>
	public IReadOnlyList<string>? ToneNames { get; init; }

	/// <summary>The name of the quick picks of <see cref="MokaEmojiShape.Strip" />. Default "Quick picks".</summary>
	public string QuickLabel { get; init; } = "Quick picks";

	/// <summary>The button that opens the full grid under a strip. Default "More".</summary>
	public string More { get; init; } = "More";

	/// <summary>The same button while the grid is open. Default "Less".</summary>
	public string Less { get; init; } = "Less";

	/// <summary>The key hint for the arrows. Default "Move".</summary>
	public string HintMove { get; init; } = "Move";

	/// <summary>The key hint for Enter. Default "Pick".</summary>
	public string HintPick { get; init; } = "Pick";

	/// <summary>The key hint for Page Up and Page Down. Default "Group".</summary>
	public string HintGroup { get; init; } = "Group";

	/// <summary>The key hint for Home and End. Default "Ends".</summary>
	public string HintEnds { get; init; } = "Ends";

	/// <summary>The key hint for Escape. Default "Close".</summary>
	public string HintClose { get; init; } = "Close";

	internal static IReadOnlyList<string> EnglishGroups { get; } =
	[
		"Smileys and emotion", "People and body", "Animals and nature", "Food and drink", "Travel and places",
		"Activities", "Objects", "Symbols", "Flags"
	];

	internal static IReadOnlyList<string> EnglishToneNames { get; } =
		["Default", "Light", "Medium light", "Medium", "Medium dark", "Dark"];

	internal string GroupName(int index) =>
		Groups is { } names && index < names.Count ? names[index] : EnglishGroups[index];

	internal string ToneName(MokaSkinTone tone) =>
		ToneNames is { } names && (int)tone < names.Count ? names[(int)tone] : EnglishToneNames[(int)tone];
}
