namespace Moka.Red.Primitives.Reactions;

/// <summary>One reaction on a message: an emoji, how many people reacted with it, and whether the viewer did.</summary>
/// <param name="Emoji">The emoji.</param>
/// <param name="Count">How many people reacted with it. A reaction at 0 isn't shown.</param>
/// <param name="Mine">Whether the person looking is one of them, which draws the chip pressed.</param>
public sealed record MokaReaction(string Emoji, int Count, bool Mine);
