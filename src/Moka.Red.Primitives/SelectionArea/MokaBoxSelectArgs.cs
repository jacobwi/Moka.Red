namespace Moka.Red.Primitives.SelectionArea;

/// <summary>What a box selection covers, reported while it is dragged and once when it ends.</summary>
public sealed class MokaBoxSelectArgs
{
	/// <summary>Creates a report, for testing a handler.</summary>
	/// <param name="phase">Where the box selection is.</param>
	/// <param name="mode">How it combines with the selection it started from.</param>
	/// <param name="keys">The keys of the drawn items the box touches, in page order.</param>
	/// <param name="range">The items covered according to the geometry, or null.</param>
	public MokaBoxSelectArgs(MokaBoxSelectPhase phase, MokaBoxSelectMode mode, IReadOnlyList<string> keys, MokaBoxSelectRange? range = null)
	{
		ArgumentNullException.ThrowIfNull(keys);
		Phase = phase;
		Mode = mode;
		Keys = keys;
		Range = range;
	}

	/// <summary>Where the box selection is.</summary>
	public MokaBoxSelectPhase Phase { get; }

	/// <summary>How the box combines with the selection it started from, from the keys held when it started.</summary>
	public MokaBoxSelectMode Mode { get; }

	/// <summary>The keys of the drawn items the box touches, in page order. Empty when it was cancelled.</summary>
	public IReadOnlyList<string> Keys { get; }

	/// <summary>
	///     With a <see cref="MokaBoxSelectGeometry" />, the items covered whether drawn or not. Null without one, when it
	///     covers no item, and when it was cancelled.
	/// </summary>
	public MokaBoxSelectRange? Range { get; }

	/// <summary>
	///     The selection this report asks for: what the box covers combined with the selection kept when it started,
	///     by <see cref="Mode" />. A cancelled box gives back the selection kept.
	/// </summary>
	/// <typeparam name="T">The app's key type.</typeparam>
	/// <param name="before">The selection kept when the box started.</param>
	/// <param name="covered">What the box covers, as the app's keys.</param>
	/// <param name="comparer">Compares keys. Null is the default for the type.</param>
	/// <returns>A new set, so the kept selection can be combined again on the next report.</returns>
	public HashSet<T> Combine<T>(IEnumerable<T> before, IEnumerable<T> covered, IEqualityComparer<T>? comparer = null)
	{
		ArgumentNullException.ThrowIfNull(before);
		ArgumentNullException.ThrowIfNull(covered);

		if (Phase == MokaBoxSelectPhase.Cancelled)
		{
			return new HashSet<T>(before, comparer);
		}

		switch (Mode)
		{
			case MokaBoxSelectMode.Add:
				HashSet<T> added = new(before, comparer);
				added.UnionWith(covered);
				return added;
			case MokaBoxSelectMode.Toggle:
				HashSet<T> toggled = new(before, comparer);
				toggled.SymmetricExceptWith(covered);
				return toggled;
			default:
				return new HashSet<T>(covered, comparer);
		}
	}
}
