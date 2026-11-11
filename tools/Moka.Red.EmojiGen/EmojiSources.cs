using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace Moka.Red.EmojiGen;

/// <summary>A group of the data file, with the name emoji-test.txt gives it.</summary>
internal sealed record EmojiGroup(string Id, string Name, string Source);

/// <summary>One fully-qualified emoji from emoji-test.txt, without skin tone, with where its tone goes.</summary>
internal sealed record EmojiTestEntry(string Glyph, int Group, double Version, string Name, IReadOnlyList<int> Tone);

/// <summary>What CLDR's annotations say about one emoji.</summary>
internal sealed record CldrAnnotation(string? Name, IReadOnlyList<string> Keywords);

/// <summary>What gemoji's db/emoji.json says about one emoji.</summary>
internal sealed record GemojiEntry(IReadOnlyList<string> Aliases, IReadOnlyList<string> Tags);

/// <summary>One row of the data file.</summary>
internal sealed record EmojiRow(
	string Glyph,
	int Group,
	string Name,
	IReadOnlyList<string> Shortcodes,
	IReadOnlyList<string> Keywords,
	double Version,
	IReadOnlyList<int> Tone);

/// <summary>The rows, and how many needed a stand-in.</summary>
internal sealed record EmojiBuild(IReadOnlyList<EmojiRow> Rows, int FallbackShortcodes, int MissingNames);

/// <summary>
///     Reads Unicode's emoji-test.txt, CLDR's English annotations and gemoji's emoji.json, and writes the data file
///     MokaEmojiPanel loads (format 1).
/// </summary>
internal static class EmojiSources
{
	public const int Format = 1;

	private const int FirstTone = 0x1F3FB;
	private const int LastTone = 0x1F3FF;
	private const int VariationSelector = 0xFE0F;

	public static readonly IReadOnlyList<EmojiGroup> Groups =
	[
		new("smileys", "Smileys and emotion", "Smileys & Emotion"),
		new("people", "People and body", "People & Body"),
		new("nature", "Animals and nature", "Animals & Nature"),
		new("food", "Food and drink", "Food & Drink"),
		new("travel", "Travel and places", "Travel & Places"),
		new("activities", "Activities", "Activities"),
		new("objects", "Objects", "Objects"),
		new("symbols", "Symbols", "Symbols"),
		new("flags", "Flags", "Flags")
	];

	/// <summary>
	///     Where emoji-test.txt for an emoji version lives: "latest" is the newest release, which is what the shipped
	///     file was built from (Emoji 18.0, 2026-04-30).
	/// </summary>
	public static Uri EmojiTestUrl(string version)
	{
		ArgumentNullException.ThrowIfNull(version);

		return new Uri($"https://www.unicode.org/Public/emoji/{version}/emoji-test.txt");
	}

	/// <summary>Where CLDR's English annotations, or its derived ones, live for a release tag.</summary>
	public static Uri CldrUrl(string tag, bool derived) =>
		new($"https://raw.githubusercontent.com/unicode-org/cldr/{tag}/common/{(derived ? "annotationsDerived" : "annotations")}/en.xml");

	/// <summary>Where gemoji's emoji list lives for a branch or tag.</summary>
	public static Uri GemojiUrl(string reference) => new($"https://raw.githubusercontent.com/github/gemoji/{reference}/db/emoji.json");

	/// <summary>
	///     The fully-qualified emoji of every group but Component, in file order, without their skin tone rows. Each
	///     carries the code point indices its tone modifier goes after, worked out from its light skin tone row and kept
	///     only when all five tones then give the sequences the file lists.
	/// </summary>
	public static IReadOnlyList<EmojiTestEntry> ParseEmojiTest(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		List<(int[] CodePoints, int Group, double Version, string Name)> bases = [];
		HashSet<string> toned = new(StringComparer.Ordinal);
		Dictionary<string, int[]> lightByBase = new(StringComparer.Ordinal);
		int group = -1;
		bool component = false;

		foreach (string raw in text.Split('\n'))
		{
			string line = raw.Trim();
			if (line.StartsWith("# group:", StringComparison.Ordinal))
			{
				string name = line["# group:".Length..].Trim();
				component = name == "Component";
				group = component ? -1 : GroupIndex(name);
				continue;
			}

			if (line.Length == 0 || line[0] == '#' || component || group < 0)
			{
				continue;
			}

			int semicolon = line.IndexOf(';', StringComparison.Ordinal);
			int hash = semicolon < 0 ? -1 : line.IndexOf('#', semicolon);
			if (hash < 0 || line[(semicolon + 1)..hash].Trim() != "fully-qualified")
			{
				continue;
			}

			int[] codePoints = [.. line[..semicolon].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(static hex => int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture))];
			string glyph = FromCodePoints(codePoints);

			if (codePoints.Any(IsTone))
			{
				toned.Add(glyph);
				if (codePoints.Where(IsTone).All(static tone => tone == FirstTone))
				{
					lightByBase.TryAdd(WithoutVariation(FromCodePoints([.. codePoints.Where(static c => !IsTone(c))])), codePoints);
				}

				continue;
			}

			(double version, string emojiName) = ReadComment(line[(hash + 1)..]);
			bases.Add((codePoints, group, version, emojiName));
		}

		List<EmojiTestEntry> entries = [];
		foreach ((int[] codePoints, int entryGroup, double version, string name) in bases)
		{
			string glyph = FromCodePoints(codePoints);
			List<int> tone = lightByBase.TryGetValue(WithoutVariation(glyph), out int[]? light)
				? ToneTemplate(codePoints, light) ?? []
				: [];
			if (tone.Count > 0 && !Enumerable.Range(FirstTone, LastTone - FirstTone + 1).All(m => toned.Contains(ApplyTone(glyph, tone, m))))
			{
				tone = [];
			}

			entries.Add(new EmojiTestEntry(glyph, entryGroup, version, name, tone));
		}

		return entries;
	}

	/// <summary>
	///     Names and keywords by emoji, FE0F removed from the key since CLDR mostly leaves it out. A name or keyword in
	///     an earlier document wins.
	/// </summary>
	public static IReadOnlyDictionary<string, CldrAnnotation> ParseCldr(params string[] documents)
	{
		ArgumentNullException.ThrowIfNull(documents);

		Dictionary<string, string> names = new(StringComparer.Ordinal);
		Dictionary<string, List<string>> keywords = new(StringComparer.Ordinal);
		XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };

		foreach (string document in documents)
		{
			using StringReader text = new(document);
			using XmlReader reader = XmlReader.Create(text, settings);
			XDocument xml = XDocument.Load(reader);
			foreach (XElement annotation in xml.Descendants("annotation"))
			{
				if (annotation.Attribute("cp")?.Value is not { Length: > 0 } cp)
				{
					continue;
				}

				string key = WithoutVariation(cp);
				if (annotation.Attribute("type")?.Value == "tts")
				{
					names.TryAdd(key, annotation.Value.Trim());
					continue;
				}

				if (!keywords.TryGetValue(key, out List<string>? list))
				{
					keywords.Add(key, list = []);
				}

				foreach (string word in annotation.Value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				{
					if (!list.Contains(word, StringComparer.OrdinalIgnoreCase))
					{
						list.Add(word);
					}
				}
			}
		}

		Dictionary<string, CldrAnnotation> result = new(StringComparer.Ordinal);
		foreach (string key in names.Keys.Union(keywords.Keys, StringComparer.Ordinal))
		{
			result.Add(key, new CldrAnnotation(names.GetValueOrDefault(key), keywords.GetValueOrDefault(key) ?? []));
		}

		return result;
	}

	/// <summary>Aliases and tags by emoji, FE0F removed from the key. The first entry for an emoji wins.</summary>
	public static IReadOnlyDictionary<string, GemojiEntry> ParseGemoji(string json)
	{
		ArgumentNullException.ThrowIfNull(json);

		Dictionary<string, GemojiEntry> result = new(StringComparer.Ordinal);
		using JsonDocument document = JsonDocument.Parse(json);
		foreach (JsonElement item in document.RootElement.EnumerateArray())
		{
			if (!item.TryGetProperty("emoji", out JsonElement emoji) || emoji.GetString() is not { Length: > 0 } glyph)
			{
				continue;
			}

			result.TryAdd(WithoutVariation(glyph), new GemojiEntry(Strings(item, "aliases"), Strings(item, "tags")));
		}

		return result;
	}

	/// <summary>
	///     The data file's rows in group order, file order within a group. Shortcodes are gemoji's aliases that are
	///     still free, or the name in snake case when none is; keywords are CLDR's, then gemoji's tags.
	/// </summary>
	public static EmojiBuild Build(IReadOnlyList<EmojiTestEntry> entries, IReadOnlyDictionary<string, CldrAnnotation> cldr,
		IReadOnlyDictionary<string, GemojiEntry> gemoji)
	{
		ArgumentNullException.ThrowIfNull(entries);
		ArgumentNullException.ThrowIfNull(cldr);
		ArgumentNullException.ThrowIfNull(gemoji);

		HashSet<string> used = new(StringComparer.Ordinal);
		List<EmojiRow> rows = [];
		int fallbacks = 0;
		int missingNames = 0;

		foreach (EmojiTestEntry entry in entries.OrderBy(static e => e.Group))
		{
			string key = WithoutVariation(entry.Glyph);
			CldrAnnotation? annotation = cldr.GetValueOrDefault(key);
			GemojiEntry? gem = gemoji.GetValueOrDefault(key);

			string name = annotation?.Name ?? entry.Name;
			if (annotation?.Name is null)
			{
				missingNames++;
			}

			List<string> shortcodes = [];
			foreach (string alias in gem?.Aliases ?? [])
			{
				string code = Lower(alias.Trim());
				if (IsShortcode(code) && used.Add(code))
				{
					shortcodes.Add(code);
				}
			}

			if (shortcodes.Count == 0)
			{
				fallbacks++;
				string snake = SnakeCase(name);
				shortcodes.Add(Unique(snake.Length > 0 ? snake : Hex(entry.Glyph), used));
			}

			List<string> keywords = [];
			foreach (string word in (annotation?.Keywords ?? []).Concat(gem?.Tags ?? []))
			{
				// People type a hyphen, so a CLDR keyword written with a dash (police car's 5-0) is kept with a hyphen.
				string clean = Lower(word.Replace('|', ' ').Replace('\u2013', '-').Replace('\u2014', '-').Trim());
				if (clean.Length > 0 && !keywords.Contains(clean, StringComparer.Ordinal))
				{
					keywords.Add(clean);
				}
			}

			rows.Add(new EmojiRow(entry.Glyph, entry.Group, name, shortcodes, keywords, entry.Version, entry.Tone));
		}

		return new EmojiBuild(rows, fallbacks, missingNames);
	}

	/// <summary>
	///     The data file as text, one row per line, emoji as characters. System.Text.Json would write every character
	///     outside the basic plane as a pair of escapes, which doubles the size of each emoji.
	/// </summary>
	public static string Write(IReadOnlyList<EmojiRow> rows, string unicodeVersion, bool starter)
	{
		ArgumentNullException.ThrowIfNull(rows);

		StringBuilder json = new();
		json.Append("{\n  \"format\": ").Append(Format.ToString(CultureInfo.InvariantCulture))
			.Append(",\n  \"language\": \"en\",\n  \"unicode\": ").Append(Quote(unicodeVersion))
			.Append(",\n  \"starter\": ").Append(starter ? "true" : "false")
			.Append(",\n  \"groups\": [\n");
		json.AppendJoin(",\n", Groups.Select(static g => $"    {{\"id\": {Quote(g.Id)}, \"name\": {Quote(g.Name)}}}"));
		json.Append("\n  ],\n  \"emoji\": [\n");
		json.AppendJoin(",\n", rows.Select(static row => "    " + Row(row)));
		json.Append("\n  ]\n}\n");
		return json.ToString();
	}

	/// <summary>
	///     The emoji with a skin tone modifier after each code point index in <paramref name="tone" />. An FE0F right
	///     after one is dropped, as emoji-test.txt drops it.
	/// </summary>
	public static string ApplyTone(string glyph, IReadOnlyList<int> tone, int modifier)
	{
		ArgumentNullException.ThrowIfNull(glyph);
		ArgumentNullException.ThrowIfNull(tone);

		int[] codePoints = CodePoints(glyph);
		StringBuilder result = new();
		for (int i = 0; i < codePoints.Length; i++)
		{
			if (i > 0 && codePoints[i] == VariationSelector && tone.Contains(i - 1))
			{
				continue;
			}

			result.Append(char.ConvertFromUtf32(codePoints[i]));
			if (tone.Contains(i))
			{
				result.Append(char.ConvertFromUtf32(modifier));
			}
		}

		return result.ToString();
	}

	/// <summary>The text with every U+FE0F removed, which is how CLDR and gemoji key most emoji.</summary>
	public static string WithoutVariation(string text) =>
		text?.Replace("\uFE0F", "", StringComparison.Ordinal) ?? throw new ArgumentNullException(nameof(text));

	/// <summary>The emoji version and date emoji-test.txt names in its header, or null where it names none.</summary>
	public static (string? Version, string? Date) ReadEmojiTestHeader(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		string? version = null;
		string? date = null;
		foreach (string raw in text.Split('\n'))
		{
			string line = raw.Trim();
			if (line.Length > 0 && line[0] != '#')
			{
				break;
			}

			if (line.StartsWith("# Version:", StringComparison.Ordinal))
			{
				version = line["# Version:".Length..].Trim();
			}
			else if (line.StartsWith("# Date:", StringComparison.Ordinal))
			{
				date = line["# Date:".Length..].Trim();
			}
		}

		return (version, date);
	}

	/// <summary>A name as a shortcode: lower-case ASCII letters and digits joined by underscores.</summary>
	public static string SnakeCase(string name)
	{
		ArgumentNullException.ThrowIfNull(name);

		StringBuilder result = new();
		bool gap = false;
		foreach (char c in name.Normalize(NormalizationForm.FormD))
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
			{
				continue;
			}

			if (char.IsAsciiLetterOrDigit(c))
			{
				if (gap && result.Length > 0)
				{
					result.Append('_');
				}

				result.Append(char.ToLowerInvariant(c));
				gap = false;
			}
			else
			{
				gap = true;
			}
		}

		return result.ToString();
	}

	private static string Row(EmojiRow row) => string.Create(CultureInfo.InvariantCulture,
		$"[{Quote(row.Glyph)},{row.Group},{Quote(row.Name)},{Quote(string.Join('|', row.Shortcodes))},{Quote(string.Join('|', row.Keywords))},{row.Version},{Tone(row.Tone)}]");

	private static string Tone(IReadOnlyList<int> tone) => tone.Count switch
	{
		0 => "0",
		1 when tone[0] == 0 => "1",
		_ => "[" + string.Join(',', tone.Select(static i => i.ToString(CultureInfo.InvariantCulture))) + "]"
	};

	private static string Quote(string text)
	{
		StringBuilder result = new(text.Length + 2);
		result.Append('"');
		foreach (char c in text)
		{
			switch (c)
			{
				case '"':
					result.Append("\\\"");
					break;
				case '\\':
					result.Append("\\\\");
					break;
				case < ' ':
					result.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
					break;
				default:
					result.Append(c);
					break;
			}
		}

		return result.Append('"').ToString();
	}

	private static List<int>? ToneTemplate(int[] bare, int[] toned)
	{
		List<int> after = [];
		int i = 0;
		for (int j = 0; j < toned.Length; j++)
		{
			if (IsTone(toned[j]))
			{
				if (i == 0)
				{
					return null;
				}

				after.Add(i - 1);
				if (i < bare.Length && bare[i] == VariationSelector && (j + 1 >= toned.Length || toned[j + 1] != VariationSelector))
				{
					i++;
				}

				continue;
			}

			if (i >= bare.Length || bare[i] != toned[j])
			{
				return null;
			}

			i++;
		}

		return i == bare.Length && after.Count > 0 ? after : null;
	}

	private static (double Version, string Name) ReadComment(string comment)
	{
		string[] words = comment.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
		if (words.Length < 3 || words[1].Length < 2 || words[1][0] != 'E')
		{
			throw new InvalidDataException($"No emoji version in \"{comment.Trim()}\".");
		}

		return (double.Parse(words[1][1..], NumberStyles.Float, CultureInfo.InvariantCulture), words[2].Trim());
	}

	private static int GroupIndex(string source)
	{
		for (int i = 0; i < Groups.Count; i++)
		{
			if (Groups[i].Source == source)
			{
				return i;
			}
		}

		throw new InvalidDataException($"emoji-test.txt has a group this tool doesn't know: \"{source}\".");
	}

	private static IReadOnlyList<string> Strings(JsonElement item, string property) =>
		item.TryGetProperty(property, out JsonElement array) && array.ValueKind == JsonValueKind.Array
			? [.. array.EnumerateArray().Select(static e => e.GetString()).OfType<string>()]
			: [];

	private static bool IsShortcode(string code) =>
		code.Length > 0 && code.All(static c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c is '_' or '+' or '-');

	private static string Unique(string code, HashSet<string> used)
	{
		string candidate = code;
		for (int n = 2; !used.Add(candidate); n++)
		{
			candidate = string.Create(CultureInfo.InvariantCulture, $"{code}_{n}");
		}

		return candidate;
	}

	private static string Lower(string text) => string.Create(text.Length, text, static (span, source) =>
	{
		for (int i = 0; i < source.Length; i++)
		{
			span[i] = char.ToLowerInvariant(source[i]);
		}
	});

	private static string Hex(string glyph) =>
		"u" + string.Join('_', CodePoints(glyph).Where(static c => c != VariationSelector).Select(static c => c.ToString("x", CultureInfo.InvariantCulture)));

	private static bool IsTone(int codePoint) => codePoint is >= FirstTone and <= LastTone;

	private static int[] CodePoints(string text) => [.. text.EnumerateRunes().Select(static r => r.Value)];

	private static string FromCodePoints(IEnumerable<int> codePoints) => string.Concat(codePoints.Select(char.ConvertFromUtf32));
}
