using System.Buffers;

namespace Moka.Red.Feedback.CommandPalette;

internal static class MokaCommandSearch
{
	private const int Letter = 16;
	private const int WordStart = 8;
	private const int TextStart = 2;
	private const int Run = 6;
	private const int GapOpen = 3;
	private const int GapExtend = 1;
	private const int TitleWeight = 2;
	private const int StackLimit = 256;
	private const int None = int.MinValue / 4;

	public static bool Contains(MokaCommand command, string query) =>
		command.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
		(command.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
		(command.Keywords?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
		(command.Group?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);

	public static string FuzzyQuery(string typed) =>
		string.Concat(typed.Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();

	public static int? Fuzzy(MokaCommand command, string query)
	{
		int title = Score(command.Title, query);
		int best = title == None ? None : title * TitleWeight;
		best = Math.Max(best, Score(command.Description, query));
		best = Math.Max(best, Score(command.Keywords, query));
		best = Math.Max(best, Score(command.Group, query));
		return best == None ? null : best;
	}

	// The best placement of the query's letters in the text, in order: a letter scores, more when it starts a word or
	// follows the letter before it, and every letter skipped between two matches costs a little. Two rows of the
	// dynamic programme are enough, since each letter only looks back at the one before.
	internal static int Score(string? text, string query)
	{
		if (string.IsNullOrEmpty(text) || query.Length == 0 || query.Length > text.Length || !IsSubsequence(text, query))
		{
			return None;
		}

		int length = text.Length;
		int[]? rented = null;
		Span<int> rows = length * 2 <= StackLimit
			? stackalloc int[length * 2]
			: (rented = ArrayPool<int>.Shared.Rent(length * 2)).AsSpan(0, length * 2);

		try
		{
			Span<int> previous = rows[..length];
			Span<int> current = rows[length..];

			for (int at = 0; at < length; at++)
			{
				previous[at] = Same(text[at], query[0]) ? Letter + Bonus(text, at) : None;
			}

			for (int letter = 1; letter < query.Length; letter++)
			{
				char wanted = query[letter];
				int gap = None;
				for (int at = 0; at < length; at++)
				{
					if (at >= 2)
					{
						int opened = previous[at - 2] == None ? None : previous[at - 2] - GapOpen;
						int extended = gap == None ? None : gap - GapExtend;
						gap = Math.Max(opened, extended);
					}

					int score = None;
					if (Same(text[at], wanted))
					{
						int adjacent = at >= 1 && previous[at - 1] != None ? previous[at - 1] + Run : None;
						int from = Math.Max(adjacent, gap);
						if (from != None)
						{
							score = from + Letter + Bonus(text, at);
						}
					}

					current[at] = score;
				}

				Span<int> swap = previous;
				previous = current;
				current = swap;
			}

			int best = None;
			foreach (int score in previous)
			{
				best = Math.Max(best, score);
			}

			return best;
		}
		finally
		{
			if (rented is not null)
			{
				ArrayPool<int>.Shared.Return(rented);
			}
		}
	}

	private static bool IsSubsequence(string text, string query)
	{
		int found = 0;
		for (int at = 0; at < text.Length && found < query.Length; at++)
		{
			if (Same(text[at], query[found]))
			{
				found++;
			}
		}

		return found == query.Length;
	}

	private static bool Same(char letter, char upper) => char.ToUpperInvariant(letter) == upper;

	private static int Bonus(string text, int at)
	{
		if (at == 0)
		{
			return WordStart + TextStart;
		}

		char before = text[at - 1];
		return !char.IsLetterOrDigit(before) || (char.IsLower(before) && char.IsUpper(text[at])) ? WordStart : 0;
	}
}
