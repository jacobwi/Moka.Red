using System.Text.Json;
using Moka.Red.EmojiGen;

namespace Moka.Red.Primitives.Tests.Emoji;

/// <summary>
///     The generator in tools/Moka.Red.EmojiGen, run over small copies of its four sources: Unicode's emoji-test.txt,
///     CLDR's annotations and derived annotations, and gemoji's emoji.json.
/// </summary>
public sealed class EmojiGeneratorTests
{
	private const string EmojiTest = """
		# emoji-test.txt
		# Version: 16.0

		# group: Smileys & Emotion

		# subgroup: face-smiling
		1F600                                                  ; fully-qualified     # 😀 E1.0 grinning face
		1F602                                                  ; fully-qualified     # 😂 E0.6 face with tears of joy

		# subgroup: heart
		2764 FE0F                                              ; fully-qualified     # ❤️ E0.6 red heart
		2764                                                   ; unqualified         # ❤ E0.6 red heart

		# group: People & Body

		# subgroup: hand-fingers-open
		1F91A                                                  ; fully-qualified     # 🤚 E3.0 raised back of hand
		1F91A 1F3FB                                            ; fully-qualified     # 🤚🏻 E3.0 raised back of hand: light skin tone

		# subgroup: hand-fingers-partial
		270C FE0F                                              ; fully-qualified     # ✌️ E0.6 victory hand
		270C                                                   ; unqualified         # ✌ E0.6 victory hand
		270C 1F3FB                                             ; fully-qualified     # ✌🏻 E1.0 victory hand: light skin tone
		270C 1F3FC                                             ; fully-qualified     # ✌🏼 E1.0 victory hand: medium-light skin tone
		270C 1F3FD                                             ; fully-qualified     # ✌🏽 E1.0 victory hand: medium skin tone
		270C 1F3FE                                             ; fully-qualified     # ✌🏾 E1.0 victory hand: medium-dark skin tone
		270C 1F3FF                                             ; fully-qualified     # ✌🏿 E1.0 victory hand: dark skin tone

		# subgroup: hand-fingers-closed
		1F44D                                                  ; fully-qualified     # 👍 E0.6 thumbs up
		1F44D 1F3FB                                            ; fully-qualified     # 👍🏻 E1.0 thumbs up: light skin tone
		1F44D 1F3FC                                            ; fully-qualified     # 👍🏼 E1.0 thumbs up: medium-light skin tone
		1F44D 1F3FD                                            ; fully-qualified     # 👍🏽 E1.0 thumbs up: medium skin tone
		1F44D 1F3FE                                            ; fully-qualified     # 👍🏾 E1.0 thumbs up: medium-dark skin tone
		1F44D 1F3FF                                            ; fully-qualified     # 👍🏿 E1.0 thumbs up: dark skin tone

		# subgroup: family
		1F9D1 200D 1F91D 200D 1F9D1                            ; fully-qualified     # 🧑‍🤝‍🧑 E12.0 people holding hands
		1F9D1 1F3FB 200D 1F91D 200D 1F9D1 1F3FB                ; fully-qualified     # 🧑🏻‍🤝‍🧑🏻 E12.0 people holding hands: light skin tone
		1F9D1 1F3FB 200D 1F91D 200D 1F9D1 1F3FC                ; fully-qualified     # 🧑🏻‍🤝‍🧑🏼 E12.1 people holding hands: light skin tone, medium-light skin tone
		1F9D1 1F3FC 200D 1F91D 200D 1F9D1 1F3FC                ; fully-qualified     # 🧑🏼‍🤝‍🧑🏼 E12.0 people holding hands: medium-light skin tone
		1F9D1 1F3FD 200D 1F91D 200D 1F9D1 1F3FD                ; fully-qualified     # 🧑🏽‍🤝‍🧑🏽 E12.0 people holding hands: medium skin tone
		1F9D1 1F3FE 200D 1F91D 200D 1F9D1 1F3FE                ; fully-qualified     # 🧑🏾‍🤝‍🧑🏾 E12.0 people holding hands: medium-dark skin tone
		1F9D1 1F3FF 200D 1F91D 200D 1F9D1 1F3FF                ; fully-qualified     # 🧑🏿‍🤝‍🧑🏿 E12.0 people holding hands: dark skin tone

		# subgroup: person-symbol
		1F463                                                  ; fully-qualified     # 👣 E0.6 footprints

		# group: Component

		# subgroup: skin-tone
		1F3FB                                                  ; fully-qualified     # 🏻 E1.0 light skin tone

		# group: Flags

		# subgroup: country-flag
		1F1F8 1F1EA                                            ; fully-qualified     # 🇸🇪 E2.0 flag: Sweden
		1F3F3 FE0F 200D 1F308                                  ; fully-qualified     # 🏳️‍🌈 E4.0 rainbow flag
		1F3F3 200D 1F308                                       ; unqualified         # 🏳‍🌈 E4.0 rainbow flag

		#EOF
		""";

	private const string Cldr = """
		<?xml version="1.0" encoding="UTF-8" ?>
		<!DOCTYPE ldml SYSTEM "../../common/dtd/ldml.dtd">
		<ldml>
			<identity>
				<language type="en"/>
			</identity>
			<annotations>
				<annotation cp="😀">face | grin | grinning face</annotation>
				<annotation cp="😀" type="tts">grinning face</annotation>
				<annotation cp="😂">face | Joy | laugh | tear</annotation>
				<annotation cp="😂" type="tts">face with tears of joy</annotation>
				<annotation cp="❤">heart</annotation>
				<annotation cp="❤" type="tts">red heart</annotation>
				<annotation cp="✌">hand | v | victory</annotation>
				<annotation cp="✌" type="tts">victory hand</annotation>
				<annotation cp="👍">+1 | hand | thumb | up</annotation>
				<annotation cp="👍" type="tts">thumbs up</annotation>
			</annotations>
		</ldml>
		""";

	private const string CldrDerived = """
		<?xml version="1.0" encoding="UTF-8" ?>
		<!DOCTYPE ldml SYSTEM "../../common/dtd/ldml.dtd">
		<ldml>
			<annotations>
				<annotation cp="🧑‍🤝‍🧑">couple | hand | hold | holding hands | person</annotation>
				<annotation cp="🧑‍🤝‍🧑" type="tts">people holding hands</annotation>
				<annotation cp="🇸🇪">flag</annotation>
				<annotation cp="🇸🇪" type="tts">flag: Sweden</annotation>
				<annotation cp="🏳‍🌈">pride | rainbow</annotation>
				<annotation cp="🏳‍🌈" type="tts">rainbow flag</annotation>
			</annotations>
		</ldml>
		""";

	private const string Gemoji = """
		[
		  {"emoji": "😀", "description": "grinning face", "aliases": ["grinning"], "tags": ["smile", "happy"]},
		  {"emoji": "😂", "description": "face with tears of joy", "aliases": ["joy", "footprints"], "tags": ["tears"]},
		  {"emoji": "✌️", "description": "victory hand", "aliases": ["V"], "tags": ["victory", "peace"]},
		  {"emoji": "👍", "description": "thumbs up", "aliases": ["+1", "thumbsup"], "tags": ["approve", "ok"]},
		  {"emoji": "🧑‍🤝‍🧑", "description": "people holding hands", "aliases": ["red_heart", "people_holding_hands"], "tags": ["couple", "date"]},
		  {"emoji": "🏳️‍🌈", "description": "rainbow flag", "aliases": ["rainbow_flag"], "tags": ["pride"]}
		]
		""";

	private static IReadOnlyList<EmojiTestEntry> Entries => EmojiSources.ParseEmojiTest(EmojiTest);

	private static EmojiBuild Built => EmojiSources.Build(Entries, EmojiSources.ParseCldr(Cldr, CldrDerived), EmojiSources.ParseGemoji(Gemoji));

	private static EmojiRow Row(string glyph) => Assert.Single(Built.Rows, row => row.Glyph == glyph);

	[Fact]
	public void EmojiTest_KeepsOnlyFullyQualifiedRowsWithoutTones_AndSkipsComponent()
	{
		Assert.Equal(["😀", "😂", "❤️", "🤚", "✌️", "👍", "🧑‍🤝‍🧑", "👣", "🇸🇪", "🏳️‍🌈"], Entries.Select(e => e.Glyph));
	}

	[Fact]
	public void EmojiTest_MapsGroupsToTheirIndexes()
	{
		Assert.Equal([0, 0, 0, 1, 1, 1, 1, 1, 8, 8], Entries.Select(e => e.Group));
		Assert.Equal("smileys", EmojiSources.Groups[0].Id);
		Assert.Equal("flags", EmojiSources.Groups[8].Id);
		Assert.Equal(9, EmojiSources.Groups.Count);
	}

	[Fact]
	public void EmojiTest_ReadsVersionsAndNames()
	{
		EmojiTestEntry grinning = Entries[0];
		EmojiTestEntry joy = Entries[1];

		Assert.Equal(1.0, grinning.Version);
		Assert.Equal(0.6, joy.Version);
		Assert.Equal(12.0, Entries.Single(e => e.Glyph == "🧑‍🤝‍🧑").Version);
		Assert.Equal("face with tears of joy", joy.Name);
		Assert.Equal("flag: Sweden", Entries.Single(e => e.Glyph == "🇸🇪").Name);
	}

	[Fact]
	public void EmojiTest_ToneGoesAfterTheFirstCodePoint()
	{
		Assert.Equal([0], Entries.Single(e => e.Glyph == "👍").Tone);
		Assert.Equal("👍🏽", EmojiSources.ApplyTone("👍", [0], 0x1F3FD));
	}

	[Fact]
	public void EmojiTest_ToneDropsTheVariationSelectorAfterIt()
	{
		Assert.Equal([0], Entries.Single(e => e.Glyph == "✌️").Tone);
		Assert.Equal("✌🏽", EmojiSources.ApplyTone("✌️", [0], 0x1F3FD));
		Assert.Equal("🏋🏽‍♀️", EmojiSources.ApplyTone("🏋️‍♀️", [0], 0x1F3FD));
	}

	[Fact]
	public void EmojiTest_TwoPeopleTakeTheToneTwice()
	{
		Assert.Equal([0, 4], Entries.Single(e => e.Glyph == "🧑‍🤝‍🧑").Tone);
		Assert.Equal("🧑🏽‍🤝‍🧑🏽", EmojiSources.ApplyTone("🧑‍🤝‍🧑", [0, 4], 0x1F3FD));
	}

	[Fact]
	public void EmojiTest_KeepsNoToneUnlessAllFiveAreListed()
	{
		Assert.Empty(Entries.Single(e => e.Glyph == "🤚").Tone);
		Assert.Empty(Entries.Single(e => e.Glyph == "😀").Tone);
	}

	[Fact]
	public void Build_TakesTheCldrNameAndMergesKeywordsWithGemojiTags()
	{
		Assert.Equal("grinning face", Row("😀").Name);
		Assert.Equal(["face", "grin", "grinning face", "smile", "happy"], Row("😀").Keywords);
		Assert.Equal(["couple", "hand", "hold", "holding hands", "person", "date"], Row("🧑‍🤝‍🧑").Keywords);
		Assert.Equal(["face", "joy", "laugh", "tear", "tears"], Row("😂").Keywords);
	}

	[Fact]
	public void Build_FindsCldrAndGemojiEntriesWrittenWithoutTheVariationSelector()
	{
		Assert.Equal("rainbow flag", Row("🏳️‍🌈").Name);
		Assert.Equal(["pride", "rainbow"], Row("🏳️‍🌈").Keywords);
		Assert.Equal(["v"], Row("✌️").Shortcodes);
	}

	[Fact]
	public void Build_KeepsGemojiAliasesInOrder()
	{
		Assert.Equal(["+1", "thumbsup"], Row("👍").Shortcodes);
		Assert.Equal(["grinning"], Row("😀").Shortcodes);
	}

	[Fact]
	public void Build_FallsBackToTheNameInSnakeCase()
	{
		Assert.Equal(["red_heart"], Row("❤️").Shortcodes);
		Assert.Equal(["flag_sweden"], Row("🇸🇪").Shortcodes);
		Assert.Equal("flag_sweden", EmojiSources.SnakeCase("flag: Sweden"));
		Assert.Equal("pinata", EmojiSources.SnakeCase("piñata"));
		Assert.Equal("keycap", EmojiSources.SnakeCase("keycap: #"));
	}

	[Fact]
	public void Build_NeverGivesTwoEmojiTheSameShortcode()
	{
		EmojiBuild built = Built;
		List<string> all = [.. built.Rows.SelectMany(row => row.Shortcodes)];

		Assert.Equal(all.Count, all.Distinct(StringComparer.Ordinal).Count());
		Assert.Equal(["people_holding_hands"], Row("🧑‍🤝‍🧑").Shortcodes);
		Assert.Equal(["footprints_2"], Row("👣").Shortcodes);
		Assert.Equal(["joy", "footprints"], Row("😂").Shortcodes);
	}

	[Fact]
	public void Build_CountsWhatNeededAStandIn()
	{
		EmojiBuild built = Built;

		Assert.Equal(4, built.FallbackShortcodes);
		Assert.Equal(2, built.MissingNames);
	}

	[Fact]
	public void Write_GivesTheDataFileFormat_WithEmojiAsCharacters()
	{
		string json = EmojiSources.Write(Built.Rows, "16.0", starter: false);

		Assert.Contains("\"😀\"", json, StringComparison.Ordinal);
		Assert.Contains("[\"🧑‍🤝‍🧑\",1,\"people holding hands\",\"people_holding_hands\",\"couple|hand|hold|holding hands|person|date\",12,[0,4]]", json, StringComparison.Ordinal);
		Assert.Contains("[\"👍\",1,\"thumbs up\",\"+1|thumbsup\",\"+1|hand|thumb|up|approve|ok\",0.6,1]", json, StringComparison.Ordinal);
		Assert.DoesNotContain("\\u", json, StringComparison.Ordinal);

		using JsonDocument document = JsonDocument.Parse(json);
		JsonElement root = document.RootElement;
		Assert.Equal(1, root.GetProperty("format").GetInt32());
		Assert.Equal("en", root.GetProperty("language").GetString());
		Assert.Equal("16.0", root.GetProperty("unicode").GetString());
		Assert.False(root.GetProperty("starter").GetBoolean());
		Assert.Equal(EmojiSources.Groups.Select(g => g.Id), root.GetProperty("groups").EnumerateArray().Select(g => g.GetProperty("id").GetString()));

		JsonElement[] rows = [.. root.GetProperty("emoji").EnumerateArray()];
		Assert.Equal(10, rows.Length);
		Assert.All(rows, row => Assert.Equal(7, row.GetArrayLength()));
		Assert.Equal("😀", rows[0][0].GetString());
		Assert.Equal(0, rows[0][6].GetInt32());
		Assert.Equal(0.6, rows[1][5].GetDouble());
	}

	[Fact]
	public void Write_EscapesQuotesAndBackslashes()
	{
		EmojiRow row = new("😀", 0, "a \"quoted\" \\ name", ["x"], [], 1, []);

		using JsonDocument document = JsonDocument.Parse(EmojiSources.Write([row], "16.0", starter: true));

		Assert.Equal("a \"quoted\" \\ name", document.RootElement.GetProperty("emoji")[0][2].GetString());
		Assert.True(document.RootElement.GetProperty("starter").GetBoolean());
	}

	[Theory]
	[InlineData("latest", "https://www.unicode.org/Public/emoji/latest/emoji-test.txt")]
	[InlineData("16.0", "https://www.unicode.org/Public/emoji/16.0/emoji-test.txt")]
	public void EmojiTestUrl_FollowsWhereUnicodePublishesIt(string version, string expected)
	{
		Assert.Equal(new Uri(expected), EmojiSources.EmojiTestUrl(version));
	}

	[Fact]
	public void ADashInAKeyword_BecomesTheHyphenPeopleType()
	{
		EmojiTestEntry car = new("\U0001F693", 4, 0.6, "police car", []);
		Dictionary<string, CldrAnnotation> cldr = new(StringComparer.Ordinal)
		{
			["\U0001F693"] = new CldrAnnotation("police car", ["5\u20130", "car", "cops\u2014patrol"])
		};

		EmojiRow row = Assert.Single(EmojiSources.Build([car], cldr, new Dictionary<string, GemojiEntry>()).Rows);

		Assert.Equal(["5-0", "car", "cops-patrol"], row.Keywords);
	}

	[Fact]
	public void TheHeader_NamesTheVersionAndDate()
	{
		(string? version, string? date) = EmojiSources.ReadEmojiTestHeader(
			"# emoji-test.txt\n# Date: 2026-04-30, 12:00:00 GMT\n# Version: 18.0\n#\n1F600 ; fully-qualified # 😀 E1.0 grinning face\n# Version: 99.0\n");

		Assert.Equal("18.0", version);
		Assert.Equal("2026-04-30, 12:00:00 GMT", date);
	}

	[Fact]
	public void SourceUrls_PinTheReleaseTheyAreGiven()
	{
		Assert.Equal(new Uri("https://raw.githubusercontent.com/unicode-org/cldr/main/common/annotations/en.xml"),
			EmojiSources.CldrUrl("main", derived: false));
		Assert.Equal(new Uri("https://raw.githubusercontent.com/unicode-org/cldr/main/common/annotationsDerived/en.xml"),
			EmojiSources.CldrUrl("main", derived: true));
		Assert.Equal(new Uri("https://raw.githubusercontent.com/github/gemoji/master/db/emoji.json"), EmojiSources.GemojiUrl("master"));
	}
}
