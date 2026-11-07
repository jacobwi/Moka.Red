namespace Moka.Red.Forms.CodeEditor;

/// <summary>What a stretch of text is, which decides its color in a <see cref="MokaCodeEditor" />.</summary>
public enum MokaCodeTokenKind
{
	/// <summary>Ordinary text, in the editor's text color.</summary>
	Plain,

	/// <summary>A section or heading, such as <c>[Interface]</c>.</summary>
	Heading,

	/// <summary>A language keyword.</summary>
	Keyword,

	/// <summary>The name in a name and value pair, or a property.</summary>
	Key,

	/// <summary>The value in a name and value pair.</summary>
	Value,

	/// <summary>A string, or another literal such as an address.</summary>
	Literal,

	/// <summary>A number.</summary>
	Number,

	/// <summary>A constant, such as a key or a hash.</summary>
	Constant,

	/// <summary>A comment.</summary>
	Comment,

	/// <summary>An operator or punctuation.</summary>
	Punctuation
}

/// <summary>A colored stretch of a <see cref="MokaCodeEditor" />'s text.</summary>
/// <param name="Start">Where it starts, as an index into the whole text.</param>
/// <param name="Length">How many characters it covers.</param>
/// <param name="Kind">What it is.</param>
public readonly record struct MokaCodeSpan(int Start, int Length, MokaCodeTokenKind Kind);

/// <summary>How serious a <see cref="MokaCodeDiagnostic" /> is.</summary>
public enum MokaCodeSeverity
{
	/// <summary>Worth knowing.</summary>
	Info,

	/// <summary>Probably a mistake.</summary>
	Warning,

	/// <summary>A mistake.</summary>
	Error
}

/// <summary>A problem found in a <see cref="MokaCodeEditor" />'s text: underlined in place and listed below it.</summary>
/// <param name="Severity">How serious it is.</param>
/// <param name="Message">What is wrong, in a sentence.</param>
/// <param name="Line">The line it is on, from 1. 0 for a problem with the whole text.</param>
/// <param name="Column">The column it starts at, from 1. 0 marks the whole line.</param>
/// <param name="Length">How many characters it covers. 0 runs to the end of the line.</param>
/// <param name="Code">A short identifier shown beside it, such as "WG012".</param>
public sealed record MokaCodeDiagnostic(
	MokaCodeSeverity Severity,
	string Message,
	int Line = 0,
	int Column = 0,
	int Length = 0,
	string? Code = null);
