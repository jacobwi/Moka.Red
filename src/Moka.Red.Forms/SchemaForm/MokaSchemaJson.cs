using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Moka.Red.Forms.SchemaForm;

internal static class MokaSchemaJson
{
	// A missing property, JSON null and empty text all mean a setting that isn't set.
	internal static bool IsUnset(JsonNode? node) =>
		node is null || node.GetValueKind() switch
		{
			JsonValueKind.Null => true,
			JsonValueKind.String => TryGetText(node, out string? text) && text.Length == 0,
			_ => false
		};

	internal static bool TryGetText(JsonNode node, [NotNullWhen(true)] out string? text)
	{
		if (node is not JsonValue value || value.GetValueKind() != JsonValueKind.String)
		{
			text = null;
			return false;
		}

		if (value.TryGetValue(out text))
		{
			return true;
		}

		// A value built in code keeps the CLR type it was made from: a char or a Guid is text in JSON, but
		// TryGetValue<string> refuses it.
		text = JsonNode.Parse(value.ToJsonString())!.GetValue<string>();
		return true;
	}

	internal static bool TryGetNumber(JsonNode node, out double number, out string raw)
	{
		if (node is not JsonValue || node.GetValueKind() != JsonValueKind.Number)
		{
			number = 0;
			raw = string.Empty;
			return false;
		}

		// Read from the JSON text, since a value built in code holds whichever number type it was made from.
		raw = node.ToJsonString();
		return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && double.IsFinite(number);
	}

	internal static bool IsPrimitive(JsonNode node) =>
		node.GetValueKind() is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False;

	// What makes two choices the same: text compared exactly, numbers by value, so 1 and 1.0 match. Null for anything
	// that can't be a choice.
	internal static string? KeyOf(JsonNode node)
	{
		if (TryGetText(node, out string? text))
		{
			return "s:" + text;
		}

		if (!TryGetNumber(node, out double number, out string raw))
		{
			return null;
		}

		// Dividing by one with the most decimals a decimal holds drops trailing zeros, so 2.50 and 2.5 read the same.
		return decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal exact)
			? "n:" + (exact / 1.0000000000000000000000000000m).ToString(CultureInfo.InvariantCulture)
			: "n:" + number.ToString("R", CultureInfo.InvariantCulture);
	}

	internal static string Display(JsonNode node) => TryGetText(node, out string? text) ? text : node.ToJsonString();

	internal static JsonElement ToElement(JsonNode node)
	{
		using JsonDocument document = JsonDocument.Parse(node.ToJsonString());
		return document.RootElement.Clone();
	}
}
