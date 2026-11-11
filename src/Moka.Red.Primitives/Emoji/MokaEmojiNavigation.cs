namespace Moka.Red.Primitives.Emoji;

/// <summary>How an emoji panel lets people move between Unicode's groups.</summary>
public enum MokaEmojiNavigation
{
	/// <summary>A row of icons over the grid, one per group. The default.</summary>
	Tabs,

	/// <summary>The same icons down the left edge.</summary>
	Rail,

	/// <summary>One long scroll with sticky group names and no tabs.</summary>
	Headings,

	/// <summary>No groups at all: the most used emoji until something is typed, then the matches.</summary>
	SearchOnly
}
