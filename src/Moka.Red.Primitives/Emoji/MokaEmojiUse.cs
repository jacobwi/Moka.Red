namespace Moka.Red.Primitives.Emoji;

/// <summary>
///     How often and how lately someone picked an emoji, for the Frequently used group. The app keeps the list: it
///     passes it in through <see cref="MokaEmojiOptions.Recent" /> and stores what the panel hands back.
/// </summary>
/// <param name="Emoji">The emoji without a skin tone.</param>
/// <param name="Count">How many times it was picked.</param>
/// <param name="LastUsed">When it was last picked.</param>
public sealed record MokaEmojiUse(string Emoji, int Count, DateTimeOffset LastUsed);
