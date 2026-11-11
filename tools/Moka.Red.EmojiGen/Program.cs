using System.Globalization;
using System.Text;
using Moka.Red.EmojiGen;

const string EmojiTestFile = "emoji-test.txt";
const string CldrFile = "annotations-en.xml";
const string CldrDerivedFile = "annotationsDerived-en.xml";
const string GemojiFile = "emoji.json";

string emojiVersion = "latest";
string cldrTag = "main";
string gemojiReference = "master";
string sources = Path.Combine(Path.GetTempPath(), "moka-emoji-sources");
Dictionary<string, string> paths = new(StringComparer.Ordinal);
string? output = null;
bool download = false;

for (int i = 0; i < args.Length; i++)
{
	switch (args[i])
	{
		case "--download":
			download = true;
			break;
		case "--sources":
			sources = Take(args, ref i);
			break;
		case "--emoji-test":
			paths[EmojiTestFile] = Take(args, ref i);
			break;
		case "--cldr-annotations":
			paths[CldrFile] = Take(args, ref i);
			break;
		case "--cldr-derived":
			paths[CldrDerivedFile] = Take(args, ref i);
			break;
		case "--gemoji-file":
			paths[GemojiFile] = Take(args, ref i);
			break;
		case "--emoji":
			emojiVersion = Take(args, ref i);
			break;
		case "--cldr":
			cldrTag = Take(args, ref i);
			break;
		case "--gemoji":
			gemojiReference = Take(args, ref i);
			break;
		case "--out":
			output = Take(args, ref i);
			break;
		case "--help" or "-h" or "/?":
			Usage();
			return 0;
		default:
			await Console.Error.WriteLineAsync($"Unknown argument: {args[i]}");
			Usage();
			return 2;
	}
}

(string File, Uri Url)[] files =
[
	(EmojiTestFile, EmojiSources.EmojiTestUrl(emojiVersion)),
	(CldrFile, EmojiSources.CldrUrl(cldrTag, derived: false)),
	(CldrDerivedFile, EmojiSources.CldrUrl(cldrTag, derived: true)),
	(GemojiFile, EmojiSources.GemojiUrl(gemojiReference))
];

if (download)
{
	Directory.CreateDirectory(sources);
	using HttpClient http = new();
	http.DefaultRequestHeaders.UserAgent.ParseAdd("Moka.Red.EmojiGen/1.0");
	foreach ((string file, Uri url) in files)
	{
		Console.WriteLine($"Fetching {url}");
		string text = await http.GetStringAsync(url);
		await File.WriteAllTextAsync(Path.Combine(sources, file), text, Encoding.UTF8);
	}
}

string PathOf(string file) => paths.GetValueOrDefault(file) ?? Path.Combine(sources, file);

string[] missing = [.. files.Select(f => f.File).Where(file => !File.Exists(PathOf(file)))];
if (missing.Length > 0)
{
	await Console.Error.WriteLineAsync($"Missing: {string.Join(", ", missing.Select(PathOf))}. Run with --download, or pass each file:");
	foreach ((string file, Uri url) in files)
	{
		await Console.Error.WriteLineAsync($"  {file} from {url}");
	}

	return 1;
}

output ??= DefaultOutput();
if (output is null)
{
	await Console.Error.WriteLineAsync("Moka.Red.slnx wasn't found above this folder, so pass --out <path>.");
	return 1;
}

string emojiTest = await Read(EmojiTestFile);
(string? headerVersion, string? headerDate) = EmojiSources.ReadEmojiTestHeader(emojiTest);
string unicodeVersion = headerVersion ?? emojiVersion;

IReadOnlyList<EmojiTestEntry> entries = EmojiSources.ParseEmojiTest(emojiTest);
IReadOnlyDictionary<string, CldrAnnotation> cldr = EmojiSources.ParseCldr(await Read(CldrFile), await Read(CldrDerivedFile));
IReadOnlyDictionary<string, GemojiEntry> gemoji = EmojiSources.ParseGemoji(await Read(GemojiFile));
EmojiBuild build = EmojiSources.Build(entries, cldr, gemoji);

string json = EmojiSources.Write(build.Rows, unicodeVersion, starter: false);
await File.WriteAllTextAsync(output, json, new UTF8Encoding(false));

Console.WriteLine($"Wrote {build.Rows.Count} emoji to {output}");
Console.WriteLine($"  emoji-test.txt: Emoji {unicodeVersion}, {headerDate ?? "no date"}");
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  Size: {Encoding.UTF8.GetByteCount(json)} bytes"));
for (int index = 0; index < EmojiSources.Groups.Count; index++)
{
	int group = index;
	Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
		$"  {EmojiSources.Groups[group].Name}: {build.Rows.Count(r => r.Group == group)}"));
}

Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  Take a skin tone: {build.Rows.Count(r => r.Tone.Count > 0)}"));
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  Shortcode from the name (no gemoji alias): {build.FallbackShortcodes}"));
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  Name from emoji-test.txt (no CLDR name): {build.MissingNames}"));
return 0;

Task<string> Read(string file) => File.ReadAllTextAsync(PathOf(file), Encoding.UTF8);

static string Take(string[] arguments, ref int index)
{
	if (index + 1 >= arguments.Length)
	{
		throw new ArgumentException($"{arguments[index]} needs a value.");
	}

	return arguments[++index];
}

static string? DefaultOutput()
{
	foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
	{
		for (DirectoryInfo? folder = new(start); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "Moka.Red.slnx")))
			{
				return Path.Combine(folder.FullName, "src", "Moka.Red.Primitives", "wwwroot", "moka-emoji.en.json");
			}
		}
	}

	return null;
}

static void Usage()
{
	Console.WriteLine("Writes src/Moka.Red.Primitives/wwwroot/moka-emoji.en.json.");
	Console.WriteLine();
	Console.WriteLine("  --download                  fetch the four sources into the sources folder first");
	Console.WriteLine("  --sources <dir>             where the sources are (default: %TEMP%/moka-emoji-sources), named");
	Console.WriteLine("                              emoji-test.txt, annotations-en.xml, annotationsDerived-en.xml, emoji.json");
	Console.WriteLine("  --emoji-test <path>         emoji-test.txt somewhere else");
	Console.WriteLine("  --cldr-annotations <path>   CLDR common/annotations/en.xml somewhere else");
	Console.WriteLine("  --cldr-derived <path>       CLDR common/annotationsDerived/en.xml somewhere else");
	Console.WriteLine("  --gemoji-file <path>        gemoji db/emoji.json somewhere else");
	Console.WriteLine("  --emoji <v>                 Unicode emoji version to fetch (default latest)");
	Console.WriteLine("  --cldr <tag>                CLDR branch or release tag to fetch (default main)");
	Console.WriteLine("  --gemoji <ref>              gemoji branch or tag to fetch (default master)");
	Console.WriteLine("  --out <path>                the file to write (default: the one in Moka.Red.Primitives)");
}
