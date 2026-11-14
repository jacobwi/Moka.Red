namespace Moka.Red.Core.Code;

/// <summary>
///     One highlighter run over a text: the spans found so far, in order, and how deep the scanners are nested. Razor
///     nests C# in markup in C# as deep as the text does, so past <see cref="MaxDepth" /> a construct that would nest
///     again is left uncolored rather than taking more stack.
/// </summary>
internal sealed class CodeScan(string text)
{
	private const int MaxDepth = 32;

	public string Text { get; } = text;

	public List<MokaCodeSpan> Spans { get; } = [];

	public bool CanNest => Depth < MaxDepth;

	private int Depth { get; set; }

	public void Enter() => Depth++;

	public void Leave() => Depth--;

	/// <summary>Marks the text from <paramref name="start" /> up to <paramref name="end" />, joining a span of the same kind that ends there.</summary>
	public void Mark(int start, int end, MokaCodeTokenKind kind)
	{
		if (end <= start)
		{
			return;
		}

		if (Spans.Count > 0)
		{
			MokaCodeSpan last = Spans[^1];
			if (last.Kind == kind && last.Start + last.Length == start)
			{
				Spans[^1] = last with { Length = end - last.Start };
				return;
			}
		}

		Spans.Add(new MokaCodeSpan(start, end - start, kind));
	}
}
