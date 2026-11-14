namespace Moka.Red.Feedback.Toast;

/// <summary>
///     What happens when a toast repeats one already showing: both have the same <see cref="MokaToastOptions.Key" />,
///     or, when neither has a key, the same severity, title and message.
/// </summary>
public enum MokaToastRepeats
{
	/// <summary>
	///     Each call shows a toast of its own, as before. A toast with a <see cref="MokaToastOptions.Key" /> is the
	///     exception: a key keeps one toast per key, so it acts as <see cref="Replace" />.
	/// </summary>
	Stack,

	/// <summary>The toast showing stays, counts the repeat ("×3") and starts its countdown again.</summary>
	Count,

	/// <summary>The newest words take the place of the toast showing, without a count, and its countdown starts again.</summary>
	Replace
}
