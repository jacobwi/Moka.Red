namespace Moka.Red.Forms.ColorInput;

/// <summary>How <see cref="MokaColorInput" /> draws its field.</summary>
public enum MokaColorInputLook
{
	/// <summary>A text field with a small swatch and the browser's own color button. The original field.</summary>
	Standard,

	/// <summary>A round swatch with the hex beside it in mono. Clicking it opens the picker underneath.</summary>
	SwatchButton,

	/// <summary>Swatch, hex and an eyedropper in one bordered field. The swatch opens the picker.</summary>
	Split,

	/// <summary>A small pill with a dot and the hex, for toolbars and table cells. Clicking it opens the picker.</summary>
	Chip,

	/// <summary>Preset swatches and a Custom button that opens the picker, for tag and label colors.</summary>
	Palette,

	/// <summary>Mono uppercase hex beside a small glowing square, with an invalid state.</summary>
	Terminal
}
