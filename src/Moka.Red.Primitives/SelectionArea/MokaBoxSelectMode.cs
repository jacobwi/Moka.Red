namespace Moka.Red.Primitives.SelectionArea;

/// <summary>How what a box covers combines with the selection it started from, from the keys held when it started.</summary>
public enum MokaBoxSelectMode
{
	/// <summary>No modifier: the box's items are the selection.</summary>
	Replace = 0,

	/// <summary>Shift, with or without Control: the box's items join the selection.</summary>
	Add = 1,

	/// <summary>Control, or Command on a Mac: each of the box's items flips in or out of the selection.</summary>
	Toggle = 2
}
