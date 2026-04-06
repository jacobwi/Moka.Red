namespace Moka.Red.Core.Enums;

/// <summary>Barcode symbology format.</summary>
public enum MokaBarcodeFormat
{
	/// <summary>Code 128 — supports full ASCII.</summary>
	Code128,

	/// <summary>Code 39 — alphanumeric plus some symbols.</summary>
	Code39,

	/// <summary>EAN-13 — 13-digit international article number.</summary>
	EAN13,

	/// <summary>EAN-8 — 8-digit international article number.</summary>
	EAN8,

	/// <summary>UPC-A — 12-digit Universal Product Code.</summary>
	UPC
}
