using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Moka.Red.Primitives.Tests.Emoji;

/// <summary>
///     The emoji data file MokaEmojiPanel loads, src/Moka.Red.Primitives/wwwroot/moka-emoji.en.json, read from the
///     repository so the shipped file is the one checked. While "starter" is true it is the hand-written stand-in for
///     the file tools/Moka.Red.EmojiGen writes.
/// </summary>
public sealed partial class EmojiDataFileTests
{
	private const int MediumTone = 0x1F3FD;

	private static readonly string[] GroupIds = ["smileys", "people", "nature", "food", "travel", "activities", "objects", "symbols", "flags"];

	// The 91 emoji the board BC approved draws.
	private static readonly string[] BoardEmoji =
	[
		"😀", "😂", "🙂", "😉", "😍", "🤔", "😎", "😅", "😮", "😭", "😴", "🤯", "🙃", "❤️", "💯", "💬",
		"👍", "👎", "👋", "👏", "🙌", "🙏", "💪", "✌️", "👀", "🤝", "🧑‍💻", "🐶", "🐱", "🦊", "🐼", "🐢",
		"🐛", "🦋", "🌵", "🌸", "🍀", "☕", "🍕", "🍔", "🌮", "🍣", "🍎", "🍩", "🎂", "🍺", "🚀", "✈️",
		"🚗", "🏠", "🌍", "🔥", "⚡", "🌈", "⭐", "☀️", "🎉", "🎁", "🏆", "⚽", "🎮", "🎯", "🎨", "🧩",
		"💡", "📌", "🔒", "🔑", "📎", "📅", "💻", "🔧", "📦", "✅", "❌", "⚠️", "❓", "🚫", "♻️", "🔴",
		"🟢", "➕", "🏁", "🏳️‍🌈", "🇸🇪", "🇺🇸", "🇯🇵", "🇧🇷", "🇩🇪", "🇺🇦", "🇰🇪"
	];

	private static readonly Lazy<string> Text = new(() => File.ReadAllText(DataFilePath(), Encoding.UTF8));

	private static readonly Lazy<List<Row>> AllRows = new(ReadRows);

	[Fact]
	public void TheFile_IsFormatOneInEnglish()
	{
		using JsonDocument document = JsonDocument.Parse(Text.Value);
		JsonElement root = document.RootElement;

		Assert.Equal(1, root.GetProperty("format").GetInt32());
		Assert.Equal("en", root.GetProperty("language").GetString());
		Assert.Matches(UnicodeVersion(), root.GetProperty("unicode").GetString() ?? "");
		Assert.True(root.GetProperty("starter").ValueKind is JsonValueKind.True or JsonValueKind.False);
	}

	[Fact]
	public void TheGroups_AreUnicodesNineInOrder()
	{
		using JsonDocument document = JsonDocument.Parse(Text.Value);
		JsonElement[] groups = [.. document.RootElement.GetProperty("groups").EnumerateArray()];

		Assert.Equal(GroupIds, groups.Select(g => g.GetProperty("id").GetString()));
		Assert.All(groups, g => Assert.False(string.IsNullOrWhiteSpace(g.GetProperty("name").GetString())));
	}

	[Fact]
	public void EveryRow_HasTheSevenFieldsOfFormatOne()
	{
		List<string> found = [];
		foreach (Row row in AllRows.Value)
		{
			if (row.Glyph.Length == 0 || row.Name.Length == 0 || row.Shortcodes.Length == 0 || row.Version <= 0 ||
			    row.Group is < 0 or > 8 || row.Keywords.Any(string.IsNullOrWhiteSpace))
			{
				found.Add(row.Glyph);
			}
		}

		Assert.Empty(found);
	}

	[Fact]
	public void TheRows_AreInGroupOrder_AndEveryGroupHasSome()
	{
		int[] groups = [.. AllRows.Value.Select(row => row.Group)];

		Assert.Equal(groups.Order(), groups);
		Assert.All(Enumerable.Range(0, GroupIds.Length), g => Assert.Contains(g, groups));
	}

	[Fact]
	public void NoEmoji_IsListedTwice()
	{
		List<string> glyphs = [.. AllRows.Value.Select(row => row.Glyph)];

		Assert.Equal(glyphs.Count, glyphs.Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void EveryShortcode_IsUniqueAndTypeable()
	{
		List<string> codes = [.. AllRows.Value.SelectMany(row => row.Shortcodes)];

		Assert.All(codes, code => Assert.Matches(Shortcode(), code));
		Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void NoRow_HoldsASkinTone()
	{
		Assert.DoesNotContain(AllRows.Value, row => row.Glyph.EnumerateRunes().Any(r => r.Value is >= 0x1F3FB and <= 0x1F3FF));
	}

	[Fact]
	public void EveryTone_PointsInsideItsEmoji()
	{
		List<string> found = [];
		foreach (Row row in AllRows.Value)
		{
			int length = row.Glyph.EnumerateRunes().Count();
			if (row.Tone.Any(i => i < 0 || i >= length) || row.Tone.Distinct().Count() != row.Tone.Length)
			{
				found.Add(row.Glyph);
			}
		}

		Assert.Empty(found);
	}

	[Fact]
	public void EveryEmojiOnTheBoard_IsInTheFile()
	{
		HashSet<string> glyphs = [.. AllRows.Value.Select(row => row.Glyph)];

		Assert.Equal(91, BoardEmoji.Length);
		Assert.All(BoardEmoji, emoji => Assert.Contains(emoji, glyphs));
	}

	[Fact]
	public void TheFile_HasEnoughRows()
	{
		using JsonDocument document = JsonDocument.Parse(Text.Value);
		bool starter = document.RootElement.GetProperty("starter").GetBoolean();
		int count = AllRows.Value.Count;

		Assert.True(count >= (starter ? 300 : 1800), $"{count} rows");
	}

	[Theory]
	[InlineData("👍", "👍🏽")]
	[InlineData("🧑‍💻", "🧑🏽‍💻")]
	[InlineData("✌️", "✌🏽")]
	[InlineData("🧑‍🤝‍🧑", "🧑🏽‍🤝‍🧑🏽")]
	[InlineData("🏋️‍♀️", "🏋🏽‍♀️")]
	public void ATone_GoesWhereTheRowSays(string emoji, string toned)
	{
		Row row = Assert.Single(AllRows.Value, r => r.Glyph == emoji);

		Assert.Equal(toned, WithTone(row, MediumTone));
	}

	[Fact]
	public void AnEmojiWithoutATone_StaysAsItIs()
	{
		Row row = Assert.Single(AllRows.Value, r => r.Glyph == "😀");

		Assert.Empty(row.Tone);
		Assert.Equal("😀", WithTone(row, MediumTone));
	}

	[Fact]
	public void TheFile_WritesEmojiAsCharacters()
	{
		Assert.DoesNotContain("\\u", Text.Value, StringComparison.Ordinal);
	}

	[Fact]
	public void RecentEmoji_AreThereForTheVersionProbeToHide()
	{
		Assert.Contains(AllRows.Value, row => row.Version >= 16);
		Assert.Contains(AllRows.Value, row => row.Version is >= 15 and < 16);
		Assert.Contains(AllRows.Value, row => row.Version is >= 14 and < 15);
	}

	// The rule the panel's script applies: a modifier after each listed code point, dropping an FE0F right after it.
	private static string WithTone(Row row, int modifier)
	{
		int[] codePoints = [.. row.Glyph.EnumerateRunes().Select(r => r.Value)];
		StringBuilder result = new();
		for (int i = 0; i < codePoints.Length; i++)
		{
			if (i > 0 && codePoints[i] == 0xFE0F && row.Tone.Contains(i - 1))
			{
				continue;
			}

			result.Append(char.ConvertFromUtf32(codePoints[i]));
			if (row.Tone.Contains(i))
			{
				result.Append(char.ConvertFromUtf32(modifier));
			}
		}

		return result.ToString();
	}

	private static List<Row> ReadRows()
	{
		using JsonDocument document = JsonDocument.Parse(Text.Value);
		List<Row> rows = [];
		foreach (JsonElement row in document.RootElement.GetProperty("emoji").EnumerateArray())
		{
			Assert.Equal(JsonValueKind.Array, row.ValueKind);
			Assert.Equal(7, row.GetArrayLength());
			Assert.Equal(JsonValueKind.String, row[0].ValueKind);
			Assert.Equal(JsonValueKind.Number, row[1].ValueKind);
			Assert.Equal(JsonValueKind.String, row[2].ValueKind);
			Assert.Equal(JsonValueKind.String, row[3].ValueKind);
			Assert.Equal(JsonValueKind.String, row[4].ValueKind);
			Assert.Equal(JsonValueKind.Number, row[5].ValueKind);

			JsonElement tone = row[6];
			int[] after = tone.ValueKind switch
			{
				JsonValueKind.Number when tone.GetInt32() == 0 => [],
				JsonValueKind.Number when tone.GetInt32() == 1 => [0],
				JsonValueKind.Array => [.. tone.EnumerateArray().Select(i => i.GetInt32())],
				_ => throw new InvalidDataException($"Row {row[0].GetString()} has the tone {tone.GetRawText()}.")
			};

			string keywords = row[4].GetString()!;
			rows.Add(new Row(
				row[0].GetString()!,
				row[1].GetInt32(),
				row[2].GetString()!,
				row[3].GetString()!.Split('|'),
				keywords.Length == 0 ? [] : keywords.Split('|'),
				row[5].GetDouble(),
				after));
		}

		return rows;
	}

	private static string DataFilePath()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "Moka.Red.slnx")))
			{
				return Path.Combine(folder.FullName, "src", "Moka.Red.Primitives", "wwwroot", "moka-emoji.en.json");
			}
		}

		throw new InvalidOperationException("The repository root was not found.");
	}

	[GeneratedRegex(@"^[a-z0-9_+\-]+$")]
	private static partial Regex Shortcode();

	[GeneratedRegex(@"^\d+\.\d+$")]
	private static partial Regex UnicodeVersion();

	private sealed record Row(string Glyph, int Group, string Name, string[] Shortcodes, string[] Keywords, double Version, int[] Tone);
}
