using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
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
	// Used in .razor template - analyzer cannot see cross-file partial usage.
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
	// The fields on screen, apart from Fields: changes go out through FieldsChanged as new lists, and an edited field
	// as a new field, never by editing what the parent passed, so a parent that keeps its list puts it back. _seen
	// holds what Fields contained when it was last read, so a parent that changes its list, in place or not, is picked
	// up, while a re-render passing the same fields again cannot undo an edit (gotcha #9).
	private readonly MokaInputValue<IList<MokaFormField>> _fields;
	private List<MokaFormField> _seen = [];

	// By reference: two fields may share an Id, and selecting one must not highlight both. An edit replaces the
	// selected field with a new one, so _selectedVersions holds the ones it replaced, oldest first, to find the
	// selection again in a list the parent passes or puts back.
	private MokaFormField? _selectedField;
	private readonly List<MokaFormField> _selectedVersions = [];

	/// <summary>Creates the builder. It shows its own fields until the parent passes a list.</summary>
	public MokaFormBuilder() => _fields = new MokaInputValue<IList<MokaFormField>>(ShowParentFields,
		DispatchExceptionAsync, new List<MokaFormField>());

	/// <summary>
	///     The list of form fields. Two-way bindable via <see cref="FieldsChanged" />. A change is reported as a new
	///     list, with an edited field as a new <see cref="MokaFormField" />; neither the list nor the fields passed in
	///     are edited.
	/// </summary>

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

	private IList<MokaFormField> ShownFields => _fields.Shown ?? [];

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		if (parameters.TryGetValue(nameof(Fields), out IList<MokaFormField>? fields))
		{
			IList<MokaFormField> given = fields ?? [];
			if (!given.SequenceEqual(_seen, ReferenceEqualityComparer.Instance))
			{
				_seen = [.. given];
			}

			if (_fields.Take(_seen))
			{
				FindSelection();
			}
		}

		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);
		_fields.AfterRender();
	}

	/// <inheritdoc />
	protected override ValueTask DisposeAsyncCore()
	{
		_fields.Stop();
		return base.DisposeAsyncCore();
	}

	private void ShowParentFields()
	{
		FindSelection();
		StateHasChanged();
	}

	// The newest version of the selected field the list holds stays selected, or nothing when it holds none.
	private void FindSelection()
	{
		MokaFormField? found = null;
		for (int i = _selectedVersions.Count - 1; i >= 0 && found is null; i--)
		{
			if (IndexOf(_selectedVersions[i]) >= 0)
			{
				found = _selectedVersions[i];
			}
		}

		Select(found);
	}

	private void Select(MokaFormField? field)
	{
		_selectedField = field;
		_selectedVersions.Clear();
		if (field is not null)
		{
			_selectedVersions.Add(field);
		}
	}

	private int IndexOf(MokaFormField field)
	{
		IList<MokaFormField> shown = ShownFields;
		for (int i = 0; i < shown.Count; i++)
		{
			if (ReferenceEquals(shown[i], field))
			{
				return i;
			}
		}

		return -1;
	}

	private string FieldItemCssClass(MokaFormField field) => new CssBuilder("moka-form-builder__field-item")
		.AddClass("moka-form-builder__field-item--selected", ReferenceEquals(field, _selectedField))
		.Build();

	// Blazor throws when two sibling rows carry the same @key, which the same field object listed
	// twice would. A key several rows share is dropped for all of them, so they render unkeyed.
	private object?[] FieldKeys()
	{
		IList<MokaFormField> shown = ShownFields;
		var keys = new object?[shown.Count];
		var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
		HashSet<object>? repeated = null;
		for (int i = 0; i < shown.Count; i++)
		{
			keys[i] = shown[i];
			if (!seen.Add(shown[i]))
			{
				(repeated ??= new HashSet<object>(ReferenceEqualityComparer.Instance)).Add(shown[i]);
			}
		}

		if (repeated is not null)
		{
			for (int i = 0; i < keys.Length; i++)
			{
				if (keys[i] is { } key && repeated.Contains(key))
				{
					keys[i] = null;
				}
			}
		}

		return keys;
	}

	private void Commit(List<MokaFormField> next) => _fields.Pick(next, FieldsChanged);

	private void AddField(MokaFormFieldType type)
	{
		var field = new MokaFormField
		{
			Type = type,
			Label = GetDefaultLabel(type),
			Options = HasOptions(type) ? ["Option 1", "Option 2", "Option 3"] : null
		};

		Select(field);
		Commit([.. ShownFields, field]);
	}

	private void SelectField(MokaFormField field) => Select(field);

	private void RemoveField(MokaFormField field)
	{
		if (ReferenceEquals(_selectedField, field))
		{
			Select(null);
		}

		Commit([.. ShownFields.Where(f => !ReferenceEquals(f, field))]);
	}

	private void MoveField(int index, int direction)
	{
		int newIndex = index + direction;
		if (newIndex < 0 || newIndex >= ShownFields.Count)
		{
			return;
		}

		List<MokaFormField> next = [.. ShownFields];
		MokaFormField item = next[index];
		next.RemoveAt(index);
		next.Insert(newIndex, item);
		Commit(next);
	}

	// The selected field is replaced by an edited copy, so the parent's own field is never changed.
	private void UpdateSelected(Action<MokaFormField> update)
	{
		if (_selectedField is not { } field)
		{
			return;
		}

		int index = IndexOf(field);
		if (index < 0)
		{
			return;
		}

		MokaFormField edited = field.Copy();
		update(edited);
		List<MokaFormField> next = [.. ShownFields];
		next[index] = edited;
		_selectedField = edited;
		_selectedVersions.Add(edited);
		Commit(next);
	}

	private void UpdateOptions(string? text) => UpdateSelected(field => field.Options = string.IsNullOrWhiteSpace(text)
		? null
		: text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList());

	private async Task HandleExport()
	{
		string output = ExportFormat switch
		{
			MokaFormExportFormat.Json => MokaFormBuilderExport.ToJson([.. ShownFields]),
			MokaFormExportFormat.Razor => MokaFormBuilderExport.ToRazor([.. ShownFields], Columns),

			_ => ""
		};

		await OnExport.InvokeAsync(output);
	}

	// Called from .razor template - must be instance method.
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

	// A number input reads and sends its value in the invariant culture. Some cultures write the minus
	// sign differently (U+2212 in Swedish, with a direction mark in Arabic), so in the current culture
	// a negative Min or Max would show as empty or be lost on the way back.
	private static string? InvariantNumber(int? value) => value?.ToString(CultureInfo.InvariantCulture);

	private static int? ParseNumber(object? value) =>
		int.TryParse(value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
			? number
			: null;

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
