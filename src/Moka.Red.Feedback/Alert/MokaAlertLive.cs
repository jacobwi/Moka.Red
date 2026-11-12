namespace Moka.Red.Feedback.Alert;

/// <summary>How a <see cref="MokaAlert" /> tells a screen reader about itself.</summary>
public enum MokaAlertLive
{
	/// <summary>
	///     <c>role="alert"</c>: read out at once, interrupting what the screen reader was saying. For something the
	///     reader has to know now, such as a failed save.
	/// </summary>
	Alert,

	/// <summary>
	///     <c>role="status"</c>: read out politely, once the screen reader is idle. For news that can wait, such as
	///     "Saved".
	/// </summary>
	Status,

	/// <summary>
	///     No live role: the alert is read where it sits, like a callout. For a message that is part of the page from
	///     the start rather than news.
	/// </summary>
	None
}
