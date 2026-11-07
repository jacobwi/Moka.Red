using System.Text.Json;
using System.Text.Json.Serialization;

namespace Moka.Red.Forms.FormBuilder;

// Source-generated so the trimmer keeps MokaFormField's properties, which reflection-based JSON would lose.
[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	Converters = [typeof(CamelCaseFieldTypeConverter)])]
[JsonSerializable(typeof(IReadOnlyList<MokaFormField>))]
[JsonSerializable(typeof(List<MokaFormField>))]
internal sealed partial class FormBuilderJsonContext : JsonSerializerContext;

internal sealed class CamelCaseFieldTypeConverter()
	: JsonStringEnumConverter<MokaFormFieldType>(JsonNamingPolicy.CamelCase);
