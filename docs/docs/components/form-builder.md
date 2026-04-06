---
title: Form Builder
description: Visual form designer with drag-and-drop field palette, property editor, and export to JSON or Razor markup.
order: 64
---

# Form Builder

`MokaFormBuilder` is a visual form designer that lets users compose forms by selecting field types from a palette, reordering them, editing properties in a side panel, and exporting the result as JSON schema or Blazor `.razor` code.

## Parameters

### MokaFormBuilder

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Fields` | `IList<MokaFormField>` | `[]` | The list of fields in the form; supports two-way binding via `FieldsChanged` |
| `FieldsChanged` | `EventCallback<IList<MokaFormField>>` | -- | Callback invoked when the field list changes |
| `OnExport` | `EventCallback<string>` | -- | Callback invoked when the user clicks Export, passing the output string |
| `ShowPreview` | `bool` | `true` | Whether to show the live preview below the canvas |
| `ShowExport` | `bool` | `true` | Whether to show the export button |
| `Columns` | `int` | `1` | Number of grid columns in the form layout |
| `ExportFormat` | `MokaFormExportFormat` | `Json` | Export output format: `Json` or `Razor` |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaFormField

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Id` | `string` | auto-generated | Unique identifier for the field |
| `Type` | `MokaFormFieldType` | -- | The type of form input (TextField, TextArea, Select, etc.) |
| `Label` | `string` | `"New Field"` | Display label for the field |
| `Placeholder` | `string?` | -- | Placeholder text shown when the field is empty |
| `HelperText` | `string?` | -- | Helper text displayed below the field |
| `Required` | `bool` | `false` | Whether the field is required for form submission |
| `Disabled` | `bool` | `false` | Whether the field is disabled |
| `MaxLength` | `int?` | -- | Maximum character length for text-based fields |
| `DefaultValue` | `string?` | -- | Default value for the field |
| `Options` | `List<string>?` | -- | Options for Select, RadioGroup, and similar multi-choice fields |
| `Min` | `int?` | -- | Minimum value for numeric and slider fields |
| `Max` | `int?` | -- | Maximum value for numeric and slider fields |
| `ColSpan` | `int` | `1` | Number of grid columns this field spans |

### MokaFormFieldType Enum

| Value | Description |
|-------|-------------|
| `TextField` | Single-line text input |
| `TextArea` | Multi-line text area |
| `NumericField` | Numeric input with optional min/max |
| `PasswordField` | Password input with visibility toggle |
| `Email` | Email address input |
| `Phone` | Phone number input |
| `Checkbox` | Boolean checkbox toggle |
| `Switch` | Boolean switch toggle |
| `Select` | Dropdown select from a list of options |
| `RadioGroup` | Radio button group |
| `DatePicker` | Date picker input |
| `TimePicker` | Time picker input |
| `FileUpload` | File upload input |
| `Rating` | Star rating input |
| `Slider` | Range slider input |
| `Divider` | Visual divider (non-input) |
| `Heading` | Section heading (non-input) |

### MokaFormExportFormat Enum

| Value | Description |
|-------|-------------|
| `Json` | JSON schema representation |
| `Razor` | Blazor `.razor` markup using Moka.Red components |

## Basic Builder

Click a field type on the left to add it to the form canvas. Select a field to edit its properties.

```blazor-preview
<MokaFormBuilder />
```

## Pre-Populated Fields

Start with existing fields by binding the `Fields` parameter.

```blazor-preview
<MokaFormBuilder Fields="@_fields" ShowExport="false" />

@code {
    IList<MokaFormField> _fields = new List<MokaFormField>
    {
        new MokaFormField { Type = MokaFormFieldType.TextField, Label = "Full Name", Placeholder = "Jane Doe", Required = true },
        new MokaFormField { Type = MokaFormFieldType.Email, Label = "Email Address", Placeholder = "jane@example.com", Required = true },
        new MokaFormField { Type = MokaFormFieldType.Select, Label = "Role", Options = new() { "Admin", "Editor", "Viewer" } }
    };
}
```

## Export to Razor

Set `ExportFormat` to `Razor` to generate Blazor component markup.

```blazor-preview
<MokaFormBuilder ExportFormat="MokaFormExportFormat.Razor" OnExport="HandleExport" />

@if (!string.IsNullOrEmpty(_exportedCode))
{
    <MokaCallout Type="MokaCalloutType.Info" Title="Exported Razor Code" Style="margin-top: var(--moka-spacing-md); white-space: pre-wrap; font-size: var(--moka-font-size-xs);">
        @_exportedCode
    </MokaCallout>
}

@code {
    string? _exportedCode;
    void HandleExport(string code) => _exportedCode = code;
}
```
