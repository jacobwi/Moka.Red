namespace Moka.Red.Forms.InlineEdit;

/// <summary>What a <see cref="MokaInlineEdit" /> selects when it opens.</summary>
public enum MokaInlineEditSelection
{
	/// <summary>All of the text, so typing replaces it.</summary>
	All,

	/// <summary>
	///     The part of a file name before its extension. All of the text when the name has no extension or starts with
	///     its only dot, as <c>.gitignore</c> does.
	/// </summary>
	Stem,

	/// <summary>Nothing, with the caret after the text.</summary>
	End
}
