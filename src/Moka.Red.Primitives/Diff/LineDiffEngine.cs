namespace Moka.Red.Primitives.Diff;

/// <summary>
///     Computes a line-based diff between two texts via a longest-common-subsequence walk.
///     The LCS table is O(n*m), so inputs above <see cref="MaxLinesPerSide" /> lines per side
///     skip it and fall back to reporting the whole old text as removed and the whole new
///     text as added.
/// </summary>
internal static class LineDiffEngine
{
	private const int MaxLinesPerSide = 2000;

	/// <summary>Computes the diff lines projecting <paramref name="oldText" /> onto <paramref name="newText" />.</summary>
	internal static IReadOnlyList<MokaDiffLine> Compute(string? oldText, string? newText)
	{
		var left = SplitLines(oldText);
		var right = SplitLines(newText);

		var n = left.Length;
		var m = right.Length;

		if (n > MaxLinesPerSide || m > MaxLinesPerSide)
		{
			return NaiveDiff(left, right);
		}

		// dp[i][j] = length of the LCS of left[..i] and right[..j]. Jagged rather than
		// multidimensional (CA1814) - rows are dense so nothing is wasted.
		var dp = new int[n + 1][];
		for (var i = 0; i <= n; i++)
		{
			dp[i] = new int[m + 1];
		}

		for (var i = 0; i < n; i++)
		{
			for (var j = 0; j < m; j++)
			{
				dp[i + 1][j + 1] = string.Equals(left[i], right[j], StringComparison.Ordinal)
					? dp[i][j] + 1
					: Math.Max(dp[i + 1][j], dp[i][j + 1]);
			}
		}

		// Walk the table backwards, then reverse to get document order.
		var output = new List<MokaDiffLine>(n + m);
		var x = n;
		var y = m;

		while (x > 0 && y > 0)
		{
			if (string.Equals(left[x - 1], right[y - 1], StringComparison.Ordinal))
			{
				output.Add(new MokaDiffLine(MokaDiffLineKind.Unchanged, left[x - 1]));
				x--;
				y--;
			}
			else if (dp[x][y - 1] >= dp[x - 1][y])
			{
				output.Add(new MokaDiffLine(MokaDiffLineKind.Added, right[y - 1]));
				y--;
			}
			else
			{
				output.Add(new MokaDiffLine(MokaDiffLineKind.Removed, left[x - 1]));
				x--;
			}
		}

		while (x > 0)
		{
			output.Add(new MokaDiffLine(MokaDiffLineKind.Removed, left[x - 1]));
			x--;
		}

		while (y > 0)
		{
			output.Add(new MokaDiffLine(MokaDiffLineKind.Added, right[y - 1]));
			y--;
		}

		output.Reverse();
		return output;
	}

	private static string[] SplitLines(string? text) =>
		(text ?? string.Empty).ReplaceLineEndings("\n").Split('\n');

	private static List<MokaDiffLine> NaiveDiff(string[] left, string[] right)
	{
		var output = new List<MokaDiffLine>(left.Length + right.Length);

		foreach (var line in left)
		{
			output.Add(new MokaDiffLine(MokaDiffLineKind.Removed, line));
		}

		foreach (var line in right)
		{
			output.Add(new MokaDiffLine(MokaDiffLineKind.Added, line));
		}

		return output;
	}
}
