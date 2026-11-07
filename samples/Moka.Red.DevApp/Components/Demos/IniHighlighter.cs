using System.Globalization;
using System.Net;
using Moka.Red.Forms.CodeEditor;

namespace Moka.Red.DevApp.Components.Demos;

// A small INI highlighter and checker for the code editor demo. An app passes its own.
internal static class IniHighlighter
{
	public static IEnumerable<MokaCodeSpan> Spans(string text)
	{
		int offset = 0;
		foreach (string line in text.Split('\n'))
		{
			foreach (MokaCodeSpan span in Line(line, offset))
			{
				yield return span;
			}

			offset += line.Length + 1;
		}
	}

	public static IReadOnlyList<MokaCodeDiagnostic> Check(string text)
	{
		List<MokaCodeDiagnostic> problems = [];
		string[] lines = text.Split('\n');
		for (int i = 0; i < lines.Length; i++)
		{
			string line = lines[i].Trim();
			if (line.Length == 0 || line[0] is ';' or '#' or '[')
			{
				continue;
			}

			int equals = lines[i].IndexOf('=', StringComparison.Ordinal);
			if (equals < 0)
			{
				problems.Add(new MokaCodeDiagnostic(MokaCodeSeverity.Error, "A setting needs an equals sign and a value.", i + 1, 0, 0, "INI001"));
				continue;
			}

			string key = lines[i][..equals].Trim();
			string value = lines[i][(equals + 1)..].Trim();
			if (value.Length == 0)
			{
				problems.Add(new MokaCodeDiagnostic(MokaCodeSeverity.Warning, $"{key} has no value.", i + 1, 0, 0, "INI002"));
			}
			else if (key.Equals("port", StringComparison.OrdinalIgnoreCase) && !int.TryParse(value, CultureInfo.InvariantCulture, out _))
			{
				int column = lines[i].IndexOf(value, equals, StringComparison.Ordinal) + 1;
				problems.Add(new MokaCodeDiagnostic(MokaCodeSeverity.Error, "A port is a number from 1 to 65535.", i + 1, column, value.Length, "INI003"));
			}
		}

		return problems;
	}

	private static IEnumerable<MokaCodeSpan> Line(string line, int offset)
	{
		string trimmed = line.TrimStart();
		int start = line.Length - trimmed.Length;
		if (trimmed.Length == 0)
		{
			yield break;
		}

		if (trimmed[0] is ';' or '#')
		{
			yield return new MokaCodeSpan(offset + start, trimmed.Length, MokaCodeTokenKind.Comment);
			yield break;
		}

		if (trimmed[0] == '[')
		{
			yield return new MokaCodeSpan(offset + start, trimmed.TrimEnd().Length, MokaCodeTokenKind.Heading);
			yield break;
		}

		int equals = line.IndexOf('=', StringComparison.Ordinal);
		if (equals < 0)
		{
			yield return new MokaCodeSpan(offset + start, trimmed.Length, MokaCodeTokenKind.Key);
			yield break;
		}

		yield return new MokaCodeSpan(offset + start, equals - start, MokaCodeTokenKind.Key);
		yield return new MokaCodeSpan(offset + equals, 1, MokaCodeTokenKind.Punctuation);

		string value = line[(equals + 1)..].Trim();
		if (value.Length == 0)
		{
			yield break;
		}

		int valueStart = line.IndexOf(value, equals + 1, StringComparison.Ordinal);
		MokaCodeTokenKind kind = value switch
		{
			"true" or "false" => MokaCodeTokenKind.Keyword,
			_ when double.TryParse(value, CultureInfo.InvariantCulture, out _) => MokaCodeTokenKind.Number,
			_ when value.StartsWith('"') => MokaCodeTokenKind.Literal,
			_ when IPAddress.TryParse(value, out _) => MokaCodeTokenKind.Literal,
			_ => MokaCodeTokenKind.Value
		};
		yield return new MokaCodeSpan(offset + valueStart, value.Length, kind);
	}
}
