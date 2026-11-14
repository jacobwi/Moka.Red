namespace Moka.Red.Data.Table;

/// <summary>When an actions column shows its buttons.</summary>
public enum MokaActionsReveal
{
	/// <summary>Always.</summary>
	Always,

	/// <summary>
	///     While the pointer is over the row or focus is inside it, by CSS alone, and always on a touch screen, which has
	///     no hover.
	/// </summary>
	OnHover
}
