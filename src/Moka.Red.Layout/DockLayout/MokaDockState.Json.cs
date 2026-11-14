using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Moka.Red.Layout.DockLayout;

public sealed partial record MokaDockState
{
	// One step per format change: each takes the JSON of its version and gives back the next one's. A format change
	// adds a step here and raises FormatVersion, so a layout saved by any earlier release still loads.
	private static readonly Func<JsonObject, JsonObject>[] FormatSteps = [];

	/// <summary>Writes the state as JSON, with its <see cref="FormatVersion" />.</summary>
	/// <returns>The JSON.</returns>
	public string ToJson() =>
		JsonSerializer.Serialize(this with { Version = FormatVersion }, MokaDockJsonContext.Default.MokaDockState);

	/// <summary>
	///     Reads a state <see cref="ToJson" /> wrote, from this release or an earlier one: an older format is brought up
	///     to <see cref="FormatVersion" /> first.
	/// </summary>
	/// <param name="json">The JSON.</param>
	/// <returns>
	///     The state, or null for empty or malformed JSON, or for a format newer than this release knows, which a later
	///     release wrote: the layout then starts from its markup rather than guess.
	/// </returns>
	public static MokaDockState? FromJson(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}

		try
		{
			if (JsonNode.Parse(json) is not JsonObject document)
			{
				return null;
			}

			int version = document["version"] is JsonValue value && value.TryGetValue(out int v) ? v : 0;
			if (version < FormatVersion - FormatSteps.Length || version > FormatVersion)
			{
				return null;
			}

			for (int step = version; step < FormatVersion; step++)
			{
				document = FormatSteps[step - (FormatVersion - FormatSteps.Length)](document);
			}

			document["version"] = FormatVersion;
			return document.Deserialize(MokaDockJsonContext.Default.MokaDockState);
		}
		catch (JsonException)
		{
			return null;
		}
	}
}

[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	UseStringEnumConverter = true)]
[JsonSerializable(typeof(MokaDockState))]
internal sealed partial class MokaDockJsonContext : JsonSerializerContext;
