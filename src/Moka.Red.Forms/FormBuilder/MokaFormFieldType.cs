namespace Moka.Red.Forms.FormBuilder;

/// <summary>Available field types for the form builder.</summary>
public enum MokaFormFieldType
{
	/// <summary>Single-line text input.</summary>
	TextField,

	/// <summary>Multi-line text area.</summary>
	TextArea,

	/// <summary>Numeric input with optional min/max/step.</summary>
	NumericField,

	/// <summary>Password input with visibility toggle.</summary>
	PasswordField,

	/// <summary>Email address input.</summary>
	Email,

	/// <summary>Phone number input.</summary>
	Phone,

	/// <summary>Boolean checkbox toggle.</summary>
	Checkbox,

	/// <summary>Boolean switch toggle.</summary>
	Switch,

	/// <summary>Dropdown select from a list of options.</summary>
	Select,

	/// <summary>Radio button group from a list of options.</summary>
	RadioGroup,

	/// <summary>Date picker input.</summary>
	DatePicker,

	/// <summary>Time picker input.</summary>
	TimePicker,

	/// <summary>File upload input.</summary>
	FileUpload,

	/// <summary>Star rating input.</summary>
	Rating,

	/// <summary>Range slider input.</summary>
	Slider,

	/// <summary>Visual divider (non-input).</summary>
	Divider,

	/// <summary>Section heading (non-input).</summary>
	Heading
}
