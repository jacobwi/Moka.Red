namespace Moka.Red.Core.Enums;

/// <summary>Barcode symbology format.</summary>
public enum MokaBarcodeFormat
{
	/// <summary>Code 128 Subset B. Supports printable ASCII.</summary>
	Code128,

	/// <summary>Code 39. Digits, uppercase letters, space and the symbols - . $ / + %.</summary>
	Code39,

	/// <summary>EAN-13. 13-digit international article number (12 data digits plus a check digit).</summary>
	EAN13,

	/// <summary>EAN-8. 8-digit international article number (7 data digits plus a check digit).</summary>
	EAN8,

	/// <summary>UPC-A. 12-digit Universal Product Code (11 data digits plus a check digit).</summary>
	UPC
}
