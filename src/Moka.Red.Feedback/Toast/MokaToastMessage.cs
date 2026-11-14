using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;

namespace Moka.Red.Feedback.Toast;

/// <summary>
///     One toast as it stands. Immutable: <see cref="IMokaToastService" /> replaces a toast with a new record when it
///     changes, so a snapshot read from <see cref="IMokaToastService.Toasts" /> never changes under its reader, and a
///     record that is the same instance as before is a toast that didn't change.
/// </summary>
public sealed record MokaToastMessage
{
	/// <summary>Unique identifier for this toast.</summary>
	public required Guid Id { get; init; }

	/// <summary>The body text.</summary>
	public required string Message { get; init; }

	/// <summary>Color and icon.</summary>
	public required MokaToastSeverity Severity { get; init; }

	/// <inheritdoc cref="MokaToastOptions.Title" />
	public string? Title { get; init; }

	/// <inheritdoc cref="MokaToastOptions.Duration" />
	public TimeSpan? Duration { get; init; }

	/// <inheritdoc cref="MokaToastOptions.ShowCloseButton" />
	public bool ShowCloseButton { get; init; } = true;

	/// <inheritdoc cref="MokaToastOptions.ShowIcon" />
	public bool ShowIcon { get; init; } = true;

	/// <inheritdoc cref="MokaToastOptions.Icon" />
	public MokaIconDefinition? Icon { get; init; }

	/// <inheritdoc cref="MokaToastOptions.OnClick" />
	public Action? OnClick { get; init; }

	/// <inheritdoc cref="MokaToastOptions.OnClickAsync" />
	public Func<Task>? OnClickAsync { get; init; }

	/// <inheritdoc cref="MokaToastOptions.ActionText" />
	public string? ActionText { get; init; }

	/// <inheritdoc cref="MokaToastOptions.OnAction" />
	public Action? OnAction { get; init; }

	/// <inheritdoc cref="MokaToastOptions.OnActionAsync" />
	public Func<Task>? OnActionAsync { get; init; }

	/// <inheritdoc cref="MokaToastOptions.CloseOnAction" />
	public bool CloseOnAction { get; init; } = true;

	/// <inheritdoc cref="MokaToastOptions.Key" />
	public string? Key { get; init; }

	/// <inheritdoc cref="MokaToastOptions.Repeats" />
	public MokaToastRepeats Repeats { get; init; }

	/// <inheritdoc cref="MokaToastOptions.Progress" />
	public MokaToastProgress? Progress { get; init; }

	/// <inheritdoc cref="MokaToastOptions.ProgressLook" />
	public MokaToastProgressLook? ProgressLook { get; init; }

	/// <inheritdoc cref="MokaToastOptions.AlsoInCenter" />
	public bool AlsoInCenter { get; init; }

	/// <inheritdoc cref="MokaToastOptions.Content" />
	public RenderFragment? Content { get; init; }

	/// <inheritdoc cref="MokaToastOptions.OnDismissed" />
	public Action<MokaToastDismissReason>? OnDismissed { get; init; }

	/// <summary>How many times the toast has been shown, with <see cref="MokaToastRepeats.Count" />. 1 for one.</summary>
	public int Count { get; init; } = 1;

	/// <summary>
	///     Counts the toast's countdowns. It moves on whenever the countdown starts again (a repeat, new words, the end
	///     of its progress), and a host ends the toast only for the countdown it drew.
	/// </summary>
	public int Run { get; init; }

	/// <summary>Moves on whenever screen readers should hear the toast again: new words, or the end of its progress.</summary>
	public int Announcement { get; init; }

	/// <summary>When the toast was first shown, from the registered <see cref="TimeProvider" />, else the system clock.</summary>
	public DateTimeOffset CreatedAt { get; init; }

	/// <summary>When the toast last changed.</summary>
	public DateTimeOffset UpdatedAt { get; init; }

	/// <summary>
	///     How long the rest of the work should take, from how fast <see cref="Progress" /> has moved since it first had a
	///     value. Null until at least three seconds and five percent have passed, since an earlier guess means little.
	/// </summary>
	public TimeSpan? EstimatedRemaining { get; init; }

	/// <summary>Whether the toast counts down: it has a <see cref="Duration" /> and no <see cref="Progress" />.</summary>
	public bool CountsDown => Duration is { } duration && duration > TimeSpan.Zero && Progress is null;

	/// <summary>The time left to show: what the app reported, else the estimate.</summary>
	public TimeSpan? Remaining => Progress?.Remaining ?? EstimatedRemaining;

	internal DateTimeOffset? ProgressStartedAt { get; init; }

	internal double ProgressStartValue { get; init; }
}
