using System.Text;

namespace Moka.Red.Primitives.Emoji;

/// <summary>The frequently used list: counting a pick, and ranking by count and then by how recent.</summary>
internal static class MokaEmojiRecent
{
	// Enough to rank a full group by count without the list growing for ever.
	internal const int Kept = 64;

	/// <summary>The list with one more pick of <paramref name="emoji" />, as a new list.</summary>
	public static IReadOnlyList<MokaEmojiUse> Add(IReadOnlyList<MokaEmojiUse>? recent, string emoji, DateTimeOffset now)
	{
		string key = Bare(emoji);
		List<MokaEmojiUse> uses = [];
		int count = 1;
		foreach (MokaEmojiUse use in recent ?? [])
		{
			if (string.Equals(Bare(use.Emoji), key, StringComparison.Ordinal))
			{
				count += Math.Max(0, use.Count);
			}
			else
			{
				uses.Add(use);
			}
		}

		uses.Add(new MokaEmojiUse(WithoutTone(emoji), count, now));
		return [.. Order(uses).Take(Kept)];
	}

	private static string WithoutTone(string emoji)
	{
		StringBuilder result = new(emoji.Length);
		foreach (Rune rune in emoji.EnumerateRunes())
		{
			if (rune.Value is < 0x1F3FB or > 0x1F3FF)
			{
				result.Append(rune.ToString());
			}
		}

		return result.ToString();
	}

	/// <summary>The <paramref name="count" /> emoji picked most, then most lately.</summary>
	public static IReadOnlyList<string> Ranked(IReadOnlyList<MokaEmojiUse>? recent, int count) =>
		recent is null || count <= 0 ? [] : [.. Order(recent).Select(static use => use.Emoji).Take(count)];

	/// <summary>The emoji without skin tone modifiers or variation selectors, which is how picks are counted.</summary>
	public static string Bare(string emoji)
	{
		ArgumentNullException.ThrowIfNull(emoji);

		StringBuilder result = new(emoji.Length);
		foreach (Rune rune in emoji.EnumerateRunes())
		{
			if (rune.Value is 0xFE0F or >= 0x1F3FB and <= 0x1F3FF)
			{
				continue;
			}

			result.Append(rune.ToString());
		}

		return result.ToString();
	}

	private static IEnumerable<MokaEmojiUse> Order(IEnumerable<MokaEmojiUse> uses) =>
		uses.Where(static use => !string.IsNullOrEmpty(use.Emoji))
			.OrderByDescending(static use => use.Count)
			.ThenByDescending(static use => use.LastUsed);
}
