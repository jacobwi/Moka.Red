using System.Globalization;
using System.Text;

namespace Moka.Red.Data.Chart;

/// <summary>Path data for chart lines, written in the invariant culture so an SVG attribute never carries a decimal comma.</summary>
internal static class MokaChartPaths
{
	/// <summary>
	///     Appends a smooth line through the points as cubic curves (monotone, after Fritsch and Carlson), so it never
	///     bulges above a peak or below a trough the way a plain spline does. Starts with a move to the first point.
	/// </summary>
	public static void AppendMonotone(StringBuilder path, ReadOnlySpan<(double X, double Y)> points)
	{
		int count = points.Length;
		if (count == 0)
		{
			return;
		}

		path.Append("M ").Append(Num(points[0].X)).Append(' ').Append(Num(points[0].Y));
		if (count == 1)
		{
			return;
		}

		double[] secants = new double[count - 1];
		double[] tangents = new double[count];
		for (int i = 0; i < count - 1; i++)
		{
			double dx = points[i + 1].X - points[i].X;
			secants[i] = dx > 0 ? (points[i + 1].Y - points[i].Y) / dx : 0;
		}

		tangents[0] = secants[0];
		tangents[count - 1] = secants[count - 2];
		for (int i = 1; i < count - 1; i++)
		{
			tangents[i] = secants[i - 1] * secants[i] <= 0 ? 0 : (secants[i - 1] + secants[i]) / 2;
		}

		for (int i = 0; i < count - 1; i++)
		{
			if (secants[i] == 0)
			{
				tangents[i] = 0;
				tangents[i + 1] = 0;
				continue;
			}

			double a = tangents[i] / secants[i];
			double b = tangents[i + 1] / secants[i];
			double sum = a * a + b * b;
			if (sum > 9)
			{
				double tau = 3 / Math.Sqrt(sum);
				tangents[i] = tau * a * secants[i];
				tangents[i + 1] = tau * b * secants[i];
			}
		}

		for (int i = 0; i < count - 1; i++)
		{
			(double x0, double y0) = points[i];
			(double x1, double y1) = points[i + 1];
			double third = (x1 - x0) / 3;
			path.Append(" C ").Append(Num(x0 + third)).Append(' ').Append(Num(y0 + tangents[i] * third))
				.Append(' ').Append(Num(x1 - third)).Append(' ').Append(Num(y1 - tangents[i + 1] * third))
				.Append(' ').Append(Num(x1)).Append(' ').Append(Num(y1));
		}
	}

	private static string Num(double value) => value.ToString("F2", CultureInfo.InvariantCulture);
}
