namespace Moka.Red.Feedback.Reconnect;

/// <summary>
///     The words <see cref="MokaReconnectModal" /> shows in each state. Set the ones that should say something
///     about your own app, or all of them to translate it.
/// </summary>
public sealed record MokaReconnectText
{
	/// <summary>Heading while the first attempt runs.</summary>
	public string ReconnectingTitle { get; init; } = "Reconnecting";

	/// <summary>Text while the first attempt runs.</summary>
	public string ReconnectingMessage { get; init; } = "The connection to the server dropped. Trying to get it back.";

	/// <summary>Heading between later attempts.</summary>
	public string RetryingTitle { get; init; } = "Still reconnecting";

	/// <summary>
	///     Text between later attempts. <c>{0}</c> marks where the seconds to the next attempt go, and Blazor
	///     keeps that number current.
	/// </summary>
	public string RetryingMessage { get; init; } = "Trying again in {0}s.";

	/// <summary>Heading once Blazor has stopped trying.</summary>
	public string FailedTitle { get; init; } = "Can't reach the server";

	/// <summary>Text once Blazor has stopped trying.</summary>
	public string FailedMessage { get; init; } = "Check your connection, then try again.";

	/// <summary>Heading when the server has paused this page's circuit. .NET 10 only.</summary>
	public string PausedTitle { get; init; } = "Paused while you were away";

	/// <summary>Text when the server has paused this page's circuit. .NET 10 only.</summary>
	public string PausedMessage { get; init; } =
		"The server set this page aside to save memory. Resume to carry on where you left off.";

	/// <summary>Heading when a paused circuit could not be resumed. .NET 10 only.</summary>
	public string ResumeFailedTitle { get; init; } = "Couldn't pick up where you left off";

	/// <summary>Text when a paused circuit could not be resumed. .NET 10 only.</summary>
	public string ResumeFailedMessage { get; init; } = "The server no longer has this page's place. Reload to start it again.";

	/// <summary>Label of the button that tries again after Blazor has stopped.</summary>
	public string TryAgainLabel { get; init; } = "Try again";

	/// <summary>Label of the button that resumes a paused circuit.</summary>
	public string ResumeLabel { get; init; } = "Resume";

	/// <summary>Label of the button that reloads the page.</summary>
	public string ReloadLabel { get; init; } = "Reload";
}
