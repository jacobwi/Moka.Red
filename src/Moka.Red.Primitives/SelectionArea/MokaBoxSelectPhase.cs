namespace Moka.Red.Primitives.SelectionArea;

/// <summary>Where a box selection is when it reports.</summary>
public enum MokaBoxSelectPhase
{
	/// <summary>The press moved far enough to draw a box. Keep the selection as it is now, to combine with or put back.</summary>
	Started = 0,

	/// <summary>What the box covers changed while it is being dragged.</summary>
	Changed = 1,

	/// <summary>The button was released. This is what the box covers in the end.</summary>
	Finished = 2,

	/// <summary>Escape, a lost pointer or the box being turned off ended it. Put back the selection kept at the start.</summary>
	Cancelled = 3
}
