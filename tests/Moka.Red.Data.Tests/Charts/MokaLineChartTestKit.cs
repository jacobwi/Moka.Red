using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>What the line chart tests share: series, and reading the marks back out of the svg.</summary>
internal static partial class MokaLineChartTestKit
{
	/// <summary>A series with these values.</summary>
	public static MokaLineSeries Series(string name, params double[] values) => new() { Name = name, Values = values };

	/// <summary>The svg a modern line chart draws its marks in, in its first plot.</summary>
	public static IElement Marks(IElement root) => root.QuerySelector("svg.moka-linechart__marks")
	                                                ?? throw new InvalidOperationException("No marks.");

	/// <summary>The series groups in drawing order, by key.</summary>
	public static IReadOnlyList<string> GroupKeys(IElement root) =>
		[.. Marks(root).QuerySelectorAll("g.moka-linechart__series").Select(g => g.GetAttribute("data-moka-key") ?? string.Empty)];

	/// <summary>A series group by its key.</summary>
	public static IElement Group(IElement root, string key) =>
		Marks(root).QuerySelectorAll("g.moka-linechart__series").FirstOrDefault(g => g.GetAttribute("data-moka-key") == key)
		?? throw new InvalidOperationException($"No group {key}.");

	/// <summary>The path data of a series' part, such as <c>moka-linechart__line</c>.</summary>
	public static string? PathOf(IElement group, string part) => group.QuerySelector($"path.{part}")?.GetAttribute("d");

	/// <summary>Every point of a path, in the marks' units, with the command that reached it.</summary>
	public static IReadOnlyList<(char Command, int X, int Y)> Points(string? path)
	{
		List<(char, int, int)> points = [];
		if (string.IsNullOrEmpty(path))
		{
			return points;
		}

		char command = 'M';
		int x = 0;
		int y = 0;
		foreach (Match token in Token().Matches(path))
		{
			string text = token.Value;
			if (char.IsLetter(text[0]))
			{
				command = text[0];
				continue;
			}

			switch (command)
			{
				case 'H':
					x = int.Parse(text, CultureInfo.InvariantCulture);
					points.Add(('H', x, y));
					break;
				case 'V':
					y = int.Parse(text, CultureInfo.InvariantCulture);
					points.Add(('V', x, y));
					break;
				case 'h':
					points.Add(('h', x, y));
					break;
				default:
					string[] pair = text.Split(',');
					x = int.Parse(pair[0], CultureInfo.InvariantCulture);
					y = int.Parse(pair[1], CultureInfo.InvariantCulture);
					points.Add((command, x, y));
					break;
			}
		}

		return points;
	}

	/// <summary>How many separate stretches a path draws: one per move.</summary>
	public static int Moves(string? path) => path is null ? 0 : path.Count(c => c == 'M');

	[GeneratedRegex(@"[MLHVCZhz]|-?\d+,-?\d+|-?\d+")]
	private static partial Regex Token();
}
