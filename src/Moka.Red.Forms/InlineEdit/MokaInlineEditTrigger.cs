namespace Moka.Red.Forms.InlineEdit;

/// <summary>What starts editing a <see cref="MokaInlineEdit" />.</summary>
public enum MokaInlineEditTrigger
{
	/// <summary>A click on the text.</summary>
	Click,

	/// <summary>A double click on the text. Enter and Space on the focused text start it too.</summary>
	DoubleClick,

	/// <summary>Only the parent, through <c>Editing</c>. The text is then plain text, not a button.</summary>
	Manual
}
