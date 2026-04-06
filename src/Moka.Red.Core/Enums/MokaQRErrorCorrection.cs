namespace Moka.Red.Core.Enums;

/// <summary>Error correction level for QR codes.</summary>
public enum MokaQRErrorCorrection
{
	/// <summary>~7% error recovery.</summary>
	Low,

	/// <summary>~15% error recovery.</summary>
	Medium,

	/// <summary>~25% error recovery.</summary>
	Quartile,

	/// <summary>~30% error recovery.</summary>
	High
}
