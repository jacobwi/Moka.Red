namespace Moka.Red.ContextMenu;

/// <summary>The surfaces of a <see cref="MokaRadialMenu" />. Each one works with every <see cref="MokaRadialShape" />.</summary>
public enum MokaRadialLook
{
	/// <summary>Raised buttons with a border and a shadow, in a band edged with the accent. The pointed one fills with the accent and glows.</summary>
	Standard,

	/// <summary>
	///     See-through buttons that blur the page behind them. The pointed one takes an accent tint instead of a fill.
	///     Costs a backdrop blur per button while the menu is open.
	/// </summary>
	Glass,

	/// <summary>Flat buttons on one raised disc, with no band. Only the pointed button has a color: it fills with the accent and grows.</summary>
	Solid,

	/// <summary>Thin rings and no fills over a light veil. The pointed button gets the accent ring the library uses for focus.</summary>
	Outline,

	/// <summary>Square-ish tiles with a monospace word under each icon, a dashed track, and an accent glow on the pointed one.</summary>
	Terminal,

	/// <summary>Borderless buttons on a plain band. The pointed one is tinted with the accent instead of filled.</summary>
	Soft
}
