namespace Moka.Red.Forms.ColorPicker;

/// <summary>The WCAG 2 level a contrast ratio between text and its background meets.</summary>
public enum MokaContrastLevel
{
	/// <summary>Below 3:1. Not enough even for large text or icons.</summary>
	Fails,

	/// <summary>3:1 or more. Enough for large text, icons and borders, not for body text.</summary>
	AaLarge,

	/// <summary>4.5:1 or more. Enough for body text.</summary>
	Aa,

	/// <summary>7:1 or more. The enhanced level for body text.</summary>
	Aaa
}
