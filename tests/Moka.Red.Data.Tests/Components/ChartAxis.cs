using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;

namespace Moka.Red.Data.Tests.Components;

/// <summary>
///     Reads how a chart's x-axis thins its labels. Each label carries a width class, and the chart's stylesheet hides
///     that class with a container query on an axis narrower than the width it names. bUnit applies no CSS, so the
///     queries are read as text.
/// </summary>
internal static partial class ChartAxis
{
	/// <summary>The width classes the stylesheet hides, each with the axis width in em it hides below.</summary>
	public static IReadOnlyDictionary<string, int> HiddenBelow(string file, string container)
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", file));
		Dictionary<string, int> widths = new(StringComparer.Ordinal);
		foreach (Match rule in Query().Matches(css))
		{
			if (rule.Groups["name"].Value == container)
			{
				widths[rule.Groups["class"].Value] = int.Parse(rule.Groups["em"].Value, CultureInfo.InvariantCulture);
			}
		}

		return widths;
	}

	/// <summary>The axis width a label needs, from its width class, or null when it always shows.</summary>
	public static int? WidthOf(IElement label, string prefix)
	{
		string? found = label.ClassList.FirstOrDefault(c => c.StartsWith($"{prefix}--w", StringComparison.Ordinal));
		return found is null ? null : int.Parse(found[(prefix.Length + 3)..], CultureInfo.InvariantCulture);
	}

	/// <summary>Where a label is centred, as a percentage of the axis's width.</summary>
	public static double LeftOf(IElement label)
	{
		string style = label.GetAttribute("style") ?? string.Empty;
		return double.Parse(LeftPercent().Match(style).Groups["left"].Value, CultureInfo.InvariantCulture);
	}

	[GeneratedRegex(@"left:\s*(?<left>[\d.]+)%")]
	private static partial Regex LeftPercent();

	[GeneratedRegex(@"@container\s+(?<name>[\w-]+)\s*\(width\s*<\s*(?<em>\d+)em\)\s*\{\s*\.(?<class>[\w-]+)\s*\{\s*display:\s*none;?\s*\}\s*\}")]
	private static partial Regex Query();
}
