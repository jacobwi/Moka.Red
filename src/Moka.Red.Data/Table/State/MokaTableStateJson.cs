using System.Text.Json.Serialization;

namespace Moka.Red.Data.Table;

[JsonSourceGenerationOptions(
	UseStringEnumConverter = true,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MokaTableState))]
internal sealed partial class MokaTableStateJson : JsonSerializerContext;
