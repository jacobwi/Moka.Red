using System.Text;
using System.Text.RegularExpressions;

namespace Moka.Red.DevApp.Components.Library;

internal sealed record Release(string Version, string Date, string Line, string Component, string Href, string Where);

// The dev app embeds the repo's CHANGELOG.md, since a running sample has no other way to the file.
internal static partial class ReleaseNotes
{
	public const string ChangelogHref = "https://github.com/jacobwi/Moka.Red/blob/master/CHANGELOG.md";

	private static readonly Lazy<IReadOnlyList<Release>> Data = new(Load);

	public static IReadOnlyList<Release> Minor(string prefix) =>
		[.. Data.Value.Where(release => release.Version.StartsWith(prefix, StringComparison.Ordinal))];

	[GeneratedRegex(@"^## \[(?<version>[^\]]+)\](?: - (?<date>\S+))?")]
	private static partial Regex Heading();

	[GeneratedRegex(@"Moka(?<name>[A-Z][A-Za-z0-9]+)")]
	private static partial Regex ComponentName();

	[GeneratedRegex(@"`([^`]*)`")]
	private static partial Regex Code();

	[GeneratedRegex(@"(?<=[a-z0-9\)])\.\s")]
	private static partial Regex SentenceEnd();

	private static List<Release> Load()
	{
		using Stream? stream = typeof(ReleaseNotes).Assembly.GetManifestResourceStream("CHANGELOG.md");
		if (stream is null)
		{
			return [];
		}

		using var reader = new StreamReader(stream, Encoding.UTF8);
		string[] lines = reader.ReadToEnd().Split('\n').Select(line => line.TrimEnd('\r')).ToArray();

		var releases = new List<Release>();
		for (int i = 0; i < lines.Length; i++)
		{
			Match heading = Heading().Match(lines[i]);
			if (!heading.Success || heading.Groups["version"].Value == "Unreleased")
			{
				continue;
			}

			int end = Array.FindIndex(lines, i + 1, line => line.StartsWith("## ", StringComparison.Ordinal));
			string[] section = lines[(i + 1)..(end < 0 ? lines.Length : end)];
			releases.Add(Describe(heading.Groups["version"].Value, heading.Groups["date"].Value, section));
		}

		return releases;
	}

	private static Release Describe(string version, string date, string[] section)
	{
		List<string> bullets = Bullets(section);
		int added = Array.FindIndex(section, line => line.StartsWith("### Added", StringComparison.Ordinal));
		string first = added >= 0 ? Bullets(section[added..]).FirstOrDefault() ?? string.Empty : bullets.FirstOrDefault() ?? string.Empty;
		if (first.Length == 0)
		{
			first = string.Join(' ', section.Where(line => line.Length > 0 && !line.StartsWith('#'))).Trim();
		}

		string sentence = FirstSentence(Code().Replace(first, "$1"));
		string href = LibraryCatalog.AllHref;
		string where = "All components";
		string named = "All components";
		foreach (Match match in ComponentName().Matches(first + " " + string.Join(' ', section)))
		{
			string name = match.Groups["name"].Value;
			if (LibraryCatalog.Shown(name, exactOnly: true) is { } shown)
			{
				href = shown.Href;
				where = shown.Where;
				named = name;
				break;
			}
		}

		return new Release(version, date, sentence, named, href, where);
	}

	// A bullet is a line starting "- " at the margin, plus the indented lines that carry on from it.
	private static List<string> Bullets(string[] lines)
	{
		var bullets = new List<string>();
		StringBuilder? current = null;
		foreach (string line in lines)
		{
			if (line.StartsWith("- ", StringComparison.Ordinal))
			{
				Flush();
				current = new StringBuilder(line[2..].Trim());
			}
			else if (current is not null && line.StartsWith("  ", StringComparison.Ordinal)
				&& !line.TrimStart().StartsWith("- ", StringComparison.Ordinal))
			{
				current.Append(' ').Append(line.Trim());
			}
			else
			{
				Flush();
			}
		}

		Flush();
		return bullets;

		void Flush()
		{
			if (current is { Length: > 0 })
			{
				bullets.Add(current.ToString());
			}

			current = null;
		}
	}

	private static string FirstSentence(string text)
	{
		Match end = SentenceEnd().Match(text);
		string sentence = end.Success ? text[..(end.Index + 1)] : text;
		return sentence.TrimEnd('.', ':', ' ');
	}
}
