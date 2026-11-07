using System.Text.Json;
using System.Text.Json.Serialization;
using Moka.Red.Forms.FormBuilder;

namespace Moka.Red.Forms.Tests.Components;

// The export went from reflection to source-generated JSON so a trimmed app keeps its properties. A saved
// form is someone's data, so the text has to stay exactly what reflection wrote, and old text has to read back.
public class MokaFormBuilderExportTests
{
	private static readonly JsonSerializerOptions ReflectionOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private static readonly MokaFormField[] Fields =
	[
		new()
		{
			Id = "a1",
			Type = MokaFormFieldType.TextField,
			Label = "Name <required>",
			Placeholder = "Your name",
			Required = true,
			MaxLength = 40,
			ColSpan = 2
		},
		new()
		{
			Id = "b2",
			Type = MokaFormFieldType.TextArea,
			Label = "Bio",
			Disabled = true,
			Options = ["One", "Two"],
			Min = 1,
			Max = 9
		}
	];

	[Fact]
	public void ToJson_WritesWhatReflectionWrote() =>
		Assert.Equal(
			JsonSerializer.Serialize<IReadOnlyList<MokaFormField>>(Fields, ReflectionOptions),
			MokaFormBuilderExport.ToJson(Fields));

	[Fact]
	public void ToJson_UsesCamelCaseNamesAndEnumsAndLeavesNullsOut()
	{
		string json = MokaFormBuilderExport.ToJson(Fields);

		Assert.Contains("\"type\": \"textField\"", json, StringComparison.Ordinal);
		Assert.Contains("\"maxLength\": 40", json, StringComparison.Ordinal);
		Assert.DoesNotContain("defaultValue", json, StringComparison.Ordinal);
	}

	[Fact]
	public void FromJson_ReadsWhatReflectionWrote()
	{
		string saved = JsonSerializer.Serialize<IReadOnlyList<MokaFormField>>(Fields, ReflectionOptions);

		IList<MokaFormField> restored = MokaFormBuilderExport.FromJson(saved);

		Assert.Equal(2, restored.Count);
		Assert.Equal(MokaFormFieldType.TextArea, restored[1].Type);
		Assert.Equal(["One", "Two"], restored[1].Options!);
		Assert.Equal("Name <required>", restored[0].Label);
		Assert.Equal(2, restored[0].ColSpan);
	}

	[Theory]
	[InlineData("not json")]
	[InlineData("{\"id\": 1}")]
	public void FromJson_ReturnsNothingForWhatIsNotAFieldList(string json) =>
		Assert.Empty(MokaFormBuilderExport.FromJson(json));
}
