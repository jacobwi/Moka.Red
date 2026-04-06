using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Icons;

namespace Moka.Red.Forms.FormBuilder;

/// <summary>
///     Visual form designer with a field palette, design canvas, and property editor.
///     Supports live preview and export to JSON or Blazor Razor markup.
/// </summary>
public partial class MokaFormBuilder
{
	// Used in .razor template — analyzer cannot see cross-file partial usage.
#pragma warning disable CA1823
	private static readonly FieldTypeInfo[] _fieldTypes =
	[
		new(MokaFormFieldType.TextField, "Text Field", MokaIcons.Action.Edit),
		new(MokaFormFieldType.TextArea, "Text Area", MokaIcons.File.FileText),
		new(MokaFormFieldType.NumericField, "Number", MokaIcons.Action.Edit),
		new(MokaFormFieldType.PasswordField, "Password", MokaIcons.Toggle.Lock),
		new(MokaFormFieldType.Email, "Email", MokaIcons.Action.Edit),
		new(MokaFormFieldType.Phone, "Phone", MokaIcons.Action.Edit),
		new(MokaFormFieldType.Checkbox, "Checkbox", MokaIcons.Status.Check),
		new(MokaFormFieldType.Switch, "Switch", MokaIcons.Toggle.Eye),
		new(MokaFormFieldType.Select, "Select", MokaIcons.Navigation.ChevronDown),
		new(MokaFormFieldType.RadioGroup, "Radio Group", MokaIcons.Status.CheckCircle),
		new(MokaFormFieldType.DatePicker, "Date Picker", MokaIcons.Status.Clock),
		new(MokaFormFieldType.TimePicker, "Time Picker", MokaIcons.Status.Clock),
		new(MokaFormFieldType.FileUpload, "File Upload", MokaIcons.Action.Upload),
		new(MokaFormFieldType.Rating, "Rating", MokaIcons.Toggle.Star),
		new(MokaFormFieldType.Slider, "Slider", MokaIcons.Action.Remove),
		new(MokaFormFieldType.Divider, "Divider", MokaIcons.Action.Remove),
		new(MokaFormFieldType.Heading, "Heading", MokaIcons.File.FileText)
	];
#pragma warning restore CA1823
	private MokaFormField? _selectedField;
	private string? _selectedFieldId;

	/// <summary>The list of form fields. Two-way bindable via <see cref="FieldsChanged" />.</summary>
	[Parameter]
	public IList<MokaFormField> Fields { get; set; } = new List<MokaFormField>();

	/// <summary>Fires when the fields list changes.</summary>
	[Parameter]
	public EventCallback<IList<MokaFormField>> FieldsChanged { get; set; }

	/// <summary>Fires with the exported code string when the user clicks Export.</summary>
	[Parameter]
	public EventCallback<string> OnExport { get; set; }

	/// <summary>Whether to show the live form preview. Default true.</summary>
	[Parameter]
	public bool ShowPreview { get; set; } = true;

	/// <summary>Whether to show the export button. Default true.</summary>
	[Parameter]
	public bool ShowExport { get; set; } = true;

	/// <summary>Number of grid columns for the form layout. Default 1.</summary>
	[Parameter]
	public int Columns { get; set; } = 1;

	/// <summary>Export format (JSON or Razor). Default Razor.</summary>
	[Parameter]
	public MokaFormExportFormat ExportFormat { get; set; } = MokaFormExportFormat.Razor;

	/// <inheritdoc />
	protected override string RootClass => "moka-form-builder";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	private string PreviewGridStyle => Columns > 1
		? $"display: grid; grid-template-columns: repeat({Columns}, 1fr); gap: var(--moka-spacing-md);"
		: "";

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private async Task AddField(MokaFormFieldType type)
	{
		var field = new MokaFormField
		{
			Type = type,
			Label = GetDefaultLabel(type),
			Options = HasOptions(type) ? ["Option 1", "Option 2", "Option 3"] : null
		};

		Fields.Add(field);
		SelectField(field);
		await FieldsChanged.InvokeAsync(Fields);
	}

	private void SelectField(MokaFormField field)
	{
		_selectedFieldId = field.Id;
		_selectedField = field;
	}

	private async Task RemoveField(MokaFormField field)
	{
		Fields.Remove(field);

		if (_selectedFieldId == field.Id)
		{
			_selectedFieldId = null;
			_selectedField = null;
		}

		await FieldsChanged.InvokeAsync(Fields);
	}

	private async Task MoveField(int index, int direction)
	{
		int newIndex = index + direction;
		if (newIndex < 0 || newIndex >= Fields.Count)
		{
			return;
		}

		MokaFormField item = Fields[index];
		Fields.RemoveAt(index);
		Fields.Insert(newIndex, item);
		await FieldsChanged.InvokeAsync(Fields);
	}

	private async Task UpdateProperty(Action update)
	{
		update();
		await FieldsChanged.InvokeAsync(Fields);
	}

	private async Task UpdateOptions(string? text)
	{
		if (_selectedField is null)
		{
			return;
		}

		_selectedField.Options = string.IsNullOrWhiteSpace(text)
			? null
			: text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

		await FieldsChanged.InvokeAsync(Fields);
	}

	private async Task HandleExport()
	{
		string output = ExportFormat switch
		{
			MokaFormExportFormat.Json => MokaFormBuilderExport.ToJson(Fields.ToList()),
			MokaFormExportFormat.Razor => MokaFormBuilderExport.ToRazor(Fields.ToList(), Columns),
			_ => ""
		};

		await OnExport.InvokeAsync(output);
	}

	// Called from .razor template — must be instance method.
#pragma warning disable CA1822
	private RenderFragment RenderFieldPreview(MokaFormField field) => builder =>
	{
		builder.OpenElement(0, "div");
		builder.AddAttribute(1, "class", "moka-form-builder__preview-field");

		switch (field.Type)
		{
			case MokaFormFieldType.Divider:
				builder.OpenElement(2, "hr");
				builder.AddAttribute(3, "class", "moka-form-builder__preview-divider");
				builder.CloseElement();
				break;

			case MokaFormFieldType.Heading:
				builder.OpenElement(2, "h3");
				builder.AddAttribute(3, "class", "moka-form-builder__preview-heading");
				builder.AddContent(4, field.Label);
				builder.CloseElement();
				break;

			case MokaFormFieldType.Checkbox:
			case MokaFormFieldType.Switch:
				builder.OpenElement(2, "label");
				builder.AddAttribute(3, "class", "moka-form-builder__preview-toggle");
				builder.OpenElement(4, "input");
				builder.AddAttribute(5, "type", "checkbox");
				builder.AddAttribute(6, "disabled", true);
				builder.CloseElement();
				builder.OpenElement(7, "span");
				builder.AddContent(8, field.Label);
				if (field.Required)
				{
					builder.OpenElement(9, "span");
					builder.AddAttribute(10, "class", "moka-form-builder__preview-required");
					builder.AddContent(11, " *");
					builder.CloseElement();
				}

				builder.CloseElement();
				builder.CloseElement();
				break;

			default:
				if (!string.IsNullOrWhiteSpace(field.Label))
				{
					builder.OpenElement(2, "label");
					builder.AddAttribute(3, "class", "moka-form-builder__preview-label");
					builder.AddContent(4, field.Label);
					if (field.Required)
					{
						builder.OpenElement(5, "span");
						builder.AddAttribute(6, "class", "moka-form-builder__preview-required");
						builder.AddContent(7, " *");
						builder.CloseElement();
					}

					builder.CloseElement();
				}

				builder.OpenElement(8, "div");
				builder.AddAttribute(9, "class", "moka-form-builder__preview-input");
				builder.AddContent(10, field.Placeholder ?? "");
				builder.CloseElement();

				if (!string.IsNullOrWhiteSpace(field.HelperText))
				{
					builder.OpenElement(11, "span");
					builder.AddAttribute(12, "class", "moka-form-builder__preview-helper");
					builder.AddContent(13, field.HelperText);
					builder.CloseElement();
				}

				break;
		}

		builder.CloseElement();
	};
#pragma warning restore CA1822

	private static string GetFieldColStyle(MokaFormField field) =>
		field.ColSpan > 1 ? $"grid-column: span {field.ColSpan}" : "";

	private static string GetDefaultLabel(MokaFormFieldType type) => type switch
	{
		MokaFormFieldType.TextField => "Text Field",
		MokaFormFieldType.TextArea => "Text Area",
		MokaFormFieldType.NumericField => "Number",
		MokaFormFieldType.PasswordField => "Password",
		MokaFormFieldType.Email => "Email",
		MokaFormFieldType.Phone => "Phone",
		MokaFormFieldType.Checkbox => "Checkbox",
		MokaFormFieldType.Switch => "Switch",
		MokaFormFieldType.Select => "Select",
		MokaFormFieldType.RadioGroup => "Radio Group",
		MokaFormFieldType.DatePicker => "Date",
		MokaFormFieldType.TimePicker => "Time",
		MokaFormFieldType.FileUpload => "File Upload",
		MokaFormFieldType.Rating => "Rating",
		MokaFormFieldType.Slider => "Slider",
		MokaFormFieldType.Divider => "",
		MokaFormFieldType.Heading => "Section",
		_ => "Field"
	};

	private static bool IsTextType(MokaFormFieldType type) =>
		type is MokaFormFieldType.TextField or MokaFormFieldType.TextArea
			or MokaFormFieldType.PasswordField or MokaFormFieldType.Email
			or MokaFormFieldType.Phone;

	private static bool IsNumericType(MokaFormFieldType type) =>
		type is MokaFormFieldType.NumericField or MokaFormFieldType.Slider;

	private static bool HasOptions(MokaFormFieldType type) =>
		type is MokaFormFieldType.Select or MokaFormFieldType.RadioGroup;

	private static bool IsLayoutType(MokaFormFieldType type) =>
		type is MokaFormFieldType.Divider or MokaFormFieldType.Heading;

	private static string OptionsToText(IList<string>? options) =>
		options is { Count: > 0 } ? string.Join("\n", options) : "";

	private sealed record FieldTypeInfo(MokaFormFieldType Type, string Label, MokaIconDefinition Icon);
}
