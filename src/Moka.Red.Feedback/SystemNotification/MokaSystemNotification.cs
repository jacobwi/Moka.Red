namespace Moka.Red.Feedback.SystemNotification;

/// <summary>A notification the operating system shows outside the page, through <see cref="IMokaSystemNotifier" />.</summary>
public sealed record MokaSystemNotification
{
	/// <summary>The first line, in bold.</summary>
	public required string Title { get; init; }

	/// <summary>The text under the title.</summary>
	public string? Body { get; init; }

	/// <summary>The address of a picture beside the text, such as the app's icon.</summary>
	public string? Icon { get; init; }

	/// <summary>
	///     Names the notification, so a later one with the same tag replaces it rather than piling up, as a chat app
	///     does for one conversation.
	/// </summary>
	public string? Tag { get; init; }

	/// <summary>
	///     Shows the notification only while the page is out of sight (another tab, a minimized window), for news the
	///     page would show itself while someone is looking at it. Off by default.
	/// </summary>
	public bool OnlyWhenHidden { get; init; }

	/// <summary>
	///     Runs when the person clicks the notification, after the browser has brought the page's window forward. Its
	///     failure is reported in the browser's console, since nothing in the page asked for it.
	/// </summary>
	public Func<Task>? OnClick { get; init; }
}
