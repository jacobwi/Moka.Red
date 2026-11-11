using Moka.Red.Primitives.Emoji;

namespace Moka.Red.Primitives.Tests.Emoji;

public sealed class MokaEmojiRecentTests
{
	private static readonly DateTimeOffset Noon = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

	[Fact]
	public void APick_StartsACount()
	{
		IReadOnlyList<MokaEmojiUse> uses = MokaEmojiRecent.Add(null, "\U0001F389", Noon);

		Assert.Equal([new MokaEmojiUse("\U0001F389", 1, Noon)], uses);
	}

	[Fact]
	public void PicksOfOneEmoji_CountTogether_WhateverTheirToneOrVariationSelector()
	{
		IReadOnlyList<MokaEmojiUse> uses = MokaEmojiRecent.Add(null, "\U0001F44D", Noon);
		uses = MokaEmojiRecent.Add(uses, "\U0001F44D\U0001F3FD", Noon.AddMinutes(1));
		uses = MokaEmojiRecent.Add(uses, "\u2764\uFE0F", Noon.AddMinutes(2));
		uses = MokaEmojiRecent.Add(uses, "\u2764", Noon.AddMinutes(3));

		Assert.Equal(2, uses.Count);
		Assert.Contains(new MokaEmojiUse("\U0001F44D", 2, Noon.AddMinutes(1)), uses);
		Assert.Contains(new MokaEmojiUse("\u2764", 2, Noon.AddMinutes(3)), uses);
	}

	[Fact]
	public void TheRanking_IsByCount_ThenByHowRecent()
	{
		IReadOnlyList<MokaEmojiUse> uses =
		[
			new("a", 1, Noon),
			new("b", 3, Noon.AddDays(-1)),
			new("c", 3, Noon),
			new("d", 2, Noon)
		];

		Assert.Equal(["c", "b", "d", "a"], MokaEmojiRecent.Ranked(uses, 10));
		Assert.Equal(["c", "b"], MokaEmojiRecent.Ranked(uses, 2));
		Assert.Empty(MokaEmojiRecent.Ranked(uses, 0));
		Assert.Empty(MokaEmojiRecent.Ranked(null, 5));
	}

	[Fact]
	public void TheList_IsCapped()
	{
		IReadOnlyList<MokaEmojiUse>? uses = null;
		for (int i = 0; i < MokaEmojiRecent.Kept + 20; i++)
		{
			uses = MokaEmojiRecent.Add(uses, char.ConvertFromUtf32(0x1F600 + i), Noon.AddMinutes(i));
		}

		Assert.Equal(MokaEmojiRecent.Kept, uses!.Count);
		Assert.Contains(uses, use => use.Emoji == char.ConvertFromUtf32(0x1F600 + MokaEmojiRecent.Kept + 19));
	}

	[Fact]
	public void Bare_DropsTonesAndVariationSelectors_AndKeepsZwj()
	{
		Assert.Equal("\U0001F9D1\u200D\U0001F4BB", MokaEmojiRecent.Bare("\U0001F9D1\U0001F3FD\u200D\U0001F4BB"));
		Assert.Equal("\u270C", MokaEmojiRecent.Bare("\u270C\uFE0F"));
	}
}
