namespace Moka.Red.Core.Code;

/// <summary>A colored stretch of code, as a highlighter returns it for a <c>MokaCodeEditor</c> or a <c>MokaCodeBlock</c>.</summary>
/// <param name="Start">Where it starts, as an index into the whole text.</param>
/// <param name="Length">How many characters it covers.</param>
/// <param name="Kind">What it is.</param>
public readonly record struct MokaCodeSpan(int Start, int Length, MokaCodeTokenKind Kind);
