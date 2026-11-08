namespace Moka.Red.Data.FileBrowser;

internal static class MokaFileNames
{
	/// <summary>
	///     Ignores case and compares runs of digits by value, so <c>log2</c> sorts before <c>log10</c>. Leading zeros
	///     only decide between names otherwise equal. Text runs compare ordinally, since culture rules applied to pieces
	///     of a name can make the order intransitive.
	/// </summary>
	public static int Compare(string a, string b)
	{
		int i = 0;
		int j = 0;
		int zeros = 0;
		while (i < a.Length && j < b.Length)
		{
			int startA = i;
			int startB = j;
			int result;
			if (char.IsAsciiDigit(a[i]) && char.IsAsciiDigit(b[j]))
			{
				i = SkipRun(a, i, digits: true);
				j = SkipRun(b, j, digits: true);
				ReadOnlySpan<char> digitsA = a.AsSpan(startA, i - startA);
				ReadOnlySpan<char> digitsB = b.AsSpan(startB, j - startB);
				result = CompareNumbers(digitsA.TrimStart('0'), digitsB.TrimStart('0'));
				if (result == 0 && zeros == 0)
				{
					zeros = digitsA.Length.CompareTo(digitsB.Length);
				}
			}
			else
			{
				i = SkipRun(a, i, digits: false);
				j = SkipRun(b, j, digits: false);
				result = a.AsSpan(startA, i - startA).CompareTo(b.AsSpan(startB, j - startB), StringComparison.OrdinalIgnoreCase);
			}

			if (result != 0)
			{
				return result;
			}
		}

		int length = (a.Length - i).CompareTo(b.Length - j);
		return length != 0 ? length : zeros;
	}

	/// <summary>A name for something new: <paramref name="wanted" />, or with the first free number after it.</summary>
	public static string Free(string wanted, IEnumerable<string> taken)
	{
		HashSet<string> names = new(taken, StringComparer.OrdinalIgnoreCase);
		if (!names.Contains(wanted))
		{
			return wanted;
		}

		for (int number = 2; ; number++)
		{
			string candidate = FormattableString.Invariant($"{wanted} {number}");
			if (!names.Contains(candidate))
			{
				return candidate;
			}
		}
	}

	private static int SkipRun(string text, int index, bool digits)
	{
		while (index < text.Length && char.IsAsciiDigit(text[index]) == digits)
		{
			index++;
		}

		return index;
	}

	// Both runs have their leading zeros removed, so a longer run is a larger number.
	private static int CompareNumbers(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
	{
		int result = a.Length.CompareTo(b.Length);
		return result != 0 ? result : a.SequenceCompareTo(b);
	}
}
