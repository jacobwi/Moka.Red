namespace Moka.Red.Core.Enums;

/// <summary>Border radius scale for components.</summary>
public enum MokaRounding
{
	/// <summary>No border radius (sharp corners).</summary>
	None,

	/// <summary>Small radius — var(--moka-radius-sm) = 0.125rem.</summary>
	Sm,

	/// <summary>Medium radius — var(--moka-radius-md) = 0.25rem. This is the default.</summary>
	Md,

	/// <summary>Large radius — var(--moka-radius-lg) = 0.375rem.</summary>
	Lg,

	/// <summary>Extra large radius — var(--moka-radius-xl) = 0.5rem.</summary>
	Xl,

	/// <summary>Full/pill radius — 9999px. Makes the element fully rounded.</summary>
	Full
}
