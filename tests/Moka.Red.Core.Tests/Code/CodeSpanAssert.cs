using Moka.Red.Core.Code;

namespace Moka.Red.Core.Tests.Code;

/// <summary>Checks on a highlighter's spans, and what kind a piece of the code came out as.</summary>
internal static class CodeSpanAssert
{
	/// <summary>Spans in order, none empty, none overlapping, all inside the text.</summary>
	public static void Valid(string code, IReadOnlyList<MokaCodeSpan> spans, string? source = null)
	{
		ArgumentNullException.ThrowIfNull(code);
		ArgumentNullException.ThrowIfNull(spans);
		int position = 0;
		for (int i = 0; i < spans.Count; i++)
		{
			MokaCodeSpan span = spans[i];
			string where = $"{source ?? "code"}: span {i} ({span.Start}, {span.Length}, {span.Kind})";
			Assert.True(span.Length > 0, $"{where} is empty.");
			Assert.True(span.Start >= position, $"{where} starts before the previous span ends at {position}.");
			Assert.True(span.Start + span.Length <= code.Length, $"{where} runs past the end at {code.Length}.");
			position = span.Start + span.Length;
		}
	}

	/// <summary>Highlights the code, checks the spans and returns them.</summary>
	public static IReadOnlyList<MokaCodeSpan> Highlight(Func<string, IEnumerable<MokaCodeSpan>> highlighter, string code)
	{
		List<MokaCodeSpan> spans = [.. highlighter(code)];
		Valid(code, spans);
		return spans;
	}

	/// <summary>
	///     The kind every character of the <paramref name="occurrence" />th <paramref name="fragment" /> came out as, Plain
	///     where no span covers it. Fails when its characters differ.
	/// </summary>
	public static MokaCodeTokenKind KindOf(string code, IReadOnlyList<MokaCodeSpan> spans, string fragment, int occurrence = 0)
	{
		ArgumentNullException.ThrowIfNull(code);
		ArgumentNullException.ThrowIfNull(fragment);
		int index = -1;
		for (int i = 0; i <= occurrence; i++)
		{
			index = code.IndexOf(fragment, index + 1, StringComparison.Ordinal);
			Assert.True(index >= 0, $"'{fragment}' (occurrence {occurrence}) isn't in the code.");
		}

		MokaCodeTokenKind first = KindAt(spans, index);
		for (int i = index + 1; i < index + fragment.Length; i++)
		{
			MokaCodeTokenKind kind = KindAt(spans, i);
			Assert.True(kind == first, $"'{fragment}' is {first} at its start and {kind} at offset {i - index}.");
		}

		return first;
	}

	private static MokaCodeTokenKind KindAt(IReadOnlyList<MokaCodeSpan> spans, int position)
	{
		foreach (MokaCodeSpan span in spans)
		{
			if (position >= span.Start && position < span.Start + span.Length)
			{
				return span.Kind;
			}
		}

		return MokaCodeTokenKind.Plain;
	}
}
