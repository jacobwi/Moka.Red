namespace Moka.Red.Primitives.Emoji;

/// <summary>An emoji someone picked.</summary>
/// <param name="Emoji">The emoji as it goes into text, with the skin tone applied.</param>
/// <param name="Name">Its name from Unicode CLDR, such as "party popper". Empty when the data file hasn't loaded.</param>
/// <param name="Shortcode">Its main shortcode without colons, such as "tada". Empty when it has none.</param>
/// <param name="Base">The emoji without a skin tone, which is what <see cref="MokaEmojiUse" /> counts.</param>
public sealed record MokaEmojiPick(string Emoji, string Name, string Shortcode, string Base);
