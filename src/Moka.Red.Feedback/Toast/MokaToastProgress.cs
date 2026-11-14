namespace Moka.Red.Feedback.Toast;

/// <summary>
///     How far along the work a toast reports is: a share from 0 to 1, or <see cref="Indeterminate" /> when nobody
///     knows. A number converts to it, so <c>o.Progress = 0.4</c> works. A toast with progress doesn't count down; it
///     stays until an update takes the progress away or something dismisses it.
/// </summary>
public readonly record struct MokaToastProgress
{
	/// <summary>A share of the work done.</summary>
	/// <param name="value">From 0 to 1. Values past either end are held at it; NaN is refused.</param>
	/// <param name="remaining">How long the rest will take, when the app knows. Shown by <see cref="MokaToastProgressLook.Detailed" />.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="value" /> is NaN.</exception>
	public MokaToastProgress(double value, TimeSpan? remaining = null)
	{
		if (double.IsNaN(value))
		{
			throw new ArgumentOutOfRangeException(nameof(value), value, "A share of the work can't be NaN.");
		}

		Value = Math.Clamp(value, 0, 1);
		Remaining = remaining;
	}

	/// <summary>Work under way whose share nobody knows.</summary>
	public static MokaToastProgress Indeterminate => default;

	/// <summary>The share done, from 0 to 1, or null while <see cref="IsIndeterminate" />.</summary>
	public double? Value { get; }

	/// <summary>
	///     How long the rest will take, as the app reported it. When it's null the toast estimates it from how fast the
	///     share has moved, once it has moved enough for the estimate to mean something.
	/// </summary>
	public TimeSpan? Remaining { get; }

	/// <summary>Whether nobody knows the share.</summary>
	public bool IsIndeterminate => Value is null;

	/// <summary>A share of the work done, from 0 to 1.</summary>
	/// <param name="value">The share.</param>
	public static implicit operator MokaToastProgress(double value) => new(value);

	/// <summary>A share of the work done, from 0 to 1. The same as the conversion from a number.</summary>
	/// <param name="value">The share.</param>
	/// <returns>The progress.</returns>
	public static MokaToastProgress FromDouble(double value) => new(value);
}
