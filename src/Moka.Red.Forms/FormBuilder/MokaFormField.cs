namespace Moka.Red.Forms.FormBuilder;

/// <summary>Data model representing a single field in a form builder layout.</summary>
public sealed class MokaFormField
{
	/// <summary>Unique identifier for the field.</summary>
	public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

	/// <summary>The type of form input this field represents.</summary>
	public MokaFormFieldType Type { get; set; }

	/// <summary>Display label for the field.</summary>
	public string Label { get; set; } = "New Field";

	/// <summary>Placeholder text shown when the field is empty.</summary>
	public string? Placeholder { get; set; }

	/// <summary>Helper text displayed below the field.</summary>
	public string? HelperText { get; set; }

	/// <summary>Whether the field is required for form submission.</summary>
	public bool Required { get; set; }

	/// <summary>Whether the field is disabled.</summary>
	public bool Disabled { get; set; }

	/// <summary>Maximum character length for text-based fields.</summary>
	public int? MaxLength { get; set; }

	/// <summary>Default value for the field.</summary>
	public string? DefaultValue { get; set; }

	/// <summary>Options for select, radio group, and similar multi-choice fields.</summary>
	public IList<string>? Options { get; set; }

	/// <summary>Minimum value for numeric and slider fields.</summary>
	public int? Min { get; set; }

	/// <summary>Maximum value for numeric and slider fields.</summary>
	public int? Max { get; set; }

	/// <summary>Number of grid columns this field spans (1 or 2).</summary>
	public int ColSpan { get; set; } = 1;
}
