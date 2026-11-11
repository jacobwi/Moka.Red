namespace Moka.Red.Data.Chart;

/// <summary>A rectangle in the treemap's own units.</summary>
/// <param name="X">Its left edge.</param>
/// <param name="Y">Its top edge.</param>
/// <param name="Width">Its width.</param>
/// <param name="Height">Its height.</param>
internal readonly record struct TreemapRect(double X, double Y, double Width, double Height)
{
	/// <summary>The same rectangle, smaller by <paramref name="by" /> on every side, never below nothing.</summary>
	/// <param name="by">How much to take off each side.</param>
	public TreemapRect Inset(double by)
	{
		double x = Math.Min(by, Width / 2);
		double y = Math.Min(by, Height / 2);
		return new TreemapRect(X + x, Y + y, Width - 2 * x, Height - 2 * y);
	}
}

/// <summary>
///     The squarified treemap layout of Bruls, Huizing and van Wijk: values laid out in rows along the shorter side of
///     what is left, a row growing while that keeps its worst aspect ratio from getting worse, so tiles stay as close
///     to square as the values allow.
/// </summary>
internal static class TreemapLayout
{
	/// <summary>Lays values out in a rectangle, each tile's area its share of their sum.</summary>
	/// <param name="values">The values, in the order to place them; largest first gives the squarest tiles.</param>
	/// <param name="box">The rectangle to fill.</param>
	/// <returns>A rectangle per value, in the same order; a value of zero or less gets an empty one.</returns>
	public static TreemapRect[] Squarify(IReadOnlyList<double> values, TreemapRect box)
	{
		ArgumentNullException.ThrowIfNull(values);

		TreemapRect[] result = new TreemapRect[values.Count];
		double total = values.Where(v => v > 0 && double.IsFinite(v)).Sum();
		if (total <= 0 || box.Width <= 0 || box.Height <= 0)
		{
			return result;
		}

		double scale = box.Width * box.Height / total;
		List<int> order = [.. Enumerable.Range(0, values.Count).Where(i => values[i] > 0 && double.IsFinite(values[i]))];
		TreemapRect rest = box;
		int start = 0;
		while (start < order.Count)
		{
			double side = Math.Min(rest.Width, rest.Height);
			int end = start + 1;
			double worst = Worst(order, values, start, end, scale, side);
			while (end < order.Count)
			{
				double next = Worst(order, values, start, end + 1, scale, side);
				if (next > worst)
				{
					break;
				}

				worst = next;
				end++;
			}

			double rowArea = 0;
			for (int k = start; k < end; k++)
			{
				rowArea += values[order[k]] * scale;
			}

			bool last = end == order.Count;
			if (rest.Width >= rest.Height)
			{
				double width = last ? rest.Width : rowArea / rest.Height;
				double y = rest.Y;
				for (int k = start; k < end; k++)
				{
					double height = k == end - 1 ? rest.Y + rest.Height - y : values[order[k]] * scale / width;
					result[order[k]] = new TreemapRect(rest.X, y, width, height);
					y += height;
				}

				rest = new TreemapRect(rest.X + width, rest.Y, Math.Max(0, rest.Width - width), rest.Height);
			}
			else
			{
				double height = last ? rest.Height : rowArea / rest.Width;
				double x = rest.X;
				for (int k = start; k < end; k++)
				{
					double width = k == end - 1 ? rest.X + rest.Width - x : values[order[k]] * scale / height;
					result[order[k]] = new TreemapRect(x, rest.Y, width, height);
					x += width;
				}

				rest = new TreemapRect(rest.X, rest.Y + height, rest.Width, Math.Max(0, rest.Height - height));
			}

			start = end;
		}

		return result;
	}

	// The worst aspect ratio in a row laid along a side of this length.
	private static double Worst(List<int> order, IReadOnlyList<double> values, int start, int end, double scale, double side)
	{
		double sum = 0;
		double max = 0;
		double min = double.MaxValue;
		for (int k = start; k < end; k++)
		{
			double area = values[order[k]] * scale;
			sum += area;
			max = Math.Max(max, area);
			min = Math.Min(min, area);
		}

		double sideSquared = side * side;
		double sumSquared = sum * sum;
		return Math.Max(sideSquared * max / sumSquared, sumSquared / (sideSquared * min));
	}
}
