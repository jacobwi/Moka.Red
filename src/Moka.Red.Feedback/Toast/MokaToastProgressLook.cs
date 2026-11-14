namespace Moka.Red.Feedback.Toast;

/// <summary>
///     How a toast with <see cref="MokaToastOptions.Progress" /> shows it. Only the new design draws the choice: on
///     <c>MokaTheme.Dark</c> and <c>MokaTheme.Light</c> every toast draws <see cref="Spinner" />.
/// </summary>
public enum MokaToastProgressLook
{
	/// <summary>A spinner in the icon's place and a bar along the toast's bottom edge.</summary>
	Spinner,

	/// <summary>A ring in the icon's place with the percent inside it.</summary>
	Ring,

	/// <summary>A bar under the message with the percent and, when it is known, the time left.</summary>
	Detailed,

	/// <summary>The percent on the same line as the message. Made for the Pill and Snackbar toast looks.</summary>
	Pill,

	/// <summary>Only the bar along the bottom edge; the severity icon stays as it is.</summary>
	BarOnly
}
