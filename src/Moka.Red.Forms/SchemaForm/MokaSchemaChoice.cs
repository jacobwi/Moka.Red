using System.Text.Json;
using System.Text.Json.Nodes;

namespace Moka.Red.Forms.SchemaForm;

/// <summary>
///     One choice of a <see cref="MokaSchemaFieldKind.Choice" /> field: a value from <c>enum</c>, or a <c>const</c> from
///     <c>oneOf</c> or <c>anyOf</c> with its <c>title</c> as the label.
/// </summary>
public sealed class MokaSchemaChoice
{
	internal MokaSchemaChoice(JsonNode node, string key, string label)
	{
		Node = node;
		Key = key;
		Label = label;
		Value = MokaSchemaJson.ToElement(node);
	}

	/// <summary>The value the setting takes when this choice is picked: text or a number.</summary>
	public JsonElement Value { get; }

	/// <summary>What the select shows: the choice's <c>title</c>, else its value.</summary>
	public string Label { get; }

	internal JsonNode Node { get; }

	internal string Key { get; }
}
