namespace Moka.Red.Primitives.Diff;

/// <summary>Classifies a single line in a line-based diff.</summary>
public enum MokaDiffLineKind
{
	/// <summary>The line is present in both the old and new text.</summary>
	Unchanged,

	/// <summary>The line exists only in the new text.</summary>
	Added,

	/// <summary>The line exists only in the old text.</summary>
	Removed
}

/// <summary>
///     A single line of a line-based diff, as rendered by <see cref="MokaDiffViewer" />.
/// </summary>
/// <param name="Kind">How the line changed between the old and new text.</param>
/// <param name="Text">The raw line content, without a trailing newline.</param>
public sealed record MokaDiffLine(MokaDiffLineKind Kind, string Text);
