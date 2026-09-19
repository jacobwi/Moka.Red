namespace Moka.Red.Primitives.Diff;

/// <summary>
///     Computes a line-based diff between two texts with Myers' O(ND) algorithm in its linear-space
///     form (the "middle snake" divide and conquer from Myers' 1986 paper). Time grows with the size
///     of the texts times the number of changed lines, and memory with the size of the texts only.
///     <para>
///         The result keeps as many lines unchanged as possible. Where several such diffs exist,
///         removed lines come before added lines inside a change, and a block of only added or only
///         removed lines is moved as far down as the text allows, the way git and GNU diff place it.
///     </para>
///     <para>
///         Work is capped at <see cref="MaxWork" /> steps so a huge, very different pair of texts
///         cannot freeze a render. Past the cap, the parts not matched yet show as removed then added.
///     </para>
/// </summary>
internal static class LineDiffEngine
{
	/// <summary>
	///     Step budget for the matching, about 100 ms of server CPU. Ordinary edits to files of tens of
	///     thousands of lines stay far below it. Texts that differ almost everywhere while sharing many
	///     repeated lines, or a block of thousands of lines moved, can reach it. The old LCS table spent
	///     about as long on two 2,000-line texts, its cap.
	/// </summary>
	internal const long MaxWork = 10_000_000;

	/// <summary>Computes the diff lines projecting <paramref name="oldText" /> onto <paramref name="newText" />.</summary>
	internal static IReadOnlyList<MokaDiffLine> Compute(string? oldText, string? newText) =>
		Compute(oldText, newText, MaxWork);

	/// <summary>Same as <see cref="Compute(string?, string?)" /> with a custom step budget.</summary>
	internal static IReadOnlyList<MokaDiffLine> Compute(string? oldText, string? newText, long maxWork)
	{
		string[] left = SplitLines(oldText);
		string[] right = SplitLines(newText);

		// Equal lines get equal numbers, so the inner loops compare ints instead of strings.
		var ids = new Dictionary<string, int>(StringComparer.Ordinal);
		int[] a = Encode(left, ids);
		int[] b = Encode(right, ids);

		bool[] removed = new bool[a.Length];
		bool[] added = new bool[b.Length];
		int emptyId = ids.TryGetValue(string.Empty, out int id) ? id : -1;
		MarkChanges(a, b, ids.Count, emptyId, removed, added, maxWork);

		List<MokaDiffLine> lines = Merge(left, right, removed, added);
		SlideBlocksDown(lines);
		return lines;
	}

	private static string[] SplitLines(string? text) =>
		(text ?? string.Empty).ReplaceLineEndings("\n").Split('\n');

	private static int[] Encode(string[] lines, Dictionary<string, int> ids)
	{
		int[] encoded = new int[lines.Length];
		for (int i = 0; i < lines.Length; i++)
		{
			if (!ids.TryGetValue(lines[i], out int id))
			{
				id = ids.Count;
				ids.Add(lines[i], id);
			}

			encoded[i] = id;
		}

		return encoded;
	}

	/// <summary>Sets <paramref name="removed" /> and <paramref name="added" /> for every line that is not kept.</summary>
	private static void MarkChanges(int[] a, int[] b, int idCount, int emptyId, bool[] removed, bool[] added,
		long maxWork)
	{
		int endA = a.Length;
		int endB = b.Length;

		// A text that ends with a newline ends with an empty line. When both do, those two lines stay
		// matched, so a block appended at the end reads like git shows it, not as a block that ends
		// with an added empty line.
		if (endA > 0 && endB > 0 && a[endA - 1] == emptyId && b[endB - 1] == emptyId)
		{
			endA--;
			endB--;
		}

		// The common start and end are taken first, so an edit next to a repeated line (a closing
		// brace, a blank line) is matched in place rather than somewhere in the middle.
		int start = 0;
		while (start < endA && start < endB && a[start] == b[start])
		{
			start++;
		}

		while (endA > start && endB > start && a[endA - 1] == b[endB - 1])
		{
			endA--;
			endB--;
		}

		// A line that never occurs on the other side cannot be kept, so it is marked now and left out
		// of the search. Rewritten lines are usually unique, which keeps the search small.
		int[] countA = new int[idCount];
		int[] countB = new int[idCount];
		for (int i = start; i < endA; i++)
		{
			countA[a[i]]++;
		}

		for (int j = start; j < endB; j++)
		{
			countB[b[j]]++;
		}

		var keptA = new List<int>(endA - start);
		for (int i = start; i < endA; i++)
		{
			if (countB[a[i]] > 0)
			{
				keptA.Add(i);
			}
			else
			{
				removed[i] = true;
			}
		}

		var keptB = new List<int>(endB - start);
		for (int j = start; j < endB; j++)
		{
			if (countA[b[j]] > 0)
			{
				keptB.Add(j);
			}
			else
			{
				added[j] = true;
			}
		}

		new MiddleSnakeDiff(a, b, keptA, keptB, removed, added, maxWork).Run();
	}

	/// <summary>
	///     Myers' linear-space diff over the kept lines. Positions inside it index the kept lists, and
	///     marking translates them back to line numbers.
	/// </summary>
	private sealed class MiddleSnakeDiff
	{
		private readonly int[] _a;
		private readonly bool[] _added;
		private readonly int[] _b;
		private readonly List<int> _keptA;
		private readonly List<int> _keptB;
		private readonly long _maxWork;
		private readonly bool[] _removed;

		// Forward and reverse furthest-reaching x per diagonal. One pair serves every call, because
		// a split is found before the recursion that uses the next one.
		private readonly int[] _forward;
		private readonly int[] _reverse;
		private long _work;

		internal MiddleSnakeDiff(int[] a, int[] b, List<int> keptA, List<int> keptB, bool[] removed, bool[] added,
			long maxWork)
		{
			_a = new int[keptA.Count];
			for (int i = 0; i < keptA.Count; i++)
			{
				_a[i] = a[keptA[i]];
			}

			_b = new int[keptB.Count];
			for (int j = 0; j < keptB.Count; j++)
			{
				_b[j] = b[keptB[j]];
			}

			_keptA = keptA;
			_keptB = keptB;
			_removed = removed;
			_added = added;
			_maxWork = maxWork;

			int capacity = _a.Length + _b.Length + 3;
			_forward = new int[capacity];
			_reverse = new int[capacity];
		}

		internal void Run() => Diff(0, _a.Length, 0, _b.Length);

		private void Diff(int aLo, int aHi, int bLo, int bHi)
		{
			while (aLo < aHi && bLo < bHi && _a[aLo] == _b[bLo])
			{
				aLo++;
				bLo++;
			}

			while (aLo < aHi && bLo < bHi && _a[aHi - 1] == _b[bHi - 1])
			{
				aHi--;
				bHi--;
			}

			if (aLo == aHi || bLo == bHi || !TryFindSplit(aLo, aHi, bLo, bHi, out int splitA, out int splitB))
			{
				// One side is used up, or the budget is: whatever is left here is a change.
				MarkRemoved(aLo, aHi);
				MarkAdded(bLo, bHi);
				return;
			}

			// Both halves need at most half the edits of this range, so the depth stays near log2 of
			// the edit count.
			Diff(aLo, splitA, bLo, splitB);
			Diff(splitA, aHi, splitB, bHi);
		}

		/// <summary>
		///     Runs the forward and reverse searches until they overlap on a diagonal and returns a point
		///     on a shortest path through the range. Returns false when the step budget runs out first.
		/// </summary>
		private bool TryFindSplit(int aLo, int aHi, int bLo, int bHi, out int splitA, out int splitB)
		{
			splitA = 0;
			splitB = 0;
			if (_work > _maxWork)
			{
				return false;
			}

			int n = aHi - aLo;
			int m = bHi - bLo;
			int maxD = (n + m + 1) / 2;
			int offset = maxD;
			int length = (2 * maxD) + 2;

			Array.Fill(_forward, -1, 0, length);
			Array.Fill(_reverse, -1, 0, length);
			_forward[offset + 1] = 0;
			_reverse[offset + 1] = 0;
			_work += length;

			int delta = n - m;

			// With an odd delta the paths can only meet during a forward step, with an even one only
			// during a reverse step.
			bool meetForward = (delta & 1) != 0;

			// Diagonals that ran off the edge of the range are skipped from then on.
			int k1Start = 0;
			int k1End = 0;
			int k2Start = 0;
			int k2End = 0;

			for (int d = 0; d < maxD; d++)
			{
				if (_work > _maxWork)
				{
					break;
				}

				for (int k1 = -d + k1Start; k1 <= d - k1End; k1 += 2)
				{
					int k1Offset = offset + k1;
					int x1 = k1 == -d || (k1 != d && _forward[k1Offset - 1] < _forward[k1Offset + 1])
						? _forward[k1Offset + 1]
						: _forward[k1Offset - 1] + 1;
					int y1 = x1 - k1;
					int snakeStart = x1;
					while (x1 < n && y1 < m && _a[aLo + x1] == _b[bLo + y1])
					{
						x1++;
						y1++;
					}

					_work += 1 + x1 - snakeStart;
					_forward[k1Offset] = x1;

					if (x1 > n)
					{
						k1End += 2;
					}
					else if (y1 > m)
					{
						k1Start += 2;
					}
					else if (meetForward)
					{
						int k2Offset = offset + delta - k1;
						if (k2Offset >= 0 && k2Offset < length && _reverse[k2Offset] != -1 && x1 >= n - _reverse[k2Offset])
						{
							splitA = aLo + x1;
							splitB = bLo + y1;
							return true;
						}
					}
				}

				for (int k2 = -d + k2Start; k2 <= d - k2End; k2 += 2)
				{
					int k2Offset = offset + k2;

					// The reverse search walks from the end, with x and y counted from the far corner.
					int x2 = k2 == -d || (k2 != d && _reverse[k2Offset - 1] < _reverse[k2Offset + 1])
						? _reverse[k2Offset + 1]
						: _reverse[k2Offset - 1] + 1;
					int y2 = x2 - k2;
					int snakeStart = x2;
					while (x2 < n && y2 < m && _a[aHi - x2 - 1] == _b[bHi - y2 - 1])
					{
						x2++;
						y2++;
					}

					_work += 1 + x2 - snakeStart;
					_reverse[k2Offset] = x2;

					if (x2 > n)
					{
						k2End += 2;
					}
					else if (y2 > m)
					{
						k2Start += 2;
					}
					else if (!meetForward)
					{
						int k1Offset = offset + delta - k2;
						if (k1Offset >= 0 && k1Offset < length && _forward[k1Offset] != -1)
						{
							int x1 = _forward[k1Offset];
							int y1 = offset + x1 - k1Offset;
							if (x1 >= n - x2)
							{
								splitA = aLo + x1;
								splitB = bLo + y1;
								return true;
							}
						}
					}
				}
			}

			return false;
		}

		private void MarkRemoved(int lo, int hi)
		{
			for (int i = lo; i < hi; i++)
			{
				_removed[_keptA[i]] = true;
			}
		}

		private void MarkAdded(int lo, int hi)
		{
			for (int j = lo; j < hi; j++)
			{
				_added[_keptB[j]] = true;
			}
		}
	}

	/// <summary>
	///     Walks both texts in order. Between two kept lines it lists the removed lines first, then the
	///     added ones.
	/// </summary>
	private static List<MokaDiffLine> Merge(string[] left, string[] right, bool[] removed, bool[] added)
	{
		var output = new List<MokaDiffLine>(left.Length + right.Length);
		int i = 0;
		int j = 0;

		while (i < left.Length || j < right.Length)
		{
			while (i < left.Length && removed[i])
			{
				output.Add(new MokaDiffLine(MokaDiffLineKind.Removed, left[i++]));
			}

			while (j < right.Length && added[j])
			{
				output.Add(new MokaDiffLine(MokaDiffLineKind.Added, right[j++]));
			}

			if (i < left.Length && j < right.Length)
			{
				output.Add(new MokaDiffLine(MokaDiffLineKind.Unchanged, left[i]));
				i++;
				j++;
			}
			else
			{
				// Both sides keep the same number of unchanged lines, so this only runs at the end.
				// Anything left is shown as changed rather than dropped.
				while (i < left.Length)
				{
					output.Add(new MokaDiffLine(MokaDiffLineKind.Removed, left[i++]));
				}

				while (j < right.Length)
				{
					output.Add(new MokaDiffLine(MokaDiffLineKind.Added, right[j++]));
				}
			}
		}

		return output;
	}

	/// <summary>
	///     Moves each block of only added or only removed lines down while the kept line after it
	///     equals its first line. Both placements are equally short, but the lower one reads as the
	///     edit: a function appended after another shows as one block, not as a block that starts
	///     with the previous function's closing brace.
	/// </summary>
	private static void SlideBlocksDown(List<MokaDiffLine> lines)
	{
		// From the last block to the first, so a block only ever stops against one that is already
		// where it will stay.
		int end = lines.Count;
		while (end > 0)
		{
			if (lines[end - 1].Kind == MokaDiffLineKind.Unchanged)
			{
				end--;
				continue;
			}

			int start = end - 1;
			while (start > 0 && lines[start - 1].Kind != MokaDiffLineKind.Unchanged)
			{
				start--;
			}

			if (IsSingleKind(lines, start, end))
			{
				MokaDiffLineKind kind = lines[start].Kind;
				int blockStart = start;
				int blockEnd = end;
				while (CanSlideDown(lines, blockStart, blockEnd))
				{
					// The kept line and the block's first line have the same text, so swapping which of
					// them counts as kept leaves both texts as they were.
					lines[blockEnd] = new MokaDiffLine(kind, lines[blockEnd].Text);
					lines[blockStart] = new MokaDiffLine(MokaDiffLineKind.Unchanged, lines[blockStart].Text);
					blockStart++;
					blockEnd++;
				}
			}

			end = start;
		}
	}

	private static bool CanSlideDown(List<MokaDiffLine> lines, int blockStart, int blockEnd)
	{
		if (blockEnd >= lines.Count
		    || lines[blockEnd].Kind != MokaDiffLineKind.Unchanged
		    || !string.Equals(lines[blockEnd].Text, lines[blockStart].Text, StringComparison.Ordinal))
		{
			return false;
		}

		// A kept empty last line is the final newline of both texts, which stays where it is.
		if (blockEnd == lines.Count - 1)
		{
			return lines[blockEnd].Text.Length > 0;
		}

		// A block that reached the next change would merge with it into one.
		return lines[blockEnd + 1].Kind == MokaDiffLineKind.Unchanged;
	}

	private static bool IsSingleKind(List<MokaDiffLine> lines, int start, int end)
	{
		for (int i = start + 1; i < end; i++)
		{
			if (lines[i].Kind != lines[start].Kind)
			{
				return false;
			}
		}

		return true;
	}
}
