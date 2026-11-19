namespace Moka.Red.Forms.SchemaForm;

/// <summary>What kind of control a <see cref="MokaSchemaField" /> is drawn with.</summary>
public enum MokaSchemaFieldKind
{
	/// <summary>A <c>"string"</c> property: a text field, a text area with <c>x-multiline</c>, a password field when write-only.</summary>
	Text,

	/// <summary>An <c>"integer"</c> property: a number field that takes whole numbers.</summary>
	WholeNumber,

	/// <summary>A <c>"number"</c> property: a number field.</summary>
	Number,

	/// <summary>A <c>"boolean"</c> property: a switch.</summary>
	Toggle,

	/// <summary>A property with <c>enum</c>, or with <c>oneOf</c> or <c>anyOf</c> of labelled constants: a select.</summary>
	Choice
}
