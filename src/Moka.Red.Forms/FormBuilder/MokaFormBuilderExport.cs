using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Moka.Red.Forms.FormBuilder;

/// <summary>
///     Import/export utilities for form builder field definitions.
///     Supports JSON schema and Blazor Razor markup generation.
/// </summary>
public static class MokaFormBuilderExport
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	/// <summary>Serializes the field list to a JSON schema string.</summary>
	/// <param name="fields">The form fields to serialize.</param>
	/// <returns>A formatted JSON string.</returns>
	public static string ToJson(IReadOnlyList<MokaFormField> fields)
		=> JsonSerializer.Serialize(fields, JsonOptions);

	/// <summary>Deserializes a JSON string back into a list of form fields.</summary>
	/// <param name="json">The JSON string to parse.</param>
	/// <returns>A list of form fields, or an empty list if parsing fails.</returns>
	public static IList<MokaFormField> FromJson(string json)
	{
		try
		{
			return JsonSerializer.Deserialize<List<MokaFormField>>(json, JsonOptions) ?? (IList<MokaFormField>)[];
		}
		catch (JsonException)
		{
			return [];
		}
	}

	/// <summary>Generates Blazor .razor markup using Moka.Red components.</summary>
	/// <param name="fields">The form fields to generate markup for.</param>
	/// <param name="columns">Number of grid columns for the form layout.</param>
	/// <returns>A complete Razor markup string.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="fields" /> is null.</exception>
	public static string ToRazor(IReadOnlyList<MokaFormField> fields, int columns = 1)
	{
		ArgumentNullException.ThrowIfNull(fields);

		var sb = new StringBuilder();
		sb.AppendLine("@using Moka.Red.Forms.TextField");
		sb.AppendLine("@using Moka.Red.Forms.TextArea");
		sb.AppendLine("@using Moka.Red.Forms.NumericField");
		sb.AppendLine("@using Moka.Red.Forms.PasswordField");
		sb.AppendLine("@using Moka.Red.Forms.Checkbox");
		sb.AppendLine("@using Moka.Red.Forms.Switch");
		sb.AppendLine("@using Moka.Red.Forms.SelectField");
		sb.AppendLine("@using Moka.Red.Forms.RadioGroup");
		sb.AppendLine("@using Moka.Red.Forms.DatePicker");
		sb.AppendLine("@using Moka.Red.Forms.TimePicker");
		sb.AppendLine("@using Moka.Red.Forms.FileUpload");
		sb.AppendLine("@using Moka.Red.Forms.Rating");
		sb.AppendLine("@using Moka.Red.Forms.Slider");
		sb.AppendLine("@using Moka.Red.Layout.Divider");
		sb.AppendLine("@using Moka.Red.Primitives.Typography");
		sb.AppendLine();

		if (columns > 1)
		{
			sb.Append(CultureInfo.InvariantCulture,
				$"<div style=\"display: grid; grid-template-columns: repeat({columns}, 1fr); gap: 1rem;\">");
			sb.AppendLine();
		}

		foreach (MokaFormField field in fields)
		{
			string indent = columns > 1 ? "\t" : "";
			string colStyle = field.ColSpan > 1 && columns > 1
				? string.Create(CultureInfo.InvariantCulture, $" style=\"grid-column: span {field.ColSpan}\"")
				: "";

			if (columns > 1 && field.ColSpan > 0)
			{
				sb.Append(CultureInfo.InvariantCulture, $"{indent}<div{colStyle}>");
				sb.AppendLine();
				indent += "\t";
			}

			sb.Append(CultureInfo.InvariantCulture, $"{indent}{FieldToRazor(field)}");
			sb.AppendLine();

			if (columns > 1 && field.ColSpan > 0)
			{
				indent = indent[..^1];
				sb.Append(CultureInfo.InvariantCulture, $"{indent}</div>");
				sb.AppendLine();
			}
		}

		if (columns > 1)
		{
			sb.AppendLine("</div>");
		}

		return sb.ToString();
	}

	private static string FieldToRazor(MokaFormField field)
	{
		var attrs = new StringBuilder();

		if (!string.IsNullOrWhiteSpace(field.Label) &&
		    field.Type is not MokaFormFieldType.Divider and not MokaFormFieldType.Heading)
		{
			attrs.Append(CultureInfo.InvariantCulture, $" Label=\"{Escape(field.Label)}\"");
		}

		if (!string.IsNullOrWhiteSpace(field.Placeholder))
		{
			attrs.Append(CultureInfo.InvariantCulture, $" Placeholder=\"{Escape(field.Placeholder)}\"");
		}

		if (!string.IsNullOrWhiteSpace(field.HelperText))
		{
			attrs.Append(CultureInfo.InvariantCulture, $" HelperText=\"{Escape(field.HelperText)}\"");
		}

		if (field.Required)
		{
			attrs.Append(" Required=\"true\"");
		}

		if (field.Disabled)
		{
			attrs.Append(" Disabled=\"true\"");
		}

		if (field.MaxLength.HasValue)
		{
			attrs.Append(CultureInfo.InvariantCulture, $" MaxLength=\"{field.MaxLength}\"");
		}

		return field.Type switch
		{
			MokaFormFieldType.TextField => string.Create(CultureInfo.InvariantCulture, $"<MokaTextField{attrs} />"),
			MokaFormFieldType.Email => string.Create(CultureInfo.InvariantCulture,
				$"<MokaTextField{attrs} Type=\"email\" />"),
			MokaFormFieldType.Phone => string.Create(CultureInfo.InvariantCulture, $"<MokaPhoneInput{attrs} />"),
			MokaFormFieldType.TextArea => string.Create(CultureInfo.InvariantCulture, $"<MokaTextArea{attrs} />"),
			MokaFormFieldType.NumericField => BuildNumeric(field, attrs),
			MokaFormFieldType.PasswordField => string.Create(CultureInfo.InvariantCulture,
				$"<MokaPasswordField{attrs} />"),
			MokaFormFieldType.Checkbox => string.Create(CultureInfo.InvariantCulture, $"<MokaCheckbox{attrs} />"),
			MokaFormFieldType.Switch => string.Create(CultureInfo.InvariantCulture, $"<MokaSwitch{attrs} />"),
			MokaFormFieldType.Select => BuildSelect(field, attrs),
			MokaFormFieldType.RadioGroup => BuildRadioGroup(field),
			MokaFormFieldType.DatePicker => string.Create(CultureInfo.InvariantCulture, $"<MokaDatePicker{attrs} />"),
			MokaFormFieldType.TimePicker => string.Create(CultureInfo.InvariantCulture, $"<MokaTimePicker{attrs} />"),
			MokaFormFieldType.FileUpload => string.Create(CultureInfo.InvariantCulture, $"<MokaFileUpload{attrs} />"),
			MokaFormFieldType.Rating => string.Create(CultureInfo.InvariantCulture, $"<MokaRating{attrs} />"),
			MokaFormFieldType.Slider => BuildSlider(field, attrs),
			MokaFormFieldType.Divider => "<MokaDivider />",
			MokaFormFieldType.Heading => string.Create(CultureInfo.InvariantCulture,
				$"<MokaHeading Level=\"3\">{Escape(field.Label)}</MokaHeading>"),
			_ => string.Create(CultureInfo.InvariantCulture, $"<!-- Unknown field type: {field.Type} -->")
		};
	}

	private static string BuildNumeric(MokaFormField field, StringBuilder attrs)
	{
		if (field.Min.HasValue)
		{
			attrs.Append(CultureInfo.InvariantCulture, $" Min=\"{field.Min}\"");
		}

		if (field.Max.HasValue)
		{
			attrs.Append(CultureInfo.InvariantCulture, $" Max=\"{field.Max}\"");
		}

		return string.Create(CultureInfo.InvariantCulture, $"<MokaNumericField{attrs} />");
	}

	private static string BuildSlider(MokaFormField field, StringBuilder attrs)
	{
		if (field.Min.HasValue)
		{
			attrs.Append(CultureInfo.InvariantCulture, $" Min=\"{field.Min}\"");
		}

		if (field.Max.HasValue)
		{
			attrs.Append(CultureInfo.InvariantCulture, $" Max=\"{field.Max}\"");
		}

		return string.Create(CultureInfo.InvariantCulture, $"<MokaSlider{attrs} />");
	}

	private static string BuildSelect(MokaFormField field, StringBuilder attrs)
	{
		if (field.Options is { Count: > 0 })
		{
			string items = string.Join(", ",
				field.Options.Select(o => string.Create(CultureInfo.InvariantCulture, $"\"{Escape(o)}\"")));
			attrs.Append(CultureInfo.InvariantCulture, $" Items=\"@(new[] {{ {items} }})\"");
		}

		return string.Create(CultureInfo.InvariantCulture, $"<MokaSelectField{attrs} />");
	}

	private static string BuildRadioGroup(MokaFormField field)
	{
		var sb = new StringBuilder();
		sb.Append("<MokaRadioGroup");
		if (!string.IsNullOrWhiteSpace(field.Label))
		{
			sb.Append(CultureInfo.InvariantCulture, $" Label=\"{Escape(field.Label)}\"");
		}

		sb.AppendLine(">");

		if (field.Options is { Count: > 0 })
		{
			foreach (string option in field.Options)
			{
				sb.Append(CultureInfo.InvariantCulture,
					$"\t<MokaRadioItem Value=\"\\\"{Escape(option)}\\\"\" Label=\"{Escape(option)}\" />");
			}

			sb.AppendLine();
		}

		sb.Append("</MokaRadioGroup>");
		return sb.ToString();
	}

	private static string Escape(string value)
		=> value.Replace("\"", "&quot;", StringComparison.Ordinal)
			.Replace("<", "&lt;", StringComparison.Ordinal)
			.Replace(">", "&gt;", StringComparison.Ordinal);
}
