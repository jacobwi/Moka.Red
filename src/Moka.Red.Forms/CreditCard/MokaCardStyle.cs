namespace Moka.Red.Forms.CreditCard;

/// <summary>
///     Visual style variants for the credit card preview.
/// </summary>
public enum MokaCardStyle
{
	/// <summary>
	///     Automatically changes gradient based on detected card type (Visa=blue, MC=orange, Amex=green,
	///     Discover=yellow).
	/// </summary>
	Auto,

	/// <summary>Dark gradient — deep charcoal to black.</summary>
	Dark,

	/// <summary>Light gradient — white/gray with dark text.</summary>
	Light,

	/// <summary>Neon gradient — vibrant purple to pink.</summary>
	Neon,

	/// <summary>Gold gradient — warm gold to amber.</summary>
	Gold,

	/// <summary>Platinum gradient — cool silver to steel.</summary>
	Platinum,

	/// <summary>Rose gradient — soft pink to rose.</summary>
	Rose,

	/// <summary>Ocean gradient — deep teal to cyan.</summary>
	Ocean,

	/// <summary>Sunset gradient — warm orange to red.</summary>
	Sunset,

	/// <summary>Minimal — flat surface color with subtle border, no gradient.</summary>
	Minimal,

	/// <summary>Custom — uses CustomCardBackground and CustomCardTextColor parameters.</summary>
	Custom
}
