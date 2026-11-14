namespace Moka.Red.Data.Table;

/// <summary>Which edge a column stays at while the rows scroll sideways.</summary>
public enum MokaColumnPin
{
	/// <summary>Scrolls with the others.</summary>
	None,

	/// <summary>Stays at the start edge, left in a left-to-right page.</summary>
	Start,

	/// <summary>Stays at the end edge.</summary>
	End
}
