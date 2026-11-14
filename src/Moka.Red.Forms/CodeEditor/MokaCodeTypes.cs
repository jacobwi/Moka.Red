namespace Moka.Red.Forms.CodeEditor;

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
