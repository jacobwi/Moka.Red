namespace Moka.Red.Feedback.Notification;

/// <summary>A button on a notification in the center, such as "Retry" or "Open".</summary>
public sealed record MokaNotificationAction
{
	/// <summary>A button that runs <paramref name="onClick" />.</summary>
	/// <param name="text">The words on the button.</param>
	/// <param name="onClick">Runs when the button is pressed. A failure reaches the app's error handling.</param>
	public MokaNotificationAction(string text, Func<Task> onClick)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(text);
		ArgumentNullException.ThrowIfNull(onClick);
		Text = text;
		OnClick = onClick;
	}

	/// <summary>A button that runs <paramref name="onClick" />.</summary>
	/// <param name="text">The words on the button.</param>
	/// <param name="onClick">Runs when the button is pressed.</param>
	public MokaNotificationAction(string text, Action onClick)
		: this(text, Wrap(onClick))
	{
	}

	/// <summary>The words on the button.</summary>
	public string Text { get; }

	/// <summary>Runs when the button is pressed.</summary>
	public Func<Task> OnClick { get; }

	/// <summary>Whether pressing the button also marks the notification read. On by default.</summary>
	public bool MarksRead { get; init; } = true;

	/// <summary>Whether pressing the button also takes the notification out of the center once it has run.</summary>
	public bool Removes { get; init; }

	private static Func<Task> Wrap(Action onClick)
	{
		ArgumentNullException.ThrowIfNull(onClick);
		return () =>
		{
			onClick();
			return Task.CompletedTask;
		};
	}
}
