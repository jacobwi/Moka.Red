using AngleSharp.Dom;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>What the tests of the charts drawn in rows and points share.</summary>
internal static class RowChartKit
{
	/// <summary>The value of one declaration in an element's style attribute, such as a mark's <c>--_dp-v</c>.</summary>
	/// <param name="element">The element.</param>
	/// <param name="name">The property.</param>
	public static string? Var(IElement element, string name)
	{
		ArgumentNullException.ThrowIfNull(element);
		string style = element.GetAttribute("style") ?? "";
		foreach (string part in style.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
		{
			int colon = part.IndexOf(':', StringComparison.Ordinal);
			if (colon > 0 && part[..colon].Trim() == name)
			{
				return part[(colon + 1)..].Trim();
			}
		}

		return null;
	}

	/// <summary>A percentage written by the chart, as a number.</summary>
	/// <param name="css">The value, such as <c>12.5%</c>.</param>
	public static double Percent(string? css) =>
		double.Parse((css ?? "NaN%").TrimEnd('%'), System.Globalization.CultureInfo.InvariantCulture);
}
